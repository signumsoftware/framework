using Signum.Utilities;
using System;
using System.Linq.Expressions;

namespace Signum.Entities.Dynamic
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Transactional)]
    public class DynamicTypeConditionSymbolEntity : Entity
    {
        [StringLengthValidator(Min = 1, Max = 100), IdentifierValidator(IdentifierType.PascalAscii)]
        public string Name { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Name);
    }

    [AutoInit]
    public static class DynamicTypeConditionSymbolOperation
    {
        public static readonly ExecuteSymbol<DynamicTypeConditionSymbolEntity> Save;
    }

}
