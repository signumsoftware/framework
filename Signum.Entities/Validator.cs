using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Reflection;
using System.Windows;
using System.Linq.Expressions;

namespace Signum.Entities
{
    public static class Validator
    {
        static Dictionary<Type, Dictionary<string, PropertyPack>> validators = new Dictionary<Type, Dictionary<string, PropertyPack>>();

        public static PropertyPack GetPropertyPack<T, S>(Expression<Func<T, S>> property) where T : ModifiableEntity
        {
            return GetPropertyPacks(typeof(T)).TryGetC(ReflectionTools.GetPropertyInfo(property).Name);
        }

        public static PropertyPack GetPropertyPack(Type type, string property)
        {
            return GetPropertyPacks(type).TryGetC(property);
        }

        public static Dictionary<string, PropertyPack> GetPropertyPacks(Type type)
        {
            lock (validators)
            {
                return validators.GetOrCreate(type, () =>
                    MemberEntryFactory.GenerateIList(type, MemberOptions.Properties | MemberOptions.Getter | MemberOptions.Setters | MemberOptions.Untyped)
                    .Cast<IMemberEntry>()
                    .Where(p => !Attribute.IsDefined(p.MemberInfo, typeof(HiddenPropertyAttribute)))
                    .ToDictionary(p => p.Name, p => new PropertyPack((PropertyInfo)p.MemberInfo, p.UntypedGetter, p.UntypedSetter)));
            }
        }

        public static bool Is<T>(this PropertyInfo pi, Expression<Func<T>> property)
        {
            PropertyInfo pi2 = ReflectionTools.BasePropertyInfo(property);
            return ReflectionTools.MemeberEquals(pi, pi2);
        }

        public static bool Is<S, T>(this PropertyInfo pi, Expression<Func<S, T>> property)
        {
            PropertyInfo pi2 = ReflectionTools.BasePropertyInfo(property);
            return ReflectionTools.MemeberEquals(pi, pi2);
        }
    }

    public class PropertyPack
    {
        internal PropertyPack(PropertyInfo pi, Func<object, object> getValue, Action<object, object> setValue)
        {
            this.PropertyInfo = pi;
            Validators = pi.GetCustomAttributes(typeof(ValidatorAttribute), true).OfType<ValidatorAttribute>().ToList();

            this.GetValue = getValue;
            this.SetValue = setValue;
        }

        public readonly Func<object, object> GetValue;
        public readonly Action<object, object> SetValue;
        public readonly PropertyInfo PropertyInfo;
        public readonly List<ValidatorAttribute> Validators;
        
        public bool DoNotValidate { get; set; }
        public bool SkipPropertyValidation { get; set; }
        public bool SkipExternalPropertyValidation { get; set; }
        public PropertyValidationEventHandler StaticPropertyValidation;
    }

    public delegate string PropertyValidationEventHandler(ModifiableEntity sender, PropertyInfo pi, object propertyValue);
}
