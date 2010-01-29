using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Signum.Entities;
using System.Windows.Markup;
using Signum.Windows.Properties;
using System.Globalization;

namespace Signum.Windows
{
    public class PropertyRouteConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            try
            {
                if (context == null)
                    return PropertyRoute.Root(typeof(IdentifiableEntity)); //HACK: Improve Design-Time support

                IXamlTypeResolver resolver = (IXamlTypeResolver)context.GetService(typeof(IXamlTypeResolver));

                if (resolver == null)
                    return PropertyRoute.Root(typeof(IdentifiableEntity)); //HACK: Improve Design-Time support

                if (value == null)
                    throw new Exception("value is null");

                return PropertyRoute.Root(resolver.Resolve((string)value));
            }
            catch (Exception e)
            {
                throw new Exception(Resources.ConvertingToTypeContext + e.Message);
            }
        }
    }
}
