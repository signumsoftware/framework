using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Signum.Web
{
    public class ActionFilerConfigControllerActionInvoker : ControllerActionInvoker
    {
        protected override FilterInfo GetFilters(ControllerContext controllerContext,
                                                 ActionDescriptor actionDescriptor)
        {
            var filters = base.GetFilters(controllerContext, actionDescriptor);

            if (ConfigActionFilter.Config.ContainsKey("Controller"))
                AddFiltersToFilerList(actionDescriptor, filters, "Controller");

            var controllerName = controllerContext.Controller.GetType().Name;

            if (ConfigActionFilter.Config.ContainsKey(controllerName))
                AddFiltersToFilerList(actionDescriptor, filters, controllerName);

            return filters;
        }


        private void AddFiltersToFilerList(ActionDescriptor actionDescriptor,
                                           FilterInfo filters,
                                           string controllerName)
        {
            var config = ConfigActionFilter.Config[controllerName];

            if (config != null)
            {
                AddFiltersToFilterList(config.ActionFilterAddedToController, filters);

                if (config.ActionFilterAddedToActions.ContainsKey(actionDescriptor.ActionName))
                {
                    AddFiltersToFilterList(
                                config.ActionFilterAddedToActions[actionDescriptor.ActionName],
                                filters);
                }
            }
        }


        private void AddFiltersToFilterList(
                                    IEnumerable<FilterAttribute> actionFilters,
                                    FilterInfo filters)
        {
            if (actionFilters != null)
            {
                foreach (var actionFilter in actionFilters)
                {
                    AddFilterToFilterList<IActionFilter>(actionFilter, filters.ActionFilters);
                    AddFilterToFilterList<IResultFilter>(actionFilter, filters.ResultFilters);
                    AddFilterToFilterList<IAuthorizationFilter>(actionFilter, filters.AuthorizationFilters);
                    AddFilterToFilterList<IExceptionFilter>(actionFilter, filters.ExceptionFilters);
                }
            }
        }


        private static void AddFilterToFilterList<TFilter>(
                                                FilterAttribute filter,
                                                IList<TFilter> filterList) where TFilter : class
        {
            TFilter item = filter as TFilter;
            if (item != null)
            {
                filterList.Insert(0, item);
            }
        }
    }
}
