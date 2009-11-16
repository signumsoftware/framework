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
     public class EmbeddedControlSettings {
            //public readonly Dictionary<string, object> ViewData = new Dictionary<string, object>(0);
            public Dictionary<string, object> ViewData = new Dictionary<string, object>(0);
            public string ViewName { get; set; }
        }

        public static class EmbeddedControlHelper
        {

       public static void EmbeddedControl<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            Type runtimeType = typeof(S);
            if (context.Value != null)
            {
                if (typeof(Lite).IsAssignableFrom(context.Value.GetType()))
                    runtimeType = (context.Value as Lite).RuntimeType;
                else
                    runtimeType = context.Value.GetType();
                if (runtimeType != typeof(S))
                {
                    var s = Expression.Parameter(typeof(S), "s");
                    var lambda = Expression.Lambda(Expression.Convert(s, runtimeType), s);
                    TypeContext c = Common.UntypedTypeContext(context, lambda, runtimeType);
                    EmbeddedControl(helper, c, Navigator.Manager.EntitySettings[runtimeType].PartialViewName, null);
                    return; 
                }
            }
            else
            {
                runtimeType = Reflector.ExtractLite(runtimeType) ?? runtimeType;
            }
            EmbeddedControl(helper, context, Navigator.Manager.EntitySettings[runtimeType].PartialViewName, null);
        }

       public static void EmbeddedControl<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EmbeddedControlSettings> settingsModifier)
        {
            EmbeddedControlSettings ec = new EmbeddedControlSettings();
            settingsModifier(ec);

            TypeContext<S> context = Common.WalkExpression(tc, property);

            Type runtimeType = typeof(S);
            if (context.Value != null)
            {
                if (typeof(Lite).IsAssignableFrom(context.Value.GetType()))
                    runtimeType = (context.Value as Lite).RuntimeType;
                else
                    runtimeType = context.Value.GetType();
                if (runtimeType != typeof(S))
                {
                    var s = Expression.Parameter(typeof(S), "s");
                    var lambda = Expression.Lambda(Expression.Convert(s, runtimeType), s);
                    TypeContext c = Common.UntypedTypeContext(context, lambda, runtimeType);
                    EmbeddedControl(helper, c, ec.ViewName ?? Navigator.Manager.EntitySettings[runtimeType].PartialViewName, ec.ViewData);
                    return; 
                }
            }
            else
            {
                runtimeType = Reflector.ExtractLite(runtimeType) ?? runtimeType;
            }

            EmbeddedControl(helper, context, ec.ViewName ?? Navigator.Manager.EntitySettings[runtimeType].PartialViewName, ec.ViewData);
        }

        private static void EmbeddedControl(this HtmlHelper helper, TypeContext tc, string ViewName, Dictionary<string,object> ViewData)
        { 
            string prefixedName = tc.Name;
            ViewDataDictionary vdd = new ViewDataDictionary()
            {
                { ViewDataKeys.TypeContextKey, prefixedName },
                { prefixedName, tc }, //Directly the context instead of the context.Value so we don't lose its context path
                { ViewDataKeys.EmbeddedControl, "" },
            };
            if (helper.ViewData.ContainsKey(ViewDataKeys.PopupPrefix))
                vdd[ViewDataKeys.PopupPrefix] = helper.ViewData[ViewDataKeys.PopupPrefix];

            if (ViewData != null) {
                foreach (KeyValuePair<string,object> key in ViewData)
                    if (!vdd.ContainsKey(key.Key)) vdd[key.Key] = ViewData[key.Key];
            }
            helper.PropagateSFKeys(vdd);

            prefixedName = helper.GlobalName(prefixedName);

            if (tc.UntypedValue != null && typeof(IIdentifiable).IsAssignableFrom(tc.UntypedValue.GetType()) && ((IIdentifiable)tc.UntypedValue).IsNew)
                helper.Write(helper.Hidden(TypeContext.Compose(prefixedName, EntityBaseKeys.IsNew), ""));

            if (helper.ViewData.ContainsKey(ViewDataKeys.Reactive))
            {
                long? ticks = helper.GetChangeTicks(prefixedName);
                helper.Write("<input type='hidden' id='{0}' name='{0}' value='{1}'/>".Formato(TypeContext.Compose(prefixedName, TypeContext.Ticks), ticks!=null ? ticks.Value.ToString() : ""));
            }

            helper.RenderPartial(ViewName, vdd);
        }
    }
}
