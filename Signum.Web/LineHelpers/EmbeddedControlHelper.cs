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
    public class EmbeddedControlSettings
    {
        public Dictionary<string, object> ViewData = new Dictionary<string, object>(0);
        public string ViewName { get; set; }
    }

    public static class EmbeddedControlHelper
    {
        public static void RenderEmbeddedControl<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
        {
            RenderEmbeddedControl<T, S>(helper, tc, property, null);
        }

        public static void RenderEmbeddedControl<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EmbeddedControlSettings> settingsModifier)
        {
            EmbeddedControlSettings ec = new EmbeddedControlSettings();

            if (settingsModifier != null)
                settingsModifier(ec);

            TypeContext ntc = TypeContextUtilities.CleanTypeContext(Common.WalkExpression(tc, property));

            if(ec.ViewName == null)
                ec.ViewName = Navigator.Manager.EntitySettings[ntc.Type].OnPartialViewName((ModifiableEntity)ntc.UntypedValue); 
            
            ViewDataDictionary vdd = new ViewDataDictionary(ntc);
            if (ec.ViewData != null)
                vdd.AddRange(ec.ViewData); 

            helper.PropagateSFKeys(vdd);

            helper.RenderPartial(ec.ViewName, vdd);
        }

        public static MvcHtmlString EmbeddedControl<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
        {
            return EmbeddedControl<T, S>(helper, tc, property, null);
        }

        public static MvcHtmlString EmbeddedControl<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EmbeddedControlSettings> settingsModifier)
        {
            EmbeddedControlSettings ec = new EmbeddedControlSettings();

            if (settingsModifier != null)
                settingsModifier(ec);

            TypeContext ntc = TypeContextUtilities.CleanTypeContext(Common.WalkExpression(tc, property));

            if (ec.ViewName == null)
                ec.ViewName = Navigator.Manager.EntitySettings[ntc.Type].OnPartialViewName((ModifiableEntity)ntc.UntypedValue);

            ViewDataDictionary vdd = new ViewDataDictionary(ntc);
            if (ec.ViewData != null)
                vdd.AddRange(ec.ViewData);

            helper.PropagateSFKeys(vdd);

            return helper.Partial(ec.ViewName, vdd);
        }
    }
}
