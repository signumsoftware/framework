using Signum.Cache;
using Signum.Dynamic.Types;
using Signum.Dynamic;
using Signum.Eval;

namespace Signum.Dynamic.Expression;

public static class DynamicExpressionLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<DynamicExpressionEntity>()
            .WithUniqueIndex(a => new { a.FromType, a.Name })
            .WithSave(DynamicExpressionOperation.Save)
            .WithDelete(DynamicExpressionOperation.Delete)
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.Name,
                e.ReturnType,
                e.FromType,
            });

        new Graph<DynamicExpressionEntity>.ConstructFrom<DynamicExpressionEntity>(DynamicExpressionOperation.Clone)
        {
            Construct = (e, _) =>
            {
                return new DynamicExpressionEntity
                {
                    Name = e.Name + "_2",
                    ReturnType = e.ReturnType,
                    FromType = e.FromType,
                    Body = e.Body,
                };
            }
        }.Register();

        DynamicLogic.GetCodeFiles += GetCodeFiles;
        DynamicLogic.OnWriteDynamicStarter += WriteDynamicStarter;
        EvalLogic.RegisteredDynamicTypes.Add(typeof(DynamicExpressionEntity));

        DynamicTypeLogic.GetAlreadyTranslatedExpressions = () =>
        {
            var cacheOldDisabled = CacheLogic.GloballyDisabled;
            CacheLogic.GloballyDisabled = true;
            try
            {
                if (!Administrator.ExistsTable<DynamicExpressionEntity>())
                    return new Dictionary<string, Dictionary<string, string>>();

                using (ExecutionMode.Global())
                    return Database.Query<DynamicExpressionEntity>()
                    .Where(a => a.Translation == DynamicExpressionTranslation.TranslateExpressionName)
                    .AgGroupToDictionary(a => a.FromType, gr => gr.ToDictionary(a => a.Name, a => "CodeGenExpressionMessage." + a.Name));
            }
            finally
            {
                CacheLogic.GloballyDisabled = cacheOldDisabled;
            }
        };

        DynamicTypeLogic.GetFormattedExpressions = () =>
        {
            var cacheOldDisabled = CacheLogic.GloballyDisabled;
            CacheLogic.GloballyDisabled = true;
            try
            {
                if (!Administrator.ExistsTable<DynamicExpressionEntity>())
                    return new Dictionary<string, Dictionary<string, FormatUnit>>();

                using (ExecutionMode.Global())
                    return Database.Query<DynamicExpressionEntity>()
                    .Where(a => a.Format != null || a.Unit != null)
                    .AgGroupToDictionary(a => a.FromType!, gr => gr.ToDictionary(a => a.Name!, a => new FormatUnit(a.Format, a.Unit)));
            }
            finally
            {
                CacheLogic.GloballyDisabled = cacheOldDisabled;
            }
        };

        sb.Schema.EntityEvents<TypeEntity>().PreDeleteSqlSync += type => Administrator.UnsafeDeletePreCommand(Database.Query<DynamicExpressionEntity>().Where(de => de.FromType == ((TypeEntity)type).ClassName));
    }


    public static void WriteDynamicStarter(StringBuilder sb, int indent)
    {

        sb.AppendLine("CodeGenExpressionStarter.Start(sb);".Indent(indent));
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

                var expressions = !Administrator.ExistsTable<DynamicExpressionEntity>() ?
                    new List<DynamicExpressionEntity>() :
                    Database.Query<DynamicExpressionEntity>().ToList();

                var dtcg = new DynamicExpressionCodeGenerator(DynamicLogic.CodeGenNamespace, expressions, EvalLogic.Namespaces);

                var content = dtcg.GetFileCode();
                result.Add(new CodeFile("CodeGenExpressionStarter.cs", content));
                return result;
            }
        }
        finally
        {
            CacheLogic.GloballyDisabled = cacheOldDisabled;
        }
    }
}

public class DynamicExpressionCodeGenerator
{
    public HashSet<string> Usings { get; private set; }
    public string Namespace { get; private set; }
    public List<DynamicExpressionEntity> Expressions { get; private set; }

    public DynamicExpressionCodeGenerator(string @namespace, List<DynamicExpressionEntity> expressions, HashSet<string> usings)
    {
        Usings = usings;
        Namespace = @namespace;
        Expressions = expressions;
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
        var fieldNames = Expressions
            .GroupBy(a => a.Name)
            .SelectMany(gr => gr.Count() == 1 ?
            new[] { KeyValuePair.Create(gr.SingleEx().Name + "Expression", gr.SingleEx()) } :
            gr.Select(a => KeyValuePair.Create(a.Name + "_" + a.FromType.RemoveChars('<', '>', '.') + "Expression", a))
            ).ToDictionaryEx("DynamicExpressions");

        var namesToTranslate = Expressions.Where(a => a.Translation == DynamicExpressionTranslation.TranslateExpressionName).Select(a => a.Name).Distinct();

        if (namesToTranslate.Any())
        {
            sb.AppendLine($"public enum CodeGenExpressionMessage");
            sb.AppendLine("{");
            foreach (var item in namesToTranslate)
            {
                sb.AppendLine("   " + item + ",");
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }

        sb.AppendLine($"public static class CodeGenExpressionStarter");
        sb.AppendLine("{");

        foreach (var kvp in fieldNames)
        {
            var expressionName = $"{kvp.Value.Name}From{kvp.Value.FromType.BeforeLast("Entity")}Expression".FirstUpper();
            sb.AppendLine($"    static Expression<Func<{kvp.Value.FromType}, {kvp.Value.ReturnType}>> {expressionName} = ");
            sb.AppendLine($"        e => {kvp.Value.Body};");
            sb.AppendLine($"    [ExpressionField(\"{expressionName}\")]");
            sb.AppendLine($"    public static {kvp.Value.ReturnType} {kvp.Value.Name}(this {kvp.Value.FromType} e)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        return {expressionName}.Evaluate(e);");
            sb.AppendLine($"    }}");
            sb.AppendLine("");
        }

        sb.AppendLine("    public static void Start(SchemaBuilder sb)");
        sb.AppendLine("    {");

        foreach (var kvp in fieldNames)
        {
            var entity = kvp.Value;
            var varName = $"{entity.Name}{entity.FromType.BeforeLast("Entity")}".FirstLower();

            sb.AppendLine($"        var {varName} = QueryLogic.Expressions.Register(({entity.FromType} e) => e.{entity.Name}(){GetNiceNameCode(entity)});");
            if (entity.Format.HasText())
                sb.AppendLine($"        {varName}.ForceFormat = {CSharpRenderer.Value(entity.Format)};");
            if (entity.Unit.HasText())
                sb.AppendLine($"        {varName}.ForceUnit = {CSharpRenderer.Value(entity.Unit)};");
        }
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string? GetNiceNameCode(DynamicExpressionEntity value)
    {
        switch (value.Translation)
        {
            case DynamicExpressionTranslation.TranslateExpressionName:
                return $", () => CodeGenExpressionMessage.{value.Name}.NiceToString()";

            case DynamicExpressionTranslation.ReuseTranslationOfReturnType:
                if (value.ReturnType.StartsWith("IQueryable<") && value.ReturnType.EndsWith(">"))
                    return $", () => typeof({value.ReturnType.After("IQueryable<").BeforeLast(">")}).NicePluralName()";

                return $", () => typeof({value.ReturnType}).NiceName()";

            case DynamicExpressionTranslation.NoTranslation:
                return null;

            default:
                throw new InvalidOperationException("Unexpected translaltion");
        }
    }
}
