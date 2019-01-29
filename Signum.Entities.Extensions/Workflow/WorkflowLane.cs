using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class WorkflowLaneEntity : Entity, IWorkflowObjectEntity, IWithModel
    {
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100)]
        public string BpmnElementId { get; set; }

        [NotNullValidator, AvoidDump]
        public WorkflowXmlEmbedded Xml { get; set; }

        [NotNullValidator]
        public WorkflowPoolEntity Pool { get; set; }

        [ImplementedBy(typeof(UserEntity), typeof(RoleEntity))]
        [NotNullValidator, NoRepeatValidator]
        public MList<Lite<Entity>> Actors { get; set; } = new MList<Lite<Entity>>();

        [NotifyChildProperty]
        public WorkflowLaneActorsEval ActorsEval { get; set; }

        static Expression<Func<WorkflowLaneEntity, string>> ToStringExpression = @this => @this.Name ?? @this.BpmnElementId;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public ModelEntity GetModel()
        {
            var model = new WorkflowLaneModel()
            {
                MainEntityType = this.Pool.Workflow.MainEntityType
            };
            model.Actors.AssignMList(this.Actors);
            model.ActorsEval = this.ActorsEval;
            model.Name = this.Name;
            return model;
        }

        public void SetModel(ModelEntity model)
        {
            var wModel = (WorkflowLaneModel)model;
            this.Name = wModel.Name;
            this.ActorsEval = wModel.ActorsEval;
            this.Actors.AssignMList(wModel.Actors);
        }
    }

    [Serializable]
    public class WorkflowLaneActorsEval : EvalEmbedded<IWorkflowLaneActorsEvaluator>
    {
        protected override CompilationResult Compile()
        {
            var parent = (WorkflowLaneEntity)this.GetParentEntity();

            var script = this.Script.Trim();
            script = script.Contains(';') ? script : ("return " + script + ";");
            var WorkflowEntityTypeName = parent.Pool.Workflow.MainEntityType.ToType().FullName;

            return Compile(DynamicCode.GetCoreMetadataReferences()
                .Concat(DynamicCode.GetMetadataReferences()), DynamicCode.GetUsingNamespaces() +
                    @"
                    namespace Signum.Entities.Workflow
                    {
                        class MyWorkflowLaneActorEvaluator : IWorkflowLaneActorsEvaluator
                        {
                            public IEnumerable<Lite<Entity>> GetActors(ICaseMainEntity mainEntity, WorkflowTransitionContext ctx)
                            {
                                return this.Evaluate((" + WorkflowEntityTypeName + @")mainEntity, ctx);
                            }

                            IEnumerable<Lite<Entity>> Evaluate(" + WorkflowEntityTypeName + @" e, WorkflowTransitionContext ctx)
                            {
                                " + script + @"
                            }
                        }
                    }");
        }

        public WorkflowLaneActorsEval Clone()
        {
            return new WorkflowLaneActorsEval() { Script = this.Script };
        }
    }

    public interface IWorkflowLaneActorsEvaluator
    {
        IEnumerable<Lite<Entity>> GetActors(ICaseMainEntity mainEntity, WorkflowTransitionContext ctx);
    }

    [AutoInit]
    public static class WorkflowLaneOperation
    {
        public static readonly ExecuteSymbol<WorkflowLaneEntity> Save;
        public static readonly DeleteSymbol<WorkflowLaneEntity> Delete;
    }

    [Serializable]
    public class WorkflowLaneModel : ModelEntity
    {
        [NotNullValidator, InTypeScript(Undefined = false, Null = false)]
        public TypeEntity MainEntityType { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        [ImplementedBy(typeof(UserEntity), typeof(RoleEntity))]
        [NotNullValidator, NoRepeatValidator]
        public MList<Lite<Entity>> Actors { get; set; } = new MList<Lite<Entity>>();

        public WorkflowLaneActorsEval ActorsEval { get; set; }
    }
}
