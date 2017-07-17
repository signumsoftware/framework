using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.Reflection;

namespace Signum.Entities
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class NotifyCollectionChangedAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class NotifyChildPropertyAttribute : Attribute
    {

    }


    //Used by NotifyCollectionChangedAttribute, NotifyChildPropertyAttribute
    internal static class AttributeManager<T>
        where T : Attribute
    {
        //Consider using ImmutableAVLTree instead
        readonly static Dictionary<Type, TypeAttributePack> fieldAndProperties = new Dictionary<Type, TypeAttributePack>();

        static TypeAttributePack GetFieldsAndProperties(Type type)
        {
            lock (fieldAndProperties)
            {
                return fieldAndProperties.GetOrCreate(type, () =>
                {
                    var list = Reflector.InstanceFieldsInOrder(type).Where(fi => fi.HasAttribute<T>() || (Reflector.TryFindPropertyInfo(fi)?.HasAttribute<T>() ?? false)).ToList();

                    if (list.Count == 0)
                        return null;

                    return new TypeAttributePack
                    {
                        Fields = list.Select(fi => ReflectionTools.CreateGetterUntyped(type, fi)).ToArray(),
                        PropertyNames = list.Select(fi => Reflector.FindPropertyInfo(fi).Name).ToArray()
                    };
                });
            }
        }

        public static bool FieldContainsAttribute(Type type, PropertyInfo pi)
        {
            TypeAttributePack pack = GetFieldsAndProperties(type);

            if (pack == null)
                return false;

            return pack.PropertyNames.Contains(pi.Name);
        }

        readonly static object[] EmptyArray = new object[0];

        public static object[] FieldsWithAttribute(ModifiableEntity entity)
        {
            TypeAttributePack pack = GetFieldsAndProperties(entity.GetType());

            if (pack == null)
                return EmptyArray;

            return pack.Fields.Select(f => f(entity)).ToArray();
        }

        public static string FindPropertyName(ModifiableEntity entity, object fieldValue)
        {
            TypeAttributePack pack = GetFieldsAndProperties(entity.GetType());

            if (pack == null)
                return null;

            int index = pack.Fields.IndexOf(f => f(entity) == fieldValue);

            if (index == -1)
                return null;

            return pack.PropertyNames[index];
        }
    }

    internal class TypeAttributePack
    {
        public Func<object, object>[] Fields;
        public string[] PropertyNames;
    }
}
