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

namespace Signum.Web
{
    public class EmbeddedControl
    {
        public Dictionary<string, object> ViewData = new Dictionary<string, object>(0);
        public string ViewName { get; set; }
    }

    public static class EmbeddedControlHelper
    {
        public static MvcHtmlString EmbeddedControl<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
        {
            return EmbeddedControl<T, S>(helper, tc, property, null);
        }

        public static MvcHtmlString EmbeddedControl<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EmbeddedControl> settingsModifier)
        {
            TypeContext context = TypeContextUtilities.CleanTypeContext(Common.WalkExpression(tc, property));

            var ec = new EmbeddedControl();
            
            if (settingsModifier != null)
                settingsModifier(ec);

            string viewName = ec.ViewName;
            if (viewName == null)
            {
                var es = Navigator.Manager.EntitySettings.GetOrThrow(context.Type.CleanType());

                viewName = es.OnPartialViewName((ModifiableEntity)context.UntypedValue);

                context.ViewOverrides = es.ViewOverrides;
            }

            ViewDataDictionary vdd = new ViewDataDictionary(context);
            if (ec.ViewData != null)
                vdd.AddRange(ec.ViewData);

            var result = helper.Partial(viewName, vdd); 

            var vo = tc.ViewOverrides; 

            if (vo == null)
                return result;

            return vo.OnSurroundLine(context.PropertyRoute, helper, tc, result);
        }
    }
}
