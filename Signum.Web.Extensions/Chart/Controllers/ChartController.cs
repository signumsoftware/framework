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

namespace Signum.Web.Chart
{
    public class ChartController : Controller
    {
        public ActionResult Index(FindOptions findOptions)
        {
            if (!Navigator.IsFindable(findOptions.QueryName))
                throw new UnauthorizedAccessException(Resources.Chart_Query0IsNotAllowed.Formato(findOptions.QueryName));

            Navigator.SetTokens(findOptions.QueryName, findOptions.FilterOptions);

            var request = new ChartRequest(findOptions.QueryName)
            {
                Filters = findOptions.FilterOptions.Select(fo => fo.ToFilter()).ToList()
            };
            
            var queryDescription = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);

            var entityColumn = queryDescription.Columns.SingleEx(a => a.IsEntity);
            Type entitiesType = Reflector.ExtractLite(entityColumn.Type);
            Implementations implementations = entityColumn.Implementations;
            
            ViewData[ViewDataKeys.PartialViewName] = ChartClient.ChartControlView;
            ViewData[ViewDataKeys.Title] = Navigator.Manager.SearchTitle(findOptions.QueryName);
            ViewData[ViewDataKeys.QueryDescription] = queryDescription;
            ViewData[ViewDataKeys.FilterOptions] = findOptions.FilterOptions;
            ViewData[ViewDataKeys.View] = findOptions.View && (implementations != null || Navigator.IsViewable(entitiesType, EntitySettingsContext.Admin));
            
            return View(Navigator.Manager.SearchPageView,  new TypeContext<ChartRequest>(request, ""));
        }

        [HttpPost]
        public PartialViewResult UpdateChartBuilder(string prefix)
        {
            var request = ExtractChartRequestCtx(prefix).Value;   

            ViewData[ViewDataKeys.QueryDescription] = DynamicQueryManager.Current.QueryDescription(request.QueryName);
            
            return PartialView(ChartClient.ChartBuilderView, new TypeContext<ChartRequest>(request, prefix));
        }

        [HttpPost]
        public ActionResult Draw(string prefix)
        {
            var requestCtx = ExtractChartRequestCtx(prefix).ValidateGlobal();

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

        MappingContext<ChartRequest> ExtractChartRequestCtx(string prefix)
        {
            return new ChartRequest(Navigator.ResolveQueryName(Request.Form[TypeContextUtilities.Compose(prefix, ViewDataKeys.QueryName)]))
                    .ApplyChanges(this.ControllerContext, prefix, mappingChartRequest);
        }

        static EntityMapping<ChartTokenDN> mappingChartToken = new EntityMapping<ChartTokenDN>(true)
            .SetProperty(ct => ct.Token, ctx =>
            {
                var tokenName = "";

                var chartTokenInputs = ctx.Parent.Inputs;
                bool stop = false;
                for (var i = 0; !stop; i++)
                {
                    var subtokenName = chartTokenInputs.TryGetC("ddlTokens_" + i);
                    if (string.IsNullOrEmpty(subtokenName))
                        stop = true;
                    else
                        tokenName = tokenName.HasText() ? (tokenName + "." + subtokenName) : subtokenName;
                }

                if (string.IsNullOrEmpty(tokenName))
                    return null;

                var qd = DynamicQueryManager.Current.QueryDescription(
                    Navigator.ResolveQueryName(ctx.GlobalInputs[TypeContextUtilities.Compose(ctx.Root.ControlID, ViewDataKeys.QueryName)]));

                return QueryUtils.Parse(tokenName, qd);
            })
            .SetProperty(ct => ct.DisplayName, ctx =>
            {
                if (string.IsNullOrEmpty(ctx.Input))
                    return ctx.None();

                return ctx.Input;
            });

        static List<Entities.DynamicQuery.Filter> ExtractChartFilters(MappingContext<List<Entities.DynamicQuery.Filter>> ctx)
        {
            var qd = DynamicQueryManager.Current.QueryDescription(
                Navigator.ResolveQueryName(ctx.GlobalInputs[TypeContextUtilities.Compose(ctx.Root.ControlID, ViewDataKeys.QueryName)]));

            return FindOptionsModelBinder.ExtractFilterOptions(ctx.ControllerContext.HttpContext, qd).Select(fo => fo.ToFilter()).ToList();
        }
        
        static EntityMapping<ChartRequest> mappingChartRequest = new EntityMapping<ChartRequest>(true)
            .SetProperty(cr => cr.Chart, new EntityMapping<ChartBase>(true)
                .SetProperty(cb => cb.Dimension1, mappingChartToken)
                .SetProperty(cb => cb.Dimension2, mappingChartToken)
                .SetProperty(cb => cb.Value1, mappingChartToken)
                .SetProperty(cb => cb.Value2, mappingChartToken))
            .SetProperty(cr => cr.Filters, ctx => ExtractChartFilters(ctx));

        public ActionResult OpenSubgroup(string prefix)
        {
            var chartRequest = ExtractChartRequestCtx(prefix).Value;

            if (chartRequest.Chart.GroupResults)
            {
                var filters = chartRequest.Filters.Select(f => new FilterOption { Token = f.Token, Value = f.Value, Operation = f.Operation }).ToList();

                var chartTokenFilters = new List<FilterOption>
                {
                    AddFilter(chartRequest.Chart.Dimension1, "d1"),
                    AddFilter(chartRequest.Chart.Dimension2, "d2"),
                    AddFilter(chartRequest.Chart.Value1, "v1"),
                    AddFilter(chartRequest.Chart.Value2, "v2")
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

        private FilterOption AddFilter(ChartTokenDN chartToken, string key)
        {
            if (chartToken == null || chartToken.Aggregate != null)
                return null;
            
            var token = chartToken.Token;
            return new FilterOption
            {
                Token = token,
                Operation = FilterOperation.EqualTo,
                Value = FindOptionsModelBinder.Convert(FindOptionsModelBinder.DecodeValue(Request.Params[key]), token.Type)
            };
        }
    }
}