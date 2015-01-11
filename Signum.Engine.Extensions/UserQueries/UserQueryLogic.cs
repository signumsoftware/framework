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

namespace Signum.Engine.UserQueries
{
    public static class UserQueryLogic
    {
        public static ResetLazy<Dictionary<Lite<UserQueryEntity>, UserQueryEntity>> UserQueries;
        public static ResetLazy<Dictionary<Type, List<Lite<UserQueryEntity>>>> UserQueriesByType;
        public static ResetLazy<Dictionary<object, List<Lite<UserQueryEntity>>>> UserQueriesByQuery;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryLogic.Start(sb);

                PermissionAuthLogic.RegisterPermissions(UserQueryPermission.ViewUserQuery);

                UserAssetsImporter.UserAssetNames.Add("UserQuery", typeof(UserQueryEntity));

                sb.Schema.Synchronizing += Schema_Synchronizing;

                sb.Include<UserQueryEntity>();

                dqm.RegisterQuery(typeof(UserQueryEntity), () =>
                    from uq in Database.Query<UserQueryEntity>()
                    select new
                    {
                        Entity = uq,
                        uq.Query,
                        uq.Id,
                        uq.DisplayName,
                        uq.EntityType,
                    });

                sb.Schema.EntityEvents<UserQueryEntity>().Retrieved += UserQueryLogic_Retrieved;

                new Graph<UserQueryEntity>.Execute(UserQueryOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (uq, _) => { }
                }.Register();

                new Graph<UserQueryEntity>.Delete(UserQueryOperation.Delete)
                {
                    Lite = true,
                    Delete = (uq, _) => uq.Delete()
                }.Register();

                UserQueries = sb.GlobalLazy(() => Database.Query<UserQueryEntity>().ToDictionary(a => a.ToLite()),
                    new InvalidateWith(typeof(UserQueryEntity)));

                UserQueriesByQuery = sb.GlobalLazy(() => UserQueries.Value.Values.Where(a => a.EntityType == null).GroupToDictionary(a => a.Query.ToQueryName(), a => a.ToLite()),
                    new InvalidateWith(typeof(UserQueryEntity)));

                UserQueriesByType = sb.GlobalLazy(() => UserQueries.Value.Values.Where(a => a.EntityType != null).GroupToDictionary(a => TypeLogic.IdToType.GetOrThrow(a.EntityType.Id), a => a.ToLite()),
                    new InvalidateWith(typeof(UserQueryEntity)));
            }
        }

        public static UserQueryEntity ParseAndSave(this UserQueryEntity userQuery)
        {
            if (!userQuery.IsNew || userQuery.queryName == null)
                throw new InvalidOperationException("userQuery should be new and have queryName");

            userQuery.Query = QueryLogic.GetQueryEntity(userQuery.queryName);

            QueryDescription description = DynamicQueryManager.Current.QueryDescription(userQuery.queryName);

            userQuery.ParseData(description);

            return userQuery.Execute(UserQueryOperation.Save);
        }


        static void UserQueryLogic_Retrieved(UserQueryEntity userQuery)
        {
            object queryName = QueryLogic.ToQueryName(userQuery.Query.Key);
            QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

            userQuery.ParseData(description);
        }

        public static List<Lite<UserQueryEntity>> GetUserQueries(object queryName)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<UserQueryEntity>(userInterface: true);

            return UserQueriesByQuery.Value.TryGetC(queryName).EmptyIfNull()
                .Where(e => isAllowed(UserQueries.Value.GetOrThrow(e))).ToList();
        }

        public static List<Lite<UserQueryEntity>> GetUserQueriesEntity(Type entityType)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<UserQueryEntity>(userInterface: true);

            return UserQueriesByType.Value.TryGetC(entityType).EmptyIfNull()
                .Where(e => isAllowed(UserQueries.Value.GetOrThrow(e))).ToList();
        }

        public static List<Lite<UserQueryEntity>> Autocomplete(string subString, int limit)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<UserQueryEntity>(userInterface: true);

            return UserQueries.Value.Where(a => a.Value.EntityType == null && isAllowed(a.Value))
                .Select(a => a.Key).Autocomplete(subString, limit).ToList();
        }

        public static UserQueryEntity RetrieveUserQuery(this Lite<UserQueryEntity> userQuery)
        {
            using (ViewLogLogic.LogView(userQuery, "UserQuery"))
            {
                var result = UserQueries.Value.GetOrThrow(userQuery);

                var isAllowed = Schema.Current.GetInMemoryFilter<UserQueryEntity>(userInterface: true);
                if (!isAllowed(result))
                    throw new EntityNotFoundException(userQuery.EntityType, userQuery.Id);

                return result;
            }
        }

        public static void RegisterUserTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((UserQueryEntity uq) => uq.Owner, typeof(UserEntity));

            TypeConditionLogic.RegisterCompile<UserQueryEntity>(typeCondition,
                uq => uq.Owner.RefersTo(UserEntity.Current));
        }

        public static void RegisterRoleTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((UserQueryEntity uq) => uq.Owner, typeof(RoleEntity));

            TypeConditionLogic.RegisterCompile<UserQueryEntity>(typeCondition,
                uq => AuthLogic.CurrentRoles().Contains(uq.Owner));
        }


        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            if (!replacements.Interactive)
                return null;

            var list = Database.Query<UserQueryEntity>().ToList();

            var table = Schema.Current.Table(typeof(UserQueryEntity));

            SqlPreCommand cmd = list.Select(uq => ProcessUserQuery(replacements, table, uq)).Combine(Spacing.Double);

            return cmd;
        }

        static SqlPreCommand ProcessUserQuery(Replacements replacements, Table table, UserQueryEntity uq)
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
                            QueryTokenEntity token = item.Token;
                            switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement, "{0} {1}".FormatWith(item.Operation, item.ValueString)))
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
                            QueryTokenEntity token = item.Token;
                            switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement, item.DisplayName.HasText() ? "'{0}'".FormatWith(item.DisplayName) : null))
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
                            QueryTokenEntity token = item.Token;
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

                using (replacements.WithReplacedDatabaseName())
                    return table.UpdateSqlSync(uq, includeCollections: true);
            }
            catch (Exception e)
            {
                return new SqlPreCommandSimple("-- Exception in {0}: {1}".FormatWith(uq.BaseToString(), e.Message));
            }
        }
    }
}
