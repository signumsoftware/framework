using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Linq.Expressions;

namespace Signum.Entities.Dynamic
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class DynamicMixinConnectionEntity : Entity
    {
        
        public Lite<TypeEntity> EntityType { get; set; }

        [StringLengthValidator(Max = 100)]
        public string MixinName { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => EntityType + " - " + MixinName);
    }

    [AutoInit]
    public static class DynamicMixinConnectionOperation
    {
        public static readonly ExecuteSymbol<DynamicMixinConnectionEntity> Save;
        public static readonly DeleteSymbol<DynamicMixinConnectionEntity> Delete;
    }
}
