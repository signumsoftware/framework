using Signum.Authorization.Rules;
using Signum.Cache;
using Signum.Dynamic.SqlMigrations;
using Signum.Dynamic;
using Signum.Eval;

namespace Signum.Dynamic.Types;

public static class DynamicTypeConditionLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<DynamicTypeConditionSymbolEntity>()
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.Name,
            });

        sb.Include<DynamicTypeConditionEntity>()
            .WithUniqueIndex(e => new { e.SymbolName, e.EntityType })
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
            Construct = (e, args) => new DynamicTypeConditionEntity()
            {
                SymbolName = e.SymbolName,
                EntityType = e.EntityType,
                Eval = new DynamicTypeConditionEval() { Script = e.Eval.Script },
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
                    var old = e.ToLite().RetrieveAndRemember();
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
        EvalLogic.RegisteredDynamicTypes.Add(typeof(DynamicTypeConditionEntity));
        sb.Schema.EntityEvents<TypeEntity>().PreDeleteSqlSync += type => Administrator.UnsafeDeletePreCommand(Database.Query<DynamicTypeConditionEntity>().Where(dtc => dtc.EntityType.Is(type)));
    }

    public static void WriteDynamicStarter(StringBuilder sb, int indent)
    {

        sb.AppendLine("CodeGenTypeConditionStarter.Start(sb);".Indent(indent));
    }

    public static List<CodeFile> GetCodeFiles()
    {
        var cacheOldDisabled = CacheLogic.GloballyDisabled;
        CacheLogic.GloballyDisabled = true;
        try
        {
            using (ExecutionMode.Global())
            {
                var result = new List<CodeFile>();
                var typeConditions = !Administrator.ExistsTable<DynamicTypeConditionEntity>() ? new List<DynamicTypeConditionEntity>() : Database.Query<DynamicTypeConditionEntity>().ToList();
                var typeConditionSymbols = !Administrator.ExistsTable<DynamicTypeConditionSymbolEntity>() ? new List<DynamicTypeConditionSymbolEntity>() : Database.Query<DynamicTypeConditionSymbolEntity>().ToList();

                var dtcg = new DynamicTypeConditionCodeGenerator(DynamicLogic.CodeGenNamespace, typeConditions, typeConditionSymbols, EvalLogic.Namespaces);

                var content = dtcg.GetFileCode();
                result.Add(new CodeFile("CodeGenTypeCondition.cs", content));
                return result;
            }
        }
        finally
        {
            CacheLogic.GloballyDisabled = cacheOldDisabled;
        }
    }
}

public class DynamicTypeConditionCodeGenerator
{
    public HashSet<string> Usings { get; private set; }
    public string Namespace { get; private set; }
    public List<DynamicTypeConditionSymbolEntity> TypeConditionSymbols { get; private set; }
    public List<DynamicTypeConditionEntity> TypeConditions { get; private set; }

    public DynamicTypeConditionCodeGenerator(string @namespace, List<DynamicTypeConditionEntity> types, List<DynamicTypeConditionSymbolEntity> symbols, HashSet<string> usings)
    {
        Usings = usings;
        Namespace = @namespace;
        TypeConditions = types;
        TypeConditionSymbols = symbols;
    }

    public string GetFileCode()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var item in Usings)
            sb.AppendLine("using {0};".FormatWith(item));

        sb.AppendLine();
        sb.AppendLine("namespace " + Namespace);
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
        foreach (var item in TypeConditions)
        {
            sb.AppendLine($"TypeConditionLogic.Register<{item.EntityType.ClassName}>(CodeGenTypeCondition.{item.SymbolName.Name}, e => {item.Eval.Script});".Indent(8));
        }
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine("[AutoInit]");
        sb.AppendLine("public static class CodeGenTypeCondition");
        sb.AppendLine("{");
        foreach (var item in TypeConditionSymbols)
        {
            sb.AppendLine($@"    public static TypeConditionSymbol {item.Name} = new TypeConditionSymbol(typeof(CodeGenTypeCondition), ""{item.Name}"");");
        }
        sb.AppendLine("}");

        return sb.ToString();
    }
}
