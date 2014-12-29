using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Signum.Utilities;

namespace Signum.Entities.Migrations
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class MigrationEntity : Entity
    {
        string versionNumber;
        public string VersionNumber
        {
            get { return versionNumber; }
            set { Set(ref versionNumber, value); }
        }

        DateTime executionDate;
        public DateTime ExecutionDate
        {
            get { return executionDate; }
            set { SetToStr(ref executionDate, value); }
        }
    }
}
