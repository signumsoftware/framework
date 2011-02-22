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
    /// Checks the User's authentication using FormsAuthentication
    /// and redirects to the Login Url for the application on fail
    /// </summary>
    public class AuthenticationRequiredAttribute : AuthorizeAttribute
    {
        public static Action<AuthorizationContext> Authenticate;

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (Authenticate != null)
                Authenticate(filterContext);
        }
    }
}