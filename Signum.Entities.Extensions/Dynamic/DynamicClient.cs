using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Signum.Entities.Dynamic
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    [Mixin(typeof(DisabledMixin))]
    public class DynamicClientEntity : Entity
    {
        [UniqueIndex]
        [StringLengthValidator(Min = 3, Max = 100), IdentifierValidator(IdentifierType.PascalAscii)]
        public string Name { get; set; }
        
        [StringLengthValidator(MultiLine = true)]
        public string Code { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Name);
    }

    [AutoInit]
    public static class DynamicClientOperation
    {
        public static readonly ConstructSymbol<DynamicClientEntity>.From<DynamicClientEntity> Clone;
        public static readonly ExecuteSymbol<DynamicClientEntity> Save;
        public static readonly DeleteSymbol<DynamicClientEntity> Delete;
    }

}
