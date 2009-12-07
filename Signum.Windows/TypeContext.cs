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
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using Signum.Entities.Reflection;

namespace Signum.Windows
{
    [TypeConverter(typeof(TypeContextConverter))]
    public class TypeContext
    {
        public readonly Type Type;

        protected TypeContext(Type type)
        {
            this.Type = type;
        }

        public static TypeContext Root(Type type)
        {
            if (!typeof(IdentifiableEntity).IsAssignableFrom(type))
                throw new InvalidOperationException(Resources.RootTypeContextHasToBeAnIdentifiableEntity); 

            return new TypeContext(type); 
        }

 
        public static TypeContext SubContext<T>(Expression<Func<T, object>> lambda)
        {
            TypeContext result = Root(typeof(T));

            foreach (var mi in Reflector.GetMemberList(lambda))
            {
                result = new TypeSubContext((PropertyInfo)mi, result);
            }
            return result; 
        }

        public override string ToString()
        {
            return Type.TypeName();
        }
    }

    public class TypeSubContext : TypeContext
    {
        public readonly TypeContext Parent;

        public readonly PropertyInfo PropertyInfo;

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
                    return TypeContext.Root(typeof(IdentifiableEntity)); //HACK: Improve Design-Time support

                IXamlTypeResolver resolver = (IXamlTypeResolver)context.GetService(typeof(IXamlTypeResolver));

                if (resolver == null)
                    return TypeContext.Root(typeof(IdentifiableEntity)); //HACK: Improve Design-Time support

                if(value ==  null)
                    throw new Exception("value is null");

                return TypeContext.Root(resolver.Resolve((string)value));
            }
            catch (Exception e)
            {
                throw new Exception("Converting to TypeContext: " + e.Message);
            }
        }
    }
}
