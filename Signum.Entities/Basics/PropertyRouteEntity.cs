using System;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.Basics
{
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false), InTypeScript(Undefined = false)]
    public class PropertyRouteEntity : Entity
    {
        [field: Ignore]
        PropertyRoute route;
        [HiddenProperty]
        public PropertyRoute Route
        {
            get { return route; }
            set { route = value; }
        }

        [StringLengthValidator(Min = 1, Max = 100)]
        public string Path { get; set; }

        [NotNullValidator]
        public TypeEntity RootType { get; set; }

        public static Func<PropertyRouteEntity, PropertyRoute> ToPropertyRouteFunc;
        public PropertyRoute ToPropertyRoute()
        {
            return ToPropertyRouteFunc(this);
        }

        static readonly Expression<Func<PropertyRouteEntity, string>> ToStringExpression = e => e.Path;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
