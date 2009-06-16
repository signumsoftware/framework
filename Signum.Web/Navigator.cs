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
using Signum.Entities.Properties;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Engine.DynamicQuery;

namespace Signum.Web
{
    public static class Navigator
    {
        private static Func<Type, int, string> viewRoute;
        public static Func<Type, int, string> ViewRoute 
        { 
            get 
            {
                return viewRoute ??
                       (viewRoute = (Type type, int id) => "/View/{0}/{1}".Formato(Navigator.TypesToURLNames[type], id));
            }
            set 
            {
                viewRoute = value;
            }
        }

        private static Func<string, string> findRoute;
        public static Func<string, string> FindRoute
        {
            get
            {
                return findRoute ??
                       (findRoute = (string urlQueryName) => "/Find/{0}".Formato(urlQueryName));
            }
            set
            {
                findRoute = value;
            }
        }

        public static string FindTypeRoute(Type type)
        {
            return findRoute(Navigator.TypesToURLNames[type]);
        }

        public static NavigationManager NavigationManager;
        
        public static Type ResolveType(string typeName)
        {
            return NavigationManager.ResolveType(typeName);
        }

        public static Type ResolveTypeFromUrlName(string typeUrlName)
        {
            return NavigationManager.ResolveTypeFromUrlName(typeUrlName); 
        }

        public static object ResolveQueryFromUrlName(string queryUrlName)
        {
            return NavigationManager.ResolveQueryFromUrlName(queryUrlName);
        }

        public static object ResolveQueryFromToStr(string queryNameToStr)
        {
            return NavigationManager.ResolveQueryFromToStr(queryNameToStr);
        }

        public static ViewResult View(this Controller controller, object obj)
        {
            return NavigationManager.View(controller, obj); 
        }

        public static PartialViewResult PartialView<T>(this Controller controller, T entity, string prefix)
        {
            return NavigationManager.PartialView(controller, entity, prefix);
        }

        public static ViewResult Find(Controller controller, object queryName)
        {
            return Find(controller, new FindOptions(queryName));
        }

        public static ViewResult Find(Controller controller, FindOptions findOptions)
        {
            return NavigationManager.Find(controller, findOptions);
        }

        public static PartialViewResult PartialFind(Controller controller, FindOptions findOptions, string prefix, string prefixEnd)
        {
            return NavigationManager.PartialFind(controller, findOptions, prefix, prefixEnd);
        }

        public static PartialViewResult Search(Controller controller, object queryName, List<Filter> filters, int? resultsLimit, bool allowMultiple, string prefix)
        {
            return NavigationManager.Search(controller, queryName, filters, resultsLimit, allowMultiple, prefix);
        }

        internal static List<Filter> ExtractFilters(NameValueCollection form, object queryName, string prefix)
        {
            return NavigationManager.ExtractFilters(form, queryName); //, prefix);
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

        public static Dictionary<string, List<string>> ApplyChangesAndValidate<T>(NameValueCollection form, string prefixToIgnore, ref T obj)
        {
            SortedList<string, object> formValues = ToSortedList(form, prefixToIgnore);

            return NavigationManager.ApplyChangesAndValidate(formValues, ref obj, null);
        }

        public static Dictionary<string, List<string>> ApplyChangesAndValidate<T>(SortedList<string, object> formValues, ref T obj)
        {
            return NavigationManager.ApplyChangesAndValidate(formValues, ref obj, null);
        }

        public static Dictionary<string, List<string>> ApplyChangesAndValidate<T>(SortedList<string, object> formValues, ref T obj, string prefix)
            where T:Modifiable
        {
            return NavigationManager.ApplyChangesAndValidate(formValues, ref obj, prefix);
        }

        public static IdentifiableEntity ExtractEntity(NameValueCollection form)
        {
            return NavigationManager.ExtractEntity(form, null);
        }

        public static IdentifiableEntity ExtractEntity(NameValueCollection form, string prefix)
        {
            return NavigationManager.ExtractEntity(form, prefix);
        }

        public static ModifiableEntity CreateInstance(Type type)
        {
            lock (NavigationManager.Constructors)
            {
                return NavigationManager.Constructors
                    .GetOrCreate(type, () => ReflectionTools.CreateConstructor<ModifiableEntity>(type))
                    .Invoke();
            }
        }

        static Dictionary<string, Type> nameToType;
        public static Dictionary<string, Type> NameToType
        {
            get { return nameToType.ThrowIfNullC("Names to Types dictionary not initialized"); }
            internal set { nameToType = value; }
        }

        public static Dictionary<Type, string> TypesToURLNames
        {
            get { return NavigationManager.TypesToURLNames; }
        }

        public static string NormalPageUrl
        {
            get { return NavigationManager.NormalPageUrl; }
            set { NavigationManager.NormalPageUrl = value; }
        }


        internal static void ConfigureEntityBase(EntityBase el, Type entityType, bool admin)
        {
            EntitySettings es = NavigationManager.EntitySettings[entityType];
            if (es.IsCreable != null)
                el.Create = es.IsCreable(admin);

            if (es.IsViewable != null)
                el.View = es.IsViewable(admin);

            el.Find = NavigationManager.ExistsQuery(TypesToURLNames[entityType]);
        }
    }

    public class NavigationManagerSettings
    {
        public Dictionary<Type, EntitySettings> EntitySettings = new Dictionary<Type, EntitySettings>();
        public Dictionary<object, QuerySettings> QuerySettings;
        public DynamicQueryManager Queries { get; set; }
        public Dictionary<Type, Func<ModifiableEntity>> Constructors = new Dictionary<Type, Func<ModifiableEntity>>();
    }

    public class NavigationManager
    {
        protected internal Dictionary<Type, EntitySettings> EntitySettings;
        protected internal Dictionary<object, QuerySettings> QuerySettings;
        protected internal DynamicQueryManager Queries { get; set; }
        
        protected internal string NormalPageUrl = "~/Plugin/Signum.Web.dll/Signum.Web.Views.NormalPage.aspx";
        protected internal string PopupControlUrl = "~/Plugin/Signum.Web.dll/Signum.Web.Views.PopupControl.ascx";
        protected internal string SearchPopupControlUrl = "~/Plugin/Signum.Web.dll/Signum.Web.Views.SearchPopupControl.ascx";
        protected internal string SearchWindowUrl = "~/Plugin/Signum.Web.dll/Signum.Web.Views.SearchWindow.aspx";
        protected internal string SearchControlUrl = "~/Plugin/Signum.Web.dll/Signum.Web.Views.SearchControl.ascx";
        
        protected internal Dictionary<string, Type> URLNamesToTypes { get; private set; }
        protected internal Dictionary<Type, string> TypesToURLNames { get; private set; }
        protected internal Dictionary<string, object> UrlQueryNames { get; private set; }

        protected internal Dictionary<Type, Func<ModifiableEntity>> Constructors;

        internal bool ExistsQuery(string urlQueryName)
        {
            return UrlQueryNames.Count(kvp => kvp.Key == urlQueryName) > 0;
        }

        public NavigationManager(NavigationManagerSettings settings)
        {
            Constructors = settings.Constructors;
            EntitySettings = settings.EntitySettings;
            QuerySettings = settings.QuerySettings;
            Queries = settings.Queries;
            URLNamesToTypes = EntitySettings.ToDictionary(
                kvp => kvp.Value.UrlName ?? (kvp.Key.Name.EndsWith("DN") ? kvp.Key.Name.Substring(0, kvp.Key.Name.Length - 2) : kvp.Key.Name), 
                kvp => kvp.Key);
            TypesToURLNames = URLNamesToTypes.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            Navigator.NameToType = EntitySettings.ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Key);
            if (QuerySettings != null)
                UrlQueryNames = QuerySettings.ToDictionary(kvp => kvp.Value.UrlName ?? GetQueryName(kvp.Key), kvp => kvp.Key);
        }

        protected internal virtual ViewResult View(Controller controller, object obj)
        {
            EntitySettings es = Navigator.NavigationManager.EntitySettings.TryGetC(obj.GetType()).ThrowIfNullC("No hay una vista asociada al tipo: " + obj.GetType());
            //string urlName = Navigator.NavigationManager.TypesToURLNames.TryGetC(obj.GetType()).ThrowIfNullC("No hay un nombre asociado al tipo: " + obj.GetType());

            controller.ViewData[ViewDataKeys.ResourcesRoute] = System.Configuration.ConfigurationManager.AppSettings["RutaResources"] ?? "../../";
            controller.ViewData[ViewDataKeys.MainControlUrl] = es.PartialViewName;
            IdentifiableEntity entity = (IdentifiableEntity)obj;
            controller.ViewData.Model = entity;
            if (controller.ViewData.Keys.Count(s => s==ViewDataKeys.PageTitle)==0)
                controller.ViewData[ViewDataKeys.PageTitle] = entity.ToStr;

            return new ViewResult()
            {
                ViewName = NormalPageUrl,
                MasterName = null,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual PartialViewResult PartialView<T>(Controller controller, T entity, string prefix)
        {
            EntitySettings es = Navigator.NavigationManager.EntitySettings.TryGetC(entity.GetType()).ThrowIfNullC("No hay una vista asociada al tipo: " + entity.GetType());

            controller.ViewData[ViewDataKeys.ResourcesRoute] = System.Configuration.ConfigurationManager.AppSettings["RutaResources"] ?? "../../";
            controller.ViewData[ViewDataKeys.MainControlUrl] = es.PartialViewName;
            controller.ViewData[ViewDataKeys.PopupPrefix] = prefix;

            controller.ViewData.Model = entity;
            
            return new PartialViewResult
            {
                ViewName = PopupControlUrl,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual ViewResult Find(Controller controller, FindOptions findOptions)
        {
            QueryDescription queryDescription = Queries.QueryDescription(findOptions.QueryName);

            Type entitiesType = Reflector.ExtractLazy(queryDescription.Columns.Single(a => a.IsEntity).Type);

            List<Column> columns = queryDescription.Columns.Where(a => a.Filterable).ToList();

            foreach (FilterOptions opt in findOptions.FilterOptions)
            {
                opt.Column = queryDescription.Columns.Where(c => c.Name == opt.ColumnName)
                    .Single("Filter Column {0} not found or found more than once in query description".Formato(opt.ColumnName));
            }

            controller.ViewData[ViewDataKeys.ResourcesRoute] = System.Configuration.ConfigurationManager.AppSettings["RutaResources"] ?? "../../";
            controller.ViewData[ViewDataKeys.MainControlUrl] = SearchControlUrl;
            controller.ViewData[ViewDataKeys.FilterColumns] = columns;
            controller.ViewData[ViewDataKeys.FindOptions] = findOptions;
            controller.ViewData[ViewDataKeys.Top] = QuerySettings.TryGetC(findOptions.QueryName).ThrowIfNullC("QuerySettings not present for QueryName {0}".Formato(findOptions.QueryName.ToString())).Top;
            if (controller.ViewData.Keys.Count(s => s == ViewDataKeys.PageTitle) == 0)
                controller.ViewData[ViewDataKeys.PageTitle] = SearchTitle(findOptions.QueryName);
            controller.ViewData[ViewDataKeys.EntityTypeName] = entitiesType.Name;
            controller.ViewData[ViewDataKeys.Create] =
                (findOptions.Create.HasValue) ?
                findOptions.Create.Value :
                EntitySettings[entitiesType].ThrowIfNullC("Invalid type {0}".Formato(entitiesType.Name)).IsCreable(false);

            return new ViewResult()
            {
                ViewName = SearchWindowUrl,
                MasterName = null,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual PartialViewResult PartialFind(Controller controller, FindOptions findOptions, string prefix, string prefixEnd)
        {
            QueryDescription queryDescription = Queries.QueryDescription(findOptions.QueryName);

            Type entitiesType = Reflector.ExtractLazy(queryDescription.Columns.Single(a => a.IsEntity).Type);

            List<Column> columns = queryDescription.Columns.Where(a => a.Filterable).ToList();

            controller.ViewData[ViewDataKeys.ResourcesRoute] = System.Configuration.ConfigurationManager.AppSettings["RutaResources"] ?? "../../";
            controller.ViewData[ViewDataKeys.MainControlUrl] = SearchControlUrl;
            controller.ViewData[ViewDataKeys.PopupPrefix] = prefix;
            controller.ViewData[ViewDataKeys.PopupSufix] = prefixEnd ?? "";

            controller.ViewData[ViewDataKeys.FilterColumns] = columns;
            controller.ViewData[ViewDataKeys.FindOptions] = findOptions;
            controller.ViewData[ViewDataKeys.Top] = QuerySettings.TryGetC(findOptions.QueryName).ThrowIfNullC("QuerySettings not present for QueryName {0}".Formato(findOptions.QueryName.ToString())).Top;
            if (controller.ViewData.Keys.Count(s => s == ViewDataKeys.PageTitle) == 0)
                controller.ViewData[ViewDataKeys.PageTitle] = SearchTitle(findOptions.QueryName);
            controller.ViewData[ViewDataKeys.EntityTypeName] = entitiesType.Name;
            controller.ViewData[ViewDataKeys.Create] =
                (findOptions.Create.HasValue) ?
                findOptions.Create.Value :
                EntitySettings[entitiesType].ThrowIfNullC("Invalid type {0}".Formato(entitiesType.Name)).IsCreable(false);

            return new PartialViewResult
            {
                ViewName = PopupControlUrl,
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

            return GetQueryName(queryName);
        }

        protected virtual string GetQueryName(object queryName)
        { 
            return (queryName is Type) ? TypesToURLNames[(Type)queryName] :
                   (queryName is Enum) ? EnumExtensions.NiceToString(queryName) :
                   queryName.ToString();
        }

        protected internal virtual PartialViewResult Search(Controller controller, object queryName, List<Filter> filters, int? resultsLimit, bool allowMultiple, string prefix)
        {
            QueryResult queryResult = Queries.ExecuteQuery(queryName, filters, resultsLimit);

            controller.ViewData[ViewDataKeys.ResourcesRoute] = System.Configuration.ConfigurationManager.AppSettings["RutaResources"] ?? "../../";
            controller.ViewData[ViewDataKeys.Results] = queryResult;
            controller.ViewData[ViewDataKeys.AllowMultiple] = allowMultiple;
            controller.ViewData[ViewDataKeys.PopupPrefix] = prefix;

            if (queryResult != null && queryResult.Data != null && queryResult.Data.Length > 0 && queryResult.VisibleColums.Count > 0)
            {
                int entityColumnIndex = queryResult.Columns.IndexOf(c => c.IsEntity);
                controller.ViewData[ViewDataKeys.EntityColumnIndex] = entityColumnIndex;
            }

            return new PartialViewResult
            {
                ViewName = "~/Plugin/Signum.Web.dll/Signum.Web.Views.SearchResults.ascx",
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual List<Filter> ExtractFilters(NameValueCollection form, object queryName)
        {
            List<Filter> result = new List<Filter>();

            QueryDescription queryDescription = Queries.QueryDescription(queryName);

            int index = 0;
            string name;
            object value;
            string operation;
            Type type;
            var names = form.AllKeys.Where(k => k.StartsWith("name"));
            foreach(string nameKey in names)
            {
                if (!int.TryParse(nameKey.RemoveLeft(4), out index))
                    continue;

                name = form[nameKey];
                value = form["val" + index.ToString()];
                operation = form["sel" + index.ToString()];
                type = queryDescription.Columns
                           .SingleOrDefault(c => c.Name == name)
                           .ThrowIfNullC("Invalid filter, column \"{0}\" not found".Formato(name))
                           .Type;

                if (type == typeof(bool))
                {
                    string[] vals = ((string)value).Split(',');
                    value = (vals[0] == "true") ? true : false;
                }

                if (typeof(Lazy).IsAssignableFrom(type))
                {
                    int intValue;
                    if (value!=null && int.TryParse(value.ToString(), out intValue))
                        value = Lazy.Create(Reflector.ExtractLazy(type), intValue);
                    else
                        value = null;
                }
                FilterOperation filterOperation = ((FilterOperation[])Enum.GetValues(typeof(FilterOperation))).SingleOrDefault(op => op.ToString() == operation);

                result.Add(new Filter
                {
                    Column = new Column() { Name = name, Type = type },
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
                .ThrowIfNullC("No hay un tipo asociado al nombre: " + typeUrlName);
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
                    return UrlQueryNames[urlName].ThrowIfNullC("No hay una query asociado al nombre: " + queryUrlName);
            }

            throw new ArgumentException("No hay una query asociado al nombre: " + queryUrlName);
        }

        protected internal virtual object ResolveQueryFromToStr(string queryNameToStr)
        {
            return Queries.GetQueryNames()
                .SingleOrDefault(qn => qn.ToString() == queryNameToStr)
                .ThrowIfNullC("No hay una query asociado al nombre: " + queryNameToStr);
        }

        protected internal virtual Type ResolveType(string typeName)
        {
            Type type = null;
            if (Navigator.NameToType.ContainsKey(typeName))
                type = Navigator.NameToType[typeName];
            
            if (type == null)
                throw new ArgumentException(Resource.Type0NotFoundInTheSchema);
            return type;
        }

        protected internal virtual Dictionary<string, List<string>> ApplyChangesAndValidate<T>(SortedList<string, object> formValues, ref T obj, string prefix)
        {
            Modification modification = GenerateModification(formValues, (Modifiable)(object)obj, prefix ?? "");

            obj = (T)modification.ApplyChanges(this, obj);

            return GenerateErrors((Modifiable)(object)obj, modification);
        }

        protected internal virtual Dictionary<string, List<string>> GenerateErrors(Modifiable obj, Modification modification)
        {
            Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();
            modification.Validate(obj, errors);

            Dictionary<Modifiable, string> dicGlobalErrors = obj.FullIntegrityCheckDictionary();
            //Split each error in one entry in the HashTable:
            var globalErrors = dicGlobalErrors.SelectMany(a => a.Value.Lines()).ToList();
            //eliminar de globalErrors los que ya hemos metido en el diccionario
            errors.SelectMany(a => a.Value)
                  .ForEach(e => globalErrors.Remove(e));
            //meter el resto en el diccionario
            if (globalErrors.Count > 0)
                errors.Add(ViewDataKeys.GlobalErrors, globalErrors.ToList());

            return errors;
        }

        protected internal virtual Modification GenerateModification(SortedList<string, object> formValues, Modifiable obj, string prefix)
        {
            MinMax<int> interval = Modification.FindSubInterval(formValues, prefix);

            return Modification.Create(obj.GetType(), formValues, interval, prefix);
        }

        protected internal virtual IdentifiableEntity ExtractEntity(NameValueCollection form, string prefix)
        {
            string typeName = form[(prefix ?? "") + TypeContext.Separator + TypeContext.RuntimeType];
            string id = form[(prefix ?? "") + TypeContext.Separator + TypeContext.Id];

            Type type = Navigator.NameToType.GetOrThrow(typeName, Resource.Type0NotFoundInTheSchema);

            if (!string.IsNullOrEmpty(id))
                return Database.Retrieve(type, int.Parse(id));
            else
                return (IdentifiableEntity)Constructors[type]();
        }
    }

    public static class ModelStateExtensions
    { 
        public static void FromDictionary(this ModelStateDictionary modelState, Dictionary<string, List<string>> errors, NameValueCollection form)
        {
            if (errors != null)
                errors.ForEach(p => p.Value.ForEach(v => modelState.AddModelError(p.Key, v, form[p.Key])));
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
