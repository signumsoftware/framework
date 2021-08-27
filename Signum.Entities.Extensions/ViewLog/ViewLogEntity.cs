using System;
using Signum.Entities.Basics;
using System.Linq.Expressions;
using Signum.Utilities;

namespace Signum.Entities.ViewLog
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class ViewLogEntity : Entity
    {
        public ViewLogEntity()
        {
            RebindEvents();
        }

        [ImplementedByAll]
        public Lite<Entity> Target { get; set; }
        
        public Lite<IUserEntity> User { get; set; }

        [StringLengthValidator(Min = 3, Max = 100)]
        public string ViewAction { get; set; }

        [Format("G")]
        public DateTime StartDate { get; private set; } = TimeZoneManager.Now;

        [Format("G")]
        public DateTime EndDate { get; set; }

        [NotifyChildProperty]
        public BigStringEmbedded Data { get; set; } = new BigStringEmbedded();

        [AutoExpressionField, Unit("ms")]
        public double Duration => As.Expression(() => (EndDate - StartDate).TotalMilliseconds);
    }

    public enum ViewLogMessage
    {
        ViewLogMyLast
    }
}
