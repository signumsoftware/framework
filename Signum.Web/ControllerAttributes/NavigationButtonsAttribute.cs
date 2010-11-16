using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Net;
using System.Web;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Web.Security;
using Signum.Utilities;
using System.Web.Routing;
using Signum.Engine.Exceptions;

namespace Signum.Web
{
    /// <summary>
    /// Muestra u oculta los botones de navegación inferiores
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class NavigationButtonsAttribute : FilterAttribute, IActionFilter
    {
        public bool Show { get; set; }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            filterContext.Controller.ViewData[ViewDataKeys.NavigationButtons] = Show;
        }

        #region IActionFilter Members

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            //throw new NotImplementedException();
        }

        #endregion
    }
}