using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Entities.Scheduler;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.ComponentModel;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.String, EntityData.Master)]
    public class WorkflowEventEntity : Entity, IWorkflowNodeEntity, IWithModel
    {
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string Name { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100)]
        public string BpmnElementId { get; set; }

        [NotNullValidator]
        public WorkflowLaneEntity Lane { get; set; }
        
        public WorkflowEventType Type { get; set; }

        public WorkflowTimerEmbedded Timer { get; set; }

        [NotNullValidator]
        public WorkflowXmlEmbedded Xml { get; set; }

        static Expression<Func<WorkflowEventEntity, string>> ToStringExpression = @this => @this.Name ?? @this.BpmnElementId;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public ModelEntity GetModel()
        {
            var model = new WorkflowEventModel()
            {
                MainEntityType = this.Lane.Pool.Workflow.MainEntityType,
                Name = this.Name,
                Type = this.Type,
                Task = WorkflowEventTaskModel.GetModel(this),
                Timer = this.Timer,
            };
            return model;
        }

        public void SetModel(ModelEntity model)
        {
            var wModel = (WorkflowEventModel)model;
            this.Name = wModel.Name;
            this.Type = wModel.Type;
            this.Timer = wModel.Timer;
            //WorkflowEventTaskModel.ApplyModel(this, wModel.Task);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if(pi.Name == nameof(Timer))
            {
                if (Timer == null && this.Type.IsTimer())
                    return ValidationMessage._0IsMandatoryWhen1IsSetTo2.NiceToString(pi.NiceName(), NicePropertyName(() => Type), Type.NiceToString());
                
                if (Timer != null && !this.Type.IsTimer())
                    return ValidationMessage._0ShouldBeNullWhen1IsNotSetTo2.NiceToString(pi.NiceName(), NicePropertyName(() => Type), Type.NiceToString());
            }

            return base.PropertyValidation(pi);
        }
    }

    [Serializable]
    public class WorkflowTimerEmbedded : EmbeddedEntity
    {
        public TimeSpanEmbedded Duration { get; set; }

        public Lite<WorkflowTimerConditionEntity> Condition { get; set; }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(Duration) && Duration == null && Condition == null)
            {
                return ValidationMessage._0IsMandatoryWhen1IsNotSet.NiceToString(pi.NiceName(), NicePropertyName(() => Condition));
            }
            return base.PropertyValidation(pi);
        }
    }

    public enum WorkflowEventType
    {
        Start,
        ScheduledStart,
        Finish,
        BoundaryForkTimer,
        BoundaryInterruptingTimer,
        IntermediateTimer,
    }


    public static class WorkflowEventTypeExtension
    {
        public static bool IsStart(this WorkflowEventType type) =>
            type == WorkflowEventType.Start ||
            type == WorkflowEventType.ScheduledStart;

        public static bool IsScheduledStart(this WorkflowEventType type) =>
            type == WorkflowEventType.ScheduledStart;

        public static bool IsFinish(this WorkflowEventType type) =>
            type == WorkflowEventType.Finish;

        public static bool IsTimer(this WorkflowEventType type) =>
            type == WorkflowEventType.BoundaryForkTimer ||
            type == WorkflowEventType.BoundaryInterruptingTimer ||
            type == WorkflowEventType.IntermediateTimer;
    }

    [AutoInit]
    public static class WorkflowEventOperation
    {
        public static readonly ExecuteSymbol<WorkflowEventEntity> Save;
        public static readonly DeleteSymbol<WorkflowEventEntity> Delete;
    }

    [Serializable]
    public class WorkflowEventModel : ModelEntity
    {
        [NotNullValidator, InTypeScript(Undefined = false, Null = false)]
        public TypeEntity MainEntityType { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        public WorkflowEventType Type { get; set; }
        
        public WorkflowEventTaskModel Task { get; set; }

        public WorkflowTimerEmbedded Timer { get; set; }
    }
}
