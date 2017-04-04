using Signum.Entities.Basics;
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
    public class RunProcessEntity : Entity
    {
        public string ProcessName { get; set; }

        public DateTime ExecutionDate { get; set; }

        public Lite<ExceptionEntity> Exception{ get; set; }

        static Expression<Func<RunProcessEntity, string>> ToStringExpression = e => e.ProcessName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
