using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Help
{
    [Serializable, EntityKind(EntityKind.SharedPart, EntityData.Master)]
    public class OperationHelpEntity : Entity
    {
        [NotNullable]
        [NotNullValidator]
        public OperationSymbol Operation { get; set; }

        [NotNullable]
        [NotNullValidator]
        public CultureInfoEntity Culture { get; set; }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        [StringLengthValidator(AllowNulls = false, Min = 3, MultiLine = true)]
        public string Description { get; set; }

        static Expression<Func<OperationHelpEntity, string>> ToStringExpression = e => e.Operation.ToString();
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }


    [AutoInit]
    public static class OperationHelpOperation
    {
        public static ExecuteSymbol<OperationHelpEntity> Save;
    }
}
