#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Collections;
using System.Linq.Expressions;
using System.Web.Mvc.Html;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using System.Configuration;
using System.Web.Routing;
#endregion

namespace Signum.Web
{
    public class EntityList : EntityListBase
    {
        public readonly RouteValueDictionary ListHtmlProps = new RouteValueDictionary();
        
        public EntityList(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
            Reorder = false;
        }

        public override string ToJS()
        {
            return "$('#{0}').data('SF-entityList')".Formato(ControlID);
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

        private JsInstruction JsCreate(JsViewOptions viewOptions)
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

        protected override string DefaultMoveUp()
        {
            return JsMoveUp().ToJS();
        }

        public JsInstruction JsMoveUp()
        {
            return new JsInstruction(() => "{0}.moveUp()".Formato(this.ToJS()));
        }

        protected override string DefaultMoveDown()
        {
            return JsMoveDown().ToJS();
        }

        public JsInstruction JsMoveDown()
        {
            return new JsInstruction(() => "{0}.moveDown()".Formato(this.ToJS()));
        }
    }
}
