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
    public sealed class UniqueIndexAttribute : Attribute
    {
        public bool AllowMultipleNulls { get; set; }
    }

    

    [Serializable]
    public struct Implementations : IEquatable<Implementations>
    {
        object arrayOrType;

        public bool IsByAll { get { return arrayOrType == null; } }
        public IEnumerable<Type> Types
        {
            get
            {
                if (arrayOrType == null)
                    throw new InvalidOperationException("ImplementedByAll");

                return Enumerate();
            }
        }

        private IEnumerable<Type> Enumerate()
        {
            if (arrayOrType is Type)
            {
                yield return (Type)arrayOrType;
            }
            else
            {
                foreach (var item in ((Type[])arrayOrType))
                    yield return item;
            }
        }

        public static Implementations? TryFromAttributes(Type t, Attribute[] fieldAttributes, PropertyRoute route)
        {
            ImplementedByAttribute ib = fieldAttributes.OfType<ImplementedByAttribute>().SingleOrDefaultEx();
            ImplementedByAllAttribute iba = fieldAttributes.OfType<ImplementedByAllAttribute>().SingleOrDefaultEx();

            if (ib != null && iba != null)
                throw new NotSupportedException("Route {0} contains both {1} and {2}".Formato(route, ib.GetType().Name, iba.GetType().Name));

            if (ib != null) return Implementations.By(ib.ImplementedTypes);
            if (iba != null) return Implementations.ByAll;

            if (Error(t) == null)
                return Implementations.By(t);

            return null;
        }


        public static Implementations FromAttributes(Type t, Attribute[] fieldAttributes, PropertyRoute route)
        {
            Implementations? imp = TryFromAttributes(t, fieldAttributes, route);

            if (imp == null)
                throw new InvalidOperationException(Error(t) + ". Set implementations for {0}".Formato(route));

            return imp.Value;
        }

        public static Implementations ByAll { get { return new Implementations(); } }

        public static Implementations By(Type type)
        {
            var error = Error(type);

            if (error.HasText())
                throw new InvalidOperationException(error);

            return new Implementations { arrayOrType = type };
        }

        public static Implementations By(params Type[] types)
        {
            if (types == null || types.Length == 0)
                return new Implementations { arrayOrType = types ?? new Type[0] };

            if (types.Length == 1)
                return By(types[0]);

           var error = types.Select(Error).NotNull().ToString("\r\n");

           if (error.HasText())
               throw new InvalidOperationException(error);

           return new Implementations { arrayOrType = types };
        }

        static string Error(Type type)
        {
            if(!type.IsIdentifiableEntity())
                return  "{0} is not {1}".Formato(type.Name, typeof(IdentifiableEntity).Name);

            if (type.IsAbstract)
                return "{0} is abstract".Formato(type.Name);
            
            return null;
        }

        public override string ToString()
        {
            if (IsByAll)
                return "ImplementedByAll";

            return "ImplementedBy({0})".Formato(Types.ToString(t => t.Name, ", "));
        }

        public override bool Equals(object obj)
        {
            return  obj is Implementations || Equals((Implementations)obj);
        }
        
        public bool Equals(Implementations other)
        {
            return IsByAll && other.IsByAll || 
                arrayOrType == other.arrayOrType ||
                Enumerable.SequenceEqual(Types, other.Types);
        }

        public override int GetHashCode()
        {
            return arrayOrType == null ? 0 : Types.Aggregate(0, (acum, type) => acum ^ type.GetHashCode());
        }
    }

    [Serializable, AttributeUsage(AttributeTargets.Field)]
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

    [Serializable, AttributeUsage(AttributeTargets.Field)]
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
    public sealed class FieldWithoutPropertyAttribute : Attribute
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

        public string UdtTypeName { get; set; }
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

    [AttributeUsage(AttributeTargets.Field)]
    public class ForceForeignKey : Attribute
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
