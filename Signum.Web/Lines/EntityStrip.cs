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
using Newtonsoft.Json.Linq;
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

        protected override JObject OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();
            return result;
        }
    }
}
