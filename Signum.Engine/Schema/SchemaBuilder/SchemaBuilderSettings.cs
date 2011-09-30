﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Engine.Properties;
using System.Data;
using Signum.Entities.Reflection;

namespace Signum.Engine.Maps
{
    public class SchemaSettings
    {
        public Dictionary<PropertyRoute, Attribute[]> OverridenAttributes = new Dictionary<PropertyRoute, Attribute[]>();

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

            {typeof(Guid), SqlDbType.UniqueIdentifier}
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

        public bool IsOverriden<T>(Expression<Func<T, object>> route) where T : IdentifiableEntity
        {
            return IsOverriden(PropertyRoute.Construct(route));
        }

        private bool IsOverriden(PropertyRoute route)
        {
            return OverridenAttributes.ContainsKey(route);
        }

        public void OverrideAttributes<T>(Expression<Func<T, object>> route, params Attribute[] attributes)
            where T : IdentifiableEntity
        {
            OverrideAttributes(PropertyRoute.Construct(route), attributes);
        }

        public void OverrideAttributes(PropertyRoute route, params Attribute[] attributes)
        {
            AssertCorrect(attributes, AttributeTargets.Field);

            OverridenAttributes.Add(route, attributes);
        }

        private void AssertCorrect(Attribute[] attributes, AttributeTargets attributeTargets)
        {
            var incorrects = attributes.Where(a => a.GetType().SingleAttribute<AttributeUsageAttribute>().TryCS(au => (au.ValidOn & attributeTargets) == 0) ?? false);

            if (incorrects.Count() > 0)
                throw new InvalidOperationException("The following attributes ar not compatible with targets {0}: {1}".Formato(attributeTargets, incorrects.ToString(a => a.GetType().Name, ", ")));
        }

        public Attribute[] Attributes<T>(Expression<Func<T, object>> route)
        {
            return Attributes(route);
        }

        public Attribute[] Attributes(PropertyRoute route)
        {
            var overriden = OverridenAttributes.TryGetC(route) ; 

            if(overriden!= null)
                return overriden; 

            switch (route.PropertyRouteType)
	        {
                case PropertyRouteType.FieldOrProperty: 
                    return route.FieldInfo.GetCustomAttributes(false).Cast<Attribute>().ToArray(); 
                case PropertyRouteType.MListItems: 
                    return route.Parent.FieldInfo.GetCustomAttributes(false).Cast<Attribute>().ToArray();

                default:
                    throw new InvalidOperationException("Route of type {0} not supported for this method".Formato(route.PropertyRouteType));
	        }
        }

        internal bool IsNullable(PropertyRoute route, bool forceNull)
        {
            if (forceNull)
                return true;

            var attrs = Attributes(route);

            if (attrs.OfType<NotNullableAttribute>().Any())
                return false;

            if (attrs.OfType<NullableAttribute>().Any())
                return true;

            return !route.Type.IsValueType || route.Type.IsNullable();
        }

        internal IndexType GetIndexType(PropertyRoute route)
        {
            UniqueIndexAttribute at = Attributes(route).OfType<UniqueIndexAttribute>().SingleOrDefaultEx();

            return at == null ? IndexType.None :
                at.AllowMultipleNulls ? IndexType.UniqueMultipleNulls :
                IndexType.Unique;
        }

        public bool ImplementedBy<T>(Expression<Func<T, object>> route, Type typeToImplement) where T : IdentifiableEntity
        {
            var imp = GetImplementations(route);
            return imp != null && imp.ImplementedBy(typeToImplement);
        }

        public void AssertImplementedBy<T>(Expression<Func<T, object>> route, Type typeToImplement) where T : IdentifiableEntity
        {
            var propRoute = PropertyRoute.Construct(route);

            var imp = GetImplementations(propRoute);

            if (imp == null || !imp.ImplementedBy(typeToImplement))
                throw new InvalidOperationException("Route {0} is not ImplementedBy {2}".Formato(route, typeToImplement.Name));
        }

        public Implementations GetImplementations<T>(Expression<Func<T, object>> route) where T : IdentifiableEntity
        {
            return GetImplementations(PropertyRoute.Construct(route));
        }

        internal Implementations GetImplementations(PropertyRoute route)
        {
            var fieldAtt = Attributes(route);

            ImplementedByAttribute ib = fieldAtt.OfType<ImplementedByAttribute>().SingleOrDefaultEx();
            ImplementedByAllAttribute iba = fieldAtt.OfType<ImplementedByAllAttribute>().SingleOrDefaultEx();

            if (ib != null && iba != null)
                throw new NotSupportedException("Route {0} contains both {1} and {2}".Formato(route, ib.GetType().Name, iba.GetType().Name));

            if (ib != null) return ib;
            if (iba != null) return iba;

            return null;
        }

        internal SqlDbType? GetSqlDbType(PropertyRoute route)
        {
            SqlDbTypeAttribute att = Attributes(route).OfType<SqlDbTypeAttribute>().SingleOrDefaultEx();

            if (att != null && att.HasSqlDbType)
                return att.SqlDbType;

            return TypeValues.TryGetS(route.Type.UnNullify());
        }

        internal int? GetSqlSize(PropertyRoute route, SqlDbType sqlDbType)
        {
            SqlDbTypeAttribute att = Attributes(route).OfType<SqlDbTypeAttribute>().SingleOrDefaultEx();

            if (att != null && att.HasSize)
                return att.Size;

            return defaultSize.TryGetS(sqlDbType);
        }

        internal int? GetSqlScale(PropertyRoute route, SqlDbType sqlDbType)
        {
            SqlDbTypeAttribute att = Attributes(route).OfType<SqlDbTypeAttribute>().SingleOrDefaultEx();

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
    }

    internal enum ReferenceFieldType
    {
        Reference,
        ImplementedBy,
        ImplmentedByAll,
    }
}
