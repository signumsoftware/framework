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
using Newtonsoft.Json.Linq;

namespace Signum.Web
{
    public static class EntityStripKeys
    {
        public const string ItemsContainer = "sfItemsContainer";
        public const string StripElement = "sfStripItem";
    }

    public class EntityStrip : EntityListBase
    {
        public bool Autocomplete { get; set; }
        public string AutocompleteUrl { get; set; }

        public bool Vertical { get; set; }

        public EntityStrip(Type type, object untypedValue, Context parent, string prefix, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, prefix, propertyRoute)
        {
            bool isEmbedded = type.ElementType().IsEmbeddedEntity();

            Find = false;
            Move = false;
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

        protected override Dictionary<string, object> OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();

            if (AutocompleteUrl.HasText())
                result.Add("autoCompleteUrl", AutocompleteUrl);

            result.Add("vertical", Vertical);
            return result;
        }
    }
}
