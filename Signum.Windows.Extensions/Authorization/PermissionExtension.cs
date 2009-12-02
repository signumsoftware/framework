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
        public PermissionExtension(object value)
        {
            this.permission = (Enum) value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Server.Return((IPermissionAuthServer s) => s.IsAuthorized(permission)); 
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
            return Server.Return((IPermissionAuthServer s) => s.IsAuthorized(permission)) ? Visibility.Visible : Visibility.Collapsed; 
        }
    }
}
