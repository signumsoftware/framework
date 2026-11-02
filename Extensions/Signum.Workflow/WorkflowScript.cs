using Signum.UserAssets;
using Signum.Eval;
using System.Xml.Linq;

namespace Signum.Workflow;

[EntityKind(EntityKind.Shared, EntityData.Master), PrimaryKey(typeof(Guid))]
public class WorkflowScriptEntity : Entity, IUserAssetEntity
{
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
             new XAttribute("Guid", (Guid)Id),
             new XAttribute("Name", Name),
             new XAttribute("MainEntityType", MainEntityType.CleanName),
             new XElement("Eval",
               EvalXml.ToXml("Script", Eval),
               EvalXml.ToXml("CustomTypes", Eval.CustomTypesCS.DefaultToNull(), Eval.CustomTypesTS))
             );
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        Name = element.Attribute("Name")!.Value;
        MainEntityType = ctx.GetType(element.Attribute("MainEntityType")!.Value);

        if (Eval == null)
            Eval = new WorkflowScriptEval();

        var xEval = element.Element("Eval")!;

        EvalXml.FromXml(xEval, "Script", Eval);
        EvalXml.FromXml(xEval, "CustomTypes", cs => Eval.CustomTypesCS = cs, ts => Eval.CustomTypesTS = ts);
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
    public string? CustomTypesCS { get; set; }

    //The TypeScript twin, emitted by the Altea application sharing the database. Never compiled here.
    [StringLengthValidator(MultiLine = true)]
    public string? CustomTypesTS { get; set; }

    protected override CompilationResult Compile()
    {
        var parent = this.GetParentEntity<WorkflowScriptEntity>();

        var script = this.ScriptCS.Trim();
        var WorkflowEntityTypeName = parent.MainEntityType.ToType().FullName;

        return Compile(EvalLogic.GetCoreMetadataReferences()
            .Concat(EvalLogic.GetMetadataReferences()), EvalLogic.GetUsingNamespaces() +
                @"
                namespace Signum.Workflow
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

                    " + CustomTypesCS + @"
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
