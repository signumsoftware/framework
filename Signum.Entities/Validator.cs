using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Reflection;

namespace Signum.Entities
{
    public static class Validator
    {
        static Dictionary<Type, Dictionary<string, PropertyPack>> validators = new Dictionary<Type, Dictionary<string, PropertyPack>>();

        public static IsApplicableOf IsApplicable<V>() where V : ValidatorAttribute
        {
            return new IsApplicableOf { validatorType = typeof(V) };
        }

        public static PropertyPack GetOrCreatePropertyPack<T, S>(Expression<Func<T, S>> property) where T : ModifiableEntity
        {
            return GetOrCreatePropertyPack(typeof(T), ReflectionTools.GetPropertyInfo(property).Name);
        }

        public static PropertyPack GetOrCreatePropertyPack(PropertyRoute route)
        {
            if (route.PropertyRouteType != PropertyRouteType.FieldOrProperty)
                throw new InvalidOperationException("PropertyRoute of type Property expected");

            return GetOrCreatePropertyPack(route.Parent.Type, route.PropertyInfo.Name);
        }

        public static PropertyPack GetOrCreatePropertyPack(Type type, string property)
        {
            return GetPropertyPacks(type).TryGetC(property);
        }

        public static Dictionary<string, PropertyPack> GetPropertyPacks(Type type)
        {
            lock (validators)
            {
                return validators.GetOrCreate(type, () => MemberEntryFactory.GenerateIList(type, MemberOptions.Properties | MemberOptions.Getter | MemberOptions.Setters | MemberOptions.Untyped)
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

        public class IsApplicableOf
        {
            internal Type validatorType;

            public IsApplicableWhen<T> Of<T, S>(Expression<Func<T, S>> property)
            where T : ModifiableEntity
            {
                var pp = GetOrCreatePropertyPack(property);

                var val = pp.Validators.SingleOrDefaultEx(v => v.GetType() == validatorType);

                if (val == null)
                    throw new InvalidOperationException("No '{0}' found on '{1}'".Formato(validatorType.NiceName(), pp.PropertyInfo.PropertyName()));

                return new IsApplicableWhen<T> { validator = val };
            }
        }

        public class IsApplicableWhen<T> where T : ModifiableEntity
        {
            internal ValidatorAttribute validator;

            public void When(Func<T, bool> isApplicable)
            {
                if (isApplicable == null)
                    validator.IsApplicable = null;
                else
                    validator.IsApplicable = m => isApplicable((T)m);
            }
        }
    }

    public class PropertyPack
    {
        internal PropertyPack(PropertyInfo pi, Func<object, object> getValue, Action<object, object> setValue)
        {
            this.PropertyInfo = pi;
            Validators = pi.GetCustomAttributes(typeof(ValidatorAttribute), true)
                .OfType<ValidatorAttribute>()
                .OrderBy(va => va.Order).ThenBy(va => va.GetType().Name).ToList();

            this.GetValue = getValue;
            this.SetValue = setValue;
        }

        public readonly Func<object, object> GetValue;
        public readonly Action<object, object> SetValue;
        public readonly PropertyInfo PropertyInfo;
        public readonly List<ValidatorAttribute> Validators;

        public Func<ModifiableEntity, bool> IsAplicable { get; set; }
        public Func<ModifiableEntity, bool> IsAplicablePropertyValidation { get; set; }
        public Func<ModifiableEntity, bool> IsAplicableExternalPropertyValidation { get; set; }

        public PropertyValidationEventHandler StaticPropertyValidation { get; set; }
        
        public void ReplaceValidators(params ValidatorAttribute[] validators)
        {
            Validators.Clear();
            Validators.AddRange(validators); 
        }
    }

    public delegate string PropertyValidationEventHandler(ModifiableEntity sender, PropertyInfo pi);
}
