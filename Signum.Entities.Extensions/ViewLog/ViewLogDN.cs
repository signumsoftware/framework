using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using System.Linq.Expressions;
using Signum.Utilities;

namespace Signum.Entities.ViewLog
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class ViewLogDN : Entity
    {
        [NotNullable, ImplementedByAll]
        Lite<IdentifiableEntity> target;
        [NotNullValidator]
        public Lite<IdentifiableEntity> Target
        {
            get { return target; }
            set { Set(ref target, value); }
        }

        [NotNullable]
        Lite<IUserDN> user;
        [NotNullValidator]
        public Lite<IUserDN> User
        {
            get { return user; }
            set { Set(ref user, value); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string viewAction;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string ViewAction
        {
            get { return viewAction; }
            set { Set(ref viewAction, value); }
        }

        DateTime startDate = TimeZoneManager.Now;
        public DateTime StartDate
        {
            get { return startDate; }
            private set { Set(ref startDate, value); }
        }

        DateTime endDate;
        public DateTime EndDate
        {
            get { return endDate; }
            set { Set(ref endDate, value); }
        }

        static Expression<Func<ViewLogDN, int>> DurationExpression =
           sl => (sl.EndDate - sl.StartDate).Milliseconds;
        [Unit("ms")]
        public int Duration
        {
            get { return DurationExpression.Evaluate(this); }
        }
    }
}
