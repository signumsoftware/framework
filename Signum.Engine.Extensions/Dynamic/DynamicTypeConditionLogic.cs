using Signum.Engine.Cache;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Signum.Engine.Dynamic
{
    public static class DynamicTypeConditionLogic
    {
        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<DynamicTypeConditionSymbolEntity>()
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                    });

                sb.Include<DynamicTypeConditionEntity>()
                    .WithSave(DynamicTypeConditionOperation.Save)
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.SymbolName,
                        e.EntityType,
                        e.Eval.Script,
                    });

                new Graph<DynamicTypeConditionEntity>.ConstructFrom<DynamicTypeConditionEntity>(DynamicTypeConditionOperation.Clone)
                {
                    Construct = (e, args) => new DynamicTypeConditionEntity() {
                        SymbolName = e.SymbolName,
                        EntityType = e.EntityType,
                        Eval = new DynamicTypeConditionEval() { Script = e.Eval.Script } ,
                    }
                }.Register();

                new Graph<DynamicTypeConditionSymbolEntity>.Execute(DynamicTypeConditionSymbolOperation.Save)
                {
                    CanBeModified = true,
                    CanBeNew = true,
                    Execute = (e, _) =>
                    {
                        if (!e.IsNew)
                        {
                            var old = e.ToLite().Retrieve();
                            if (old.Name != e.Name)
                            {
                                DynamicSqlMigrationLogic.AddDynamicRename(typeof(TypeConditionSymbol).Name,
                                    $"CodeGenTypeCondition.{old.Name}",
                                    $"CodeGenTypeCondition.{e.Name}");
                            }
                        }
                    }
                }.Register();

                DynamicLogic.GetCodeFiles += GetCodeFiles;
                DynamicLogic.OnWriteDynamicStarter += WriteDynamicStarter;
                DynamicCode.RegisteredDynamicTypes.Add(typeof(DynamicTypeConditionEntity));
                sb.Schema.Table<TypeEntity>().PreDeleteSqlSync += type => Administrator.UnsafeDeletePreCommand(Database.Query<DynamicTypeConditionEntity>().Where(dtc => dtc.EntityType == type));
                sb.AddUniqueIndex((DynamicTypeConditionEntity e) => new { e.SymbolName, e.EntityType });
            }
        }

        public static void WriteDynamicStarter(StringBuilder sb, int indent) {

            sb.AppendLine("CodeGenTypeConditionStarter.Start(sb);".Indent(indent));
        }

        public static List<CodeFile> GetCodeFiles()
        {
            CacheLogic.GloballyDisabled = true;
            try
            {
                using (ExecutionMode.Global())
                {
                    var result = new List<CodeFile>();
                    var typeConditions = !Administrator.ExistsTable<DynamicTypeConditionEntity>() ? new List<DynamicTypeConditionEntity>()  : Database.Query<DynamicTypeConditionEntity>().ToList();
                    var typeConditionSymbols = !Administrator.ExistsTable<DynamicTypeConditionSymbolEntity>() ? new List<DynamicTypeConditionSymbolEntity>() : Database.Query<DynamicTypeConditionSymbolEntity>().ToList();

                    var dtcg = new DynamicTypeConditionCodeGenerator(DynamicCode.CodeGenEntitiesNamespace, typeConditions, typeConditionSymbols, DynamicCode.Namespaces);

                    var content = dtcg.GetFileCode();
                    result.Add(new CodeFile
                    {
                        FileName = "CodeGenTypeCondition.cs",
                        FileContent = content,
                    });
                    return result;
                }
            }
            finally
            {
                CacheLogic.GloballyDisabled = false;
            }
        }
    }

    public class DynamicTypeConditionCodeGenerator
    {
        public HashSet<string> Usings { get; private set; }
        public string Namespace { get; private set; }
        public string TypeName { get; private set; }
        public List<DynamicTypeConditionSymbolEntity> TypeConditionSymbols { get; private set; }
        public List<DynamicTypeConditionEntity> TypeConditions { get; private set; }

        public DynamicTypeConditionCodeGenerator(string @namespace, List<DynamicTypeConditionEntity> types, List<DynamicTypeConditionSymbolEntity> symbols, HashSet<string> usings)
        {
            this.Usings = usings;
            this.Namespace = @namespace;
            this.TypeConditions = types;
            this.TypeConditionSymbols = symbols;
        }

        public string GetFileCode()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in this.Usings)
                sb.AppendLine("using {0};".FormatWith(item));

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

            sb.AppendLine($"public static class CodeGenTypeConditionStarter");
            sb.AppendLine("{");
            sb.AppendLine("    public static void Start(SchemaBuilder sb)");
            sb.AppendLine("    {");
            foreach (var item in this.TypeConditions)
            {
                sb.AppendLine($"TypeConditionLogic.Register<{item.EntityType.ClassName}>(CodeGenTypeCondition.{item.SymbolName.Name}, e => {item.Eval.Script});".Indent(8));
            }
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine("[AutoInit]");
            sb.AppendLine("public static class CodeGenTypeCondition");
            sb.AppendLine("{");
            foreach (var item in this.TypeConditionSymbols)
            {
                sb.AppendLine($@"    public static TypeConditionSymbol {item.Name} = new TypeConditionSymbol(typeof(CodeGenTypeCondition), ""{item.Name}"");");
            }
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
