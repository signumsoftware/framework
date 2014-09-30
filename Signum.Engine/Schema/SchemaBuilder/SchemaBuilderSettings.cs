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
    public enum DBMS
    {
        SqlCompact,
        SqlServer2005,
        SqlServer2008,
        SqlServer2012,
    }

    public class SchemaSettings
    {
        public SchemaSettings()
        { 

        }

        public SchemaSettings(DBMS dbms)
        {
            DBMS = dbms;
            if (dbms >= Maps.DBMS.SqlServer2008)
            {
                TypeValues.Add(typeof(TimeSpan), SqlDbType.Time);

                UdtSqlName.Add(typeof(SqlHierarchyId), "HierarchyId");
                UdtSqlName.Add(typeof(SqlGeography), "Geography");
                UdtSqlName.Add(typeof(SqlGeometry), "Geometry");
            }
        }


        public Func<Type, string> CanOverrideAttributes = null;

        public int MaxNumberOfParameters = 2000;
        public int MaxNumberOfStatementsInSaveQueries = 16; 

        public DBMS DBMS { get; private set; }

        public Dictionary<PropertyRoute, Attribute[]> OverridenAttributes = new Dictionary<PropertyRoute, Attribute[]>();

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

        public bool IsOverriden<T, S>(Expression<Func<T, S>> propertyRoute) where T : IdentifiableEntity
        {
            return IsOverriden(PropertyRoute.Construct(propertyRoute));
        }

        private bool IsOverriden(PropertyRoute propertyRoute)
        {
            return OverridenAttributes.ContainsKey(propertyRoute);
        }

        public void OverrideAttributes<T, S>(Expression<Func<T, S>> propertyRoute, params Attribute[] attributes)
            where T : IdentifiableEntity
        {
            OverrideAttributes(PropertyRoute.Construct(propertyRoute), attributes);
        }

        public void OverrideAttributes(PropertyRoute propertyRoute, params Attribute[] attributes)
        {
            string error = CanOverrideAttributes == null ? null : CanOverrideAttributes(propertyRoute.RootType); 

            if (error != null)
                throw new InvalidOperationException(error);

            AssertCorrect(attributes, AttributeTargets.Field);

            OverridenAttributes.Add(propertyRoute, attributes);
        }

        private void AssertCorrect(Attribute[] attributes, AttributeTargets attributeTargets)
        {
            var incorrects = attributes.Where(a => a.GetType().SingleAttribute<AttributeUsageAttribute>().Try(au => (au.ValidOn & attributeTargets) == 0) ?? false);

            if (incorrects.Count() > 0)
                throw new InvalidOperationException("The following attributes ar not compatible with targets {0}: {1}".Formato(attributeTargets, incorrects.ToString(a => a.GetType().Name, ", ")));
        }

        public void AssertNotIgnored<T, S>(Expression<Func<T, S>> propertyRoute, string errorContext) where T : IdentifiableEntity
        {
            var pr = PropertyRoute.Construct<T, S>(propertyRoute);

            if (FieldAttributes(pr).OfType<IgnoreAttribute>().Any())
                throw new InvalidOperationException("In order to {0} you need to OverrideAttributes for {1} to remove IgnoreAttribute".Formato(errorContext, pr));
        }

        public Attribute[] FieldAttributes<T, S>(Expression<Func<T, S>> propertyRoute) where T : IdentifiableEntity
        {
            return FieldAttributes(PropertyRoute.Construct<T, S>(propertyRoute));
        }

        public Attribute[] FieldAttributes(PropertyRoute propertyRoute)
        {
            if(propertyRoute.PropertyRouteType == PropertyRouteType.Root || propertyRoute.PropertyRouteType == PropertyRouteType.LiteEntity)
                throw new InvalidOperationException("Route of type {0} not supported for this method".Formato(propertyRoute.PropertyRouteType));

            var overriden = OverridenAttributes.TryGetC(propertyRoute); 

            if(overriden!= null)
                return overriden; 

            switch (propertyRoute.PropertyRouteType)
	        {
                case PropertyRouteType.FieldOrProperty:
                    if (propertyRoute.FieldInfo == null)
                        return new Attribute[0];
                    return propertyRoute.FieldInfo.GetCustomAttributes(false).Cast<Attribute>().ToArray(); 
                case PropertyRouteType.MListItems:
                    if (propertyRoute.Parent.FieldInfo == null)
                        return new Attribute[0];
                    return propertyRoute.Parent.FieldInfo.GetCustomAttributes(false).Cast<Attribute>().ToArray();
                default:
                    throw new InvalidOperationException("Route of type {0} not supported for this method".Formato(propertyRoute.PropertyRouteType));
	        }
        }

        internal bool IsNullable(PropertyRoute propertyRoute, bool forceNull)
        {
            if (forceNull)
                return true;

            var attrs = FieldAttributes(propertyRoute);

            if (attrs.OfType<NotNullableAttribute>().Any())
                return false;

            if (attrs.OfType<NullableAttribute>().Any())
                return true;

            return !propertyRoute.Type.IsValueType || propertyRoute.Type.IsNullable();
        }

        internal IndexType GetIndexType(PropertyRoute propertyRoute)
        {
            UniqueIndexAttribute at = FieldAttributes(propertyRoute).OfType<UniqueIndexAttribute>().SingleOrDefaultEx();

            return at == null ? IndexType.None :
                at.AllowMultipleNulls ? IndexType.UniqueMultipleNulls :
                IndexType.Unique;
        }

        public bool ImplementedBy<T>(Expression<Func<T, object>> propertyRoute, Type typeToImplement) where T : IdentifiableEntity
        {
            var imp = GetImplementations(propertyRoute);
            return !imp.IsByAll  && imp.Types.Contains(typeToImplement);
        }

        public void AssertImplementedBy<T>(Expression<Func<T, object>> propertyRoute, Type typeToImplement) where T : IdentifiableEntity
        {
            var propRoute = PropertyRoute.Construct(propertyRoute);

            Implementations imp = GetImplementations(propRoute);

            if (imp.IsByAll || !imp.Types.Contains(typeToImplement))
                throw new InvalidOperationException("Route {0} is not ImplementedBy {1}".Formato(propRoute, typeToImplement.Name));
        }

        public Implementations GetImplementations<T>(Expression<Func<T, object>> propertyRoute) where T : IdentifiableEntity
        {
            return GetImplementations(PropertyRoute.Construct(propertyRoute));
        }

        public Implementations GetImplementations(PropertyRoute propertyRoute)
        {
            var cleanType = propertyRoute.Type.CleanType();  
            if (!propertyRoute.Type.CleanType().IsIIdentifiable())
                throw new InvalidOperationException("{0} is not a {1}".Formato(propertyRoute, typeof(IIdentifiable).Name));

            var fieldAtt = FieldAttributes(propertyRoute);

            return Implementations.FromAttributes(cleanType, fieldAtt, propertyRoute);
        }

        internal SqlDbTypePair GetSqlDbType(PropertyRoute propertyRoute)
        {
            SqlDbTypeAttribute att = FieldAttributes(propertyRoute).OfType<SqlDbTypeAttribute>().SingleOrDefaultEx();

            if (att != null && att.HasSqlDbType)
                return new SqlDbTypePair(att.SqlDbType, att.UdtTypeName);

            return GetSqlDbTypePair(propertyRoute.Type.UnNullify());
        }

        internal int? GetSqlSize(PropertyRoute propertyRoute, SqlDbType sqlDbType)
        {
            SqlDbTypeAttribute att = FieldAttributes(propertyRoute).OfType<SqlDbTypeAttribute>().SingleOrDefaultEx();

            if (att != null && att.HasSize)
                return att.Size;

            return defaultSize.TryGetS(sqlDbType);
        }

        internal int? GetSqlScale(PropertyRoute propertyRoute, SqlDbType sqlDbType)
        {
            SqlDbTypeAttribute att = FieldAttributes(propertyRoute).OfType<SqlDbTypeAttribute>().SingleOrDefaultEx();

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

        internal void FixType(ref SqlDbType type, ref int? size, ref int? scale)
        {
            if (DBMS == Maps.DBMS.SqlCompact && (type == SqlDbType.NVarChar || type == SqlDbType.VarChar) && size > 4000)
            {
                type = SqlDbType.NText;
                size = null;
            }
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
            var att = udtType.SingleAttribute<SqlUserDefinedTypeAttribute>();

            if (att == null)
                return null;

            return UdtSqlName[udtType];
        }

        public bool IsDbType(Type type)
        {
            return type.IsEnum || GetSqlDbTypePair(type) != null;
        }

        internal Type GetType(SqlDbType sqlDbType)
        {
            switch (sqlDbType)
            {
                case SqlDbType.BigInt: return typeof(long);
                case SqlDbType.Binary: return typeof(byte[]);
                case SqlDbType.Bit: return typeof(bool);
                case SqlDbType.Char: return typeof(char);
                case SqlDbType.Date: return typeof(DateTime);
                case SqlDbType.DateTime: return typeof(DateTime);
                case SqlDbType.DateTime2: return typeof(DateTime);
                case SqlDbType.DateTimeOffset: return typeof(DateTimeOffset);
                case SqlDbType.Decimal: return typeof(decimal);
                case SqlDbType.Float: return typeof(double);
                case SqlDbType.Image: return typeof(byte[]);
                case SqlDbType.Int: return typeof(int);
                case SqlDbType.Money: return typeof(decimal);
                case SqlDbType.NChar: return typeof(string);
                case SqlDbType.NText: return typeof(string);
                case SqlDbType.NVarChar: return typeof(string);
                case SqlDbType.Real: return typeof(float);
                case SqlDbType.SmallDateTime: return typeof(DateTime);
                case SqlDbType.SmallInt: return typeof(short);
                case SqlDbType.SmallMoney: return typeof(decimal);               
                case SqlDbType.Text: return typeof(string);
                case SqlDbType.Time: return typeof(TimeSpan);
                case SqlDbType.Timestamp: return typeof(TimeSpan);
                case SqlDbType.TinyInt: return typeof(int);               
                case SqlDbType.UniqueIdentifier: return typeof(Guid);
                case SqlDbType.VarBinary: return typeof(byte[]);
                case SqlDbType.VarChar: return typeof(string);
                case SqlDbType.Xml: return typeof(string);
                case SqlDbType.Variant:
                case SqlDbType.Structured:
                case SqlDbType.Udt: 
                default: throw new InvalidOperationException();
            }
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
