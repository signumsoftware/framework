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
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Basics;

namespace Signum.Entities
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class UniqueIndexAttribute : Attribute
    {
        public bool AllowMultipleNulls { get; set; }

        public bool AvoidAttachToUniqueIndexes { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AttachToUniqueIndexesAttribute : Attribute
    {
    }

    [Serializable]
    public struct Implementations : IEquatable<Implementations>, ISerializable
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
            if (arrayOrType is Type t)
            {
                yield return t;
            }
            else
            {
                foreach (var item in ((Type[])arrayOrType))
                    yield return item;
            }
        }

        public static Implementations? TryFromAttributes(Type t, PropertyRoute route, ImplementedByAttribute ib, ImplementedByAllAttribute iba)
        {
            if (ib != null && iba != null)
                throw new NotSupportedException("Route {0} contains both {1} and {2}".FormatWith(route, ib.GetType().Name, iba.GetType().Name));

            if (ib != null) return Implementations.By(ib.ImplementedTypes);
            if (iba != null) return Implementations.ByAll;

            if (Error(t) == null)
                return Implementations.By(t);

            return null;
        }


        public static Implementations FromAttributes(Type t, PropertyRoute route, ImplementedByAttribute ib, ImplementedByAllAttribute iba)
        {
            Implementations? imp = TryFromAttributes(t, route, ib, iba);

            if (imp == null)
            {
                var message = Error(t) + @". Set implementations for {0}.".FormatWith(route);

                if(t.IsInterface || t.IsAbstract)
                {
                    message += @"\r\n" + ConsiderMessage(route, "typeof(YourConcrete" + t.TypeName() + ")");
                }

                throw new InvalidOperationException(message);
            }

            return imp.Value;
        }

        internal static string ConsiderMessage(PropertyRoute route, string targetTypes)
        {
            return $@"Consider writing something like this in your Starter class: 
sb.Schema.Settings.FieldAttributes(({route.RootType.TypeName()} a) => a.{route.PropertyString().Replace("/", ".First().")}).Replace(new ImplementedByAttribute({targetTypes}))";
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
            if (type.IsInterface)
                return "{0} is an interface".FormatWith(type.Name);

            if (type.IsAbstract)
                return "{0} is abstract".FormatWith(type.Name);

            if (!type.IsEntity())
                return "{0} is not {1}".FormatWith(type.Name, typeof(Entity).Name);

            return null;
        }

        public string Key()
        {
            if (IsByAll)
                return "[ALL]";

            return Types.ToString(TypeEntity.GetCleanName, ", ");
        }


        public override string ToString()
        {
            if (IsByAll)
                return "ImplementedByAll";

            return "ImplementedBy({0})".FormatWith(Types.ToString(t => t.Name, ", "));
        }

        public override bool Equals(object obj)
        {
            return obj is Implementations || Equals((Implementations)obj);
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

        Implementations(SerializationInfo info, StreamingContext context)
        {
            string str = info.GetString("arrayOrType");

            arrayOrType = str == "ALL" ? null :
                str.Split('|').Select(Type.GetType).ToArray();

            if (arrayOrType is Type[] array && array.Length == 1)
                arrayOrType = array[0];
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("arrayOrType", arrayOrType == null ? "ALL" :
                arrayOrType is Type ? ((Type)arrayOrType).AssemblyQualifiedName :
                arrayOrType is Type[] ? ((Type[])arrayOrType).ToString(a => a.AssemblyQualifiedName, "|") : null);
        }
    }

    [Serializable, AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
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

    [Serializable, AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ImplementedByAllAttribute : Attribute
    {
        public ImplementedByAllAttribute()
        {
        }
    }


    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class IgnoreAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class FieldWithoutPropertyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class NotNullableAttribute : Attribute
    {
    }


    /// <summary>
    /// Very rare. Reference types (classes) or Nullable are already nullable in the database.
    /// This attribute is only necessary in the case an entity field is not-nullable but you can not make the DB column nullable because of legacy data, or cycles in a graph of entities.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class NullableAttribute : Attribute
    {
    }


    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SqlDbTypeAttribute : Attribute
    {
        SqlDbType? sqlDbType;
        int? size;
        int? scale;

        public SqlDbType SqlDbType
        {
            get { return sqlDbType.Value; }
            set { sqlDbType = value; }
        }

        public bool HasSqlDbType
        {
            get { return sqlDbType.HasValue; }
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
        
        public string UserDefinedTypeName { get; set; }

        public string Default { get; set; }

        public string Collation { get; set; }

        public const string NewId = "NEWID()";
        public const string NewSequentialId = "NEWSEQUENTIALID()";
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property /*MList fields*/, Inherited = true, AllowMultiple = false)]
    public sealed class PrimaryKeyAttribute : SqlDbTypeAttribute
    {
        public Type Type { get; set; }

        public string Name { get; set; }

        public bool Identity { get; set; }

        bool identityBehaviour;
        public bool IdentityBehaviour
        {
            get { return identityBehaviour; }
            set
            {
                identityBehaviour = value;
                if (Type == typeof(Guid))
                {
                    this.Default = identityBehaviour ? NewSequentialId : null;
                }
            }
        }

        public PrimaryKeyAttribute(Type type, string name = "ID")
        {
            this.Type = type;
            this.Name = name;
            this.Identity = type == typeof(Guid) ? false : true;
            this.IdentityBehaviour = true;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ColumnNameAttribute : Attribute
    {
        public string Name { get; set; }

        public ColumnNameAttribute(string name)
        {
            this.Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class BackReferenceColumnNameAttribute : Attribute
    {
        public string Name { get; set; }

        public BackReferenceColumnNameAttribute(string name)
        {
            this.Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ViewPrimaryKeyAttribute : Attribute
    { 
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property /*MList fields*/, Inherited = true, AllowMultiple = false)]
    public sealed class TableNameAttribute : Attribute
    {
        public string Name { get; set; }
        public string SchemaName { get; set; }
        public string DatabaseName { get; set; }
        public string ServerName { get; set; }

        public TableNameAttribute(string fullName)
        {
            var parts = fullName.Split('.');
            this.Name = parts.ElementAtOrDefault(parts.Length - 1).Trim('[', ']');
            this.SchemaName = parts.ElementAtOrDefault(parts.Length - 2)?.Trim('[', ']');
            this.DatabaseName = parts.ElementAtOrDefault(parts.Length - 3)?.Trim('[', ']');
            this.ServerName = parts.ElementAtOrDefault(parts.Length - 4)?.Trim('[', ']');
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class TicksColumnAttribute : SqlDbTypeAttribute
    {
        public bool HasTicks { get; private set; }

        public string Name { get; set; }

        public Type Type { get; set; }

        public TicksColumnAttribute(bool hasTicks = true)
        {
            this.HasTicks = hasTicks;
        }
    }

    /// <summary>
    /// Activates SQL Server 2016 Temporal Tables
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property /*MList fields*/, Inherited = true, AllowMultiple = false)]
    public sealed class SystemVersionedAttribute : Attribute
    {
        public string TemporalTableName { get; set; }
        public string StartDateColumnName { get; set; } = "SysStartDate";
        public string EndDateColumnName { get; set; } = "SysEndDate";
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AvoidForeignKeyAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AvoidExpandQueryAttribute : Attribute
    {

    }


    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class CombineStrategyAttribute : Attribute
    {
        public readonly CombineStrategy Strategy;

        public CombineStrategyAttribute(CombineStrategy strategy)
        {
            this.Strategy = strategy;
        }
    }

    public enum CombineStrategy
    {
        Union,
        Case,
    }

    public static class LinqHintEntities
    {
        public static T CombineCase<T>(this T value) where T : IEntity
        {
            return value;
        }

        public static T CombineUnion<T>(this T value) where T : IEntity
        {
            return value;
        }
    }
}
