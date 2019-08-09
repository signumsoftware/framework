using System;
using Signum.Entities.Basics;
using System.Linq.Expressions;
using Signum.Utilities;

namespace Signum.Entities.ViewLog
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class ViewLogEntity : Entity
    {
        [ImplementedByAll]
        public Lite<Entity> Target { get; set; }
        
        public Lite<IUserEntity> User { get; set; }

        [StringLengthValidator(Min = 3, Max = 100)]
        public string ViewAction { get; set; }

        public DateTime StartDate { get; private set; } = TimeZoneManager.Now;

        public DateTime EndDate { get; set; }

        [StringLengthValidator(Min = 0, MultiLine = true)]
        public string? Data { get; set; }

        [AutoExpressionField, Unit("ms")]
        public double Duration => As.Expression(() => (EndDate - StartDate).TotalMilliseconds);
    }

    public enum ViewLogMessage
    {
        ViewLogMyLast
    }
}
