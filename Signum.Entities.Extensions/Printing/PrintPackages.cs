using System;
using Signum.Entities.Processes;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.Printing
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class PrintPackageEntity : Entity, IProcessDataEntity
    {
        [StringLengthValidator(AllowNulls = true, Max = 200)]
        public string Name { get; set; }

        static Expression<Func<PrintPackageEntity, string>> ToStringExpression = e => e.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
