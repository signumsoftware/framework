using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Data;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using Signum.Utilities.DataStructures;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Reflection;
using Signum.Entities.Reflection;
using System.ComponentModel;
using System.Collections;

namespace Signum.Entities
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NoIndexAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class UniqueIndexAttribute : Attribute
    {
        public bool AllowMultipleNulls { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MultipleIndexAttribute : Attribute
    {
    }

    public enum Index
    {
        None = 0,
        Unique,
        Multiple,
        UniqueMultiNulls
    }

    [Serializable]
    public abstract class Implementations : Attribute
    {
        public abstract bool IsByAll { get; }
    }

    [Serializable, AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class ImplementedByAttribute : Implementations
    {
        Type[] implementedTypes;

        public Type[] ImplementedTypes
        {
            get { return implementedTypes; }
        }

        public ImplementedByAttribute(params Type[] types)
        {
            implementedTypes = types;
        }

        public override bool IsByAll
        {
            get { return false; }
        }
    }

    [Serializable, AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class ImplementedByAllAttribute : Implementations
    {
        public ImplementedByAllAttribute()
        {
        }

        public override bool IsByAll
        {
            get { return true; }
        }
    }


    [AttributeUsage(AttributeTargets.Field)]
    public sealed class IgnoreAttribute : Attribute
    {
    }


    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NotNullableAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NullableAttribute : Attribute
    {
    }


    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SqlDbTypeAttribute : Attribute
    {
        SqlDbType? type;
        int? size;
        int? scale;

        public SqlDbType SqlDbType
        {
            get { return type.Value; }
            set { type = value; }
        }

        public bool HasSqlDbType
        {
            get { return type.HasValue; }
        }

        public int Size
        {
            get { return size.Value; }
            set { size = value; }
        }

        public bool HasSize
        {
            get { return size.HasValue; }
        }

        public int Scale
        {
            get { return scale.Value; }
            set { scale = value; }
        }

        public bool HasScale
        {
            get { return scale.HasValue; }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class LowPopulationAttribute : Attribute
    {
        bool low = true;

        public bool Low
        {
            get { return low; }
            set { low = value; }
        }

        public LowPopulationAttribute(bool low)
        {
            this.Low = low;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NotifyCollectionChangedAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NotifyChildPropertyAttribute : Attribute
    {
        
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ValidateChildPropertyAttribute : Attribute
    {

    }


    //Used by NotifyCollectionChangedAttribute, NotifyChildPropertyAttribute, ValidateChildPropertyAttribute
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
                    var list = Reflector.InstanceFieldsInOrder(type).Where(fi=>fi.HasAttribute<T>()).ToList();

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

            if(pack == null)
                return false;

            return pack.PropertyNames.Contains(pi.Name);
        }

        readonly static object[] EmptyArray = new object[0];

        public static object[] FieldsWithAttribute(ModifiableEntity entity)
        {
            TypeAttributePack pack = GetFieldsAndProperties(entity.GetType());

            if (pack == null)
                return EmptyArray;

            return pack.Fields.Select(f=>f(entity)).ToArray();
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
