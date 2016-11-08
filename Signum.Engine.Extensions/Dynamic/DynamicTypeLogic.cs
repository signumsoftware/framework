using Newtonsoft.Json;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.Cache;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Dynamic
{
    public static class DynamicTypeLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<DynamicTypeEntity>()
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.TypeName,
                    });

                DynamicTypeGraph.Register();

                DynamicLogic.GetCodeFiles += GetCodeFiles;
                DynamicLogic.OnWriteDynamicStarter += WriteDynamicStarter;
            }
        }

        public class DynamicTypeGraph : Graph<DynamicTypeEntity>
        {
            public static void Register()
            {
                new Construct(DynamicTypeOperation.Create)
                {
                    Construct = (_) => new DynamicTypeEntity { },
                }.Register();

                new ConstructFrom<DynamicTypeEntity>(DynamicTypeOperation.Clone)
                {
                    Construct = (e, _) => {

                        var def = e.GetDefinition();
                        var result = new DynamicTypeEntity { TypeName = null };
                        result.SetDefinition(def);
                        return result;
                    },
                }.Register();

                new Execute(DynamicTypeOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => {

                        if (!e.IsNew)
                        {
                            var old = e.ToLite().Retrieve();
                            if (e.TypeName != old.TypeName)
                                DynamicSqlMigrationLogic.AddDynamicRename(Replacements.KeyTables, old.TypeName, e.TypeName);

                            var newDef = e.GetDefinition();
                            var oldDef = old.GetDefinition();

                            var pairs = newDef.Properties
                                .Join(oldDef.Properties, n => n.UID, o => o.UID, (n, o) => new { n, o })
                                .Where(a => a.n.Type == a.o.Type);
                            
                            foreach (var a in pairs.Where(a =>  a.n.Name != a.o.Name))
                            {
                                DynamicSqlMigrationLogic.AddDynamicRename(Replacements.KeyColumnsForTable(old.TypeName),
                                    a.o.Name, a.n.Name);
                            }
                        }
                    },
                }.Register();

                new Delete(DynamicTypeOperation.Delete)
                {
                    Delete = (e, _) =>
                    {
                        e.Delete();
                    }
                }.Register();
            }
        }

        public static string GetPropertyType(DynamicProperty property)
        {
            var generator = new DynamicTypeCodeGenerator(DynamicLogic.CodeGenEntitiesNamespace, null, null, new List<string>());

            return generator.GetPropertyType(property);
        }

        internal static List<DynamicTypeEntity> GetTypes()
        {
            CacheLogic.GloballyDisabled = true;
            try
            {
                return ExecutionMode.Global().Using(a => Database.Query<DynamicTypeEntity>().ToList());
            }
            finally
            {
                CacheLogic.GloballyDisabled = false;
            }
        }

        public static void WriteDynamicStarter(StringBuilder sb, int indent) {

            var types = GetTypes();
            foreach (var item in types)
                sb.AppendLine($"{item}Logic.Start(sb, dqm);".Indent(indent));
        }

        public static List<CodeFile> GetCodeFiles()
        {
            if (!Administrator.ExistTable<DynamicTypeEntity>())
                return new List<CodeFile>();

            var types = GetTypes();
            var entities =  types.Select(dt =>
            {
                var def = dt.GetDefinition();

                var dcg = new DynamicTypeCodeGenerator(DynamicLogic.CodeGenEntitiesNamespace, dt.TypeName, def, DynamicLogic.Namespaces);

                var content = dcg.GetFileCode();
                return new CodeFile
                {
                    FileName = dt.TypeName + ".cs",
                    FileContent = content
                };
            }).ToList();
             
            var logics = types.Select(dt =>
            {
                var def = dt.GetDefinition();

                var dlg = new DynamicTypeLogicGenerator(DynamicLogic.CodeGenEntitiesNamespace, dt.TypeName, def, DynamicLogic.Namespaces);

                var content = dlg.GetFileCode();
                return new CodeFile
                {
                    FileName = dt.TypeName + "Logic.cs",
                    FileContent = content
                };
            }).ToList();

            return entities.Concat(logics).ToList();
        }
    }

    public class DynamicTypeCodeGenerator
    {
        public List<string> Usings { get; private set; }
        public string Namespace { get; private set; }
        public string TypeName { get; private set; }
        public DynamicTypeDefinition Def { get; private set; }

        public DynamicTypeCodeGenerator(string @namespace, string typeName, DynamicTypeDefinition def, List<string> usings)
        {
            this.Usings = usings;
            this.Namespace = @namespace;
            this.TypeName = typeName;
            this.Def = def;
        }

        public string GetFileCode()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in this.Usings)
                sb.AppendLine("using {0};".FormatWith(item));

            sb.AppendLine();
            sb.AppendLine("namespace " + this.Namespace);
            sb.AppendLine("{");
            sb.Append(GetEntityCode().Indent(4));
         
            var ops = GetEntityOperation();
            if (ops != null)
            {
                sb.AppendLine();
                sb.Append(ops.Indent(4));
            }
            sb.AppendLine("}");

            return sb.ToString();
        }

        public string GetEntityCode()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var gr in GetEntityAttributes().GroupsOf(a => a.Length, 100))
            {
                sb.AppendLine("[" + gr.ToString(", ") + "]");
            }
            sb.AppendLine($"public class {this.TypeName}Entity : {GetEntityBaseClass(Def.BaseType)}");
            sb.AppendLine("{");

            foreach (var prop in Def.Properties)
            {
                string field = WriteProperty(prop);

                if (field != null)
                {
                    sb.Append(field.Indent(4));
                    sb.AppendLine();
                }
            }

            string toString = GetToString();
            if (toString != null)
            {
                sb.Append(toString.Indent(4));
                sb.AppendLine();
            }

            sb.AppendLine("}");
            sb.AppendLine();

            return sb.ToString();
        }

        public string GetEntityOperation()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[AutoInit]"); //Only for ReflectionServer
            sb.AppendLine($"public static class {this.TypeName}Operation");
            sb.AppendLine("{");
            sb.AppendLine($"    public static readonly ConstructSymbol<{this.TypeName}Entity>.Simple Create = OperationSymbol.Construct<{this.TypeName}Entity>.Simple(typeof({ this.TypeName}Operation), \"Create\");");
            sb.AppendLine($"    public static readonly ExecuteSymbol<{this.TypeName}Entity> Save = OperationSymbol.Execute<{this.TypeName}Entity>(typeof({ this.TypeName}Operation), \"Save\");");
            sb.AppendLine($"    public static readonly DeleteSymbol<{this.TypeName}Entity> Delete = OperationSymbol.Delete<{this.TypeName}Entity>(typeof({ this.TypeName}Operation), \"Delete\");");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string GetEntityBaseClass(DynamicBaseType baseType)
        {
            switch (baseType)
            {
                case DynamicBaseType.Entity: return "Entity";
                default: throw new NotImplementedException();
            }
        }

        protected virtual string GetToString()
        {
            if (Def.ToStringExpression == null)
                return null;
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"static Expression<Func<{TypeName}Entity, string>> ToStringExpression = e => {Def.ToStringExpression};");
            sb.AppendLine("[ExpressionField(\"ToStringExpression\")]");
            sb.AppendLine("public override string ToString()");
            sb.AppendLine("{");
            sb.AppendLine("    return ToStringExpression.Evaluate(this);");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private List<string> GetEntityAttributes()
        {
            List<string> atts = new List<string> { "Serializable" };

            atts.Add("EntityKind(EntityKind." + Def.EntityKind.Value + ", EntityData." + Def.EntityData.Value + ")");

            return atts;
        }

        string Literal(object obj)
        {
            return CSharpRenderer.Value(obj, obj.GetType(), null);
        }

        protected virtual string WriteProperty(DynamicProperty property)
        {
            string type = GetPropertyType(property);

            StringBuilder sb = new StringBuilder();

            string inititalizer = property.IsMList ? $" = new {type}()": null;
            string fieldName = property.Name.FirstLower();

            WriteAttributeTag(sb, GetFieldAttributes(property));
            sb.AppendLine($"{type} {fieldName}{inititalizer};");
            WriteAttributeTag(sb, GetPropertyAttributes(property));
            sb.AppendLine($"public {type} {property.Name}");
            sb.AppendLine("{");
            sb.AppendLine($"    get {{ return this.Get({fieldName}); }}");
            sb.AppendLine($"    set {{ this.Set(ref {fieldName}, value); }}");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private IEnumerable<string> GetPropertyAttributes(DynamicProperty property)
        {
            return property.Validators.EmptyIfNull().Select(v => GetValidatorAttribute(v));
        }

        private string GetValidatorAttribute(DynamicValidator v)
        {
            var name = v.Type + "Validator";

            var extra = v.ExtraArguments();

            if (extra == null)
                return name;

            return $"{name}({extra})";
        }

        protected virtual void WriteAttributeTag(StringBuilder sb, IEnumerable<string> attributes)
        {
            foreach (var gr in attributes.GroupsOf(a => a.Length, 100))
            {
                sb.AppendLine("[" + gr.ToString(", ") + "]");
            }
        }

        private List<string> GetFieldAttributes(DynamicProperty property)
        {
            List<string> atts = new List<string>();
            if (property.IsNullable != IsNullable.Yes)
                atts.Add("NotNullable");

            if (property.Size != null || property.Scale != null)
            {
                var props = new[]
                {
                    property.Size != null ? "Size = " + Literal(property.Size) : null,
                    property.Scale != null ? "Scale = " + Literal(property.Scale) : null,
                }.NotNull().ToString(", ");

                atts.Add($"SqlDbType({props})");
            }

            switch (property.UniqueIndex)
            {
                case Entities.Dynamic.UniqueIndex.No: break;
                case Entities.Dynamic.UniqueIndex.Yes: atts.Add("UniqueIndex"); break;
                case Entities.Dynamic.UniqueIndex.YesAllowNull: atts.Add("UniqueIndex(AllowMultipleNulls = true)"); break;
            }

            return atts;
        }

        public virtual string GetPropertyType(DynamicProperty property)
        {
            if (string.IsNullOrEmpty(property.Type))
                return "";

            string result = SimplifyType(property.Type);

            var t = TryResolveType(property.Type);
            
            if (property.IsNullable != IsNullable.No && t?.IsValueType == true)
                result = result + "?";

            if (property.IsLite)
                result = "Lite<" + result + ">";
            
            if (property.IsMList)
                result = "MList<" + result + ">";

            return result;
        }

        private string SimplifyType(string type)
        {
            var ns = type.TryBeforeLast(".");

            if (ns == null)
                return type;

            if (this.Namespace == ns || this.Usings.Contains(ns))
                return type.AfterLast(".");

            return type;
        }

        public Type TryResolveType(string typeName)
        {
            switch (typeName)
            {
                case "bool": return typeof(bool);
                case "byte": return typeof(byte);
                case "char": return typeof(char);
                case "decimal": return typeof(decimal);
                case "double": return typeof(double);
                case "short": return typeof(short);
                case "int": return typeof(int);
                case "long": return typeof(long);
                case "sbyte": return typeof(sbyte);
                case "float": return typeof(float);
                case "string": return typeof(string);
                case "ushort": return typeof(ushort);
                case "uint": return typeof(uint);
                case "ulong": return typeof(ulong);
            }


            var result = Type.GetType("System." + typeName);

            if (result != null)
                return result;

            var type = TypeLogic.TryGetType(typeName);
            if (type != null)
                return type;

            return null;
        }
    }

    public class DynamicTypeLogicGenerator {

        public List<string> Usings { get; private set; }
        public string Namespace { get; private set; }
        public string TypeName { get; private set; }
        public DynamicTypeDefinition Def { get; private set; }

        public DynamicTypeLogicGenerator(string @namespace, string typeName, DynamicTypeDefinition def, List<string> usings)
        {
            this.Usings = usings;
            this.Namespace = @namespace;
            this.TypeName = typeName;
            this.Def = def;
        }

        public string GetFileCode()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in this.Usings)
                sb.AppendLine("using {0};".FormatWith(item));

            sb.AppendLine();
            sb.AppendLine($"namespace {this.Namespace}");
            sb.AppendLine($"{{");

            sb.AppendLine($"    public static class {this.TypeName}Logic");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))");
            sb.AppendLine($"            {{");
            sb.AppendLine(GetInclude().Indent(16));
            sb.AppendLine(RegisterComplexOperations().Indent(16));
            sb.AppendLine($"            }}");
            sb.AppendLine($"        }}");
            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            return sb.ToString();
        }

        private string GetInclude()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"sb.Include<{this.TypeName}Entity>()");

            if (string.IsNullOrWhiteSpace(this.Def.OperationExecute?.Execute.Trim()))
                sb.AppendLine($"    .WithSave({this.TypeName}Operation.Save)");

            if (string.IsNullOrWhiteSpace(this.Def.OperationDelete?.Delete.Trim()))
                sb.AppendLine($"    .WithDelete({this.TypeName}Operation.Delete)");

            var mcui = this.Def.MultiColumnUniqueIndex;
            if (mcui != null)
                sb.AppendLine($"    .WithUniqueIndex(e => new {{{ mcui.Fields.Select(f => "e." + f).ToString(", ")}}}{(mcui.Where.HasText() ? ", " + mcui.Where : "")})");

            if (this.Def.QueryFields.EmptyIfNull().Any()) {
                var lines = new[] { "Entity = e" }.Concat(this.Def.QueryFields.Select(f => "e." + f));

                sb.AppendLine($@"    .WithQuery(dqm, e => new 
    {{ 
{ lines.ToString(",\r\n").Indent(8)}
    }})");
            }

            sb.Insert(sb.Length - 2, ';');
            return sb.ToString();
        }

        private string RegisterComplexOperations()
        {
            StringBuilder sb = new StringBuilder();
            var operationConstruct = this.Def.OperationConstruct?.Construct.Trim();
            if (!string.IsNullOrWhiteSpace(operationConstruct))
            {
                sb.AppendLine();
                sb.AppendLine("new Graph<{0}Entity>.Construct({0}Operation.Create)".FormatWith(this.TypeName));
                sb.AppendLine("{");
                sb.AppendLine("    Construct = (args) => {\r\n" + operationConstruct + "\r\n}");
                sb.AppendLine("}.Register();");
            }

            var operationExecute = this.Def.OperationExecute?.Execute.Trim();
            var operationCanExecute = this.Def.OperationExecute?.CanExecute?.Trim();
            if (!string.IsNullOrWhiteSpace(operationExecute))
            {
                sb.AppendLine();
                sb.AppendLine("new Graph<{0}Entity>.Execute({0}Operation.Save)".FormatWith(this.TypeName));
                sb.AppendLine("{");

                if (!string.IsNullOrWhiteSpace(operationCanExecute))
                    sb.AppendLine($"    CanExecute = e => {operationCanExecute},");

                sb.AppendLine("    AllowsNew = true,");
                sb.AppendLine("    Lite = false,");
                sb.AppendLine("    Execute = (e, args) => {\r\n" + operationExecute + "\r\n}");
                sb.AppendLine("}.Register();");
            }

            var operationDelete = this.Def.OperationDelete?.Delete.Trim();
            var operationCanDelete = this.Def.OperationDelete?.CanDelete?.Trim();
            if (!string.IsNullOrWhiteSpace(operationDelete))
            {
                sb.AppendLine();
                sb.AppendLine("new Graph<{0}Entity>.Delete({0}Operation.Delete)".FormatWith(this.TypeName));
                sb.AppendLine("{");

                if (!string.IsNullOrWhiteSpace(operationCanDelete))
                    sb.AppendLine($"    CanDelete = e => {operationCanDelete},");

                sb.AppendLine("    Delete = (e, args) => {\r\n" + operationDelete + "\r\n}");
                sb.AppendLine("}.Register();");
            }

            return sb.ToString();
        }
    }

}
