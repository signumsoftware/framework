using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Signum.Entities;
using System.Windows.Markup;
using Signum.Windows.Properties;
using System.Globalization;
using System.Text.RegularExpressions;
using Signum.Utilities;
using System.Windows;

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
                if (value == null)
                    throw new Exception("value is null");

                string val = (string)value;

                if (context == null)
                    return PropertyRoute.Root(typeof(IdentifiableEntity)); //HACK: Improve Design-Time support

                IXamlTypeResolver resolver = (IXamlTypeResolver)context.GetService(typeof(IXamlTypeResolver));
                if (resolver == null)
                    return PropertyRoute.Root(typeof(IdentifiableEntity)); //HACK: Improve Design-Time support

                return PropertyRoute.Root(resolver.Resolve(val));
            }
            catch (Exception e)
            {
                throw new Exception(Resources.ConvertingToTypeContext + e.Message);
            }
        } 
    }

    [MarkupExtensionReturnType(typeof(PropertyRoute))]
    public class ContinueRouteExtension : MarkupExtension
    {
        private string continuation;

        public ContinueRouteExtension(string continuation)
        {
            if (string.IsNullOrEmpty(continuation))
                throw new Exception("continuation is null");

            this.continuation = continuation;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            IProvideValueTarget provider = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));
            
            if (!(provider.TargetObject is DependencyObject))
                return this; 
            
            var depObj = (DependencyObject)provider.TargetObject;

            PropertyRoute route = Common.GetTypeContext(depObj);

            if (route == null)
                throw new FormatException("ContinueRoute is only available with a previous TypeContext");

            return Continue(route, continuation);
        }

        static readonly Regex validIdentifier = new Regex(@"^[_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}][_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}\p{Nd}]*$");
        public static PropertyRoute Continue(PropertyRoute route, string continuation)
        {
            string[] steps = continuation.Replace("/", ".Item.").Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            PropertyRoute context = route;

            foreach (var step in steps)
            {
                if (!validIdentifier.IsMatch(step))
                    throw new ApplicationException(Resources.IsNotAValidIdentifier.Formato(step));

                context = context.Add(step);
            }

            return context;
        }
    }
}
