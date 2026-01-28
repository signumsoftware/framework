using Signum.Cache;
using Signum.Dynamic;
using Signum.Eval;

namespace Signum.Dynamic.Controllers;

public static class DynamicApiLogic
{
    public static bool IsStarted = false;
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<DynamicApiEntity>()
            .WithSave(DynamicApiOperation.Save)
            .WithDelete(DynamicApiOperation.Delete)
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.Name,
                Script = e.Eval.Script.Etc(50),
            });

        new Graph<DynamicApiEntity>.ConstructFrom<DynamicApiEntity>(DynamicApiOperation.Clone)
        {
            Construct = (e, _) =>
            {
                return new DynamicApiEntity
                {
                    Name = e.Name + "_2",
                    Eval = new DynamicApiEval() { Script = e.Eval.Script },
                };
            }
        }.Register();

        EvalLogic.RegisteredDynamicTypes.Add(typeof(DynamicApiEntity));
        IsStarted = true;
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

                var controllers = !Administrator.ExistsTable<DynamicApiEntity>() ?
                    new List<DynamicApiEntity>() :
                    Database.Query<DynamicApiEntity>()
                        .Where(a => a.Mixin<DisabledMixin>().IsDisabled == false)
                        .ToList();

                var dtcg = new DynamicApiCodeGenerator(DynamicLogic.CodeGenNamespace, controllers, EvalLogic.GetNamespaces().ToHashSet());

                var content = dtcg.GetFileCode();
                result.Add(new CodeFile("CodeGenController.cs", content));
                return result;
            }
        }
        finally
        {
            CacheLogic.GloballyDisabled = cacheOldDisabled;
        }
    }
}

public class DynamicApiCodeGenerator
{
    public HashSet<string> Usings { get; private set; }
    public string Namespace { get; private set; }
    public List<DynamicApiEntity> DynamicApis { get; private set; }

    public DynamicApiCodeGenerator(string @namespace, List<DynamicApiEntity> dynamicApis, HashSet<string> usings)
    {
        Usings = usings;
        Namespace = @namespace;
        DynamicApis = dynamicApis;
    }

    public string GetFileCode()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var item in Usings)
            sb.AppendLine("using {0};".FormatWith(item));

        sb.AppendLine();
        sb.AppendLine("namespace " + Namespace);
        sb.AppendLine("{");
        sb.Append(GetControllerClassCode().Indent(4));
        sb.AppendLine("}");

        return sb.ToString();
    }

    public string GetControllerClassCode()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"public class CodeGenController: ControllerBase");
        sb.AppendLine("{");

        foreach (var c in DynamicApis)
        {
            sb.AppendLine();
            sb.AppendLine(c.Eval.Script.Indent(4));
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }
}
