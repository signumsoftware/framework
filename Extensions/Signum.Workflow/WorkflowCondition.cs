using Signum.UserAssets;
using Signum.Eval;
using System.Xml.Linq;

namespace Signum.Workflow;

[EntityKind(EntityKind.Shared, EntityData.Master), PrimaryKey(typeof(Guid))]
public class WorkflowConditionEntity : Entity, IUserAssetEntity
{
    [UniqueIndex]
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    public TypeEntity MainEntityType { get; set; }

    [BindParent]
    public WorkflowConditionEval Eval { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("WorkflowCondition",
             new XAttribute("Guid", (Guid)Id),
             new XAttribute("Name", Name),
             new XAttribute("MainEntityType", MainEntityType.CleanName),
             new XElement("Eval",
                EvalXml.ToXml("Script", Eval)));
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        Name = element.Attribute("Name")!.Value;
        MainEntityType = ctx.GetType(element.Attribute("MainEntityType")!.Value);

        if (Eval == null)
            Eval = new WorkflowConditionEval();

        var xEval = element.Element("Eval")!;

        EvalXml.FromXml(xEval, "Script", Eval);
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

        var script = this.ScriptCS.Trim();
        script = script.Contains(';') ? script : ("return " + script + ";");
        var WorkflowEntityTypeName = parent.MainEntityType.ToType().FullName;

        return Compile(EvalLogic.GetCoreMetadataReferences()
            .Concat(EvalLogic.GetMetadataReferences()), EvalLogic.GetUsingNamespaces() +
                @"
                namespace Signum.Workflow
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
