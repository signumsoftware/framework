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
        Type type;
        public Type Type
        {
            get { return type; }
            set { type = value; }
        }

        bool sort;
        public bool Sort
        {
            get { return sort; }
            set { sort = value; }
        }

        bool nicePairs;
        public bool NicePairs
        {
            get { return nicePairs; }
            set { nicePairs = value; }
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var values = Enum.GetValues(Type).Cast<Enum>();
            if (nicePairs)
            {
                var valuePairs = values.Select(a => new Tuple<Enum, string>(a, EnumExtensions.NiceToString(a)));
                if (sort)
                    valuePairs = valuePairs.OrderBy(a => a.Second);

                return valuePairs.ToArray();
            }
            else
            {
                if (sort)
                    values = values.OrderBy(a => a.ToString());
                return values.ToArray();
            }
        }
    }
}
