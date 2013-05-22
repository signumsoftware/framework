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
        public IEnumerable<Lite<IIdentifiable>> Data { get; set; }
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

        protected override void SetReadOnly()
        {
            Parent.ReadOnly = true;
            Find = false;
            Create = false;
            Remove = false;
        }

        public override string ToJS()
        {
            return "$('#{0}').data('SF-entityCombo')".Formato(ControlID);
        }

        protected override string DefaultView()
        {
            return JsView(DefaultJsViewOptions()).ToJS();
        }

        public JsInstruction JsView(JsViewOptions viewOptions)
        {
            if (Navigate)
            {
                viewOptions.Navigate = true;
                viewOptions.ControllerUrl = Navigator.NavigateRoute(Type.CleanType(), null);
            }
            
            return new JsInstruction(() => "{0}.view({1})".Formato(
                    this.ToJS(),
                    viewOptions.TryCC(v => v.ToJS()) ?? ""));
        }

        protected override string DefaultCreate()
        {
            return JsCreate(DefaultJsViewOptions()).ToJS();
        }

        private JsInstruction JsCreate(JsViewOptions viewOptions)
        {
            return new JsInstruction(() => "{0}.create({1})".Formato(
                this.ToJS(),
                viewOptions.TryCC(v => v.ToJS()) ?? ""));
        }

        protected override string DefaultFind()
        {
            return null;
        }

        protected override string DefaultRemove()
        {
            return null;
        }
    }
}
