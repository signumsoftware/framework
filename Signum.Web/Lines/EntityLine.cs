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
        public bool Navigate { get; set; }
        
        public EntityLine(string prefix)
        {
            Prefix = prefix;
            Navigate = true;
            Autocomplete = true;        
        }

        public override void SetReadOnly()
        {
            ReadOnly = true;
            Find = false;
            Create = false;
            Remove = false;
            Autocomplete = false;
            Implementations = null;
        }

        public override string ToJS()
        {
            return "new ELine(" + this.OptionsJS() + ")";
        }

        protected override string DefaultViewing()
        {
            return EntityLine.JsViewing(this, DefaultJsViewOptions()).ToJS();
        }

        public static JsRenderer JsViewing(EntityLine eline, JsViewOptions viewOptions)
        {
            return new JsRenderer(() => "ELineOnViewing({0})".Formato(",".Combine(
                eline.ToJS(),
                viewOptions.TryCC(v => v.ToJS()))));
        }

        protected override string DefaultCreating()
        {
            return EntityLine.JsCreating(this, DefaultJsViewOptions()).ToJS();
        }

        public static JsRenderer JsCreating(EntityLine eline, JsViewOptions viewOptions)
        { 
            return new JsRenderer(() => "ELineOnCreating({0})".Formato(",".Combine(
                eline.ToJS(),
                viewOptions.TryCC(v => v.ToJS()))));
        }

        protected override string DefaultFinding()
        {
            return EntityLine.JsFinding(this, DefaultJsfindOptions()).ToJS();
        }

        public static JsRenderer JsFinding(EntityLine eline, JsFindOptions findOptions)
        {
            return new JsRenderer(() => "ELineOnFinding({0})".Formato(",".Combine(
                eline.ToJS(),
                findOptions.TryCC(v => v.ToJS()))));
        }

        protected override string DefaultRemoving()
        {
            return EntityLine.JsRemoving(this).ToJS();
        }

        public static JsRenderer JsRemoving(EntityLine eline)
        {
            return new JsRenderer(() => "ELineOnRemoving({0})".Formato(eline.ToJS()));
        }
    }
}
