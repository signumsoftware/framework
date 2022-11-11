using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Entities.UserAssets;
using System.Xml.Linq;

namespace Signum.Entities.Workflow;

[EntityKind(EntityKind.Shared, EntityData.Master)]
public class WorkflowConditionEntity : Entity, IUserAssetEntity
{
    [UniqueIndex]
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    [UniqueIndex]
    public Guid Guid { get; set; } = Guid.NewGuid();

    public TypeEntity MainEntityType { get; set; }

    [BindParent]
    public WorkflowConditionEval Eval { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("WorkflowCondition",
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
            Eval = new WorkflowConditionEval();

        var xEval = element.Element("Eval")!;

        Eval.Script = xEval.Element("Script")!.Value;
    }
}

[AutoInit]
public static class WorkflowConditionOperation
{
    public static readonly ConstructSymbol<WorkflowConditionEntity>.From<WorkflowConditionEntity> Clone;
    public static readonly ExecuteSymbol<WorkflowConditionEntity> Save;
    public static readonly DeleteSymbol<WorkflowConditionEntity> Delete;
}

public class WorkflowConditionEval : EvalEmbedded<IWorkflowConditionEvaluator>
{
    protected override CompilationResult Compile()
    {
        var parent = this.GetParentEntity<WorkflowConditionEntity>();

        var script = this.Script.Trim();
        script = script.Contains(';') ? script : ("return " + script + ";");
        var WorkflowEntityTypeName = parent.MainEntityType.ToType().FullName;

        return Compile(DynamicCode.GetCoreMetadataReferences()
            .Concat(DynamicCode.GetMetadataReferences()), DynamicCode.GetUsingNamespaces() +
                @"
                namespace Signum.Entities.Workflow
                {
                    class MyWorkflowConditionEvaluator : IWorkflowConditionEvaluator
                    {
                        public bool EvaluateUntyped(ICaseMainEntity mainEntity, WorkflowTransitionContext ctx)
                        {
                            return this.Evaluate((" + WorkflowEntityTypeName + @")mainEntity, ctx);
                        }

                        bool Evaluate(" + WorkflowEntityTypeName + @" e, WorkflowTransitionContext ctx)
                        {
                            " + script + @"
                        }
                    }
                }");
    }
}

public interface IWorkflowConditionEvaluator
{
    bool EvaluateUntyped(ICaseMainEntity mainEntity, WorkflowTransitionContext ctx);
}
