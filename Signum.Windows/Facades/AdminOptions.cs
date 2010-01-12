using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;

namespace Signum.Windows
{
    public class AdminOptions
    {
        public Type Type { get; set; }

    }

    public class Admin: MarkupExtension
    {
        public Type Type { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new AdminOptions { Type = Type };
        }
    }
}
