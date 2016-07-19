using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Migrations
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
    public class CSharpMigrationEntity : Entity
    {
        [UniqueIndex]
        public string UniqueName { get; set; }

        public DateTime ExecutionDate { get; set; }

        static Expression<Func<CSharpMigrationEntity, string>> ToStringExpression = e => e.UniqueName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
