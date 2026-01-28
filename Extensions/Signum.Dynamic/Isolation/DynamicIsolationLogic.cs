using Signum.Cache;
using Signum.Dynamic.Types;
using Signum.Dynamic;
using Signum.Eval;
using Signum.Isolation;

namespace Signum.Dynamic.Isolation;

public static class DynamicIsolationLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        DynamicLogic.GetCodeFiles += GetCodeFiles;
        DynamicLogic.OnWriteDynamicStarter += WriteDynamicStarter;
    }

    public static void WriteDynamicStarter(StringBuilder sb, int indent)
    {
        // Nothing
    }

    public static List<CodeFile> GetCodeFiles()
    {
        var result = new List<CodeFile>();

        var cacheOldDisabled = CacheLogic.GloballyDisabled;
        CacheLogic.GloballyDisabled = true;
        try
        {
            var entities = !Administrator.ExistsTable<DynamicTypeEntity>() ? new List<DynamicTypeEntity>() :
                ExecutionMode.Global().Using(a => Database.Query<DynamicTypeEntity>().Where(a => a.BaseType == DynamicBaseType.Entity).ToList());
            var dlg = new DynamicIsolationLogicGenerator(DynamicLogic.CodeGenNamespace, entities, EvalLogic.Namespaces);
            var content = dlg.GetFileCode();
            result.Add(new CodeFile("CodeGenIsolationLogic.cs", content));
        }
        finally
        {
            CacheLogic.GloballyDisabled = cacheOldDisabled;
        }

        return result;
    }
}

public class DynamicIsolationLogicGenerator
{
    public HashSet<string> Usings { get; private set; }
    public string Namespace { get; private set; }
    public List<DynamicTypeEntity> Entities { get; private set; }

    public DynamicIsolationLogicGenerator(string @namespace, List<DynamicTypeEntity> entities, HashSet<string> usings)
    {
        Usings = usings;
        Namespace = @namespace;
        Entities = entities;
    }

    public string GetFileCode()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var item in Usings)
            sb.AppendLine("using {0};".FormatWith(item));

        sb.AppendLine();
        sb.AppendLine($"namespace {Namespace}");
        sb.AppendLine($"{{");
        sb.AppendLine($"    public static class CodeGenIsolationLogic");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        public static void Start()");
        sb.AppendLine($"        {{");

        if (Entities != null && Entities.Count > 0)
            Entities.ForEach(m => sb.AppendLine($"IsolationLogic.Register<{m.TypeName}Entity>(IsolationStrategy.{m.TryMixin<DynamicIsolationMixin>()?.IsolationStrategy ?? IsolationStrategy.None});".Indent(12)));

        sb.AppendLine($"        }}");
        sb.AppendLine($"    }}");
        sb.AppendLine($"}}");

        return sb.ToString();
    }
}

