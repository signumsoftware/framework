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
    //public static class EntityLineKeys
    //{
    //    public const string DDL = "sfDDL";
    //}

    //public class EntityLine : EntityBase
    //{
    //    private bool autocomplete = true;
    //    public bool Autocomplete
    //    {
    //        get { return autocomplete; }
    //        set { autocomplete = value; }
    //    }

    //    public EntityLine()
    //    {
    //    }

    //    public override void SetReadOnly()
    //    {
    //        Find = false;
    //        Create = false;
    //        Remove = false;
    //        Autocomplete = false;
    //        Implementations = null;
    //    }

    //    bool reloadOnChange = false;
    //    public bool ReloadOnChange
    //    {
    //        get { return reloadOnChange; }
    //        set { reloadOnChange = value; }
    //    }
    //}

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
            }
            else
            {
                runtimeType = Reflector.ExtractLazy(runtimeType) ?? runtimeType;
            }

            string prefixedName = context.Name;
            //if (!helper.ViewData.ContainsKey(ViewDataKeys.TypeContextKey) ||
            //    !((string)helper.ViewData[ViewDataKeys.TypeContextKey]).HasText() ||
            //    !prefixedName.StartsWith((string)helper.ViewData[ViewDataKeys.TypeContextKey]))
            //prefixedName = helper.GlobalPrefixedName(context.Name);

            ViewDataDictionary vdd = new ViewDataDictionary()
            {
                { ViewDataKeys.TypeContextKey, prefixedName },
                { prefixedName, context.Value },
                { ViewDataKeys.EmbeddedControl, "" },
            };
            if (helper.ViewData.ContainsKey(ViewDataKeys.PopupPrefix))
                vdd[ViewDataKeys.PopupPrefix] = helper.ViewData[ViewDataKeys.PopupPrefix];

            if (context.Value != null && typeof(IIdentifiable).IsAssignableFrom(context.Value.GetType()) && ((IIdentifiable)context.Value).IsNew)
                helper.Write(helper.Hidden(prefixedName + TypeContext.Separator + EntityBaseKeys.IsNew, ""));

            helper.RenderPartial(Navigator.Manager.EntitySettings[runtimeType].PartialViewName, vdd);
        }
    }
}
