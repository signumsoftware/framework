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
#endregion

namespace Signum.Web
{
    public static class Navigator
    {
        private static Func<Type, int?, string> viewRoute;
        public static Func<Type, int?, string> ViewRoute 
        { 
            get 
            {
                return viewRoute ??
                       (viewRoute = (Type type, int? id) => "View/{0}/{1}".Formato(Navigator.TypesToURLNames[type], id.TryToString()));
            }
            set 
            {
                viewRoute = value;
            }
        }

        private static Func<object, string> findRoute;
        public static Func<object, string> FindRoute
        {
            get
            {
                return findRoute ??
                       (findRoute = (object queryName) => "Find/{0}".Formato(Manager.QuerySettings[queryName].UrlName));
            }
            set
            {
                findRoute = value;
            }
        }

        public static NavigationManager Manager;

        public static void Start(NavigationManager manager)
        {
            Manager = manager;
        }
        
        public static Type ResolveType(string typeName)
        {
            return Manager.ResolveType(typeName);
        }

        public static Type ResolveTypeFromUrlName(string typeUrlName)
        {
            return Manager.ResolveTypeFromUrlName(typeUrlName); 
        }

        public static object ResolveQueryFromUrlName(string queryUrlName)
        {
            return Manager.ResolveQueryFromUrlName(queryUrlName);
        }

        public static string SearchTitle(object queryName)
        {
            return  Manager.GetNiceQueryName(queryName);
        }

        public static object ResolveQueryFromKey(string queryNameKey)
        {
            return Manager.ResolveQueryFromKey(queryNameKey);
        }

        public static ViewResult View(ControllerBase controller, IdentifiableEntity entity)
        {
            return Manager.View(controller, entity, null, false); 
        }

        public static ViewResult View(ControllerBase controller, IdentifiableEntity entity, bool admin)
        {
            return Manager.View(controller, entity, null, admin); 
        }

        public static ViewResult View(ControllerBase controller, IdentifiableEntity entity, string partialViewName)
        {
            return Manager.View(controller, entity, partialViewName, false);
        }

        public static ViewResult View(ControllerBase controller, IdentifiableEntity entity, string partialViewName, bool admin)
        {
            return Manager.View(controller, entity, partialViewName, admin);
        }

        public static PartialViewResult NormalControl(ControllerBase controller, IdentifiableEntity entity)
        {
            return Manager.NormalControl(controller, entity, null, false); 
        }

        public static PartialViewResult NormalControl(ControllerBase controller, IdentifiableEntity entity, bool admin)
        {
            return Manager.NormalControl(controller, entity, null, admin); 
        }

        public static PartialViewResult NormalControl(ControllerBase controller, IdentifiableEntity entity, string partialViewName)
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

        public static PartialViewResult PopupView(this ControllerBase controller, IIdentifiable entity, string prefix)
        {
            TypeContext tc = TypeContextUtilities.UntypedNew((IdentifiableEntity)entity, prefix);
            return Manager.PopupView(controller, tc, null);
        }

        public static PartialViewResult PopupView(this ControllerBase controller, IIdentifiable entity, string prefix, string partialViewName)
        {
            TypeContext tc = TypeContextUtilities.UntypedNew((IdentifiableEntity)entity, prefix);
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

        public static PartialViewResult PartialView(this ControllerBase controller, IIdentifiable entity, string prefix)
        {
            TypeContext tc = TypeContextUtilities.UntypedNew((IdentifiableEntity)entity, prefix);
            return Manager.PartialView(controller, tc, null);
        }

        public static PartialViewResult PartialView(this ControllerBase controller, IIdentifiable entity, string prefix, string partialViewName)
        {
            TypeContext tc = TypeContextUtilities.UntypedNew((IdentifiableEntity)entity, prefix);
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

        public static EntitySettings<T> EntitySettings<T>() where T : ModifiableEntity
        {
            return (EntitySettings<T>)Manager.EntitySettings[typeof(T)];
        }

        public static MappingContext UntypedApplyChanges(this ModifiableEntity entity, ControllerContext controllerContext, string prefix, bool admin)
        {
            return (MappingContext)miApplyChanges.GenericInvoke(new Type[] { entity.GetType() }, null, new object[] { entity, controllerContext, prefix, admin });
        }

        static MethodInfo miApplyChanges = ReflectionTools.GetMethodInfo(()=>new TypeDN().ApplyChanges(null, null, true)).GetGenericMethodDefinition();
        public static MappingContext<T> ApplyChanges<T>(this T entity, ControllerContext controllerContext, string prefix, bool admin) where T : ModifiableEntity
        {
            SortedList<string, string> inputs = controllerContext.HttpContext.Request.Form.ToSortedList(prefix, null);
            Mapping<T> mapping = Navigator.EntitySettings<T>().Map(s => admin ? s.MappingAdmin : s.MappingDefault);

            return Manager.ApplyChanges<T>(controllerContext, entity, prefix, mapping, inputs);
        }

        public static MappingContext<T> ApplyChanges<T>(this T entity, ControllerContext controllerContext, string prefix, bool admin, SortedList<string, string> inputs) where T : ModifiableEntity
        {
            Mapping<T> mapping = Navigator.EntitySettings<T>().Map(s => admin ? s.MappingAdmin : s.MappingDefault);

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

        public static Dictionary<string, Type> URLNamesToTypes
        {
            get { return Manager.URLNamesToTypes; }
        }

        public static Dictionary<Type, string> TypesToURLNames
        {
            get { return Manager.TypesToURLNames; }
        }

        public static Dictionary<string, Type> NamesToTypes
        {
            get { return Manager.NamesToTypes; }
        }

        public static Dictionary<Type, string> TypesToNames
        {
            get { return Manager.TypesToNames; }
        }

        public static bool RegisterTypeName<T>()
        {
            return RegisterTypeName(typeof(T), typeof(T).Name);
        }

        public static bool RegisterTypeName(Type type)
        {
            return RegisterTypeName(type, type.Name);
        }

        public static bool RegisterTypeName(Type type, string name)
        {
            if (TypesToNames.ContainsKey(type))
                return false;

            TypesToNames.Add(type, name);
            return true;
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
            return Manager.EntitySettings.GetOrThrow(entity.GetType(), "There's no EntitySettings registered for type {0}").OnPartialViewName(entity); 
        }

        public static string GetName(Type type)
        {
            return TypesToNames.GetOrThrow(type, "{0} not registered. Call Navigator.RegisterTypeName");
        }

        public static Lite ParseLite(Type staticType, string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            return Lite.ParseLite(staticType, value, typeName =>
                Navigator.NamesToTypes.GetOrThrow(typeName, "The name {0} does not correspond to any type in navigator"));
        }

        public static string Key(this Lite lite)
        {
            if (lite == null)
                return null;

            return lite.Key(rt => Navigator.TypesToNames.GetOrThrow(rt, "The type {0} is not registered in navigator"));
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

        public static string ViewsPrefix = "signum/Views/";

        public string AjaxErrorPageUrl = ViewsPrefix + "AjaxError";
        public string ErrorPageUrl = ViewsPrefix + "Error";
        public string NormalPageUrl = ViewsPrefix + "NormalPage";
        public string NormalControlUrl = ViewsPrefix + "NormalControl";
        public string PopupControlUrl = ViewsPrefix + "PopupControl";
        public string ChooserPopupUrl = ViewsPrefix + "ChooserPopup";
        public string SearchPopupControlUrl = ViewsPrefix + "SearchPopupControl";
        public string SearchWindowUrl = ViewsPrefix + "SearchWindow";
        public string SearchControlUrl = ViewsPrefix + "SearchControl";
        public string SearchResultsUrl = ViewsPrefix + "SearchResults";
        public string FilterBuilderUrl = ViewsPrefix + "FilterBuilder";
        public string ValueLineBoxUrl = ViewsPrefix + "ValueLineBox";
        
        protected internal Dictionary<string, Type> URLNamesToTypes { get; private set; }
        protected internal Dictionary<Type, string> TypesToURLNames { get; private set; }
        protected internal Dictionary<string, Type> NamesToTypes { get; private set; }
        protected internal Dictionary<Type, string> TypesToNames { get; private set; }

        protected internal Dictionary<string, object> UrlQueryNames { get; private set; }

        //public event Func<Type, bool> GlobalIsCreable;
        //public event Func<Type, bool> GlobalIsViewable;
        //public event Func<Type, bool> GlobalIsNavigable;
        //public event Func<Type, bool> GlobalIsReadOnly;
        //public event Func<object, bool> GlobalIsFindable;

        public Func<string, bool> AllowUserColumns = s => s.HasText() ? false : true;

        public NavigationManager()
        {
            EntitySettings = new Dictionary<Type, EntitySettings>();
            QuerySettings = new Dictionary<object, QuerySettings>();
            TypesToNames = new Dictionary<Type, string>();
        }

        public event Action Initializing;
        public bool Initialized { get; private set; }
        internal void Initialize()
        {
            if (!Initialized)
            {
                Navigator.AddSetting(new EntitySettings<ValueLineBoxModel>(EntityType.Default) { PartialViewName = _ => ValueLineBoxUrl });

                Navigator.RegisterTypeName<IIdentifiable>();
                Navigator.RegisterTypeName<IdentifiableEntity>();

                TypesToURLNames = EntitySettings.SelectDictionary(k => k, (k, v) => v.UrlName ?? Reflector.CleanTypeName(k));
                URLNamesToTypes = TypesToURLNames.Inverse(StringComparer.InvariantCultureIgnoreCase, "URLNamesToTypes");

                TypesToNames.AddRange(EntitySettings, kvp => kvp.Key, kvp => kvp.Value.TypeName ?? kvp.Key.Name, "TypeToNames");
                NamesToTypes = TypesToNames.Inverse("NamesToTypes");

                if (DynamicQueryManager.Current != null)
                {
                    foreach (object o in DynamicQueryManager.Current.GetQueryNames())
                    {
                        if (!QuerySettings.ContainsKey(o))
                            QuerySettings.Add(o, new QuerySettings() { Top = 50 });
                        if (!QuerySettings[o].UrlName.HasText())
                            QuerySettings[o].UrlName = GetQueryName(o);
                    }

                    UrlQueryNames = QuerySettings.ToDictionary(kvp => kvp.Value.UrlName ?? GetQueryName(kvp.Key), kvp => kvp.Key, StringComparer.InvariantCultureIgnoreCase, "UrlQueryNames");
                }

                ConfigureSignumWebApplication();

                if (Initializing != null)
                    Initializing();

                Initialized = true;
            }
        }

        private void ConfigureSignumWebApplication()
        {
            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(new SignumViewEngine());

            AssemblyResourceManager.RegisterAreaResources(
                new AssemblyResourceStore(typeof(Navigator), "/signum/", "Signum.Web.Signum."));

            RouteTable.Routes.InsertRouteAt0("signum/{resourcesFolder}/{*resourceName}",
                    new { controller = "Resources", action = "Index", area = "signum" },
                    new { resourcesFolder = new InArray(new string[] { "Scripts", "Content", "Images" }) });

            ModelBinders.Binders.DefaultBinder = new LiteModelBinder();
            ModelBinders.Binders.Add(typeof(Implementations), new ImplementationsModelBinder());
            ModelBinders.Binders.Add(typeof(FindOptions), new FindOptionsModelBinder());

            HostingEnvironment.RegisterVirtualPathProvider(new AssemblyResourceProvider());
        }

        HashSet<string> loadedModules = new HashSet<string>();

        public bool NotDefined(MethodBase currentMethod)
        {
            string methodName = currentMethod.DeclaringType.TypeName() + "." + currentMethod.Name;

            return loadedModules.Add(methodName);
        }

        protected internal virtual string GetQueryName(object queryName)
        {
            return (queryName is Type) ? (TypesToURLNames.TryGetC<Type, string>((Type)queryName) ?? ((Type)queryName).Name) :
                   //(queryName is Enum) ? queryName.ToString() : 
                   queryName.ToString();
        }

        protected internal virtual string GetNiceQueryName(object queryName)
        {
            return QueryUtils.GetNiceQueryName(queryName); 
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

        protected internal virtual ViewResult View(ControllerBase controller, IdentifiableEntity entity, string partialViewName, bool admin)
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

        protected internal virtual PartialViewResult NormalControl(ControllerBase controller, IdentifiableEntity entity, string partialViewName, bool admin)
        {
            FillViewDataForViewing(controller, entity, partialViewName, admin);

            return new PartialViewResult()
            {
                ViewName = NormalControlUrl,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        private void FillViewDataForViewing(ControllerBase controller, IdentifiableEntity entity, string partialViewName, bool admin)
        { 
            Type type = entity.GetType();
            
            TypeContext tc = TypeContextUtilities.UntypedNew(entity, "");
            controller.ViewData.Model = tc; 

            controller.ViewData[ViewDataKeys.PartialViewName] = partialViewName ?? Navigator.OnPartialViewName(entity);
            
            if (!controller.ViewData.Keys.Any(s => s==ViewDataKeys.PageTitle))
                controller.ViewData[ViewDataKeys.PageTitle] = entity.ToStr;
            
            string tabID = GetOrCreateTabID(controller);
            controller.ViewData[ViewDataKeys.TabId] = tabID;

            if (!Navigator.IsNavigable(type, admin))
                throw new UnauthorizedAccessException(Resources.ViewForType0IsNotAllowed.Formato(type));

            if (Navigator.IsReadOnly(type, admin))
                tc.ReadOnly = true;

            bool useSessionWhenNew = GraphExplorer.FromRoot(entity).Any(m => (m as IIdentifiable).TryCS(i => i.IsNew) == true && m.GetType().HasAttribute<UseSessionWhenNew>());
            bool isReactive = GraphExplorer.FromRoot(entity).Any(m => m.GetType().HasAttribute<Reactive>());

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

            if (controller.ViewData.Keys.Count(s => s == ViewDataKeys.PageTitle) == 0)
                controller.ViewData[ViewDataKeys.PageTitle] = SearchTitle(findOptions.QueryName);
            
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
            
            if (controller.ViewData.Keys.Count(s => s == ViewDataKeys.PageTitle) == 0)
                controller.ViewData[ViewDataKeys.PageTitle] = SearchTitle(findOptions.QueryName);
            
            return new PartialViewResult
            {
                ViewName = SearchPopupControlUrl,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual string SearchTitle(object queryName)
        {
            if (QuerySettings != null)
            {
                QuerySettings qs = QuerySettings.TryGetC(queryName);
                if (qs != null && qs.Title != null)
                    return qs.Title;
            }

            return GetNiceQueryName(queryName);
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


        protected internal virtual Type ResolveTypeFromUrlName(string typeUrlName)
        {
            return URLNamesToTypes.GetOrThrow(typeUrlName, "No Type for url name {0}");
        }

        protected internal virtual object ResolveQueryFromUrlName(string queryUrlName)
        {
            if (UrlQueryNames.ContainsKey(queryUrlName)) 
                return UrlQueryNames[queryUrlName];

            //If it's the name of a Type
            if (Navigator.NamesToTypes.ContainsKey(queryUrlName))
            {
                return NamesToTypes[queryUrlName];
                //string urlName = TypesToURLNames[];
                //if (!string.IsNullOrEmpty(urlName))
                //    return UrlQueryNames[urlName].ThrowIfNullC("There's no query with name {0}".Formato(queryUrlName));
            }

            throw new ArgumentException("There's no query with name {0}".Formato(queryUrlName));
        }

        protected internal virtual object ResolveQueryFromKey(string queryNameKey)
        {
            return QuerySettings.Keys.Where(k => QueryUtils.GetQueryName(k) == queryNameKey).Single("No query with name {0}".Formato(queryNameKey));
        }

        protected internal virtual Type ResolveType(string typeName)
        {
            Type type = Navigator.NamesToTypes.TryGetC(typeName) ?? Type.GetType(typeName, false);

            if (type == null)
                throw new ArgumentException("Type {0} not found Navigator.NamesToTypes".Formato(typeName));

            
            return type;
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
                    (typeof(EmbeddedEntity).IsAssignableFrom(mod.GetType()) || ((IIdentifiable)mod).IdOrNull == parentRuntimeInfo.IdOrNull))
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
            if (QuerySettings == null)
                return false;

            return QuerySettings.ContainsKey(queryName);
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
            return new ContentResult { Content = modelStateData.ToString() };
        }

        internal ContentResult RedirectUrl(string url)
        {
            var dic = new
            { 
                jsonResultType = JsonResultType.Url.ToString(),
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
        Url,
        ModelState
    }
}
