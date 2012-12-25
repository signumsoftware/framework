using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Services;
using Signum.Entities.Authorization;
using Signum.Entities;
using System.Windows;
using System.Windows.Markup;

namespace Signum.Windows.Authorization
{
    public static class FacadeMethodAuthClient
    {
        public static bool Started { get; private set; }

        static DefaultDictionary<string, bool> autorizedFacadeMethods;

        internal static void Start()
        {
            Started = true;

            Links.RegisterEntityLinks<RoleDN>((r, c) =>
            {
                bool authorized = BasicPermission.AdminRules.TryIsAuthorized() ?? true;
                return new QuickLink[]
                {
                    new QuickLinkAction("Facade Method Rules", () => new FacadeMethodRules { Role = r.ToLite(), Owner = Window.GetWindow(c) }.Show())
                    { 
                        IsVisible = authorized
                    },
                };
            });

            AuthClient.UpdateCacheEvent += new Action(AuthClient_UpdateCacheEvent);
        }

        static void AuthClient_UpdateCacheEvent()
        {
            autorizedFacadeMethods = Server.Return((IFacadeMethodAuthServer s) => s.FacadeMethodRules());
        }

        public static bool GetAllowed(string facadeMethodName)
        {
            return autorizedFacadeMethods.GetAllowed(facadeMethodName);
        }
    }


    [MarkupExtensionReturnType(typeof(bool))]
    public class FacadeMethodAllowedExtension : MarkupExtension
    {
        string methodName;
        public FacadeMethodAllowedExtension(object value)
        {
            this.methodName = (string)value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return FacadeMethodAuthClient.GetAllowed(methodName);
        }
    }

    [MarkupExtensionReturnType(typeof(Visibility))]
    public class FacadeMethodVisiblityExtension : MarkupExtension
    {
        string methodName;
        public FacadeMethodVisiblityExtension(object value)
        {
            this.methodName = (string)value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return FacadeMethodAuthClient.GetAllowed(methodName) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
