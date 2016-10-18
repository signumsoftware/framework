using Newtonsoft.Json;
using Signum.Engine;
using Signum.Engine.Basics;
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

        public static string GetPropertyType(DynamicProperty property)
        {
            var generator = new DynamicTypeCodeGenerator(DynamicallyGeneratedEntitiesNamespace, null, null, new string[0]);

            return generator.GetPropertyType(property);
        }
    }

    public class DynamicTypeCodeGenerator
    {
        public string[] Usings { get; private set; }
        public string Namespace { get; private set; }
        public string TypeName { get; private set; }
        public DynamicTypeDefinition Def { get; private set; }

        public DynamicTypeCodeGenerator(string @namespace, string typeName, DynamicTypeDefinition def, string[] usings)
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
            int length = sb.Length;
            sb.Append(GetEntityCode());

            if (sb.Length == length)
                return null;

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
            sb.AppendLine($"public class {Def.TableName} : {GetEntityBaseClass(Def.BaseType)}");
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

        private string GetEntityBaseClass(DynamcBaseType baseType)
        {
            switch (baseType)
            {
                case DynamcBaseType.Entity: return "Entity";
                default: throw new NotImplementedException();
            }
        }

        protected virtual string GetToString()
        {
            if (Def.ToStringExpression == null)
                return null;
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"static Expression<Func<{TypeName}, string>> ToStringExpression = e => {Def.ToStringExpression};");
            sb.AppendLine("[ExpressionField]");
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

            WriteAttributeTag(sb, GetFieldAttributes(property));
            WriteAttributeTag(sb, GetPropertyAttributes(property));

            string inititalizer = property.IsMList ? $" = new {type}()": null;

            sb.AppendLine($"public {type} {property.Name} {{ get; set; }}{inititalizer}");

            return sb.ToString();
        }

        private IEnumerable<string> GetPropertyAttributes(DynamicProperty property)
        {
            return property.Validators.Select(v => GetValidatorAttribute(v));
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
}
