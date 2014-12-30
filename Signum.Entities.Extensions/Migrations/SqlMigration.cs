using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Signum.Utilities;

namespace Signum.Entities.Migrations
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
    public class SqlMigrationEntity : Entity
    {
        [UniqueIndex]
        string versionNumber;
        public string VersionNumber
        {
            get { return versionNumber; }
            set { Set(ref versionNumber, value); }
        }

        static Expression<Func<SqlMigrationEntity, string>> ToStringExpression = e => e.VersionNumber;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
