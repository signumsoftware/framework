using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Web.Extensions.Properties;
using Signum.Entities.Chart;
using Signum.Engine.DynamicQuery;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Entities;
using Signum.Engine.Extensions.Chart;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserQueries;
using System.Collections.Specialized;
using Signum.Engine;
using Signum.Entities.Authorization;
using Signum.Engine.Basics;
using System.Web.Script.Serialization;

namespace Signum.Web.Chart
{
    public class ChartController : Controller
    {
        #region chart
        public ActionResult Index(FindOptions findOptions)
        {
            if (!Navigator.IsFindable(findOptions.QueryName))
                throw new UnauthorizedAccessException(Resources.Chart_Query0IsNotAllowed.Formato(findOptions.QueryName));

            Navigator.SetTokens(findOptions.QueryName, findOptions.FilterOptions);

            var request = new ChartRequest(findOptions.QueryName)
            {
                Filters = findOptions.FilterOptions.Select(fo => fo.ToFilter()).ToList()
            };

            var queryDescription = DynamicQueryManager.Current.QueryDescription(request.QueryName);

            var entityColumn = queryDescription.Columns.SingleEx(a => a.IsEntity);
            Type entitiesType = Reflector.ExtractLite(entityColumn.Type);
            Implementations implementations = entityColumn.Implementations;
            
            return OpenChartRequest(request, 
                findOptions.FilterOptions,
                findOptions.View && (implementations != null || Navigator.IsViewable(entitiesType, EntitySettingsContext.Admin)));
        }

        ViewResult OpenChartRequest(ChartRequest request, List<FilterOption> filterOptions, bool view)
        { 
            var queryDescription = DynamicQueryManager.Current.QueryDescription(request.QueryName);

            ViewData[ViewDataKeys.PartialViewName] = ChartClient.ChartControlView;
            ViewData[ViewDataKeys.Title] = Navigator.Manager.SearchTitle(request.QueryName);
            ViewData[ViewDataKeys.QueryDescription] = queryDescription;
            ViewData[ViewDataKeys.FilterOptions] = filterOptions;
            ViewData[ViewDataKeys.View] = view;
            
            return View(Navigator.Manager.SearchPageView,  new TypeContext<ChartRequest>(request, ""));
        }

        [HttpPost]
        public PartialViewResult UpdateChartBuilder(string prefix)
        {
            var lastToken = Request["lastTokenChanged"];
            
            var request = ExtractChartRequestCtx(prefix, lastToken.HasText() ? (ChartTokenName?)lastToken.ToEnum<ChartTokenName>() : null).Value;   

            ViewData[ViewDataKeys.QueryDescription] = DynamicQueryManager.Current.QueryDescription(request.QueryName);
            
            return PartialView(ChartClient.ChartBuilderView, new TypeContext<ChartRequest>(request, prefix).SubContext(cr => cr.Chart));
        }

        [HttpPost]
        public ActionResult Draw(string prefix)
        {
            var requestCtx = ExtractChartRequestCtx(prefix, null).ValidateGlobal();

            if (requestCtx.GlobalErrors.Any())
            {
                ModelState.FromContext(requestCtx);
                return JsonAction.ModelState(ModelState);
            }

            var request = requestCtx.Value;

            var resultTable = ChartLogic.ExecuteChart(request);

            var queryDescription = DynamicQueryManager.Current.QueryDescription(request.QueryName);
            var querySettings = Navigator.QuerySettings(request.QueryName);

            var entityColumn = queryDescription.Columns.SingleEx(a => a.IsEntity);
            Type entitiesType = Reflector.ExtractLite(entityColumn.Type);
            Implementations implementations = entityColumn.Implementations;

            ViewData[ViewDataKeys.Results] = resultTable;

            ViewData[ViewDataKeys.View] = implementations != null || Navigator.IsViewable(entitiesType, EntitySettingsContext.Admin);
            ViewData[ViewDataKeys.Formatters] = resultTable.Columns.Select((c, i) => new { c, i }).ToDictionary(c => c.i, c => querySettings.GetFormatter(c.c.Column));

            return PartialView(ChartClient.ChartResultsView, new TypeContext<ChartRequest>(request, prefix));
        }

        MappingContext<ChartRequest> ExtractChartRequestCtx(string prefix, ChartTokenName? lastTokenChanged)
        {
            var ctx = new ChartRequest(Navigator.ResolveQueryName(Request.Form[TypeContextUtilities.Compose(prefix, ViewDataKeys.QueryName)]))
                    .ApplyChanges(this.ControllerContext, prefix, ChartClient.MappingChartRequest);

            var ch = ctx.Value.Chart;
            switch (lastTokenChanged)
            {
                case ChartTokenName.Dimension1: ch.Dimension1.TokenChanged(); break;
                case ChartTokenName.Dimension2: ch.Dimension2.TokenChanged(); break;
                case ChartTokenName.Value1: ch.Value1.TokenChanged(); break;
                case ChartTokenName.Value2: ch.Value2.TokenChanged(); break;
                default:
                    break;
            }

            return ctx;
        }

        public ActionResult OpenSubgroup(string prefix)
        {
            var chartRequest = ExtractChartRequestCtx(prefix, null).Value;

            if (chartRequest.Chart.GroupResults)
            {
                var filters = chartRequest.Filters.Select(f => new FilterOption { Token = f.Token, Value = f.Value, Operation = f.Operation }).ToList();

                var chartTokenFilters = new List<FilterOption>
                {
                    AddFilter(chartRequest, chartRequest.Chart.Dimension1, "d1"),
                    AddFilter(chartRequest, chartRequest.Chart.Dimension2, "d2"),
                    AddFilter(chartRequest, chartRequest.Chart.Value1, "v1"),
                    AddFilter(chartRequest, chartRequest.Chart.Value2, "v2")
                };

                filters.AddRange(chartTokenFilters.NotNull());

                return Navigator.PartialFind(this, new FindOptions(chartRequest.QueryName)
                {
                    FilterOptions = filters,
                    SearchOnLoad = true
                }, "New");
            }
            else
            {
                string entity = Request.Params["entity"];
                if (string.IsNullOrEmpty(entity))
                    throw new Exception("If the chart is not grouping, entity must be provided");
                 
                var queryDescription = DynamicQueryManager.Current.QueryDescription(chartRequest.QueryName);
                var querySettings = Navigator.QuerySettings(chartRequest.QueryName);

                var entityColumn = queryDescription.Columns.SingleEx(a => a.IsEntity);
                Type entitiesType = Reflector.ExtractLite(entityColumn.Type);

                Lite lite = TypeLogic.ParseLite(entitiesType, entity);
                return Navigator.PopupOpen(this, new ViewOkOptions(TypeContextUtilities.UntypedNew(Database.Retrieve(lite), "New")));
            }
        }

        private FilterOption AddFilter(ChartRequest request, ChartTokenDN chartToken, string key)
        {
            if (chartToken == null || chartToken.Aggregate != null)
                return null;

            bool hasKey = Request.Params.AllKeys.Contains(key);
            var value = hasKey ? Request.Params[key] : null;

            var token = chartToken.Token;

            return new FilterOption
            {
                Token = token,
                Operation = FilterOperation.EqualTo,
                Value = hasKey ? FindOptionsModelBinder.Convert(FindOptionsModelBinder.DecodeValue(value), token.Type) : null
            };
        }
        #endregion

        #region user chart
        public ActionResult CreateUserChart(string prefix)
        {
            var request = ExtractChartRequestCtx(prefix, null).Value;

            if (!Navigator.IsFindable(request.QueryName))
                throw new UnauthorizedAccessException(Resources.Chart_Query0IsNotAllowed.Formato(request.QueryName));

            var userChart = UserChartDN.FromRequest(request);

            userChart.Related = UserDN.Current.ToLite<IdentifiableEntity>();

            ViewData[ViewDataKeys.QueryDescription] = DynamicQueryManager.Current.QueryDescription(request.QueryName);

            return Navigator.View(this, userChart);
        }

        public ActionResult ViewUserChart(Lite<UserChartDN> lite)
        {
            UserChartDN uc = Database.Retrieve<UserChartDN>(lite);

            ChartRequest request = UserChartDN.ToRequest(uc);

            var queryDescription = DynamicQueryManager.Current.QueryDescription(request.QueryName);

            var entityColumn = queryDescription.Columns.SingleEx(a => a.IsEntity);
            Type entitiesType = Reflector.ExtractLite(entityColumn.Type);
            Implementations implementations = entityColumn.Implementations;

            return OpenChartRequest(request,
                request.Filters.Select(f => new FilterOption { Token = f.Token, Operation = f.Operation, Value = f.Value }).ToList(),
                (implementations != null || Navigator.IsViewable(entitiesType, EntitySettingsContext.Admin)));
        }

        public ActionResult DeleteUserChart(Lite<UserChartDN> lite)
        {
            var queryName = QueryLogic.ToQueryName(lite.InDB().Select(uq => uq.Query.Key).FirstEx());

            Database.Delete<UserChartDN>(lite);

            return Redirect(Navigator.FindRoute(queryName));
        }
        #endregion
    }
}