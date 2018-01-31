using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Migrations
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
    public class SqlMigrationEntity : Entity
    {
        [UniqueIndex]
        public string VersionNumber { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 0, Max = 400)]
        public string Comment { get; set; }

        static Expression<Func<SqlMigrationEntity, string>> ToStringExpression = e => e.VersionNumber;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
