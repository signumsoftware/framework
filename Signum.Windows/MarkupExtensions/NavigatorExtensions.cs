using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Windows;

namespace Signum.Windows.MarkupExtensions
{
    [MarkupExtensionReturnType(typeof(bool))]
    public class IsFindableExtension : MarkupExtension
    {
        string queryName;
        public IsFindableExtension(object queryName)
        {
            this.queryName = (string)queryName;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Finder.IsFindable(queryName);
        }
    }

    [MarkupExtensionReturnType(typeof(Visibility))]
    public class IsFindableVisibilityExtension : MarkupExtension
    {
        string queryName;
        public IsFindableVisibilityExtension(object queryName)
        {
            this.queryName = (string)queryName;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Finder.IsFindable(queryName) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
