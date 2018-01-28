using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Basics
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false), InTypeScript(Undefined = false)]
    public class PropertyRouteEntity : Entity
    {
        public PropertyRouteEntity() { }

        [field: Ignore]
        PropertyRoute route;
        [HiddenProperty]
        public PropertyRoute Route
        {
            get { return route; }
            set { route = value; }
        }

        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100)]
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
