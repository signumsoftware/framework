using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Entities.UserAssets;
using System.Xml.Linq;

namespace Signum.Entities.Workflow;

[EntityKind(EntityKind.Shared, EntityData.Master)]
public class WorkflowTimerConditionEntity : Entity, IUserAssetEntity
{
    [UniqueIndex]
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    [UniqueIndex]
    public Guid Guid { get; set; } = Guid.NewGuid();

    public TypeEntity MainEntityType { get; set; }

    [BindParent]
    public WorkflowTimerConditionEval Eval { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("WorkflowTimerCondition",
             new XAttribute("Guid", Guid),
             new XAttribute("Name", Name),
             new XAttribute("MainEntityType", MainEntityType.CleanName),
             new XElement("Eval",
               new XElement("Script", new XCData(Eval.Script))
             )
        );
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        Name = element.Attribute("Name")!.Value;
        MainEntityType = ctx.GetType(element.Attribute("MainEntityType")!.Value);

        if (Eval == null)
            Eval = new WorkflowTimerConditionEval();

        var xEval = element.Element("Eval")!;

        Eval.Script = xEval.Element("Script")!.Value;
    }

   
}

[AutoInit]
public static class WorkflowTimerConditionOperation
{
    public static readonly ConstructSymbol<WorkflowTimerConditionEntity>.From<WorkflowTimerConditionEntity> Clone;
    public static readonly ExecuteSymbol<WorkflowTimerConditionEntity> Save;
    public static readonly DeleteSymbol<WorkflowTimerConditionEntity> Delete;
}

public class WorkflowTimerConditionEval : EvalEmbedded<IWorkflowTimerConditionEvaluator>
{
    protected override CompilationResult Compile()
    {
        var parent = this.GetParentEntity<WorkflowTimerConditionEntity>();

        var script = this.Script.Trim();
        script = script.Contains(';') ? script : ("return " + script + ";");
        var WorkflowEntityTypeName = parent.MainEntityType.ToType().FullName;

        return Compile(DynamicCode.GetCoreMetadataReferences()
            .Concat(DynamicCode.GetMetadataReferences()), DynamicCode.GetUsingNamespaces() +
                @"
                namespace Signum.Entities.Workflow
                {
                    class MyWorkflowTimerConditionEvaluator : IWorkflowTimerConditionEvaluator
                    {

                        public bool EvaluateUntyped(CaseActivityEntity ca, DateTime now)
                        {
                            return this.Evaluate(ca, (" + WorkflowEntityTypeName + @")ca.Case.MainEntity, now);
                        }

                        bool Evaluate(CaseActivityEntity ca, " + WorkflowEntityTypeName + @" e, DateTime now)
                        {
                            " + script + @"
                        }
                    }
                }");
    }
}

public interface IWorkflowTimerConditionEvaluator
{
    bool EvaluateUntyped(CaseActivityEntity ca, DateTime now);
}


