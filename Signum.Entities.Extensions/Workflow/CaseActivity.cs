using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), InTypeScript(Undefined = false)]
    public class CaseActivityEntity : Entity
    {
        [NotNullable]
        [NotNullValidator]
        public CaseEntity Case { get; set; }
        
        public WorkflowActivityEntity WorkflowActivity { get; set; }

        [NotNullable, SqlDbType(Size = 255)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 255)]
        public string OriginalWorkflowActivityName { get; set; }

        public DateTime StartDate { get; set; } = TimeZoneManager.Now;
        public DateTime? DoneDate { get; set; }
        public Lite<UserEntity> DoneBy { get; set; }

        static Expression<Func<CaseActivityEntity, string>> ToStringExpression = @this => @this.WorkflowActivity + " " + @this.DoneBy;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }


    [AutoInit]
    public static class CaseActivityOperation
    {
        public static readonly ConstructSymbol<CaseActivityEntity>.From<WorkflowEntity> Create;
        public static readonly ExecuteSymbol<CaseActivityEntity> Register;
        public static readonly DeleteSymbol<CaseActivityEntity> Delete;
        public static readonly ExecuteSymbol<CaseActivityEntity> Next;
        public static readonly ExecuteSymbol<CaseActivityEntity> Approve;
        public static readonly ExecuteSymbol<CaseActivityEntity> Decline;
    }

    public enum CaseActivityMessage
    {
        [Description("Only for {0} activities")]
        OnlyFor0Activites,
        AlreadyDone,
        ActivityAlreadyRegistered,
        CaseContainsOtherActivities,
        NoNextConnectionThatSatisfiesTheConditionsFound
    }


    public enum CaseActivityQuery
    {
        Inbox
    }
}
