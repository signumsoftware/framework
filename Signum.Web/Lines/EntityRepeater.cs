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
using Signum.Web.Properties;
using Signum.Engine;
#endregion

namespace Signum.Web
{
    public static class EntityRepeaterKeys
    {
        public const string ItemsContainer = "sfItemsContainer";
        public const string RepeaterElement = "sfRepeaterItem";
    }

    public class EntityRepeater : EntityListBase
    {
        public string AddElementLinkText { get; set; }
        public string RemoveElementLinkText{get;set;}

        public int? MaxElements { get; set; }

        public EntityRepeater(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
            RemoveElementLinkText = EntityControlMessage.Remove.NiceToString();
            AddElementLinkText = LiteMessage.New.NiceToString();
            Find = false;
            LabelClass = "sf-label-repeater-line";
            Reorder = false;
        }

        public override string ToJS()
        {
            return "$('#{0}').data('entityRepeater')".Formato(ControlID);
        }

        protected override JsOptionsBuilder OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();
            result.Add("maxElements", MaxElements.TryToString());
            if (RemoveElementLinkText.HasText())
                result.Add("removeItemLinkText", RemoveElementLinkText.SingleQuote());
            return result;
        }

        protected override string DefaultView()
        {
            return null;
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
            return JsFind(DefaultJsfindOptions(), DefaultJsViewOptions()).ToJS();
        }

        public JsInstruction JsFind(JsFindOptions findOptions, JsViewOptions viewOptions)
        {
            string findParams = ",".Combine(
                findOptions.TryCC(v => v.ToJS()),
                viewOptions.TryCC(v => v.ToJS()));

            return new JsInstruction(() => "{0}.find({1})".Formato(this.ToJS(), findParams));
        }

        protected override string DefaultRemove()
        {
            return null;
        }

        protected override string DefaultMoveUp()
        {
            return null;
        }

        protected override string DefaultMoveDown()
        {
            return null;
        }
    }
}
