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

            var entityColumn = queryDescription.Columns.Single(a => a.IsEntity);
            Type entitiesType = Reflector.ExtractLite(entityColumn.Type);
            Implementations implementations = entityColumn.Implementations;
            
            ViewData[ViewDataKeys.PartialViewName] = ChartClient.ChartControlView;
            ViewData[ViewDataKeys.Title] = Navigator.Manager.SearchTitle(findOptions.QueryName);
            ViewData[ViewDataKeys.QueryDescription] = queryDescription;
            ViewData[ViewDataKeys.FilterOptions] = findOptions.FilterOptions;
            ViewData[ViewDataKeys.View] = findOptions.View && (implementations != null || Navigator.IsViewable(entitiesType, true));
            
            return View(Navigator.Manager.SearchPageView,  new TypeContext<ChartRequest>(request, ""));
        }

        [HttpPost]
        public PartialViewResult ChangeType(string prefix)
        {
            var request = ExtractChartRequest(prefix);   

            ViewData[ViewDataKeys.QueryDescription] = DynamicQueryManager.Current.QueryDescription(request.QueryName);
            
            return PartialView(ChartClient.ChartBuilderView, new TypeContext<ChartRequest>(request, prefix));
        }

        [HttpPost]
        public PartialViewResult Draw(string prefix)
        {
            var request = ExtractChartRequest(prefix);

            var resultTable = ChartLogic.ExecuteChart(request);

            var queryDescription = DynamicQueryManager.Current.QueryDescription(request.QueryName);
            var querySettings = Navigator.QuerySettings(request.QueryName);

            var entityColumn = queryDescription.Columns.Single(a => a.IsEntity);
            Type entitiesType = Reflector.ExtractLite(entityColumn.Type);
            Implementations implementations = entityColumn.Implementations;

            ViewData[ViewDataKeys.Results] = resultTable;

            ViewData[ViewDataKeys.View] = implementations != null || Navigator.IsViewable(entitiesType, true);
            ViewData[ViewDataKeys.Formatters] = resultTable.Columns.Select((c, i) => new { c, i }).ToDictionary(c => c.i, c => querySettings.GetFormatter(c.c.Column));

            return PartialView(ChartClient.ChartResultsView, new TypeContext<ChartRequest>(request, prefix));
        }        

        ChartRequest ExtractChartRequest(string prefix)
        {
            return new ChartRequest(Navigator.ResolveQueryName(Request.Form[TypeContextUtilities.Compose(prefix, ViewDataKeys.QueryName)]))
                    .ApplyChanges(this.ControllerContext, prefix, mappingChartRequest).Value;
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
            });

        static EntityMapping<ChartRequest> mappingChartRequest = new EntityMapping<ChartRequest>(true)
            .SetProperty(cr => cr.Chart, new EntityMapping<ChartBase>(true)
                .SetProperty(cb => cb.FirstDimension, mappingChartToken)
                .SetProperty(cb => cb.SecondDimension, mappingChartToken)
                .SetProperty(cb => cb.FirstValue, mappingChartToken)
                .SetProperty(cb => cb.SecondValue, mappingChartToken));

    }
}