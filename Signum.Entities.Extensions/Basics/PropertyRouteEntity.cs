using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Reflection;
using System.Linq.Expressions;

namespace Signum.Entities.Basics
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false)]
    public class PropertyRouteEntity : Entity
    {
        public PropertyRouteEntity() { }

        [field: Ignore]
        PropertyRoute route;
        public PropertyRoute Route
        {
            get { return route; }
            set { route = value; }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string path;
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100)]
        public string Path
        {
            get { return path; }
            set { SetToStr(ref path, value); }
        }

        TypeEntity rootType;
        [NotNullValidator]
        public TypeEntity RootType
        {
            get { return rootType; }
            set { Set(ref rootType, value); }
        }

        static readonly Expression<Func<PropertyRouteEntity, string>> ToStringExpression = e => e.path;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
