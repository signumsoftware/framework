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

namespace Signum.Engine.Maps
{
    public class SchemaSettings
    {
        public SchemaSettings()
        { 

        }

        public PrimaryKeyAttribute DefaultPrimaryKeyAttribute = new PrimaryKeyAttribute(typeof(int), "Id");
        public int DefaultImplementedBySize = 40;

        public Func<Type, string> CanOverrideAttributes = null;

        public int MaxNumberOfParameters = 2000;
        public int MaxNumberOfStatementsInSaveQueries = 16;
        
        public Dictionary<PropertyRoute, Attribute[]> OverridenFieldAttributes = new Dictionary<PropertyRoute, Attribute[]>();
        public Dictionary<Type, Attribute[]> OverridenTypeAttributes = new Dictionary<Type, Attribute[]>();

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
            {SqlDbType.Image, 8000}, 
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

        public bool IsOverriden<T, S>(Expression<Func<T, S>> propertyRoute) where T : Entity
        {
            return IsOverriden(PropertyRoute.Construct(propertyRoute));
        }

        private bool IsOverriden(PropertyRoute propertyRoute)
        {
            return OverridenFieldAttributes.ContainsKey(propertyRoute);
        }

        public bool IsOverridenType<T>() where T : Entity
        {
            return IsOverridenType(typeof(T));
        }

        private bool IsOverridenType(Type type)
        {
            return OverridenTypeAttributes.ContainsKey(type);
        }

        public void OverrideAttributes<T, S>(Expression<Func<T, S>> propertyRoute, params Attribute[] attributes)
            where T : Entity
        {
            OverrideAttributes(PropertyRoute.Construct(propertyRoute), attributes);
        }

        public void OverrideAttributes(PropertyRoute propertyRoute, params Attribute[] attributes)
        {
            string error = CanOverrideAttributes == null ? null : CanOverrideAttributes(propertyRoute.RootType); 

            if (error != null)
                throw new InvalidOperationException(error);

            AssertCorrect(attributes, AttributeTargets.Field);

            OverridenFieldAttributes.Add(propertyRoute, attributes);
        }

        public void OverrideTypeAttributes<T>(params Attribute[] attributes) where T: Entity
        {
            OverrideTypeAttributes(typeof(T), attributes);
        }

        public void OverrideTypeAttributes(Type type, params Attribute[] attributes)
        {
            OverridenTypeAttributes.Add(type, attributes);
        }

        private void AssertCorrect(Attribute[] attributes, AttributeTargets attributeTargets)
        {
            var incorrects = attributes.Where(a => a.GetType().GetCustomAttribute<AttributeUsageAttribute>().Try(au => (au.ValidOn & attributeTargets) == 0) ?? false);

            if (incorrects.Count() > 0)
                throw new InvalidOperationException("The following attributes ar not compatible with targets {0}: {1}".Formato(attributeTargets, incorrects.ToString(a => a.GetType().Name, ", ")));
        }

        public void AssertNotIgnored<T, S>(Expression<Func<T, S>> propertyRoute, string errorContext) where T : Entity
        {
            var pr = PropertyRoute.Construct<T, S>(propertyRoute);

            if (FieldAttribute<IgnoreAttribute>(pr) != null)
                throw new InvalidOperationException("In order to {0} you need to OverrideAttributes for {1} to remove IgnoreAttribute".Formato(errorContext, pr));
        }

        public A FieldAttribute<A>(PropertyRoute propertyRoute) where A : Attribute
        {
            if(propertyRoute.PropertyRouteType == PropertyRouteType.Root || propertyRoute.PropertyRouteType == PropertyRouteType.LiteEntity)
                throw new InvalidOperationException("Route of type {0} not supported for this method".Formato(propertyRoute.PropertyRouteType));

            var overriden = OverridenFieldAttributes.TryGetC(propertyRoute);

            if (overriden != null)
            {
                var res = overriden.OfType<A>().FirstOrDefault();

                if (res != null)
                    return res;
            }
             
            switch (propertyRoute.PropertyRouteType)
	        {
                case PropertyRouteType.FieldOrProperty:
                    if (propertyRoute.FieldInfo == null)
                        return null;
                    return propertyRoute.FieldInfo.GetCustomAttribute<A>(false); 
                case PropertyRouteType.MListItems:
                    if (propertyRoute.Parent.FieldInfo == null)
                        return null;
                    return propertyRoute.Parent.FieldInfo.GetCustomAttribute<A>(false);
                default:
                    throw new InvalidOperationException("Route of type {0} not supported for this method".Formato(propertyRoute.PropertyRouteType));
	        }
        }

        public A TypeAttributes<A>(Type type) where A : Attribute
        {
            if (!typeof(Entity).IsAssignableFrom(type))
                return null;

            var overriden = OverridenTypeAttributes.TryGetC(type);
            if (overriden != null)
            {
                var r = overriden.OfType<A>().FirstOrDefault();
                if (r != null)
                    return r;
            }

            var res = type.GetCustomAttribute<A>(false);
            if (res != null)
                return res;

            return TypeAttributes<A>(type.BaseType);
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
                throw new InvalidOperationException("Route {0} is not ImplementedBy {1}".Formato(propRoute, typeToImplement.Name));
        }

        public Implementations GetImplementations<T>(Expression<Func<T, object>> propertyRoute) where T : Entity
        {
            return GetImplementations(PropertyRoute.Construct(propertyRoute));
        }

        public Implementations GetImplementations(PropertyRoute propertyRoute)
        {
            var cleanType = propertyRoute.Type.CleanType();  
            if (!propertyRoute.Type.CleanType().IsIEntity())
                throw new InvalidOperationException("{0} is not a {1}".Formato(propertyRoute, typeof(IEntity).Name));

            return Implementations.FromAttributes(cleanType, propertyRoute,
                FieldAttribute<ImplementedByAttribute>(propertyRoute),
                FieldAttribute<ImplementedByAllAttribute>(propertyRoute));
        }

        internal SqlDbTypePair GetSqlDbType(SqlDbTypeAttribute att, Type type)
        {
            if (att != null && att.HasSqlDbType)
                return new SqlDbTypePair(att.SqlDbType, att.UdtTypeName);

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
        public string UdtTypeName { get; private set; }

        public SqlDbTypePair() { }

        public SqlDbTypePair(SqlDbType type, string udtTypeName)
        {
            this.SqlDbType = type;
            this.UdtTypeName = udtTypeName;
        }
    }

    internal enum ReferenceFieldType
    {
        Reference,
        ImplementedBy,
        ImplmentedByAll,
    }
}
