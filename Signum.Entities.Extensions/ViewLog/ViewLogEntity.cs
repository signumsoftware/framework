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
        [NotNullable, ImplementedByAll]
        [NotNullValidator]
        public Lite<Entity> Target { get; set; }

        [NotNullable]
        [NotNullValidator]
        public Lite<IUserEntity> User { get; set; }

        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string ViewAction { get; set; }

        public DateTime StartDate { get; private set; } = TimeZoneManager.Now;

        public DateTime EndDate { get; set; }

        [SqlDbType(Size = int.MaxValue)]
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
}
