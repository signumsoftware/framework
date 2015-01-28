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
    public static class EntityRepeaterKeys
    {
        public const string ItemsContainer = "sfItemsContainer";
        public const string TabsContainer = "sfTabsContainer";
        public const string RepeaterElement = "sfRepeaterItem";
    }

    public class EntityRepeater : EntityListBase
    {
        public EntityRepeater(Type type, object untypedValue, Context parent, string prefix, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, prefix, propertyRoute)
        {
            Find = false;
            Move = false;
        }
    }

    public class EntityTabRepeater : EntityRepeater
    {
        public EntityTabRepeater(Type type, object untypedValue, Context parent, string prefix, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, prefix, propertyRoute)
        {
        }
    }
}
