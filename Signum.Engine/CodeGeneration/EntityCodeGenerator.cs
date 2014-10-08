using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;

namespace Signum.Engine.CodeGeneration
{
    public class EntityCodeGenerator
    {
        public string SolutionName;
        public string SolutionFolder;

        public Dictionary<ObjectName, DiffTable> Tables;
        public DirectedGraph<DiffTable> Graph;

        public Schema CurrentSchema; 

        public virtual void GenerateEntitiesFromDatabaseTables()
        {
            CurrentSchema = Schema.Current;

            var tables = SchemaSynchronizer.DefaultGetDatabaseDescription(Schema.Current.DatabaseNames()).Values.ToList();

            CleanSchema(tables);

            this.Tables = tables.ToDictionary(a=>a.Name);

            Graph = DirectedGraph<DiffTable>.Generate(tables, t =>
                t.Colums.Values.Select(a => a.ForeingKey).NotNull().Select(a => a.TargetTable).Distinct().Select(on => this.Tables.GetOrThrow(on)));

            GetSolutionInfo(out SolutionFolder, out SolutionName);

            string projectFolder = GetProjectFolder();

            if (!Directory.Exists(projectFolder))
                throw new InvalidOperationException("{0} not found. Override GetProjectFolder".Formato(projectFolder));

            foreach (var gr in Tables.Values.GroupBy(t => GetFileName(t)))
            {
                string str = WriteFile(gr.Key, gr);

                string fileName = Path.Combine(projectFolder, gr.Key);

                FileTools.CreateParentDirectory(fileName);
                
                File.WriteAllText(fileName, str);
            }
        }

        protected virtual string GetProjectFolder()
        {
            return Path.Combine(SolutionFolder, SolutionName + ".Entities");
        }

        protected virtual void CleanSchema(List<DiffTable> tables)
        {
            
        }
        
        protected virtual void GetSolutionInfo(out string solutionFolder, out string solutionName)
        {
            CodeGenerator.GetSolutionInfo(out solutionFolder, out solutionName);
        }

        protected virtual string GetFileName(DiffTable t)
        {
            string name = t.Name.ToString().Replace('.', '\\');

            name = Regex.Replace(name, "[" + Regex.Escape(new string(Path.GetInvalidPathChars())) + "]", "");

            return name + ".cs";
        }

        protected virtual string WriteFile(string fileName, IEnumerable<DiffTable> tables)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in GetUsingNamespaces(fileName, tables))
                sb.AppendLine("using {0};".Formato(item));

            sb.AppendLine();
            sb.AppendLine("namespace " + GetNamespace(fileName, tables));
            sb.AppendLine("{");

            foreach (var t in tables.OrderByDescending(a => a.Colums.Count))
            {
                sb.Append(WriteEntity(fileName, t).Indent(4));

                sb.AppendLine();
                sb.AppendLine();
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

        protected virtual IEnumerable<string> GetUsingNamespaces(string fileName, IEnumerable<DiffTable> tables)
        {
            return new List<string> 
            {
                "System",
                "System.Collections.Generic",
                "System.Data",
                "System.Linq",
                "System.Linq.Expressions",
                "System.Text",
                "Signum.Entities",
                "Signum.Utilities",
            };
        }

        protected virtual string GetNamespace(string fileName, IEnumerable<DiffTable> tables)
        {
            return SolutionName + ".Entities";
        }

        protected virtual void WriteAttributeTag(StringBuilder sb, IEnumerable<string> attributes)
        {
            foreach (var gr in attributes.GroupsOf(a => a.Length, 100))
            {
                sb.AppendLine("[" + gr.ToString(", ") + "]");
            }
        }

        protected virtual string WriteEntity(string fileName, DiffTable table)
        {
            var mListInfo = GetMListInfo(table);

            if (mListInfo != null && mListInfo.TrivialElementColumn != null)
                return null;

            var name = GetEntityName(table.Name);

            StringBuilder sb = new StringBuilder();
            WriteAttributeTag(sb, GetEntityAttributes(fileName, table, mListInfo));
            sb.AppendLine("public class {0} : {1}".Formato(name, GetBaseClass(table.Name, mListInfo)));
            sb.AppendLine("{");

            string beforeFields = WriteBeforeFields(table, name);
            if (beforeFields != null)
            {
                sb.Append(beforeFields.Indent(4));
                sb.AppendLine();
            }

            foreach (var col in table.Colums.Values)
            {
                string field = WriteField(fileName, table, col);

                if (field != null)
                {
                    sb.Append(field.Indent(4));
                    sb.AppendLine();
                }
            }

            if (mListInfo == null)
            {
                foreach (var relatedTable in Graph.InverseRelatedTo(table))
                {
                    var mListInfo2 = GetMListInfo(relatedTable);

                    if (mListInfo2 != null)
                    {
                        string field = WriteFieldMList(fileName, table, mListInfo2, relatedTable);

                        if (field != null)
                        {
                            sb.AppendLine(field.Indent(4));
                            sb.AppendLine();
                        }
                    }
                }
            }

            string afterFields = WriteAfterFields(table, name);
            if (afterFields != null)
            {
                sb.Append(afterFields.Indent(4));
                sb.AppendLine();
            }

            if (mListInfo == null)
            {
                string toString = WriteToString(table);
                if (toString != null)
                {
                    sb.Append(toString.Indent(4));
                    sb.AppendLine();
                }
            }

            sb.AppendLine("}");

            if (mListInfo == null)
            {
                string operations = WriteOperations(table);
                if (operations != null)
                {
                    sb.AppendLine();
                    sb.Append(operations);
                }
            }

            return sb.ToString();
        }

        protected virtual string WriteBeforeFields(DiffTable table, string name)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var ix in table.Indices.Values.Where(a => a.Columns.Count > 1 || a.FilterDefinition.HasText()))
            {
                sb.AppendLine("//Add to Logic class");
                sb.AppendLine("//sb.AddUniqueIndex<{0}>(e => new {{ {1} }}{2});".Formato(name,
                    ix.Columns.ToString(c => "e." + GetFieldName(table, table.Colums.GetOrThrow(c)), ", "),
                    ix.FilterDefinition == null ? null : ", " + ix.FilterDefinition));
            }

            return sb.ToString().DefaultText(null);
        }

        protected virtual string WriteAfterFields(DiffTable table, string name)
        {
            return null;
        }

        protected virtual string WriteOperations(DiffTable table)
        {
            var kind = GetEntityKind(table);
            if (!(kind == EntityKind.Main || kind == EntityKind.Shared || kind == EntityKind.Main))
                return null;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("public static class {0}".Formato(GetOperationName(table.Name)));
            sb.AppendLine("{");
            sb.AppendLine("    public static readonly ExecuteSymbol<{0}> Save = OperationSymbol.Execute<{0}>();".Formato(GetEntityName(table.Name)));
            sb.AppendLine("}");
            return sb.ToString();
        }

        protected virtual string GetOperationName(ObjectName objectName)
        {
            return GetEntityName(objectName).RemoveSuffix("DN") + "Operation";
        }

        protected virtual MListInfo GetMListInfo(DiffTable table)
        {
            return null;
        }

        protected virtual IEnumerable<string> GetEntityAttributes(string fileName, DiffTable table, MListInfo mListInfo)
        {
            List<string> atts = new List<string> { "Serializable" };

            if (mListInfo == null)
            {
                atts.Add("EntityKind(EntityKind." + GetEntityKind(table) + ", EntityData." + GetEntityData(table) + ")");

                string tableNameAttribute = GetTableNameAttribute(table.Name, null);

                if (tableNameAttribute != null)
                    atts.Add(tableNameAttribute);

                string primaryKeyAttribute = GetPrimaryKeyAttribute(table);

                if (primaryKeyAttribute != null)
                    atts.Add(primaryKeyAttribute);

                if (HasTicksField(table))
                    atts.Add("TicksField(false)");
            }

            return atts;
        }

        protected virtual bool HasTicksField(DiffTable table)
        {
            return false;
        }

        protected virtual string GetPrimaryKeyAttribute(DiffTable table)
        {
            DiffColumn primaryKey = GetPrimaryKeyColumn(table);

            if (primaryKey == null)
                return null;

            var def = CurrentSchema.Settings.DefaultPrimaryKeyAttribute;
            
            Type type = GetValueType(primaryKey);

            List<string> parts = new List<string>();
          
            if (primaryKey.Name != def.Name)
                parts.Add("Name=\"" + primaryKey.Name + "\"");

            if (primaryKey.Identity != def.Identity)
            {
                parts.Add("Identity=" + primaryKey.Identity.ToString().ToLower());
                parts.Add("IdentityBehaviour=" + primaryKey.Identity.ToString().ToLower());
            }

            parts.AddRange(GetSqlDbTypeParts(primaryKey, type));

            if (type != def.Type || parts.Any())
                parts.Insert(0, "typeof(" + type.TypeName() + ")");

            if(parts.Any())
                return "PrimaryKey(" + parts.ToString(", ") + ")";

            return null;
        }

        protected virtual DiffColumn GetPrimaryKeyColumn(DiffTable table)
        {
            return table.Colums.Values.SingleOrDefaultEx(a => a.PrimaryKey);
        }

        protected virtual string GetTableNameAttribute(ObjectName objectName, MListInfo mListInfo)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("TableName(\"" + objectName.Name + "\"");
            if (objectName.Schema != SchemaName.Default)
                sb.Append(", SchemaName=\"" + objectName.Schema.Name + "\"");

            if (objectName.Schema.Database != null)
            {
                sb.Append(", DatabaseName=\"" + objectName.Schema.Database.Name + "\"");

                if (objectName.Schema.Database != null)
                {
                    sb.Append(", ServerName=\"" + objectName.Schema.Database.Server.Name + "\"");
                }
            }

            sb.Append(")");
            return sb.ToString();
        }

        protected virtual EntityData GetEntityData(DiffTable table)
        {
            return EntityData.Transactional;
        }

        protected virtual EntityKind GetEntityKind(DiffTable table)
        {
            return EntityKind.Main;
        } 

        protected virtual string GetEntityName(ObjectName objectName)
        {
            return objectName.Name + "DN";
        }

        protected virtual string GetBaseClass(ObjectName objectName, MListInfo mListInfo)
        {
            return mListInfo != null ? typeof(EmbeddedEntity).Name : typeof(Entity).Name;
        }

        protected virtual string WriteField(string fileName, DiffTable table, DiffColumn col)
        {
            if (col.PrimaryKey)
                return null;

            string relatedEntity = GetRelatedEntity(table, col);

            string type = GetFieldType(table, col, relatedEntity);

            string fieldName = GetFieldName(table, col);

            StringBuilder sb = new StringBuilder();

            WriteAttributeTag(sb, GetFieldAttributes(table, col, relatedEntity));
            sb.AppendLine("{0} {1};".Formato(type, CSharpRenderer.Escape(fieldName)));
            WriteAttributeTag(sb, GetPropertyAttributes(table, col, relatedEntity));

            sb.AppendLine("public {0} {1}".Formato(type, fieldName.FirstUpper()));
            sb.AppendLine("{");
            sb.AppendLine("    get { return " + CSharpRenderer.Escape(fieldName) + "; }");
            if (!IsReadonly(table, col))
                sb.AppendLine("    set { Set(ref " + CSharpRenderer.Escape(fieldName) + ", value); }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        protected virtual string GetRelatedEntity(DiffTable table, DiffColumn col)
        {
            if (col.ForeingKey == null)
                return null;

            return GetEntityName(col.ForeingKey.TargetTable);
        }

        protected virtual bool IsReadonly(DiffTable table, DiffColumn col)
        {
            return false;
        }

        protected virtual IEnumerable<string> GetPropertyAttributes(DiffTable table, DiffColumn col, string relatedEntity)
        {
            List<string> attributes = new List<string>();

            if (!col.Nullable && (relatedEntity != null || GetValueType(col).IsClass))
                attributes.Add("NotNullValidator");

            return attributes;
        }

        protected virtual string GetFieldName(DiffTable table, DiffColumn col)
        {
            string name = col.Name.ToPascal(false);

            if (this.GetRelatedEntity(table, col) != null)
            {
                if (name.Length > 2 && name.EndsWith("Id", StringComparison.InvariantCultureIgnoreCase))
                    name = name.RemoveEnd("Id".Length);

                if (name.Length > 2 && name.StartsWith("Id", StringComparison.InvariantCultureIgnoreCase))
                    name = name.RemoveStart("Id".Length);
            }

            return name.FirstLower();
        }

        protected virtual IEnumerable<string> GetFieldAttributes(DiffTable table, DiffColumn col, string relatedEntity)
        {
            List<string> attributes = new List<string>();

            if (!col.Nullable && (relatedEntity != null || GetValueType(col).IsClass))
                attributes.Add("NotNullable");

            if (col.ForeingKey == null)
            {
                string sqlDbType = GetSqlTypeAttribute(table, col);

                if (sqlDbType != null)
                    attributes.Add(sqlDbType);
            }

            if (col.Name != GetFieldName(table, col).FirstUpper())
                attributes.Add("ColumnName(\"" + col.Name + "\")");

            if (table.Indices.Values.Any(a =>  a.FilterDefinition == null && a.Columns.Only() == col.Name))
                attributes.Add("UniqueIndex");

            return attributes;
        }

        protected virtual string GetSqlTypeAttribute(DiffTable table, DiffColumn col)
        {
            Type type = GetValueType(col);
            List<string> parts = GetSqlDbTypeParts(col, type);

            if (parts.Any())
                return "SqlDbType(" + parts.ToString(", ") + ")";

            return null;
        }

        protected virtual List<string> GetSqlDbTypeParts(DiffColumn col, Type type)
        {
            List<string> parts = new List<string>();
            var pair = CurrentSchema.Settings.GetSqlDbTypePair(type);
            if (pair.SqlDbType != col.SqlDbType)
                parts.Add("SqlDbType=SqlDbType." + col.SqlDbType);

            var defaultSize = CurrentSchema.Settings.GetSqlSize(null, pair.SqlDbType);
            if (!(defaultSize == null || defaultSize == col.Precission || defaultSize == col.Length / 2 || defaultSize == int.MaxValue && col.Length == -1))
                parts.Add("Size=" + (col.Length == -1 ? "int.MaxValue" :
                                    col.Length != 0 ? (col.Length / 2).ToString() :
                                    col.Precission != 0 ? col.Precission.ToString() : "0"));

            var defaultScale = CurrentSchema.Settings.GetSqlScale(null, col.SqlDbType);
            if (!(defaultScale == null || col.Scale == defaultScale))
                parts.Add("Scale=" + col.Scale);
            return parts;
        }

        protected virtual string GetFieldType(DiffTable table, DiffColumn col, string relatedEntity)
        {
            if (relatedEntity != null)
            {
                if (IsLite(table, col))
                    return "Lite<" + relatedEntity + ">";

                return relatedEntity;
            }

            var valueType = GetValueType(col);

            if (col.Nullable)
                return valueType.Nullify().TypeName();

            return valueType.TypeName();
        }

        protected virtual bool IsLite(DiffTable table, DiffColumn col)
        {
            return true;
        }

        protected virtual Type GetValueType(DiffColumn col)
        {
            switch (col.SqlDbType)
            {
                case SqlDbType.BigInt: return typeof(long);
                case SqlDbType.Binary: return typeof(byte[]);
                case SqlDbType.Bit: return typeof(bool);
                case SqlDbType.Char: return typeof(char);
                case SqlDbType.Date: return typeof(DateTime);
                case SqlDbType.DateTime: return typeof(DateTime);
                case SqlDbType.DateTime2: return typeof(DateTime);
                case SqlDbType.DateTimeOffset: return typeof(DateTimeOffset);
                case SqlDbType.Decimal: return typeof(Decimal);
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
                case SqlDbType.TinyInt: return typeof(byte);
                case SqlDbType.UniqueIdentifier: return typeof(Guid);
                case SqlDbType.VarBinary: return typeof(byte[]);
                case SqlDbType.VarChar: return typeof(string);
                case SqlDbType.Xml: return typeof(string);
                case SqlDbType.Udt: return Schema.Current.Settings.UdtSqlName
                    .SingleOrDefaultEx(kvp => StringComparer.InvariantCultureIgnoreCase.Equals(kvp.Value, col.UserTypeName))
                    .Key;
                default: throw new NotImplementedException("Unknown translation for " + col.SqlDbType);
            }
        }

        protected virtual string WriteFieldMList(string fileName, DiffTable table, MListInfo mListInfo, DiffTable relatedTable)
        {
            string type;
            List<string> fieldAttributes;
            if(mListInfo.TrivialElementColumn == null )
            {
                type = GetEntityName(relatedTable.Name);
                fieldAttributes = new List<string>{"NotNullable"};
            }
            else
            {
                string relatedEntity = GetRelatedEntity(relatedTable, mListInfo.TrivialElementColumn);
                type = GetFieldType(relatedTable, mListInfo.TrivialElementColumn, relatedEntity);

                fieldAttributes = GetFieldAttributes(relatedTable, mListInfo.TrivialElementColumn, relatedEntity).ToList(); 
            }

       
            string primaryKey = GetPrimaryKeyAttribute(relatedTable);
            if (primaryKey != null)
                fieldAttributes.Add(primaryKey);

            string tableName = GetTableNameAttribute(relatedTable.Name, mListInfo);
            if (tableName != null)
                fieldAttributes.Add(tableName);

            StringBuilder sb = new StringBuilder();

            string fieldName = GetFieldMListName(table, relatedTable);
            WriteAttributeTag(sb, fieldAttributes);

            sb.AppendLine("MList<{0}> {1} = new MList<{0}>()".Formato(type, CSharpRenderer.Escape(fieldName)));
            sb.AppendLine("[NotNullValidator, NoRepeatValidator]");
            sb.AppendLine("public MList<{0}> {1}".Formato(type, fieldName.FirstUpper()));
            sb.AppendLine("{");
            sb.AppendLine("    get { return " + CSharpRenderer.Escape(fieldName) + "; }");
            sb.AppendLine("    set { Set(ref " + CSharpRenderer.Escape(fieldName) + ", value); }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        protected virtual string GetFieldMListName(DiffTable table, DiffTable relatedTable)
        {
            return NaturalLanguageTools.Pluralize(relatedTable.Name.Name).FirstLower();
        }

        protected virtual string WriteToString(DiffTable table)
        {
            var toStringColumn = GetToStringColumn(table);
            if (toStringColumn == null)
                return null;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("static Expression<Func<{0}, string>> ToStringExpression = e => e.Name;".Formato(GetEntityName(table.Name)));
            sb.AppendLine("public override string ToString()");
            sb.AppendLine("{");
            sb.AppendLine("    return ToStringExpression.Evaluate(this);");
            sb.AppendLine("}");
            return sb.ToString();
        }

        protected virtual DiffColumn GetToStringColumn(DiffTable table)
        {
            return table.Colums.TryGetC("Name");
        }
    }

    public class MListInfo
    {
        public MListInfo(DiffColumn backReferenceColumn)
        {
            this.BackReferenceColumn = backReferenceColumn;
        }

        public readonly DiffColumn BackReferenceColumn;
        public DiffColumn TrivialElementColumn;
    }
}
