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
using Newtonsoft.Json.Linq;
#endregion

namespace Signum.Web
{
    public class EntityLineDetail : EntityBase
    {
        public string DefaultDetailDiv { get; private set; }
        public string DetailDiv { get; set; }

        public EntityLineDetail(Type type, object untypedValue, Context parent, string prefix, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, prefix, propertyRoute)
        {
            DefaultDetailDiv = DetailDiv = this.Compose(EntityBaseKeys.Detail);
            View = false;
            LabelClass = "sf-label-detail-line";
        }

        protected override void SetReadOnly()
        {
            Parent.ReadOnly = true;
            Find = false;
            Create = false;
            Remove = false;
        }

        protected override JObject OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();
            if (DetailDiv.HasText())
                result.Add("detailDiv", DetailDiv);
            return result;
        }
    }
}
