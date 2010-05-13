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
#endregion

namespace Signum.Web
{
    public delegate bool IsViewableEvent(Type type, bool admin);

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

        public static void Start()
        {
            Manager.Start();
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

        public static object ResolveQueryFromToStr(string queryNameToStr)
        {
            return Manager.ResolveQueryFromToStr(queryNameToStr);
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

            if (!form.AllKeys.Contains(ViewDataKeys.TabId))
                throw new InvalidOperationException(Resources.RequestDoesntHaveNecessaryTabIdentifier);
            
            string tabID = (string)form[ViewDataKeys.TabId];
            if (!tabID.HasText())
                throw new InvalidOperationException(Resources.RequestDoesntHaveNecessaryTabIdentifier);
            
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

        internal static List<Filter> ExtractFilters(HttpContextBase httpContext, object queryName)
        {
            return Manager.ExtractFilters(httpContext, queryName);
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
            Navigator.Manager.EntitySettings.Add(settings.StaticType, settings);
        }

        public static void AddSettings(List<EntitySettings> settings)
        {
            Navigator.Manager.EntitySettings.AddRange(settings.ToDictionary(s => s.StaticType)); 
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

        internal static void ConfigureEntityBase(EntityBase eb, Type entityType, bool admin)
        {
            if (Manager.EntitySettings.ContainsKey(entityType))
            {
                eb.Create = Navigator.IsCreable(entityType, admin);
                eb.View = Navigator.IsViewable(entityType, admin);
                eb.Find = Navigator.IsFindable(entityType);
            }
            EntityLine el = eb as EntityLine;
            if (el != null)
                el.Navigate = Navigator.IsNavigable(entityType, admin);
        }

        public static bool IsNavigable(Type type, bool admin)
        {
            return Manager.IsNavigable(type, admin);
        }
        public static bool IsViewable(Type type, bool admin)
        {
            return Manager.IsViewable(type, admin);
        }
        public static bool IsReadOnly(Type type)
        {
            return Manager.IsReadOnly(type);
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

        public static string OnPartialViewName(ModifiableEntity entity)
        {
            return Manager.EntitySettings.GetOrThrow(entity.GetType(), Resources.TheresNotAViewForType0).OnPartialViewName(entity); 
        }

        internal static bool AnyDelegate<T>(this Func<T, bool> func, T val)
        {
            foreach (Func<T, bool> f in func.GetInvocationList())
            {
                if (f(val))
                    return true;
            }

            return false;
        }

        internal static bool AllDelegate<T>(this Func<T, bool> func, T val)
        {
            foreach (Func<T, bool> f in func.GetInvocationList())
            {
                if (!f(val))
                    return false;
            }

            return true;
        }

        public static string GetName(Type type)
        {
            return TypesToNames.GetOrThrow(type, "{0} not registered. Call Navigator.RegisterTypeName");
        }
    }
    
    public class NavigationManager
    {
        public Dictionary<Type, EntitySettings> EntitySettings;
        public Dictionary<object, QuerySettings> QuerySettings;

        public static string ViewsPrefix = "~/Plugin/Signum.Web.dll/Signum.Web.Views.";

        public string AjaxErrorPageUrl = ViewsPrefix + "AjaxError.ascx";
        public string ErrorPageUrl = ViewsPrefix + "Error.aspx";
        public string NormalPageUrl = ViewsPrefix + "NormalPage.aspx";
        public string NormalControlUrl = ViewsPrefix + "NormalControl.ascx";
        public string PopupControlUrl = ViewsPrefix + "PopupControl.ascx";
        public string ChooserPopupUrl = ViewsPrefix + "ChooserPopup.ascx";
        public string SearchPopupControlUrl = ViewsPrefix + "SearchPopupControl.ascx";
        public string SearchWindowUrl = ViewsPrefix + "SearchWindow.aspx";
        public string SearchControlUrl = ViewsPrefix + "SearchControl.ascx";
        public string SearchResultsUrl = ViewsPrefix + "SearchResults.ascx";
        public string FilterBuilderUrl = ViewsPrefix + "FilterBuilder.ascx";
        public string ValueLineBoxUrl = ViewsPrefix + "ValueLineBox.ascx";
        
        protected internal Dictionary<string, Type> URLNamesToTypes { get; private set; }
        protected internal Dictionary<Type, string> TypesToURLNames { get; private set; }
        protected internal Dictionary<string, Type> NamesToTypes { get; private set; }
        protected internal Dictionary<Type, string> TypesToNames { get; private set; }

        protected internal Dictionary<string, object> UrlQueryNames { get; private set; }
        //protected internal Dictionary<string, Type> FullNamesToTypes { get; private set; }

        public event Func<Type, bool> GlobalIsCreable;
        public event Func<Type, bool> GlobalIsViewable;
        public event Func<Type, bool> GlobalIsNavigable;
        public event Func<Type, bool> GlobalIsReadOnly;
        public event Func<object, bool> GlobalIsFindable;

        public NavigationManager()
        {
            TypesToNames = new Dictionary<Type, string>();
        }

        public void Start()
        {
            Navigator.AddSetting(new EntitySettings<ValueLineBoxModel>(EntityType.Default) { PartialViewName = _ => ValueLineBoxUrl });

            TypesToURLNames = EntitySettings.SelectDictionary(k => k, (k, v) => v.UrlName ?? Reflector.CleanTypeName(k));
            URLNamesToTypes = TypesToURLNames.Inverse(StringComparer.InvariantCultureIgnoreCase, "URLNamesToTypes");

            TypesToNames.AddRange(EntitySettings, kvp => kvp.Key, kvp => kvp.Value.TypeName ?? kvp.Key.Name, "TypeToNames");
            NamesToTypes = TypesToNames.Inverse("NamesToTypes");

            if (DynamicQueryManager.Current != null)
            {
                if (QuerySettings == null)
                    QuerySettings = new Dictionary<object, QuerySettings>();
                foreach (object o in DynamicQueryManager.Current.GetQueryNames())
                {
                    if (!QuerySettings.ContainsKey(o))
                        QuerySettings.Add(o, new QuerySettings() { Top = 50});
                    if (!QuerySettings[o].UrlName.HasText())
                        QuerySettings[o].UrlName = GetQueryName(o);
                }

                UrlQueryNames = QuerySettings.ToDictionary(kvp => kvp.Value.UrlName ?? GetQueryName(kvp.Key), kvp => kvp.Key, StringComparer.InvariantCultureIgnoreCase, "UrlQueryNames");
            }

            ModelBinders.Binders.DefaultBinder = new LiteModelBinder();
            ModelBinders.Binders.Add(typeof(Implementations), new ImplementationsModelBinder());

            Started = true;
        }

        public bool Started { get; private set; } 

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

            if (Navigator.IsReadOnly(type))
                tc.ReadOnly = true;

            if (controller.IsReactive() || GraphExplorer.FromRoot(entity).Any(m => m.GetType().HasAttribute<Reactive>()))
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
            
            if (Navigator.IsReadOnly(cleanType))
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
                throw new UnauthorizedAccessException(Resources.ViewForType0IsNotAllowed.Formato(findOptions.QueryName));

            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);

            foreach (FilterOption opt in findOptions.FilterOptions)
                opt.Token = QueryToken.Parse(queryDescription, opt.ColumnName);

            controller.ViewData.Model = new Context(null, "");

            controller.ViewData[ViewDataKeys.PartialViewName] = SearchControlUrl;

            controller.ViewData[ViewDataKeys.QueryDescription] = queryDescription;
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

            var filters = options.FilterOptions.Select(f => f.ToFilter()).ToList();
            var orders = options.OrderOptions.Select(o => o.ToOrder()).ToList();

            return DynamicQueryManager.Current.ExecuteUniqueEntity(options.QueryName, filters, orders, options.UniqueType);
        }

        protected internal virtual int QueryCount(CountOptions options)
        {
            SetTokens(options.QueryName, options.FilterOptions);

            var filters = options.FilterOptions.Select(f => f.ToFilter()).ToList();

            return DynamicQueryManager.Current.ExecuteQueryCount(options.QueryName, filters);
        }

        protected internal void SetTokens(object queryName, List<FilterOption> filters)
        {
            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(queryName);

            Column entity = queryDescription.StaticColumns.SingleOrDefault(a => a.IsEntity);

            foreach (var f in filters)
            {
                f.Token = QueryToken.Parse(queryDescription, f.ColumnName);
            }
        }

        public void SetTokens(object queryName, IEnumerable<OrderOption> orders)
        {
            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(queryName);

            foreach (var o in orders)
            {
                o.Token = QueryToken.Parse(queryDescription, o.ColumnName);
            }
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

            var filters = findOptions.FilterOptions.Select(fo => fo.ToFilter()).ToList();
            var orders = findOptions.OrderOptions.Select(fo => fo.ToOrder()).ToList();

            ResultTable queryResult = DynamicQueryManager.Current.ExecuteQuery(findOptions.QueryName, null, filters, orders, top);

            controller.ViewData.Model = context;
            
            controller.ViewData[ViewDataKeys.FindOptions] = findOptions;
            controller.ViewData[ViewDataKeys.QueryDescription] = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);
            
            controller.ViewData[ViewDataKeys.Results] = queryResult;
            
            if (queryResult != null && queryResult.Rows != null && queryResult.Rows.Length > 0 && queryResult.VisibleColumns.Count() > 0)
            {
                int entityColumnIndex = queryResult.Columns.OfType<StaticColumn>().IndexOf(c => c.IsEntity);
                controller.ViewData[ViewDataKeys.EntityColumnIndex] = entityColumnIndex;
            }

            QuerySettings settings = QuerySettings[findOptions.QueryName];
            controller.ViewData[ViewDataKeys.Formatters] = queryResult.Columns.Select(c =>settings.GetFormatter(c)).ToList();

            return new PartialViewResult
            {
                ViewName = SearchResultsUrl,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual List<Filter> ExtractFilters(HttpContextBase httpContext, object queryName)
        {
            List<Filter> result = new List<Filter>();

            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(queryName);

            int index = 0;
            string name;
            object value;
            string operation;
            Type type;
            NameValueCollection parameters = httpContext.Request.Params;
            var names = parameters.AllKeys.Where(k => k.StartsWith("cn"));
            foreach(string nameKey in names)
            {
                if (!int.TryParse(nameKey.RemoveLeft(2), out index))
                    continue;

                name = parameters[nameKey];
                value = parameters["val" + index.ToString()];
                operation = parameters["sel" + index.ToString()];
                type = queryDescription.StaticColumns
                           .SingleOrDefault(c => c.Name == name)
                           .ThrowIfNullC(Resources.InvalidFilterColumn0NotFound.Formato(name))
                           .Type;

                if (type == typeof(bool))
                {
                    string[] vals = ((string)value).Split(',');
                    value = (vals[0] == "true") ? true : false;
                }

                if (typeof(Lite).IsAssignableFrom(type))
                {
                    string[] vals = ((string)value).Split(';');
                    int intValue;
                    if (vals[0].HasText() && int.TryParse(vals[0], out intValue))
                    {
                        Type liteType = Navigator.NamesToTypes[vals[1]];
                        if (typeof(Lite).IsAssignableFrom(liteType))
                            liteType = Reflector.ExtractLite(liteType);
                        value = Lite.Create(liteType, intValue);
                    }
                    else
                        value = null;
                }
                FilterOperation filterOperation = ((FilterOperation[])Enum.GetValues(typeof(FilterOperation))).SingleOrDefault(op => op.ToString() == operation);

                result.Add(new Filter
                {
                    Token = QueryToken.Parse(queryDescription, name),
                    Operation = filterOperation,
                    Value = value,
                });
            }
            return result;
        }


        protected internal virtual Type ResolveTypeFromUrlName(string typeUrlName)
        {
            return URLNamesToTypes
                .TryGetC(typeUrlName)
                .ThrowIfNullC(Resources.NoTypeForUrlName0.Formato(typeUrlName));
        }

        protected internal virtual object ResolveQueryFromUrlName(string queryUrlName)
        {
            if (UrlQueryNames.ContainsKey(queryUrlName)) 
                return UrlQueryNames[queryUrlName];

            //If it's the name of a Type
            if (Navigator.NamesToTypes.ContainsKey(queryUrlName))
            {
                string urlName = TypesToURLNames[Navigator.NamesToTypes[queryUrlName]];
                if (!string.IsNullOrEmpty(urlName))
                    return UrlQueryNames[urlName].ThrowIfNullC(Resources.NoQueryWithName0.Formato(queryUrlName));
            }

            throw new ArgumentException(Resources.NoQueryWithName0.Formato(queryUrlName));
        }

        protected internal virtual object ResolveQueryFromToStr(string queryNameToStr)
        {
            return DynamicQueryManager.Current.GetQueryNames()
                .SingleOrDefault(qn => qn.ToString() == queryNameToStr)
                .ThrowIfNullC(Resources.NoQueryWithName0.Formato(queryNameToStr));
        }

        protected internal virtual Type ResolveType(string typeName)
        {
            Type type = Navigator.NamesToTypes.TryGetC(typeName) ?? Type.GetType(typeName, false);

            if (type == null)
                throw new ArgumentException(Resources.Type0NotFoundInNavigatorNamesToTypes.Formato(typeName));

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
            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(form[TypeContextUtilities.Compose(prefix ?? "", EntityBaseKeys.RuntimeInfo)]);
            
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
            if (GlobalIsViewable != null && !GlobalIsViewable.AllDelegate(type))
                return false;

            EntitySettings es = EntitySettings.TryGetC(type);
            if (es == null || es.HasPartialViewName)
                return false;

            if (es.IsViewable == null)
                return true;

            return es.IsViewable(admin);
        }

        protected internal virtual bool IsNavigable(Type type, bool admin)
        {
            if (typeof(EmbeddedEntity).IsAssignableFrom(type))
                return false;

            if (GlobalIsNavigable != null && !GlobalIsNavigable.AllDelegate(type))
                return false;

            EntitySettings es = EntitySettings.TryGetC(type);
            if (es == null || es.HasPartialViewName)
                return false;

            if (es.IsNavigable == null)
                return true;

            return es.IsNavigable(admin);
        }

        protected internal virtual bool IsReadOnly(Type type)
        {
            if (GlobalIsReadOnly != null && GlobalIsReadOnly.AnyDelegate(type))
                return true;

            EntitySettings es = EntitySettings.TryGetC(type);
            if (es == null)
                return false;

            return es.IsReadOnly;
        }

        protected internal virtual bool IsCreable(Type type, bool admin)
        {
            if (GlobalIsCreable != null && !GlobalIsCreable.AllDelegate(type))
                return false;

            EntitySettings es = EntitySettings.TryGetC(type);
            if (es == null || es.IsCreable == null)
                return true;

            return es.IsCreable(admin);
        }

        protected internal virtual bool IsFindable(object queryName)
        {
            if (GlobalIsFindable != null && !GlobalIsFindable.AllDelegate(queryName))
                return false;

            if (QuerySettings == null)
                return false;

            return QuerySettings.ContainsKey(queryName);
        }

        public virtual bool ShowOkSave(Type type, bool admin)
        {
            EntitySettings es = EntitySettings.TryGetC(type);
            if (es != null && es.ShowOkSave != null)
                return es.ShowOkSave(admin);

            return true;
        }

        public virtual bool ShowSearchOkButton(object queryName, bool admin)
        {
            QuerySettings qs = QuerySettings.TryGetC(queryName);
            if (qs != null && qs.ShowOkButton != null)
                return qs.ShowOkButton(admin);

            return true;
        }

        internal ContentResult ModelState(ModelStateData modelStateData)
        {
            return new ContentResult { Content = modelStateData.ToString() };
        }
    }
}
