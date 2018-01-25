using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Dynamic
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Transactional)]
    public class DynamicTypeConditionSymbolEntity : Entity
    {
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100), IdentifierValidator(IdentifierType.PascalAscii)]
        public string Name { get; set; }

        static Expression<Func<DynamicTypeConditionSymbolEntity, string>> ToStringExpression = @this => @this.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class DynamicTypeConditionSymbolOperation
    {
        public static readonly ExecuteSymbol<DynamicTypeConditionSymbolEntity> Save;
    }

}
