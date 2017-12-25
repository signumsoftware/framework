using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Engine;

namespace Signum.Web
{
    public static class Finder
    {
        public static FinderManager Manager;

        public static void Start(FinderManager manager)
        {
            Manager = manager;
        }

        public const string FindRouteName = "sfFind";

        public static Func<UrlHelper, object, string> FindRouteFunc;
        public static string FindRoute(object queryName)
        {

            if (FindRouteFunc != null)
                return FindRouteFunc(new UrlHelper(HttpContext.Current.Request.RequestContext), queryName);

            return new UrlHelper(HttpContext.Current.Request.RequestContext).RouteUrl(FindRouteName, new
            {
                webQueryName = ResolveWebQueryName(queryName)
            });
        }

        public static ViewResult SearchPage(this ControllerBase controller, FindOptions findOptions)
        {
            return Manager.SearchPage(controller, findOptions);
        }

        public static PartialViewResult SearchPopup(this ControllerBase controller, FindOptions findOptions, FindMode mode, Context context)
        {
            return Manager.SearchPopup(controller, findOptions, mode, context);
        }

        public static PartialViewResult SearchPopup(this ControllerBase controller, FindOptions findOptions, FindMode mode, string prefix)
        {
            return Manager.SearchPopup(controller, findOptions, mode, new Context(null, prefix));
        }

        public static PartialViewResult SearchResults(ControllerBase controller, QueryRequest request, bool allowSelection, bool navigate, bool showFooter, string prefix)
        {
            return Manager.Search(controller, request, allowSelection, navigate, showFooter, new Context(null, prefix));
        }

        public static Lite<Entity> FindUnique(UniqueOptions options)
        {
            return Manager.FindUnique(options);
        }

        public static int QueryCount(CountOptions options)
        {
            return Manager.QueryCount(options);
        }

        public static void AddQuerySettings(List<QuerySettings> settings)
        {
            Manager.QuerySettings.AddRange(settings, s => s.QueryName, s => s, "QuerySettings");
        }

        public static void AddQuerySetting(QuerySettings setting)
        {
            Manager.QuerySettings.AddOrThrow(setting.QueryName, setting, "QuerySettings {0} repeated");
        }

        public static QuerySettings QuerySettings(object queryName)
        {
            return Manager.QuerySettings.GetOrThrow(queryName, "no QuerySettings for queryName {0} found");
        }

        public static List<Lite<T>> ParseLiteKeys<T>(this ControllerBase controller) where T : class, IEntity
        {
            return ParseLiteKeys<T>(controller.ControllerContext.RequestContext.HttpContext.Request["liteKeys"]);
        }

        public static List<Lite<T>> ParseLiteKeys<T>(string liteKeys) where T : class, IEntity
        {
            return liteKeys.SplitNoEmpty(",").Select(Lite.Parse<T>).ToList();
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

        public static ActionResult SimpleFilterBuilderResult(ControllerBase controller, List<FilterOption> filterOptions)
        {
            return Manager.SimpleFilterBuilderResult(controller, filterOptions);
        }
    }

    public class FinderManager
    {
        public static string ViewPrefix = "~/Signum/Views/{0}.cshtml";

        public Func<bool> AllowChangeColumns = () => true;
        public Func<bool> AllowOrder = () => true;

        public string SearchPopupControlView = ViewPrefix.FormatWith("SearchPopupControl");
        public string SearchPageView = ViewPrefix.FormatWith("SearchPage");
        public string SearchControlView = ViewPrefix.FormatWith("SearchControl");
        public string SearchResultsView = ViewPrefix.FormatWith("SearchResults");
        public string FilterBuilderView = ViewPrefix.FormatWith("FilterBuilder");
        public string FilterRowsView = ViewPrefix.FormatWith("FilterRows");
        public string PaginationSelectorView = ViewPrefix.FormatWith("PaginationSelector");

        public Dictionary<object, QuerySettings> QuerySettings { get; set; }
        protected Dictionary<string, object> WebQueryNames { get; private set; }

        public FinderManager()
        {
            QuerySettings = new Dictionary<object, QuerySettings>();
        }

        public event Action Initializing;
        public bool Initialized { get; private set; }

        internal void Initialize()
        {
            if (!Initialized)
            {
                if (DynamicQueryManager.Current != null)
                {
                    foreach (object o in DynamicQueryManager.Current.GetQueryNames())
                    {
                        if (!QuerySettings.ContainsKey(o))
                            QuerySettings.Add(o, new QuerySettings(o));
                        if (!QuerySettings[o].WebQueryName.HasText())
                            QuerySettings[o].WebQueryName = GenerateWebQueryName(o);
                    }

                    WebQueryNames = QuerySettings.ToDictionaryEx(
                        kvp => kvp.Value.WebQueryName,
                        kvp => kvp.Key,
                        StringComparer.InvariantCultureIgnoreCase,
                        "WebQueryNames");
                }

                if (Initializing != null)
                    Initializing();

                Initialized = true;
            }
        }

        protected internal virtual ViewResult SearchPage(ControllerBase controller, FindOptions findOptions)
        {
            if (!Finder.IsFindable(findOptions.QueryName))
                throw new UnauthorizedAccessException(SearchMessage.Query0IsNotAllowed.NiceToString().FormatWith(findOptions.QueryName));

            QueryDescription description = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);

            //Finder.SetTokens(findOptions.FilterOptions, description, canAggregate: false);
            SetSearchViewableAndCreable(findOptions, description);
            SetDefaultOrder(findOptions, description);

            controller.ViewData.Model = new Context(null, "");

            controller.ViewData[ViewDataKeys.QueryDescription] = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);
            controller.ViewData[ViewDataKeys.FindOptions] = findOptions;

            if (!controller.ViewData.ContainsKey(ViewDataKeys.Title))
                controller.ViewData[ViewDataKeys.Title] = QueryUtils.GetNiceName(findOptions.QueryName);
            
            return new ViewResult()
            {
                ViewName = SearchPageView,
                MasterName = null,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual Lite<Entity> FindUnique(UniqueOptions options)
        {
            var queryDescription = DynamicQueryManager.Current.QueryDescription(options.QueryName);

            FilterOption.SetFilterTokens(options.FilterOptions, queryDescription, canAggregate: false);
            OrderOption.SetOrderTokens(options.OrderOptions, queryDescription, canAggregate: false);

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

            FilterOption.SetFilterTokens(options.FilterOptions, queryDescription, canAggregate: false);

            var request = new QueryValueRequest
            { 
                QueryName = options.QueryName,
                Filters = options.FilterOptions.Select(f => f.ToFilter()).ToList()
            };

            return (int)DynamicQueryManager.Current.ExecuteQueryCount(request);
        }


    


        
        public virtual void SetSearchViewableAndCreable(FindOptions findOptions, QueryDescription description)
        {
            var entityColumn = description.Columns.SingleEx(a => a.IsEntity);
            Type entitiesType = Lite.Extract(entityColumn.Type);
            Implementations? implementations = entityColumn.Implementations;

            if (findOptions.Navigate)
            {
                findOptions.Navigate = implementations.Value.IsByAll ? true : implementations.Value.Types.Any(t => Navigator.IsNavigable(t, null, true));
            }
            if (findOptions.Create)
            {
                findOptions.Create = (implementations.Value.IsByAll ? true : implementations.Value.Types.Any(t => Navigator.IsCreable(t, true)));
            }
        }
        
        internal static void SetDefaultOrder(FindOptions findOptions, QueryDescription description)
        {
            var entityColumn = description.Columns.SingleOrDefaultEx(cd => cd.IsEntity);

            if (findOptions.OrderOptions.IsNullOrEmpty() && !entityColumn.Implementations.Value.IsByAll)
            {
                var orderType = entityColumn.Implementations.Value.Types.All(t => EntityKindCache.GetEntityData(t) == EntityData.Master) ? OrderType.Ascending : OrderType.Descending;

                var settings = Finder.QuerySettings(description.QueryName);
                
                var column = description.Columns.SingleOrDefaultEx(c => c.Name == settings.DefaultOrderColumn);

                if (column != null)
                {
                    findOptions.OrderOptions.Add(new OrderOption{ Token = new ColumnToken(column, description.QueryName), ColumnName = column.Name, OrderType = orderType });
                }
            }
        }
        
        protected internal virtual PartialViewResult SearchPopup(ControllerBase controller, FindOptions findOptions, FindMode mode, Context context)
        {
            if (!Finder.IsFindable(findOptions.QueryName))
                throw new UnauthorizedAccessException(NormalControlMessage.ViewForType0IsNotAllowed.NiceToString().FormatWith(findOptions.QueryName));

            var desc =  DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);

            SetSearchViewableAndCreable(findOptions, desc);
            SetDefaultOrder(findOptions, desc);

            controller.ViewData.Model = context;

            controller.ViewData[ViewDataKeys.FindMode] = mode;
            controller.ViewData[ViewDataKeys.FindOptions] = findOptions;
            controller.ViewData[ViewDataKeys.QueryDescription] = desc;
            
            if (!controller.ViewData.ContainsKey(ViewDataKeys.Title))
                controller.ViewData[ViewDataKeys.Title] = QueryUtils.GetNiceName(findOptions.QueryName);
            
            return new PartialViewResult
            {
                ViewName = SearchPopupControlView,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual PartialViewResult Search(ControllerBase controller, QueryRequest request, bool allowSelection, bool navigate, bool showFooter, Context context)
        {
            if (!Finder.IsFindable(request.QueryName))
                throw new UnauthorizedAccessException(NormalControlMessage.ViewForType0IsNotAllowed.NiceToString().FormatWith(request.QueryName));

            QuerySettings settings = QuerySettings[request.QueryName];
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(request.QueryName);

            if(settings.HiddenColumns != null)
            {
                if (settings.HiddenColumns.Any(a => a.Token == null))
                    using (ExecutionMode.Global())
                        ColumnOption.SetColumnTokens(settings.HiddenColumns, qd, canAggregate: false);

                request.Columns.AddRange(settings.HiddenColumns.Select(c => c.ToColumn(qd, isVisible: false)));
            }

            ResultTable queryResult = DynamicQueryManager.Current.ExecuteQuery(request);

            controller.ViewData.Model = context;

            controller.ViewData[ViewDataKeys.AllowSelection] = allowSelection;
            controller.ViewData[ViewDataKeys.Navigate] = navigate;
            controller.ViewData[ViewDataKeys.ShowFooter] = showFooter;

          
            controller.ViewData[ViewDataKeys.QueryDescription] = qd;

            Type entitiesType = Lite.Extract(qd.Columns.SingleEx(a => a.IsEntity).Type);
            string message = CollectionElementToken.MultipliedMessage(request.Multiplications(), entitiesType);
            if (message.HasText())
                controller.ViewData[ViewDataKeys.MultipliedMessage] = message;

            controller.ViewData[ViewDataKeys.Results] = queryResult;
            controller.ViewData[ViewDataKeys.QueryRequest] = request;

            controller.ViewData[ViewDataKeys.Formatters] = queryResult.Columns.Select((c, i)=>new {c,i}).ToDictionary(c=>c.i, c =>settings.GetFormatter(c.c.Column));
            controller.ViewData[ViewDataKeys.EntityFormatter] = settings.EntityFormatter;
            controller.ViewData[ViewDataKeys.RowAttributes] = settings.RowAttributes;

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
                var es = Navigator.Manager.EntitySettings.TryGetC(type);
                if (es != null)
                    return es.WebTypeName;

                return TypeLogic.TryGetCleanName(type) ?? Reflector.CleanTypeName(type);
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

        public event Func<object, bool> IsFindable;

        internal protected virtual bool OnIsFindable(object queryName)
        {
            QuerySettings es = QuerySettings.TryGetC(queryName);
            if (es == null || !es.IsFindable)
                return false;

            if (IsFindable != null)
                foreach (var isFindable in IsFindable.GetInvocationListTyped())
                {
                    if (!isFindable(queryName))
                        return false;
                }

            return true;
        }

        protected internal virtual ActionResult SimpleFilterBuilderResult(ControllerBase controller, List<FilterOption> filterOptions)
        {
            object queryName = Finder.ResolveQueryName(controller.ParseValue<string>("webQueryName"));

            var qd = DynamicQueryManager.Current.QueryDescription(queryName);

            FilterOption.SetFilterTokens(filterOptions, qd, canAggregate: false);

            if (controller.ParseValue<bool>("returnHtml"))
            {
                controller.ViewData.Model = new Context(null, controller.Prefix());
                controller.ViewData[ViewDataKeys.FilterOptions] = filterOptions;

                return new PartialViewResult
                {
                    ViewName = FilterRowsView,
                    ViewData = controller.ViewData,
                };
            }
            else
            {
                return new ContentResult
                {
                    Content = filterOptions.ToString(";")
                };
            }
        }
    }
}