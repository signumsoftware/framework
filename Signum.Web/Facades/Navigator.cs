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
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.UI;
using Signum.Services;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.Serialization;
using Microsoft.SqlServer.Types;
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

        public static PartialViewResult PartialView(this ControllerBase controller, TypeContext tc, string partialViewName)
        {
            return Manager.PartialView(controller, tc, partialViewName);
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

        public static PartialViewResult PartialFind(ControllerBase controller, FindOptions findOptions, FindMode mode, Context context)
        {
            return Manager.PartialFind(controller, findOptions, mode, context);
        }

        public static PartialViewResult PartialFind(ControllerBase controller, FindOptions findOptions, FindMode mode, string prefix)
        {
            return Manager.PartialFind(controller, findOptions, mode, new Context(null, prefix));
        }

        public static Lite<IdentifiableEntity> FindUnique(FindUniqueOptions options)
        {
            return Manager.FindUnique(options);
        }

        public static int QueryCount(CountOptions options)
        {
            return Manager.QueryCount(options);
        }

        public static PartialViewResult Search(ControllerBase controller, QueryRequest request, bool allowSelection, bool navigate, FilterMode filterMode, string prefix)
        {
            return Manager.Search(controller, request, allowSelection, navigate, filterMode, new Context(null, prefix));
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
            Navigator.Manager.EntitySettings.AddOrThrow(settings.StaticType, settings, "EntitySettings for {0} already registered");
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

        public static MappingContext UntypedApplyChanges(this ModifiableEntity entity, ControllerContext controllerContext, bool admin, string prefix = null, PropertyRoute route = null, SortedList<string, string> inputs = null)
        {
            return miApplyChanges.GetInvoker(entity.GetType()).Invoke(entity, controllerContext, admin, prefix, route, inputs);
        }

        static GenericInvoker<Func<ModifiableEntity, ControllerContext, bool, string, PropertyRoute, SortedList<string, string>, MappingContext>> miApplyChanges =
            new GenericInvoker<Func<ModifiableEntity, ControllerContext, bool, string, PropertyRoute, SortedList<string, string>, MappingContext>>((me, cc, ad, prefix, route, dic) => ApplyChanges<TypeDN>((TypeDN)me, cc, ad, prefix, route, dic));
        public static MappingContext<T> ApplyChanges<T>(this T entity, ControllerContext controllerContext, bool admin, string prefix = null, PropertyRoute route = null, SortedList<string, string> inputs = null) where T : ModifiableEntity
        {
            Mapping<T> mapping = (Mapping<T>)Navigator.EntitySettings(typeof(T)).Let(s => admin ? s.UntypedMappingMain : s.UntypedMappingLine);

            return ApplyChanges<T>(entity, controllerContext, mapping, prefix, route, inputs);
        }

        public static MappingContext<T> ApplyChanges<T>(this T entity, ControllerContext controllerContext, Mapping<T> mapping, string prefix = null,  PropertyRoute route = null, SortedList<string, string> inputs = null) where T : ModifiableEntity
        {
            if (prefix == null)
                prefix = controllerContext.Controller.Prefix();

            if (inputs == null)
                inputs = controllerContext.HttpContext.Request.Form.ToSortedList(prefix);

            if (route == null)
            {
                if (!typeof(IRootEntity).IsAssignableFrom(typeof(T)))
                    throw new InvalidOperationException("In order to use ApplyChanges with EmbeddedEntities set 'route' argument");

                route = PropertyRoute.Root(typeof(T));
            }

            return Manager.ApplyChanges<T>(controllerContext, entity, prefix, mapping, route, inputs);
        }

        public static string Prefix(this ControllerBase controller)
        {
            return controller.ControllerContext.HttpContext.Request["prefix"];
        }

        public static ModifiableEntity UntypedExtractEntity(this ControllerBase controller, string prefix = null)
        {
            return Manager.ExtractEntity(controller, prefix ?? controller.Prefix());
        }


        public static ValueLineBoxModel ExtractValueLineBox(this ControllerBase controller)
        {
            var valueLinePrefix = controller.ControllerContext.HttpContext.Request["valueLinePrefix"];
            return controller.ExtractEntity<ValueLineBoxModel>(valueLinePrefix)
             .ApplyChanges(controller.ControllerContext, true, valueLinePrefix).Value;
        }

        public static T ExtractEntity<T>(this ControllerBase controller, string prefix = null) where T : ModifiableEntity
        {
            return (T)Manager.ExtractEntity(controller, prefix ?? controller.Prefix());
        }

        public static Lite<T> ExtractLite<T>(this ControllerBase controller, string prefix = null) where T : class, IIdentifiable
        {
            return (Lite<T>)Manager.ExtractLite<T>(controller, prefix ?? controller.Prefix());
        }

        public static List<Lite<T>> ParseLiteKeys<T>(this ControllerBase controller) where T : class, IIdentifiable
        {
            return ParseLiteKeys<T>(controller.ControllerContext.RequestContext.HttpContext.Request["liteKeys"]);
        }

        public static List<Lite<T>> ParseLiteKeys<T>(string liteKeys) where T : class, IIdentifiable
        {
            return liteKeys.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(Lite.Parse<T>).ToList();
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

        public static bool IsViewable(Type type, string partialViewName)
        {
            return Manager.OnIsViewable(type, null, partialViewName);
        }

        public static bool IsViewable(ModifiableEntity entity, string partialViewName)
        {
            return Manager.OnIsViewable(entity.GetType(), entity, partialViewName);
        }

        public static bool IsNavigable(Type type, string partialViewName, bool isSearchEntity = false)
        {
            return Manager.OnIsNavigable(type, null, partialViewName, isSearchEntity);
        }

        public static bool IsNavigable(ModifiableEntity entity, string partialViewName, bool isSearchEntity = false)
        {
            return Manager.OnIsNavigable(entity.GetType(), entity, partialViewName, isSearchEntity);
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
        public string PaginationSelectorView = ViewPrefix.Formato("PaginationSelector");
        public string ValueLineBoxView = ViewPrefix.Formato("ValueLineBox");
        
        protected Dictionary<string, Type> WebTypeNames { get; private set; }
        protected Dictionary<string, object> WebQueryNames { get; private set; }

        public Func<bool> AllowChangeColumns = () => true;
      
        public NavigationManager(string entityStateKeyToHash)
            : this(new MD5CryptoServiceProvider().Using(p => p.ComputeHash(UTF8Encoding.UTF8.GetBytes(entityStateKeyToHash))))
        {
        }

        public NavigationManager(byte[] entityStateKey)
        {
            EntitySettings = new Dictionary<Type, EntitySettings>();
            QuerySettings = new Dictionary<object, QuerySettings>();
            EntityStateKey = entityStateKey;

            formatter = new NetDataContractSerializer();

            var ss = new SurrogateSelector();
            ss.AddSurrogate(typeof(SqlHierarchyId),
                new StreamingContext(StreamingContextStates.All),
                new SqlHierarchySurrogate());

            formatter.SurrogateSelector = ss;
        }

        class SqlHierarchySurrogate : ISerializationSurrogate 
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                SqlHierarchyId shi = (SqlHierarchyId)obj;
                info.AddValue("route", shi.ToString());
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                return SqlHierarchyId.Parse(info.GetString("route"));
            }
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

                Navigator.RegisterArea(typeof(Navigator), "Signum");
                FileRepositoryManager.Register(new LocalizedJavaScriptRepository(typeof(JavascriptMessage), "Signum"));
                FileRepositoryManager.Register(new CalendarLocalizedJavaScriptRepository("~/Signum/calendarResources/"));
                FileRepositoryManager.Register(new UrlsRepository("~/Signum/urls/"));



                Schema.Current.ApplicationName = System.Web.Hosting.HostingEnvironment.ApplicationHost.GetPhysicalPath();

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

            var entity = (ModifiableEntity)options.Entity;

            controller.ViewData[ViewDataKeys.PartialViewName] = options.PartialViewName ?? Navigator.OnPartialViewName(entity);
            tc.ViewOverrides = Navigator.EntitySettings(entity.GetType()).ViewOverrides;

            if (controller.ViewData[ViewDataKeys.TabId] == null)
                controller.ViewData[ViewDataKeys.TabId] = GetOrCreateTabID(controller);

            controller.ViewData[ViewDataKeys.ShowOperations] = options.ShowOperations;

            AssertViewableEntitySettings(entity);

            tc.ReadOnly = options.ReadOnly ?? Navigator.IsReadOnly(entity);
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
                return LiteMessage.New.NiceToString().ForGenderAndNumber(ident.GetType().GetGender()) + " " + niceName;
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
            typeContext.ViewOverrides = Navigator.EntitySettings(entity.GetType()).ViewOverrides;

            bool isReadOnly = viewOptions.ReadOnly ?? Navigator.IsReadOnly(entity);
            if (isReadOnly)
                typeContext.ReadOnly = true;

            ViewMode mode = viewOptions.ViewMode;
            controller.ViewData[ViewDataKeys.ViewMode] = mode;
            controller.ViewData[ViewDataKeys.ShowOperations] = viewOptions.ShowOperations;
            if (mode == ViewMode.View)
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

            if (!Navigator.IsViewable(cleanType, partialViewName))
                throw new Exception(NormalControlMessage.ViewForType0IsNotAllowed.NiceToString().Formato(cleanType.Name));

            controller.ViewData.Model = cleanTC;

            if (Navigator.IsReadOnly(cleanType))
                cleanTC.ReadOnly = true;

            cleanTC.ViewOverrides = Navigator.EntitySettings(cleanType).ViewOverrides;

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
                throw new UnauthorizedAccessException(SearchMessage.Query0IsNotAllowed.NiceToString().Formato(findOptions.QueryName));

            QueryDescription description = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);

            //Navigator.SetTokens(findOptions.FilterOptions, description, canAggregate: false);
            SetSearchViewableAndCreable(findOptions, description);
            SetDefaultOrder(findOptions, description);

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

        public virtual void SetSearchViewableAndCreable(FindOptions findOptions, QueryDescription description)
        {
            var entityColumn = description.Columns.SingleEx(a => a.IsEntity);
            Type entitiesType = Lite.Extract(entityColumn.Type);
            Implementations? implementations = entityColumn.Implementations;

            if (findOptions.Navigate)
            {
                findOptions.Navigate = implementations.Value.IsByAll ? true : 
                    implementations.Value.Types.Any(t => Navigator.IsNavigable(t, null, true));
            }
            if (findOptions.Create)
            {
                findOptions.Create = findOptions.Navigate &&
                    (implementations.Value.IsByAll ? true : implementations.Value.Types.Any(t => Navigator.IsCreable(t, true)));
            }
        }

        public virtual void SetDefaultOrder(FindOptions findOptions, QueryDescription description)
        {
            var entityColumn = description.Columns.SingleOrDefaultEx(cd => cd.IsEntity);

            if (findOptions.OrderOptions.IsNullOrEmpty() && !entityColumn.Implementations.Value.IsByAll)
            {
                var orderType = entityColumn.Implementations.Value.Types.All(t => EntityKindCache.GetEntityData(t) == EntityData.Master) ? OrderType.Ascending : OrderType.Descending;

                var column = description.Columns.SingleOrDefaultEx(c => c.Name == "Id");

                if (column != null)
                {
                    findOptions.OrderOptions.Add(new OrderOption{ Token = new ColumnToken(column, description.QueryName), ColumnName = column.Name, OrderType = orderType });
                }
            }
        }
        
        protected internal virtual PartialViewResult PartialFind(ControllerBase controller, FindOptions findOptions, FindMode mode, Context context)
        {
            if (!Navigator.IsFindable(findOptions.QueryName))
                throw new UnauthorizedAccessException(NormalControlMessage.ViewForType0IsNotAllowed.NiceToString().Formato(findOptions.QueryName));

            var desc =  DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);

            SetSearchViewableAndCreable(findOptions, desc);
            SetDefaultOrder(findOptions, desc);

            controller.ViewData.Model = context;
            controller.ViewData[ViewDataKeys.PartialViewName] = SearchControlView;

            controller.ViewData[ViewDataKeys.FindMode] = mode;
            controller.ViewData[ViewDataKeys.FindOptions] = findOptions;
            controller.ViewData[ViewDataKeys.QueryDescription] = desc;
            
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

        protected internal virtual PartialViewResult Search(ControllerBase controller, QueryRequest request, bool allowSelection, bool navigate, FilterMode filterMode, Context context)
        {
            if (!Navigator.IsFindable(request.QueryName))
                throw new UnauthorizedAccessException(NormalControlMessage.ViewForType0IsNotAllowed.NiceToString().Formato(request.QueryName));

            ResultTable queryResult = DynamicQueryManager.Current.ExecuteQuery(request);
            
            controller.ViewData.Model = context;

            controller.ViewData[ViewDataKeys.AllowSelection] = allowSelection;
            controller.ViewData[ViewDataKeys.Navigate] = navigate;
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

        protected internal virtual MappingContext<T> ApplyChanges<T>(ControllerContext controllerContext, T entity, string prefix, Mapping<T> mapping, PropertyRoute route, SortedList<string, string> inputs) where T: ModifiableEntity
        {
            using (HeavyProfiler.Log("ApplyChanges", () => typeof(T).TypeName()))
            using (new EntityCache(EntityCacheType.Normal))
            {
                EntityCache.AddFullGraph((ModifiableEntity)entity);

                RootContext<T> ctx = new RootContext<T>(prefix, inputs, route, controllerContext) { Value = entity };
                mapping(ctx);
                return ctx;
            }
        }

        protected internal virtual ModifiableEntity ExtractEntity(ControllerBase controller, string prefix)
        {
            NameValueCollection form = controller.ControllerContext.HttpContext.Request.Form;

            var state = form[TypeContextUtilities.Compose(prefix, EntityBaseKeys.EntityState)];

            if (state.HasText())
                return Navigator.Manager.DeserializeEntity(state);


            var key = TypeContextUtilities.Compose(prefix, EntityBaseKeys.RuntimeInfo);

            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(form[key]);

            if (runtimeInfo == null)
                throw new ArgumentNullException("{0} not found in form request".Formato(key));

            if (runtimeInfo.IdOrNull != null)
                return Database.Retrieve(runtimeInfo.EntityType, runtimeInfo.IdOrNull.Value);
            else
                return (ModifiableEntity)Constructor.Construct(runtimeInfo.EntityType);
        }

        protected internal virtual Lite<T> ExtractLite<T>(ControllerBase controller, string prefix)
            where T:class, IIdentifiable
        {
            NameValueCollection form = controller.ControllerContext.HttpContext.Request.Form;
            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(form[TypeContextUtilities.Compose(prefix, EntityBaseKeys.RuntimeInfo)]);

            if (!runtimeInfo.IdOrNull.HasValue)
                throw new ArgumentException("Could not create a Lite without an Id");

            return (Lite<T>)runtimeInfo.ToLite();
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

        internal protected virtual bool OnIsNavigable(Type type, ModifiableEntity entity, string partialViewName, bool isSearchEntity)
        {
            EntitySettings es = EntitySettings.TryGetC(type);

            return es != null &&
                IsViewableBase(type, entity) &&
                es.OnIsNavigable(partialViewName, isSearchEntity);
        }

        internal protected virtual bool OnIsViewable(Type type, ModifiableEntity entity, string partialViewName)
        {
            EntitySettings es = EntitySettings.TryGetC(type);

            return es != null &&
                IsViewableBase(type, entity) &&
                es.OnIsViewable(partialViewName);
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

        public byte[] EntityStateKey;

        public string SerializeEntity(ModifiableEntity entity)
        {
            using (HeavyProfiler.LogNoStackTrace("SerializeEntity"))
            {
                var array = new MemoryStream().Using(ms =>
                {
                    using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress))
                        formatter.Serialize(ds, entity);
                    return ms.ToArray();
                });

                if (EntityStateKey != null)
                    array = Encrypt(array);

                return Convert.ToBase64String(array); 
            }
        }

        public IFormatter Formatter { get { return formatter; } }

        IFormatter formatter;

        public ModifiableEntity DeserializeEntity(string viewState)
        {
            using (HeavyProfiler.LogNoStackTrace("DeserializeEntity"))
            {
                var array = Convert.FromBase64String(viewState);

                if (EntityStateKey != null)
                    array = Decrypt(array);

                using (var ms = new MemoryStream(array))
                using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                    return (ModifiableEntity)formatter.Deserialize(ds);
            }
        }

        //http://stackoverflow.com/questions/8041451/good-aes-initialization-vector-practice
        byte[] Encrypt(byte[] toEncryptBytes)
        {
            using (var provider = new AesCryptoServiceProvider())
            {
                provider.Key = EntityStateKey;
                provider.Mode = CipherMode.CBC;
                provider.Padding = PaddingMode.PKCS7;
                using (var encryptor = provider.CreateEncryptor(provider.Key, provider.IV))
                {
                    using (var ms = new MemoryStream())
                    {
                        ms.Write(provider.IV, 0, provider.IV.Length);
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(toEncryptBytes, 0, toEncryptBytes.Length);
                            cs.FlushFinalBlock();
                        }
                        return ms.ToArray();
                    }
                }
            }
        }

        byte[] Decrypt(byte[] encryptedString)
        {
            using (var provider = new AesCryptoServiceProvider())
            {
                provider.Key = EntityStateKey;
                using (var ms = new MemoryStream(encryptedString))
                {
                    // Read the first 16 bytes which is the IV.
                    byte[] iv = new byte[16];
                    ms.Read(iv, 0, 16);
                    provider.IV = iv;

                    using (var decryptor = provider.CreateDecryptor())
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            return cs.ReadAllBytes();
                        }
                    }
                }
            }
        }


    }

    public enum JsonResultType
    {
        url,
        ModelState
    }

    public static class JsonAction
    {
        public static ActionResult RedirectHttpOrAjax(this ControllerBase controller,string url)
        {
            if (controller.ControllerContext.HttpContext.Request.IsAjaxRequest())
                return RedirectAjax(url);
            else
                return new RedirectResult(url);
        }

        public static JsonResult RedirectAjax(string url)
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
                result.Add(EntityBaseKeys.Link, newToStringLink);

            return new JsonResult { Data = result };
        }
    }
}
