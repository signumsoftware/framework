using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Entities.UserAssets;
using System.Xml.Linq;

namespace Signum.Entities.Workflow;

[EntityKind(EntityKind.Shared, EntityData.Master)]
public class WorkflowActionEntity : Entity, IUserAssetEntity
{
    [UniqueIndex]
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    [UniqueIndex]
    public Guid Guid { get; set; } = Guid.NewGuid();

    public TypeEntity MainEntityType { get; set; }

    [BindParent]
    public WorkflowActionEval Eval { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("WorkflowAction",
             new XAttribute("Guid", Guid),
             new XAttribute("Name", Name),
             new XAttribute("MainEntityType", MainEntityType.CleanName),
             new XElement("Eval",
                new XElement("Script", new XCData(Eval.Script))));
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        Name = element.Attribute("Name")!.Value;
        MainEntityType = ctx.GetType(element.Attribute("MainEntityType")!.Value);

        if (Eval == null)
            Eval = new WorkflowActionEval();

        var xEval = element.Element("Eval")!;

        Eval.Script = xEval.Element("Script")!.Value;
    }
}

[AutoInit]
public static class WorkflowActionOperation
{
    public static readonly ConstructSymbol<WorkflowActionEntity>.From<WorkflowActionEntity> Clone;
    public static readonly ExecuteSymbol<WorkflowActionEntity> Save;
    public static readonly DeleteSymbol<WorkflowActionEntity> Delete;
}

public class WorkflowActionEval : EvalEmbedded<IWorkflowActionExecutor>
{
    protected override CompilationResult Compile()
    {
        var parent = this.GetParentEntity<WorkflowActionEntity>();
        var script = this.Script.Trim();
        var WorkflowEntityTypeName = parent.MainEntityType.ToType().FullName;

        return Compile(DynamicCode.GetCoreMetadataReferences()
            .Concat(DynamicCode.GetMetadataReferences()), DynamicCode.GetUsingNamespaces() +
                @"
                namespace Signum.Entities.Workflow
                {
                    class MyWorkflowActionEvaluator : IWorkflowActionExecutor
                    {
                        public void ExecuteUntyped(ICaseMainEntity mainEntity, WorkflowTransitionContext ctx)
                        {
                            this.Execute((" + WorkflowEntityTypeName + @")mainEntity, ctx);
                        }

                        void Execute(" + WorkflowEntityTypeName + @" e, WorkflowTransitionContext ctx)
                        {
                            " + script + @"
                        }
                    }
                }");
    }
}

public interface IWorkflowActionExecutor
{
    void ExecuteUntyped(ICaseMainEntity mainEntity, WorkflowTransitionContext ctx);
}
