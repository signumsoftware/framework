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
        public static void EmbeddedControl<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
        {
            EmbeddedControl<T, S>(helper, tc, property, null);
        }

        public static void EmbeddedControl<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EmbeddedControlSettings> settingsModifier)
        {
            EmbeddedControlSettings ec = new EmbeddedControlSettings();

            if (settingsModifier != null)
                settingsModifier(ec);

            TypeContext<S> context = Common.WalkExpression(tc, property);

            //if (context.Value == null)
            //    throw new NullReferenceException("EmbeddedControl");

            TypeContext ntc = TypeContextUtilities.CleanTypeContext(context);

            string viewName = ec.ViewName ?? Navigator.Manager.EntitySettings[ntc.Type].OnPartialViewName((ModifiableEntity)ntc.UntypedValue);

            ViewDataDictionary vdd = new ViewDataDictionary(ntc);

            if (ec.ViewData != null)
            {
                foreach (KeyValuePair<string, object> key in ec.ViewData)
                    if (!vdd.ContainsKey(key.Key)) vdd[key.Key] = ec.ViewData[key.Key];
            }
            helper.PropagateSFKeys(vdd);

            helper.RenderPartial(viewName, vdd);
        }
    }
}
