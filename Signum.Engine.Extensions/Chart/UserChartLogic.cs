using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Engine.UserAssets;
using Signum.Engine.ViewLog;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Chart;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserAssets;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Signum.Engine.Chart
{
    public static class UserChartLogic
    {
        public static ResetLazy<Dictionary<Lite<UserChartEntity>, UserChartEntity>> UserCharts;
        public static ResetLazy<Dictionary<Type, List<Lite<UserChartEntity>>>> UserChartsByTypeForQuickLinks;
        public static ResetLazy<Dictionary<object, List<Lite<UserChartEntity>>>> UserChartsByQuery;

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                UserAssetsImporter.RegisterName<UserChartEntity>("UserChart");

                sb.Schema.Synchronizing += Schema_Synchronizing;

                sb.Include<UserChartEntity>()
                    .WithSave(UserChartOperation.Save)
                    .WithDelete(UserChartOperation.Delete)
                    .WithQuery(() => uq => new
                    {
                        Entity = uq,
                        uq.Id,
                        uq.Query,
                        uq.EntityType,
                        uq.DisplayName,
                        uq.ChartScript,
                    });

                sb.Schema.EntityEvents<UserChartEntity>().Retrieved += ChartLogic_Retrieved;

                sb.Schema.Table<QueryEntity>().PreDeleteSqlSync += e =>
                  Administrator.UnsafeDeletePreCommand(Database.Query<UserChartEntity>().Where(a => a.Query == e));
                
               
                UserCharts = sb.GlobalLazy(() => Database.Query<UserChartEntity>().ToDictionary(a => a.ToLite()),
                 new InvalidateWith(typeof(UserChartEntity)));

                UserChartsByQuery = sb.GlobalLazy(() => UserCharts.Value.Values.Where(a => a.EntityType == null).SelectCatch(uc => KVP.Create(uc.Query.ToQueryName(), uc.ToLite())).GroupToDictionary(),
                    new InvalidateWith(typeof(UserChartEntity)));

                UserChartsByTypeForQuickLinks = sb.GlobalLazy(() => UserCharts.Value.Values.Where(a => a.EntityType != null && !a.HideQuickLink)
                .SelectCatch(a => KVP.Create(TypeLogic.IdToType.GetOrThrow(a.EntityType.Id), a.ToLite()))
                .GroupToDictionary(),
                    new InvalidateWith(typeof(UserChartEntity)));
            }
        }

        public static UserChartEntity ParseData(this UserChartEntity userChart)
        {
            if (!userChart.IsNew || userChart.queryName == null)
                throw new InvalidOperationException("userChart should be new and have queryName");

            userChart.Query = QueryLogic.GetQueryEntity(userChart.queryName);

            QueryDescription description = QueryLogic.Queries.QueryDescription(userChart.queryName);

            userChart.ParseData(description);

            return userChart;
        }

        static void ChartLogic_Retrieved(UserChartEntity userChart)
        {
            object queryName;
            try
            {
                queryName = QueryLogic.ToQueryName(userChart.Query.Key);
            }
            catch (KeyNotFoundException ex) when (StartParameters.IgnoredCodeErrors != null)
            {
                StartParameters.IgnoredCodeErrors.Add(ex);

                return;
            }

            QueryDescription description = QueryLogic.Queries.QueryDescription(queryName);

            foreach (var item in userChart.Columns)
            {
                item.parentChart = userChart;
            }

            userChart.ParseData(description);
        }

        public static List<Lite<UserChartEntity>> GetUserCharts(object queryName)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<UserChartEntity>(userInterface: true);

            return UserChartsByQuery.Value.TryGetC(queryName).EmptyIfNull()
                .Where(e => isAllowed(UserCharts.Value.GetOrThrow(e))).ToList();
        }

        public static List<Lite<UserChartEntity>> GetUserChartsEntity(Type entityType)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<UserChartEntity>(userInterface: true);

            return UserChartsByTypeForQuickLinks.Value.TryGetC(entityType).EmptyIfNull()
                .Where(e => isAllowed(UserCharts.Value.GetOrThrow(e))).ToList();
        }

        public static List<Lite<UserChartEntity>> Autocomplete(string subString, int limit)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<UserChartEntity>(userInterface: true);

            return UserCharts.Value.Where(a => a.Value.EntityType == null && isAllowed(a.Value))
                .Select(a => a.Key).Autocomplete(subString, limit).ToList();
        }

        public static UserChartEntity RetrieveUserChart(this Lite<UserChartEntity> userChart)
        {
            using (ViewLogLogic.LogView(userChart, "UserChart"))
            {
                var result = UserCharts.Value.GetOrThrow(userChart);

                var isAllowed = Schema.Current.GetInMemoryFilter<UserChartEntity>(userInterface: true);
                if (!isAllowed(result))
                    throw new EntityNotFoundException(userChart.EntityType, userChart.Id);

                return result;
            }
        }

        internal static ChartRequestModel ToChartRequest(UserChartEntity userChart)
        {
            var cr = new ChartRequestModel(userChart.Query.ToQueryName())
            {
                ChartScript = userChart.ChartScript,
                Filters = userChart.Filters.ToFilterList(),
                Parameters = userChart.Parameters.ToMList(),
            };
            
            cr.Columns.ZipForeach(userChart.Columns, (a, b) =>
            {
                a.Token = b.Token == null ? null : new QueryTokenEmbedded(b.Token.Token);
                a.DisplayName = b.DisplayName;
                a.OrderByIndex = b.OrderByIndex;
                a.OrderByType = b.OrderByType;
            });

            return cr;
        }

        public static void RegisterUserTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((UserChartEntity uq) => uq.Owner, typeof(UserEntity));

            TypeConditionLogic.RegisterCompile<UserChartEntity>(typeCondition, uq => uq.Owner.Is(UserEntity.Current));
        }


        public static void RegisterRoleTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((UserChartEntity uq) => uq.Owner, typeof(RoleEntity));

            TypeConditionLogic.RegisterCompile<UserChartEntity>(typeCondition, uq => AuthLogic.CurrentRoles().Contains(uq.Owner));
        }

        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            if (!replacements.Interactive)
                return null;

            var list = Database.Query<UserChartEntity>().ToList();

            var table = Schema.Current.Table(typeof(UserChartEntity));

            using (replacements.WithReplacedDatabaseName())
            {
                SqlPreCommand cmd = list.Select(uq => ProcessUserChart(replacements, table, uq)).Combine(Spacing.Double);

                return cmd;
            }
        }
        
        static SqlPreCommand ProcessUserChart(Replacements replacements, Table table, UserChartEntity uc)
        {
            Console.Write(".");
            using (DelayedConsole.Delay(() => SafeConsole.WriteLineColor(ConsoleColor.White, "UserChart: " + uc.DisplayName)))
            using (DelayedConsole.Delay(() => Console.WriteLine(" ChartScript: " + uc.ChartScript.ToString())))
            using (DelayedConsole.Delay(() => Console.WriteLine(" Query: " + uc.Query.Key)))
            {
                try
                {
                    if (uc.Filters.Any(a => a.Token?.ParseException != null) ||
                       uc.Columns.Any(a => a.Token?.ParseException != null))
                    {
                        QueryDescription qd = QueryLogic.Queries.QueryDescription(uc.Query.ToQueryName());

                        if (uc.Filters.Any())
                        {
                            using (DelayedConsole.Delay(() => Console.WriteLine(" Filters:")))
                            {
                                foreach (var item in uc.Filters.ToList())
                                {
                                    QueryTokenEmbedded token = item.Token;
                                    if (item.Token != null)
                                    {
                                        switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate, "{0} {1}".FormatWith(item.Operation, item.ValueString), allowRemoveToken: true, allowReCreate: false))
                                        {
                                            case FixTokenResult.Nothing: break;
                                            case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(uc, u => u.Guid == uc.Guid);
                                            case FixTokenResult.RemoveToken: uc.Filters.Remove(item); break;
                                            case FixTokenResult.SkipEntity: return null;
                                            case FixTokenResult.Fix: item.Token = token; break;
                                            default: break;
                                        }
                                    }
                                }
                            }
                        }

                        if (uc.Columns.Any())
                        {
                            using (DelayedConsole.Delay(() => Console.WriteLine(" Columns:")))
                            {
                                foreach (var item in uc.Columns.ToList())
                                {
                                    QueryTokenEmbedded token = item.Token;
                                    if (item.Token == null)
                                        continue;

                                    switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement | SubTokensOptions.CanAggregate, item.ScriptColumn.DisplayName, allowRemoveToken: item.ScriptColumn.IsOptional, allowReCreate: false))
                                    {
                                        case FixTokenResult.Nothing: break;
                                        case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(uc, u => u.Guid == uc.Guid);
                                        case FixTokenResult.RemoveToken: item.Token = null; break;
                                        case FixTokenResult.SkipEntity: return null;
                                        case FixTokenResult.Fix: item.Token = token; break;
                                        default: break;
                                    }
                                }
                            }
                        }
                    }

                    foreach (var item in uc.Filters.Where(f => !f.IsGroup).ToList())
                    {
                        retry:
                        string val = item.ValueString;
                        switch (QueryTokenSynchronizer.FixValue(replacements, item.Token.Token.Type, ref val, allowRemoveToken: true, isList: item.Operation.Value.IsList()))
                        {
                            case FixTokenResult.Nothing: break;
                            case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(uc, u => u.Guid == uc.Guid);
                            case FixTokenResult.RemoveToken: uc.Filters.Remove(item); break;
                            case FixTokenResult.SkipEntity: return null;
                            case FixTokenResult.Fix: item.ValueString = val; goto retry;
                        }
                    }

                    foreach (var item in uc.Columns)
                    {
                        uc.FixParameters(item);
                    }

                    foreach (var item in uc.Parameters)
                    {
                        string val = item.Value;
                        retry:
                        switch (FixParameter(item, ref val))
                        {
                            case FixTokenResult.Nothing: break;
                            case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(uc, u => u.Guid == uc.Guid);
                            case FixTokenResult.RemoveToken: uc.Parameters.Remove(item); break;
                            case FixTokenResult.SkipEntity: return null;
                            case FixTokenResult.Fix: { item.Value = val; goto retry; }
                        }
                    }


                    try
                    {
                        return table.UpdateSqlSync(uc, u => u.Guid == uc.Guid, includeCollections: true);
                    }
                    catch (Exception e)
                    {
                        DelayedConsole.Flush();
                        Console.WriteLine("Integrity Error:");
                        SafeConsole.WriteLineColor(ConsoleColor.DarkRed, e.Message);
                        while (true)
                        {
                            SafeConsole.WriteLineColor(ConsoleColor.Yellow, "- s: Skip entity");
                            SafeConsole.WriteLineColor(ConsoleColor.Red, "- d: Delete entity");

                            string answer = Console.ReadLine();

                            if (answer == null)
                                throw new InvalidOperationException("Impossible to synchronize interactively without Console");

                            answer = answer.ToLower();

                            if (answer == "s")
                                return null;

                            if (answer == "d")
                                return table.DeleteSqlSync(uc, u => u.Guid == uc.Guid);
                        }
                    }
                }
                catch (Exception e)
                {
                    return new SqlPreCommandSimple("-- Exception in {0}: {1}".FormatWith(uc.BaseToString(), e.Message));
                }
            }
        }

        private static FixTokenResult FixParameter(ChartParameterEmbedded item, ref string val)
        {
            var error = item.PropertyCheck(nameof(item.Value));
            if (error == null)
                return FixTokenResult.Nothing;

            DelayedConsole.Flush();

            SafeConsole.WriteLineColor(ConsoleColor.White, "Parameter Name: {0}".FormatWith(item.ScriptParameter.Name));
            SafeConsole.WriteLineColor(ConsoleColor.White, "Parameter Definition: {0}".FormatWith(item.ScriptParameter.ValueDefinition));
            SafeConsole.WriteLineColor(ConsoleColor.White, "CurrentValue: {0}".FormatWith(item.Value));
            SafeConsole.WriteLineColor(ConsoleColor.White, "Error: {0}.".FormatWith(error));
            SafeConsole.WriteLineColor(ConsoleColor.Yellow, "- s: Skip entity");
            SafeConsole.WriteLineColor(ConsoleColor.DarkRed, "- r: Remove parame");
            SafeConsole.WriteLineColor(ConsoleColor.Red, "- d: Delete entity");
            SafeConsole.WriteLineColor(ConsoleColor.Green, "- freeText: New value");

            string answer = Console.ReadLine();

            if (answer == null)
                throw new InvalidOperationException("Impossible to synchronize interactively without Console");

            string a = answer.ToLower();

            if (a == "s")
                return FixTokenResult.SkipEntity;

            if (a == "r")
                return FixTokenResult.RemoveToken;

            if (a == "d")
                return FixTokenResult.DeleteEntity;

            val = answer;
            return FixTokenResult.Fix;
        }
    }
}
