using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Processes;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.SMS
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class SMSSendPackageDN : SMSPackageDN
    {

    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class SMSUpdatePackageDN : SMSPackageDN
    {

    }

    [Serializable]
    public abstract class SMSPackageDN : Entity, IProcessDataDN
    {
        public SMSPackageDN()
        {
            this.name = GetType().NiceName() + ": " + TimeZoneManager.Now.ToString();
        }

        [SqlDbType(Size = 200)]
        string name;
        [StringLengthValidator(AllowNulls = true, Max = 200)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        static Expression<Func<SMSPackageDN, string>> ToStringExpression = e => e.Name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
