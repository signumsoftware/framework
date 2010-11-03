using System;
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
        class SchemaTypeSettings
        {
            public Attribute[] TypeAttributes;
            public Dictionary<string, Attribute[]> FieldAttributes = new Dictionary<string, Attribute[]>(); 
        }

        Dictionary<Type, SchemaTypeSettings> types = new Dictionary<Type,SchemaTypeSettings>();

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

        public bool IsTypeAttributesOverriden<T>()
        {
            return IsTypeAttributesOverriden(typeof(T));
        }

        public bool IsTypeAttributesOverriden(Type type)
        {
            var t = types.TryGetC(type);
            return t != null && t.TypeAttributes != null;
        }

        public void OverrideTypeAttributes<T>(params Attribute[] attributes) where T : IIdentifiable
        {
            OverrideTypeAttributes(typeof(T), attributes); 
        }

        public void OverrideTypeAttributes(Type type, params Attribute[] attributes)
        {
            AssertCorrect(attributes, AttributeTargets.Class);
            types.GetOrCreate(type).TypeAttributes = attributes;
        }

        public bool IsFieldAttributesOverriden<T, R>(Expression<Func<T, R>> propertyOrField)
        {
            MemberInfo mi = ReflectionTools.GetMemberInfo(propertyOrField);
            Type type = ReflectionTools.GetReceiverType(propertyOrField);
            FieldInfo fi = Reflector.FindFieldInfo(type, mi, true);
            return IsFieldAttributesOverriden(typeof(T), fi.Name);
        }

        private bool IsFieldAttributesOverriden(Type type, string fieldName)
        {
            var t = types.TryGetC(type);
            return t != null && t.FieldAttributes.ContainsKey(fieldName);
        }

        public void OverrideFieldAttributes<T, R>(Expression<Func<T, R>> lambda, params Attribute[] attributes)
        {
            MemberInfo mi = ReflectionTools.GetMemberInfo(lambda);
            Type type = ReflectionTools.GetReceiverType(lambda);
            FieldInfo fi = Reflector.FindFieldInfo(type, mi, true); 
            OverrideFieldAttributes(typeof(T), fi.Name, attributes); 
        }

        public void OverrideFieldAttributes(Type type, string fieldName, params Attribute[] attributes)
        {
            AssertCorrect(attributes, AttributeTargets.Field);

            types.GetOrCreate(type).FieldAttributes[fieldName] = attributes;
        }

        private void AssertCorrect(Attribute[] attributes, AttributeTargets attributeTargets)
        {
            var incorrects = attributes.Where(a => a.GetType().SingleAttribute<AttributeUsageAttribute>().TryCS(au => (au.ValidOn & attributeTargets) == 0) ?? false);

            if (incorrects.Count() > 0)
                throw new InvalidOperationException("The following attributes ar not compatible with targets {0}: {1}".Formato(attributeTargets, incorrects.ToString(a => a.GetType().Name, ", ")));
        }

        public Attribute[] TypeAttributes(Type type)
        {
            return types.TryGetC(type).TryCC(a => a.TypeAttributes) ?? type.GetCustomAttributes(false).Cast<Attribute>().ToArray(); 
        }

        public Attribute[] FieldInfoAttributes<T, R>(Expression<Func<T, R>> propertyOrField)
        {
            MemberInfo mi = ReflectionTools.GetMemberInfo(propertyOrField);
            Type type = ReflectionTools.GetReceiverType(propertyOrField);
            FieldInfo fi = Reflector.FindFieldInfo(type, mi, true);
            return FieldInfoAttributes(type, fi);
        }

        public Attribute[] FieldInfoAttributes(Type type, FieldInfo fi)
        {
            var result = type.For(t => t != fi.DeclaringType.BaseType, t => t.BaseType)
                .Select(t=>types.TryGetC(t).TryCC(a => a.FieldAttributes.TryGetC(fi.Name)))
                .NotNull().FirstOrDefault();

            return result ?? fi.GetCustomAttributes(false).Cast<Attribute>().ToArray();
        }

        internal bool IsNullable(Type type, FieldInfo fi, Type fieldType, bool forceNull)
        {
            if (forceNull)
                return true;

            if (FieldInfoAttributes(type, fi).OfType<NotNullableAttribute>().Any())
                return false;

            if (FieldInfoAttributes(type, fi).OfType<NullableAttribute>().Any())
                return false;

            return fieldType.IsValueType ? Nullable.GetUnderlyingType(fieldType) != null : true;
        }

        internal IndexType GetIndexType(Type type, FieldInfo fi)
        {
            var att = FieldInfoAttributes(type, fi);

            UniqueIndexAttribute at = att.OfType<UniqueIndexAttribute>().SingleOrDefault();

            return at == null ? IndexType.None :
                at.AllowMultipleNulls ? IndexType.UniqueMultipleNulls :
                IndexType.Unique; 
        }

         public bool ImplementedBy<T, R>(Expression<Func<T, R>> propertyOrField, Type typeToImplement)
         {
             var imp = GetImplementations(propertyOrField);
             return imp != null && imp.ImplementedBy(typeToImplement); 
         }

         public void AssertImplementedBy<T, R>(Expression<Func<T, R>> propertyOrField, Type typeToImplement)
         {
             MemberInfo mi = ReflectionTools.GetMemberInfo(propertyOrField);
             Type type = ReflectionTools.GetReceiverType(propertyOrField);
             FieldInfo fi = Reflector.FindFieldInfo(type, mi, true);
             var imp = GetImplementations(type, fi);

             if (imp == null || !imp.ImplementedBy(typeToImplement))
             {
                 throw new InvalidOperationException("Field {0} from {1} is not ImplementedBy {2}".Formato(fi.Name, type.Name, typeToImplement.Name));
             }
         }

         public Implementations GetImplementations<T, R>(Expression<Func<T, R>> propertyOrField)
         {
             MemberInfo mi = ReflectionTools.GetMemberInfo(propertyOrField);
             Type type = ReflectionTools.GetReceiverType(propertyOrField);
             FieldInfo fi = Reflector.FindFieldInfo(type, mi, true);
             return GetImplementations(type, fi);
         }

        internal Implementations GetImplementations(Type type, FieldInfo fi)
        {
            var fieldAtt = FieldInfoAttributes(type, fi);

            ImplementedByAttribute ib = fieldAtt.OfType<ImplementedByAttribute>().SingleOrDefault();
            ImplementedByAllAttribute iba = fieldAtt.OfType<ImplementedByAllAttribute>().SingleOrDefault();

            if (ib != null && iba != null)
                throw new NotSupportedException("Field {0} contains both {1} and {2}".Formato(fi, ib.GetType(), iba.GetType()));

            if (ib != null) return ib;
            if (iba != null) return iba;

            Type entityType = fi.FieldType;
            entityType = entityType.ElementType() ?? entityType;
            entityType = Reflector.ExtractLite(entityType) ?? entityType; 

            var typeAtt = TypeAttributes(entityType);

            ib = typeAtt.OfType<ImplementedByAttribute>().SingleOrDefault();
            iba = typeAtt.OfType<ImplementedByAllAttribute>().SingleOrDefault();

            if (ib != null && iba != null)
                throw new NotSupportedException("Type {0} contains both {1} and {2}".Formato(entityType, ib.GetType(), iba.GetType()));

            if (ib != null) return ib;
            if (iba != null) return iba;

            return null;
        }

        internal SqlDbType? GetSqlDbType(Type type, FieldInfo fi, Type fieldType)
        {
            SqlDbTypeAttribute att = FieldInfoAttributes(type, fi).OfType<SqlDbTypeAttribute>().SingleOrDefault();

            return att.TryCS(a => a.HasSqlDbType ? a.SqlDbType : (SqlDbType?)null) ?? TypeValues.TryGetS(fieldType.UnNullify());
        }

        internal int? GetSqlSize(Type type, FieldInfo fi, SqlDbType sqlDbType)
        {
            SqlDbTypeAttribute att = FieldInfoAttributes(type, fi).OfType<SqlDbTypeAttribute>().SingleOrDefault();

            return att.TryCS(a => a.HasSize ? a.Size : (int?)null) ?? defaultSize.TryGetS(sqlDbType);
        }

        internal int? GetSqlScale(Type type, FieldInfo fi, SqlDbType sqlDbType)
        {
            SqlDbTypeAttribute att = FieldInfoAttributes(type, fi).OfType<SqlDbTypeAttribute>().SingleOrDefault();

            return att.TryCS(a => a.HasScale ? a.Scale : (int?)null) ?? defaultScale.TryGetS(sqlDbType);
        }

        internal SqlDbType DefaultSqlType(Type type)
        {
            return this.TypeValues.GetOrThrow(type, "Type {0} not registered"); 
        }
    }

    internal enum ReferenceFieldType
    {
        Reference,
        ImplementedBy,
        ImplmentedByAll,
    }
}
