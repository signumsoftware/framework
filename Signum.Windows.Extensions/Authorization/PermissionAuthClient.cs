using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Windows;
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Services;
using Signum.Utilities;

namespace Signum.Windows.Authorization
{
    public static class PermissionAuthClient
    {
        public static bool Started { get; private set; }

        static DefaultDictionary<PermissionSymbol, bool> permissionRules;

        internal static void Start()
        {
            Started = true;

            AuthClient.UpdateCacheEvent += new Action(AuthClient_UpdateCacheEvent);

            Server.SetSymbolIds<PermissionSymbol>();

            LinksClient.RegisterEntityLinks<RoleEntity>((r, c) =>
            {
                bool authorized = BasicPermission.AdminRules.IsAuthorized();
                return new QuickLink[]
                {
                    new QuickLinkAction(AuthAdminMessage.PermissionRules, () => 
                        Navigator.OpenIndependentWindow(()=>new PermissionRules { Role = r }))
                    {
                        IsVisible = authorized
                    },
                };
            }); 
        }

        static void AuthClient_UpdateCacheEvent()
        {
            permissionRules = Server.Return((IPermissionAuthServer s) => s.PermissionRules());
        }


        public static bool IsAuthorized(this PermissionSymbol permissionSymbol)
        {
            if (!Started)
                return true;

            if (permissionRules == null)
                throw new InvalidOperationException("Permissions not enabled in AuthClient");

            return permissionRules.GetAllowed(permissionSymbol);
        }

        public static void Authorize(this PermissionSymbol permissionSymbol)
        {
            if (IsAuthorized(permissionSymbol) == false)
                throw new UnauthorizedAccessException("Permission '{0}' is denied".FormatWith(permissionSymbol));
        }
    }

    [MarkupExtensionReturnType(typeof(bool))]
    public class PermissionAllowedExtension : MarkupExtension
    {
        PermissionSymbol permission;
        public PermissionAllowedExtension(object value)
        {
            this.permission = (PermissionSymbol)value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return permission.IsAuthorized();
        }
    }

    [MarkupExtensionReturnType(typeof(Visibility))]
    public class PermissionVisiblityExtension : MarkupExtension
    {
        PermissionSymbol permission;
        public PermissionVisiblityExtension(object value)
        {
            this.permission = (PermissionSymbol)value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return permission.IsAuthorized() ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}

