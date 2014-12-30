using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Migrations
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
    public class CSharpMigrationEntity : Entity
    {
        [UniqueIndex]
        string uniqueName;
        public string UniqueName
        {
            get { return uniqueName; }
            set { Set(ref uniqueName, value); }
        }

        DateTime executionDate;
        public DateTime ExecutionDate
        {
            get { return executionDate; }
            set { SetToStr(ref executionDate, value); }
        }

        static Expression<Func<CSharpMigrationEntity, string>> ToStringExpression = e => e.UniqueName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
