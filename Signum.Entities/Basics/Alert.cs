using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

namespace Signum.Entities.Basics
{
    public class CountAlerts
    {
        public int WarnedAlerts;
        public int CheckedAlerts;
        public int FutureAlerts;
    }

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

        public override string ToString()
        {
            return text.EtcLines(200);
        }
    }
}
