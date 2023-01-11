using Signum.Engine.Authorization;
using Signum.Engine.Chart;
using Signum.Engine.Files;
using Signum.Engine.Json;
using Signum.Engine.Scheduler;
using Signum.Engine.Translation;
using Signum.Engine.UserAssets;
using Signum.Engine.UserQueries;
using Signum.Engine.ViewLog;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Chart;
using Signum.Entities.Dashboard;
using Signum.Entities.UserAssets;
using Signum.Entities.UserQueries;
using Signum.Utilities.Reflection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signum.Engine.Dashboard;

public static class DashboardLogic
{
    public static ResetLazy<Dictionary<Lite<DashboardEntity>, DashboardEntity>> Dashboards = null!;
    public static ResetLazy<Dictionary<Lite<DashboardEntity>, List<CachedQueryEntity>>> CachedQueriesCache = null!;
    public static ResetLazy<Dictionary<Type, List<Lite<DashboardEntity>>>> DashboardsByType = null!;

    public static Polymorphic<Func<IPartEntity, PanelPartEmbedded, IEnumerable<CachedQueryDefinition>>> OnGetCachedQueryDefinition = new();


    [AutoExpressionField]
    public static IQueryable<CachedQueryEntity> CachedQueries(this DashboardEntity db) =>
        As.Expression(() => Database.Query<CachedQueryEntity>().Where(a => a.Dashboard.Is(db)));

    [AutoExpressionField]
    public static IQueryable<CachedQueryEntity> CachedQueries(this UserQueryEntity uq) =>
    As.Expression(() => Database.Query<CachedQueryEntity>().Where(a => a.UserAssets.Contains(uq.ToLite())));

    [AutoExpressionField]
    public static IQueryable<CachedQueryEntity> CachedQueries(this UserChartEntity uc) =>
        As.Expression(() => Database.Query<CachedQueryEntity>().Where(a => a.UserAssets.Contains(uc.ToLite())));

    public static void Start(SchemaBuilder sb, IFileTypeAlgorithm cachedQueryAlgorithm)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            PermissionAuthLogic.RegisterPermissions(DashboardPermission.ViewDashboard);

            FileTypeLogic.Register(CachedQueryFileType.CachedQuery, cachedQueryAlgorithm);

            UserAssetsImporter.Register<DashboardEntity>("Dashboard", DashboardOperation.Save);

            UserAssetsImporter.PartNames.AddRange(new Dictionary<string, Type>
            {
                {"UserChartPart", typeof(UserChartPartEntity)},
                {"CombinedUserChartPart", typeof(CombinedUserChartPartEntity)},
                {"UserQueryPart", typeof(UserQueryPartEntity)},
                {"LinkListPart", typeof(LinkListPartEntity)},
                {"ValueUserQueryListPart", typeof(ValueUserQueryListPartEntity)},
                {"UserTreePart", typeof(UserTreePartEntity)},
                {"ImagePart", typeof(ImagePartEntity)},
                {"SeparatorPart", typeof(SeparatorPartEntity)},
            });


            AuthLogic.HasRuleOverridesEvent += role => Database.Query<DashboardEntity>().Any(a => a.Owner.Is(role));

            SchedulerLogic.ExecuteTask.Register((DashboardEntity db, ScheduledTaskContext ctx) => { db.Execute(DashboardOperation.RegenerateCachedQueries); return null; });

            OnGetCachedQueryDefinition.Register((UserChartPartEntity ucp, PanelPartEmbedded pp) =>  new[] { new CachedQueryDefinition(ucp.UserChart.ToChartRequest().ToQueryRequest(), ucp.UserChart.Filters.GetDashboardPinnedFilterTokens(), pp, ucp.UserChart, ucp.IsQueryCached, canWriteFilters: true) });
            OnGetCachedQueryDefinition.Register((CombinedUserChartPartEntity cucp, PanelPartEmbedded pp) => cucp.UserCharts.Select(uc => new CachedQueryDefinition(uc.UserChart.ToChartRequest().ToQueryRequest(), uc.UserChart.Filters.GetDashboardPinnedFilterTokens(), pp, uc.UserChart, uc.IsQueryCached, canWriteFilters: false)));
            OnGetCachedQueryDefinition.Register((UserQueryPartEntity uqp, PanelPartEmbedded pp) => new[] { new CachedQueryDefinition(uqp.RenderMode == UserQueryPartRenderMode.BigValue ? uqp.UserQuery.ToQueryRequestValue() : uqp.UserQuery.ToQueryRequest(), uqp.UserQuery.Filters.GetDashboardPinnedFilterTokens(), pp, uqp.UserQuery, uqp.IsQueryCached, canWriteFilters: false) });
            OnGetCachedQueryDefinition.Register((ValueUserQueryListPartEntity vuql, PanelPartEmbedded pp) => vuql.UserQueries.Select(uqe => new CachedQueryDefinition(uqe.UserQuery.ToQueryRequestValue(), uqe.UserQuery.Filters.GetDashboardPinnedFilterTokens(), pp, uqe.UserQuery, uqe.IsQueryCached, canWriteFilters: false)));
            OnGetCachedQueryDefinition.Register((UserTreePartEntity ute, PanelPartEmbedded pp) => Array.Empty<CachedQueryDefinition>());
            OnGetCachedQueryDefinition.Register((LinkListPartEntity uqp, PanelPartEmbedded pp) => Array.Empty<CachedQueryDefinition>());

            sb.Include<DashboardEntity>()
                .WithVirtualMList(a => a.TokenEquivalencesGroups, e => e.Dashboard)
                .WithQuery(() => cp => new
                {
                    Entity = cp,
                    cp.Id,
                    cp.DisplayName,
                    cp.EntityType,
                    cp.Owner,
                    cp.DashboardPriority,
                });

            sb.Schema.EntityEvents<DashboardEntity>().Retrieved += DashboardLogic_Retrieved;


            sb.Include<CachedQueryEntity>()
                .WithExpressionFrom((DashboardEntity d) => d.CachedQueries())
                .WithExpressionFrom((UserChartEntity d) => d.CachedQueries())
                .WithExpressionFrom((UserQueryEntity d) => d.CachedQueries())
                  .WithQuery(() => e => new
                  {
                      Entity = e,
                      e.Id,
                      e.CreationDate,
                      e.NumColumns,
                      e.NumRows,
                      e.QueryDuration,
                      e.UploadDuration,
                      e.File,
                      UserAssetsCount = e.UserAssets.Count,
                      e.Dashboard,
                  });

            if (sb.Settings.ImplementedBy((DashboardEntity cp) => cp.Parts.First().Content, typeof(UserQueryPartEntity)))
            {
                sb.Schema.EntityEvents<UserQueryEntity>().PreUnsafeDelete += query =>
                {
                    Database.MListQuery((DashboardEntity cp) => cp.Parts).Where(mle => query.Contains(((UserQueryPartEntity)mle.Element.Content).UserQuery)).UnsafeDeleteMList();
                    Database.Query<UserQueryPartEntity>().Where(uqp => query.Contains(uqp.UserQuery)).UnsafeDelete();
                    return null;
                };

                sb.Schema.Table<QueryEntity>().PreDeleteSqlSync += q =>
                {
                    var parts = Administrator.UnsafeDeletePreCommandMList((DashboardEntity cp) => cp.Parts, Database.MListQuery((DashboardEntity cp) => cp.Parts).Where(mle =>((UserQueryPartEntity)mle.Element.Content).UserQuery.Query.Is(q)));
                    var parts2 = Administrator.UnsafeDeletePreCommand(Database.Query<UserQueryPartEntity>().Where(uqp => uqp.UserQuery.Query.Is(q)));
                    return SqlPreCommand.Combine(Spacing.Simple, parts, parts2);
                };

                sb.Schema.Table<UserQueryEntity>().PreDeleteSqlSync += arg =>
                {
                    var uq = (UserQueryEntity)arg;

                    var parts = Administrator.UnsafeDeletePreCommandMList((DashboardEntity cp) => cp.Parts, Database.MListQuery((DashboardEntity cp) => cp.Parts)
                        .Where(mle => ((UserQueryPartEntity)mle.Element.Content).UserQuery.Is(uq)));

                    var parts2 = Administrator.UnsafeDeletePreCommand(Database.Query<UserQueryPartEntity>()
                      .Where(mle => mle.UserQuery.Is(uq)));

                    return SqlPreCommand.Combine(Spacing.Simple, parts, parts2);
                };

                Validator.PropertyValidator((UserQueryPartEntity e) => e.AggregateFromSummaryHeader).StaticPropertyValidation += (UserQueryPartEntity uqp, PropertyInfo pi) =>
                {
                    if(uqp.AggregateFromSummaryHeader && uqp.RenderMode == UserQueryPartRenderMode.BigValue && uqp.UserQuery != null)
                    {
                        var first = uqp.UserQuery.Columns.FirstOrDefault(a => a.SummaryToken != null);

                        if (first == null)
                            return DashboardMessage.TheUserQuery0HasNoColumnWithSummaryHeader.NiceToString(uqp.UserQuery);
                    }

                    return null;
                };
            }

            if (sb.Settings.ImplementedBy((DashboardEntity cp) => cp.Parts.First().Content, typeof(UserChartPartEntity)))
            {
                sb.Schema.EntityEvents<UserChartEntity>().PreUnsafeDelete += query =>
                {
                    Database.MListQuery((DashboardEntity cp) => cp.Parts).Where(mle => query.Contains(((UserChartPartEntity)mle.Element.Content).UserChart)).UnsafeDeleteMList();
                    Database.Query<UserChartPartEntity>().Where(uqp => query.Contains(uqp.UserChart)).UnsafeDelete();

                    return null;
                };

                sb.Schema.Table<QueryEntity>().PreDeleteSqlSync += q =>
                {
                    var parts = Administrator.UnsafeDeletePreCommandMList((DashboardEntity cp) => cp.Parts, Database.MListQuery((DashboardEntity cp) => cp.Parts).Where(mle => ((UserChartPartEntity)mle.Element.Content).UserChart.Query.Is(q)));
                    var parts2 = Administrator.UnsafeDeletePreCommand(Database.Query<UserChartPartEntity>().Where(uqp => uqp.UserChart.Query.Is(q)));
                    return SqlPreCommand.Combine(Spacing.Simple, parts, parts2);
                };

                sb.Schema.Table<UserChartEntity>().PreDeleteSqlSync += arg =>
                {
                    var uc = (UserChartEntity)arg;

                    var mlistElems = Administrator.UnsafeDeletePreCommandMList((DashboardEntity cp) => cp.Parts, Database.MListQuery((DashboardEntity cp) => cp.Parts)
                        .Where(mle => ((UserChartPartEntity)mle.Element.Content).UserChart.Is(uc)));

                    var parts = Administrator.UnsafeDeletePreCommand(Database.Query<UserChartPartEntity>()
                       .Where(ucp => ucp.UserChart.Is(uc)));

                    return SqlPreCommand.Combine(Spacing.Simple, mlistElems, parts);
                };
            }

            if (sb.Settings.ImplementedBy((DashboardEntity cp) => cp.Parts.First().Content, typeof(CombinedUserChartPartEntity)))
            {
                sb.Schema.EntityEvents<UserChartEntity>().PreUnsafeDelete += query =>
                {
                    Database.MListQuery((CombinedUserChartPartEntity e) => e.UserCharts).Where(mle => query.Contains(mle.Element.UserChart)).UnsafeDeleteMList();

                    return null;
                };

                sb.Schema.Table<UserChartEntity>().PreDeleteSqlSync += arg =>
                {
                    var uc = (UserChartEntity)arg;
                   
                    var mlistElems2 = Administrator.UnsafeDeletePreCommandMList((CombinedUserChartPartEntity e) => e.UserCharts,
                         Database.MListQuery((CombinedUserChartPartEntity e) => e.UserCharts).Where(mle => mle.Element.UserChart.Is(uc)));

                    return SqlPreCommand.Combine(Spacing.Simple, mlistElems2);
                };

                sb.Schema.Table<QueryEntity>().PreDeleteSqlSync += arg =>
                {
                    var query = (QueryEntity)arg;

                    var parts = Administrator.UnsafeDeletePreCommandMList((CombinedUserChartPartEntity e) => e.UserCharts,
                         Database.MListQuery((CombinedUserChartPartEntity e) => e.UserCharts).Where(mle => mle.Element.UserChart.Query.Is(query)));
                    return SqlPreCommand.Combine(Spacing.Simple, parts);
                };
            }

            sb.Schema.EntityEvents<DashboardEntity>().PreUnsafeDelete += query =>
            {
                query.SelectMany(d => d.CachedQueries()).UnsafeDelete();
                return null;
            };

            DashboardGraph.Register();


            Dashboards = sb.GlobalLazy(() => Database.Query<DashboardEntity>().ToDictionary(a => a.ToLite()),
                new InvalidateWith(typeof(DashboardEntity)));

            CachedQueriesCache = sb.GlobalLazy(() => Database.Query<CachedQueryEntity>().GroupToDictionary(a => a.Dashboard),
                new InvalidateWith(typeof(CachedQueryEntity)));

            DashboardsByType = sb.GlobalLazy(() => Dashboards.Value.Values.Where(a => a.EntityType != null)
            .SelectCatch(d => KeyValuePair.Create(TypeLogic.IdToType.GetOrThrow(d.EntityType!.Id), d.ToLite()))
            .GroupToDictionary(),
                new InvalidateWith(typeof(DashboardEntity)));
        }
    }

    public static void UpdateDashboardIconNameInDB()
    {
        Database.Query<DashboardEntity>().Where(db => db.Parts.Any(p => p.IconName.HasText())).ToList().ForEach(db => {
            db.Parts.Where(p => p.IconName.HasText()).ToList().ForEach(p => {
                p.IconName = FontAwesomeV6Upgrade.UpdateIconName(p.IconName!);
            });
            db.Save();
        });
    }

    private static void DashboardLogic_Retrieved(DashboardEntity db, PostRetrievingContext ctx)
    {
        db.ParseData(query =>
        {
            object? queryName = query.ToQueryNameCatch();
            if (queryName == null)
                return null;

            return QueryLogic.Queries.QueryDescription(queryName);
        });
    }

    class DashboardGraph : Graph<DashboardEntity>
    {
        public static void Register()
        {
            new Execute(DashboardOperation.Save)
            {
                CanBeNew = true,
                CanBeModified = true,
                Execute = (cp, _) => 
                {
                    var oldParts = cp.IsNew ? new() : cp.InDB().SelectMany(a => a.Parts).Select(p => p.Content).ToList();

                    cp.Save();

                    var newParts = cp.Parts.Select(a => a.Content).ToList();

                    var toDelete = oldParts.Except(newParts).ToList();

                    Database.DeleteList(toDelete);
                }
            }.Register();

            new Delete(DashboardOperation.Delete)
            {
                Delete = (cp, _) =>
                {
                    var parts = cp.Parts.Select(a => a.Content).ToList();
                    cp.Delete();
                    Database.DeleteList(parts);
                }
            }.Register();

            new ConstructFrom<DashboardEntity>(DashboardOperation.Clone)
            {
                Construct = (cp, _) => cp.Clone()
            }.Register();

            new Execute(DashboardOperation.RegenerateCachedQueries)
            {
                CanExecute = c => c.CacheQueryConfiguration == null ? ValidationMessage._0IsNotSet.NiceToString(ReflectionTools.GetPropertyInfo(() => c.CacheQueryConfiguration)) : null,
                AvoidImplicitSave = true,
                Execute = (db, _) =>
                {
                    var cq = db.CacheQueryConfiguration!;

                    var oldCachedQueries = db.CachedQueries().ToList();
                    oldCachedQueries.ForEach(a => a.File.DeleteFileOnCommit());
                    db.CachedQueries().UnsafeDelete();

                    var definitions = DashboardLogic.GetCachedQueryDefinitions(db).ToList();

                    var combined = DashboardLogic.CombineCachedQueryDefinitions(definitions);

                    foreach (var c in combined)
                    {
                        var qr = c.QueryRequest;

                        if (qr.Pagination is Pagination.All)
                        {
                            qr = qr.Clone();
                            qr.Pagination = new Pagination.Firsts(cq.MaxRows + 1);
                        }

                        var now = Clock.Now;

                        Stopwatch sw = Stopwatch.StartNew();

                        var rt = Connector.CommandTimeoutScope(cq.TimeoutForQueries).Using(_ => QueryLogic.Queries.ExecuteQuery(qr));

                        var queryDuration = sw.ElapsedMilliseconds;

                        if(c.QueryRequest.Pagination is Pagination.All)
                        {
                            if (rt.Rows.Length == cq.MaxRows)
                                throw new ApplicationException($"The query for {c.UserAssets.CommaAnd(a => a.KeyLong())} has returned more than {cq.MaxRows} rows: " +
                                    JsonSerializer.Serialize(QueryRequestTS.FromQueryRequest(c.QueryRequest), EntityJsonContext.FullJsonSerializerOptions));
                            else
                                rt = new ResultTable(rt.AllColumns(), null, new Pagination.All());
                        }


                        sw.Restart();

                        var json = new CachedQueryJS
                        {
                            CreationDate = now,
                            QueryRequest = QueryRequestTS.FromQueryRequest(c.QueryRequest),
                            ResultTable = rt,
                        };

                        var bytes =  JsonSerializer.SerializeToUtf8Bytes(json, EntityJsonContext.FullJsonSerializerOptions);

                        var file = new Entities.Files.FilePathEmbedded(CachedQueryFileType.CachedQuery, "CachedQuery.json", bytes).SaveFile();

                        var uploadDuration = sw.ElapsedMilliseconds;

                        new CachedQueryEntity
                        {
                            CreationDate = now,
                            UserAssets = c.UserAssets.ToMList(),
                            NumColumns = qr.Columns.Count + (qr.GroupResults ? 0 : 1),
                            NumRows = rt.Rows.Length,
                            QueryDuration = queryDuration,
                            UploadDuration = uploadDuration,
                            File = file,
                            Dashboard = db.ToLite(),
                        }.Save();
                    }

                }
            }.SetMinimumTypeAllowed(TypeAllowedBasic.Read).Register();
        }
    }

    public static DashboardEntity? GetHomePageDashboard()
    {
        var result = GetDashboard(null);

        if (result == null)
            return null;

        using (ViewLogLogic.LogView(result.ToLite(), "GetHomePageDashboard"))
            return result;
    }

    public static Func<string?, DashboardEntity?> GetDashboard = GetDashboardDefault;

    static DashboardEntity? GetDashboardDefault(string? key)
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<DashboardEntity>(userInterface: false);

        var result = Dashboards.Value.Values
            .Where(d =>
                (!key.HasText() || d.Key == key)
                && d.EntityType == null && d.DashboardPriority.HasValue && isAllowed(d))
            .OrderByDescending(a => a.DashboardPriority)
            .FirstOrDefault();

        return result;
    }

    public static void RegisterTranslatableRoutes()
    {
        TranslatedInstanceLogic.AddRoute((DashboardEntity d) => d.DisplayName);
        TranslatedInstanceLogic.AddRoute((DashboardEntity d) => d.Parts[0].Title);
    }

    public static List<DashboardEntity> GetEmbeddedDashboards(Type entityType)
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<DashboardEntity>(userInterface: false);

        var result = DashboardsByType.Value.TryGetC(entityType).EmptyIfNull().Select(lite => Dashboards.Value.GetOrThrow(lite))
            .Where(d => d.EmbeddedInEntity != DashboardEmbedededInEntity.None && isAllowed(d))
            .OrderByDescending(a => a.DashboardPriority)
            .ToList();

        if (!result.Any())
            return result;

        foreach (var item in result)
        {
            using (ViewLogLogic.LogView(item.ToLite(), "GetEmbeddedDashboards"))
            {
            } 
        }
        return result;
    }

    public static List<Lite<DashboardEntity>> GetDashboards()
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<DashboardEntity>(userInterface: false);
        return Dashboards.Value.Values
            .Where(d => d.EntityType == null && isAllowed(d))
            .Select(d => d.ToLite(TranslatedInstanceLogic.TranslatedField(d, d => d.DisplayName)))
            .ToList();
    }

    public static List<Lite<DashboardEntity>> GetDashboardsEntity(Type entityType)
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<DashboardEntity>(userInterface: false);
        return DashboardsByType.Value.TryGetC(entityType)
            .EmptyIfNull()
            .Select(lite => Dashboards.Value.GetOrThrow(lite))
            .Where(d => isAllowed(d))
            .Select(d => d.ToLite(TranslatedInstanceLogic.TranslatedField(d, d => d.DisplayName)))
            .ToList();
    }

    public static List<Lite<DashboardEntity>> Autocomplete(string subString, int limit)
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<DashboardEntity>(userInterface: false);
        return Dashboards.Value.Values
            .Where(d => d.EntityType == null && isAllowed(d))
            .Select(d => d.ToLite(TranslatedInstanceLogic.TranslatedField(d, d => d.DisplayName)))
            .Autocomplete(subString, limit).ToList();
    }

    public static DashboardEntity RetrieveDashboard(this Lite<DashboardEntity> dashboard)
    {
        using (ViewLogLogic.LogView(dashboard, "Dashboard"))
        {
            var result = Dashboards.Value.GetOrThrow(dashboard);

            var isAllowed = Schema.Current.GetInMemoryFilter<DashboardEntity>(userInterface: false);
            if (!isAllowed(result))
                throw new EntityNotFoundException(dashboard.EntityType, dashboard.Id);

            return result;
        }
    }

    public static IEnumerable<CachedQueryEntity> GetCachedQueries(Lite<DashboardEntity> dashboard)
    {
        return CachedQueriesCache.Value.TryGetC(dashboard).EmptyIfNull();
    }

    public static void RegisterUserTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
    {
        sb.Schema.Settings.AssertImplementedBy((DashboardEntity uq) => uq.Owner, typeof(UserEntity));

        TypeConditionLogic.RegisterCompile<DashboardEntity>(typeCondition,
            uq => uq.Owner.Is(UserEntity.Current));

        RegisterPartsTypeCondition(typeCondition);
    }

    public static void RegisterRoleTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
    {
        sb.Schema.Settings.AssertImplementedBy((DashboardEntity uq) => uq.Owner, typeof(RoleEntity));

        TypeConditionLogic.RegisterCompile<DashboardEntity>(typeCondition,
            uq => AuthLogic.CurrentRoles().Contains(uq.Owner) || uq.Owner == null);

        RegisterPartsTypeCondition(typeCondition);
    }

    public static void RegisterPartsTypeCondition(TypeConditionSymbol typeCondition)
    {
        TypeConditionLogic.Register<TokenEquivalenceGroupEntity>(typeCondition,
            teg => Database.Query<DashboardEntity>().WhereCondition(typeCondition).Any(d => d.TokenEquivalencesGroups.Contains(teg)));

        TypeConditionLogic.Register<ValueUserQueryListPartEntity>(typeCondition,
             cscp => Database.Query<DashboardEntity>().WhereCondition(typeCondition).Any(d => d.ContainsContent(cscp)));

        TypeConditionLogic.Register<LinkListPartEntity>(typeCondition,
             llp => Database.Query<DashboardEntity>().WhereCondition(typeCondition).Any(d => d.ContainsContent(llp)));

        TypeConditionLogic.Register<UserChartPartEntity>(typeCondition,
             ucp => Database.Query<DashboardEntity>().WhereCondition(typeCondition).Any(d => d.ContainsContent(ucp)));

        TypeConditionLogic.Register<CombinedUserChartPartEntity>(typeCondition,
            ucp => Database.Query<DashboardEntity>().WhereCondition(typeCondition).Any(d => d.ContainsContent(ucp)));

        TypeConditionLogic.Register<UserQueryPartEntity>(typeCondition,
            uqp => Database.Query<DashboardEntity>().WhereCondition(typeCondition).Any(d => d.ContainsContent(uqp)));

        TypeConditionLogic.Register<ImagePartEntity>(typeCondition,
            uqp => Database.Query<DashboardEntity>().WhereCondition(typeCondition).Any(d => d.ContainsContent(uqp)));

        TypeConditionLogic.Register<SeparatorPartEntity>(typeCondition,
            uqp => Database.Query<DashboardEntity>().WhereCondition(typeCondition).Any(d => d.ContainsContent(uqp)));
    }

    public static List<CachedQueryDefinition> GetCachedQueryDefinitions(DashboardEntity db)
    {
        var definitions = db.Parts.SelectMany(p => OnGetCachedQueryDefinition.Invoke(p.Content, p)).ToList();

        var groups = definitions
            .Where(a => a.PanelPart.InteractionGroup != null)
            .GroupToDictionary(a => a.PanelPart.InteractionGroup!.Value);

        foreach (var (key, cqdefs) in groups)
        {
            var writers = cqdefs.Where(a => a.CanWriteFilters).ToList();
            if (!writers.Any())
                continue;

            var equivalenceGroups = db.TokenEquivalencesGroups.Where(a => a.InteractionGroup == key || a.InteractionGroup == null);

            foreach (var wr in writers)
            {
                var keyColumns = wr.QueryRequest.GroupResults ?
                    wr.QueryRequest.Columns.Where(c => c.Token is not AggregateToken).Select(c => c.Token).Distinct().ToList() :
                    wr.QueryRequest.Columns.Select(c => c.Token).Distinct().ToList();
                
                Dictionary<QueryToken, Dictionary<object, List<QueryToken>>> equivalencesDictionary = GetEquivalenceDictionary(equivalenceGroups, fromQuery: wr.QueryRequest.QueryName);

                foreach (var cqd in cqdefs.Where(e => e != wr))
                {
                    List<QueryToken> extraColumns = ExtraColumns(keyColumns, cqd, equivalencesDictionary);

                    if (extraColumns.Any())
                    {
                        ExpandColumns(cqd, extraColumns, "Dashboard Filters from " + key);
                    }

                    cqd.QueryRequest.Pagination = new Pagination.All();
                }
            }
        }

        foreach (var writer in definitions)
        {
            if (writer.PinnedFiltersTokens.Any())
            {
                var pft = writer.PinnedFiltersTokens.Where(a => a.prototedToDashboard == false).Select(a=>a.token).ToList();
                if (pft.Any())
                    ExpandColumns(writer, pft, "Pinned Filters");

                var dpft = writer.PinnedFiltersTokens.Where(a => a.prototedToDashboard == true).Select(a => a.token).ToList();
                if (dpft.Any())
                {
                    var equivalenceGroups = db.TokenEquivalencesGroups.Where(a => /*a.InteractionGroup == writer.PanelPart.InteractionGroup Needed? ||*/ a.InteractionGroup == null);

                    Dictionary<QueryToken, Dictionary<object, List<QueryToken>>> equivalencesDictionary = GetEquivalenceDictionary(equivalenceGroups, fromQuery: writer.QueryRequest.QueryName);

                    foreach (var cqd in definitions.Where(e => e != writer))
                    {
                        List<QueryToken> extraColumns = ExtraColumns(dpft, cqd, equivalencesDictionary);
                        if (extraColumns.Any())
                        {
                            ExpandColumns(cqd, extraColumns, "Dashboard Pinned Filters");
                        }
                    }
                }

            }
        }

        var cached = definitions.Where(a => a.IsQueryCached).ToList();

        return cached;
    }

    private static List<QueryToken> ExtraColumns(List<QueryToken> requiredTokens, CachedQueryDefinition cqd, Dictionary<QueryToken, Dictionary<object, List<QueryToken>>> equivalencesDictionary)
    {
        var extraColumns = requiredTokens.Select(t =>
        {
            var translatedToken = TranslatedToken(t, cqd.QueryRequest.QueryName, equivalencesDictionary);

            if (translatedToken == null)
                return null;

            if (!cqd.QueryRequest.Columns.Any(c => translatedToken.Contains(c.Token)))
                return translatedToken.FirstEx(); //Doesn't really matter if we add "Product" or "Entity.Product"; 

            return null;
        }).NotNull().ToList();
        return extraColumns;
    }

    private static Dictionary<QueryToken, Dictionary<object, List<QueryToken>>> GetEquivalenceDictionary(IEnumerable<TokenEquivalenceGroupEntity> equivalences, object fromQuery)
    {
        return (from gr in equivalences
                from t in gr.TokenEquivalences.Where(a => a.Query.ToQueryName() == fromQuery)
                select KeyValuePair.Create(t.Token.Token, gr.TokenEquivalences.GroupToDictionary(a => a.Query.ToQueryName(), a => a.Token.Token)))
                .ToDictionaryEx();
    }

    private static void ExpandColumns(CachedQueryDefinition cqd, List<QueryToken> extraColumns, string errorContext)
    {
        if (cqd.QueryRequest.GroupResults)
        {
            var errors = extraColumns
                .Select(a => new {
                    token = a,
                    error = QueryUtils.CanColumn(a) ?? (cqd.QueryRequest.GroupResults && !a.IsGroupable ? "Is not groupable" : null)
                })
                .Where(a => a.error != null);

            if (errors.Any())
                throw new InvalidOperationException($"Unable to expand columns in '{cqd.UserAsset.KeyLong()}' (query {QueryUtils.GetKey(cqd.QueryRequest.QueryName)}) requested by {errorContext} because: \r\n{errors.ToString(a => a.token.FullKey() + ": " + a.error, "\r\n")}");
        }

        cqd.QueryRequest.Columns.AddRange(extraColumns.Select(c => new Column(c, null)));
        var avgs = cqd.QueryRequest.Columns.Extract(a => a.Token is AggregateToken at && at.AggregateFunction == AggregateFunction.Average);
                            foreach (var av in avgs)
                            {
            cqd.QueryRequest.Columns.Remove(av);
            cqd.QueryRequest.Columns.Add(new Column(new AggregateToken(AggregateFunction.Sum, av.Token.Parent!), null));
            cqd.QueryRequest.Columns.Add(new Column(new AggregateToken(AggregateFunction.Count, av.Token.Parent!, FilterOperation.DistinctTo, null), null));
                            }
                        }

    private static List<QueryToken>? TranslatedToken(QueryToken original, object targetQueryName, Dictionary<QueryToken, Dictionary<object, List<QueryToken>>> equivalences)
    {
        var toAppend = new List<QueryToken>();
        for (var t = original; t != null; t = t.Parent)
        {
            {
                if (equivalences.TryGetValue(t, out var dic) && dic.TryGetValue(targetQueryName, out var list))
                    return list.Select(t => AppendTokens(t, toAppend)).ToList();
                    }

            toAppend.Insert(0, t);

            if(t.Parent == null)
            {
                var entityToken = QueryUtils.Parse("Entity", QueryLogic.Queries.QueryDescription(original.QueryName), 0);

                if(equivalences.TryGetValue(entityToken, out var dic) && dic.TryGetValue(targetQueryName, out var list))
                    return list.Select(t => AppendTokens(t, toAppend)).ToList();
                }
            }

        if (original.QueryName == targetQueryName)
            return new List<QueryToken> { original };

        return null;

        }

    private static QueryToken AppendTokens(QueryToken t, List<QueryToken> toAppend)
    {
        var qd = QueryLogic.Queries.QueryDescription(t.QueryName);

        foreach (var nt in toAppend)
        {
            var newToken = QueryUtils.SubToken(t, qd, SubTokensOptions.CanAnyAll | SubTokensOptions.CanAggregate | SubTokensOptions.CanElement, nt.Key);

            if (newToken == null)
                throw new FormatException("Token with key '{0}' not found on {1} of query {2}".FormatWith(nt.Key, t, QueryUtils.GetKey(qd.QueryName)));

            t = newToken;
    }

        return t;
    }

    public static List<CombinedCachedQueryDefinition> CombineCachedQueryDefinitions(List<CachedQueryDefinition> cachedQueryDefinition)
    {
        var result = new List<CombinedCachedQueryDefinition>();
        foreach (var cqd in cachedQueryDefinition)
        {

            var combined = false;
            foreach (var r in result)
            {
                if (r.CombineIfPossible(cqd))
                {
                    combined = true;
                    break;
                }
            }
            if (!combined)
                result.Add(new CombinedCachedQueryDefinition(cqd));
        }
        return result;
    }
        
}

public class CachedQueryDefinition
{
    public CachedQueryDefinition(QueryRequest queryRequest, List<(QueryToken token, bool prototedToDashboard)> pinnedFiltersTokens, PanelPartEmbedded panelPart, IUserAssetEntity userAsset, bool isQueryCached, bool canWriteFilters)
    {
        QueryRequest = queryRequest;
        PinnedFiltersTokens = pinnedFiltersTokens;
        PanelPart = panelPart;
        Guid = userAsset.Guid;
        UserAsset = userAsset.ToLite();
        IsQueryCached = isQueryCached;
        CanWriteFilters = canWriteFilters;
    }

    public QueryRequest QueryRequest { get; set; }
    public List<(QueryToken token, bool prototedToDashboard)> PinnedFiltersTokens { get; set; }
    public PanelPartEmbedded PanelPart { get; set; }
    public Guid Guid { get; set; }
    public Lite<IUserAssetEntity> UserAsset { get; set; }
    public bool IsQueryCached { get; }
    public bool CanWriteFilters { get; }

    public override string ToString() => $"{UserAsset} IsQueryCached={IsQueryCached} CanWriteFilters={CanWriteFilters}";
}

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
public class CachedQueryJS
{
    public DateTime CreationDate;
    public QueryRequestTS QueryRequest;
    public ResultTable ResultTable;
}


public class CombinedCachedQueryDefinition
{
    public QueryRequest QueryRequest { get; set; }
    public HashSet<Lite<IUserAssetEntity>> UserAssets { get; set; }

    public CombinedCachedQueryDefinition(CachedQueryDefinition definition)
    {
        this.QueryRequest = definition.QueryRequest;
        this.UserAssets = new HashSet<Lite<IUserAssetEntity>> { definition.UserAsset };
    }

    public bool CombineIfPossible(CachedQueryDefinition definition)
    {
        var me = QueryRequest;
        var other = definition.QueryRequest;

        if (!me.QueryName.Equals(other.QueryName))
            return false;

        if (me.GroupResults != other.GroupResults)
            return false;

        if (me.GroupResults)
        {
            var meKeys = me.Columns.Select(a => a.Token).Where(t => t is not AggregateToken).ToHashSet();
            var otherKeys = other.Columns.Select(a => a.Token).Where(t => t is not AggregateToken).ToHashSet();
            if (!meKeys.SetEquals(otherKeys))
                return false;
        }

        var meExtraFilters = me.Filters.Distinct(FilterComparer.Instance).Except(other.Filters, FilterComparer.Instance).ToList();
        var otherExtraFilters = other.Filters.Distinct(FilterComparer.Instance).Except(me.Filters, FilterComparer.Instance).ToList();

        if (meExtraFilters.Count > 0 || otherExtraFilters.Count > 0)
            return false;

        if (me.Pagination is Pagination.All)
        {
            this.QueryRequest = WithExtraColumns(me, other);

            this.UserAssets.Add(definition.UserAsset);

            return true;

        }
        
        if (other.Pagination is Pagination.All)
        {
            this.QueryRequest = WithExtraColumns(other, me);

            this.UserAssets.Add(definition.UserAsset);

            return true;
        }

        if (me.Pagination.Equals(other.Pagination) && me.Orders.SequenceEqual(other.Orders))
        {
            this.QueryRequest = WithExtraColumns(me, other);

            this.UserAssets.Add(definition.UserAsset);

            return true;
        }   

        //More cases?

        return false;
    }

    static QueryRequest WithExtraColumns(QueryRequest me, QueryRequest other)
    {
        var otherExtraColumns = other.Columns.Where(c => !me.GroupResults || c.Token is AggregateToken).Where(c => !me.Columns.Any(c2 => c.Token.Equals(c2.Token))).ToList();

        if (otherExtraColumns.Count == 0)
            return me;

        var clone = me.Clone();
        clone.Columns = me.Columns.Concat(otherExtraColumns).ToList();
        return clone;
    }
}

public class FilterComparer : IEqualityComparer<Filter>
{
    public static readonly FilterComparer Instance = new FilterComparer();

    public bool Equals(Filter? x, Filter? y)
    {
        if (x == null)
            return y == null;

        if (y == null)
            return false;

        if (x is FilterCondition xc)
        {
            if (y is not FilterCondition yc)
                return false;

            return xc.Token.Equals(yc.Token)
            && xc.Operation == yc.Operation
            && object.Equals(xc.Value, yc.Value);
        }
        else if (x is FilterGroup xg)
        {
            if (y is not FilterGroup yg)
                return false;

            return object.Equals(xg.Token, yg.Token) &&
                xg.GroupOperation == yg.GroupOperation &&
                xg.Filters.ToHashSet(this).SetEquals(yg.Filters);
        }
        else 
            throw new UnexpectedValueException(x);
    }

    public int GetHashCode([DisallowNull] Filter obj)
    {
        return obj is FilterCondition f ? f.Token.GetHashCode() :
            obj is FilterGroup fg ? fg.GroupOperation.GetHashCode() :
            throw new UnexpectedValueException(obj);
    }
}
