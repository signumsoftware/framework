using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using System.Reflection;
using Signum.Entities.UserQueries;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using Signum.Entities.Basics;
using Signum.Engine.Operations;
using Signum.Utilities;
using Signum.Engine.UserAssets;
using Signum.Entities.UserAssets;
using Signum.Engine.ViewLog;
using Signum.Engine.Exceptions;

namespace Signum.Engine.UserQueries
{
    public static class UserQueryLogic
    {
        public static ResetLazy<Dictionary<Lite<UserQueryDN>, UserQueryDN>> UserQueries;
        public static ResetLazy<Dictionary<Type, List<Lite<UserQueryDN>>>> UserQueriesByType;
        public static ResetLazy<Dictionary<object, List<Lite<UserQueryDN>>>> UserQueriesByQuery;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryLogic.Start(sb);

                PermissionAuthLogic.RegisterPermissions(UserQueryPermission.ViewUserQuery);

                UserAssetsImporter.UserAssetNames.Add("UserQuery", typeof(UserQueryDN));

                sb.Schema.Synchronizing += Schema_Synchronizing;

                sb.Include<UserQueryDN>();

                dqm.RegisterQuery(typeof(UserQueryDN), () =>
                    from uq in Database.Query<UserQueryDN>()
                    select new
                    {
                        Entity = uq,
                        uq.Query,
                        uq.Id,
                        uq.DisplayName,
                        uq.EntityType,
                    });

                sb.Schema.EntityEvents<UserQueryDN>().Retrieved += UserQueryLogic_Retrieved;

                new Graph<UserQueryDN>.Execute(UserQueryOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (uq, _) => { }
                }.Register();

                new Graph<UserQueryDN>.Delete(UserQueryOperation.Delete)
                {
                    Lite = true,
                    Delete = (uq, _) => uq.Delete()
                }.Register();

                UserQueries = sb.GlobalLazy(() => Database.Query<UserQueryDN>().ToDictionary(a => a.ToLite()),
                    new InvalidateWith(typeof(UserQueryDN)));

                UserQueriesByQuery = sb.GlobalLazy(() => UserQueries.Value.Values.Where(a => a.EntityType == null).GroupToDictionary(a => a.Query.ToQueryName(), a => a.ToLite()),
                    new InvalidateWith(typeof(UserQueryDN)));

                UserQueriesByType = sb.GlobalLazy(() => UserQueries.Value.Values.Where(a => a.EntityType != null).GroupToDictionary(a => TypeLogic.IdToType.GetOrThrow(a.EntityType.Id), a => a.ToLite()),
                    new InvalidateWith(typeof(UserQueryDN)));
            }
        }

        public static UserQueryDN ParseAndSave(this UserQueryDN userQuery)
        {
            if (!userQuery.IsNew || userQuery.queryName == null)
                throw new InvalidOperationException("userQuery should be new and have queryName");

            userQuery.Query = QueryLogic.GetQuery(userQuery.queryName);

            QueryDescription description = DynamicQueryManager.Current.QueryDescription(userQuery.queryName);

            userQuery.ParseData(description);

            return userQuery.Execute(UserQueryOperation.Save);
        }


        static void UserQueryLogic_Retrieved(UserQueryDN userQuery)
        {
            object queryName = QueryLogic.ToQueryName(userQuery.Query.Key);
            QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

            userQuery.ParseData(description);
        }

        public static List<Lite<UserQueryDN>> GetUserQueries(object queryName)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<UserQueryDN>(userInterface: true);

            return UserQueriesByQuery.Value.TryGetC(queryName).EmptyIfNull()
                .Where(e => isAllowed(UserQueries.Value.GetOrThrow(e))).ToList();
        }

        public static List<Lite<UserQueryDN>> GetUserQueriesEntity(Type entityType)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<UserQueryDN>(userInterface: true);

            return UserQueriesByType.Value.TryGetC(entityType).EmptyIfNull()
                .Where(e => isAllowed(UserQueries.Value.GetOrThrow(e))).ToList();
        }

        public static List<Lite<UserQueryDN>> Autocomplete(string subString, int limit)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<UserQueryDN>(userInterface: true);

            return UserQueries.Value.Where(a => a.Value.EntityType == null && isAllowed(a.Value))
                .Select(a => a.Key).Autocomplete(subString, limit).ToList();
        }

        public static UserQueryDN RetrieveUserQuery(this Lite<UserQueryDN> userQuery)
        {
            using (ViewLogLogic.LogView(userQuery, "UserQuery"))
            {
                var result = UserQueries.Value.GetOrThrow(userQuery);

                var isAllowed = Schema.Current.GetInMemoryFilter<UserQueryDN>(userInterface: true);
                if (!isAllowed(result))
                    throw new EntityNotFoundException(userQuery.EntityType, userQuery.Id);

                return result;
            }
        }

        public static void RegisterUserTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((UserQueryDN uq) => uq.Owner, typeof(UserDN));

            TypeConditionLogic.RegisterCompile<UserQueryDN>(typeCondition,
                uq => uq.Owner.RefersTo(UserDN.Current));
        }

        public static void RegisterRoleTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((UserQueryDN uq) => uq.Owner, typeof(RoleDN));

            TypeConditionLogic.RegisterCompile<UserQueryDN>(typeCondition,
                uq => AuthLogic.CurrentRoles().Contains(uq.Owner));
        }


        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            if (!SafeConsole.IsConsolePresent)
                return null;

            var list = Database.Query<UserQueryDN>().ToList();

            var table = Schema.Current.Table(typeof(UserQueryDN));

            SqlPreCommand cmd = list.Select(uq => ProcessUserQuery(replacements, table, uq)).Combine(Spacing.Double);

            return cmd;
        }

        static SqlPreCommand ProcessUserQuery(Replacements replacements, Table table, UserQueryDN uq)
        {
            try
            {
                Console.Clear();

                SafeConsole.WriteLineColor(ConsoleColor.White, "UserQuery: " + uq.DisplayName);
                Console.WriteLine(" Query: " + uq.Query.Key);

                if (uq.Filters.Any(a => a.Token.ParseException != null) ||
                   uq.Columns.Any(a => a.Token != null && a.Token.ParseException != null) ||
                   uq.Orders.Any(a => a.Token.ParseException != null))
                {

                    QueryDescription qd = DynamicQueryManager.Current.QueryDescription(uq.Query.ToQueryName());

                    if (uq.Filters.Any())
                    {
                        Console.WriteLine(" Filters:");
                        foreach (var item in uq.Filters.ToList())
                        {
                            QueryTokenDN token = item.Token;
                            switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement, "{0} {1}".Formato(item.Operation, item.ValueString)))
                            {
                                case FixTokenResult.Nothing: break;
                                case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(uq);
                                case FixTokenResult.RemoveToken: uq.Filters.Remove(item); break;
                                case FixTokenResult.SkipEntity: return null;
                                case FixTokenResult.Fix: item.Token = token; break;
                                default: break;
                            }
                        }
                    }

                    if (uq.Columns.Any())
                    {
                        Console.WriteLine(" Columns:");
                        foreach (var item in uq.Columns.ToList())
                        {
                            QueryTokenDN token = item.Token;
                            switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement, item.DisplayName.HasText() ? "'{0}'".Formato(item.DisplayName) : null))
                            {
                                case FixTokenResult.Nothing: break;
                                case FixTokenResult.DeleteEntity: ; return table.DeleteSqlSync(uq);
                                case FixTokenResult.RemoveToken: uq.Columns.Remove(item); break;
                                case FixTokenResult.SkipEntity: return null;
                                case FixTokenResult.Fix: item.Token = token; break;
                                default: break;
                            }
                        }
                    }

                    if (uq.Orders.Any())
                    {
                        Console.WriteLine(" Orders:");
                        foreach (var item in uq.Orders.ToList())
                        {
                            QueryTokenDN token = item.Token;
                            switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement, item.OrderType.ToString()))
                            {
                                case FixTokenResult.Nothing: break;
                                case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(uq);
                                case FixTokenResult.RemoveToken: uq.Orders.Remove(item); break;
                                case FixTokenResult.SkipEntity: return null;
                                case FixTokenResult.Fix: item.Token = token; break;
                                default: break;
                            }
                        }
                    }
                }

                foreach (var item in uq.Filters.ToList())
                {
                retry:
                    string val = item.ValueString;
                    switch (QueryTokenSynchronizer.FixValue(replacements, item.Token.Token.Type, ref val, allowRemoveToken: true, isList: item.Operation == FilterOperation.IsIn))
                    {
                        case FixTokenResult.Nothing: break;
                        case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(uq);
                        case FixTokenResult.RemoveToken: uq.Filters.Remove(item); break;
                        case FixTokenResult.SkipEntity: return null;
                        case FixTokenResult.Fix: item.ValueString = val; goto retry;
                    }
                }

                if (uq.WithoutFilters)
                    uq.Filters.Clear();

                if (!uq.ShouldHaveElements && uq.ElementsPerPage.HasValue)
                    uq.ElementsPerPage = null;

                if (uq.ShouldHaveElements && !uq.ElementsPerPage.HasValue)
                    uq.ElementsPerPage = 20;

                Console.Clear();

                return table.UpdateSqlSync(uq, includeCollections: true);
            }
            catch (Exception e)
            {
                return new SqlPreCommandSimple("-- Exception in {0}: {1}".Formato(uq.BaseToString(), e.Message));
            }
        }
    }
}
