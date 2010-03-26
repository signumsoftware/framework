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
            private bool writeSFInfo = true;
            public bool WriteSFInfo
            {
                get { return writeSFInfo; }
                set { writeSFInfo = value; }
            }
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
                    TypeContext c = Common.UntypedWalkExpression(context, lambda);
                    EmbeddedControl(helper, c, Navigator.Manager.EntitySettings[runtimeType].PartialViewName((ModifiableEntity)c.UntypedValue), null, true);
                    return; 
                }
            }
            else
            {
                runtimeType = Reflector.ExtractLite(runtimeType) ?? runtimeType;
            }
            EmbeddedControl(helper, context, Navigator.Manager.EntitySettings[runtimeType].PartialViewName((ModifiableEntity)context.UntypedValue), null, true);
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
                    TypeContext c = Common.UntypedWalkExpression(context, lambda);
                    EmbeddedControl(helper, c, ec.ViewName ?? Navigator.Manager.EntitySettings[runtimeType].PartialViewName((ModifiableEntity)c.UntypedValue), ec.ViewData, ec.WriteSFInfo);
                    return; 
                }
            }
            else
            {
                runtimeType = Reflector.ExtractLite(runtimeType) ?? runtimeType;
            }

            EmbeddedControl(helper, context, ec.ViewName ?? Navigator.Manager.EntitySettings[runtimeType].PartialViewName((ModifiableEntity)context.UntypedValue), ec.ViewData, ec.WriteSFInfo);
        }

        private static void EmbeddedControl(this HtmlHelper helper, TypeContext tc, string ViewName, Dictionary<string,object> ViewData, bool writeSFInfo)
        { 
            string prefixedName = tc.Name;
            ViewDataDictionary vdd = new ViewDataDictionary()
            {
                { ViewDataKeys.TypeContextKey, prefixedName },
                { prefixedName, tc }, //Directly the context instead of the context.Value so we don't lose its context path
            };
            if (writeSFInfo)
                vdd.Add(ViewDataKeys.WriteSFInfo, "");

            if (helper.ViewData.ContainsKey(ViewDataKeys.PopupPrefix))
                vdd[ViewDataKeys.PopupPrefix] = helper.ViewData[ViewDataKeys.PopupPrefix];

            if (ViewData != null) {
                foreach (KeyValuePair<string,object> key in ViewData)
                    if (!vdd.ContainsKey(key.Key)) vdd[key.Key] = ViewData[key.Key];
            }
            helper.PropagateSFKeys(vdd);

            prefixedName = helper.GlobalName(prefixedName);

            //The info commented below should be now automatically included in sfRuntimeInfo field
            //if (tc.UntypedValue != null && typeof(IIdentifiable).IsAssignableFrom(tc.UntypedValue.GetType()) && ((IIdentifiable)tc.UntypedValue).IsNew)
            //    helper.Write(helper.Hidden(TypeContext.Compose(prefixedName, EntityBaseKeys.IsNew), ""));
            //if (helper.ViewData.ContainsKey(ViewDataKeys.Reactive))
            //{
            //    long? ticks = helper.GetChangeTicks(prefixedName);
            //    helper.Write("<input type='hidden' id='{0}' name='{0}' value='{1}'/>".Formato(TypeContext.Compose(prefixedName, TypeContext.Ticks), ticks!=null ? ticks.Value.ToString() : ""));
            //}

            helper.RenderPartial(ViewName, vdd);
        }
    }
}
