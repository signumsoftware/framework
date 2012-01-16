using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Media;
using Signum.Entities;
using Signum.Entities.Reflection;

namespace Signum.Windows
{
    [MarkupExtensionReturnType(typeof(Type))]
    public class LiteExtension : MarkupExtension
    {
        string typeName;
        public LiteExtension(string typeName)
        {
            this.typeName = typeName;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            IXamlTypeResolver typeResolver = (IXamlTypeResolver)serviceProvider.GetService(typeof(IXamlTypeResolver));

            return Reflector.GenerateLite(typeResolver.Resolve(typeName)); 
        }
    }
}
