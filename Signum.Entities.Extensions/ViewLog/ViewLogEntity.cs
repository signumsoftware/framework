using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.ViewLog
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class ViewLogEntity : Entity
    {
        [ImplementedByAll]
        [NotNullValidator]
        public Lite<Entity> Target { get; set; }

        [NotNullValidator]
        public Lite<IUserEntity> User { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string ViewAction { get; set; }

        public DateTime StartDate { get; private set; } = TimeZoneManager.Now;

        public DateTime EndDate { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 0, MultiLine = true)]
        public string Data { get; set; }

        static Expression<Func<ViewLogEntity, double>> DurationExpression =
           sl => (sl.EndDate - sl.StartDate).TotalMilliseconds;
        [ExpressionField, Unit("ms")]
        public double Duration
        {
            get { return DurationExpression.Evaluate(this); }
        }
    }

    public enum ViewLogMessage
    {
        ViewLogMyLast
    }
}
