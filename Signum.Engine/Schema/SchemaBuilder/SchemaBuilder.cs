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
using System.Linq.Expressions;
using System.Runtime.Remoting.Contexts;

namespace Signum.Engine.Maps
{
    public class SchemaBuilder
    {
        Schema schema;
        public SchemaSettings Settings
        {
            get { return schema.Settings; }
        }

        public SchemaBuilder()
        {
            schema = new Schema(new SchemaSettings());
            Include<TypeDN>();
        }

        protected SchemaBuilder(Schema schema)
        {
            this.schema = schema; 
        }

        public SchemaBuilder(SchemaSettings settings)
        {
            schema = new Schema(settings);
        }
       
        public Schema Schema
        {
            get { return schema; }
        }

    
        public void AddUniqueIndex<T>(Expression<Func<T, object>> fields) where T : IdentifiableEntity
        {
            AddUniqueIndex<T>(fields, null); 
        }

        public void AddUniqueIndex<T>(Expression<Func<T, object>> fields, Expression<Func<T, object>> fieldsNotNull) where T : IdentifiableEntity
        {
            Schema schema = Schema.Current;

            Expression<Func<T, object>>[] fieldLambdas = Split(fields);
            Expression<Func<T, object>>[] fieldsNotNullLambdas = Split(fieldsNotNull);

            Field[] colFields = fieldLambdas.Select(fun => schema.Field<T>(fun)).ToArray();
            Field[] collFieldsNotNull = fieldsNotNullLambdas.Select(fun => schema.Field<T>(fun)).ToArray();

            AddUniqueIndex(new UniqueIndex(schema.Table<T>(), colFields).WhereNotNull(collFieldsNotNull));
        }

        Expression<Func<T, object>>[] Split<T>(Expression<Func<T, object>> columns)
            where T : IdentifiableEntity
        {
            if (columns == null)
                return new Expression<Func<T, object>>[0];

            if (columns.Body.NodeType == ExpressionType.New)
            {
                return ((NewExpression)columns.Body).Arguments
                    .Select(a => Expression.Lambda<Func<T, object>>(Expression.Convert(a, typeof(object)), columns.Parameters))
                    .ToArray();
            }

            return new[] { columns };
        }

        public void AddUniqueIndex(ITable table, Field[] fields, Field[] notNullFields)
        {
            AddUniqueIndex(new UniqueIndex(table, fields).WhereNotNull(notNullFields));
        }

        public void AddUniqueIndex(ITable table, IColumn[] columns, IColumn[] notNullColumns)
        {
            AddUniqueIndex(new UniqueIndex(table, columns).WhereNotNull(notNullColumns));
        }

        private void AddUniqueIndex(UniqueIndex uniqueIndex)
        {
            ITable table = uniqueIndex.Table;

            if (table.MultiIndexes == null)
                table.MultiIndexes = new List<UniqueIndex>();

            table.MultiIndexes.Add(uniqueIndex);
        }

        public Table Include<T>() where T : IdentifiableEntity
        {
            return Include(typeof(T));
        }

        public virtual Table Include(Type type)
        {
            Table result;
            if (!schema.Tables.TryGetValue(type, out result))
            {
                if (type.IsAbstract)
                    throw new InvalidOperationException("Impossible to include in the Schema the type {0} because is abstract".Formato(type));

                if (!Reflector.IsIdentifiableEntity(type))
                    throw new InvalidOperationException("Impossible to include in the Schema the type {0} because is not and IdentifiableEntity".Formato(type));

                result = new Table(type);

                schema.Tables.Add(type, result);

                string name = schema.Settings.desambiguatedNames.TryGetC(type) ?? Reflector.CleanTypeName(Reflector.ExtractEnumProxy(type) ?? type);

                if (schema.NameToType.ContainsKey(name))
                    throw new InvalidOperationException("Two types have the same cleanName, desambiguate using Schema.Current.Settings.Desambiguate method: \r\n {0}\r\n {1}".Formato(schema.NameToType[name].FullName, type.FullName)); 

                schema.NameToType[name] = type;
                schema.TypeToName[type] = name;

                Complete(result);
            }
            return result;
        }

        void Complete(Table table)
        {
            Type type = table.Type;
            table.Identity = Reflector.ExtractEnumProxy(type) == null;
            table.Name = GenerateTableName(type);
            table.CleanTypeName = GenerateCleanTypeName(type);
            table.Fields = GenerateFields(type, Contexts.Normal, table, NameSequence.Void, false);
            table.GenerateColumns();
        }

        HashSet<string> loadedModules = new HashSet<string>();
        public bool NotDefined(MethodBase methodBase)
        {
            return loadedModules.Add(methodBase.DeclaringType.TypeName() + "." + methodBase.Name); 
        }

        public void AssertDefined(MethodBase methodBase)
        {
            string name = methodBase.DeclaringType.TypeName() + "." + methodBase.Name;

            if (!loadedModules.Contains(name))
                throw new ApplicationException("Call {0} first".Formato(name)); 
        }

        #region Field Generator
        protected Dictionary<string, EntityField> GenerateFields(Type type, Contexts contexto, Table table, NameSequence preName, bool forceNull)
        {
            Dictionary<string, EntityField> result = new Dictionary<string, EntityField>();
            foreach (FieldInfo fi in Reflector.InstanceFieldsInOrder(type))
            {
                if (!Settings.FieldInfoAttributes(type, fi).Any(a=>a is IgnoreAttribute))
                {
                    if (!SilentMode() && Reflector.FindPropertyInfo(fi) == null)
                        Debug.WriteLine("Field {0} of type {1} has no property".Formato(fi.Name, type.Name));

                    Field campo = GenerateField(type, fi, fi.FieldType, contexto, table, preName, forceNull);

                    if (result.ContainsKey(fi.Name))
                        throw new InvalidOperationException("Duplicated field with name {0} on {1}, shadowing not supported".Formato(fi.Name, type.TypeName())); 

                    result.Add(fi.Name, new EntityField(type, fi) { Field = campo });
                }
            }
            return result;
        }

        protected virtual bool SilentMode()
        {
            return schema.SilentMode; 
        }

        protected virtual Field GenerateField(Type type, FieldInfo fi, Type fieldType, Contexts contexto, Table table, NameSequence preName, bool forceNull)
        {
            //fieldType: Va variando segun se entra en colecciones o contenidos
            //fi.Type: el tipo del campo asociado

            KindOfField kof = GetKindOfField(type, fi, fieldType).ThrowIfNullS("Field {0} of type {1} has no database representation".Formato(fi.Name, type.Name));

            if ((allowedContexts[kof] & contexto) != contexto)
                throw new InvalidOperationException("Field {0} of Type {1} should be mapped as {2} but is incompatible with context {3}".Formato(fi.Name, type.Name, fieldType, contexto));

            //generacion del nombre del campo
            NameSequence name = preName;
            if (contexto == Contexts.Normal || contexto == Contexts.Embedded || contexto == Contexts.View)
                name = name.Add(GenerateFieldName(type, fi, kof));
            else if (contexto == Contexts.MList && (kof == KindOfField.Enum || kof == KindOfField.Reference))
                name = name.Add(GenerateFieldName(Reflector.ExtractLite(fieldType) ?? fieldType, kof));

            switch (kof)
            {
                case KindOfField.PrimaryKey:
                    return GenerateFieldPrimaryKey(type, fi, table, name);
                case KindOfField.Value:
                    return GenerateFieldValue(type, fi, fieldType, name, forceNull);
                case KindOfField.Reference:
                    {
                        Implementations at = Settings.GetImplementations(type, fi);
                        if (at == null)
                            return GenerateFieldReference(type, fi, fieldType, name, forceNull);
                        else if (at is ImplementedByAttribute)
                            return GenerateFieldImplmentedBy(type, fi, fieldType, name, forceNull, (ImplementedByAttribute)at);
                        else
                            return GenerateFieldImplmentedByAll(type, fi, fieldType, name, forceNull, (ImplementedByAllAttribute)at);
                    }
                case KindOfField.Enum:
                    return GenerateFieldEnum(type, fi, fieldType, name, forceNull);
                case KindOfField.Embedded:
                    return GenerateFieldEmbedded(type, fi, fieldType, name, forceNull);
                case KindOfField.MList:
                    return GenerateFieldMList(type, fi, table, name);
                default:
                    throw new NotSupportedException(Resources.NoWayOfMappingType0Found.Formato(fieldType));
            }
        }

        static Dictionary<KindOfField, Contexts> allowedContexts = new Dictionary<KindOfField, Contexts>()
        {
            {KindOfField.PrimaryKey,    Contexts.Normal },
            {KindOfField.Value,         Contexts.Normal | Contexts.MList | Contexts.Embedded | Contexts.View },
            {KindOfField.Reference,     Contexts.Normal | Contexts.MList | Contexts.Embedded | Contexts.View },
            {KindOfField.Enum,          Contexts.Normal | Contexts.MList | Contexts.Embedded | Contexts.View },
            {KindOfField.Embedded,      Contexts.Normal | Contexts.MList | Contexts.Embedded | Contexts.View },
            {KindOfField.MList,         Contexts.Normal },
        };

        private KindOfField? GetKindOfField(Type type, FieldInfo fi, Type fieldType)
        {
            if (fi.FieldEquals((IdentifiableEntity ie) => ie.id))
                return KindOfField.PrimaryKey;

            if (Settings.GetSqlDbType(type, fi, fieldType.UnNullify()) != null)
                return KindOfField.Value;

            if (fieldType.UnNullify().IsEnum)
                return KindOfField.Enum;

            if (Reflector.IsIIdentifiable(Reflector.ExtractLite(fieldType) ?? fieldType))
                return KindOfField.Reference;

            if (Reflector.IsEmbeddedEntity(fieldType))
                return KindOfField.Embedded;

            if (Reflector.IsMList(fieldType))
                return KindOfField.MList;

            return null;
        }

        private static Field GenerateFieldPrimaryKey(Type type, FieldInfo fi, Table table, NameSequence name)
        {
            return new FieldPrimaryKey(fi.FieldType, table);
        }

        protected virtual Field GenerateFieldValue(Type type, FieldInfo fi, Type fieldType, NameSequence name, bool forceNull)
        {
            SqlDbType sqlDbType = Settings.GetSqlDbType(type, fi, fieldType.UnNullify()).Value;

            return new FieldValue(fieldType)
            {
                Name = name.ToString(),
                SqlDbType = sqlDbType,
                Nullable = Settings.IsNullable(type, fi, fieldType, forceNull),
                Size = Settings.GetSqlSize(type, fi, sqlDbType),
                Scale = Settings.GetSqlScale(type, fi, sqlDbType),
                IndexType = Settings.GetIndexType(type, fi)
            };
        }

        protected virtual Field GenerateFieldEnum(Type type, FieldInfo fi, Type fieldType, NameSequence name, bool forceNull)
        {
            Type cleanEnum = fieldType.UnNullify();

            var table = Include(Reflector.GenerateEnumProxy(cleanEnum));

            return new FieldEnum(fieldType)
            {
                Name = name.ToString(),
                Nullable = Settings.IsNullable(type, fi, fieldType, forceNull),
                IsLite = false,
                IndexType = Settings.GetIndexType(type, fi),
                ReferenceTable = cleanEnum.HasAttribute<FlagsAttribute>() && !fi.HasAttribute<ForceForeignKey>() ? null : table,
            };
        }

        protected virtual Field GenerateFieldReference(Type type, FieldInfo fi, Type fieldType, NameSequence name, bool forceNull)
        {
            return new FieldReference(fieldType)
            {
                Name = name.ToString(),
                IndexType = Settings.GetIndexType(type, fi),
                Nullable = Settings.IsNullable(type, fi, fieldType, forceNull),
                IsLite  = Reflector.ExtractLite(fieldType) != null,
                ReferenceTable = Include(Reflector.ExtractLite(fieldType) ?? fieldType),
            };
        }

        protected virtual Field GenerateFieldImplmentedBy(Type type, FieldInfo fi, Type fieldType, NameSequence name, bool forceNull, ImplementedByAttribute ib)
        {
            Type cleanType = Reflector.ExtractLite(fieldType) ?? fieldType;
            string erroneos = ib.ImplementedTypes.Where(t => !cleanType.IsAssignableFrom(t)).ToString(t => t.TypeName(), ", ");
            if (erroneos.Length != 0)
                throw new InvalidOperationException("Type {0} do not implement {1}".Formato(erroneos, cleanType));

            bool nullable = Settings.IsNullable(type, fi, fieldType, forceNull) || ib.ImplementedTypes.Length > 1;

            return new FieldImplementedBy(fieldType)
            {
                IndexType = Settings.GetIndexType(type, fi),
                ImplementationColumns = ib.ImplementedTypes.ToDictionary(t => t, t => new ImplementationColumn
                {
                    ReferenceTable = Include(t),
                    Name = name.Add(TypeLogic.GetCleanName(t)).ToString(),
                    Nullable = nullable,
                }),
                IsLite  = Reflector.ExtractLite(fieldType) != null
            };
        }

        protected virtual Field GenerateFieldImplmentedByAll(Type type, FieldInfo fi, Type fieldType, NameSequence preName, bool forceNull, ImplementedByAllAttribute iba)
        {
            bool nullable = Settings.IsNullable(type, fi, fieldType, forceNull);

            return new FieldImplementedByAll(fieldType)
            {
                IndexType = Settings.GetIndexType(type, fi),
                Column = new ImplementationColumn
                {
                    Name = preName.ToString(),
                    Nullable = nullable,
                    ReferenceTable = null,
                },
                ColumnTypes = new ImplementationColumn
                {
                    Name = preName.Add("Type").ToString(),
                    Nullable = nullable,
                    ReferenceTable = Include(typeof(TypeDN))
                },
                IsLite = Reflector.ExtractLite(fieldType) != null
            };
        }

        protected virtual Field GenerateFieldMList(Type type, FieldInfo fi, Table table, NameSequence name)
        {
            Type elementType = fi.FieldType.ElementType();

            RelationalTable relationalTable = new RelationalTable(fi.FieldType)
            {
                Name = GenerateTableNameCollection(type, name),
                BackReference = new FieldReference(table.Type)
                {
                    Name = GenerateBackReferenceName(type),
                    ReferenceTable = table
                },
                PrimaryKey = new RelationalTable.PrimaryKeyColumn(),
                Field = GenerateField(type, fi, elementType, Contexts.MList, null, NameSequence.Void, false) 
            };

            relationalTable.GenerateColumns(); 

            return new FieldMList(fi.FieldType)
            {
                RelationalTable = relationalTable,
            };
        }

        protected virtual Field GenerateFieldEmbedded(Type type, FieldInfo fi, Type fieldType, NameSequence name, bool forceNull)
        {
            bool nullable = Settings.IsNullable(type, fi, fieldType, false);

            return new FieldEmbedded(fieldType)
            {
                HasValue = nullable ? new FieldEmbedded.EmbeddedHasValueColumn() { Name = name.Add("HasValue").ToString() } : null,
                EmbeddedFields = GenerateFields(fieldType, Contexts.Embedded, null, name, nullable || forceNull)
            };
        }
        #endregion

        #region Names

        public virtual string GenerateTableName(Type type)
        {
            return CleanType(type).Name;
        }

        public virtual string GenerateCleanTypeName(Type type)
        {
            type = CleanType(type);

            CleanTypeNameAttribute ctn = Settings.TypeAttributes(type).OfType<CleanTypeNameAttribute>().SingleOrDefault();
            if (ctn != null)
                return ctn.Name;

            return Reflector.CleanTypeName(type);
        }

        protected static Type CleanType(Type type)
        {
            type = Reflector.ExtractLite(type) ?? type;
            type = Reflector.ExtractEnumProxy(type) ?? type;
            return type;
        }

        public virtual string GenerateTableNameCollection(Type type, NameSequence name)
        {
            return CleanType(type).Name + name.ToString();
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
                    return "id" + CleanType(type).Name;
                default:
                    throw new NotImplementedException("No field name for type {0} defined".Formato(type));
            }
        }

        public virtual string GenerateFieldName(Type type, FieldInfo fi, KindOfField tipoCampo)
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
                    throw new NotImplementedException("No name for {0} defined".Formato(fi.Name));
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
        public ViewBuilder(Schema schema) : base(schema)
        {
        }

        public override Table Include(Type type)
        {
            return Schema.Table(type);
        }

        protected override bool SilentMode()
        {
            return true;
        }

        public Table NewView(Type type)
        {
            Table table = new Table(type)
            {
                Name = GenerateTableName(type),
                IsView = true
            };

            table.Fields = GenerateFields(type, Contexts.View, table, NameSequence.Void, false);

            return table;
        }

        public override string GenerateTableName(Type type)
        {
            SqlViewNameAttribute vn = type.SingleAttribute<SqlViewNameAttribute>();
            if (vn != null)
                return vn.Name;

            return CleanType(type).Name;
        }

        public override string GenerateFieldName(Type type, FieldInfo fi, KindOfField tipoCampo)
        {
            SqlViewColumnAttribute vc = fi.SingleAttribute<SqlViewColumnAttribute>();
            if (vc != null)
                return vc.Name;

            return base.GenerateFieldName(type, fi, tipoCampo);
        }

        public override string GenerateFieldName(Type type, KindOfField tipoCampo)
        {
            return base.GenerateFieldName(type, tipoCampo);
        }
    }

}
