using Signum.Authorization;
using Signum.Authorization.Rules;
using Signum.Dashboard;
using Signum.Engine.Sync;
using Signum.Toolbar;
using Signum.UserAssets;
using Signum.UserAssets.Queries;
using Signum.UserAssets.QueryTokens;
using Signum.UserAssets.TokenMigrations;
using Signum.ViewLog;
using System.Collections.Frozen;

namespace Signum.Chart.UserChart;

public static class UserChartLogic
{
    public static ResetLazy<FrozenDictionary<Lite<UserChartEntity>, UserChartEntity>> UserCharts = null!;
    public static ResetLazy<FrozenDictionary<Type, List<Lite<UserChartEntity>>>> UserChartsByType = null!;
    public static ResetLazy<FrozenDictionary<object, List<Lite<UserChartEntity>>>> UserChartsByQuery = null!;

    [AutoExpressionField]
    public static IQueryable<CachedQueryEntity> CachedQueries(this UserChartEntity uc) =>
        As.Expression(() => Database.Query<CachedQueryEntity>().Where(a => a.UserAssets.Contains(uc.ToLite())));
    
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        UserAssetsImporter.Register<UserChartEntity>("UserChart", UserChartOperation.Save);

        TokenMigrationLogic.TokenSynchronizing += TokenMigration_Sync;

        sb.Include<UserChartEntity>()
            .WithExpressionTo((UserChartEntity d) => d.CachedQueries())
            .WithLiteModel(uq => new UserChartLiteModel { DisplayName = uq.DisplayName, Query = uq.Query, HideQuickLink = uq.HideQuickLink })
            .WithSave(UserChartOperation.Save)
            .WithDelete(UserChartOperation.Delete)
            .WithQuery(() => uq => new
            {
                Entity = uq,
                uq.Id,
                uq.DisplayName,
                uq.Query,
                uq.EntityType,
                uq.ChartScript,
                uq.Owner,
            });

        sb.Schema.WhenIncluded<ToolbarEntity>(() =>
        {
            sb.Schema.Settings.AssertImplementedBy((ToolbarEntity t) => t.Elements.First().Content, typeof(UserChartEntity));

            ToolbarLogic.RegisterDelete<UserChartEntity>(sb, uq => uq.Query);

            new ToolbarContentConfig<UserChartEntity>
            {
                DefaultLabel = lite => PropertyRouteTranslationLogic.TranslatedField(UserCharts.Value.GetOrCreate(lite), a => a.DisplayName),
                IsAuthorized = lite =>
                {
                    var uc = UserCharts.Value.GetOrCreate(lite);
                    return ToolbarLogic.InMemoryFilter(uc) && QueryLogic.Queries.QueryAllowed(uc.Query.ToQueryName(), true);
                },
                GetRelatedQuery = lite => lite.RetrieveUserChart().Query,
            }.Register();
        });

        sb.Schema.WhenIncluded<CachedQueryEntity>(() =>
        {
            sb.Schema.Settings.AssertImplementedBy((CachedQueryEntity c) => c.UserAssets.First(), typeof(UserChartEntity));
        });

        sb.Schema.WhenIncluded<DashboardEntity>(() =>
        {
            
            sb.Schema.Settings.AssertImplementedBy((DashboardEntity d) => d.Parts.First().Content, typeof(UserChartPartEntity));

            DashboardLogic.PartNames.AddRange(new Dictionary<string, Type>
            {
                {"UserChartPart", typeof(UserChartPartEntity)},
            });

            DashboardLogic.OnGetCachedQueryDefinition.Register((UserChartPartEntity ucp, PanelPartEmbedded pp) => new[] { new CachedQueryDefinition(ucp.UserChart.ToChartRequest().ToQueryRequest(), ucp.UserChart.Filters.GetDashboardPinnedFilterTokens(), pp, ucp.UserChart, ucp.IsQueryCached, canWriteFilters: true) });

            sb.Schema.EntityEvents<UserChartEntity>().PreUnsafeDelete += query =>
            {
                Database.MListQuery((DashboardEntity cp) => cp.Parts).Where(mle => query.Contains(((UserChartPartEntity)mle.Element.Content).UserChart)).UnsafeDeleteMList();
                Database.Query<UserChartPartEntity>().Where(uqp => query.Contains(uqp.UserChart)).UnsafeDelete();

                return null;
            };

            sb.Schema.EntityEvents<QueryEntity>().PreDeleteSqlSync += q =>
            {
                var parts = Administrator.UnsafeDeletePreCommandMList((DashboardEntity cp) => cp.Parts, Database.MListQuery((DashboardEntity cp) => cp.Parts).Where(mle => ((UserChartPartEntity)mle.Element.Content).UserChart.Query.Is(q)));
                var parts2 = Administrator.UnsafeDeletePreCommand(Database.Query<UserChartPartEntity>().Where(uqp => uqp.UserChart.Query.Is(q)));
                return SqlPreCommand.Combine(Spacing.Simple, parts, parts2);
            };

            sb.Schema.EntityEvents<UserChartEntity>().PreDeleteSqlSync += uc =>
            {
                var mlistElems = Administrator.UnsafeDeletePreCommandMList((DashboardEntity cp) => cp.Parts, Database.MListQuery((DashboardEntity cp) => cp.Parts)
                    .Where(mle => ((UserChartPartEntity)mle.Element.Content).UserChart.Is(uc)));

                var parts = Administrator.UnsafeDeletePreCommand(Database.Query<UserChartPartEntity>()
                    .Where(ucp => ucp.UserChart.Is(uc)));

                return SqlPreCommand.Combine(Spacing.Simple, mlistElems, parts);
            };
            
            sb.Schema.Settings.AssertImplementedBy((DashboardEntity d) => d.Parts.First().Content, typeof(CombinedUserChartPartEntity));

            DashboardLogic.PartNames.AddRange(new Dictionary<string, Type>
            {
                {"CombinedUserChartPart", typeof(CombinedUserChartPartEntity)},
            });

            DashboardLogic.OnGetCachedQueryDefinition.Register((CombinedUserChartPartEntity cucp, PanelPartEmbedded pp) => cucp.UserCharts.Select(uc => new CachedQueryDefinition(uc.UserChart.ToChartRequest().ToQueryRequest(), uc.UserChart.Filters.GetDashboardPinnedFilterTokens(), pp, uc.UserChart, uc.IsQueryCached, canWriteFilters: false)));


            sb.Schema.EntityEvents<UserChartEntity>().PreUnsafeDelete += query =>
            {
                Database.MListQuery((CombinedUserChartPartEntity e) => e.UserCharts).Where(mle => query.Contains(mle.Element.UserChart)).UnsafeDeleteMList();

                return null;
            };

            sb.Schema.EntityEvents<UserChartEntity>().PreDeleteSqlSync += uc =>
            {
                var mlistElems2 = Administrator.UnsafeDeletePreCommandMList((CombinedUserChartPartEntity e) => e.UserCharts,
                        Database.MListQuery((CombinedUserChartPartEntity e) => e.UserCharts).Where(mle => mle.Element.UserChart.Is(uc)));

                return SqlPreCommand.Combine(Spacing.Simple, mlistElems2);
            };

            sb.Schema.EntityEvents<QueryEntity>().PreDeleteSqlSync += query =>
            {
                var parts = Administrator.UnsafeDeletePreCommandMList((CombinedUserChartPartEntity e) => e.UserCharts,
                        Database.MListQuery((CombinedUserChartPartEntity e) => e.UserCharts).Where(mle => mle.Element.UserChart.Query.Is(query)));
                return SqlPreCommand.Combine(Spacing.Simple, parts);
            };
            
        });


        AuthLogic.HasRuleOverridesEvent += role => Database.Query<UserChartEntity>().Any(a => a.Owner.Is(role));

        sb.Schema.EntityEvents<UserChartEntity>().Retrieved += ChartLogic_Retrieved;

        sb.Schema.EntityEvents<QueryEntity>().PreDeleteSqlSync += e =>
          Administrator.UnsafeDeletePreCommand(Database.Query<UserChartEntity>().Where(a => a.Query.Is(e)));


        UserCharts = sb.GlobalLazy(() => Database.Query<UserChartEntity>().ToFrozenDictionaryEx(a => a.ToLite()),
            new InvalidateWith(typeof(UserChartEntity)));

        UserChartsByQuery = sb.GlobalLazy(() => UserCharts.Value.Values.Where(a => a.EntityType == null).SelectCatch(uc => KeyValuePair.Create(uc.Query.ToQueryName(), uc.ToLite())).GroupToDictionary().ToFrozenDictionary(),
            new InvalidateWith(typeof(UserChartEntity)));

        UserChartsByType = sb.GlobalLazy(() => UserCharts.Value.Values.Where(a => a.EntityType != null)
        .SelectCatch(a => KeyValuePair.Create(TypeLogic.IdToType.GetOrThrow(a.EntityType!.Id), a.ToLite()))
        .GroupToDictionary().ToFrozenDictionary(),
            new InvalidateWith(typeof(UserChartEntity)));
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

    static void ChartLogic_Retrieved(UserChartEntity userChart, PostRetrievingContext ctx)
    {
        object? queryName = userChart.Query.ToQueryNameCatch();
        if (queryName == null)
            return;

        QueryDescription description = QueryLogic.Queries.QueryDescription(queryName);

        foreach (var item in userChart.Columns)
        {
            item.parentChart = userChart;
        }

        userChart.ParseData(description);
    }

    public static List<Lite<UserChartEntity>> GetUserCharts(object queryName)
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<UserChartEntity>(userInterface: false);

        return UserChartsByQuery.Value.TryGetC(queryName).EmptyIfNull()
            .Select(lite => UserCharts.Value.GetOrThrow(lite))
            .Where(uc => isAllowed(uc))
            .Select(uc => uc.ToLite(UserChartLiteModel.Translated(uc)))
            .ToList();
    }

    public static IEnumerable<UserChartEntity> GetUserChartsEntity(Type entityType)
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<UserChartEntity>(userInterface: false);

        return UserChartsByType.Value.TryGetC(entityType).EmptyIfNull()
            .Select(lite => UserCharts.Value.GetOrThrow(lite))
            .Where(uc => isAllowed(uc));
    }

    public static List<Lite<UserChartEntity>> GetUserCharts(Type entityType)
    {
        return GetUserChartsEntity(entityType)
              .Select(uc => uc.ToLite(UserChartLiteModel.Translated(uc)))
              .ToList();
    }

    public static List<Lite<UserChartEntity>> GetUserChartsModel(Type entityType)
    {
        return GetUserChartsEntity(entityType)
             .Select(uc => uc.ToLite(UserChartLiteModel.Translated(uc)))
             .ToList();
    }

    public static List<Lite<UserChartEntity>> Autocomplete(string subString, int limit)
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<UserChartEntity>(userInterface: false);

        return UserCharts.Value.Values.Where(uc => uc.EntityType == null && isAllowed(uc))
            .Select(d => d.ToLite(PropertyRouteTranslationLogic.TranslatedField(d, d => d.DisplayName)))
            .Autocomplete(subString, limit)
            .ToList();
    }

    public static UserChartEntity RetrieveUserChart(this Lite<UserChartEntity> userChart)
    {
        using (ViewLogLogic.LogView(userChart, "UserChart"))
        {
            var result = UserCharts.Value.GetOrThrow(userChart);

            var isAllowed = Schema.Current.GetInMemoryFilter<UserChartEntity>(userInterface: false);
            if (!isAllowed(result))
                throw new EntityNotFoundException(userChart.EntityType, userChart.Id);

            return result;
        }
    }

    public static ChartRequestModel ToChartRequest(this UserChartEntity userChart)
    {
        var cr = new ChartRequestModel(userChart.Query.ToQueryName())
        {
            ChartScript = userChart.ChartScript,
            Filters = userChart.Filters.ToFilterList(),
            Parameters = userChart.Parameters.ToMList(),
            MaxRows = userChart.MaxRows,
            ChartTimeSeries = userChart.ChartTimeSeries,
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

    public static void RegisterUserTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition) => 
        RegisterTypeCondition(sb, typeCondition, typeof(UserEntity), uq => uq.Owner.Is(UserEntity.Current));

    public static void RegisterRoleTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition) => 
        RegisterTypeCondition(sb, typeCondition, typeof(RoleEntity), uq => uq.Owner == null || AuthLogic.CurrentRoles().Contains(uq.Owner));

    public static void RegisterTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition, Type ownerType, Expression<Func<UserChartEntity, bool>> condition, Func<UserChartEntity, bool>? inMemoryCondition = null)
    {
        sb.Schema.Settings.AssertImplementedBy((UserChartEntity uq) => uq.Owner, ownerType);

        if (inMemoryCondition == null)
            TypeConditionLogic.RegisterCompile<UserChartEntity>(typeCondition, condition);
        else
            TypeConditionLogic.Register<UserChartEntity>(typeCondition, condition, inMemoryCondition);

        DashboardLogic.RegisterTypeConditionForPart<UserChartPartEntity>(typeCondition);
        DashboardLogic.RegisterTypeConditionForPart<CombinedUserChartPartEntity>(typeCondition);
    }

    internal static void TokenMigration_Sync(TokenSyncContext ctx)
    {
        QueryLogic.AssertLoaded();
        TypeLogic.AssertLoaded();

        foreach (var uc in Database.Query<UserChartEntity>().ToList())
            ProcessUserChart(ctx, uc);
    }

    static void SkipUserChart(TokenSyncContext ctx, UserChartEntity uc)
    {
        if (ctx.Mode == TokenSyncMode.Record)
            ctx.AddUserAssetAction(uc, UserAssetEntityActionType.Skip);
        ctx.LogEntityChange(uc, UserAssetEntityActionType.Skip);
    }

    static void DeleteUserChart(TokenSyncContext ctx, UserChartEntity uc)
    {
        if (ctx.Mode == TokenSyncMode.Record)
        {
            ctx.AddUserAssetAction(uc, UserAssetEntityActionType.Delete);
        }
        else
        {
            using (var tr = Transaction.ForceNew())
            {
                uc.Delete();
                tr.Commit();
            }
        }
        ctx.LogEntityChange(uc, UserAssetEntityActionType.Delete);
    }

    static void SaveUserChart(UserChartEntity uc)
    {
        using (var tr = Transaction.ForceNew())
        {
            uc.Save();
            tr.Commit();
        }
    }

    static void ProcessUserChart(TokenSyncContext ctx, UserChartEntity uc)
    {
        if (ctx.Mode == TokenSyncMode.Apply && ctx.IsKnownAction(uc, out var preAction))
        {
            try
            {
                switch (preAction)
                {
                    case UserAssetEntityActionType.Skip: SkipUserChart(ctx, uc); return;
                    case UserAssetEntityActionType.Delete: DeleteUserChart(ctx, uc); return;
                    case UserAssetEntityActionType.Regenerate:
                        // Regenerate isn't meaningful for UserChart; treat as Skip.
                        SkipUserChart(ctx, uc);
                        return;
                }
            }
            catch (Exception ex) { ctx.LogEntityError(uc, ex); return; }
        }

        Console.Write(".");
        var changes = new List<string>();
        bool entityTouched = false;

        using (DelayedConsole.Delay(() => SafeConsole.WriteLineColor(ConsoleColor.White, "UserChart: " + uc.DisplayName)))
        using (DelayedConsole.Delay(() => Console.WriteLine(" ChartScript: " + uc.ChartScript.ToString())))
        using (DelayedConsole.Delay(() => Console.WriteLine(" Query: " + uc.Query.Key)))
        {
            try
            {
                QueryDescription qd = QueryLogic.Queries.QueryDescription(uc.Query.ToQueryName());

                if (uc.Filters.Any(a => a.Token?.ParseException != null) ||
                   uc.Columns.Any(a => a.Token?.ParseException != null))
                {
                    if (uc.Filters.Any())
                    {
                        using (DelayedConsole.Delay(() => Console.WriteLine(" Filters:")))
                        {
                            foreach (var item in uc.Filters.ToList())
                            {
                                if (item.Token == null) continue;

                                QueryTokenEmbedded token = item.Token;
                                var r = QueryTokenSynchronizer.FixToken(ctx, ref token, qd, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate, " {0} {1}".FormatWith(item.Operation, item.ValueString), allowRemoveToken: true, allowReCreate: false);
                                switch (r)
                                {
                                    case FixTokenResult.Nothing: break;
                                    case FixTokenResult.RemoveToken: uc.Filters.Remove(item); entityTouched = true; changes.Add("filter removed"); break;
                                    case FixTokenResult.Fix: item.Token = token; entityTouched = true; changes.Add("filter -> " + token.TokenString); break;
                                    case FixTokenResult.SkipEntity: SkipUserChart(ctx, uc); return;
                                    case FixTokenResult.DeleteEntity: DeleteUserChart(ctx, uc); return;
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
                                if (item.Token == null) continue;

                                QueryTokenEmbedded token = item.Token;
                                var r = QueryTokenSynchronizer.FixToken(ctx, ref token, qd, SubTokensOptions.CanElement | SubTokensOptions.CanAggregate, " " + item.ScriptColumn.DisplayName, allowRemoveToken: item.ScriptColumn.IsOptional, allowReCreate: false);
                                switch (r)
                                {
                                    case FixTokenResult.Nothing: break;
                                    case FixTokenResult.RemoveToken: item.Token = null; entityTouched = true; changes.Add("column token removed"); break;
                                    case FixTokenResult.Fix: item.Token = token; entityTouched = true; changes.Add("column -> " + token.TokenString); break;
                                    case FixTokenResult.SkipEntity: SkipUserChart(ctx, uc); return;
                                    case FixTokenResult.DeleteEntity: DeleteUserChart(ctx, uc); return;
                                }
                            }
                        }
                    }
                }

                var entityType = uc.EntityType == null ? null : TypeLogic.LiteToType.GetOrThrow(uc.EntityType);

                foreach (var item in uc.Filters.Where(f => !f.IsGroup).ToList())
                {
                retry:
                    string? val = item.ValueString;
                    switch (QueryTokenSynchronizer.FixValue(ctx, uc.Query.Key, item.Token!.TokenString, item.Token!.Token.Type, ref val, allowRemoveToken: true, isList: item.Operation!.Value.IsList(), fixInstead: true, entityType))
                    {
                        case FixTokenResult.Nothing: break;
                        case FixTokenResult.RemoveToken: uc.Filters.Remove(item); entityTouched = true; changes.Add("filter value removed"); break;
                        case FixTokenResult.Fix: item.ValueString = val; entityTouched = true; changes.Add("filter value -> " + val); goto retry;
                        case FixTokenResult.FixTokenInstead:
                            QueryTokenEmbedded itoken = item.Token;
                            switch (QueryTokenSynchronizer.FixToken(ctx, ref itoken, qd, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate,
                                " {0} {1}".FormatWith(item.Operation, item.ValueString), allowRemoveToken: true, allowReCreate: false, forceChange: true))
                            {
                                case FixTokenResult.Nothing: break;
                                case FixTokenResult.RemoveToken: uc.Filters.Remove(item); entityTouched = true; changes.Add("filter removed"); break;
                                case FixTokenResult.Fix: item.Token = itoken; entityTouched = true; changes.Add("filter -> " + itoken.TokenString); goto retry;
                                case FixTokenResult.SkipEntity: SkipUserChart(ctx, uc); return;
                                case FixTokenResult.DeleteEntity: DeleteUserChart(ctx, uc); return;
                            }
                            break;

                        case FixTokenResult.FixOperationInstead:
                            var newOperation = SafeConsole.AskMultiLine($"New filter operation for: {item.Token} {item.Operation} {item.ValueString}?", EnumEntity.GetValues(typeof(FilterOperation)).Select(a => a.ToString()).ToArray());
                            if (newOperation != null) { item.Operation = Enum.Parse<FilterOperation>(newOperation); entityTouched = true; changes.Add("filter operation -> " + newOperation); }
                            goto retry;
                        case FixTokenResult.SkipEntity: SkipUserChart(ctx, uc); return;
                        case FixTokenResult.DeleteEntity: DeleteUserChart(ctx, uc); return;
                    }
                }

                foreach (var item in uc.Columns)
                    uc.FixParameters(item);

                foreach (var item in uc.Parameters)
                {
                    string? val = item.Value;
                retryP:
                    switch (FixParameter(item, ref val))
                    {
                        case FixTokenResult.Nothing: break;
                        case FixTokenResult.RemoveToken: uc.Parameters.Remove(item); entityTouched = true; changes.Add("parameter removed"); break;
                        case FixTokenResult.Fix: item.Value = val; entityTouched = true; changes.Add("parameter -> " + val); goto retryP;
                        case FixTokenResult.SkipEntity: SkipUserChart(ctx, uc); return;
                        case FixTokenResult.DeleteEntity: DeleteUserChart(ctx, uc); return;
                    }
                }

                if (!entityTouched) return;

                if (ctx.Mode == TokenSyncMode.Apply)
                {
                    try
                    {
                        SaveUserChart(uc);
                        ctx.LogEntityChange(uc, changes.ToArray());
                    }
                    catch (Exception ex) { ctx.LogEntityError(uc, ex); }
                }
                else ctx.LogEntityChange(uc, changes.ToArray());
            }
            catch (Exception ex) { ctx.LogEntityError(uc, ex); }
        }
    }


    private static FixTokenResult FixParameter(ChartParameterEmbedded item, ref string? val)
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

        string? answer = Console.ReadLine();

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
