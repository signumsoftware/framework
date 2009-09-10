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
    public static class EmbeddedControlHelper
    {
        public static void EmbeddedControl<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            Type runtimeType = typeof(S);
            if (context.Value != null)
            {
                if (typeof(Lazy).IsAssignableFrom(context.Value.GetType()))
                    runtimeType = (context.Value as Lazy).RuntimeType;
                else
                    runtimeType = context.Value.GetType();
                if (runtimeType != typeof(S))
                {
                    var s = Expression.Parameter(typeof(S), "s");
                    var lambda = Expression.Lambda(Expression.Convert(s, runtimeType), s);
                    TypeContext c = Common.UntypedTypeContext(context, lambda, runtimeType);
                    EmbeddedControl(helper, c, Navigator.Manager.EntitySettings[runtimeType].PartialViewName);
                    return; 
                }
            }
            else
            {
                runtimeType = Reflector.ExtractLazy(runtimeType) ?? runtimeType;
            }
            EmbeddedControl(helper, context, Navigator.Manager.EntitySettings[runtimeType].PartialViewName);
        }

        public static void EmbeddedControl<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, string ViewName)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            Type runtimeType = typeof(S);
            if (context.Value != null)
            {
                if (typeof(Lazy).IsAssignableFrom(context.Value.GetType()))
                    runtimeType = (context.Value as Lazy).RuntimeType;
                else
                    runtimeType = context.Value.GetType();
            }
            else
            {
                runtimeType = Reflector.ExtractLazy(runtimeType) ?? runtimeType;
            }

            EmbeddedControl(helper, context, ViewName);
        }

        private static void EmbeddedControl(this HtmlHelper helper, TypeContext tc, string ViewName)
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

            if (tc.UntypedValue != null && typeof(IIdentifiable).IsAssignableFrom(tc.UntypedValue.GetType()) && ((IIdentifiable)tc.UntypedValue).IsNew)
                helper.Write(helper.Hidden(prefixedName + TypeContext.Separator + EntityBaseKeys.IsNew, ""));

            helper.RenderPartial(ViewName, vdd);
        }
    }
}
