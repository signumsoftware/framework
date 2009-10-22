using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using Signum.Services;
using System.Windows;

namespace Signum.Windows.Authorization
{
    [MarkupExtensionReturnType(typeof(bool))]
    public class PermissionExtension : MarkupExtension
    {
        Enum permission;
        public PermissionExtension(Enum value)
        {
            this.permission = permission;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Server.Service<IPermissionAuthServer>().IsAuthorizedFor(permission);
        }
    }

    [MarkupExtensionReturnType(typeof(Visibility))]
    public class PermissionVisiblityExtension : MarkupExtension
    {
        Enum permission;
        public PermissionVisiblityExtension(Enum value)
        {
            this.permission = permission;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Server.Service<IPermissionAuthServer>().IsAuthorizedFor(permission) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
