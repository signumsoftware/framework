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
using Signum.Engine.Reports;
using Signum.Web.Controllers;

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
        public ContentResult NewSubTokensCombo(string webQueryName, string tokenName, string prefix, int index)
        {
            var request = ExtractChartRequestCtx("", null).Value;

            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(request.QueryName);

            List<QueryToken> subtokens = request.Chart.SubTokensFilters(QueryUtils.Parse(tokenName, qt => request.Chart.SubTokensFilters(qt, qd.Columns)), qd.Columns);

            if (subtokens.IsEmpty())
                return Content("");

            var tokenOptions = SearchControlHelper.TokensCombo(subtokens, null);

            return Content(
                SearchControlHelper.TokenOptionsCombo(
                    SignumController.CreateHtmlHelper(this), request.QueryName, tokenOptions, new Context(null, prefix), index + 1, true).ToHtmlString());
        }

        [HttpPost]
        public ContentResult AddFilter(string webQueryName, string tokenName, int index, string prefix)
        {
            var request = ExtractChartRequestCtx(prefix, null).Value;

            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(request.QueryName);

            object queryName = Navigator.ResolveQueryName(webQueryName);

            FilterOption fo = new FilterOption(tokenName, null);
            if (fo.Token == null)
            {
                fo.Token = QueryUtils.Parse(tokenName, qt => request.Chart.SubTokensFilters(qt, qd.Columns));
            }
            fo.Operation = QueryUtils.GetFilterOperations(QueryUtils.GetFilterType(fo.Token.Type)).FirstEx();

            return Content(
                SearchControlHelper.NewFilter(
                    SignumController.CreateHtmlHelper(this), queryName, fo, new Context(null, prefix), index).ToHtmlString());
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
            var ctx = new ChartRequest(Navigator.ResolveQueryName(Request.Params[TypeContextUtilities.Compose(prefix, ViewDataKeys.QueryName)]))
                    .ApplyChanges(this.ControllerContext, prefix, ChartClient.MappingChartRequest, Request.Params.ToSortedList(prefix));

            var chart = ctx.Value.Chart;
            if (lastTokenChanged != null)
                chart.GetToken(lastTokenChanged.Value).TokenChanged();

            return ctx;
        }

        public ActionResult OpenSubgroup(string prefix)
        {
            var chartRequest = ExtractChartRequestCtx(prefix, null).Value;

            if (chartRequest.Chart.GroupResults)
            {
                var filters = chartRequest.Filters.Where(a=>!(a.Token is AggregateToken)).Select(f => new FilterOption { Token = f.Token, Value = f.Value, Operation = f.Operation }).ToList();

                var chartTokenFilters = new List<FilterOption>
                {
                    AddFilter(chartRequest, chartRequest.Chart.Dimension1, "d1"),
                    AddFilter(chartRequest, chartRequest.Chart.Dimension2, "d2"),
                    AddFilter(chartRequest, chartRequest.Chart.Value1, "v1"),
                    AddFilter(chartRequest, chartRequest.Chart.Value2, "v2")
                };

                filters.AddRange(chartTokenFilters.NotNull());

                var findOptions = new FindOptions(chartRequest.QueryName)
                {
                    FilterOptions = filters,
                    SearchOnLoad = true
                };

                return Redirect(findOptions.ToString());
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
                return Redirect(Navigator.ViewRoute(lite));
            }
        }

        private FilterOption AddFilter(ChartRequest request, ChartTokenDN chartToken, string key)
        {
            if (chartToken == null || chartToken.Token is AggregateToken)
                return null;

            if (key == "d1" && (request.Chart.ChartType == ChartType.StackedAreas || request.Chart.ChartType == ChartType.TotalAreas))
                return null;

            bool hasKey = Request.Params.AllKeys.Contains(key);
            var value = hasKey ? Request.Params[key] : null;

            var token = chartToken.Token;

            return new FilterOption
            {
                ColumnName = token.FullKey(),
                Token = token,
                Operation = FilterOperation.EqualTo,
                Value = hasKey ? FindOptionsModelBinder.Convert(FindOptionsModelBinder.DecodeValue(value), token.Type) : null
            };
        }

        [HttpPost]
        public JsonResult Validate(string prefix)
        {
            var requestCtx = ExtractChartRequestCtx(prefix, null).ValidateGlobal();

            ModelState.FromContext(requestCtx);
            return JsonAction.ModelState(ModelState);
        }

        [HttpPost]
        public ActionResult ExportData(string prefix)
        {
            var request = ExtractChartRequestCtx(prefix, null).Value;

            if (!Navigator.IsFindable(request.QueryName))
                throw new UnauthorizedAccessException(Resources.Chart_Query0IsNotAllowed.Formato(request.QueryName));

            var resultTable = ChartLogic.ExecuteChart(request);

            byte[] binaryFile = PlainExcelGenerator.WritePlainExcel(resultTable);

            return File(binaryFile, MimeType.FromExtension(".xlsx"), Navigator.ResolveWebQueryName(request.QueryName) + ".xlsx");
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