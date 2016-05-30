using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Reflection;
using System.Linq.Expressions;
using System.Data;
using Signum.Entities.Reflection;
using Microsoft.SqlServer.Types;
using Microsoft.SqlServer.Server;
using System.Collections.ObjectModel;

namespace Signum.Engine.Maps
{
    public class SchemaSettings
    {
        public SchemaSettings()
        { 

        }

        public PrimaryKeyAttribute DefaultPrimaryKeyAttribute = new PrimaryKeyAttribute(typeof(int), "ID");
        public int DefaultImplementedBySize = 40;

        public Action<Type> AssertNotIncluded = null;

        public int MaxNumberOfParameters = 2000;
        public int MaxNumberOfStatementsInSaveQueries = 16;

        public Dictionary<PropertyRoute, AttributeCollection> FieldAttributesCache = new Dictionary<PropertyRoute, AttributeCollection>();
        public Dictionary<Type, AttributeCollection> TypeAttributesCache = new Dictionary<Type, AttributeCollection>();

        public Dictionary<Type, string> UdtSqlName = new Dictionary<Type, string>()
        {

        };

        public Dictionary<Type, SqlDbType> TypeValues = new Dictionary<Type, SqlDbType>
        {
            {typeof(bool), SqlDbType.Bit},

            {typeof(byte), SqlDbType.TinyInt},
            {typeof(short), SqlDbType.SmallInt},
            {typeof(int), SqlDbType.Int},
            {typeof(long), SqlDbType.BigInt},

            {typeof(float), SqlDbType.Real},
            {typeof(double), SqlDbType.Float},
            {typeof(decimal), SqlDbType.Decimal},

            {typeof(char), SqlDbType.NChar},
            {typeof(string), SqlDbType.NVarChar},
            {typeof(DateTime), SqlDbType.DateTime},

            {typeof(Byte[]), SqlDbType.VarBinary},

            {typeof(Guid), SqlDbType.UniqueIdentifier},
        };

        internal Dictionary<Type, string> desambiguatedNames;

        Dictionary<SqlDbType, int> defaultSize = new Dictionary<SqlDbType, int>()
        {
            {SqlDbType.NVarChar, 200}, 
            {SqlDbType.VarChar, 200}, 
            {SqlDbType.VarBinary, int.MaxValue}, 
            {SqlDbType.Binary, 8000}, 
            {SqlDbType.Char, 1}, 
            {SqlDbType.NChar, 1}, 
            {SqlDbType.Decimal, 18}, 
        };

        Dictionary<SqlDbType, int> defaultScale = new Dictionary<SqlDbType, int>()
        {
            {SqlDbType.Decimal, 2}, 
        };

        public AttributeCollection FieldAttributes<T, S>(Expression<Func<T, S>> propertyRoute)
            where T : Entity
        {
            return FieldAttributes(PropertyRoute.Construct(propertyRoute));
        }

        public AttributeCollection FieldAttributes(PropertyRoute propertyRoute)
        {
            return FieldAttributesCache.GetOrCreate(propertyRoute, () =>
            {
                switch (propertyRoute.PropertyRouteType)
                {
                    case PropertyRouteType.FieldOrProperty:
                        if (propertyRoute.FieldInfo == null)
                            return null;
                        return CreateFieldAttributeCollection(propertyRoute);
                    case PropertyRouteType.MListItems:
                        if (propertyRoute.Parent.FieldInfo == null)
                            return null;
                        return CreateFieldAttributeCollection(propertyRoute.Parent);
                    default:
                        throw new InvalidOperationException("Route of type {0} not supported for this method".FormatWith(propertyRoute.PropertyRouteType));
                }
            });
        }

        AttributeCollection CreateFieldAttributeCollection(PropertyRoute route)
        {
            var fieldAttributes = route.FieldInfo.GetCustomAttributes(false).Cast<Attribute>();
            var fieldAttributesInProperty = route.PropertyInfo == null ? Enumerable.Empty<Attribute>() :
               route.PropertyInfo.GetCustomAttributes(false).Cast<Attribute>().Where(a => AttributeCollection.IsCompatibleWith(a, AttributeTargets.Field));
            return new AttributeCollection(AttributeTargets.Field, fieldAttributes.Concat(fieldAttributesInProperty).ToList(), () => AssertNotIncluded(route.RootType));
        }

        public AttributeCollection TypeAttributes<T>() where T : Entity
        {
            return TypeAttributes(typeof(T));
        }

        public AttributeCollection TypeAttributes(Type entityType)
        {
            if (!typeof(Entity).IsAssignableFrom(entityType) && !typeof(IView).IsAssignableFrom(entityType))
                throw new InvalidOperationException("{0} is not an Entity or View".FormatWith(entityType.Name));

            if (entityType.IsAbstract)
                throw new InvalidOperationException("{0} is abstract".FormatWith(entityType.Name));

            return TypeAttributesCache.GetOrCreate(entityType, () =>
            {
                var list = entityType.GetCustomAttributes(true).Cast<Attribute>().ToList();

                var enumType = EnumEntity.Extract(entityType);

                if (enumType != null)
                    foreach (var at in enumType.GetCustomAttributes(true).Cast<Attribute>().ToList())
                    {
                        list.RemoveAll(a => a.GetType() == at.GetType());
                        list.Add(at);
                    }

                return new AttributeCollection(AttributeTargets.Class, list, () => AssertNotIncluded(entityType));
            });
        }


        public void AssertNotIgnored<T, S>(Expression<Func<T, S>> propertyRoute, string errorContext) where T : Entity
        {
            var pr = PropertyRoute.Construct<T, S>(propertyRoute);

            if (FieldAttribute<IgnoreAttribute>(pr) != null)
                throw new InvalidOperationException("In order to {0} you need to override the attributes for {1} by using SchemaBuilderSettings.FieldAttributes to remove IgnoreAttribute".FormatWith(errorContext, pr));
        }

        public A FieldAttribute<A>(PropertyRoute propertyRoute) where A : Attribute
        {
            if(propertyRoute.PropertyRouteType == PropertyRouteType.Root || propertyRoute.PropertyRouteType == PropertyRouteType.LiteEntity)
                throw new InvalidOperationException("Route of type {0} not supported for this method".FormatWith(propertyRoute.PropertyRouteType));

            return (A)FieldAttributes(propertyRoute).FirstOrDefault(a => a.GetType() == typeof(A));
        }

        public A TypeAttribute<A>(Type entityType) where A : Attribute
        {
            return (A)TypeAttributes(entityType).FirstOrDefault(a => a.GetType() == typeof(A));
        }

        internal bool IsNullable(PropertyRoute propertyRoute, bool forceNull)
        {
            if (forceNull)
                return true;

            if (FieldAttribute<NotNullableAttribute>(propertyRoute) != null)
                return false;

            if (FieldAttribute<NullableAttribute>(propertyRoute) != null)
                return true;

            return !propertyRoute.Type.IsValueType || propertyRoute.Type.IsNullable();
        }

        public bool ImplementedBy<T>(Expression<Func<T, object>> propertyRoute, Type typeToImplement) where T : Entity
        {
            var imp = GetImplementations(propertyRoute);
            return !imp.IsByAll  && imp.Types.Contains(typeToImplement);
        }

        public void AssertImplementedBy<T>(Expression<Func<T, object>> propertyRoute, Type typeToImplement) where T : Entity
        {
            var propRoute = PropertyRoute.Construct(propertyRoute);

            Implementations imp = GetImplementations(propRoute);

            if (imp.IsByAll || !imp.Types.Contains(typeToImplement))
                throw new InvalidOperationException("Route {0} is not ImplementedBy {1}".FormatWith(propRoute, typeToImplement.Name));
        }

        public Implementations GetImplementations<T>(Expression<Func<T, object>> propertyRoute) where T : Entity
        {
            return GetImplementations(PropertyRoute.Construct(propertyRoute));
        }

        public Implementations GetImplementations(PropertyRoute propertyRoute)
        {
            var cleanType = propertyRoute.Type.CleanType();  
            if (!propertyRoute.Type.CleanType().IsIEntity())
                throw new InvalidOperationException("{0} is not a {1}".FormatWith(propertyRoute, typeof(IEntity).Name));

            return Implementations.FromAttributes(cleanType, propertyRoute,
                FieldAttribute<ImplementedByAttribute>(propertyRoute),
                FieldAttribute<ImplementedByAllAttribute>(propertyRoute));
        }

        internal SqlDbTypePair GetSqlDbType(SqlDbTypeAttribute att, Type type)
        {
            if (att != null && att.HasSqlDbType)
                return new SqlDbTypePair(att.SqlDbType, att.UserDefinedTypeName);

            return GetSqlDbTypePair(type.UnNullify());
        }

        internal int? GetSqlSize(SqlDbTypeAttribute att, SqlDbType sqlDbType)
        {
            if (att != null && att.HasSize)
                return att.Size;

            return defaultSize.TryGetS(sqlDbType);
        }

        internal int? GetSqlScale(SqlDbTypeAttribute att, SqlDbType sqlDbType)
        {
            if (att != null && att.HasScale)
                return att.Scale;

            return defaultScale.TryGetS(sqlDbType);
        }

        internal SqlDbType DefaultSqlType(Type type)
        {
            return this.TypeValues.GetOrThrow(type, "Type {0} not registered");
        }

        public void Desambiguate(Type type, string cleanName)
        {
            if (desambiguatedNames != null)
                desambiguatedNames = new Dictionary<Type, string>();

            desambiguatedNames[type] = cleanName;
        }

        public SqlDbTypePair GetSqlDbTypePair(Type type)
        {
            SqlDbType result;
            if (TypeValues.TryGetValue(type, out result))
                return new SqlDbTypePair(result, null);

            string udtTypeName = GetUdtName(type);
            if (udtTypeName != null)
                return new SqlDbTypePair(SqlDbType.Udt, udtTypeName);

            return null;
        }

        public string GetUdtName(Type udtType)
        {
            var att = udtType.GetCustomAttribute<SqlUserDefinedTypeAttribute>();

            if (att == null)
                return null;

            return UdtSqlName[udtType];
        }

        public bool IsDbType(Type type)
        {
            return type.IsEnum || GetSqlDbTypePair(type) != null;
        }

    }

    public class SqlDbTypePair
    {
        public SqlDbType SqlDbType { get; private set; }
        public string UserDefinedTypeName { get; private set; }

        public SqlDbTypePair() { }

        public SqlDbTypePair(SqlDbType type, string udtTypeName)
        {
            this.SqlDbType = type;
            this.UserDefinedTypeName = udtTypeName;
        }
    }

    public class AttributeCollection : Collection<Attribute>
    {
        AttributeTargets Targets;

        Action assertNotIncluded; 

        public AttributeCollection(AttributeTargets targets, IList<Attribute> attributes, Action assertNotIncluded):base(attributes)
        {
            this.Targets = targets; 
            this.assertNotIncluded = assertNotIncluded;
        }

        protected override void InsertItem(int index, Attribute item)
        {
            assertNotIncluded();

            if (!IsCompatibleWith(item, Targets))
                throw new InvalidOperationException("The attribute {0} is not compatible with targets {1}".FormatWith(item, Targets));

            base.InsertItem(index, item);
        }

        public static bool IsCompatibleWith(Attribute a, AttributeTargets targets)
        {
            var au = a.GetType().GetCustomAttribute<AttributeUsageAttribute>();

            return au != null && (au.ValidOn & targets) == targets;
        }

        public new AttributeCollection Add(Attribute attr)
        {
            base.Add(attr);

            return this;
        }

        public AttributeCollection Replace(Attribute attr)
        {
            if (attr is ImplementedByAttribute || attr is ImplementedByAllAttribute)
                this.RemoveAll(a => a is ImplementedByAttribute || a is ImplementedByAllAttribute);
            else
                this.RemoveAll(a => a.GetType() == attr.GetType());

            this.Add(attr);

            return this;
        }

        public AttributeCollection Remove<A>() where A : Attribute
        {
            this.RemoveAll(a=>a is A);

            return this;
        }

        protected override void ClearItems()
        {
            assertNotIncluded();

            base.ClearItems();
        }

        protected override void SetItem(int index, Attribute item)
        {
            assertNotIncluded();

            base.SetItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            assertNotIncluded();

            base.RemoveItem(index);
        }
    }

    internal enum ReferenceFieldType
    {
        Reference,
        ImplementedBy,
        ImplmentedByAll,
    }
}
