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
using Signum.Engine;
#endregion

namespace Signum.Web
{
    public static class EntityStripKeys
    {
        public const string ItemsContainer = "sfItemsContainer";
        public const string StripElement = "sfStripItem";
    }

    public class EntityStrip : EntityListBase
    {
        public int? MaxElements { get; set; }

        public bool Autocomplete { get; set; }
        public string AutocompleteUrl { get; set; }

        public bool Vertical { get; set; }

        public EntityStrip(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
            bool isEmbedded = type.ElementType().IsEmbeddedEntity();

            Find = false;
            Reorder = false;
            Create = isEmbedded;
            Navigate = !isEmbedded;
            View = isEmbedded;
            Autocomplete = !isEmbedded;
            Remove = true;
        }

        protected override void SetReadOnly()
        {
            base.SetReadOnly();
            Autocomplete = false;
        }

        protected override JsOptionsBuilder OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();
            result.Add("maxElements", MaxElements.TryToString());
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
