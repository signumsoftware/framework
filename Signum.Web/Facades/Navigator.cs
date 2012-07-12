#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Utilities;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using System.Reflection;
using Signum.Utilities.Reflection;
using System.Collections.Specialized;
using Signum.Web.Properties;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Engine.DynamicQuery;
using System.Configuration;
using Signum.Utilities.ExpressionTrees;
using Signum.Web.Controllers;
using System.Web.Hosting;
using System.Web.Compilation;
using Signum.Web.PortableAreas;
#endregion

namespace Signum.Web
{
    public static class Navigator
    {
        public static NavigationManager Manager;

        public static void Start(NavigationManager manager)
        {
            Manager = manager;
        }

        public const string ViewRouteName = "sfView";

        public static string ViewRoute(Type type, int? id)
        {
            var entitySettings = EntitySettings(type);
            if (entitySettings.ViewRoute != null)
                return entitySettings.ViewRoute(new UrlHelper(HttpContext.Current.Request.RequestContext), type, id);

            return new UrlHelper(HttpContext.Current.Request.RequestContext).RouteUrl(ViewRouteName, new
            {
                webTypeName = EntitySettings(type).WebTypeName,
                id = id.TryToString()
            });
        }

        public static string ViewRoute(IdentifiableEntity ie)
        {
            return ViewRoute(ie.GetType(), ie.Id);
        }

        public static string ViewRoute(Lite lite)
        {
            return ViewRoute(lite.RuntimeType, lite.Id);
        }

        public static RedirectResult RedirectToEntity(IdentifiableEntity ie)
        {
            return new RedirectResult(ViewRoute(ie));
        }

        public static RedirectResult RedirectToEntity(Lite lite)
        {
            return new RedirectResult(ViewRoute(lite));
        }

        public const string FindRouteName = "sfFind";

        public static string FindRoute(object queryName)
        {
            return new UrlHelper(HttpContext.Current.Request.RequestContext).RouteUrl(FindRouteName, new
            {
                webQueryName = ResolveWebQueryName(queryName)
            });
        }

        public static ViewResult View(ControllerBase controller, IRootEntity entity)
        {
            return Manager.View(controller, entity, null); 
        }

        public static ViewResult View(ControllerBase controller, IRootEntity entity, string partialViewName)
        {
            return Manager.View(controller, entity, partialViewName);
        }
     
        public static PartialViewResult NormalControl(ControllerBase controller, IRootEntity entity)
        {
            return Manager.NormalControl(controller, entity, null); 
        }
    
        public static PartialViewResult NormalControl(ControllerBase controller, IRootEntity entity, string partialViewName)
        {
            return Manager.NormalControl(controller, entity, partialViewName);
        }

        public static string GetOrCreateTabID(ControllerBase c)
        {
            return Manager.GetOrCreateTabID(c);
        }

        public static string TabID(this ControllerBase controller)
        {
            NameValueCollection form = controller.ControllerContext.HttpContext.Request.Form;

            string tabID;
            if (!form.AllKeys.Contains(ViewDataKeys.TabId) || !(tabID = (string)form[ViewDataKeys.TabId]).HasText())
                throw new InvalidOperationException("The Request doesn't have the necessary tab identifier");           
            
            return tabID;
        }

        public static PartialViewResult PopupOpen(this ControllerBase controller, ViewOptionsBase viewOptions)
        {
            return Manager.PopupOpen(controller, viewOptions);
        }

        public static PartialViewResult PartialView(this ControllerBase controller, TypeContext tc)
        {
            return Manager.PartialView(controller, tc, null);
        }

        public static PartialViewResult PartialView(this ControllerBase controller, TypeContext tc, string partialViewName)
        {
            return Manager.PartialView(controller, tc, partialViewName);
        }

        public static PartialViewResult PartialView(this ControllerBase controller, IRootEntity entity, string prefix)
        {
            TypeContext tc = TypeContextUtilities.UntypedNew(entity, prefix);
            return Manager.PartialView(controller, tc, null);
        }

        public static PartialViewResult PartialView(this ControllerBase controller, IRootEntity entity, string prefix, string partialViewName)
        {
            TypeContext tc = TypeContextUtilities.UntypedNew(entity, prefix);
            return Manager.PartialView(controller, tc, partialViewName);
        }

        public static ViewResult Find(ControllerBase controller, object queryName)
        {
            return Find(controller, new FindOptions(queryName));
        }

        public static ViewResult Find(ControllerBase controller, FindOptions findOptions)
        {
            return Manager.Find(controller, findOptions);
        }

        public static PartialViewResult PartialFind(ControllerBase controller, FindOptions findOptions, Context context)
        {
            return Manager.PartialFind(controller, findOptions, context);
        }

        public static PartialViewResult PartialFind(ControllerBase controller, FindOptions findOptions, string prefix)
        {
            return Manager.PartialFind(controller, findOptions, new Context(null, prefix));
        }

        public static Lite FindUnique(FindUniqueOptions options)
        {
            return Manager.FindUnique(options);
        }

        public static int QueryCount(CountOptions options)
        {
            return Manager.QueryCount(options);
        }

        public static PartialViewResult Search(ControllerBase controller, QueryRequest request, bool? allowMultiple, bool view, FilterMode filterMode, string prefix)
        {
            return Manager.Search(controller, request, allowMultiple, view, filterMode, new Context(null, prefix));
        }

        public static string SearchTitle(object queryName)
        {
            return Manager.SearchTitle(queryName);
        }

        public static void SetTokens(object queryName, List<FilterOption> filters)
        {
            Manager.SetTokens(queryName, filters);
        }

        public static void SetTokens(object queryName, IEnumerable<OrderOption> orders)
        {
            Manager.SetTokens(queryName, orders);
        }

        public static SortedList<string, string> ToSortedList(this NameValueCollection form, string prefix)
        {
            SortedList<string, string> formValues = new SortedList<string, string>(form.Count);
            foreach (string key in form.Keys)
            {
                if (key.HasText() && (string.IsNullOrEmpty(prefix) || key.StartsWith(prefix)))
                    formValues.Add(key, form[key]);
            }
            
            return formValues;
        }

        public static SortedList<string, string> ToSortedList(this NameValueCollection form)
        {
            return form.ToSortedList(null);
        }

        public static void AddSetting(EntitySettings settings)
        {
            Navigator.Manager.EntitySettings.AddOrThrow(settings.StaticType, settings, "EntitySettings for {0} allready registered");
        }

        public static void AddSettings(List<EntitySettings> settings)
        {
            Navigator.Manager.EntitySettings.AddRange(settings, s => s.StaticType, s => s, "EntitySettings");
        }

        public static EntitySettings<T> EntitySettings<T>() where T : IdentifiableEntity
        {
            return (EntitySettings<T>)EntitySettings(typeof(T));
        }

        public static EmbeddedEntitySettings<T> EmbeddedEntitySettings<T>() where T : EmbeddedEntity
        {
            return (EmbeddedEntitySettings<T>)EntitySettings(typeof(T));
        }

        public static EntitySettings EntitySettings(Type type)
        {
            return Manager.EntitySettings.GetOrThrow(type, "No EntitySettings for type {0} found");
        } 

        public static void AddQuerySettings(List<QuerySettings> settings)
        {
            Navigator.Manager.QuerySettings.AddRange(settings, s => s.QueryName, s => s, "QuerySettings");
        }

        public static void AddQuerySetting(QuerySettings setting)
        {
            Navigator.Manager.QuerySettings.AddOrThrow(setting.QueryName, setting, "QuerySettings {0} repeated");
        }

        public static QuerySettings QuerySettings(object queryName)
        {
            return Manager.QuerySettings.GetOrThrow(queryName, "no QuerySettings for queryName {0} found");
        }

        public static MappingContext UntypedApplyChanges(this ModifiableEntity entity, ControllerContext controllerContext, string prefix, bool admin)
        {
            return miApplyChanges.GetInvoker(entity.GetType()).Invoke(entity, controllerContext, prefix, admin);
        }

        static GenericInvoker<Func<ModifiableEntity, ControllerContext, string, bool, MappingContext>> miApplyChanges =
            new GenericInvoker<Func<ModifiableEntity, ControllerContext, string, bool, MappingContext>>((me, cc, pr, ad) => ApplyChanges<TypeDN>((TypeDN)me, cc, pr, ad));
        public static MappingContext<T> ApplyChanges<T>(this T entity, ControllerContext controllerContext, string prefix, bool admin) where T : IRootEntity
        {
            SortedList<string, string> inputs = controllerContext.HttpContext.Request.Form.ToSortedList(prefix);
            Mapping<T> mapping = (Mapping<T>)Navigator.EntitySettings(typeof(T)).Map(s => admin ? s.UntypedMappingAdmin : s.UntypedMappingDefault);

            return Manager.ApplyChanges<T>(controllerContext, entity, prefix, mapping, inputs);
        }

        public static MappingContext<T> ApplyChanges<T>(this T entity, ControllerContext controllerContext, string prefix, bool admin, SortedList<string, string> inputs) where T : IRootEntity
        {
            Mapping<T> mapping = (Mapping<T>)Navigator.EntitySettings(typeof(T)).Map(s => admin ? s.UntypedMappingAdmin : s.UntypedMappingDefault);

            return Manager.ApplyChanges<T>(controllerContext, entity, prefix, mapping, inputs);
        }

        public static MappingContext<T> ApplyChanges<T>(this T entity, ControllerContext controllerContext, string prefix, Mapping<T> mapping) where T : IRootEntity
        {
            SortedList<string, string> inputs = controllerContext.HttpContext.Request.Form.ToSortedList(prefix);

            return Manager.ApplyChanges<T>(controllerContext, entity, prefix, mapping, inputs);
        }

        public static MappingContext<T> ApplyChanges<T>(this T entity, ControllerContext controllerContext, string prefix, Mapping<T> mapping, SortedList<string, string> inputs) where T : IRootEntity
        {
            return Manager.ApplyChanges<T>(controllerContext, entity, prefix, mapping, inputs);
        }

        public static ModifiableEntity UntypedExtractEntity(this ControllerBase controller)
        {
            return Manager.ExtractEntity(controller, null);
        }

        public static ModifiableEntity UntypedExtractEntity(this ControllerBase controller, string prefix)
        {
            return Manager.ExtractEntity(controller, prefix);
        }

        public static T ExtractEntity<T>(this ControllerBase controller) where T: ModifiableEntity
        {
            return (T)Manager.ExtractEntity(controller, null);
        }

        public static T ExtractEntity<T>(this ControllerBase controller, string prefix) where T : ModifiableEntity
        {
            return (T) Manager.ExtractEntity(controller, prefix);
        }

        public static Lite<T> ExtractLite<T>(this ControllerBase controller, string prefix) where T : class, IIdentifiable
        {
            return (Lite<T>)Manager.ExtractLite<T>(controller, prefix);
        }

        public static List<Lite<T>> ExtractLitesList<T>(string commaSeparatedIds, bool retrieve) where T : IdentifiableEntity
        {
            if (!commaSeparatedIds.HasText())
                return new List<Lite<T>>();

            var ids = commaSeparatedIds.Split(new[]{','},StringSplitOptions.RemoveEmptyEntries );
            if (retrieve)
                return Database.RetrieveListLite<T>(ids.Select(i => int.Parse(i)).ToList());

            else
                return ids.Select(i => new Lite<T>(int.Parse(i))).ToList();
        }

        public static string ResolveWebTypeName(Type type)
        {
            return Manager.ResolveWebTypeName(type);
        }

        public static Type ResolveType(string webTypeName)
        {
            return Manager.ResolveType(webTypeName);
        }

        public static string ResolveWebQueryName(object queryName)
        {
            return Manager.ResolveWebQueryName(queryName);
        }

        public static object ResolveQueryName(string webQueryName)
        {
            return Manager.ResolveQueryName(webQueryName);
        }

        public static bool IsViewable(Type type, EntitySettingsContext ctx)
        {
            return Manager.IsViewable(type, ctx);
        }
        public static bool IsViewable(ModifiableEntity entity, EntitySettingsContext ctx)
        {
            return Manager.IsViewable(entity, ctx);
        }

        public static bool IsReadOnly(Type type, EntitySettingsContext ctx)
        {
            return Manager.IsReadOnly(type, ctx);
        }
        public static bool IsReadOnly(ModifiableEntity entity, EntitySettingsContext ctx)
        {
            return Manager.IsReadOnly(entity, ctx);
        }

        public static bool IsCreable(Type type, EntitySettingsContext ctx)
        {
            return Manager.IsCreable(type, ctx);
        }

        public static bool IsFindable(object queryName)
        {
            return Manager.IsFindable(queryName);
        }

        public static bool IsFindable(this FindOptions options)
        {
            return Manager.IsFindable(options.QueryName);
        }

        public static string OnPartialViewName(ModifiableEntity entity)
        {
            return EntitySettings(entity.GetType()).OnPartialViewName(entity); 
        }

        public static void RegisterArea(Type clientType)
        {
            if (!clientType.Name.EndsWith("Client"))
                throw new InvalidOperationException("The name of clientType should end with the convention 'Client'");

            RegisterArea(clientType, clientType.Name.RemoveRight("Client".Length));
        }

        public static void RegisterArea(Type clientType, string areaName)
        {
            if (areaName.Left(1) == "/")
                throw new SystemException("Invalid start character / in {0}".Formato(areaName));

            CompiledViews.RegisterArea(clientType.Assembly, areaName);
            SignumControllerFactory.RegisterControllersLike(clientType, areaName);

            EmbeddedFilesRepository rep = new EmbeddedFilesRepository(clientType.Assembly, areaName);
            if (!rep.IsEmpty)
                FileRepositoryManager.Register(rep);
        }

        public static void Initialize()
        {
            Manager.Initialize();
        }

        public static List<Lite<T>> ParseLiteKeys<T>(string commaSeparatedLites) where T : class, IIdentifiable
        {
            return commaSeparatedLites.Split(',').Select(Lite.Parse<T>).ToList();
        }

        public static List<Lite<T>> ParseLiteIds<T>(string commaSeparatedLites) where T : IdentifiableEntity
        {
            return commaSeparatedLites.Split(',').Select(str => new Lite<T>(int.Parse(str))).ToList();
        }
    }
    
    public class NavigationManager
    {
        public Dictionary<Type, EntitySettings> EntitySettings {get;set;}
        public Dictionary<object, QuerySettings> QuerySettings {get;set;}

        public static string ViewPrefix = "~/signum/Views/{0}.cshtml";

        public string NormalPageView = ViewPrefix.Formato("NormalPage");
        public string NormalControlView = ViewPrefix.Formato("NormalControl");
        public string PopupControlView = ViewPrefix.Formato("PopupControl");
        public string PopupOkControlView = ViewPrefix.Formato("PopupOkControl");
        public string ChooserPopupView = ViewPrefix.Formato("ChooserPopup");
        public string SearchPopupControlView = ViewPrefix.Formato("SearchPopupControl");
        public string SearchPageView = ViewPrefix.Formato("SearchPage");
        public string SearchControlView = ViewPrefix.Formato("SearchControl");
        public string SearchResultsView = ViewPrefix.Formato("SearchResults");
        public string FilterBuilderView = ViewPrefix.Formato("FilterBuilder");
        public string PaginationView = ViewPrefix.Formato("Pagination");
        public string ValueLineBoxView = ViewPrefix.Formato("ValueLineBox");
        
        protected Dictionary<string, Type> WebTypeNames { get; private set; }
        protected Dictionary<string, object> WebQueryNames { get; private set; }

        public Func<bool> AllowChangeColumns = () => true;

        static readonly List<string> defaultScripts = new List<string>
        {
            "~/signum/Scripts/SF_Globals.js",
            "~/signum/Scripts/SF_Popup.js",
            "~/signum/Scripts/SF_Lines.js",
            "~/signum/Scripts/SF_ViewNavigator.js",
            "~/signum/Scripts/SF_FindNavigator.js",
            "~/signum/Scripts/SF_Validator.js",
            "~/signum/Scripts/SF_Widgets.js"
        };
        public Func<List<string>> DefaultScripts = () => defaultScripts;

        public NavigationManager()
        {
            EntitySettings = new Dictionary<Type, EntitySettings>();
            QuerySettings = new Dictionary<object, QuerySettings>();
        }

        
        public event Action Initializing;
        public bool Initialized { get; private set; }
        internal void Initialize()
        {
            if (!Initialized)
            {
                Navigator.AddSetting(new EmbeddedEntitySettings<ValueLineBoxModel> { PartialViewName = _ => ValueLineBoxView });

                foreach (var es in EntitySettings.Values)
                {
                    if (string.IsNullOrEmpty(es.WebTypeName) && !es.StaticType.IsEmbeddedEntity())
                        es.WebTypeName = TypeLogic.TypeToName.TryGetC(es.StaticType) ?? Reflector.CleanTypeName(es.StaticType);
                }

                WebTypeNames = EntitySettings.Values.Where(es => es.WebTypeName.HasText())
                    .ToDictionary(es => es.WebTypeName, es => es.StaticType, StringComparer.InvariantCultureIgnoreCase, "WebTypeNames");

                if (DynamicQueryManager.Current != null)
                {
                    foreach (object o in DynamicQueryManager.Current.GetQueryNames())
                    {
                        if (!QuerySettings.ContainsKey(o))
                            QuerySettings.Add(o, new QuerySettings(o));
                        if (!QuerySettings[o].WebQueryName.HasText())
                            QuerySettings[o].WebQueryName = GenerateWebQueryName(o);
                    }

                    WebQueryNames = QuerySettings.ToDictionary(kvp => kvp.Value.WebQueryName, kvp => kvp.Key, StringComparer.InvariantCultureIgnoreCase, "WebQueryNames");
                }

                Navigator.RegisterArea(typeof(Navigator), "signum");
                FileRepositoryManager.Register(new LocalizedJavaScriptRepository(Resources.ResourceManager, "signum"));
                FileRepositoryManager.Register(new CalendarLocalizedJavaScriptRepository("~/signum/calendarResources/"));

                if (Initializing != null)
                    Initializing();

                Initialized = true;
            }
        }

        HashSet<string> loadedModules = new HashSet<string>();

        public bool NotDefined(MethodBase currentMethod)
        {
            string methodName = currentMethod.DeclaringType.TypeName() + "." + currentMethod.Name;

            return loadedModules.Add(methodName);
        }

        protected internal string GetOrCreateTabID(ControllerBase c)
        {
            if (c.ControllerContext.HttpContext.Request.Form.AllKeys.Contains(ViewDataKeys.TabId))
            {
                string tabID = c.ControllerContext.HttpContext.Request.Form[ViewDataKeys.TabId];
                if (tabID.HasText())
                    return tabID;
            }
            return Guid.NewGuid().ToString();
        }

        protected internal virtual ViewResult View(ControllerBase controller, IRootEntity entity, string partialViewName)
        {
            FillViewDataForViewing(controller, entity, partialViewName, EntitySettingsContext.Admin);

            return new ViewResult()
            {
                ViewName = NormalPageView,
                MasterName = null,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual PartialViewResult NormalControl(ControllerBase controller, IRootEntity entity, string partialViewName)
        {
            FillViewDataForViewing(controller, entity, partialViewName, EntitySettingsContext.Admin);

            return new PartialViewResult()
            {
                ViewName = NormalControlView,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        private void FillViewDataForViewing(ControllerBase controller, IRootEntity entity, string partialViewName, EntitySettingsContext ctx)
        { 
            TypeContext tc = TypeContextUtilities.UntypedNew(entity, "");
            controller.ViewData.Model = tc; 

            controller.ViewData[ViewDataKeys.PartialViewName] = partialViewName ?? Navigator.OnPartialViewName((ModifiableEntity)entity);
            
            if (controller.ViewData[ViewDataKeys.TabId] == null)
                controller.ViewData[ViewDataKeys.TabId] = GetOrCreateTabID(controller);
            
            if (entity is ModifiableEntity)
            {
                if (!Navigator.IsViewable((ModifiableEntity)entity, ctx))
                    throw new UnauthorizedAccessException(Resources.ViewForType0IsNotAllowed.Formato(entity.GetType()));

                if (Navigator.IsReadOnly((ModifiableEntity)entity, ctx))
                    tc.ReadOnly = true;
            }
            else
            {
                if (!Navigator.IsViewable(entity.GetType(), ctx))
                    throw new UnauthorizedAccessException(Resources.ViewForType0IsNotAllowed.Formato(entity.GetType()));

                if (Navigator.IsReadOnly(entity.GetType(), ctx))
                    tc.ReadOnly = true;
            }
        }

        public string GetTypeTitle(ModifiableEntity mod)
        {
            if (mod == null)
                return "";

            string niceName = mod.GetType().NiceName();

            IdentifiableEntity ident = mod as IdentifiableEntity;
            if (ident == null)
                return niceName;

            if (ident.IsNew)
            {
                Gender gender = ident.GetType().GetGender();
                return Properties.Resources.ResourceManager.GetGenderAwareResource("New", gender) + " " + niceName;

            }
            return niceName + " " + ident.Id;
        }

        protected internal virtual PartialViewResult PopupOpen(ControllerBase controller, ViewOptionsBase viewOptions)
        {
            TypeContext cleanTC = TypeContextUtilities.CleanTypeContext(viewOptions.TypeContext);
            Type cleanType = cleanTC.UntypedValue.GetType();

            if (!Navigator.IsViewable(cleanType, viewOptions.Context))
                throw new UnauthorizedAccessException(Resources.ViewForType0IsNotAllowed.Formato(cleanType.Name));

            controller.ViewData.Model = cleanTC;
            controller.ViewData[ViewDataKeys.PartialViewName] = viewOptions.PartialViewName ?? Navigator.OnPartialViewName((ModifiableEntity)cleanTC.UntypedValue);

            bool isReadOnly = viewOptions.ReadOnly ?? Navigator.IsReadOnly(cleanType, viewOptions.Context);
            if (isReadOnly)
                cleanTC.ReadOnly = true;

            ViewButtons buttons = viewOptions.GetViewButtons();
            controller.ViewData[ViewDataKeys.ViewButtons] = buttons;
            controller.ViewData[ViewDataKeys.OkVisible] = buttons == ViewButtons.Ok;
            controller.ViewData[ViewDataKeys.SaveVisible] = buttons == ViewButtons.Save && ShowSave(cleanType) && !isReadOnly;

            return new PartialViewResult
            {
                ViewName = PopupControlView,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual PartialViewResult PartialView(ControllerBase controller, TypeContext tc, string partialViewName)
        {
            TypeContext cleanTC = TypeContextUtilities.CleanTypeContext(tc);
            Type cleanType = cleanTC.UntypedValue.GetType();

            if (!Navigator.IsViewable(cleanType, EntitySettingsContext.Content))
                throw new Exception(Resources.ViewForType0IsNotAllowed.Formato(cleanType.Name));

            controller.ViewData.Model = cleanTC;

            if (Navigator.IsReadOnly(cleanType, EntitySettingsContext.Content))
                cleanTC.ReadOnly = true;

            return new PartialViewResult
            {
                ViewName = partialViewName ?? Navigator.OnPartialViewName((ModifiableEntity)cleanTC.UntypedValue),
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual ViewResult Find(ControllerBase controller, FindOptions findOptions)
        {
            if (!Navigator.IsFindable(findOptions.QueryName))
                throw new UnauthorizedAccessException(Resources.Query0IsNotAllowed.Formato(findOptions.QueryName));

            Navigator.SetTokens(findOptions.QueryName, findOptions.FilterOptions);

            controller.ViewData.Model = new Context(null, "");

            controller.ViewData[ViewDataKeys.PartialViewName] = SearchControlView;

            controller.ViewData[ViewDataKeys.QueryDescription] = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);
            controller.ViewData[ViewDataKeys.FindOptions] = findOptions;

            if (!controller.ViewData.ContainsKey(ViewDataKeys.Title))
                controller.ViewData[ViewDataKeys.Title] = SearchTitle(findOptions.QueryName);
            
            return new ViewResult()
            {
                ViewName = SearchPageView,
                MasterName = null,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual Lite FindUnique(FindUniqueOptions options)
        {
            SetTokens(options.QueryName, options.FilterOptions);
            SetTokens(options.QueryName, options.OrderOptions);

            var request = new UniqueEntityRequest
            {
                QueryName = options.QueryName,
                Filters = options.FilterOptions.Select(f => f.ToFilter()).ToList(),
                Orders = options.OrderOptions.Select(o => o.ToOrder()).ToList(),
                UniqueType = options.UniqueType,
            };

            return DynamicQueryManager.Current.ExecuteUniqueEntity(request);
        }

        protected internal virtual int QueryCount(CountOptions options)
        {
            SetTokens(options.QueryName, options.FilterOptions);

            var request = new QueryCountRequest
            { 
                QueryName = options.QueryName,
                Filters = options.FilterOptions.Select(f => f.ToFilter()).ToList()
            };

            return DynamicQueryManager.Current.ExecuteQueryCount(request);
        }

        protected internal void SetTokens(object queryName, List<FilterOption> filters)
        {
            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(queryName);

            foreach (var f in filters)
                f.Token = QueryUtils.Parse(f.ColumnName, queryDescription);
        }

        public void SetTokens(object queryName, IEnumerable<OrderOption> orders)
        {
            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(queryName);

            foreach (var o in orders)
                o.Token = QueryUtils.Parse(o.ColumnName, queryDescription);
        }

        protected internal virtual PartialViewResult PartialFind(ControllerBase controller, FindOptions findOptions, Context context)
        {
            if (!Navigator.IsFindable(findOptions.QueryName))
                throw new UnauthorizedAccessException(Resources.ViewForType0IsNotAllowed.Formato(findOptions.QueryName));

            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);

            controller.ViewData.Model = context;
            controller.ViewData[ViewDataKeys.PartialViewName] = SearchControlView;
            
            controller.ViewData[ViewDataKeys.FindOptions] = findOptions;
            controller.ViewData[ViewDataKeys.QueryDescription] = queryDescription;
            
            if (!controller.ViewData.ContainsKey(ViewDataKeys.Title))
                controller.ViewData[ViewDataKeys.Title] = SearchTitle(findOptions.QueryName);
            
            return new PartialViewResult
            {
                ViewName = SearchPopupControlView,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        public virtual string SearchTitle(object queryName)
        {
            QuerySettings qs = QuerySettings.TryGetC(queryName);
            if (qs != null && qs.Title != null)
                return qs.Title();
            else
                return QueryUtils.GetNiceName(queryName);
        }

        protected internal virtual PartialViewResult Search(ControllerBase controller, QueryRequest request, bool? allowMultiple, bool view, FilterMode filterMode, Context context)
        {
            if (!Navigator.IsFindable(request.QueryName))
                throw new UnauthorizedAccessException(Resources.ViewForType0IsNotAllowed.Formato(request.QueryName));

            ResultTable queryResult = DynamicQueryManager.Current.ExecuteQuery(request);
            
            controller.ViewData.Model = context;

            controller.ViewData[ViewDataKeys.AllowMultiple] = allowMultiple;
            controller.ViewData[ViewDataKeys.View] = view;
            controller.ViewData[ViewDataKeys.FilterMode] = filterMode;

            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(request.QueryName);
            controller.ViewData[ViewDataKeys.QueryDescription] = qd;

            Type entitiesType = Lite.Extract(qd.Columns.SingleEx(a => a.IsEntity).Type);
            string message = CollectionElementToken.MultipliedMessage(request.Multiplications, entitiesType);
            if (message.HasText())
                controller.ViewData[ViewDataKeys.MultipliedMessage] = message;

            controller.ViewData[ViewDataKeys.Results] = queryResult;

            QuerySettings settings = QuerySettings[request.QueryName];
            controller.ViewData[ViewDataKeys.Formatters] = queryResult.Columns.Select((c, i)=>new {c,i}).ToDictionary(c=>c.i, c =>settings.GetFormatter(c.c.Column));

            return new PartialViewResult
            {
                ViewName = SearchResultsView,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        private string GenerateWebQueryName(object queryName)
        {
            if (queryName is Type)
            {
                Type type = (Type)queryName;
                var es = EntitySettings.TryGetC(type);
                if (es != null)
                    return es.WebTypeName;

                return TypeLogic.TryGetCleanName(type) ?? type.Name;
            }
            
            return queryName.ToString();
        }

        protected internal virtual string ResolveWebQueryName(object queryName)
        {
            return QuerySettings.GetOrThrow(queryName, "queryName {0} not found").WebQueryName;
        }

        protected internal virtual object ResolveQueryName(string webQueryName)
        {
            return WebQueryNames.GetOrThrow(webQueryName, "webQueryName {0} not found");
        }

        protected internal virtual Type ResolveType(string webTypeName)
        {
            return WebTypeNames.TryGetC(webTypeName) ?? TypeLogic.NameToType.GetOrThrow(webTypeName, "webTypeName {0} not found");
        }

        protected internal virtual string ResolveWebTypeName(Type type)
        {
            var es = EntitySettings.TryGetC(type);
            if (es != null)
                return es.WebTypeName;

            if (type.IsIdentifiableEntity())
            {
                var cleanName = TypeLogic.TryGetCleanName(type);
                if (cleanName != null)
                    return cleanName;
            }

            throw new InvalidOperationException("Impossible to resolve WebTypeName for '{0}' because is not registered in Navigator's EntitySettings".Formato(type.Name) + 
                (type.IsIdentifiableEntity() ? " or the Schema" : null));
        }

        protected internal virtual MappingContext<T> ApplyChanges<T>(ControllerContext controllerContext, T entity, string prefix, Mapping<T> mapping, SortedList<string, string> inputs) where T : IRootEntity
        {
            RootContext<T> ctx = new RootContext<T>(prefix, inputs, controllerContext) { Value = entity };
            mapping(ctx);
            return ctx;
        }

        protected internal virtual ModifiableEntity ExtractEntity(ControllerBase controller, string prefix)
        {
            NameValueCollection form = controller.ControllerContext.HttpContext.Request.Form;
            
            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(form[TypeContextUtilities.Compose(prefix ?? "", EntityBaseKeys.RuntimeInfo)]);
            if (runtimeInfo.IdOrNull != null)
                return Database.Retrieve(runtimeInfo.RuntimeType, runtimeInfo.IdOrNull.Value);
            else
                return (ModifiableEntity)Constructor.Construct(runtimeInfo.RuntimeType);
        }

        protected internal virtual Lite<T> ExtractLite<T>(ControllerBase controller, string prefix)
            where T:class, IIdentifiable
        {
            NameValueCollection form = controller.ControllerContext.HttpContext.Request.Form;
            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(form[TypeContextUtilities.Compose(prefix ?? "", EntityBaseKeys.RuntimeInfo)]);

            if (!runtimeInfo.IdOrNull.HasValue)
                throw new ArgumentException("Could not create a Lite without an Id");

            return new Lite<T>(runtimeInfo.RuntimeType, runtimeInfo.IdOrNull.Value);
        }

        protected internal virtual bool IsViewable(Type type, EntitySettingsContext ctx)
        {
            EntitySettings es = EntitySettings.TryGetC(type);
            if (es == null)
                return false;

            return es.OnIsViewable(null, ctx);
        }

        protected internal virtual bool IsViewable(ModifiableEntity entity, EntitySettingsContext ctx)
        {
            EntitySettings es = EntitySettings.TryGetC(entity.GetType());
            if (es == null)
                return false;

            return es.OnIsViewable(entity, ctx);
        }

        protected internal virtual bool IsReadOnly(Type type, EntitySettingsContext ctx)
        {
            EntitySettings es = EntitySettings.TryGetC(type);
            if (es == null)
                return false;

            return es.OnIsReadOnly(null, ctx);
        }

        protected internal virtual bool IsReadOnly(ModifiableEntity entity, EntitySettingsContext ctx)
        {
            EntitySettings es = EntitySettings.TryGetC(entity.GetType());
            if (es == null)
                return false;

            return es.OnIsReadOnly(entity, ctx);
        }

        protected internal virtual bool IsCreable(Type type, EntitySettingsContext ctx)
        {
            EntitySettings es = EntitySettings.TryGetC(type);
            if (es == null)
                return true;

            return es.OnIsCreable(ctx);
        }

        protected internal virtual bool IsFindable(object queryName)
        {
            QuerySettings qs = QuerySettings.TryGetC(queryName);

            if (qs == null)
                return false;

            if (qs.IsFindable == null)
                return true; 

            return qs.OnIsFindable();
        }

        public virtual bool ShowSave(Type type)
        {
            EntitySettings es = EntitySettings.TryGetC(type);
            if (es != null)
                return es.OnShowSave();

            return true;
        }
    }

    public enum JsonResultType
    {
        url,
        ModelState
    }

    public static class JsonAction
    {
        public static JsonResult Redirect(string url)
        {
            return new JsonResult
            {
                Data = new
                {
                    result = JsonResultType.url.ToString(),
                    url = url
                }
            };
        }

        public static JsonResult ModelState(ModelStateDictionary dictionary)
        {
            return ModelState(dictionary, null, null); 
        }

        public static JsonResult ModelState(ModelStateDictionary dictionary, string newToString, string newToStringLink)
        {
            Dictionary<string, object> result = new Dictionary<string, object>
            {
                {"result", JsonResultType.ModelState.ToString()},
                {"ModelState", dictionary.ToJsonData()}
            };

            if (newToString != null)
                result.Add(EntityBaseKeys.ToStr, newToString);
            if (newToStringLink != null)
                result.Add(EntityBaseKeys.ToStrLink, newToStringLink);

            return new JsonResult { Data = result };
        }
    }
}
