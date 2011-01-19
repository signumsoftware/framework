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
            return Manager.View(controller, entity, null, false); 
        }

        public static ViewResult View(ControllerBase controller, IRootEntity entity, bool admin)
        {
            return Manager.View(controller, entity, null, admin); 
        }

        public static ViewResult View(ControllerBase controller, IRootEntity entity, string partialViewName)
        {
            return Manager.View(controller, entity, partialViewName, false);
        }

        public static ViewResult View(ControllerBase controller, IRootEntity entity, string partialViewName, bool admin)
        {
            return Manager.View(controller, entity, partialViewName, admin);
        }

        public static PartialViewResult NormalControl(ControllerBase controller, IRootEntity entity)
        {
            return Manager.NormalControl(controller, entity, null, false); 
        }

        public static PartialViewResult NormalControl(ControllerBase controller, IRootEntity entity, bool admin)
        {
            return Manager.NormalControl(controller, entity, null, admin); 
        }

        public static PartialViewResult NormalControl(ControllerBase controller, IRootEntity entity, string partialViewName)
        {
            return Manager.NormalControl(controller, entity, partialViewName, false);
        }

        public static string GetOrCreateTabID(ControllerBase c)
        {
            return Manager.GetOrCreateTabID(c);
        }

        public static bool IsReactive(this ControllerBase controller)
        {
            return controller.ControllerContext.HttpContext.Request.Form.AllKeys.Contains(ViewDataKeys.Reactive);
        }

        public static string TabID(this ControllerBase controller)
        {
            NameValueCollection form = controller.ControllerContext.HttpContext.Request.Form;

            string tabID;
            if (!form.AllKeys.Contains(ViewDataKeys.TabId) || !(tabID = (string)form[ViewDataKeys.TabId]).HasText())
                throw new InvalidOperationException("The Request doesn't have the necessary tab identifier");           
            
            return tabID;
        }

        public static PartialViewResult PopupView(this ControllerBase controller, TypeContext tc)
        {
            return Manager.PopupView(controller, tc, null);
        }

        public static PartialViewResult PopupView(this ControllerBase controller, TypeContext tc, string partialViewName)
        {
            return Manager.PopupView(controller, tc, partialViewName);
        }

        public static PartialViewResult PopupView(this ControllerBase controller, IRootEntity entity, string prefix)
        {
            TypeContext tc = TypeContextUtilities.UntypedNew(entity, prefix);
            return Manager.PopupView(controller, tc, null);
        }

        public static PartialViewResult PopupView(this ControllerBase controller, IRootEntity entity, string prefix, string partialViewName)
        {
            TypeContext tc = TypeContextUtilities.UntypedNew(entity, prefix);
            return Manager.PopupView(controller, tc, partialViewName);
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

        public static PartialViewResult Search(ControllerBase controller, FindOptions findOptions, int? top, Context context)
        {
            return Manager.Search(controller, findOptions, top, context);
        }

        public static PartialViewResult Search(ControllerBase controller, FindOptions findOptions, int? top, string prefix)
        {
            return Manager.Search(controller, findOptions, top, new Context(null, prefix));
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

        public static SortedList<string, string> ToSortedList(this NameValueCollection form, string prefixFilter, string prefixToIgnore)
        {
            SortedList<string, string> formValues = new SortedList<string, string>(form.Count);
            foreach (string key in form.Keys)
            {
                if (key.HasText() && key != "prefixToIgnore" && (string.IsNullOrEmpty(prefixFilter) || key.StartsWith(prefixFilter)))
                {
                    if (string.IsNullOrEmpty(prefixToIgnore) || !key.StartsWith(prefixToIgnore))
                        formValues.Add(key, form[key]);
                }
            }
            
            return formValues;
        }

        public static SortedList<string, object> ToSortedList(NameValueCollection form, string prefixToIgnore)
        {
            SortedList<string, object> formValues = new SortedList<string, object>(form.Count);
            foreach (string key in form.Keys)
            {
                if (!string.IsNullOrEmpty(key) && key != "prefixToIgnore")
                {
                    if (string.IsNullOrEmpty(prefixToIgnore) || !key.StartsWith(prefixToIgnore))
                        formValues.Add(key, form[key]);
                }
            }

            return formValues;
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
            return Manager.EntitySettings.GetOrThrow(type, "no EntitySettings for type {0} found");
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
            return (MappingContext)miApplyChanges.GetInvoker(entity.GetType()).Invoke(entity, controllerContext, prefix, admin);
        }

        static GenericInvoker miApplyChanges = GenericInvoker.Create(()=>new TypeDN().ApplyChanges(null, null, true));
        public static MappingContext<T> ApplyChanges<T>(this T entity, ControllerContext controllerContext, string prefix, bool admin) where T : ModifiableEntity
        {
            SortedList<string, string> inputs = controllerContext.HttpContext.Request.Form.ToSortedList(prefix, null);
            Mapping<T> mapping = (Mapping<T>)Navigator.EntitySettings(typeof(T)).Map(s => admin ? s.UntypedMappingAdmin : s.UntypedMappingDefault);

            return Manager.ApplyChanges<T>(controllerContext, entity, prefix, mapping, inputs);
        }

        public static MappingContext<T> ApplyChanges<T>(this T entity, ControllerContext controllerContext, string prefix, bool admin, SortedList<string, string> inputs) where T : ModifiableEntity
        {
            Mapping<T> mapping = (Mapping<T>)Navigator.EntitySettings(typeof(T)).Map(s => admin ? s.UntypedMappingAdmin : s.UntypedMappingDefault);

            return Manager.ApplyChanges<T>(controllerContext, entity, prefix, mapping, inputs);
        }

        public static MappingContext<T> ApplyChanges<T>(this T entity, ControllerContext controllerContext, string prefix, Mapping<T> mapping) where T : ModifiableEntity
        {
            SortedList<string, string> inputs = controllerContext.HttpContext.Request.Form.ToSortedList(prefix, null);

            return Manager.ApplyChanges<T>(controllerContext, entity, prefix, mapping, inputs);
        }

        public static MappingContext<T> ApplyChanges<T>(this T entity, ControllerContext controllerContext, string prefix, Mapping<T> mapping, SortedList<string, string> inputs) where T : ModifiableEntity
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

        public static List<Lite<T>> ExtractLitesList<T>(string commaSeparatedIds, bool retrive) where T : class, IIdentifiable
        {
            if (!commaSeparatedIds.HasText())
                return new List<Lite<T>>();

            var ids = commaSeparatedIds.Split(new[]{','},StringSplitOptions.RemoveEmptyEntries );
            if (retrive)
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

        public static bool IsNavigable(Type type, bool admin)
        {
            return Manager.IsNavigable(type, admin);
        }
        public static bool IsNavigable(ModifiableEntity entity, bool admin)
        {
            return Manager.IsNavigable(entity, admin);
        }

        public static bool IsViewable(Type type, bool admin)
        {
            return Manager.IsViewable(type, admin);
        }
        public static bool IsViewable(ModifiableEntity entity, bool admin)
        {
            return Manager.IsViewable(entity, admin);
        }

        public static bool IsReadOnly(Type type, bool admin)
        {
            return Manager.IsReadOnly(type, admin);
        }
        public static bool IsReadOnly(ModifiableEntity entity, bool admin)
        {
            return Manager.IsReadOnly(entity, admin);
        }

        public static bool IsCreable(Type type, bool admin)
        {
            return Manager.IsCreable(type, admin);
        }
        public static bool IsFindable(object queryName)
        {
            return Manager.IsFindable(queryName);
        }
        public static ContentResult ModelState(ModelStateData modelStateData)
        {
            return Manager.ModelState(modelStateData);
        }

        public static ContentResult ModelState(ModelStateDictionary modelState)
        {
            return Manager.ModelState(new ModelStateData(modelState));
        }

        public static ContentResult RedirectUrl(string url)
        {
            return Manager.RedirectUrl(url);
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
            CompiledViews.RegisterArea(clientType.Assembly, areaName);
            PortableAreaControllers.RegisterControllersIn(clientType, areaName);

            EmbeddedFilesRespository rep = new EmbeddedFilesRespository(clientType.Assembly, areaName);
            if (!rep.IsEmpty)
                FileRepositoryManager.Register(rep);
        }

        public static void Initialize()
        {
            Manager.Initialize();
        }
    }
    
    public class NavigationManager
    {
        public Dictionary<Type, EntitySettings> EntitySettings {get;set;}
        public Dictionary<object, QuerySettings> QuerySettings {get;set;}


        public string AjaxErrorPageUrl = RouteHelper.AreaView("AjaxError", "signum");
        public string ErrorPageUrl = RouteHelper.AreaView("Error", "signum");
        public string NormalPageUrl = RouteHelper.AreaView("NormalPage", "signum");
        public string NormalControlUrl = RouteHelper.AreaView("NormalControl", "signum");
        public string PopupControlUrl = RouteHelper.AreaView("PopupControl", "signum");
        public string ChooserPopupUrl = RouteHelper.AreaView("ChooserPopup", "signum");
        public string SearchPopupControlUrl = RouteHelper.AreaView("SearchPopupControl", "signum");
        public string SearchWindowUrl = RouteHelper.AreaView( "SearchWindow", "signum");
        public string SearchControlUrl = RouteHelper.AreaView( "SearchControl", "signum");
        public string SearchResultsUrl = RouteHelper.AreaView("SearchResults", "signum");
        public string FilterBuilderUrl = RouteHelper.AreaView("FilterBuilder", "signum");
        public string ValueLineBoxUrl = RouteHelper.AreaView("ValueLineBox", "signum");
        
        protected Dictionary<string, Type> WebTypeNames { get; private set; }
        protected Dictionary<string, object> WebQueryNames { get; private set; }

        public Func<string, bool> AllowUserColumns = s => s.HasText() ? false : true;

        public NavigationManager()
        {
            EntitySettings = new Dictionary<Type, EntitySettings>();
            QuerySettings = new Dictionary<object, QuerySettings>();
        }

        public static int QueryMaxResults = 50;

        public event Action Initializing;
        public bool Initialized { get; private set; }
        internal void Initialize()
        {
            if (!Initialized)
            {
                Navigator.AddSetting(new EmbeddedEntitySettings<ValueLineBoxModel> { PartialViewName = _ => ValueLineBoxUrl });

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
                            QuerySettings.Add(o, new QuerySettings(o) { Top = QueryMaxResults });
                        if (!QuerySettings[o].WebQueryName.HasText())
                            QuerySettings[o].WebQueryName = GenerateWebQueryName(o);
                    }

                    WebQueryNames = QuerySettings.ToDictionary(kvp => kvp.Value.WebQueryName, kvp => kvp.Key, StringComparer.InvariantCultureIgnoreCase, "WebQueryNames");
                }

                Navigator.RegisterArea(typeof(Navigator), "signum");

                FileRepositoryManager.Register(new LocalizedJavaScriptRepository(Resources.ResourceManager, "signum"));

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

        protected internal virtual ViewResult View(ControllerBase controller, IRootEntity entity, string partialViewName, bool admin)
        {
            FillViewDataForViewing(controller, entity, partialViewName, admin);

            return new ViewResult()
            {
                ViewName = NormalPageUrl,
                MasterName = null,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual PartialViewResult NormalControl(ControllerBase controller, IRootEntity entity, string partialViewName, bool admin)
        {
            FillViewDataForViewing(controller, entity, partialViewName, admin);

            return new PartialViewResult()
            {
                ViewName = NormalControlUrl,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        private void FillViewDataForViewing(ControllerBase controller, IRootEntity entity, string partialViewName, bool admin)
        { 
            Type type = entity.GetType();
            
            TypeContext tc = TypeContextUtilities.UntypedNew(entity, "");
            controller.ViewData.Model = tc; 

            controller.ViewData[ViewDataKeys.PartialViewName] = partialViewName ?? Navigator.OnPartialViewName((ModifiableEntity)entity);

            if (!controller.ViewData.ContainsKey(ViewDataKeys.Title))
                controller.ViewData[ViewDataKeys.Title] = entity.ToString();
            
            string tabID = GetOrCreateTabID(controller);
            controller.ViewData[ViewDataKeys.TabId] = tabID;

            if (!Navigator.IsNavigable(type, admin))
                throw new UnauthorizedAccessException(Resources.ViewForType0IsNotAllowed.Formato(type));

            if (Navigator.IsReadOnly(type, admin))
                tc.ReadOnly = true;
            
            bool useSessionWhenNew = GraphExplorer.FromRoot((ModifiableEntity)entity).Any(m => (m as IIdentifiable).TryCS(i => i.IsNew) == true && m.GetType().HasAttribute<UseSessionWhenNew>());
            bool isReactive = GraphExplorer.FromRoot((ModifiableEntity)entity).Any(m => m.GetType().HasAttribute<Reactive>());

            if (useSessionWhenNew || isReactive)
            {
                controller.ViewData[ViewDataKeys.Reactive] = true;
                controller.ControllerContext.HttpContext.Session[tabID] = entity;
            }
        }

        protected internal virtual PartialViewResult PopupView(ControllerBase controller, TypeContext tc, string partialViewName)
        {
            TypeContext cleanTC = TypeContextUtilities.CleanTypeContext(tc);
            Type cleanType = cleanTC.UntypedValue.GetType();

            if (!Navigator.IsViewable(cleanType, false))
                throw new UnauthorizedAccessException(Resources.ViewForType0IsNotAllowed.Formato(cleanType.Name));

            controller.ViewData.Model = cleanTC;
            controller.ViewData[ViewDataKeys.PartialViewName] = partialViewName ?? Navigator.OnPartialViewName((ModifiableEntity)cleanTC.UntypedValue);
            
            if (Navigator.IsReadOnly(cleanType, false))
                cleanTC.ReadOnly = true;
            
            return new PartialViewResult
            {
                ViewName = PopupControlUrl,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual PartialViewResult PartialView(ControllerBase controller, TypeContext tc, string partialViewName)
        {
            TypeContext cleanTC = TypeContextUtilities.CleanTypeContext(tc);
            Type cleanType = cleanTC.UntypedValue.GetType();

            if (!Navigator.IsViewable(cleanType, false))
                throw new Exception(Resources.ViewForType0IsNotAllowed.Formato(cleanType.Name));

            controller.ViewData.Model = cleanTC;

            if (Navigator.IsReadOnly(cleanType, true/*not always*/))
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
                throw new UnauthorizedAccessException(Resources.ViewForType0IsNotAllowed.Formato(findOptions.QueryName));

            Navigator.SetTokens(findOptions.QueryName, findOptions.FilterOptions);

            controller.ViewData.Model = new Context(null, "");

            controller.ViewData[ViewDataKeys.PartialViewName] = SearchControlUrl;

            controller.ViewData[ViewDataKeys.QueryDescription] = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);
            controller.ViewData[ViewDataKeys.FindOptions] = findOptions;

            if (controller.ViewData.ContainsKey(ViewDataKeys.Title))
                controller.ViewData[ViewDataKeys.Title] = SearchTitle(findOptions.QueryName);
            
            return new ViewResult()
            {
                ViewName = SearchWindowUrl,
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
            controller.ViewData[ViewDataKeys.PartialViewName] = SearchControlUrl;
            
            controller.ViewData[ViewDataKeys.FindOptions] = findOptions;
            controller.ViewData[ViewDataKeys.QueryDescription] = queryDescription;
            
            if (controller.ViewData.ContainsKey(ViewDataKeys.Title))
                controller.ViewData[ViewDataKeys.Title] = SearchTitle(findOptions.QueryName);
            
            return new PartialViewResult
            {
                ViewName = SearchPopupControlUrl,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual string SearchTitle(object queryName)
        {
            QuerySettings qs = QuerySettings.TryGetC(queryName);
            if (qs != null && qs.Title != null)
                return qs.Title();
            else
                return QueryUtils.GetNiceName(queryName);
        }

        protected internal virtual PartialViewResult Search(ControllerBase controller, FindOptions findOptions, int? top, Context context)
        {
            if (!Navigator.IsFindable(findOptions.QueryName))
                throw new UnauthorizedAccessException(Resources.ViewForType0IsNotAllowed.Formato(findOptions.QueryName));

            QueryRequest request = findOptions.ToQueryRequest(); 

            ResultTable queryResult = DynamicQueryManager.Current.ExecuteQuery(request);
            
            controller.ViewData.Model = context;
            
            controller.ViewData[ViewDataKeys.FindOptions] = findOptions;
            controller.ViewData[ViewDataKeys.QueryDescription] = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);
            
            controller.ViewData[ViewDataKeys.Results] = queryResult;

            QuerySettings settings = QuerySettings[findOptions.QueryName];
            controller.ViewData[ViewDataKeys.Formatters] = queryResult.Columns.Select((c, i)=>new {c,i}).ToDictionary(c=>c.i, c =>settings.GetFormatter(c.c.Column));

            return new PartialViewResult
            {
                ViewName = SearchResultsUrl,
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
            return es != null ? es.WebTypeName : 
                TypeLogic.GetCleanName(type); //For types registered in the schema but not in web
        }

        protected internal virtual MappingContext<T> ApplyChanges<T>(ControllerContext controllerContext, T entity, string prefix, Mapping<T> mapping, SortedList<string, string> inputs) where T : ModifiableEntity
        {
            RootContext<T> ctx = new RootContext<T>(prefix, mapping, inputs, controllerContext) { Value = entity };
            mapping.OnGetValue(ctx);
            ctx.Finish();
            return ctx;
        }

        protected internal virtual ModifiableEntity ExtractEntity(ControllerBase controller, string prefix)
        {
            NameValueCollection form = controller.ControllerContext.HttpContext.Request.Form;
            
            if (form.AllKeys.Any(s => s == ViewDataKeys.Reactive) && (string.IsNullOrEmpty(prefix) || !prefix.StartsWith("New")))
            {
                controller.ViewData[ViewDataKeys.Reactive] = true;
                ModifiableEntity mod = (ModifiableEntity)controller.ControllerContext.HttpContext.Session[controller.TabID()];
                if (mod == null)
                    throw new InvalidOperationException(Resources.YourSessionHasTimedOutClickF5ToReloadTheEntity);

                RuntimeInfo parentRuntimeInfo = RuntimeInfo.FromFormValue(form[EntityBaseKeys.RuntimeInfo]);
                if (mod.GetType() == parentRuntimeInfo.RuntimeType &&
                    (mod.GetType().IsEmbeddedEntity() || ((IIdentifiable)mod).IdOrNull == parentRuntimeInfo.IdOrNull))
                {
                    //if (clone == null || clone.Value) 
                    //    return (ModifiableEntity)((ICloneable)mod).Clone();
                    return mod;
                }
                else
                    throw new InvalidOperationException(Resources.IncorrectEntityInSessionYouMustReloadThePageToContinue);
            }

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
            return new Lite<T>(runtimeInfo.RuntimeType, runtimeInfo.IdOrNull.Value);
        }

        protected internal virtual bool IsViewable(Type type, bool admin)
        {
            EntitySettings es = EntitySettings.TryGetC(type);
            if (es == null)
                return false;

            return es.OnIsViewable(null, admin);
        }

        protected internal virtual bool IsViewable(ModifiableEntity entity, bool admin)
        {
            EntitySettings es = EntitySettings.TryGetC(entity.GetType());
            if (es == null)
                return false;

            return es.OnIsViewable(entity, admin);
        }

        protected internal virtual bool IsNavigable(Type type, bool admin)
        {
            EntitySettings es = EntitySettings.TryGetC(type);
            if (es == null)
                return false;

            return es.OnIsNavigable(null, admin);
        }

        protected internal virtual bool IsNavigable(ModifiableEntity entity, bool admin)
        {
            EntitySettings es = EntitySettings.TryGetC(entity.GetType());
            if (es == null)
                return false;

            return es.OnIsNavigable(entity, admin);
        }

        protected internal virtual bool IsReadOnly(Type type, bool admin)
        {
            EntitySettings es = EntitySettings.TryGetC(type);
            if (es == null)
                return false;

            return es.OnIsReadOnly(null, admin);
        }

        protected internal virtual bool IsReadOnly(ModifiableEntity entity, bool admin)
        {
            EntitySettings es = EntitySettings.TryGetC(entity.GetType());
            if (es == null)
                return false;

            return es.OnIsReadOnly(entity, admin);
        }

        protected internal virtual bool IsCreable(Type type, bool admin)
        {
            EntitySettings es = EntitySettings.TryGetC(type);
            if (es == null)
                return true;

            return es.OnIsCreable(admin);
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

        public virtual bool ShowOkSave(Type type, bool admin)
        {
            EntitySettings es = EntitySettings.TryGetC(type);
            if (es != null)
                return es.OnShowSave();

            return true;
        }

        internal ContentResult ModelState(ModelStateData modelStateData)
        {
            System.Diagnostics.Debug.WriteLine(modelStateData.ToString());

            return new ContentResult { Content = modelStateData.ToString() };
        }

        internal ContentResult RedirectUrl(string url)
        {
            var dic = new
            { 
                result = JsonResultType.url.ToString(),
                url = url
            };

            return new ContentResult 
            { 
                Content = dic.ToJSonObject(v => v.ToString().Quote())
            };
        }

    }

    public enum JsonResultType
    {
        url,
        ModelState
    }
}
