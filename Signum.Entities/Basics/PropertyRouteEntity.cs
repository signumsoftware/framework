using System;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.Basics
{
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false), InTypeScript(Undefined = false)]
    public class PropertyRouteEntity : Entity
    {
        [StringLengthValidator(Min = 1, Max = 100)]
        public string Path { get; set; }

        public TypeEntity RootType { get; set; }

        public static Func<PropertyRouteEntity, PropertyRoute> ToPropertyRouteFunc;
        public PropertyRoute ToPropertyRoute()
        {
            return ToPropertyRouteFunc(this);
        }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => this.Path);
    }
}
