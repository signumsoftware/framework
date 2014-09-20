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
        public static string FindRoute(object queryName)
        {
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

        public static PartialViewResult Search(ControllerBase controller, QueryRequest request, bool allowSelection, bool navigate, bool showFooter, string prefix)
        {
            return Manager.Search(controller, request, allowSelection, navigate, showFooter, new Context(null, prefix));
        }

        public static Lite<IdentifiableEntity> FindUnique(FindUniqueOptions options)
        {
            return Manager.FindUnique(options);
        }

        public static int QueryCount(CountOptions options)
        {
            return Manager.QueryCount(options);
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

        public static List<Lite<T>> ParseLiteKeys<T>(this ControllerBase controller) where T : class, IIdentifiable
        {
            return ParseLiteKeys<T>(controller.ControllerContext.RequestContext.HttpContext.Request["liteKeys"]);
        }

        public static List<Lite<T>> ParseLiteKeys<T>(string liteKeys) where T : class, IIdentifiable
        {
            return liteKeys.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(Lite.Parse<T>).ToList();
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
    }

    public class FinderManager
    {
        public static string ViewPrefix = "~/Signum/Views/{0}.cshtml";

        
        public string SearchPopupControlView = ViewPrefix.Formato("SearchPopupControl");
        public string SearchPageView = ViewPrefix.Formato("SearchPage");
        public string SearchControlView = ViewPrefix.Formato("SearchControl");
        public string SearchResultsView = ViewPrefix.Formato("SearchResults");
        public string FilterBuilderView = ViewPrefix.Formato("FilterBuilder");
        public string PaginationSelectorView = ViewPrefix.Formato("PaginationSelector");

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

                    WebQueryNames = QuerySettings.ToDictionary(kvp => kvp.Value.WebQueryName, kvp => kvp.Key, StringComparer.InvariantCultureIgnoreCase, "WebQueryNames");
                }

                if (Initializing != null)
                    Initializing();

                Initialized = true;
            }
        }

        protected internal virtual ViewResult SearchPage(ControllerBase controller, FindOptions findOptions)
        {
            if (!Finder.IsFindable(findOptions.QueryName))
                throw new UnauthorizedAccessException(SearchMessage.Query0IsNotAllowed.NiceToString().Formato(findOptions.QueryName));

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
                f.Token = QueryUtils.Parse(f.ColumnName, queryDescription, SubTokensOptions.CanAnyAll| SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0));
        }

        protected internal void SetTokens(List<OrderOption> orders, QueryDescription queryDescription, bool canAggregate)
        {
            foreach (var o in orders)
                o.Token = QueryUtils.Parse(o.ColumnName, queryDescription, SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0));
        }

        protected internal void SetTokens(List<ColumnOption> columns, QueryDescription queryDescription, bool canAggregate)
        {
            foreach (var o in columns)
                o.Token = QueryUtils.Parse(o.ColumnName, queryDescription, SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0));
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
        
        protected internal virtual PartialViewResult SearchPopup(ControllerBase controller, FindOptions findOptions, FindMode mode, Context context)
        {
            if (!Finder.IsFindable(findOptions.QueryName))
                throw new UnauthorizedAccessException(NormalControlMessage.ViewForType0IsNotAllowed.NiceToString().Formato(findOptions.QueryName));

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
                throw new UnauthorizedAccessException(NormalControlMessage.ViewForType0IsNotAllowed.NiceToString().Formato(request.QueryName));

            ResultTable queryResult = DynamicQueryManager.Current.ExecuteQuery(request);
            
            controller.ViewData.Model = context;

            controller.ViewData[ViewDataKeys.AllowSelection] = allowSelection;
            controller.ViewData[ViewDataKeys.Navigate] = navigate;
            controller.ViewData[ViewDataKeys.ShowFooter] = showFooter;

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
    }
}