using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Data;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using System.Diagnostics;
using Signum.Entities.Reflection;
using Signum.Engine.Properties;

namespace Signum.Engine.Maps
{
    public class SchemaBuilder
    {
        SchemaBuilderSettings settings;

        public SchemaBuilder() : this(new SchemaBuilderSettings()) { }

        public SchemaBuilder(SchemaBuilderSettings settings)
        {
            this.settings = settings;
        }

        Schema schema = new Schema();
        public Schema Schema
        {
            get { return schema; }
            set { schema = value; }
        }

        public Table Include<T>() where T : IdentifiableEntity
        {
            return Include(typeof(T));
        }

        /// <summary>
        /// Includes a type in the Schema
        /// </summary>
        public virtual Table Include(Type type)
        {
            Table result;
            if (!schema.Tables.TryGetValue(type, out result))
            {
                result = PreCreate(type);

                schema.Tables.Add(type, result);

                Complete(result);
            }
            return result;
        }

        Table PreCreate(Type type)
        {
            if (type.IsAbstract)
                throw new ApplicationException(Resources.ImpossibleToIncludeInTheSchema + " " +  Resources.Type0IsAbstract.Formato(type));

            if (!Reflector.IsIdentifiableEntity(type) && !type.IsEnum)
                throw new ApplicationException(Resources.ImpossibleToIncludeInTheSchema + " " + Resources.Type0IsNotAnIdentifiableEntityOrAnEnum.Formato(type));
            
            return new Table(type);
        }

        void Complete(Table table)
        {
            Type type = table.Type;
            table.Identity = Reflector.ExtractEnumProxy(type) == null;
            table.Name = GenerateTableName(type);
            table.Fields = GenerateFields(type, Contexts.Normal, table, NameSequence.Void);
            table.GenerateColumns();
        }

        HashSet<string> loadedModules = new HashSet<string>();

        public bool NotDefined<T>()
        {
            return NotDefined(typeof(T).FullName); 
        }

        public bool NotDefined(string moduleName)
        {
            return loadedModules.Add(moduleName); 
        }

        bool notifyFieldsWithoutProperty = true;
        public bool NotifyFieldsWithoutProperty
        {
            get { return notifyFieldsWithoutProperty; }
            set { this.notifyFieldsWithoutProperty = value; }
        }

        #region Field Generator
        protected Dictionary<string, EntityField> GenerateFields(Type type, Contexts contexto, Table table, NameSequence preName)
        {
            Dictionary<string, EntityField> result = new Dictionary<string, EntityField>();
            foreach (FieldInfo fi in Reflector.InstanceFieldsInOrder(type))
            {
                if (!settings.FieldInfoAttributes(type, fi).Any(a=>a is IgnoreAttribute))
                {
                    if (NotifyFieldsWithoutProperty && Reflector.FindPropertyInfo(fi) == null)
                        Debug.WriteLine(Resources.Field0OfTipe1HasNoCompatibleProperty.Formato(fi.Name, type.Name));

                    Field campo = GenerateField(type, fi, fi.FieldType, contexto, table, preName);
                    result.Add(fi.Name, new EntityField(type, fi) { Field = campo });
                }
            }
            return result;
        }

        protected virtual Field GenerateField(Type type, FieldInfo fi, Type fieldType, Contexts contexto, Table table, NameSequence preName)
        {
            //fieldType: Va variando segun se entra en colecciones o contenidos
            //fi.Type: el tipo del campo asociado

            KindOfField kof = GetKindOfField(type, fi, fieldType).ThrowIfNullS(Resources.Field0OfType1HasNoDatabaseRepresentation.Formato(fi.Name, type.Name));

            if ((contextosAdmitidos[kof] & contexto) != contexto)
                throw new InvalidOperationException(Resources.Field0OfType1ShouldBeMappedAs2ButItIsIncompatibleWithContext3.Formato(fi.Name, type.Name, fieldType, contexto));

            //generacion del nombre del campo
            NameSequence name = preName;
            if (contexto == Contexts.Normal || contexto == Contexts.Embedded || contexto == Contexts.View)
                name = name.Add(GenerateFieldName(fi, kof));
            else if (contexto == Contexts.MList && (kof == KindOfField.Enum || kof == KindOfField.Reference))
                name = name.Add(GenerateFieldName(Reflector.ExtractLazy(fieldType) ?? fieldType, kof));

            switch (kof)
            {
                case KindOfField.PrimaryKey:
                    return GenerateFieldPrimaryKey(type, fi, table.Identity, name);
                case KindOfField.Value:
                    return GenerateFieldValue(type, fi, fieldType, name);
                case KindOfField.Reference:
                    {
                        Attribute at = settings.GetReferenceFieldType(type, fi, Reflector.ExtractLazy(fieldType) ?? fieldType);
                        if (at == null)
                            return GenerateFieldReference(type, fi, fieldType, name);
                        else if (at is ImplementedByAttribute)
                            return GenerateFieldImplmentedBy(type, fi, fieldType, name, (ImplementedByAttribute)at);
                        else
                            return GenerateFieldImplmentedByAll(type, fi, fieldType, name, (ImplementedByAllAttribute)at);
                    }
                case KindOfField.Enum:
                    return GenerateFieldEnum(type, fi, fieldType, name);
                case KindOfField.Embedded:
                    return GenerateFieldEmbebed(type, fi, fieldType, name);
                case KindOfField.MList:
                    return GenerateFieldMList(type, fi, table, name);
                default:
                    throw new ApplicationException(Resources.NoWayOfMappingType0Found.Formato(fieldType));
            }
        }

        static Dictionary<KindOfField, Contexts> contextosAdmitidos = new Dictionary<KindOfField, Contexts>()
        {
            {KindOfField.PrimaryKey,      Contexts.Normal  },
            {KindOfField.Value,           Contexts.Normal | Contexts.MList | Contexts.Embedded | Contexts.View },
            {KindOfField.Reference,      Contexts.Normal  | Contexts.MList | Contexts.Embedded | Contexts.View },
            {KindOfField.Enum,            Contexts.Normal | Contexts.MList | Contexts.Embedded | Contexts.View },
            {KindOfField.Embedded,       Contexts.Normal   | Contexts.MList | Contexts.Embedded | Contexts.View},
            {KindOfField.MList,       Contexts.Normal },
        };

        private KindOfField? GetKindOfField(Type type, FieldInfo fi, Type fieldType)
        {
            if (fi.FieldEquals<IdentifiableEntity>(i => i.id))
                return KindOfField.PrimaryKey;

            if (settings.GetSqlDbType(type, fi, fieldType.UnNullify()) != null)
                return KindOfField.Value;

            if (fieldType.UnNullify().IsEnum)
                return KindOfField.Enum;

            if (Reflector.IsIIdentifiable(Reflector.ExtractLazy(fieldType) ?? fieldType))
                return KindOfField.Reference;

            if (Reflector.IsEmbeddedEntity(fieldType))
                return KindOfField.Embedded;

            if (Reflector.IsMList(fieldType))
                return KindOfField.MList;

            return null;
        }

        public virtual Index DefaultReferenceIndex()
        {
            return Index.Multiple;
        }

        private static Field GenerateFieldPrimaryKey(Type type, FieldInfo fi, bool identity, NameSequence name)
        {
            return new PrimaryKeyField(fi.FieldType)
            {
                Identity = identity
            };
        }

        protected virtual Field GenerateFieldValue(Type type, FieldInfo fi, Type fieldType, NameSequence name)
        {
            SqlDbType sqlDbType = settings.GetSqlDbType(type, fi, fieldType.UnNullify()).Value;

            return new ValueField(fieldType)
            {
                Name = name.ToString(),
                SqlDbType = sqlDbType,
                Nullable = settings.IsNullable(type, fi, fieldType),
                Size = settings.GetSqlSize(type, fi, sqlDbType),
                Scale = settings.GetSqlScale(type, fi, sqlDbType),
                Index = settings.IndexType(type, fi) ?? Index.None
            };
        }

        protected virtual Field GenerateFieldEnum(Type type, FieldInfo fi, Type fieldType, NameSequence name)
        {
            return new EnumField(fieldType)
            {
                Nullable = settings.IsNullable(type, fi, fieldType),
                IsLazy = false,
                Index = settings.IndexType(type, fi) ?? Index.None,
                Name = name.ToString(),
                ReferenceTable = Include(Reflector.GenerateEnumProxy(fieldType.UnNullify())),
            };
        }

        protected virtual Field GenerateFieldReference(Type type, FieldInfo fi, Type fieldType, NameSequence name)
        {
            return new ReferenceField(fieldType)
            {
                Name = name.ToString(),
                ReferenceTable = Include(Reflector.ExtractLazy(fieldType) ?? fieldType),
                Index = settings.IndexType(type, fi) ?? DefaultReferenceIndex(),
                Nullable = settings.IsNullable(type, fi, fieldType),
                IsLazy  = Reflector.ExtractLazy(fieldType) != null
            };
        }

        protected virtual Field GenerateFieldImplmentedBy(Type type, FieldInfo fi, Type fieldType, NameSequence name, ImplementedByAttribute ib)
        {
            string erroneos = ib.ImplementedTypes.Where(t => !fieldType.IsAssignableFrom(t)).ToString(t => t.TypeName(), ", ");
            if (erroneos.Length != 0)
                throw new InvalidOperationException(Resources.Types0DoNotImplement1.Formato(erroneos, fieldType));

            Index indice = settings.IndexType(type, fi) ?? DefaultReferenceIndex();

            bool nullable = settings.IsNullable(type, fi, fieldType) || ib.ImplementedTypes.Length > 1;

            return new ImplementedByField(fieldType)
            {
                ImplementationColumns = ib.ImplementedTypes.ToDictionary(t => t, t => new ImplementationColumn
                {
                    ReferenceTable = Include(t),
                    Name = name.Add(t.Name).ToString(),
                    Index = indice,
                    Nullable = true,
                }),
                IsLazy  = Reflector.ExtractLazy(fieldType) != null
            };
        }

        protected virtual Field GenerateFieldImplmentedByAll(Type type, FieldInfo fi, Type fieldType, NameSequence preName, ImplementedByAllAttribute implementedByAllAttribute)
        {
            Index indice = settings.IndexType(type, fi) ?? DefaultReferenceIndex();
            bool nullable = settings.IsNullable(type, fi, fieldType);

            return new ImplementedByAllField(fieldType)
            {
                Column = new ImplementationColumn
                {
                    Name = preName.ToString(),
                    Index = indice,
                    Nullable = nullable,
                    ReferenceTable = null,
                },
                ColumnTypes = new ImplementationColumn
                {
                    Name = preName.Add("Type").ToString(),
                    Index = indice,
                    Nullable = nullable,
                    ReferenceTable = Include(typeof(TypeDN))
                },
                IsLazy = Reflector.ExtractLazy(fieldType) != null
            };
        }

        protected virtual Field GenerateFieldMList(Type type, FieldInfo fi, Table table, NameSequence name)
        {
            Type elementType = Reflector.CollectionType(fi.FieldType);

            return new MListField(fi.FieldType)
            {
                RelationalTable = new RelationalTable(fi.FieldType)
                {
                    Name = GenerateTableNameCollection(type, name),
                    BackReference = new RelationalTable.BackReferenceColumn
                    {
                        Name = GenerateBackReferenceName(type),
                        Index = DefaultReferenceIndex(),
                        ReferenceTable = table
                    },
                    PrimaryKey = new RelationalTable.PrimaryKeyColumn(),
                    Field = GenerateField(type, fi, elementType, Contexts.MList, null, NameSequence.Void) // sin FieldInfo!
                }.Do(t => t.GenerateColumns())
            };
        }

        protected virtual Field GenerateFieldEmbebed(Type type, FieldInfo fi, Type fieldType, NameSequence name)
        {
            return new EmbeddedField(fieldType)
            {
                EmbeddedFields = GenerateFields(fieldType, Contexts.Embedded, null, name)
            };
        }
        #endregion

        #region Names

        public virtual string GenerateTableName(Type type)
        {
            return TypeName(type);
        }

        public virtual string TypeName(Type type)
        {
            type = Reflector.ExtractLazy(type) ?? type;
            type = Reflector.ExtractEnumProxy(type) ?? type;
            return type.Name;
        }

        public virtual string GenerateTableNameCollection(Type type, NameSequence name)
        {
            return TypeName(type) + name.ToString();
        }

        public virtual string GenerateFieldName(Type type, KindOfField tipoCampo)
        {
            switch (tipoCampo)
            {
                case KindOfField.Value:
                case KindOfField.Embedded:
                    return type.Name.FirstUpper();
                case KindOfField.Enum:
                case KindOfField.Reference:
                    return "id" + TypeName(type);
                default:
                    throw new NotImplementedException(Resources.NoNameForType0Defined.Formato(type));
            }
        }

        public virtual string GenerateFieldName(FieldInfo fi, KindOfField tipoCampo)
        {
            string name = Reflector.CleanFieldName(fi.Name);

            switch (tipoCampo)
            {
                case KindOfField.PrimaryKey:
                case KindOfField.Value:
                case KindOfField.Embedded:
                case KindOfField.MList:  //se usa solo para el nombre de la tabla 
                    return name;
                case KindOfField.Reference:
                case KindOfField.Enum:
                    return "id" + name;
                default:
                    throw new NotImplementedException(Resources.NoNameForField0Defined.Formato(fi.Name));
            }
        }

        public virtual string GenerateBackReferenceName(Type type)
        {
            return "idParent";
        }
        #endregion
    }

    internal class ViewBuilder : SchemaBuilder
    {
        public ViewBuilder(Schema schema)
        {
            this.Schema = schema;
            this.NotifyFieldsWithoutProperty = false; 
        }

        public override Table Include(Type type)
        {
            return Schema.Table(type);
        }

        public Table NewView(Type type)
        {
            Table table = new Table(type)
            {
                Name = GenerateTableName(type),
                IsView = true
            };

            table.Fields = GenerateFields(type, Contexts.View, table, NameSequence.Void);

            return table;
        }

        public override string GenerateTableName(Type type)
        {
            SqlViewNameAttribute vn = type.SingleAttribute<SqlViewNameAttribute>();

            return vn.TryCC(a => a.Name) ?? TypeName(type);
        }

        public override string GenerateFieldName(FieldInfo fi, KindOfField tipoCampo)
        {
            SqlViewColumnAttribute vc = fi.SingleAttribute<SqlViewColumnAttribute>();

            return vc.TryCC(a => a.Name) ?? base.GenerateFieldName(fi, tipoCampo);
        }

        public override string GenerateFieldName(Type type, KindOfField tipoCampo)
        {
            return base.GenerateFieldName(type, tipoCampo);
        }
    }

}
