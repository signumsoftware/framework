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

        public static ViewResult View(this Controller controller, object obj)
        {
            return Manager.View(controller, obj, null, null); 
        }

        public static ViewResult View(this Controller controller, object obj, string partialViewName)
        {
            return Manager.View(controller, obj, partialViewName, null);
        }

        public static ViewResult View(this Controller controller, object obj, Dictionary<string, long> changeTicks)
        {
            return Manager.View(controller, obj, null, changeTicks);
        }

        public static ViewResult View(this Controller controller, object obj, string partialViewName, Dictionary<string, long> changeTicks)
        {
            return Manager.View(controller, obj, partialViewName, changeTicks);
        }

        public static string GetOrCreateTabID(Controller c)
        {
            return Manager.GetOrCreateTabID(c);
        }

        public static bool ExtractIsReactive(NameValueCollection form)
        {
            return Manager.ExtractIsReactive(form);
        }

        public static string ExtractTabID(NameValueCollection form)
        {
            return Manager.ExtractTabID(form);
        }

        public static PartialViewResult PopupView<T>(this Controller controller, T entity, string prefix)
        {
            return Manager.PopupView(controller, entity, prefix, null, null);
        }

        public static PartialViewResult PopupView<T>(this Controller controller, T entity, string prefix, string partialViewName)
        {
            return Manager.PopupView(controller, entity, prefix, partialViewName, null);
        }

        public static PartialViewResult PopupView<T>(this Controller controller, T entity, string prefix, Dictionary<string, long> changeTicks)
        {
            return Manager.PopupView(controller, entity, prefix, null, changeTicks);
        }

        public static PartialViewResult PopupView<T>(this Controller controller, T entity, string prefix, string partialViewName, Dictionary<string, long> changeTicks)
        {
            return Manager.PopupView(controller, entity, prefix, partialViewName, changeTicks);
        }

        public static PartialViewResult PartialView<T>(this Controller controller, T entity, string prefix)
        {
            return Manager.PartialView(controller, entity, prefix, null, null);
        }

        public static PartialViewResult PartialView<T>(this Controller controller, T entity, string prefix, string partialViewName)
        {
            return Manager.PartialView(controller, entity, prefix, partialViewName, null);
        }

        public static PartialViewResult PartialView<T>(this Controller controller, T entity, string prefix, Dictionary<string, long> changeTicks)
        {
            return Manager.PartialView(controller, entity, prefix, null, changeTicks);
        }

        public static PartialViewResult PartialView<T>(this Controller controller, T entity, string prefix, string partialViewName, Dictionary<string, long> changeTicks)
        {
            return Manager.PartialView(controller, entity, prefix, partialViewName, changeTicks);
        }

        public static ViewResult Find(Controller controller, object queryName)
        {
            return Find(controller, new FindOptions(queryName));
        }

        public static ViewResult Find(Controller controller, FindOptions findOptions)
        {
            return Manager.Find(controller, findOptions);
        }

        public static PartialViewResult PartialFind(Controller controller, FindOptions findOptions, string prefix, string suffix)
        {
            return Manager.PartialFind(controller, findOptions, prefix, suffix);
        }

        public static Lite FindUnique(FindUniqueOptions options)
        {
            return Manager.FindUnique(options);
        }

        public static int QueryCount(CountOptions options)
        {
            return Manager.QueryCount(options);
        }

        public static PartialViewResult Search(Controller controller, FindOptions findOptions, int? top, string prefix)
        {
            return Manager.Search(controller, findOptions, top, prefix);
        }

        internal static List<Filter> ExtractFilters(HttpContextBase httpContext, object queryName)
        {
            return Manager.ExtractFilters(httpContext, queryName);
        }

        public static SortedList<string, object> ToSortedList(NameValueCollection form, string prefixFilter, string prefixToIgnore)
        {
            SortedList<string, object> formValues = new SortedList<string, object>(form.Count);
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

        public static ChangesLog ApplyChangesAndValidate<T>(Controller controller, ref T entity) where T : Modifiable
        {
            return Manager.ApplyChangesAndValidate(controller, controller.Request.Form, ref entity, null, null);
        }

        public static ChangesLog ApplyChangesAndValidate<T>(Controller controller, NameValueCollection form, ref T entity) where T : Modifiable
        {
            return Manager.ApplyChangesAndValidate(controller, form, ref entity, null, null);
        }

        public static ChangesLog ApplyChangesAndValidate<T>(Controller controller, ref T entity, string prefix) where T : Modifiable
        {
            return Manager.ApplyChangesAndValidate(controller, controller.Request.Form, ref entity, prefix, null);
        }

        public static ChangesLog ApplyChangesAndValidate<T>(Controller controller, NameValueCollection form, ref T entity, string prefix) where T : Modifiable
        {
            return Manager.ApplyChangesAndValidate(controller, form, ref entity, prefix, null);
        }

        public static ChangesLog ApplyChangesAndValidate<T>(Controller controller, ref T entity, string prefix, string prefixToIgnore) where T : Modifiable
        {
            return Manager.ApplyChangesAndValidate(controller, controller.Request.Form, ref entity, prefix, prefixToIgnore);
        }

        public static ChangesLog ApplyChangesAndValidate<T>(Controller controller, NameValueCollection form, ref T entity, string prefix, string prefixToIgnore) where T : Modifiable
        {
            return Manager.ApplyChangesAndValidate(controller, form, ref entity, prefix, prefixToIgnore);
        }

        public static ChangesLog ApplyChangesAndValidate<T>(Controller controller, ref T entity, out List<string> fullIntegrityErrors) where T : Modifiable
        {
            return Manager.ApplyChangesAndValidate(controller, controller.Request.Form, ref entity, null, null, out fullIntegrityErrors);
        }

        public static ChangesLog ApplyChangesAndValidate<T>(Controller controller, NameValueCollection form, ref T entity, out List<string> fullIntegrityErrors) where T : Modifiable
        {
            return Manager.ApplyChangesAndValidate(controller, form, ref entity, null, null, out fullIntegrityErrors);
        }

        public static ChangesLog ApplyChangesAndValidate<T>(Controller controller, ref T entity, string prefix, out List<string> fullIntegrityErrors) where T : Modifiable
        {
            return Manager.ApplyChangesAndValidate(controller, controller.Request.Form, ref entity, prefix, null, out fullIntegrityErrors);
        }

        public static ChangesLog ApplyChangesAndValidate<T>(Controller controller, NameValueCollection form, ref T entity, string prefix, out List<string> fullIntegrityErrors) where T : Modifiable
        {
            return Manager.ApplyChangesAndValidate(controller, form, ref entity, prefix, null, out fullIntegrityErrors);
        }

        public static ChangesLog ApplyChangesAndValidate<T>(Controller controller, ref T entity, string prefix, string prefixToIgnore, out List<string> fullIntegrityErrors) where T : Modifiable
        {
            return Manager.ApplyChangesAndValidate(controller, controller.Request.Form, ref entity, prefix, prefixToIgnore, out fullIntegrityErrors);
        }

        public static ChangesLog ApplyChangesAndValidate<T>(Controller controller, NameValueCollection form, ref T entity, string prefix, string prefixToIgnore, out List<string> fullIntegrityErrors) where T : Modifiable
        {
            return Manager.ApplyChangesAndValidate(controller, form, ref entity, prefix, prefixToIgnore, out fullIntegrityErrors);
        }

        public static Modification GenerateModification<T>(Controller controller, T entity) where T : Modifiable
        {
            return Manager.GenerateModification(controller, controller.Request.Form, entity, null, null);
        }

        public static Modification GenerateModification<T>(Controller controller, NameValueCollection form, T entity) where T : Modifiable
        {
            return Manager.GenerateModification(controller, form, entity, null, null);
        }

        public static Modification GenerateModification<T>(Controller controller, T entity, string prefix) where T : Modifiable
        {
            return Manager.GenerateModification(controller, controller.Request.Form, entity, prefix, null);
        }

        public static Modification GenerateModification<T>(Controller controller, NameValueCollection form, T entity, string prefix) where T : Modifiable
        {
            return Manager.GenerateModification(controller, form, entity, prefix, null);
        }

        public static Modification GenerateModification<T>(Controller controller, T entity, string prefix, string prefixToIgnore) where T : Modifiable
        {
            return Manager.GenerateModification(controller, controller.Request.Form, entity, prefix, prefixToIgnore);
        }

        public static Modification GenerateModification<T>(Controller controller, NameValueCollection form, T entity, string prefix, string prefixToIgnore) where T : Modifiable
        {
            return Manager.GenerateModification(controller, form, entity, prefix, prefixToIgnore);
        }

        public static ModificationState ApplyChanges<T>(Controller controller, Modification modification, ref T entity) where T : Modifiable
        {
            return Manager.ApplyChanges(controller, modification, ref entity);
        }

        public static Dictionary<string, List<string>> GenerateErrors(Controller controller, ModifiableEntity entity, Modification modification, string prefix)
        {
            return Manager.GenerateErrors(controller, entity, modification, prefix);
        }

        public static Dictionary<string, List<string>> GenerateErrors(Controller controller, ModifiableEntity entity, Modification modification, string prefix, out List<string> fullIntegrityErrors)
        {
            return Manager.GenerateErrors(controller, entity, modification, prefix, out fullIntegrityErrors);
        }

        public static ModifiableEntity ExtractEntity(Controller controller, NameValueCollection form)
        {
            return Manager.ExtractEntity(controller, form, null, null);
        }

        public static ModifiableEntity ExtractEntity(Controller controller, NameValueCollection form, bool clone)
        {
            return Manager.ExtractEntity(controller, form, null, clone);
        }

        public static ModifiableEntity ExtractEntity(Controller controller, NameValueCollection form, string prefix)
        {
            return Manager.ExtractEntity(controller, form, prefix, null);
        }

        public static ModifiableEntity ExtractEntity(Controller controller, NameValueCollection form, bool clone, string prefix)
        {
            return Manager.ExtractEntity(controller, form, prefix, clone);
        }

        public static object CreateInstance(Controller controller, Type type)
        {
            lock (Constructor.ConstructorManager)
            {
                return Constructor.Construct(type, controller);
            }
        }

        public static ModifiableEntity CreateInstance(Type type)
        {
            lock (Constructor.ConstructorManager)
            {
                return Constructor.ConstructStrict(type);
            }
        }

        static Dictionary<string, Type> nameToType;
        public static Dictionary<string, Type> NameToType
        {
            get { return nameToType.ThrowIfNullC(Resources.NamesToTypesDictionaryNotInitialized); }
            internal set { nameToType = value; }
        }

        public static Dictionary<Type, string> TypesToURLNames
        {
            get { return Manager.TypesToURLNames; }
        }

        internal static void ConfigureEntityBase(EntityBase el, Type entityType, bool admin)
        {
            if (Manager.EntitySettings.ContainsKey(entityType))
            {
                el.Create = Navigator.IsCreable(entityType, admin);
                el.View = Navigator.IsViewable(entityType, admin);
                el.Find = Navigator.IsFindable(entityType);
            }
        }

        public static bool IsNavigable(Type type, bool admin)
        {
            return Manager.IsNavigable(type, admin);
        }
        public static bool IsViewable(Type type, bool admin)
        {
            return Manager.IsViewable(type, admin);
        }
        public static bool IsReadOnly(Type type, bool admin)
        {
            return Manager.IsReadOnly(type, admin);
        }
        public static bool IsCreable(Type type, bool admin)
        {
            return Manager.IsCreable(type, admin);
        }
        public static bool IsFindable(object queryName)
        {
            return Manager.IsFindable(queryName);
        }
    }
    
    public class NavigationManager
    {
        public Dictionary<Type, EntitySettings> EntitySettings;
        public Dictionary<object, QuerySettings> QuerySettings;

        public string AjaxErrorPageUrl = "~/Plugin/Signum.Web.dll/Signum.Web.Views.AjaxError.ascx";
        public string ErrorPageUrl = "~/Plugin/Signum.Web.dll/Signum.Web.Views.Error.aspx";
        public string NormalPageUrl = "~/Plugin/Signum.Web.dll/Signum.Web.Views.NormalPage.aspx";
        public string PopupControlUrl = "~/Plugin/Signum.Web.dll/Signum.Web.Views.PopupControl.ascx";
        public string ChooserPopupUrl = "~/Plugin/Signum.Web.dll/Signum.Web.Views.ChooserPopup.ascx";
        public string SearchPopupControlUrl = "~/Plugin/Signum.Web.dll/Signum.Web.Views.SearchPopupControl.ascx";
        public string SearchWindowUrl = "~/Plugin/Signum.Web.dll/Signum.Web.Views.SearchWindow.aspx";
        public string SearchControlUrl = "~/Plugin/Signum.Web.dll/Signum.Web.Views.SearchControl.ascx";
        public string SearchResultsUrl = "~/Plugin/Signum.Web.dll/Signum.Web.Views.SearchResults.ascx";
        
        protected internal Dictionary<string, Type> URLNamesToTypes { get; private set; }
        protected internal Dictionary<Type, string> TypesToURLNames { get; private set; }
        protected internal Dictionary<string, object> UrlQueryNames { get; private set; }
        protected internal Dictionary<string, Type> FullNamesToTypes { get; private set; }

        public event Func<Type, bool> GlobalIsCreable;
        public event Func<Type, bool> GlobalIsViewable;
        public event Func<Type, bool> GlobalIsNavigable;
        public event Func<Type, bool> GlobalIsReadOnly;
        public event Func<object, bool> GlobalIsFindable;


        //public void AddEntitySettings(Type type, string ViewPrefix)
        //{
        //    if (!type.Name.EndsWith("DN")) throw new ApplicationException("This method is only valid for xxxDN entities");
        //    EntitySettings.Add(type, new EntitySettings(false) { PartialViewName = ViewPrefix + type.Name.RemoveRight(2) + "IU.ascx" });
        //}

        public void Start()
        {
            URLNamesToTypes = EntitySettings.ToDictionary(
                kvp => kvp.Value.UrlName ?? Reflector.CleanTypeName(kvp.Key),
                kvp => kvp.Key);
            TypesToURLNames = URLNamesToTypes.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            Navigator.NameToType = EntitySettings.ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Key);

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

                UrlQueryNames = QuerySettings.ToDictionary(kvp => kvp.Value.UrlName ?? GetQueryName(kvp.Key), kvp => kvp.Key, "UrlQueryNames");
            }

            ModelBinders.Binders.DefaultBinder = new LiteModelBinder(); 
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

        protected internal virtual ViewResult View(Controller controller, object obj, string partialViewName, Dictionary<string, long> changeTicks)
        {
            EntitySettings es = Navigator.Manager.EntitySettings.TryGetC(obj.GetType()).ThrowIfNullC(Resources.TheresNotAViewForType0.Formato(obj.GetType()));

            controller.ViewData[ViewDataKeys.MainControlUrl] = partialViewName ?? es.PartialViewName;
            IdentifiableEntity entity = (IdentifiableEntity)obj;
            controller.ViewData.Model = entity;
            controller.ViewData[ViewDataKeys.EntityTypeNiceName] = obj.GetType().NiceName();
            controller.ViewData[TypeContext.Id] = entity.IdOrNull != null ? entity.Id.ToString() : "";

            if (controller.ViewData.Keys.Count(s => s==ViewDataKeys.PageTitle)==0)
                controller.ViewData[ViewDataKeys.PageTitle] = entity.ToStr;
            if (changeTicks != null)
                controller.ViewData[ViewDataKeys.ChangeTicks] = changeTicks;
            string tabID = GetOrCreateTabID(controller);
            controller.ViewData[ViewDataKeys.TabId] = tabID;

            if (GraphExplorer.FromRoot(entity).Any(m => m.GetType().HasAttribute<Reactive>()))
            {
                controller.ViewData[ViewDataKeys.Reactive] = true;
                controller.Session[tabID] = entity;
            }

            return new ViewResult()
            {
                ViewName = NormalPageUrl,
                MasterName = controller.Request.UserAgent.Contains("iPhone") ? "Mobile" : null,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal string GetOrCreateTabID(Controller c)
        {
            if (c.Request.Form.AllKeys.Contains(ViewDataKeys.TabId))
            {
                string tabID = c.Request.Form[ViewDataKeys.TabId];
                if (tabID.HasText())
                    return tabID;
            }
            return Guid.NewGuid().ToString();
        }

        protected internal bool ExtractIsReactive(NameValueCollection form)
        {
            return form.AllKeys.Contains(ViewDataKeys.Reactive);
        }

        protected internal string ExtractTabID(NameValueCollection form)
        {
            if (!form.AllKeys.Contains(ViewDataKeys.TabId))
                throw new ApplicationException(Resources.RequestDoesntHaveNecessaryTabIdentifier);
            string tabID = (string)form[ViewDataKeys.TabId];
            if (!tabID.HasText())
                throw new ApplicationException(Resources.RequestDoesntHaveNecessaryTabIdentifier);
            return tabID;
        }

        protected internal virtual PartialViewResult PopupView<T>(Controller controller, T entity, string prefix, string partialViewName, Dictionary<string, long> changeTicks)
        {
            Type cleanType = entity != null ? entity.GetType() : typeof(T);
            if (entity != null && typeof(TypeContext).IsAssignableFrom(entity.GetType()))
            {
                TypeContext entityTC = (TypeContext)(object)entity;
                cleanType = Reflector.ExtractLite(entityTC.ContextType) ?? entityTC.ContextType;
            }

            string url = partialViewName ??
                Navigator.Manager.EntitySettings.TryGetC(cleanType).ThrowIfNullC(Resources.TheresNotAViewForType0.Formato(cleanType)).PartialViewName;

            controller.ViewData[ViewDataKeys.MainControlUrl] = url;
            controller.ViewData[ViewDataKeys.PopupPrefix] = prefix;
            if (changeTicks != null)
                controller.ViewData[ViewDataKeys.ChangeTicks] = changeTicks;

            controller.ViewData[ViewDataKeys.StyleContext] = StyleContext.RegisterCleanStyleContext(false);

            controller.ViewData.Model = entity;
            
            return new PartialViewResult
            {
                ViewName = PopupControlUrl,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual PartialViewResult PartialView<T>(Controller controller, T entity, string prefix, string partialViewName, Dictionary<string, long> changeTicks)
        {
            Type cleanType = entity != null ? entity.GetType() : typeof(T);
            if (entity != null && typeof(TypeContext).IsAssignableFrom(entity.GetType()))
            {
                TypeContext entityTC = (TypeContext)(object)entity;
                cleanType = Reflector.ExtractLite(entityTC.ContextType) ?? entityTC.ContextType;
            }

            string url = partialViewName ??
                Navigator.Manager.EntitySettings.TryGetC(cleanType).ThrowIfNullC(Resources.TheresNotAViewForType0.Formato(cleanType)).PartialViewName;

            controller.ViewData[ViewDataKeys.PopupPrefix] = prefix;
            controller.ViewData.Model = entity;
            if (changeTicks != null)
                controller.ViewData[ViewDataKeys.ChangeTicks] = changeTicks;

            //if (controller.ViewData.ContainsKey(ViewDataKeys.EmbeddedControl))
            //    controller.Response.Write("<input type='hidden' id='{0}' name='{0}' value='' />".Formato(TypeContext.Compose(prefix, EntityBaseKeys.IsNew))); 

            return new PartialViewResult
            {
                ViewName = url,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual ViewResult Find(Controller controller, FindOptions findOptions)
        {
            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);

            Type entitiesType = Reflector.ExtractLite(queryDescription.Columns.Single(a => a.IsEntity).Type);

            List<Column> columns = queryDescription.Columns.Where(a => a.Filterable).ToList();

            foreach (FilterOption opt in findOptions.FilterOptions)
            {
                opt.Column = queryDescription.Columns.Where(c => c.Name == opt.ColumnName)
                    .Single(Resources.FilterColumn0NotFoundOrFoundMoreThanOnce.Formato(opt.ColumnName));
            }

            //controller.ViewData[ViewDataKeys.SearchResourcesRoute] = ConfigurationManager.AppSettings[ViewDataKeys.SearchResourcesRoute] ?? "../../";
            controller.ViewData[ViewDataKeys.MainControlUrl] = SearchControlUrl;
            controller.ViewData[ViewDataKeys.FilterColumns] = columns;
            controller.ViewData[ViewDataKeys.FindOptions] = findOptions;
            controller.ViewData[ViewDataKeys.Top] = QuerySettings.TryGetC(findOptions.QueryName).ThrowIfNullC(Resources.MissingQuerySettingsForQueryName0.Formato(findOptions.QueryName.ToString())).Top;
            if (controller.ViewData.Keys.Count(s => s == ViewDataKeys.PageTitle) == 0)
                controller.ViewData[ViewDataKeys.PageTitle] = SearchTitle(findOptions.QueryName);
            controller.ViewData[ViewDataKeys.EntityTypeName] = entitiesType.Name;
            controller.ViewData[ViewDataKeys.EntityType] = entitiesType;
            controller.ViewData[ViewDataKeys.Create] =
                (findOptions.Create.HasValue) ?
                    findOptions.Create.Value :
                    Navigator.IsCreable(entitiesType, true); ;

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
            SetColumns(options.QueryName, options.FilterOptions);
            SetColumns(options.QueryName, options.OrderOptions);

            var filters = options.FilterOptions.Select(f => f.ToFilter()).ToList();
            var orders = options.OrderOptions.Select(o => o.ToOrder()).ToList();

            return DynamicQueryManager.Current.ExecuteUniqueEntity(options.QueryName, filters, orders, options.UniqueType);
        }

        protected internal virtual int QueryCount(CountOptions options)
        {
            SetColumns(options.QueryName, options.FilterOptions);

            var filters = options.FilterOptions.Select(f => f.ToFilter()).ToList();

            return DynamicQueryManager.Current.ExecuteQueryCount(options.QueryName, filters);
        }

        protected internal void SetColumns(object queryName, List<FilterOption> filters)
        {
            QueryDescription view = DynamicQueryManager.Current.QueryDescription(queryName);

            Column entity = view.Columns.SingleOrDefault(a => a.IsEntity);

            foreach (var f in filters)
            {
                f.Column = view.Columns.Where(c => c.Name == f.ColumnName)
                    .Single(Properties.Resources.Column0NotFoundOnQuery1.Formato(f.ColumnName, queryName));
            }
        }

        public void SetColumns(object queryName, IEnumerable<OrderOption> orders)
        {
            QueryDescription view = DynamicQueryManager.Current.QueryDescription(queryName);

            foreach (var o in orders)
            {
                o.Column = view.Columns.Where(c => c.Name == o.ColumnName)
                    .Single(Properties.Resources.Column0NotFoundOnQuery1.Formato(o.ColumnName, queryName));
            }
        }

        protected internal virtual PartialViewResult PartialFind(Controller controller, FindOptions findOptions, string prefix, string suffix)
        {
            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);

            Type entitiesType = Reflector.ExtractLite(queryDescription.Columns.Single(a => a.IsEntity).Type);

            List<Column> columns = queryDescription.Columns.Where(a => a.Filterable).ToList();

            //controller.ViewData[ViewDataKeys.ResourcesRoute] = ConfigurationManager.AppSettings[ViewDataKeys.ResourcesRoute] ?? "../../";
            controller.ViewData[ViewDataKeys.MainControlUrl] = SearchControlUrl;
            controller.ViewData[ViewDataKeys.PopupPrefix] = prefix;
            controller.ViewData[ViewDataKeys.PopupSufix] = suffix ?? "";

            controller.ViewData[ViewDataKeys.FilterColumns] = columns;
            controller.ViewData[ViewDataKeys.FindOptions] = findOptions;
            controller.ViewData[ViewDataKeys.Top] = QuerySettings.TryGetC(findOptions.QueryName).ThrowIfNullC(Resources.MissingQuerySettingsForQueryName0.Formato(findOptions.QueryName.ToString())).Top;
            //controller.ViewData[ViewDataKeys.QuerySettings] = QuerySettings.TryGetC(findOptions.QueryName).ThrowIfNullC("QuerySettings not present for QueryName {0}".Formato(findOptions.QueryName.ToString()));
            if (controller.ViewData.Keys.Count(s => s == ViewDataKeys.PageTitle) == 0)
                controller.ViewData[ViewDataKeys.PageTitle] = SearchTitle(findOptions.QueryName);
            controller.ViewData[ViewDataKeys.EntityTypeName] = entitiesType.Name;
            controller.ViewData[ViewDataKeys.EntityType] = entitiesType;
            controller.ViewData[ViewDataKeys.Create] =
                (findOptions.Create.HasValue) ?
                    findOptions.Create.Value :
                    Navigator.IsCreable(entitiesType, true);

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

        protected internal virtual PartialViewResult Search(Controller controller, FindOptions findOptions, int? top, string prefix)
        {
            var filters = findOptions.FilterOptions.Select(fo => fo.ToFilter()).ToList();
            var orders = findOptions.OrderOptions.Select(fo => fo.ToOrder()).ToList();

            ResultTable queryResult = DynamicQueryManager.Current.ExecuteQuery(findOptions.QueryName, filters, orders, top);

            //controller.ViewData[ViewDataKeys.ResourcesRoute] = ConfigurationManager.AppSettings[ViewDataKeys.ResourcesRoute] ?? "../../";
            controller.ViewData[ViewDataKeys.Results] = queryResult;
            controller.ViewData[ViewDataKeys.AllowMultiple] = findOptions.AllowMultiple;
            controller.ViewData[ViewDataKeys.PopupPrefix] = prefix;

            if (queryResult != null && queryResult.Rows != null && queryResult.Rows.Length > 0 && queryResult.VisibleColumns.Count() > 0)
            {
                int entityColumnIndex = queryResult.Columns.IndexOf(c => c.IsEntity);
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
                type = queryDescription.Columns
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
                        Type liteType = Navigator.NameToType[vals[1]];
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
                    Name = name, 
                    Type = type,
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
            if (Navigator.NameToType.ContainsKey(queryUrlName))
            { 
                string urlName= TypesToURLNames[Navigator.NameToType[queryUrlName]];
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
            Type type = Navigator.NameToType.TryGetC(typeName) ?? Type.GetType(typeName, false);
            
            if (type == null)
                throw new ArgumentException(Resources.Type0NotFoundInTheSchema.Formato(typeName));
            
            return type;
        }

        protected internal virtual ChangesLog ApplyChangesAndValidate<T>(Controller controller, NameValueCollection form, ref T entity, string prefix, string prefixToIgnore) where T : Modifiable
        {
            Modification modification = GenerateModification(controller, form, entity, prefix, prefixToIgnore);
            ModificationState modState = ApplyChanges(controller, modification, ref entity);
            return new ChangesLog
            {
                Errors = GenerateErrors(controller, (ModifiableEntity)(object)entity, modification, prefix),
                ChangeTicks = ModificationState.ToDictionary(modState.Actions),
            };
        }

        protected internal virtual ChangesLog ApplyChangesAndValidate<T>(Controller controller, NameValueCollection form, ref T entity, string prefix, string prefixToIgnore, out List<string> fullIntegrityErrors) where T : Modifiable
        {
            Modification modification = GenerateModification(controller, form, entity, prefix, prefixToIgnore);
            ModificationState modState = ApplyChanges(controller, modification, ref entity);
            return new ChangesLog
            {
                Errors = GenerateErrors(controller, (ModifiableEntity)(object)entity, modification, prefix, out fullIntegrityErrors),
                ChangeTicks = ModificationState.ToDictionary(modState.Actions),
            };
        }

        protected internal virtual Modification GenerateModification<T>(Controller controller, NameValueCollection form, T entity, string prefix, string prefixToIgnore) where T: Modifiable
        {
            SortedList<string, object> formValues;
            Modification modification;
            if (form.AllKeys.Contains(ViewDataKeys.Reactive))
            {
                formValues = Navigator.ToSortedList(form, "", prefixToIgnore); //Apply modifications to the entity and all the path
                Interval<int> interval = Modification.FindSubInterval(formValues, "");
                modification = Modification.Create(entity.GetType(), formValues, interval, "");
            }
            else
            {
                formValues = Navigator.ToSortedList(form, prefix, prefixToIgnore);
                Interval<int> interval = Modification.FindSubInterval(formValues, prefix);
                modification = Modification.Create(entity.GetType(), formValues, interval, prefix);
            }
            return modification;
        }

        protected internal virtual ModificationState ApplyChanges<T>(Controller controller, Modification modification, ref T entity)
        {
            ModificationState modState = new ModificationState();
            entity = (T)modification.ApplyChanges(controller, entity, modState);
            modState.Finish();
            return modState;
        }

        protected internal virtual Dictionary<string, List<string>> GenerateErrors(Controller controller, ModifiableEntity entity, Modification modification, string prefix)
        {
            GraphExplorer.PreSaving(() => GraphExplorer.FromRoot(entity));
            Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();
            modification.Validate(controller, entity, errors, prefix);

            Dictionary<ModifiableEntity, string> dicGlobalErrors = entity.FullIntegrityCheckDictionary();
            //Split each error in one entry in the HashTable:
            var globalErrors = dicGlobalErrors.SelectMany(a => a.Value.Lines()).ToList();
            //eliminar de globalErrors los que ya hemos metido en el diccionario
            foreach (var e in errors.SelectMany(a => a.Value))
                globalErrors.Remove(e);
            //meter el resto en el diccionario
            if (globalErrors.Count > 0)
                errors.Add(ViewDataKeys.GlobalErrors, globalErrors.ToList());

            return errors;
        }

        protected internal virtual Dictionary<string, List<string>> GenerateErrors(Controller controller, ModifiableEntity entity, Modification modification, string prefix, out List<string> fullIntegrityErrors)
        {
            fullIntegrityErrors = null;

            Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();
            modification.Validate(controller, entity, errors, prefix);

            Dictionary<ModifiableEntity, string> dicGlobalErrors = entity.FullIntegrityCheckDictionary();
            //Split each error in one entry in the HashTable:
            var globalErrors = dicGlobalErrors.SelectMany(a => a.Value.Lines()).ToList();
            //eliminar de globalErrors los que ya hemos metido en el diccionario
            foreach (var e in errors.SelectMany(a => a.Value))
                globalErrors.Remove(e);
            //meter el resto en el diccionario
            fullIntegrityErrors = globalErrors.ToList();

            return errors;
        }
 
        //public virtual Modification GenerateModification(Controller controller, Modifiable obj, string prefix)
        //{
        //    return GenerateModification(controller, obj, prefix, "");
        //}

        //public virtual Modification GenerateModification(Controller controller, Modifiable obj, string prefix, string prefixToIgnore)
        //{
        //    var formValues = Navigator.ToSortedList(controller.Request.Form, prefix, prefixToIgnore);

        //    Interval<int> interval = Modification.FindSubInterval(formValues, prefix);

        //    return Modification.Create(obj.GetType(), formValues, interval, prefix);
        //}

        protected internal virtual ModifiableEntity ExtractEntity(Controller controller, NameValueCollection form, string prefix, bool? clone)
        {
            EntityInfo entityInfo = EntityInfo.FromFormValue(form[TypeContext.Compose(prefix ?? "", EntityBaseKeys.Info)]);
            //string typeName = null; 
            //string typeNameKey = TypeContext.Compose(prefix ?? "", TypeContext.RuntimeType);
            //if (form.AllKeys.Contains(typeNameKey))
            //    typeName = form[typeNameKey];
            //else
            //    typeName = controller.Request.Params[TypeContext.RuntimeType];
                
            //Type type = Navigator.NameToType.GetOrThrow(typeName, Resources.Type0NotFoundInTheSchema);

            //string id = null;
            //string idKey = TypeContext.Compose(prefix ?? "", TypeContext.Id);
            //if (form.AllKeys.Contains(idKey))
            //    id = form[idKey];
            //else
            //    id = controller.Request.Params[TypeContext.Id];
            
            if (form.AllKeys.Any(s => s == ViewDataKeys.Reactive))
            {
                string tabID = ExtractTabID(form);
                controller.ViewData[ViewDataKeys.Reactive] = true;
                ModifiableEntity mod = (ModifiableEntity)controller.Session[tabID];
                if (mod == null)
                    throw new ApplicationException(Resources.YourSessionHasTimedOutClickF5ToReloadTheEntity);

                EntityInfo parentEntityInfo = EntityInfo.FromFormValue(form[TypeContext.Separator + EntityBaseKeys.Info]);
                //string parentTypeName = form[TypeContext.Separator + TypeContext.RuntimeType];
                //string parentId = form[TypeContext.Separator + TypeContext.Id];
                //Type parentType = Navigator.NameToType.GetOrThrow(parentTypeName, Resources.Type0NotFoundInTheSchema);
                if (mod.GetType() == parentEntityInfo.RuntimeType &&
                    (parentEntityInfo.IsEmbedded || ((IIdentifiable)mod).IdOrNull == parentEntityInfo.IdOrNull))
                {
                    if (clone == null || clone.Value) 
                        return (ModifiableEntity)((ICloneable)mod).Clone();
                    return mod;
                }
                else
                    throw new ApplicationException(Resources.IncorrectEntityInSession);
            }

            if (entityInfo.IdOrNull != null)
                return Database.Retrieve(entityInfo.RuntimeType, entityInfo.IdOrNull.Value);
            else
                return (ModifiableEntity)Constructor.Construct(entityInfo.RuntimeType, controller);
        }

        protected internal virtual bool IsViewable(Type type, bool admin)
        {
            if (GlobalIsViewable != null && !GlobalIsViewable(type))
                return false;

            return EntitySettings[type].IsViewable(admin);
        }

        protected internal virtual bool IsNavigable(Type type, bool admin)
        {
            if (GlobalIsNavigable != null && !GlobalIsNavigable(type))
                return false;

            return EntitySettings[type].IsNavigable(admin);
        }

        protected internal virtual bool IsReadOnly(Type type, bool admin)
        {
            if (GlobalIsReadOnly != null && !GlobalIsReadOnly(type))
                return false;

            return EntitySettings[type].IsReadOnly(admin);
        }

        protected internal virtual bool IsCreable(Type type, bool admin)
        {
            if (GlobalIsCreable != null && !GlobalIsCreable(type))
                return false;

            return EntitySettings[type].IsCreable(admin);
        }

        protected internal virtual bool IsFindable(object queryName)
        {
            if (GlobalIsFindable != null && !GlobalIsFindable(queryName))
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
    }

    public static class ModelStateExtensions
    { 
        public static void FromDictionary(this ModelStateDictionary modelState, Dictionary<string, List<string>> errors, NameValueCollection form)
        {
            if (errors != null)
                foreach (var p in errors)
                    foreach (var v in p.Value)
                        modelState.AddModelError(p.Key, v, form[p.Key]);
        }

        public static string ToJsonData(this ModelStateDictionary modelState)
        {
            return modelState.ToJSonObjectBig(
                key => key.Quote(),
                value => value.Errors.ToJSonArray(me => me.ErrorMessage.Quote()));
        }

        //http://www.crankingoutcode.com/2009/02/01/IssuesWithAddModelErrorSetModelValueWithMVCRC1.aspx
        //Necesary to set model value if you add a model error, otherwise some htmlhelpers throw exception
        public static void AddModelError(this ModelStateDictionary modelState, string key, string errorMessage, string attemptedValue)
        {
            modelState.AddModelError(key, errorMessage);
            modelState.SetModelValue(key, new ValueProviderResult(attemptedValue, attemptedValue, null));
        }

    }
}
