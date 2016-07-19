using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Web.Mvc.Html;
using Signum.Entities;
using System.Reflection;
using Signum.Entities.Reflection;
using Signum.Engine;
using System.Configuration;
using System.Web.Routing;

namespace Signum.Web
{
    public static class EntityComboKeys
    {
        public const string Combo = "sfCombo";
    }

    public class EntityCombo : EntityBase
    {
        public readonly RouteValueDictionary ComboHtmlProperties = new RouteValueDictionary();
        
        public IEnumerable<Lite<IEntity>> Data { get; set; }
        public int Size { get; set; }

        public bool SortElements { get; set; }

        public EntityCombo(Type type, object untypedValue, Context parent, string prefix, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, prefix, propertyRoute)
        {
            SortElements = true;
            Size = 0;
            View = false;
            Create = false;
            Remove = false;
            Find = false;
        }

        protected override void SetReadOnly()
        {
            Parent.ReadOnly = true;
            Find = false;
            Create = false;
            Remove = false;
        }

    }
}
