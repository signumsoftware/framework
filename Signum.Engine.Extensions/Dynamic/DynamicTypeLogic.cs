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
                        def.TableName = null;
                        var result = new DynamicTypeEntity { TypeName = null };
                        result.SetDefinition(def);
                        return result;
                    },
                }.Register();

                new Execute(DynamicTypeOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { },
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

        public static string DynamicallyGeneratedEntitiesNamespace = "Signum.Entities.DynamicallyGenerated";

        public static List<string> Namespaces = Eval.BasicNamespaces;

        public static string GetPropertyType(DynamicProperty property)
        {
            var generator = new DynamicTypeCodeGenerator(DynamicallyGeneratedEntitiesNamespace, null, null, new List<string>());

            return generator.GetPropertyType(property);
        }


        public static List<CodeFile> GetCodeFiles()
        {
            CacheLogic.GloballyDisabled = true;
            var types = ExecutionMode.Global().Using(a => Database.Query<DynamicTypeEntity>().ToList());
            CacheLogic.GloballyDisabled = false;

            var entities =  types.Select(dt =>
            {
                var def = dt.GetDefinition();

                var dcg = new DynamicTypeCodeGenerator(DynamicallyGeneratedEntitiesNamespace, dt.TypeName, def, Namespaces);

                var content = dcg.GetFileCode();
                return new CodeFile
                {
                    FileName = dt.TypeName + ".cs",
                    FileContent = content
                };
            }).ToList();
             
            var ns = Namespaces.ToList();
            ns.Add("Signum.Engine.DynamicQuery");
            ns.Add("Signum.Engine.Operations");
            ns.Add("Signum.Engine.Maps");

            var logics = types.Select(dt =>
            {
                var def = dt.GetDefinition();

                var dlg = new DynamicTypeLogicGenerator(DynamicallyGeneratedEntitiesNamespace, dt.TypeName, def, ns);

                var content = dlg.GetFileCode();
                return new CodeFile
                {
                    FileName = dt.TypeName + "Logic.cs",
                    FileContent = content
                };
            }).ToList();

            var dscg = new DynamicStarterCodeGenerator(DynamicallyGeneratedEntitiesNamespace, types.Select(a => a.TypeName).ToList(), ns);

            var code = dscg.GetFileCode();

            var starter = new List<CodeFile>
            {
                new CodeFile
                {
                    FileName = "DynamicStarter.cs",
                    FileContent = code,
                }
            };

            return entities.Concat(logics).Concat(starter).ToList();
        }
    }

    public class DynamicStarterCodeGenerator
    {
        public List<string> Usings { get; private set; }
        public string Namespace { get; private set; }
        public List<string> TypeNames { get; private set; }

        public DynamicStarterCodeGenerator(string @namespace, List<string> typeNames, List<string> usings)
        {
            this.Usings = usings;
            this.Namespace = @namespace;
            this.TypeNames = typeNames;
        }

        public string GetFileCode()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in this.Usings)
                sb.AppendLine("using {0};".FormatWith(item));

            sb.AppendLine("[assembly: DefaultAssemblyCulture(\"en\")]");
            sb.AppendLine();
            sb.AppendLine("namespace " + this.Namespace);
            sb.AppendLine("{");
            sb.Append(GetStarterClassCode().Indent(4));
            sb.AppendLine("}");
            
            return sb.ToString();
        }

        public string GetStarterClassCode()
        {
            StringBuilder sb = new StringBuilder();
          
            sb.AppendLine($"public static class DynamicStarter");
            sb.AppendLine("{");
            sb.AppendLine("    public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)");
            sb.AppendLine("    {");
            foreach (var item in this.TypeNames)
            {
                sb.AppendLine($"        {item}Logic.Start(sb, dqm);");
            }
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
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
            if (!this.Def.RegisterSave && !this.Def.RegisterDelete)
                return null;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[AutoInit]"); //Only for ReflectionServer
            sb.AppendLine($"public static class {this.TypeName}Operation");
            sb.AppendLine("{");
            if (this.Def.RegisterSave)
                sb.AppendLine($"    public static readonly ExecuteSymbol<{this.TypeName}Entity> Save = OperationSymbol.Execute<{this.TypeName}Entity>(typeof({this.TypeName}Operation), \"Save\");");
            if (this.Def.RegisterDelete)
                sb.AppendLine($"    public static readonly DeleteSymbol<{this.TypeName}Entity> Delete = OperationSymbol.Delete<{this.TypeName}Entity>(typeof({this.TypeName}Operation), \"Delete\");");
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

            if (property.ColumnName != null)
            {
                atts.Add($"ColumnName({Literal(property.ColumnName)})");
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
            string result = SimplifyType(property.Type);

            var t = ResolveType(property.Type);
            
            if (property.IsNullable != IsNullable.No && t.IsValueType)
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

        public Type ResolveType(string typeName)
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
                return result;

            throw new InvalidOperationException($"Type '{typeName}' Not found");

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

            if (this.Def.RegisterSave)
                sb.AppendLine($"    .WithSave({this.TypeName}Operation.Save)");

            if (this.Def.RegisterDelete)
                sb.AppendLine($"    .WithDelete({this.TypeName}Operation.Delete)");


            var mcui = this.Def.MultiColumnUniqueIndex;
            if (mcui != null)
                sb.AppendLine($"    .WithUniqueIndex(e => new {{{ mcui.Fields.Select(f => "e." + f).Comma(", ")}}}{(mcui.Where.HasText() ? ", " + mcui.Where : "")})");

            if (this.Def.QueryFields.EmptyIfNull().Any())
                sb.AppendLine($"    .WithQuery(dqm, e => new {{ Entity = e, {this.Def.QueryFields.Select(f => "e." + f).Comma(",\r\n")} }})");

            sb.Insert(sb.Length - 2, ';');
            return sb.ToString();
        }
    }

}
