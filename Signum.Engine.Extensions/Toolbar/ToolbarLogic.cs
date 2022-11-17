using Signum.Engine.Authorization;
using Signum.Engine.Chart;
using Signum.Engine.Dashboard;
using Signum.Engine.Translation;
using Signum.Engine.UserAssets;
using Signum.Engine.UserQueries;
using Signum.Engine.Workflow;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Chart;
using Signum.Entities.Dashboard;
using Signum.Entities.Toolbar;
using Signum.Entities.UserQueries;
using Signum.Entities.Workflow;
using Signum.Utilities.DataStructures;
using System.Text.Json.Serialization;

namespace Signum.Engine.Toolbar;

public static class ToolbarLogic
{
    public static ResetLazy<Dictionary<Lite<ToolbarEntity>, ToolbarEntity>> Toolbars = null!;
    public static ResetLazy<Dictionary<Lite<ToolbarMenuEntity>, ToolbarMenuEntity>> ToolbarMenus = null!;

    public static Dictionary<PermissionSymbol, Func<List<ToolbarResponse>>> CustomPermissionResponse = 
        new Dictionary<PermissionSymbol, Func<List<ToolbarResponse>>>();

    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            sb.Include<ToolbarEntity>()
                .WithSave(ToolbarOperation.Save)
                .WithDelete(ToolbarOperation.Delete)
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Name,
                    e.Owner,
                    e.Priority
                });

            sb.Schema.EntityEvents<ToolbarEntity>().Saving += IToolbar_Saving;
            sb.Schema.EntityEvents<ToolbarMenuEntity>().Saving += IToolbar_Saving;

            sb.Include<ToolbarMenuEntity>()
                .WithSave(ToolbarMenuOperation.Save)
                .WithDelete(ToolbarMenuOperation.Delete)
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Name
                });

            AuthLogic.HasRuleOverridesEvent += role =>
                Database.Query<ToolbarEntity>().Any(a => a.Owner.Is(role)) ||
                Database.Query<ToolbarMenuEntity>().Any(a => a.Owner.Is(role));

            UserAssetsImporter.Register<ToolbarEntity>("Toolbar", ToolbarOperation.Save);
            UserAssetsImporter.Register<ToolbarMenuEntity>("ToolbarMenu", ToolbarMenuOperation.Save);

            RegisterDelete<UserQueryEntity>(sb, uq => uq.Query);
            RegisterDelete<UserChartEntity>(sb, uq => uq.Query);
            RegisterDelete<QueryEntity>(sb);
            RegisterDelete<DashboardEntity>(sb);
            RegisterDelete<ToolbarMenuEntity>(sb);
            RegisterDelete<WorkflowEntity>(sb);

            RegisterContentConfig<ToolbarMenuEntity>(
                lite => ToolbarMenus.Value.GetOrCreate(lite).IsAllowedFor(TypeAllowedBasic.Read, inUserInterface: false),
                lite => TranslatedInstanceLogic.TranslatedField(ToolbarMenus.Value.GetOrCreate(lite), a => a.Name));

            RegisterContentConfig<ToolbarEntity>(
                lite => Toolbars.Value.GetOrCreate(lite).IsAllowedFor(TypeAllowedBasic.Read, inUserInterface: false),
                lite => TranslatedInstanceLogic.TranslatedField(Toolbars.Value.GetOrCreate(lite), a => a.Name));

            RegisterContentConfig<UserQueryEntity>(
                lite => { var uq = UserQueryLogic.UserQueries.Value.GetOrCreate(lite); return InMemoryFilter(uq) && QueryLogic.Queries.QueryAllowed(uq.Query.ToQueryName(), true); },
                lite => TranslatedInstanceLogic.TranslatedField(UserQueryLogic.UserQueries.Value.GetOrCreate(lite), a => a.DisplayName));

            RegisterContentConfig<UserChartEntity>(
                lite => { var uc = UserChartLogic.UserCharts.Value.GetOrCreate(lite); return InMemoryFilter(uc) && QueryLogic.Queries.QueryAllowed(uc.Query.ToQueryName(), true); },
                lite => TranslatedInstanceLogic.TranslatedField(UserChartLogic.UserCharts.Value.GetOrCreate(lite), a => a.DisplayName));

            RegisterContentConfig<QueryEntity>(
              lite => IsQueryAllowed(lite),
              lite => QueryUtils.GetNiceName(QueryLogic.QueryNames.GetOrThrow(lite.ToString()!)));

            RegisterContentConfig<DashboardEntity>(
              lite => InMemoryFilter(DashboardLogic.Dashboards.Value.GetOrCreate(lite)),
              lite => TranslatedInstanceLogic.TranslatedField(DashboardLogic.Dashboards.Value.GetOrCreate(lite), a => a.DisplayName));

            RegisterContentConfig<PermissionSymbol>(
                lite => PermissionAuthLogic.IsAuthorized(SymbolLogic<PermissionSymbol>.ToSymbol(lite.ToString()!)),
                lite => SymbolLogic<PermissionSymbol>.ToSymbol(lite.ToString()!).NiceToString());

            ToolbarLogic.GetContentConfig<PermissionSymbol>().CustomResponses = lite =>
            {
                var action = CustomPermissionResponse.TryGetC(lite.Retrieve());

                if (action != null)
                    return action();

                return null;
            };

            RegisterContentConfig<WorkflowEntity>(
              lite => { var wf = WorkflowLogic.WorkflowGraphLazy.Value.GetOrCreate(lite); return InMemoryFilter(wf.Workflow) && wf.IsStartCurrentUser(); },
                lite => TranslatedInstanceLogic.TranslatedField(WorkflowLogic.WorkflowGraphLazy.Value.GetOrCreate(lite).Workflow, a => a.Name));


            


            //    { typeof(QueryEntity), a => IsQueryAllowed((Lite<QueryEntity>)a) },
            //{ typeof(PermissionSymbol), a => PermissionAuthLogic.IsAuthorized((PermissionSymbol)a.RetrieveAndRemember()) },
            //{ typeof(UserQueryEntity), a => ,
            //{ typeof(UserChartEntity), a => { var uc = UserChartLogic.UserCharts.Value.GetOrCreate((Lite<UserChartEntity>)a); return InMemoryFilter(uc) && QueryLogic.Queries.QueryAllowed(uc.Query.ToQueryName(), true); } },
            //{ typeof(DashboardEntity), a => InMemoryFilter(DashboardLogic.Dashboards.Value.GetOrCreate((Lite<DashboardEntity>)a)) },
            //{ typeof(WorkflowEntity), a => { var wf = WorkflowLogic.WorkflowGraphLazy.Value.GetOrCreate((Lite<WorkflowEntity>)a); return InMemoryFilter(wf.Workflow) && wf.IsStartCurrentUser(); } },


            Toolbars = sb.GlobalLazy(() => Database.Query<ToolbarEntity>().ToDictionary(a => a.ToLite()),
               new InvalidateWith(typeof(ToolbarEntity)));

            ToolbarMenus = sb.GlobalLazy(() => Database.Query<ToolbarMenuEntity>().ToDictionary(a => a.ToLite()),
               new InvalidateWith(typeof(ToolbarMenuEntity)));
        }
    }

    public static void UpdateToolbarIconNameInDB()
    {
        Database.Query<ToolbarEntity>().Where(t => t.Elements.Any(e => e.IconName.HasText())).ToList().ForEach(t => {
            t.Elements.Where(e => e.IconName.HasText()).ToList().ForEach(e => {
                e.IconName = FontAwesomeV6Upgrade.UpdateIconName(e.IconName!);
            });
            t.Save();
        });

        Database.Query<ToolbarMenuEntity>().Where(t => t.Elements.Any(e => e.IconName.HasText())).ToList().ForEach(t => {
            t.Elements.Where(e => e.IconName.HasText()).ToList().ForEach(e => {
                e.IconName = FontAwesomeV6Upgrade.UpdateIconName(e.IconName!);
            });
            t.Save();
        });
    }

    private static void IToolbar_Saving(IToolbarEntity tool)
    {
        if (!tool.IsNew && tool.Elements.IsGraphModified)
        {
            using (new EntityCache(EntityCacheType.ForceNew))
            {
                EntityCache.AddFullGraph((Entity)tool);

                var toolbarGraph = DirectedGraph<IToolbarEntity>.Generate(tool, t => t.Elements.Select(a => a.Content).Where(c => c is Lite<IToolbarEntity>).Select(t => (IToolbarEntity)t!.Retrieve()));

                var problems = toolbarGraph.FeedbackEdgeSet().Edges.ToList();

                if (problems.Count > 0)
                    throw new ApplicationException(
                        ToolbarMessage._0CyclesHaveBeenFoundInTheToolbarDueToTheRelationships.NiceToString().FormatWith(problems.Count) +
                        problems.ToString("\r\n"));
            }
        }
    }

    public static void RegisterUserTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
    {
        sb.Schema.Settings.AssertImplementedBy((ToolbarEntity t) => t.Owner, typeof(UserEntity));

        TypeConditionLogic.RegisterCompile<ToolbarEntity>(typeCondition,
            t => t.Owner.Is(UserEntity.Current));

        sb.Schema.Settings.AssertImplementedBy((ToolbarMenuEntity t) => t.Owner, typeof(UserEntity));

        TypeConditionLogic.RegisterCompile<ToolbarMenuEntity>(typeCondition,
            t => t.Owner.Is(UserEntity.Current));
    }

    public static void RegisterRoleTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
    {
        sb.Schema.Settings.AssertImplementedBy((ToolbarEntity t) => t.Owner, typeof(RoleEntity));

        TypeConditionLogic.RegisterCompile<ToolbarEntity>(typeCondition,
            t => AuthLogic.CurrentRoles().Contains(t.Owner) || t.Owner == null);

        sb.Schema.Settings.AssertImplementedBy((ToolbarMenuEntity t) => t.Owner, typeof(RoleEntity));

        TypeConditionLogic.RegisterCompile<ToolbarMenuEntity>(typeCondition,
            t => AuthLogic.CurrentRoles().Contains(t.Owner) || t.Owner == null);
    }

    public static void RegisterDelete<T>(SchemaBuilder sb, Expression<Func<T, QueryEntity>>? querySelectorForSync = null) where T : Entity
    {
        if (sb.Settings.ImplementedBy((ToolbarEntity tb) => tb.Elements.First().Content, typeof(T)))
        {
            sb.Schema.EntityEvents<T>().PreUnsafeDelete += query =>
            {
                if (Schema.Current.IsAllowed(typeof(ToolbarEntity), false) == null)
                    Database.MListQuery((ToolbarEntity tb) => tb.Elements).Where(mle => query.Contains((T)mle.Element.Content!.Entity)).UnsafeDeleteMList();
                return null;
            };

            sb.Schema.Table<T>().PreDeleteSqlSync += arg =>
            {
                var entity = (T)arg;

                var parts = Administrator.UnsafeDeletePreCommandMList((ToolbarEntity tb) => tb.Elements, Database.MListQuery((ToolbarEntity tb) => tb.Elements)
                    .Where(mle => mle.Element.Content!.Entity == entity));

                return parts;
            };

            if(querySelectorForSync != null)
            {
                sb.Schema.Table<QueryEntity>().PreDeleteSqlSync += q =>
                {
                    var parts = Administrator.UnsafeDeletePreCommandMList((ToolbarEntity te) => te.Elements, Database.MListQuery((ToolbarEntity tb) => tb.Elements).Where(mle => querySelectorForSync.Evaluate((T)mle.Element.Content!.Entity).Is(q)));
                    return parts;
                };
            }
        }

        if (sb.Settings.ImplementedBy((ToolbarMenuEntity tb) => tb.Elements.First().Content, typeof(T)))
        {
            sb.Schema.EntityEvents<T>().PreUnsafeDelete += query =>
            {
                if (Schema.Current.IsAllowed(typeof(ToolbarMenuEntity), false) == null)
                    Database.MListQuery((ToolbarMenuEntity tb) => tb.Elements).Where(mle => query.Contains((T)mle.Element.Content!.Entity)).UnsafeDeleteMList();
                return null;
            };

            sb.Schema.Table<T>().PreDeleteSqlSync += arg =>
            {
                var entity = (T)arg;

                var parts = Administrator.UnsafeDeletePreCommandMList((ToolbarMenuEntity tb) => tb.Elements, Database.MListQuery((ToolbarMenuEntity tb) => tb.Elements)
                    .Where(mle => mle.Element.Content!.Entity == entity));

                return parts;
            };

            if (querySelectorForSync != null)
            {
                sb.Schema.Table<QueryEntity>().PreDeleteSqlSync += q =>
                {
                    var parts = Administrator.UnsafeDeletePreCommandMList((ToolbarMenuEntity te) => te.Elements, Database.MListQuery((ToolbarMenuEntity tb) => tb.Elements).Where(mle => querySelectorForSync.Evaluate((T)mle.Element.Content!.Entity).Is(q)));
                    return parts;
                };
            }
        }
    }

    public static void RegisterTranslatableRoutes()
    {
        TranslatedInstanceLogic.AddRoute((ToolbarEntity tb) => tb.Name);
        TranslatedInstanceLogic.AddRoute((ToolbarEntity tb) => tb.Elements[0].Label);
        TranslatedInstanceLogic.AddRoute((ToolbarMenuEntity tm) => tm.Name);
        TranslatedInstanceLogic.AddRoute((ToolbarMenuEntity tb) => tb.Elements[0].Label);
    }

    public static ToolbarEntity? GetCurrent(ToolbarLocation location)
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<ToolbarEntity>(userInterface: false);

        var result = Toolbars.Value.Values
            .Where(t => isAllowed(t) && t.Location == location)
            .OrderByDescending(a => a.Priority)
            .FirstOrDefault();

        return result;
    }

    public static ToolbarResponse? GetCurrentToolbarResponse(ToolbarLocation location)
    {
        var curr = GetCurrent(location);

        if (curr == null)
            return null;

        var responses = ToResponseList(TranslatedInstanceLogic.TranslatedMList(curr, c => c.Elements).ToList());

        if (responses.Count == 0)
            return null;

        return new ToolbarResponse
        {
            type = ToolbarElementType.Header,
            content = curr.ToLite(),
            label = TranslatedInstanceLogic.TranslatedField(curr, a => a.Name),
            elements = responses,
        };
    }

    private static List<ToolbarResponse> ToResponseList(List<TranslatableElement<ToolbarElementEmbedded>> elements)
    {
        var result = elements.SelectMany(a => ToResponse(a) ?? Enumerable.Empty<ToolbarResponse>()).NotNull().ToList();

        retry:
        var extraDividers = result.Where((a, i) => a.type == ToolbarElementType.Divider && (
            i == 0 ||
            result[i - 1].type == ToolbarElementType.Divider ||
            i == result.Count
        )).ToList();
        result.RemoveAll(extraDividers.Contains);
        var extraHeaders = result.Where((a, i) => IsPureHeader(a) && (
            i == result.Count - 1 ||
            IsPureHeader(result[i + 1]) || 
            result[i + 1].type == ToolbarElementType.Divider ||
            result[i + 1].type == ToolbarElementType.Header && result[i + 1].content is Lite<ToolbarMenuEntity>
        )).ToList();
        result.RemoveAll(extraHeaders.Contains);

        if (extraDividers.Any() || extraHeaders.Any())
            goto retry;

        return result;
    }

    private static bool IsPureHeader(ToolbarResponse tr)
    {
        return tr.type == ToolbarElementType.Header && tr.content == null && string.IsNullOrEmpty(tr.url);
    }

    private static IEnumerable<ToolbarResponse>? ToResponse(TranslatableElement<ToolbarElementEmbedded> transElement)
    {
        var element = transElement.Value;

        IContentConfig? config = null;
        if (element.Content != null)
        {
            config = ContentCondigDictionary.GetOrThrow(element.Content.EntityType);
            if (!config.IsAuhorized(element.Content))
                return null;

            var customResponse = config.CustomResponses(element.Content);
            if (customResponse != null)
                return customResponse;
        }

        var result = new ToolbarResponse
        {
            type = element.Type,
            content = element.Content,
            url = element.Url,
            label = transElement.TranslatedElement(a => a.Label!).DefaultText(null) ?? config?.DefaultLabel(element.Content!),
            iconName = element.IconName,
            iconColor = element.IconColor,
            showCount = element.ShowCount,
            autoRefreshPeriod = element.AutoRefreshPeriod,
            openInPopup = element.OpenInPopup,
        };

        if (element.Content is Lite<ToolbarMenuEntity>)
        {
            var tme = ToolbarMenus.Value.GetOrThrow((Lite<ToolbarMenuEntity>)element.Content);
            result.elements = ToResponseList(TranslatedInstanceLogic.TranslatedMList(tme, t => t.Elements).ToList());
            if (result.elements.Count == 0)
                return null;
        }

        if (element.Content is Lite<ToolbarEntity>)
        {
            var tme = Toolbars.Value.GetOrThrow((Lite<ToolbarEntity>)element.Content);
            var res = ToResponseList(TranslatedInstanceLogic.TranslatedMList(tme, t => t.Elements).ToList());
            if (res.Count == 0)
                return null;

            return res;
        }

        return new[] { result };
    }

    public static void RegisterContentConfig<T>(Func<Lite<T>, bool> isAuthorized, Func<Lite<T>, string> defaultLabel) 
        where T : Entity
    {
        ContentCondigDictionary.Add(typeof(T), new ContentConfig<T>(isAuthorized, defaultLabel));
    }

    public static ContentConfig<T> GetContentConfig<T>() where T: Entity
    {
        return (ContentConfig<T>)ContentCondigDictionary.GetOrThrow(typeof(T));
    }

    static Dictionary<Type, IContentConfig> ContentCondigDictionary = new Dictionary<Type, IContentConfig>();

    public interface IContentConfig
    {
        bool IsAuhorized(Lite<Entity> lite);
        string DefaultLabel(Lite<Entity> lite);

        List<ToolbarResponse>? CustomResponses(Lite<Entity> lite);
    }

    public class ContentConfig<T> : IContentConfig where T : Entity
    {
        public Func<Lite<T>, bool> IsAuthorized;
        public Func<Lite<T>, string> DefaultLabel;
        public Func<Lite<T>, List<ToolbarResponse>?>? CustomResponses;

        public ContentConfig(Func<Lite<T>, bool> isAuthorized, Func<Lite<T>, string> defaultLabel)
        {
            IsAuthorized = isAuthorized;
            DefaultLabel = defaultLabel;
        }

        bool IContentConfig.IsAuhorized(Lite<Entity> lite) => IsAuthorized((Lite<T>)lite);
        string IContentConfig.DefaultLabel(Lite<Entity> lite) => DefaultLabel((Lite<T>)lite);
        List<ToolbarResponse>? IContentConfig.CustomResponses(Lite<Entity> lite)
        {
            foreach (var item in CustomResponses.GetInvocationListTyped())
            {
                var resp = item.Invoke((Lite<T>)lite);
                if (resp != null)
                    return resp;
            }

            return null;
        }
    }

    static bool IsQueryAllowed(Lite<QueryEntity> query)
    {
        try
        {
            return QueryLogic.Queries.QueryAllowed(QueryLogic.QueryNames.GetOrThrow(query.ToString()!), true);
        }
        catch (Exception e) when (StartParameters.IgnoredDatabaseMismatches != null)
        {
            //Could happen when not 100% synchronized
            StartParameters.IgnoredDatabaseMismatches.Add(e);

            return false;
        }
    }

    static bool InMemoryFilter<T>(T entity) where T : Entity
    {
        if (Schema.Current.IsAllowed(typeof(T), inUserInterface: false) != null)
            return false;

        var isAllowed = Schema.Current.GetInMemoryFilter<T>(userInterface: false);
        return isAllowed(entity);
    }

}

public class ToolbarResponse
{
    public ToolbarElementType type;
    public string? label;
    public Lite<Entity>? content;
    public string? url;
    public List<ToolbarResponse>? elements;
    
    public string? iconName;
    public string? iconColor;
    public ShowCount? showCount;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? autoRefreshPeriod;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool openInPopup;

    public override string ToString() => $"{type} {label} {content} {url}";
}
