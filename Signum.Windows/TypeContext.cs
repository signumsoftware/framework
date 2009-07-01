using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Signum.Windows;
using System.Reflection;
using Signum.Utilities;
using Signum.Entities;
using Signum.Windows.Properties;
using System.Globalization;
using System.Windows.Markup;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Windows
{
    [TypeConverter(typeof(TypeContextConverter))]
    public class TypeContext
    {
        public Type Type{get; internal set;}

        public TypeContext(Type type)
        {
            this.Type = type;
        }

        public override string ToString()
        {
            return Type.TypeName();
        }
    }

    public class TypeSubContext : TypeContext
    {
        public TypeContext Parent { get; internal set; }

        public PropertyInfo PropertyInfo{get; internal set;}

        public TypeSubContext(PropertyInfo propertyInfo, TypeContext parent)
            : base(propertyInfo.PropertyType)
        {
            this.Parent = parent;
            this.PropertyInfo = propertyInfo;
        }

        public override string ToString()
        {
            return PropertyInfo.PropertyName();
        }
    }

    public class TypeContextConverter: TypeConverter
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
                    return new TypeContext(typeof(ModifiableEntity)); //HACK: Improve Design-Time support

                IXamlTypeResolver resolver = (IXamlTypeResolver)context.GetService(typeof(IXamlTypeResolver));

                if (resolver == null)
                    return new TypeContext(typeof(ModifiableEntity)); //HACK: Improve Design-Time support

                if(value ==  null)
                    throw new Exception("value is null"); 

                return new TypeContext(resolver.Resolve((string)value));
            }
            catch (Exception e)
            {
                throw new Exception("Converting to TypeContext: " + e.Message);
            }
        }
    }
}
