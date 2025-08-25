using Signum.Authorization;
using Signum.Authorization.Rules;
using Signum.Dashboard;
using Signum.Engine.Sync;
using Signum.Toolbar;
using Signum.UserAssets;
using Signum.UserAssets.Queries;
using Signum.UserAssets.QueryTokens;
using Signum.UserQueries;
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
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            UserAssetsImporter.Register<UserChartEntity>("UserChart", UserChartOperation.Save);

            sb.Schema.Synchronizing += Schema_Synchronizing;

            sb.Include<UserChartEntity>()
                .WithExpressionTo((UserChartEntity d) => d.CachedQueries())
                .WithLiteModel(uq => new UserChartLiteModel { DisplayName = uq.DisplayName, Query = uq.Query, HideQuickLink = uq.HideQuickLink })
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
                    uq.Owner,
                });

            sb.Schema.WhenIncluded<ToolbarEntity>(() =>
            {
                sb.Schema.Settings.AssertImplementedBy((ToolbarEntity t) => t.Elements.First().Content, typeof(UserChartEntity));

                ToolbarLogic.RegisterDelete<UserChartEntity>(sb, uq => uq.Query);
                ToolbarLogic.RegisterContentConfig<UserChartEntity>(
                    lite => { var uc = UserCharts.Value.GetOrCreate(lite); return ToolbarLogic.InMemoryFilter(uc) && QueryLogic.Queries.QueryAllowed(uc.Query.ToQueryName(), true); },
                    lite => PropertyRouteTranslationLogic.TranslatedField(UserCharts.Value.GetOrCreate(lite), a => a.DisplayName));
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

    public static void RegisterUserTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
    {
        sb.Schema.Settings.AssertImplementedBy((UserChartEntity uq) => uq.Owner, typeof(UserEntity));

        RegisterTypeCondition(typeCondition, uq => uq.Owner.Is(UserEntity.Current));
    }


    public static void RegisterRoleTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
    {
        sb.Schema.Settings.AssertImplementedBy((UserChartEntity uq) => uq.Owner, typeof(RoleEntity));

        RegisterTypeCondition(typeCondition, uq => AuthLogic.CurrentRoles().Contains(uq.Owner) || uq.Owner == null);
    }

    public static void RegisterTypeCondition(TypeConditionSymbol typeCondition, Expression<Func<UserChartEntity, bool>> conditionExpression)
    {
        TypeConditionLogic.RegisterCompile<UserChartEntity>(typeCondition, conditionExpression);

        DashboardLogic.RegisterTypeConditionForPart<UserChartPartEntity>(typeCondition);
        DashboardLogic.RegisterTypeConditionForPart<CombinedUserChartPartEntity>(typeCondition);
    }

    public static void RegisterTranslatableRoutes()
    {
        PropertyRouteTranslationLogic.RegisterRoute((UserChartEntity uc) => uc.DisplayName);
        PropertyRouteTranslationLogic.RegisterRoute((UserChartEntity uq) => uq.Columns[0].DisplayName);
        PropertyRouteTranslationLogic.RegisterRoute((UserChartEntity uq) => uq.Filters[0].Pinned!.Label);
    }

    static SqlPreCommand? Schema_Synchronizing(Replacements replacements)
    {
        if (!replacements.Interactive)
            return null;

        var list = Database.Query<UserChartEntity>().ToList();

        var table = Schema.Current.Table(typeof(UserChartEntity));

        using (replacements.WithReplacedDatabaseName())
        {
            SqlPreCommand? cmd = list.Select(uq => ProcessUserChart(replacements, table, uq)).Combine(Spacing.Double);

            return cmd;
        }
    }

    static SqlPreCommand? ProcessUserChart(Replacements replacements, Table table, UserChartEntity uc)
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
                                if (item.Token != null)
                                {
                                    QueryTokenEmbedded token = item.Token;
                                    switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate, " {0} {1}".FormatWith(item.Operation, item.ValueString), allowRemoveToken: true, allowReCreate: false))
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
                                if (item.Token == null)
                                    continue;

                                QueryTokenEmbedded token = item.Token;
                                switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement | SubTokensOptions.CanAggregate, " " + item.ScriptColumn.DisplayName, allowRemoveToken: item.ScriptColumn.IsOptional, allowReCreate: false))
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

                var entityType = uc.EntityType == null ? null : TypeLogic.LiteToType.GetOrThrow(uc.EntityType);

                foreach (var item in uc.Filters.Where(f => !f.IsGroup).ToList())
                {
                retry:
                    string? val = item.ValueString;
                    switch (QueryTokenSynchronizer.FixValue(replacements, item.Token!.Token.Type, ref val, allowRemoveToken: true, isList: item.Operation!.Value.IsList(), entityType))
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
                    string? val = item.Value;
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
                    return table.UpdateSqlSync(uc, u => u.Guid == uc.Guid && u.Ticks == uc.Ticks, includeCollections: true)?.TransactionBlock($"UserChart Guid = {uc.Guid} Ticks = {uc.Ticks} ({uc})"); ;
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

                        string? answer = Console.ReadLine();

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
                return new SqlPreCommandSimple("-- Exception on {0}\n{1}".FormatWith(uc.BaseToString(), e.Message.Indent(2, '-')));
            }
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
