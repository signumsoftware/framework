using Signum.Authorization;
using Signum.Authorization.Rules;
using Signum.UserAssets;
using Signum.Utilities.DataStructures;
using System.Collections.Frozen;
using System.Text.Json.Serialization;

namespace Signum.Toolbar;

public static class ToolbarLogic
{
    public static ResetLazy<FrozenDictionary<Lite<ToolbarEntity>, ToolbarEntity>> Toolbars = null!;
    public static ResetLazy<FrozenDictionary<Lite<ToolbarMenuEntity>, ToolbarMenuEntity>> ToolbarMenus = null!;
    public static ResetLazy<FrozenDictionary<Lite<ToolbarSwitcherEntity>, ToolbarSwitcherEntity>> ToolbarSwitchers = null!;

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
            sb.Schema.EntityEvents<ToolbarSwitcherEntity>().Saving += IToolbar_Saving;

            sb.Include<ToolbarMenuEntity>()
                .WithSave(ToolbarMenuOperation.Save)
                .WithDelete(ToolbarMenuOperation.Delete)
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Name
                });


            sb.Include<ToolbarSwitcherEntity>()
                  .WithSave(ToolbarSwitcherOperation.Save)
                  .WithDelete(ToolbarSwitcherOperation.Delete)
                  .WithQuery(() => e => new
                  {
                      Entity = e,
                      e.Id,
                      e.Name
                  });
             
            sb.Schema.Settings.AssertImplementedBy((ToolbarEntity t) => t.Elements.First().Content, typeof(QueryEntity));
            sb.Schema.Settings.AssertImplementedBy((ToolbarEntity t) => t.Elements.First().Content, typeof(PermissionSymbol));
            sb.Schema.Settings.AssertImplementedBy((ToolbarEntity t) => t.Elements.First().Content, typeof(ToolbarEntity));
            sb.Schema.Settings.AssertImplementedBy((ToolbarEntity t) => t.Elements.First().Content, typeof(ToolbarMenuEntity));
            sb.Schema.Settings.AssertImplementedBy((ToolbarEntity t) => t.Elements.First().Content, typeof(ToolbarSwitcherEntity));

            AuthLogic.HasRuleOverridesEvent += role =>
                Database.Query<ToolbarEntity>().Any(a => a.Owner.Is(role)) ||
                Database.Query<ToolbarMenuEntity>().Any(a => a.Owner.Is(role));

            UserAssetsImporter.Register("Toolbar", ToolbarOperation.Save);
            UserAssetsImporter.Register("ToolbarMenu", ToolbarMenuOperation.Save);
            UserAssetsImporter.Register("ToolbarSwitcher", ToolbarSwitcherOperation.Save);

            Toolbars = sb.GlobalLazy(() => Database.Query<ToolbarEntity>().ToFrozenDictionaryEx(a => a.ToLite()),
                new InvalidateWith(typeof(ToolbarEntity)));

            ToolbarMenus = sb.GlobalLazy(() => Database.Query<ToolbarMenuEntity>().ToFrozenDictionaryEx(a => a.ToLite()),
               new InvalidateWith(typeof(ToolbarMenuEntity)));

            ToolbarSwitchers = sb.GlobalLazy(() => Database.Query<ToolbarSwitcherEntity>().ToFrozenDictionaryEx(a => a.ToLite()),
                new InvalidateWith(typeof(ToolbarSwitcherEntity)));

            RegisterDelete<QueryEntity>(sb);
            RegisterDelete<ToolbarMenuEntity>(sb);
            RegisterDelete<ToolbarSwitcherEntity>(sb);

            RegisterContentConfig<ToolbarMenuEntity>(
                lite =>
                {
                    var entity = ToolbarMenus.Value.GetOrCreate(lite);
                    return entity.IsAllowedFor(TypeAllowedBasic.Read, inUserInterface: false, FilterQueryArgs.FromEntity(entity));
                },
                lite => PropertyRouteTranslationLogic.TranslatedField(ToolbarMenus.Value.GetOrCreate(lite), a => a.Name));

            RegisterContentConfig<ToolbarSwitcherEntity>(
               lite =>
               {
                   var entity = ToolbarSwitchers.Value.GetOrCreate(lite);
                   return entity.IsAllowedFor(TypeAllowedBasic.Read, inUserInterface: false, FilterQueryArgs.FromEntity(entity));
               },
             lite => PropertyRouteTranslationLogic.TranslatedField(ToolbarSwitchers.Value.GetOrCreate(lite), a => a.Name));


            RegisterContentConfig<ToolbarEntity>(
                lite =>
                {
                    ToolbarEntity entity = Toolbars.Value.GetOrCreate(lite);
                    return entity.IsAllowedFor(TypeAllowedBasic.Read, inUserInterface: false, FilterQueryArgs.FromEntity(entity));
                },
                lite => PropertyRouteTranslationLogic.TranslatedField(Toolbars.Value.GetOrCreate(lite), a => a.Name));
            
            RegisterContentConfig<QueryEntity>(
              lite => IsQueryAllowed(lite),
              lite => QueryUtils.GetNiceName(QueryLogic.QueryNames.GetOrThrow(lite.ToString()!)));

            RegisterContentConfig<PermissionSymbol>(
                lite => PermissionAuthLogic.IsAuthorized(SymbolLogic<PermissionSymbol>.ToSymbol(lite.ToString()!)),
                lite => SymbolLogic<PermissionSymbol>.ToSymbol(lite.ToString()!).NiceToString());

            GetContentConfig<PermissionSymbol>().CustomResponses = lite =>
            {
                var action = CustomPermissionResponse.TryGetC(lite.Retrieve());

                if (action != null)
                    return action();

                return null;
            };

            //    { typeof(QueryEntity), a => IsQueryAllowed((Lite<QueryEntity>)a) },
            //{ typeof(PermissionSymbol), a => PermissionAuthLogic.IsAuthorized((PermissionSymbol)a.RetrieveAndRemember()) },
            //{ typeof(UserQueryEntity), a => ,
            //{ typeof(UserChartEntity), a => { var uc = UserChartLogic.UserCharts.Value.GetOrCreate((Lite<UserChartEntity>)a); return InMemoryFilter(uc) && QueryLogic.Queries.QueryAllowed(uc.Query.ToQueryName(), true); } },
            //{ typeof(DashboardEntity), a => InMemoryFilter(DashboardLogic.Dashboards.Value.GetOrCreate((Lite<DashboardEntity>)a)) },
            //{ typeof(WorkflowEntity), a => { var wf = WorkflowLogic.WorkflowGraphLazy.Value.GetOrCreate((Lite<WorkflowEntity>)a); return InMemoryFilter(wf.Workflow) && wf.IsStartCurrentUser(); } },



        }
    }



    private static void IToolbar_Saving(IToolbarEntity tool)
    {
        var elements = 
            tool is ToolbarEntity tb ? tb.Elements :
            tool is ToolbarMenuEntity tbm ? tbm.Elements.Cast<ToolbarElementEmbedded>() :
            null;

        if (elements != null)
        {
            if (elements.FirstOrDefault()?.Type == ToolbarElementType.ExtraIcon)
                throw new InvalidOperationException(ToolbarMessage.FirstElementCanNotBeExtraIcon.NiceToString());

            if (elements.GroupWhen(e => e.Type != ToolbarElementType.ExtraIcon).Any(gr => gr.Count() > 0 && gr.Key.Type == ToolbarElementType.Divider))
                throw new InvalidOperationException(ToolbarMessage.ExtraIconCanNotComeAfterDivider.NiceToString());
        }

        if (!tool.IsNew)
        {
            using (new EntityCache(EntityCacheType.ForceNew))
            {
                EntityCache.AddFullGraph((Entity)tool);

                var toolbarGraph = DirectedGraph<IToolbarEntity>.Generate(tool, t => t.GetSubToolbars().Select(t => t!.Retrieve()));

                var problems = toolbarGraph.FeedbackEdgeSet().Edges.ToList();

                if (problems.Count > 0)
                    throw new ApplicationException(
                        ToolbarMessage._0CyclesHaveBeenFoundInTheToolbarDueToTheRelationships.NiceToString().FormatWith(problems.Count) +
                        problems.ToString("\n"));
            }
        }
    }

    public static void RegisterUserTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition) =>
        RegisterTypeCondition(sb, typeCondition,typeof(UserEntity), owner => owner.Is(UserEntity.Current));

    public static void RegisterRoleTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition) =>
        RegisterTypeCondition(sb, typeCondition, typeof(RoleEntity), owner => owner == null || AuthLogic.CurrentRoles().Contains(owner));

    public static void RegisterTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition, Type ownerType, Expression<Func<Lite<IEntity>?, bool>> isAllowed)
    {
        sb.Schema.Settings.AssertImplementedBy((ToolbarEntity t) => t.Owner, ownerType);

        TypeConditionLogic.RegisterCompile<ToolbarEntity>(typeCondition, t => isAllowed.Evaluate(t.Owner));

        sb.Schema.Settings.AssertImplementedBy((ToolbarMenuEntity t) => t.Owner, ownerType);

        TypeConditionLogic.RegisterCompile<ToolbarMenuEntity>(typeCondition, t => isAllowed.Evaluate(t.Owner));

        sb.Schema.Settings.AssertImplementedBy((ToolbarSwitcherEntity t) => t.Owner, ownerType);

        TypeConditionLogic.RegisterCompile<ToolbarSwitcherEntity>(typeCondition, t => isAllowed.Evaluate(t.Owner));
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

            sb.Schema.EntityEvents<T>().PreDeleteSqlSync += entity =>
            {
                var parts = Administrator.UnsafeDeletePreCommandMList((ToolbarEntity tb) => tb.Elements, Database.MListQuery((ToolbarEntity tb) => tb.Elements)
                    .Where(mle => mle.Element.Content!.Entity == entity));

                return parts;
            };

            if(querySelectorForSync != null)
            {
                sb.Schema.EntityEvents<QueryEntity>().PreDeleteSqlSync += q =>
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

            sb.Schema.EntityEvents<T>().PreDeleteSqlSync += entity =>
            {
                var parts = Administrator.UnsafeDeletePreCommandMList((ToolbarMenuEntity tb) => tb.Elements, Database.MListQuery((ToolbarMenuEntity tb) => tb.Elements)
                    .Where(mle => mle.Element.Content!.Entity == entity));

                return parts;
            };

            if (querySelectorForSync != null)
            {
                sb.Schema.EntityEvents<QueryEntity>().PreDeleteSqlSync += q =>
                {
                    var parts = Administrator.UnsafeDeletePreCommandMList((ToolbarMenuEntity te) => te.Elements, Database.MListQuery((ToolbarMenuEntity tb) => tb.Elements).Where(mle => querySelectorForSync.Evaluate((T)mle.Element.Content!.Entity).Is(q)));
                    return parts;
                };
            }
        }

        if (sb.Settings.ImplementedBy((ToolbarSwitcherEntity tb) => tb.Options.First().ToolbarMenu, typeof(T)))
        {
            sb.Schema.EntityEvents<T>().PreUnsafeDelete += query =>
            {
                if (Schema.Current.IsAllowed(typeof(ToolbarSwitcherEntity), false) == null)
                    Database.MListQuery((ToolbarSwitcherEntity tb) => tb.Options).Where(mle => query.Contains((T)(Entity)mle.Element.ToolbarMenu!.Entity)).UnsafeDeleteMList();
                return null;
            };
        }
    }

    public static void RegisterTranslatableRoutes()
    {
        PropertyRouteTranslationLogic.RegisterRoute((ToolbarEntity tb) => tb.Name);
        PropertyRouteTranslationLogic.RegisterRoute((ToolbarEntity tb) => tb.Elements[0].Label);
        PropertyRouteTranslationLogic.RegisterRoute((ToolbarMenuEntity tm) => tm.Name);
        PropertyRouteTranslationLogic.RegisterRoute((ToolbarMenuEntity tb) => tb.Elements[0].Label);
        PropertyRouteTranslationLogic.RegisterRoute((ToolbarSwitcherEntity tm) => tm.Name);

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

        var responses = ToResponseList(PropertyRouteTranslationLogic.TranslatedMList(curr, c => c.Elements).ToList());

        if (responses.Count == 0)
            return null;

        return new ToolbarResponse
        {
            type = ToolbarElementType.Header,
            content = curr.ToLite(),
            label = PropertyRouteTranslationLogic.TranslatedField(curr, a => a.Name),
            elements = responses,
        };
    }

    private static List<ToolbarResponse> ToResponseList<TE>(List<TranslatableElement<TE>> elements)
        where TE : ToolbarElementEmbedded
    {
        var result = elements.GroupWhen(a=>a.Value.Type != ToolbarElementType.ExtraIcon, BeforeFirstKey.Skip).SelectMany(gr => ToResponse(gr) ?? Enumerable.Empty<ToolbarResponse>()).NotNull().ToList();

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

    private static IEnumerable<ToolbarResponse>? ToResponse<T>(IGrouping<TranslatableElement<T>, TranslatableElement<T>> gr)
        where T: ToolbarElementEmbedded
    {
        var transElement = gr.Key;
        var element = gr.Key.Value;

        IContentConfig? config = null;
        if (element.Content != null)
        {
            config = ContentConfigDictionary.GetOrThrow(element.Content.EntityType);
            if (!config.IsAuhorized(element.Content))
                return null;

            var customResponse = config.CustomResponses(element.Content);
            if (customResponse != null)
                return customResponse;
        }

        if (element.Content is Lite<ToolbarEntity>)
        {
            var tme = Toolbars.Value.GetOrThrow((Lite<ToolbarEntity>)element.Content);
            var res = ToResponseList(PropertyRouteTranslationLogic.TranslatedMList(tme, t => t.Elements).ToList());
            if (res.Count == 0)
                return null;

            return res;
        }

        var result = new ToolbarResponse
        {
            type = element.Type,
            content = element.Content,
            url = element.Url,
            label = transElement.TranslatedElement(a => a.Label!).DefaultToNull() ?? config?.DefaultLabel(element.Content!),
            iconName = element.IconName.DefaultToNull() ?? config?.DefaultIconName(element.Content!),
            iconColor = element.IconColor.DefaultToNull() ?? config?.DefaultIconColor(element.Content!),
            showCount = element.ShowCount,
            autoRefreshPeriod = element.AutoRefreshPeriod,
            openInPopup = element.OpenInPopup,
            autoSelect = (element as ToolbarMenuElementEmbedded)?.AutoSelect == true,
            withEntity = (element as ToolbarMenuElementEmbedded)?.WithEntity == true,
            extraIcons = gr.IsEmpty() ? null : gr.Select(extra =>
            {
                var extraElement = extra.Value;
                IContentConfig? config = null;
                if (extraElement.Content != null)
                {
                    config = ContentConfigDictionary.GetOrThrow(extraElement.Content.EntityType);
                    if (!config.IsAuhorized(extraElement.Content))
                        return null;
                }

                if (extraElement.Content is Lite<ToolbarEntity>)
                {
                    return null;
                }

                return new ToolbarExtraIcon
                {
                    type = extraElement.Type,
                    content = extraElement.Content,
                    url = extraElement.Url,
                    label = transElement.TranslatedElement(a => a.Label!).DefaultToNull() ?? config?.DefaultLabel(extraElement.Content!),
                    iconName = extraElement.IconName.DefaultToNull() ?? config?.DefaultIconName(extraElement.Content!),
                    iconColor = extraElement.IconColor.DefaultToNull() ?? config?.DefaultIconColor(extraElement.Content!),
                    showCount = extraElement.ShowCount,
                    autoRefreshPeriod = extraElement.AutoRefreshPeriod,
                    openInPopup = extraElement.OpenInPopup,
                };
            }).NotNull().ToList()
        };

        if (element.Content is Lite<ToolbarMenuEntity> tm)
        {
            var tme = ToolbarMenus.Value.GetOrThrow(tm);
            result.entityType = GetEntityType(tm);
            result.elements = ToResponseList(PropertyRouteTranslationLogic.TranslatedMList(tme, t => t.Elements).ToList());
            if (result.elements.Count == 0)
                return null;
        }

        if (element.Content is Lite<ToolbarSwitcherEntity> ts)
        {
            var tme = ToolbarSwitchers.Value.GetOrThrow(ts);

            result.elements = tme.Options.Select(o => 
            {
                var tm = ToolbarMenus.Value.GetOrThrow(o.ToolbarMenu);
                var subElements = ToResponseList(PropertyRouteTranslationLogic.TranslatedMList(tm, t => t.Elements).ToList());

                if (subElements.IsEmpty())
                    return null;

                return new ToolbarResponse
                {
                    type = ToolbarElementType.Item,
                    content = o.ToolbarMenu,
                    entityType = GetEntityType(o.ToolbarMenu),
                    elements = subElements,
                    iconColor = o.IconColor,
                    iconName = o.IconName,
                    label = tm.TranslatedField(a => a.Name),
                };
            }).NotNull().ToList();

            if (result.elements.Count == 0)
                return null;
        }

        return [result];
    }

    private static string? GetEntityType(Lite<ToolbarMenuEntity> toolbarMenu)
    {
        var tm = ToolbarMenus.Value.GetOrThrow(toolbarMenu);

        if(tm.EntityType == null)
            return null;

        var type = TypeLogic.LiteToType.GetOrThrow(tm.EntityType);

        return TypeLogic.GetCleanName(type);
    }

    public static void RegisterContentConfig<T>(Func<Lite<T>, bool> isAuthorized, Func<Lite<T>, string> defaultLabel, Func<Lite<T>, string?>? defaultIconName = null, Func<Lite<T>, string?>? defaultIconColor = null) 
        where T : Entity
    {
        ContentConfigDictionary.Add(typeof(T), new ContentConfig<T>(isAuthorized, defaultLabel, defaultIconName, defaultIconColor));
    }

    public static ContentConfig<T> GetContentConfig<T>() where T: Entity
    {
        return (ContentConfig<T>)ContentConfigDictionary.GetOrThrow(typeof(T));
    }

    static Dictionary<Type, IContentConfig> ContentConfigDictionary = new Dictionary<Type, IContentConfig>();

    public interface IContentConfig
    {
        bool IsAuhorized(Lite<Entity> lite);
        string DefaultLabel(Lite<Entity> lite);
        string? DefaultIconName(Lite<Entity> lite);
        string? DefaultIconColor(Lite<Entity> lite);

        List<ToolbarResponse>? CustomResponses(Lite<Entity> lite);
    }

    public class ContentConfig<T> : IContentConfig where T : Entity
    {
        public Func<Lite<T>, bool> IsAuthorized;
        public Func<Lite<T>, string> DefaultLabel;
        public Func<Lite<T>, string?>? DefaultIconName;
        public Func<Lite<T>, string?>? DefaultIconColor;
        public Func<Lite<T>, List<ToolbarResponse>?>? CustomResponses;

        public ContentConfig(Func<Lite<T>, bool> isAuthorized, Func<Lite<T>, string> defaultLabel, Func<Lite<T>, string?>? defaultIconName = null, Func<Lite<T>, string?>? defaultIconColor = null)
        {
            IsAuthorized = isAuthorized;
            DefaultLabel = defaultLabel;
            DefaultIconName = defaultIconName;
            DefaultIconColor = defaultIconColor;
        }

        bool IContentConfig.IsAuhorized(Lite<Entity> lite) => IsAuthorized((Lite<T>)lite);
        string IContentConfig.DefaultLabel(Lite<Entity> lite) => DefaultLabel((Lite<T>)lite);
        string? IContentConfig.DefaultIconName(Lite<Entity> lite) => DefaultIconName?.Invoke((Lite<T>)lite);
        string? IContentConfig.DefaultIconColor(Lite<Entity> lite) => DefaultIconColor?.Invoke((Lite<T>)lite);
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

    public static bool InMemoryFilter<T>(T entity) where T : Entity
    {
        if (Schema.Current.IsAllowed(typeof(T), inUserInterface: false) != null)
            return false;

        var isAllowed = Schema.Current.GetInMemoryFilter<T>(userInterface: false);
        return isAllowed(entity);
    }

}

public class ToolbarResponse : ToolbarResponseBase
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ToolbarResponse>? elements;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ToolbarExtraIcon>? extraIcons;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? entityType;

    public override string ToString() => $"{type} {label} {content} {url}";
}

public class ToolbarExtraIcon : ToolbarResponseBase
{

}

public class ToolbarResponseBase
{
    public ToolbarElementType type;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? label;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Lite<Entity>? content;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? url;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? iconName;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? iconColor;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ShowCount? showCount;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? autoRefreshPeriod;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool openInPopup;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool autoSelect;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool withEntity;


    public override string ToString() => $"{type} {label} {content} {url}";

}
