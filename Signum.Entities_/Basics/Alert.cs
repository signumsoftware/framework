using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.Basics
{
    public interface IAlertDN : IIdentifiable
    { }

    public class AlertDN : IdentifiableEntity, IAlertDN
    {
        [ImplementedByAll]
        Lite<IdentifiableEntity> entity;
        public Lite<IdentifiableEntity> Entity
        {
            get { return entity; }
            set { Set(ref entity, value, () => Entity); }
        }

        DateTime? alertDate;
        public DateTime? AlertDate
        {
            get { return alertDate; }
            set { Set(ref alertDate, value, () => AlertDate); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(Min = 1)]
        public string Text
        {
            get { return text; }
            set { SetToStr(ref text, value, () => Text); }
        }

        DateTime? checkDate;
        public DateTime? CheckDate
        {
            get { return checkDate; }
            set { Set(ref checkDate, value, () => CheckDate); }
        }

        static Expression<Func<AlertDN, bool>> NotAttendedExpression =
            a => (a.AlertDate.HasValue && a.AlertDate <= DateTime.Now) && !a.CheckDate.HasValue;
        public bool NotAttended
        {
            get { return NotAttendedExpression.Invoke(this); }
        }

        static Expression<Func<AlertDN, bool>> AttendedExpression =
            a => a.CheckDate.HasValue;
        public bool Attended
        {
            get { return AttendedExpression.Invoke(this); }
        }

        static Expression<Func<AlertDN, bool>> FutureExpression =
            a => !a.CheckDate.HasValue && (!a.AlertDate.HasValue || a.AlertDate > DateTime.Now);
        public bool Future
        {
            get { return FutureExpression.Invoke(this); }
        }

        public override string ToString()
        {
            return text.EtcLines(200);
        }
    }

    public enum AlertQueries
    {
        NotAttended,
        Attended,
        Future
    }   
}
