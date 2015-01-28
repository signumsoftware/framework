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

namespace Signum.Web
{
    public class EntityList : EntityListBase
    {
        public readonly RouteValueDictionary ListHtmlProps = new RouteValueDictionary();
        
        public EntityList(Type type, object untypedValue, Context parent, string prefix, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, prefix, propertyRoute)
        {
            Move = false;
        }
    }
}
