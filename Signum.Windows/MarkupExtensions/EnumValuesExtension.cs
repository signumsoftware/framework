using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Windows.Data;
using System.ComponentModel;
using Signum.Utilities;
using Signum.Utilities.DataStructures;

namespace Signum.Windows
{
    [MarkupExtensionReturnType(typeof(object[]))]
    [DefaultProperty("Type")]
    public class EnumValuesExtension : MarkupExtension
    {
        public Type Type {get;set;}
        public bool Sort { get; set; }
        public bool Nullable { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var values = Enum.GetValues(Type).Cast<Enum>();

            if (Sort)
                values = values.OrderBy(a => a.NiceToString());

            if (Nullable)
                values = values.PreAndNull(); 

            return values.ToArray();
        }
    }
}
