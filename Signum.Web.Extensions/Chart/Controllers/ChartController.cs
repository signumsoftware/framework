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

        public PartialViewResult ChangeType(string prefix)
        {
            var request = this.ExtractEntity<ChartRequest>(prefix).ApplyChanges(this.ControllerContext, prefix, true).Value;

            ViewData[ViewDataKeys.QueryDescription] = DynamicQueryManager.Current.QueryDescription(request.QueryName);

            return PartialView(ChartClient.ChartBuilderView, new TypeContext<ChartRequest>(request, prefix));
        }
    }
}