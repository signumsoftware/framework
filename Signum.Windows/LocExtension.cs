using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Markup;
using System.Resources;
using System.Reflection;
using System.Windows;
using System.ComponentModel;
using Signum.Utilities;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Signum.Entities;

namespace Signum.Windows
{
    [MarkupExtensionReturnType(typeof(object))]
    public class LocExtension : MarkupExtension
    {
        [ConstructorArgument("key")]
        public Enum Key { get; set; }

        public LocExtension() { }
   
        public LocExtension(Enum key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Key == null)
                return "[null]";

            return Key.NiceToString();
        }
    }


    [MarkupExtensionReturnType(typeof(object))]
    public class LocSymbolExtension : MarkupExtension
    {
        [ConstructorArgument("key")]
        public Symbol Key { get; set; }

        public LocSymbolExtension() { }

        public LocSymbolExtension(Symbol key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Key == null)
                return "[null]";

            return Key.NiceToString();
        }
    }

    [MarkupExtensionReturnType(typeof(object))]
    public class LocTypeExtension : MarkupExtension
    {
        [ConstructorArgument("type")]
        public Type Type { get; set; }

        public LocTypeExtension() { }
        public LocTypeExtension(Type type)
        {
            Type = type;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Type == null)
                return "[null]";

            return Type.NiceName();
        }
    }

    [MarkupExtensionReturnType(typeof(object))]
    public class LocTypePluralExtension : MarkupExtension
    {
        [ConstructorArgument("type")]
        public Type Type { get; set; }

        public LocTypePluralExtension() { }
        public LocTypePluralExtension(Type type)
        {
            Type = type;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Type == null)
                return "[null]";

            return Type.NicePluralName();
        }
    }
}