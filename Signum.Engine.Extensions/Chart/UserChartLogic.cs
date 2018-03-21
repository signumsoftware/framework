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

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                UserAssetsImporter.RegisterName<UserChartEntity>("UserChart");

                sb.Schema.Synchronizing += Schema_Synchronizing;

                sb.Include<UserChartEntity>()
                    .WithSave(UserChartOperation.Save)
                    .WithDelete(UserChartOperation.Delete)
                    .WithQuery(dqm, () => uq => new
                    {
                        Entity = uq,
                        uq.Id,
                        uq.Query,
                        uq.EntityType,
                        uq.DisplayName,
                        uq.ChartScript,
                        uq.GroupResults,
                    });

                sb.Schema.EntityEvents<UserChartEntity>().Retrieved += ChartLogic_Retrieved;

                sb.Schema.Table<QueryEntity>().PreDeleteSqlSync += e =>
                  Administrator.UnsafeDeletePreCommand(Database.Query<UserChartEntity>().Where(a => a.Query == e));
                
               
                UserCharts = sb.GlobalLazy(() => Database.Query<UserChartEntity>().ToDictionary(a => a.ToLite()),
                 new InvalidateWith(typeof(UserChartEntity)));

                UserChartsByQuery = sb.GlobalLazy(() => UserCharts.Value.Values.Where(a => a.EntityType == null).GroupToDictionary(a => a.Query.ToQueryName(), a => a.ToLite()),
                    new InvalidateWith(typeof(UserChartEntity)));

                UserChartsByTypeForQuickLinks = sb.GlobalLazy(() => UserCharts.Value.Values.Where(a => a.EntityType != null && !a.HideQuickLink)
                .SelectCatch(a => new { Type = TypeLogic.IdToType.GetOrThrow(a.EntityType.Id), Lite = a.ToLite() })
                .GroupToDictionary(a => a.Type, a => a.Lite),
                    new InvalidateWith(typeof(UserChartEntity)));
            }
        }

        public static UserChartEntity ParseData(this UserChartEntity userChart)
        {
            if (!userChart.IsNew || userChart.queryName == null)
                throw new InvalidOperationException("userChart should be new and have queryName");

            userChart.Query = QueryLogic.GetQueryEntity(userChart.queryName);

            QueryDescription description = DynamicQueryManager.Current.QueryDescription(userChart.queryName);

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

            QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

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

        internal static ChartRequest ToChartRequest(UserChartEntity userChart)
        {
            var cr = new ChartRequest(userChart.Query.ToQueryName())
            {
                ChartScript = userChart.ChartScript,
                Filters = userChart.Filters.Select(qf =>
                    new Filter(qf.Token.Token, qf.Operation, FilterValueConverter.Parse(qf.ValueString, qf.Token.Token.Type, qf.Operation.IsList(), allowSmart: true)))
                .ToList(),
                GroupResults = userChart.GroupResults,
                Orders = userChart.Orders.Select(qo => new Order(qo.Token.Token, qo.OrderType)).ToList(),
                Parameters = userChart.Parameters.ToMList(),
            };
            
            cr.Columns.ZipForeach(userChart.Columns, (a, b) =>
            {
                a.Token = b.Token == null ? null : new QueryTokenEmbedded(b.Token.Token);
                a.DisplayName = b.DisplayName;
            });

            return cr;
        }

        public static void RegisterUserTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((UserChartEntity uq) => uq.Owner, typeof(UserEntity));

            TypeConditionLogic.RegisterCompile<UserChartEntity>(typeCondition, uq => uq.Owner.RefersTo(UserEntity.Current));
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
            try
            {
                Console.Clear();

                SafeConsole.WriteLineColor(ConsoleColor.White, "UserChart: " + uc.DisplayName);
                Console.WriteLine(" ChartScript: " + uc.ChartScript.ToString());
                Console.WriteLine(" Query: " + uc.Query.Key);

                if (uc.Filters.Any(a => a.Token.ParseException != null) ||
                   uc.Columns.Any(a => a.Token != null && a.Token.ParseException != null) ||
                   uc.Orders.Any(a => a.Token.ParseException != null))
                {
                    QueryDescription qd = DynamicQueryManager.Current.QueryDescription(uc.Query.ToQueryName());

                    SubTokensOptions canAggregate = uc.GroupResults ? SubTokensOptions.CanAggregate : 0;

                    if (uc.Filters.Any())
                    {
                        Console.WriteLine(" Filters:");
                        foreach (var item in uc.Filters.ToList())
                        {
                            QueryTokenEmbedded token = item.Token;
                            switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canAggregate, "{0} {1}".FormatWith(item.Operation, item.ValueString), allowRemoveToken: true, allowReCreate: false))
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

                    if (uc.Columns.Any())
                    {
                        Console.WriteLine(" Columns:");
                        foreach (var item in uc.Columns.ToList())
                        {
                            QueryTokenEmbedded token = item.Token;
                            if (item.Token == null)
                                continue;

                            switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement | canAggregate, item.ScriptColumn.DisplayName, allowRemoveToken: item.ScriptColumn.IsOptional, allowReCreate: false))
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

                    if (uc.Orders.Any())
                    {
                        Console.WriteLine(" Orders:");
                        foreach (var item in uc.Orders.ToList())
                        {
                            QueryTokenEmbedded token = item.Token;
                            switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement | canAggregate, item.OrderType.ToString(), allowRemoveToken: true, allowReCreate: false))
                            {
                                case FixTokenResult.Nothing: break;
                                case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(uc, u => u.Guid == uc.Guid);
                                case FixTokenResult.RemoveToken: uc.Orders.Remove(item); break;
                                case FixTokenResult.SkipEntity: return null;
                                case FixTokenResult.Fix: item.Token = token; break;
                                default: break;
                            }
                        }
                    }
                }

                foreach (var item in uc.Filters.ToList())
                {
                    string val = item.ValueString;
                    switch (QueryTokenSynchronizer.FixValue(replacements, item.Token.Token.Type, ref val, allowRemoveToken: true, isList: item.Operation.IsList()))
                    {
                        case FixTokenResult.Nothing: break;
                        case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(uc, u => u.Guid == uc.Guid);
                        case FixTokenResult.RemoveToken: uc.Filters.Remove(item); break;
                        case FixTokenResult.SkipEntity: return null;
                        case FixTokenResult.Fix: item.ValueString = val; break;
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
            finally
            {
                Console.Clear();
            }
        }

        private static FixTokenResult FixParameter(ChartParameterEmbedded item, ref string val)
        {
            var error = item.PropertyCheck(nameof(item.Value));
            if (error == null)
                return FixTokenResult.Nothing;

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
