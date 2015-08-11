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
using Signum.Utilities.Reflection;
using Newtonsoft.Json.Linq;

namespace Signum.Web
{
    public class EntityLine : EntityBase
    {
        public bool Autocomplete { get; set; }

        public string AutocompleteUrl { get; set; }

        public EntityLine(Type type, object untypedValue, Context parent, string prefix, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, prefix, propertyRoute)
        {
            Autocomplete = true;
            Navigate = true;
        }

        protected override Dictionary<string, object> OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();

            if (AutocompleteUrl.HasText())
                result.Add("autoCompleteUrl", AutocompleteUrl);

            return result;
        }

        protected override void SetReadOnly()
        {
            Parent.ReadOnly = true;
            Find = false;
            Create = false;
            Remove = false;
            Autocomplete = false;
        }
    }
}
