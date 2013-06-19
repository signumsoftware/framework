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

        static DefaultDictionary<Enum, bool> permissionRules;

        internal static void Start()
        {
            Started = true;

            AuthClient.UpdateCacheEvent += new Action(AuthClient_UpdateCacheEvent);

            LinksClient.RegisterEntityLinks<RoleDN>((r, c) =>
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


        public static bool IsAuthorized(this Enum permissionKey)
        {
            if (!Started)
                return true;

            if (permissionRules == null)
                throw new InvalidOperationException("Permissions not enabled in AuthClient");

            return permissionRules.GetAllowed(permissionKey);
        }

        public static void Authorize(this Enum permissionKey)
        {
            if (IsAuthorized(permissionKey) == false)
                throw new UnauthorizedAccessException("Permission '{0}' is denied".Formato(permissionKey));
        }
    }

    [MarkupExtensionReturnType(typeof(bool))]
    public class PermissionAllowedExtension : MarkupExtension
    {
        Enum permission;
        public PermissionAllowedExtension(object value)
        {
            this.permission = (Enum)value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return permission.IsAuthorized();
        }
    }

    [MarkupExtensionReturnType(typeof(Visibility))]
    public class PermissionVisiblityExtension : MarkupExtension
    {
        Enum permission;
        public PermissionVisiblityExtension(object value)
        {
            this.permission = (Enum)value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return permission.IsAuthorized() ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}

