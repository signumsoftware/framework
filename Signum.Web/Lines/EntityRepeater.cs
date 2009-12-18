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
        public string RemoveElementLinkText { get; set; }
        public string AddElementLinkText { get; set; }
        public int? MaxElements { get; set; }

        public EntityRepeater(string prefix) 
        {
            Prefix = prefix;
            RemoveElementLinkText = Resources.Remove;
            AddElementLinkText = Resources.New;
            Find = false;
        }

        public override string ToJS()
        {
            return "new ERep(" + this.OptionsJS() + ")";
        }

        public override string OptionsJS()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");

            sb.Append("prefix:'{0}'".Formato(Prefix));

            if (OnChangedTotal.HasText())
                sb.Append(",onEntityChanged:{0}".Formato(OnChangedTotal));

            if (MaxElements.HasValue)
                sb.Append(",maxElements:'{0}'".Formato(MaxElements.Value));

            sb.Append("}");
            return sb.ToString();
        }

        protected override string DefaultViewing()
        {
            return null;
        }

        protected override string DefaultCreating()
        {
            return EntityRepeater.JsCreating(this, DefaultJsViewOptions()).ToJS();
        }

        public static JsRenderer JsCreating(EntityRepeater erep, JsViewOptions viewOptions)
        {
            return new JsRenderer(() => "ERepOnCreating({0})".Formato(",".Combine(
                erep.ToJS(),
                viewOptions.TryCC(v => v.ToJS()))));
        }

        protected override string DefaultFinding()
        {
            return EntityRepeater.JsFinding(this, DefaultJsfindOptions(), DefaultJsViewOptions()).ToJS();
        }

        public static JsRenderer JsFinding(EntityRepeater erep, JsFindOptions findOptions, JsViewOptions viewOptions)
        {
            return new JsRenderer(() => "ERepOnFinding({0})".Formato(",".Combine(
                erep.ToJS(),
                findOptions.TryCC(f => f.ToJS())),
                viewOptions.TryCC(v => v.ToJS())));
        }

        protected override string DefaultRemoving()
        {
            return null;
        }
    }
}
