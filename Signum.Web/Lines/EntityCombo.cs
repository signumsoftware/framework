#region usings
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
using Signum.Web.Properties;
using System.Web.Routing;
#endregion

namespace Signum.Web
{
    public static class EntityComboKeys
    {
        public const string Combo = "sfCombo";
    }

    public class EntityCombo : EntityBase
    {
        public readonly RouteValueDictionary ComboHtmlProperties = new RouteValueDictionary();
        
        public bool Preload { get; set; }
        public List<Lite> Data { get; set; }
        public int Size { get; set; }

        public EntityCombo(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
            Size = 0;
            Preload = true;
            View = false;
            Create = false;
            Remove = false;
            Find = false;
        }

        public override void SetReadOnly()
        {
            Parent.ReadOnly = true;
            ReadOnly = true;
            Find = false;
            Create = false;
            Remove = false;
            Implementations = null;
        }

        public override string ToJS()
        {
            return "new ECombo(" + this.OptionsJS() + ")";
        }

        protected override string DefaultViewing()
        {
            return EntityCombo.JsViewing(this, DefaultJsViewOptions()).ToJS();
        }

        public static JsRenderer JsViewing(EntityCombo ecombo, JsViewOptions viewOptions)
        {
            return new JsRenderer(() => "EComboOnViewing({0})".Formato(",".Combine(
                ecombo.ToJS(),
                viewOptions.TryCC(v => v.ToJS()))));
        }

        protected override string DefaultCreating()
        {
            return EntityCombo.JsCreating(this, DefaultJsViewOptions()).ToJS();
        }

        private static JsRenderer JsCreating(EntityCombo ecombo, JsViewOptions viewOptions)
        {
            return new JsRenderer(() => "EComboOnCreating({0})".Formato(",".Combine(
                ecombo.ToJS(),
                viewOptions.TryCC(v => v.ToJS()))));
        }

        protected override string DefaultFinding()
        {
            return null;
        }

        protected override string DefaultRemoving()
        {
            return null;
        }
    }
}
