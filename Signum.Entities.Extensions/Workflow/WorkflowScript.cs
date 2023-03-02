using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Entities.UserAssets;
using System.Xml.Linq;

namespace Signum.Entities.Workflow;

[EntityKind(EntityKind.Shared, EntityData.Master)]
public class WorkflowScriptEntity : Entity, IUserAssetEntity
{
    [UniqueIndex]
    public Guid Guid { get; set; } = Guid.NewGuid();

    [UniqueIndex]
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }
    
    public TypeEntity MainEntityType { get; set; }

    [BindParent]
    public WorkflowScriptEval Eval { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("WorkflowScript",
             new XAttribute("Guid", Guid),
             new XAttribute("Name", Name),
             new XAttribute("MainEntityType", MainEntityType.CleanName),
             new XElement("Eval",
               new XElement("Script", new XCData(Eval.Script)),
               string.IsNullOrEmpty(Eval.CustomTypes) ? null! : new XElement("CustomTypes", new XCData(Eval.Script)))
             );
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        Name = element.Attribute("Name")!.Value;
        MainEntityType = ctx.GetType(element.Attribute("MainEntityType")!.Value);

        if (Eval == null)
            Eval = new WorkflowScriptEval();

        var xEval = element.Element("Eval")!;

        Eval.Script = xEval.Element("Script")!.Value;
        Eval.CustomTypes = xEval.Element("CustomTypes")?.Value;
    }
}

[AutoInit]
public static class WorkflowScriptOperation
{
    public static readonly ConstructSymbol<WorkflowScriptEntity>.From<WorkflowScriptEntity> Clone;
    public static readonly ExecuteSymbol<WorkflowScriptEntity> Save;
    public static readonly DeleteSymbol<WorkflowScriptEntity> Delete;
}


public class WorkflowScriptEval : EvalEmbedded<IWorkflowScriptExecutor>
{
    [StringLengthValidator(MultiLine = true)]
    public string? CustomTypes { get; set; }

    protected override CompilationResult Compile()
    {
        var parent = this.GetParentEntity<WorkflowScriptEntity>();

        var script = this.Script.Trim();
        var WorkflowEntityTypeName = parent.MainEntityType.ToType().FullName;

        return Compile(DynamicCode.GetCoreMetadataReferences()
            .Concat(DynamicCode.GetMetadataReferences()), DynamicCode.GetUsingNamespaces() +
                @"
                namespace Signum.Entities.Workflow
                {
                    class MyWorkflowScriptEvaluator : IWorkflowScriptExecutor
                    {
                        public void ExecuteUntyped(ICaseMainEntity mainEntity, WorkflowScriptContext ctx)
                        {
                            this.Execute((" + WorkflowEntityTypeName + @")mainEntity, ctx);
                        }

                        void Execute(" + WorkflowEntityTypeName + @" e, WorkflowScriptContext ctx)
                        {
                            " + script + @"
                        }
                    }

                    " + CustomTypes + @"
                }");
    }
}

public interface IWorkflowScriptExecutor
{
    void ExecuteUntyped(ICaseMainEntity mainEntity, WorkflowScriptContext ctx);
}

public class WorkflowScriptContext
{
    public CaseActivityEntity? CaseActivity { get; internal set; }
    public int RetryCount { get; internal set; }
}
