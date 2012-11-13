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
using System.Configuration;
using Signum.Engine;
using Signum.Web.Properties;
using Signum.Utilities.Reflection;
#endregion

namespace Signum.Web
{
    public class EntityLine : EntityBase
    {
        public bool Autocomplete { get; set; }

        public EntityLine(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
            Autocomplete = true;
            Navigate = true;
        }

        protected override void SetReadOnly()
        {
            Parent.ReadOnly = true;
            Find = false;
            Create = false;
            Remove = false;
            Autocomplete = false;
        }

        public override string ToJS()
        {
            return "$('#{0}').data('entityLine')".Formato(ControlID);
        }

        protected override string DefaultView()
        {
            return JsView(DefaultJsViewOptions()).ToJS();
        }

        public JsInstruction JsView(JsViewOptions viewOptions)
        {
            return new JsInstruction(() => "{0}.view({1})".Formato(
                    this.ToJS(),
                    viewOptions.TryCC(v => v.ToJS()) ?? ""));
        }

        protected override string DefaultCreate()
        {
            return JsCreate(DefaultJsViewOptions()).ToJS();
        }

        public JsInstruction JsCreate(JsViewOptions viewOptions)
        {
            return new JsInstruction(() => "{0}.create({1})".Formato(
                this.ToJS(), 
                viewOptions.TryCC(v => v.ToJS()) ?? ""));
        }

        protected override string DefaultFind()
        {
            return JsFind(DefaultJsfindOptions()).ToJS();
        }

        public JsInstruction JsFind(JsFindOptions findOptions)
        {
            return new JsInstruction(() => "{0}.find({1})".Formato(
                this.ToJS(), 
                findOptions.TryCC(v => v.ToJS()) ?? ""));
        }

        protected override string DefaultRemove()
        {
            return JsRemove().ToJS();
        }

        public JsInstruction JsRemove()
        {
            return new JsInstruction(() => "{0}.remove()".Formato(this.ToJS()));
        }
    }
}
