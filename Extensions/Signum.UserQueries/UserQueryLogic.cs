using Signum.Authorization;
using Signum.Authorization.Rules;
using Signum.Dashboard;
using Signum.DynamicQuery.Tokens;
using Signum.Engine.Sync;
using Signum.Omnibox;
using Signum.Toolbar;
using Signum.UserAssets;
using Signum.UserAssets.Queries;
using Signum.UserAssets.QueryTokens;
using Signum.UserAssets.TokenMigrations;
using Signum.ViewLog;
using System.Collections.Frozen;
using System.Linq.Expressions;

namespace Signum.UserQueries;

public static class UserQueryLogic
{
    public static ResetLazy<FrozenDictionary<Lite<UserQueryEntity>, UserQueryEntity>> UserQueries = null!;
    public static ResetLazy<FrozenDictionary<Type, List<Lite<UserQueryEntity>>>> UserQueriesByType = null!;
    public static ResetLazy<FrozenDictionary<object, List<Lite<UserQueryEntity>>>> UserQueriesByQuery = null!;

    [AutoExpressionField]
    public static IQueryable<CachedQueryEntity> CachedQueries(this UserQueryEntity uq) =>
    As.Expression(() => Database.Query<CachedQueryEntity>().Where(a => a.UserAssets.Contains(uq.ToLite())));

    public static void Start(SchemaBuilder sb)
    {
        

        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        QueryLogic.Start(sb);

        PermissionLogic.RegisterPermissions(UserQueryPermission.ViewUserQuery);

        CurrentUserConverter.GetCurrentUserEntity = () => UserEntity.Current.Retrieve();

        UserAssetsImporter.Register("UserQuery", UserQueryOperation.Save);

        TokenMigrationLogic.TokenSynchronizing += TokenMigration_Sync;
        sb.Schema.EntityEvents<QueryEntity>().PreDeleteSqlSync += e =>
            Administrator.UnsafeDeletePreCommand(Database.Query<UserQueryEntity>().Where(a => a.Query.Is(e)));

        sb.Include<UserQueryEntity>()
            .WithLiteModel(uq => new UserQueryLiteModel { DisplayName = uq.DisplayName, Query = uq.Query, HideQuickLink = uq.HideQuickLink})
            .WithExpressionTo((UserQueryEntity d) => d.CachedQueries())
            .WithSave(UserQueryOperation.Save)
            .WithDelete(UserQueryOperation.Delete)
            .WithQuery(() => uq => new
            {
                Entity = uq,
                uq.Id,
                uq.DisplayName,
                uq.Query,
                uq.EntityType,
                uq.Owner,
            });

        sb.Schema.WhenIncluded<ToolbarEntity>(() =>
        {
            sb.Schema.Settings.AssertImplementedBy((ToolbarEntity t) => t.Elements.First().Content, typeof(UserQueryEntity));

            ToolbarLogic.RegisterDelete<UserQueryEntity>(sb, uq => uq.Query);

            new ToolbarContentConfig<UserQueryEntity>
            {
                DefaultLabel = lite => PropertyRouteTranslationLogic.TranslatedField(UserQueries.Value.GetOrCreate(lite), a => a.DisplayName),
                IsAuthorized = lite =>
                {
                    var uq = UserQueries.Value.GetOrCreate(lite); 
                    return ToolbarLogic.InMemoryFilter(uq) && QueryLogic.Queries.QueryAllowed(uq.Query.ToQueryName(), true);
                },
                GetRelatedQuery = lite => lite.RetrieveUserQuery().Query,
            }.Register();
        });

        sb.Schema.WhenIncluded<CachedQueryEntity>(() =>
        {
            sb.Schema.Settings.AssertImplementedBy((CachedQueryEntity c) => c.UserAssets.First(), typeof(UserQueryEntity));
        });

        sb.Schema.WhenIncluded<DashboardEntity>(() =>
        {
            sb.Schema.Settings.AssertImplementedBy((DashboardEntity d) => d.Parts.First().Content, typeof(UserQueryPartEntity));
            sb.Schema.Settings.AssertImplementedBy((DashboardEntity d) => d.Parts.First().Content, typeof(ValueUserQueryListPartEntity));

            DashboardLogic.PartNames.AddRange(new Dictionary<string, Type>
            {
                {"ValueUserQueryListPart", typeof(ValueUserQueryListPartEntity)},
                {"UserQueryPart", typeof(UserQueryPartEntity)},
                {"BigValuePart", typeof(BigValuePartEntity)},
            });

            DashboardLogic.OnGetCachedQueryDefinition.Register((ValueUserQueryListPartEntity vuql, PanelPartEmbedded pp) => vuql.UserQueries.Select(uqe => new CachedQueryDefinition(uqe.UserQuery.ToQueryRequestValue(), uqe.UserQuery.Filters.GetDashboardPinnedFilterTokens(), pp, uqe.UserQuery, uqe.IsQueryCached, canWriteFilters: false)));
            DashboardLogic.OnGetCachedQueryDefinition.Register((UserQueryPartEntity uqp, PanelPartEmbedded pp) => new[] { new CachedQueryDefinition(uqp.UserQuery.ToQueryRequest(), uqp.UserQuery.Filters.GetDashboardPinnedFilterTokens(), pp, uqp.UserQuery, uqp.IsQueryCached, canWriteFilters: false) });

            sb.Schema.EntityEvents<UserQueryEntity>().PreUnsafeDelete += query =>
            {
                Database.MListQuery((DashboardEntity cp) => cp.Parts).Where(mle => query.Contains(((BigValuePartEntity)mle.Element.Content).UserQuery)).UnsafeDeleteMList();
                Database.Query<BigValuePartEntity>().Where(uqp => query.Contains(uqp.UserQuery)).UnsafeDelete();
                return null;
            };

            sb.Schema.EntityEvents<UserQueryEntity>().PreUnsafeDelete += query =>
            {
                Database.MListQuery((DashboardEntity cp) => cp.Parts).Where(mle => query.Contains(((UserQueryPartEntity)mle.Element.Content).UserQuery)).UnsafeDeleteMList();
                Database.Query<UserQueryPartEntity>().Where(uqp => query.Contains(uqp.UserQuery)).UnsafeDelete();
                return null;
            };

            sb.Schema.EntityEvents<QueryEntity>().PreDeleteSqlSync += q =>
            {
                var parts = Administrator.UnsafeDeletePreCommandMList((DashboardEntity cp) => cp.Parts, Database.MListQuery((DashboardEntity cp) => cp.Parts).Where(mle => ((UserQueryPartEntity)mle.Element.Content).UserQuery.Query.Is(q)));
                var parts2 = Administrator.UnsafeDeletePreCommand(Database.Query<UserQueryPartEntity>().Where(uqp => uqp.UserQuery.Query.Is(q)));
                return SqlPreCommand.Combine(Spacing.Simple, parts, parts2);
            };

            sb.Schema.EntityEvents<UserQueryEntity>().PreDeleteSqlSync += arg =>
            {
                var uq = (UserQueryEntity)arg;

                var uqPartsMList = Administrator.UnsafeDeletePreCommandMList((DashboardEntity cp) => cp.Parts, Database.MListQuery((DashboardEntity cp) => cp.Parts)
                    .Where(mle => ((UserQueryPartEntity)mle.Element.Content).UserQuery.Is(uq)));

                var uqParts = Administrator.UnsafeDeletePreCommand(Database.Query<UserQueryPartEntity>()
                  .Where(mle => mle.UserQuery.Is(uq)));

                var bigValuePartsMList = Administrator.UnsafeDeletePreCommandMList((DashboardEntity cp) => cp.Parts, Database.MListQuery((DashboardEntity cp) => cp.Parts)
              .Where(mle => ((BigValuePartEntity)mle.Element.Content).UserQuery.Is(uq)));

                var bigValueParts = Administrator.UnsafeDeletePreCommand(Database.Query<BigValuePartEntity>()
                  .Where(mle => mle.UserQuery.Is(uq)));

                return SqlPreCommand.Combine(Spacing.Simple, uqPartsMList, uqParts, bigValueParts, bigValuePartsMList);
            };
        });

        AuthLogic.HasRuleOverridesEvent += role => Database.Query<UserQueryEntity>().Any(a => a.Owner.Is(role));

        sb.Schema.EntityEvents<UserQueryEntity>().Retrieved += UserQueryLogic_Retrieved;

        UserQueries = sb.GlobalLazy(() => Database.Query<UserQueryEntity>().ToFrozenDictionaryEx(a => a.ToLite()),
            new InvalidateWith(typeof(UserQueryEntity)));

        UserQueriesByQuery = sb.GlobalLazy(() => UserQueries.Value.Values.Where(a => a.EntityType == null)
            .SelectCatch(uq => KeyValuePair.Create(uq.Query.ToQueryName(), uq.ToLite())).GroupToDictionary().ToFrozenDictionaryEx(),
            new InvalidateWith(typeof(UserQueryEntity)));

        UserQueriesByType = sb.GlobalLazy(() => UserQueries.Value.Values.Where(a => a.EntityType != null)
            .SelectCatch(uq => KeyValuePair.Create(TypeLogic.IdToType.GetOrThrow(uq.EntityType!.Id), uq.ToLite()))
            .GroupToDictionary().ToFrozenDictionaryEx(), 
            new InvalidateWith(typeof(UserQueryEntity)));

        		if (sb.WebServerBuilder != null)
        {
            UserQueryServer.Start(sb.WebServerBuilder);
            OmniboxParser.Generators.Add(new UserQueryOmniboxResultGenerator(UserQueryLogic.Autocomplete));
        }
    }

    public static QueryRequest ToQueryRequest(this UserQueryEntity userQuery, bool ignoreHidden = false)
    {
        var qr = new QueryRequest
        {
            QueryName = userQuery.Query.ToQueryName(),
            GroupResults = userQuery.GroupResults,
            Filters = userQuery.Filters.ToFilterList(),
            Columns = MergeColumns(userQuery, ignoreHidden),
            Orders = userQuery.Orders.Select(qo => new Order(qo.Token.Token, qo.OrderType)).ToList(),
            Pagination = userQuery.GetPagination() ?? new Pagination.All(),
            SystemTime = userQuery.SystemTime?.ToSystemTimeRequest()
        };

        return qr;
    }

    public static QueryRequest ToQueryRequestValue(this UserQueryEntity userQuery, QueryToken? valueToken = null)
    {
        var qn = userQuery.Query.ToQueryName();

        if (valueToken == null)
        {
            var qd = QueryLogic.Queries.QueryDescription(qn);
            valueToken = QueryUtils.Parse("Count", qd, SubTokensOptions.CanAggregate);
        }

        var qr = new QueryRequest
        {
            QueryName = qn,
            GroupResults = userQuery.GroupResults || valueToken is AggregateToken,
            Filters = userQuery.Filters.ToFilterList(),
            Columns = new List<Column> { new Column(valueToken, null) },
            Orders = valueToken is AggregateToken ? new List<Order>() : userQuery.Orders.Select(qo => new Order(qo.Token.Token, qo.OrderType)).ToList(),

            Pagination = userQuery.GetPagination() ?? new Pagination.All(),
            SystemTime = userQuery.SystemTime?.ToSystemTimeRequest()
        };

        return qr;
    }

    static List<Column> MergeColumns(UserQueryEntity uq, bool ignoreHidden)
    {
        QueryDescription qd = QueryLogic.Queries.QueryDescription(uq.Query.ToQueryName());

        switch (uq.ColumnsMode)
        {
            case ColumnOptionsMode.Add: return qd.Columns.Where(cd => !cd.IsEntity).Select(cd => new Column(cd, qd.QueryName)).Concat(uq.Columns.Where(a => !a.HiddenColumn || !ignoreHidden).Select(co => ToColumn(co))).ToList();
            case ColumnOptionsMode.Remove: return qd.Columns.Where(cd => !cd.IsEntity && !uq.Columns.Any(co => co.Token.TokenString == cd.Name)).Select(cd => new Column(cd, qd.QueryName)).ToList();
            case ColumnOptionsMode.ReplaceAll: return uq.Columns.Where(a => !a.HiddenColumn || !ignoreHidden).Select(co => ToColumn(co)).ToList();
            case ColumnOptionsMode.ReplaceOrAdd:
                {
                    var original = qd.Columns.Where(cd => !cd.IsEntity).Select(cd => new Column(cd, qd.QueryName)).ToList();
                    var toReplaceOrAdd = uq.Columns.Where(a => !a.HiddenColumn || !ignoreHidden).Select(co => ToColumn(co)).ToList();
                    foreach (var item in toReplaceOrAdd)
                    {
                        var index = original.FindIndex(o => o.Token.Equals(item.Token));
                        if (index != -1)
                            original[index] = item;
                        else
                            original.Add(item);
                    }
                    return original;
                }
            default: throw new InvalidOperationException("{0} is not a valid ColumnOptionMode".FormatWith(uq.ColumnsMode));
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


    static void UserQueryLogic_Retrieved(UserQueryEntity userQuery, PostRetrievingContext ctx)
    {
        object? queryName = userQuery.Query.ToQueryNameCatch();
        if(queryName == null)
            return;

        QueryDescription description = QueryLogic.Queries.QueryDescription(queryName);

        userQuery.ParseData(description);
    }

    public static List<Lite<UserQueryEntity>> GetUserQueries(object queryName, bool appendFilters)
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<UserQueryEntity>(userInterface: false);

        return UserQueriesByQuery.Value.TryGetC(queryName).EmptyIfNull()
            .Select(lite => UserQueries.Value.GetOrThrow(lite))
            .Where(uq => isAllowed(uq) && (uq.AppendFilters == appendFilters))
            .Select(uq => uq.ToLite(UserQueryLiteModel.Translated(uq)))
            .ToList();
    }

    private static IEnumerable<UserQueryEntity> GetUserQueriesEntity(Type entityType)
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<UserQueryEntity>(userInterface: false);

        return UserQueriesByType.Value.TryGetC(entityType).EmptyIfNull()
             .Select(lite => UserQueries.Value.GetOrThrow(lite))
             .Where(uq => isAllowed(uq));
    }

    public static List<Lite<UserQueryEntity>> GetUserQueries(Type entityType)
    {
        return GetUserQueriesEntity(entityType)
             .Select(uq => uq.ToLite(new UserQueryLiteModel
             {
                 DisplayName = PropertyRouteTranslationLogic.TranslatedField(uq, d => d.DisplayName),
                 HideQuickLink = uq.HideQuickLink,
                 Query = uq.Query,
             }))
             .ToList();
    }

    public static List<Lite<UserQueryEntity>> GetUserQueriesModel(Type entityType)
    {
        return GetUserQueriesEntity(entityType)
             .Select(uq => uq.ToLite(UserQueryLiteModel.Translated(uq)))
             .ToList();
    }

    public static List<Lite<UserQueryEntity>> Autocomplete(string subString, int limit)
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<UserQueryEntity>(userInterface: false);

        return UserQueries.Value.Values
            .Where(uq => uq.EntityType == null && isAllowed(uq))
             .Select(d => d.ToLite(UserQueryLiteModel.Translated(d)))
             .Autocomplete(subString, limit)
             .ToList();
    }

    public static UserQueryEntity RetrieveUserQuery(this Lite<UserQueryEntity> userQuery)
    {
        using (ViewLogLogic.LogView(userQuery, "UserQuery"))
        {
            var result = UserQueries.Value.GetOrThrow(userQuery);

            var isAllowed = Schema.Current.GetInMemoryFilter<UserQueryEntity>(userInterface: false);
            if (!isAllowed(result))
                throw new EntityNotFoundException(userQuery.EntityType, userQuery.Id);

            return result;
        }
    }

    public static void RegisterUserTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        => RegisterTypeCondition(sb, typeCondition, typeof(UserEntity), uq => uq.Owner.Is(UserEntity.Current));

    public static void RegisterRoleTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        => RegisterTypeCondition(sb, typeCondition, typeof(RoleEntity), uq => uq.Owner == null || AuthLogic.CurrentRoles().Contains(uq.Owner));

    public static void RegisterTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition, Type ownerType, Expression<Func<UserQueryEntity, bool>> condition, Func<UserQueryEntity, bool>? inMemoryCondition = null)
    {
        sb.Schema.Settings.AssertImplementedBy((UserQueryEntity uq) => uq.Owner, ownerType);

        if (inMemoryCondition == null)
            TypeConditionLogic.RegisterCompile<UserQueryEntity>(typeCondition, condition);
        else
            TypeConditionLogic.Register<UserQueryEntity>(typeCondition, condition, inMemoryCondition);


        DashboardLogic.RegisterTypeConditionForPart<ValueUserQueryListPartEntity>(typeCondition);
        DashboardLogic.RegisterTypeConditionForPart<UserQueryPartEntity>(typeCondition);
        DashboardLogic.RegisterTypeConditionForPart<BigValuePartEntity>(typeCondition);
    }

    internal static void TokenMigration_Sync(TokenSyncContext ctx)
    {
        QueryLogic.AssertLoaded();
        TypeLogic.AssertLoaded();

        var list = Database.Query<UserQueryEntity>().ToList();
        foreach (var uq in list)
            ProcessUserQuery(ctx, uq);
    }

    static void SkipUserQuery(TokenSyncContext ctx, UserQueryEntity uq)
    {
        if (ctx.Mode == TokenSyncMode.Record)
            ctx.AddUserAssetAction(uq, UserAssetEntityActionType.Skip);
        ctx.LogEntityChange(uq, UserAssetEntityActionType.Skip);
    }

    static void DeleteUserQuery(TokenSyncContext ctx, UserQueryEntity uq)
    {
        if (ctx.Mode == TokenSyncMode.Record)
        {
            ctx.AddUserAssetAction(uq, UserAssetEntityActionType.Delete);
        }
        else
        {
            using (var tr = Transaction.ForceNew())
            {
                uq.Delete();
                tr.Commit();
            }
        }
        ctx.LogEntityChange(uq, UserAssetEntityActionType.Delete);
    }

    static void SaveUserQuery(UserQueryEntity uq)
    {
        using (var tr = Transaction.ForceNew())
        {
            uq.Save();
            tr.Commit();
        }
    }

    static void ProcessUserQuery(TokenSyncContext ctx, UserQueryEntity uq)
    {
        if (ctx.Mode == TokenSyncMode.Apply && ctx.IsKnownAction(uq, out var preAction))
        {
            try
            {
                switch (preAction)
                {
                    case UserAssetEntityActionType.Skip: SkipUserQuery(ctx, uq); return;
                    case UserAssetEntityActionType.Delete: DeleteUserQuery(ctx, uq); return;
                    case UserAssetEntityActionType.Regenerate:
                        // Regenerate isn't meaningful for UserQuery (no default-template seed). Treat as Skip.
                        SkipUserQuery(ctx, uq);
                        return;
                }
            }
            catch (Exception ex) { ctx.LogEntityError(uq, ex); return; }
        }

        Console.Write(".");
        var changes = new List<string>();
        bool entityTouched = false;

        try
        {
            using (DelayedConsole.Delay(() => SafeConsole.WriteLineColor(ConsoleColor.White, "UserQuery: " + uq.DisplayName)))
            using (DelayedConsole.Delay(() => Console.WriteLine(" Query: " + uq.Query.Key)))
            {

                QueryDescription qd = QueryLogic.Queries.QueryDescription(uq.Query.ToQueryName());

                if (uq.Filters.Any(a => a.Token?.ParseException != null) ||
                   uq.Columns.Any(a => a.Token?.ParseException != null) ||
                   uq.Orders.Any(a => a.Token.ParseException != null))
                {


                    var options = uq.GroupResults ? (SubTokensOptions.CanElement | SubTokensOptions.CanAggregate) : SubTokensOptions.CanElement;

                    if (uq.Filters.Any())
                    {
                        using (DelayedConsole.Delay(() => Console.WriteLine(" Filters:")))
                        {
                            var filterOptions = options | SubTokensOptions.CanAnyAll;
                            foreach (var filter in uq.Filters.ToList())
                            {
                                if (filter.Token == null)
                                    continue;

                                QueryTokenEmbedded token = filter.Token;
                                switch (QueryTokenSynchronizer.FixToken(ctx, ref token, qd, filterOptions, " {0} {1}".FormatWith(filter.Operation, filter.ValueString), allowRemoveToken: true, allowReCreate: false))
                                {
                                    case FixTokenResult.Nothing: break;
                                    case FixTokenResult.RemoveToken: uq.Filters.Remove(filter); entityTouched = true; changes.Add("filter removed"); break;
                                    case FixTokenResult.Fix: filter.Token = token; entityTouched = true; changes.Add("filter -> " + token.TokenString); break;
                                    case FixTokenResult.SkipEntity: SkipUserQuery(ctx, uq); return;
                                    case FixTokenResult.DeleteEntity: DeleteUserQuery(ctx, uq); return;
                                }
                            }
                        }
                    }

                    if (uq.Columns.Any())
                    {
                        using (DelayedConsole.Delay(() => Console.WriteLine(" Columns:")))
                        {
                            var columnOptions = options | SubTokensOptions.CanManual | SubTokensOptions.CanToArray | SubTokensOptions.CanSnippet | SubTokensOptions.CanOperation;

                            foreach (var col in uq.Columns.ToList())
                            {
                                QueryTokenEmbedded token = col.Token;
                                switch (QueryTokenSynchronizer.FixToken(ctx, ref token, qd, columnOptions, col.DisplayName.HasText() ? " '{0}' (Summary)".FormatWith(col.DisplayName) : null, allowRemoveToken: true, allowReCreate: false))
                                {
                                    case FixTokenResult.Nothing: break;
                                    case FixTokenResult.RemoveToken: uq.Columns.Remove(col); entityTouched = true; changes.Add("column removed"); break;
                                    case FixTokenResult.Fix: col.Token = token; entityTouched = true; changes.Add("column -> " + token.TokenString); break;
                                    case FixTokenResult.SkipEntity: SkipUserQuery(ctx, uq); return;
                                    case FixTokenResult.DeleteEntity: DeleteUserQuery(ctx, uq); return;
                                }

                                if(col.SummaryToken != null)
                                {
                                    QueryTokenEmbedded sumToken = col.SummaryToken;
                                    switch (QueryTokenSynchronizer.FixToken(ctx, ref sumToken, qd, options | SubTokensOptions.CanAggregate, col.DisplayName.HasText() ? " '{0}'".FormatWith(col.DisplayName) : null, allowRemoveToken: true, allowReCreate: false))
                                    {
                                        case FixTokenResult.Nothing: break;
                                        case FixTokenResult.RemoveToken: col.SummaryToken = null; entityTouched = true; changes.Add("summary token removed"); break;
                                        case FixTokenResult.Fix: col.SummaryToken = sumToken; entityTouched = true; changes.Add("summary -> " + sumToken.TokenString); break;
                                        case FixTokenResult.SkipEntity: SkipUserQuery(ctx, uq); return;
                                    case FixTokenResult.DeleteEntity: DeleteUserQuery(ctx, uq); return;
                                    }
                                }
                            }
                        }
                    }

                    if (uq.Orders.Any())
                    {
                        using (DelayedConsole.Delay(() => Console.WriteLine(" Orders:")))
                        {
                            foreach (var ord in uq.Orders.ToList())
                            {
                                QueryTokenEmbedded token = ord.Token;
                                switch (QueryTokenSynchronizer.FixToken(ctx, ref token, qd, options, " " + ord.OrderType.ToString(), allowRemoveToken: true, allowReCreate: false))
                                {
                                    case FixTokenResult.Nothing: break;
                                    case FixTokenResult.RemoveToken: uq.Orders.Remove(ord); entityTouched = true; changes.Add("order removed"); break;
                                    case FixTokenResult.Fix: ord.Token = token; entityTouched = true; changes.Add("order -> " + token.TokenString); break;
                                    case FixTokenResult.SkipEntity: SkipUserQuery(ctx, uq); return;
                                    case FixTokenResult.DeleteEntity: DeleteUserQuery(ctx, uq); return;
                                }
                            }
                        }
                    }
                }

                var entityType = uq.EntityType == null ? null : TypeLogic.LiteToType.GetOrThrow(uq.EntityType);

                foreach (var item in uq.Filters.Where(f => !f.IsGroup).ToList())
                {
                retry:
                    string? val = item.ValueString;
                    switch (QueryTokenSynchronizer.FixValue(ctx, uq.Query.Key, item.Token!.TokenString, item.Token!.Token.Type, ref val, allowRemoveToken: true, isList: item.Operation!.Value.IsList(), fixInstead: true, entityType))
                    {
                        case FixTokenResult.Nothing: break;
                        case FixTokenResult.RemoveToken: uq.Filters.Remove(item); entityTouched = true; changes.Add("filter value removed"); break;
                        case FixTokenResult.Fix: item.ValueString = val; entityTouched = true; changes.Add("filter value -> " + val); goto retry;
                        case FixTokenResult.FixTokenInstead:
                            QueryTokenEmbedded itoken = item.Token;
                            switch (QueryTokenSynchronizer.FixToken(ctx, ref itoken, qd, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate,
                                " {0} {1}".FormatWith(item.Operation, item.ValueString), allowRemoveToken: true, allowReCreate: false, forceChange: true))
                            {
                                case FixTokenResult.Nothing: break;
                                case FixTokenResult.RemoveToken: uq.Filters.Remove(item); entityTouched = true; changes.Add("filter removed"); break;
                                case FixTokenResult.Fix: item.Token = itoken; entityTouched = true; changes.Add("filter -> " + itoken.TokenString); goto retry;
                                case FixTokenResult.SkipEntity: SkipUserQuery(ctx, uq); return;
                                case FixTokenResult.DeleteEntity: DeleteUserQuery(ctx, uq); return;
                            }
                            break;

                        case FixTokenResult.FixOperationInstead:
                            var newOperation = SafeConsole.AskMultiLine($"New filter operation for: {item.Token} {item.Operation} {item.ValueString}?", EnumEntity.GetValues(typeof(FilterOperation)).Select(a => a.ToString()).ToArray());
                            if (newOperation != null) { item.Operation = Enum.Parse<FilterOperation>(newOperation); entityTouched = true; changes.Add("filter operation -> " + newOperation); }
                            goto retry;
                        case FixTokenResult.SkipEntity: SkipUserQuery(ctx, uq); return;
                        case FixTokenResult.DeleteEntity: DeleteUserQuery(ctx, uq); return;
                    }
                }

                if (uq.AppendFilters) uq.Filters.Clear();
                if (!uq.ShouldHaveElements && uq.ElementsPerPage.HasValue) uq.ElementsPerPage = null;
                if (uq.ShouldHaveElements && !uq.ElementsPerPage.HasValue) uq.ElementsPerPage = 20;

                if (uq.SystemTime != null)
                {
                    if (uq.SystemTime.Mode is not (SystemTimeMode.Between or SystemTimeMode.ContainedIn or SystemTimeMode.AsOf))
                        uq.SystemTime.StartDate = null;
                    else
                    {
                    retryStart:
                        var date = uq.SystemTime.StartDate;
                        switch (QueryTokenSynchronizer.FixValue(ctx, uq.Query.Key, "SystemTime.StartDate", typeof(DateTime), ref date, allowRemoveToken: false, isList: false, fixInstead: false, null))
                        {
                            case FixTokenResult.Nothing: break;
                            case FixTokenResult.Fix: uq.SystemTime.StartDate = date; entityTouched = true; goto retryStart;
                            case FixTokenResult.SkipEntity: SkipUserQuery(ctx, uq); return;
                        case FixTokenResult.DeleteEntity: DeleteUserQuery(ctx, uq); return;
                        }
                    }

                    if (uq.SystemTime.Mode is not (SystemTimeMode.Between or SystemTimeMode.ContainedIn))
                        uq.SystemTime.EndDate = null;
                    else
                    {
                    retryEnd:
                        var date = uq.SystemTime.EndDate;
                        switch (QueryTokenSynchronizer.FixValue(ctx, uq.Query.Key, "SystemTime.EndDate", typeof(DateTime), ref date, allowRemoveToken: false, isList: false, fixInstead: false, null))
                        {
                            case FixTokenResult.Nothing: break;
                            case FixTokenResult.Fix: uq.SystemTime.EndDate = date; entityTouched = true; goto retryEnd;
                            case FixTokenResult.SkipEntity: SkipUserQuery(ctx, uq); return;
                        case FixTokenResult.DeleteEntity: DeleteUserQuery(ctx, uq); return;
                        }
                    }
                }

                if (!entityTouched) return;

                if (ctx.Mode == TokenSyncMode.Apply)
                {
                    try
                    {
                        SaveUserQuery(uq);
                        ctx.LogEntityChange(uq, changes.ToArray());
                    }
                    catch (Exception ex) { ctx.LogEntityError(uq, ex); }
                }
                else ctx.LogEntityChange(uq, changes.ToArray());
            }
        }
        catch (Exception ex) { ctx.LogEntityError(uq, ex); }
    }


}
