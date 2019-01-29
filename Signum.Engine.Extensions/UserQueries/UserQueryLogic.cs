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
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserAssets;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Signum.Engine.UserQueries
{
    public static class UserQueryLogic
    {
        public static ResetLazy<Dictionary<Lite<UserQueryEntity>, UserQueryEntity>> UserQueries;
        public static ResetLazy<Dictionary<Type, List<Lite<UserQueryEntity>>>> UserQueriesByTypeForQuickLinks;
        public static ResetLazy<Dictionary<object, List<Lite<UserQueryEntity>>>> UserQueriesByQuery;

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryLogic.Start(sb);

                PermissionAuthLogic.RegisterPermissions(UserQueryPermission.ViewUserQuery);

                UserAssetsImporter.RegisterName<UserQueryEntity>("UserQuery");

                sb.Schema.Synchronizing += Schema_Synchronizing;
                sb.Schema.Table<QueryEntity>().PreDeleteSqlSync += e =>
                    Administrator.UnsafeDeletePreCommand(Database.Query<UserQueryEntity>().Where(a => a.Query == e));

                sb.Include<UserQueryEntity>()
                    .WithSave(UserQueryOperation.Save)
                    .WithDelete(UserQueryOperation.Delete)
                    .WithQuery(() => uq => new
                    {
                        Entity = uq,
                        uq.Id,
                        uq.DisplayName,
                        uq.Query,
                        uq.EntityType,
                    });

                sb.Schema.EntityEvents<UserQueryEntity>().Retrieved += UserQueryLogic_Retrieved;

                UserQueries = sb.GlobalLazy(() => Database.Query<UserQueryEntity>().ToDictionary(a => a.ToLite()),
                    new InvalidateWith(typeof(UserQueryEntity)));

                UserQueriesByQuery = sb.GlobalLazy(() => UserQueries.Value.Values.Where(a => a.EntityType == null).SelectCatch(uq => KVP.Create(uq.Query.ToQueryName(), uq.ToLite())).GroupToDictionary(),
                    new InvalidateWith(typeof(UserQueryEntity)));

                UserQueriesByTypeForQuickLinks = sb.GlobalLazy(() => UserQueries.Value.Values.Where(a => a.EntityType != null && !a.HideQuickLink).SelectCatch(uq => KVP.Create(TypeLogic.IdToType.GetOrThrow(uq.EntityType.Id), uq.ToLite())).GroupToDictionary(),
                    new InvalidateWith(typeof(UserQueryEntity)));
            }
        }

        public static QueryRequest ToQueryRequest(this UserQueryEntity userQuery)
        {
            var qr = new QueryRequest()
            {
                QueryName = userQuery.Query.ToQueryName()
            };
            if (!userQuery.AppendFilters)
            {
                qr.Filters = userQuery.Filters.ToFilterList();
            }

            qr.Columns = MergeColumns(userQuery);
            qr.Orders = userQuery.Orders.Select(qo => new Order(qo.Token.Token, qo.OrderType)).ToList();

            qr.Pagination = userQuery.GetPagination() ?? new Pagination.All();

            return qr;
        }

        static List<Column> MergeColumns(UserQueryEntity uq)
        {
            QueryDescription qd = QueryLogic.Queries.QueryDescription(uq.Query.ToQueryName());

            switch (uq.ColumnsMode)
            {
                case ColumnOptionsMode.Add:
                    return qd.Columns.Where(cd => !cd.IsEntity).Select(cd => new Column(cd, qd.QueryName)).Concat(uq.Columns.Select(co => ToColumn(co))).ToList();
                case ColumnOptionsMode.Remove:
                    return qd.Columns.Where(cd => !cd.IsEntity && !uq.Columns.Any(co => co.Token.TokenString == cd.Name)).Select(cd => new Column(cd, qd.QueryName)).ToList();
                case ColumnOptionsMode.Replace:
                    return uq.Columns.Select(co => ToColumn(co)).ToList();
                default:
                    throw new InvalidOperationException("{0} is not a valid ColumnOptionMode".FormatWith(uq.ColumnsMode));
            }
        }

        private static Column ToColumn(QueryColumnEmbedded co)
        {
            return new Column(co.Token.Token, co.DisplayName.DefaultText(co.Token.Token.NiceName()));
        }

        public static UserQueryEntity ParseAndSave(this UserQueryEntity userQuery)
        {
            if (!userQuery.IsNew || userQuery.queryName == null)
                throw new InvalidOperationException("userQuery should be new and have queryName");

            userQuery.Query = QueryLogic.GetQueryEntity(userQuery.queryName);

            QueryDescription description = QueryLogic.Queries.QueryDescription(userQuery.queryName);

            userQuery.ParseData(description);

            return userQuery.Execute(UserQueryOperation.Save);
        }


        static void UserQueryLogic_Retrieved(UserQueryEntity userQuery)
        {
            object queryName;
            try
            {
                queryName = QueryLogic.ToQueryName(userQuery.Query.Key);
            }
            catch (KeyNotFoundException ex) when (StartParameters.IgnoredCodeErrors != null)
            {
                StartParameters.IgnoredCodeErrors.Add(ex);

                return;
            }

            QueryDescription description = QueryLogic.Queries.QueryDescription(queryName);

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

            return UserQueriesByTypeForQuickLinks.Value.TryGetC(entityType).EmptyIfNull()
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
                uq => uq.Owner.Is(UserEntity.Current));
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
            Console.Write(".");
            try
            {
                using (DelayedConsole.Delay(() => SafeConsole.WriteLineColor(ConsoleColor.White, "UserQuery: " + uq.DisplayName)))
                using (DelayedConsole.Delay(() => Console.WriteLine(" Query: " + uq.Query.Key)))
                {

                    if (uq.Filters.Any(a => a.Token?.ParseException != null) ||
                       uq.Columns.Any(a => a.Token?.ParseException != null) ||
                       uq.Orders.Any(a => a.Token.ParseException != null))
                    {

                        QueryDescription qd = QueryLogic.Queries.QueryDescription(uq.Query.ToQueryName());

                        if (uq.Filters.Any())
                        {
                            using (DelayedConsole.Delay(() => Console.WriteLine(" Filters:")))
                            {
                                foreach (var item in uq.Filters.ToList())
                                {
                                    QueryTokenEmbedded token = item.Token;
                                    switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement, "{0} {1}".FormatWith(item.Operation, item.ValueString), allowRemoveToken: true, allowReCreate: false))
                                    {
                                        case FixTokenResult.Nothing: break;
                                        case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(uq, u => u.Guid == uq.Guid);
                                        case FixTokenResult.RemoveToken: uq.Filters.Remove(item); break;
                                        case FixTokenResult.SkipEntity: return null;
                                        case FixTokenResult.Fix: item.Token = token; break;
                                        default: break;
                                    }
                                }
                            }
                        }

                        if (uq.Columns.Any())
                        {
                            using (DelayedConsole.Delay(() => Console.WriteLine(" Columns:")))
                            {
                                foreach (var item in uq.Columns.ToList())
                                {
                                    QueryTokenEmbedded token = item.Token;
                                    switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement, item.DisplayName.HasText() ? "'{0}'".FormatWith(item.DisplayName) : null, allowRemoveToken: true, allowReCreate: false))
                                    {
                                        case FixTokenResult.Nothing: break;
                                        case FixTokenResult.DeleteEntity:; return table.DeleteSqlSync(uq, u => u.Guid == uq.Guid);
                                        case FixTokenResult.RemoveToken: uq.Columns.Remove(item); break;
                                        case FixTokenResult.SkipEntity: return null;
                                        case FixTokenResult.Fix: item.Token = token; break;
                                        default: break;
                                    }
                                }
                            }
                        }

                        if (uq.Orders.Any())
                        {
                            using (DelayedConsole.Delay(() => Console.WriteLine(" Orders:")))
                            {
                                foreach (var item in uq.Orders.ToList())
                                {
                                    QueryTokenEmbedded token = item.Token;
                                    switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement, item.OrderType.ToString(), allowRemoveToken: true, allowReCreate: false))
                                    {
                                        case FixTokenResult.Nothing: break;
                                        case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(uq, u => u.Guid == uq.Guid);
                                        case FixTokenResult.RemoveToken: uq.Orders.Remove(item); break;
                                        case FixTokenResult.SkipEntity: return null;
                                        case FixTokenResult.Fix: item.Token = token; break;
                                        default: break;
                                    }
                                }
                            }
                        }
                    }

                    foreach (var item in uq.Filters.Where(f => !f.IsGroup).ToList())
                    {
                        retry:
                        string val = item.ValueString;
                        switch (QueryTokenSynchronizer.FixValue(replacements, item.Token.Token.Type, ref val, allowRemoveToken: true, isList: item.Operation.Value.IsList()))
                        {
                            case FixTokenResult.Nothing: break;
                            case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(uq, u => u.Guid == uq.Guid);
                            case FixTokenResult.RemoveToken: uq.Filters.Remove(item); break;
                            case FixTokenResult.SkipEntity: return null;
                            case FixTokenResult.Fix: item.ValueString = val; goto retry;
                        }
                    }

                    if (uq.AppendFilters)
                        uq.Filters.Clear();

                    if (!uq.ShouldHaveElements && uq.ElementsPerPage.HasValue)
                        uq.ElementsPerPage = null;

                    if (uq.ShouldHaveElements && !uq.ElementsPerPage.HasValue)
                        uq.ElementsPerPage = 20;

                    using (replacements.WithReplacedDatabaseName())
                        return table.UpdateSqlSync(uq, u => u.Guid == uq.Guid, includeCollections: true);
                }
            }
            catch (Exception e)
            {
                return new SqlPreCommandSimple("-- Exception in {0}: {1}".FormatWith(uq.BaseToString(), e.Message));
            }
        }
    }
}
