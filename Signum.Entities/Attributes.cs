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

namespace Signum.Entities
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ReloadEntityOnChange : Attribute
    { 
    
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class Reactive : Attribute
    {

    }

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

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class ImplementedByAttribute : Attribute
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
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class ImplementedByAllAttribute : Attribute
    {
        public ImplementedByAllAttribute()
        {
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
        static Dictionary<Type, Tuple<List<Func<object, object>>, HashSet<string>>> fieldAndProperties = new Dictionary<Type, Tuple<List<Func<object, object>>, HashSet<string>>>();
        static Tuple<List<Func<object, object>>, HashSet<string>> GetFieldsAndProperties(Type type)
        {
            lock (fieldAndProperties)
            {
                return fieldAndProperties.GetOrCreate(type, () =>
                {
                    var list = (from fi in Reflector.InstanceFieldsInOrder(type)
                                where fi.HasAttribute<NotifyCollectionChangedAttribute>() &&
                                      typeof(INotifyCollectionChanged).IsAssignableFrom(fi.FieldType)
                                select fi).ToList();

                    return Tuple.New(
                        list.Select(fi => ReflectionTools.CreateGetterUntyped(type, fi)).ToList(),
                        list.Select(fi => Reflector.FindPropertyInfo(fi).Name).ToHashSet());
                });
            }
        }

        public static bool HasToNotify(Type type, string propertyName)
        {
            return GetFieldsAndProperties(type).Second.Contains(propertyName);
        }

        public static List<Func<object, object>> FieldsToNotify(Type type)
        {
            return GetFieldsAndProperties(type).First;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NotifyPropertyChangedAttribute : Attribute
    {
        static Dictionary<Type, Tuple<List<Func<object, object>>, HashSet<string>>> fieldAndProperties = new Dictionary<Type, Tuple<List<Func<object, object>>, HashSet<string>>>();
        static Tuple<List<Func<object, object>>, HashSet<string>> GetFieldsAndProperties(Type type)
        {
            lock (fieldAndProperties)
            {
                return fieldAndProperties.GetOrCreate(type, () =>
                {
                    var list = (from fi in Reflector.InstanceFieldsInOrder(type)
                                where fi.HasAttribute<NotifyPropertyChangedAttribute>() &&
                                      typeof(INotifyPropertyChanged).IsAssignableFrom(fi.FieldType)
                                select fi).ToList();

                    return Tuple.New(
                        list.Select(fi => ReflectionTools.CreateGetterUntyped(type, fi)).ToList(),
                        list.Select(fi => Reflector.FindPropertyInfo(fi).Name).ToHashSet());
                });
            }
        }

        public static bool HasToNotify(Type type, string propertyName)
        {
            return GetFieldsAndProperties(type).Second.Contains(propertyName);
        }

        public static List<Func<object, object>> FieldsToNotify(Type type)
        {
            return GetFieldsAndProperties(type).First;
        }
    }
}
