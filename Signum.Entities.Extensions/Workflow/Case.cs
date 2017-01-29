using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), InTypeScript(Undefined = false)]
    public class CaseEntity : Entity
    {
        [NotNullable]
        [NotNullValidator]
        public WorkflowEntity Workflow { get; set; }

        public Lite<CaseEntity> ParentCase { get; set; }

        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Description { get; set; }

        [NotNullable, ImplementedByAll]
        [NotNullValidator]
        public ICaseMainEntity MainEntity { get; set; }

        public DateTime StartDate { get; set; } = TimeZoneManager.Now;
        public DateTime? FinishDate { get; set; }

        static Expression<Func<CaseEntity, string>> ToStringExpression = @this => @this.Description;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public interface ICaseMainEntity : IEntity
    {

    }
}
