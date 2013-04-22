using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Signum.Entities;
using System.Windows.Markup;
using System.Globalization;
using System.Text.RegularExpressions;
using Signum.Utilities;
using System.Windows;
using Signum.Entities.Reflection;

namespace Signum.Windows
{
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

            PropertyRoute route = Common.GetPropertyRoute(depObj);

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
