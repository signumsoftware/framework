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
using Signum.Entities.Reflection;

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
        [ConstructorArgument("continuation")]
        private string Continuation { get; set; }

        public ContinueRouteExtension() { }

        public ContinueRouteExtension(string continuation)
        {
            this.Continuation = continuation;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Continuation))
                throw new Exception("continuation is null");

            IProvideValueTarget provider = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));
            
            if (!(provider.TargetObject is DependencyObject))
                return this; 
            
            var depObj = (DependencyObject)provider.TargetObject;

            PropertyRoute route = Common.GetTypeContext(depObj);

            if (route == null)
                throw new FormatException("ContinueRoute is only available with a previous TypeContext");

            return Continue(route, Continuation);
        }

        public static PropertyRoute Continue(PropertyRoute route, string continuation)
        {
            string[] steps = continuation.Replace("/", ".Item.").Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            PropertyRoute context = route;

            foreach (var step in steps)
            {
                Reflector.AssertValidIdentifier(step);

                context = context.Add(step);
            }

            return context;
        }
    }
}
