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
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Engine.Operations;
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

        public const string NavigateRouteName = "sfView";

        public static string NavigateRoute(Type type, int? id)
        {
            var entitySettings = EntitySettings(type);
            if (entitySettings.ViewRoute != null)
                return entitySettings.ViewRoute(new UrlHelper(HttpContext.Current.Request.RequestContext), type, id);

            return new UrlHelper(HttpContext.Current.Request.RequestContext).RouteUrl(NavigateRouteName, new
            {
                webTypeName = EntitySettings(type).WebTypeName,
                id = id.TryToString()
            });
        }

        public static string NavigateRoute(IIdentifiable ie)
        {
            return NavigateRoute(ie.GetType(), ie.Id);
        }

        public static string NavigateRoute(Lite<IIdentifiable> lite)
        {
            return NavigateRoute(lite.EntityType, lite.Id);
        }

        public static RedirectResult RedirectToEntity(IIdentifiable ie)
        {
            return new RedirectResult(NavigateRoute(ie));
        }

        public static RedirectResult RedirectToEntity(Lite<IIdentifiable> lite)
        {
            return new RedirectResult(NavigateRoute(lite));
        }

        public const string FindRouteName = "sfFind";

        public static string FindRoute(object queryName)
        {
            return new UrlHelper(HttpContext.Current.Request.RequestContext).RouteUrl(FindRouteName, new
            {
                webQueryName = ResolveWebQueryName(queryName)
            });
        }

        public static ViewResult NormalPage(ControllerBase controller, IRootEntity entity)
        {
            return Manager.NormalPage(controller, new NavigateOptions(entity));
        }

        public static ViewResult NormalPage(ControllerBase controller, NavigateOptions options)
        {
            return Manager.NormalPage(controller, options);
        }
     
        public static PartialViewResult NormalControl(ControllerBase controller, IRootEntity entity)
        {
            return Manager.NormalControl(controller, new NavigateOptions(entity));
        }

        public static PartialViewResult NormalControl(ControllerBase controller, NavigateOptions options)
        {
            return Manager.NormalControl(controller, options);
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

        public static PartialViewResult PopupOpen(this ControllerBase controller, PopupOptionsBase viewOptions)
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

        public static Lite<IdentifiableEntity> FindUnique(FindUniqueOptions options)
        {
            return Manager.FindUnique(options);
        }

        public static int QueryCount(CountOptions options)
        {
            return Manager.QueryCount(options);
        }

        public static PartialViewResult Search(ControllerBase controller, QueryRequest request, bool allowMultiple, bool view, FilterMode filterMode, string prefix)
        {
            return Manager.Search(controller, request, allowMultiple, view, filterMode, new Context(null, prefix));
        }

        public static string SearchTitle(object queryName)
        {
            return Manager.SearchTitle(queryName);
        }

        public static void SetTokens(List<FilterOption> filters, QueryDescription queryDescription, bool canAggregate)
        {
            Manager.SetTokens(filters, queryDescription, canAggregate);
        }

        public static void SetTokens(List<OrderOption> orders, QueryDescription queryDescription, bool canAggregate)
        {
            Manager.SetTokens(orders, queryDescription, canAggregate);
        }

        public static void SetTokens(List<ColumnOption> columns, QueryDescription queryDescription, bool canAggregate)
        {
            Manager.SetTokens(columns, queryDescription, canAggregate);
        }

        public static void SetSearchViewableAndCreable(FindOptions findOptions)
        {
            Manager.SetSearchViewableAndCreable(findOptions);
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
            Mapping<T> mapping = (Mapping<T>)Navigator.EntitySettings(typeof(T)).Let(s => admin ? s.UntypedMappingMain : s.UntypedMappingLine);

            return Manager.ApplyChanges<T>(controllerContext, entity, prefix, mapping, inputs);
        }

        public static MappingContext<T> ApplyChanges<T>(this T entity, ControllerContext controllerContext, string prefix, bool admin, SortedList<string, string> inputs) where T : IRootEntity
        {
            Mapping<T> mapping = (Mapping<T>)Navigator.EntitySettings(typeof(T)).Let(s => admin ? s.UntypedMappingMain : s.UntypedMappingLine);

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

        public static List<Lite<T>> ParseLiteKeys<T>(string commaSeparatedLites) where T : class, IIdentifiable
        {
            return commaSeparatedLites.Split(',').Select(Lite.Parse<T>).ToList();
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

        public static bool IsFindable(object queryName)
        {
            return Manager.OnIsFindable(queryName);
        }

        public static bool IsCreable(Type type, bool isSearchEntity = false)
        {
            return Manager.OnIsCreable(type, isSearchEntity);
        }

        public static bool IsReadOnly(Type type)
        {
            return Manager.OnIsReadOnly(type, null);
        }

        public static bool IsReadOnly(ModifiableEntity entity)
        {
            return Manager.OnIsReadOnly(entity.GetType(), entity);
        }

        public static bool IsViewable(Type type)
        {
            return Manager.OnIsViewable(type, null);
        }

        public static bool IsViewable(ModifiableEntity entity)
        {
            return Manager.OnIsViewable(entity.GetType(), entity);
        }

        public static bool IsNavigable(Type type, bool isSearchEntity = false)
        {
            return Manager.OnIsNavigable(type, null, isSearchEntity);
        }

        public static bool IsNavigable(ModifiableEntity entity, bool isSearchEntity = false)
        {
            return Manager.OnIsNavigable(entity.GetType(), entity, isSearchEntity);
        }

        public static string OnPartialViewName(ModifiableEntity entity)
        {
            return EntitySettings(entity.GetType()).OnPartialViewName(entity); 
        }

        public static void RegisterArea(Type clientType)
        {
            if (!clientType.Name.EndsWith("Client"))
                throw new InvalidOperationException("The name of clientType should end with the convention 'Client'");

            RegisterArea(clientType, clientType.Name.RemoveEnd("Client".Length));
        }

        public static void RegisterArea(Type clientType, string areaName)
        {
            if (areaName.Start(1) == "/")
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

        internal static void AssertNotReadonly(IdentifiableEntity ident)
        {
            if (Navigator.IsReadOnly(ident))
                throw new UnauthorizedAccessException("{0} is read-only".Formato(ident));
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
        public string PopupCancelControlView = ViewPrefix.Formato("PopupCancelControl");
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
            "~/signum/Scripts/SF_Widgets.js",
            "~/signum/Scripts/SF_Operations.js"
        };
        public Func<List<string>> DefaultScripts = () => defaultScripts;

        public List<Func<UrlHelper, Dictionary<string, string>>> DefaultSFUrls = new List<Func<UrlHelper,Dictionary<string,string>>>
        {
            url => new Dictionary<string, string>
            {
                { "popupView", url.SignumAction("PopupView") },
                { "partialView", url.SignumAction("PartialView") },
                { "validate", url.SignumAction("Validate") },
                { "validatePartial", url.SignumAction("ValidatePartial") },
                { "trySave", url.SignumAction("TrySave") },
                { "trySavePartial", url.SignumAction("TrySavePartial") },
                { "find", url.SignumAction("Find") },
                { "partialFind", url.SignumAction("PartialFind") },
                { "search", url.SignumAction("Search") },
                { "subTokensCombo", url.SignumAction("NewSubTokensCombo") },
                { "addFilter", url.Action("AddFilter", "Signum") },
                { "quickFilter", url.SignumAction("QuickFilter") },
                { "selectedItemsContextMenu", url.SignumAction("SelectedItemsContextMenu") },
                { "create", url.SignumAction("Create") },
                { "popupNavigate", url.SignumAction("PopupNavigate") },
                { "typeChooser", url.SignumAction("GetTypeChooser") },
                { "autocomplete", url.SignumAction("Autocomplete") }
            }
        };

        public Dictionary<string, string> GetDefaultSFUrls(UrlHelper url) 
        {
            var urls = new Dictionary<string, string>();
            urls.AddRange(DefaultSFUrls.SelectMany(f => f(url)));
            return urls;
        }

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

        protected internal virtual ViewResult NormalPage(ControllerBase controller, NavigateOptions options)
        {
            FillViewDataForViewing(controller, options);

            return new ViewResult()
            {
                ViewName = NormalPageView,
                MasterName = null,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual PartialViewResult NormalControl(ControllerBase controller, NavigateOptions options)
        {
            FillViewDataForViewing(controller, options);

            return new PartialViewResult()
            {
                ViewName = NormalControlView,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        private void FillViewDataForViewing(ControllerBase controller, NavigateOptions options)
        {
            TypeContext tc = TypeContextUtilities.UntypedNew(options.Entity, "");
            controller.ViewData.Model = tc;

            var modifiable = (ModifiableEntity)options.Entity;

            controller.ViewData[ViewDataKeys.PartialViewName] = options.PartialViewName ?? Navigator.OnPartialViewName(modifiable);
            
            if (controller.ViewData[ViewDataKeys.TabId] == null)
                controller.ViewData[ViewDataKeys.TabId] = GetOrCreateTabID(controller);

            controller.ViewData[ViewDataKeys.ShowOperations] = options.ShowOperations;

            AssertViewableEntitySettings(modifiable);
            
            tc.ReadOnly = options.ReadOnly ?? Navigator.IsReadOnly(modifiable);
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

        protected internal virtual PartialViewResult PopupOpen(ControllerBase controller, PopupOptionsBase viewOptions)
        {
            TypeContext typeContext = TypeContextUtilities.CleanTypeContext(viewOptions.TypeContext);
            Type cleanType = typeContext.UntypedValue.GetType();

            ModifiableEntity entity = (ModifiableEntity)typeContext.UntypedValue;
            AssertViewableEntitySettings(entity);
            
            controller.ViewData.Model = typeContext;
            controller.ViewData[ViewDataKeys.PartialViewName] = viewOptions.PartialViewName ?? Navigator.OnPartialViewName(entity);

            bool isReadOnly = viewOptions.ReadOnly ?? Navigator.IsReadOnly(entity);
            if (isReadOnly)
                typeContext.ReadOnly = true;

            ViewButtons buttons = viewOptions.ViewButtons;
            controller.ViewData[ViewDataKeys.ViewButtons] = buttons;
            controller.ViewData[ViewDataKeys.OkVisible] = buttons == ViewButtons.Ok;
            controller.ViewData[ViewDataKeys.ShowOperations] = viewOptions.ShowOperations;
            if (buttons == ViewButtons.Ok)
            {
                controller.ViewData[ViewDataKeys.SaveProtected] = ((PopupViewOptions)viewOptions).SaveProtected ??
                    OperationLogic.IsSaveProtected(entity.GetType());
            }

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

            if (!Navigator.IsViewable(cleanType))
                throw new Exception(Resources.ViewForType0IsNotAllowed.Formato(cleanType.Name));

            controller.ViewData.Model = cleanTC;

            if (Navigator.IsReadOnly(cleanType))
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

            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);

            Navigator.SetTokens(findOptions.FilterOptions, queryDescription, canAggregate: false);
            SetSearchViewableAndCreable(findOptions);

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

        protected internal virtual Lite<IdentifiableEntity> FindUnique(FindUniqueOptions options)
        {
            var queryDescription = DynamicQueryManager.Current.QueryDescription(options.QueryName);

            SetTokens(options.FilterOptions, queryDescription, canAggregate: false);
            SetTokens(options.OrderOptions, queryDescription, canAggregate: false);

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
            var queryDescription = DynamicQueryManager.Current.QueryDescription(options.QueryName);

            SetTokens(options.FilterOptions, queryDescription, canAggregate: false);

            var request = new QueryCountRequest
            { 
                QueryName = options.QueryName,
                Filters = options.FilterOptions.Select(f => f.ToFilter()).ToList()
            };

            return DynamicQueryManager.Current.ExecuteQueryCount(request);
        }

        protected internal void SetTokens(List<FilterOption> filters, QueryDescription queryDescription, bool canAggregate)
        {
            foreach (var f in filters)
                f.Token = QueryUtils.Parse(f.ColumnName, queryDescription, canAggregate);
        }

        protected internal void SetTokens(List<OrderOption> orders, QueryDescription queryDescription, bool canAggregate)
        {
            foreach (var o in orders)
                o.Token = QueryUtils.Parse(o.ColumnName, queryDescription, canAggregate);
        }

        protected internal void SetTokens(List<ColumnOption> columns, QueryDescription queryDescription, bool canAggregate)
        {
            foreach (var o in columns)
                o.Token = QueryUtils.Parse(o.ColumnName, queryDescription, canAggregate);
        }

        protected internal virtual void SetSearchViewableAndCreable(FindOptions findOptions)
        {
            var queryDescription = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);
            var entityColumn = queryDescription.Columns.SingleEx(a => a.IsEntity);
            Type entitiesType = Lite.Extract(entityColumn.Type);
            Implementations? implementations = entityColumn.Implementations;

            if (findOptions.Navigate)
            {
                findOptions.Navigate = implementations.Value.IsByAll ? true : 
                    implementations.Value.Types.Any(t => Navigator.IsNavigable(t, true));
            }
            if (findOptions.Create)
            {
                findOptions.Create = findOptions.Navigate &&
                    (implementations.Value.IsByAll ? true : implementations.Value.Types.Any(t => Navigator.IsCreable(t, true)));
            }
        }
        
        protected internal virtual PartialViewResult PartialFind(ControllerBase controller, FindOptions findOptions, Context context)
        {
            if (!Navigator.IsFindable(findOptions.QueryName))
                throw new UnauthorizedAccessException(Resources.ViewForType0IsNotAllowed.Formato(findOptions.QueryName));

            SetSearchViewableAndCreable(findOptions);

            controller.ViewData.Model = context;
            controller.ViewData[ViewDataKeys.PartialViewName] = SearchControlView;
            
            controller.ViewData[ViewDataKeys.FindOptions] = findOptions;
            controller.ViewData[ViewDataKeys.QueryDescription] = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);
            
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

        protected internal virtual PartialViewResult Search(ControllerBase controller, QueryRequest request, bool allowMultiple, bool view, FilterMode filterMode, Context context)
        {
            if (!Navigator.IsFindable(request.QueryName))
                throw new UnauthorizedAccessException(Resources.ViewForType0IsNotAllowed.Formato(request.QueryName));

            ResultTable queryResult = DynamicQueryManager.Current.ExecuteQuery(request);
            
            controller.ViewData.Model = context;

            controller.ViewData[ViewDataKeys.AllowMultiple] = allowMultiple;
            controller.ViewData[ViewDataKeys.Navigate] = view;
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
            using (HeavyProfiler.Log("ApplyChanges", () => typeof(T).TypeName()))
            {
                RootContext<T> ctx = new RootContext<T>(prefix, inputs, controllerContext) { Value = entity };
                mapping(ctx);
                return ctx;
            }
        }

        protected internal virtual ModifiableEntity ExtractEntity(ControllerBase controller, string prefix)
        {
            NameValueCollection form = controller.ControllerContext.HttpContext.Request.Form;
            
            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(form[TypeContextUtilities.Compose(prefix ?? "", EntityBaseKeys.RuntimeInfo)]);
            if (runtimeInfo.IdOrNull != null)
                return Database.Retrieve(runtimeInfo.EntityType, runtimeInfo.IdOrNull.Value);
            else
                return (ModifiableEntity)Constructor.Construct(runtimeInfo.EntityType);
        }

        protected internal virtual Lite<T> ExtractLite<T>(ControllerBase controller, string prefix)
            where T:class, IIdentifiable
        {
            NameValueCollection form = controller.ControllerContext.HttpContext.Request.Form;
            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(form[TypeContextUtilities.Compose(prefix ?? "", EntityBaseKeys.RuntimeInfo)]);

            if (!runtimeInfo.IdOrNull.HasValue)
                throw new ArgumentException("Could not create a Lite without an Id");

            return (Lite<T>)Lite.Create(runtimeInfo.EntityType, runtimeInfo.IdOrNull.Value);
        }

        public event Func<Type, bool> IsCreable;

        internal protected virtual bool OnIsCreable(Type type, bool isSearchEntity)
        {
            EntitySettings es = EntitySettings.TryGetC(type);
            if (es == null)
                return true;

            if (!es.OnIsCreable(isSearchEntity))
                return false;


            if (IsCreable != null)
                foreach (Func<Type, bool> isCreable in IsCreable.GetInvocationList())
                {
                    if (!isCreable(type))
                        return false;
                }

            return true;
        }

        public event Func<Type, ModifiableEntity, bool> IsReadOnly;

        internal protected virtual bool OnIsReadOnly(Type type, ModifiableEntity entity)
        {
            EntitySettings es = EntitySettings.TryGetC(type);
            if (es != null)
            {
                if (es.OnIsReadonly())
                    return true;
            }

            if (IsReadOnly != null)
                foreach (Func<Type, ModifiableEntity, bool> isReadOnly in IsReadOnly.GetInvocationList())
                {
                    if (isReadOnly(type, entity))
                        return true;
                }

            return false;
        }

        public event Func<Type, ModifiableEntity, bool> IsViewable;

        protected virtual bool IsViewableBase(Type type, ModifiableEntity entity)
        {
            if (IsViewable != null)
            {
                foreach (Func<Type, ModifiableEntity, bool> isViewable in IsViewable.GetInvocationList())
                {
                    if (!isViewable(type, entity))
                        return false;
                }
            }

            return true;
        }

        internal protected virtual EntitySettings AssertViewableEntitySettings(ModifiableEntity entity)
        {
            EntitySettings es = EntitySettings.TryGetC(entity.GetType());
            if (es == null)
                throw new InvalidOperationException("No EntitySettings for type {0}".Formato(entity.GetType().Name));

            if (es.OnPartialViewName(entity) == null)
                throw new InvalidOperationException("No view has been set in the EntitySettings for {0}".Formato(entity.GetType().Name));

            if (!IsViewableBase(entity.GetType(), entity))
                throw new InvalidOperationException("Entities of type {0} are not viewable".Formato(entity.GetType().Name));

            return es;
        }

        internal protected virtual bool OnIsNavigable(Type type, ModifiableEntity entity, bool isSearchEntity)
        {
            EntitySettings es = EntitySettings.TryGetC(type);

            return es != null &&
                IsViewableBase(type, entity) &&
                es.OnIsNavigable(isSearchEntity);
        }

        internal protected virtual bool OnIsViewable(Type type, ModifiableEntity entity)
        {
            EntitySettings es = EntitySettings.TryGetC(type);

            return es != null &&
                IsViewableBase(type, entity) &&
                es.OnIsViewable();
        }

        public event Func<object, bool> IsFindable;

        internal protected virtual bool OnIsFindable(object queryName)
        {
            QuerySettings es = QuerySettings.TryGetC(queryName);
            if (es == null || !es.IsFindable)
                return false;

            if (IsFindable != null)
                foreach (Func<object, bool> isFindable in IsFindable.GetInvocationList())
                {
                    if (!isFindable(queryName))
                        return false;
                }

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
