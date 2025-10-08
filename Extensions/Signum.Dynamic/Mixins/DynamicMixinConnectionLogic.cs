using Signum.Cache;
using Signum.Dynamic;
using Signum.Eval;

namespace Signum.Dynamic.Mixins;

public static class DynamicMixinConnectionLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<DynamicMixinConnectionEntity>()
            .WithUniqueIndex(e => new { e.EntityType, e.MixinName })
            .WithSave(DynamicMixinConnectionOperation.Save)
            .WithDelete(DynamicMixinConnectionOperation.Delete)
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.EntityType,
                e.MixinName,
            });

        DynamicLogic.GetCodeFiles += GetCodeFiles;
        DynamicLogic.OnWriteDynamicStarter += WriteDynamicStarter;
        EvalLogic.RegisteredDynamicTypes.Add(typeof(DynamicMixinConnectionEntity));
        sb.Schema.EntityEvents<TypeEntity>().PreDeleteSqlSync += type => Administrator.UnsafeDeletePreCommand(Database.Query<DynamicMixinConnectionEntity>().Where(dm => dm.EntityType.Is(type)));
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
            var mixins = !Administrator.ExistsTable<DynamicMixinConnectionEntity>() ? new List<DynamicMixinConnectionEntity>() : ExecutionMode.Global().Using(a => Database.Query<DynamicMixinConnectionEntity>().ToList());
            var dlg = new DynamicMixinConnectionLogicGenerator(DynamicLogic.CodeGenNamespace, mixins, EvalLogic.Namespaces);
            var content = dlg.GetFileCode();
            result.Add(new CodeFile("CodeGenMixinLogic.cs", content));
        }
        finally
        {
            CacheLogic.GloballyDisabled = cacheOldDisabled;
        }

        return result;
    }
}

public class DynamicMixinConnectionLogicGenerator
{
    public HashSet<string> Usings { get; private set; }
    public string Namespace { get; private set; }
    public List<DynamicMixinConnectionEntity> Mixins { get; private set; }

    public DynamicMixinConnectionLogicGenerator(string @namespace, List<DynamicMixinConnectionEntity> mixins, HashSet<string> usings)
    {
        Usings = usings;
        Namespace = @namespace;
        Mixins = mixins;
    }

    public string GetFileCode()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var item in Usings)
            sb.AppendLine("using {0};".FormatWith(item));

        sb.AppendLine();
        sb.AppendLine($"namespace {Namespace}");
        sb.AppendLine($"{{");
        sb.AppendLine($"    public static class CodeGenMixinLogic");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        public static void Start()");
        sb.AppendLine($"        {{");

        if (Mixins != null && Mixins.Count > 0)
            Mixins.ForEach(m => sb.AppendLine($"MixinDeclarations.Register<{m.EntityType}Entity, {m.MixinName}Mixin>();".Indent(12)));

        sb.AppendLine($"        }}");
        sb.AppendLine($"    }}");
        sb.AppendLine($"}}");

        return sb.ToString();
    }
}

