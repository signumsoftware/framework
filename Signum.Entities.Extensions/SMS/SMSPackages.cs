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
    public class SMSSendPackageEntity : SMSPackageEntity
    {

    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class SMSUpdatePackageEntity : SMSPackageEntity
    {

    }

    [Serializable]
    public abstract class SMSPackageEntity : Entity, IProcessDataEntity
    {
        public SMSPackageEntity()
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

        static Expression<Func<SMSPackageEntity, string>> ToStringExpression = e => e.Name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
