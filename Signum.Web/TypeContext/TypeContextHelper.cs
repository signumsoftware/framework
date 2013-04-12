using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Entities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace Signum.Web
{
    public static class TypeContextHelper
    {
        public static TypeContext<T> TypeContext<T>(this HtmlHelper helper) where T : class
        {
            TypeContext<T> tc = helper.ViewData.Model as TypeContext<T>;
            if (tc != null)
                return tc;

            T element = helper.ViewData.Model as T;
            if (element != null)
                return new TypeContext<T>(element, null);


            TypeContext stc = helper.ViewData.Model as TypeContext;
            if (stc != null)
            {
                if (!typeof(T).IsAssignableFrom(stc.Type))
                    throw new InvalidOperationException("{0} is not convertible to {1}".Formato(stc.GetType().TypeName(), typeof(TypeContext<T>).TypeName()));

                return new TypeContext<T>((T)stc.UntypedValue, stc.ControlID);
            }

            throw new InvalidCastException("Impossible to convert object {0} of type {1} to {2}".Formato(
                helper.ViewData.Model,
                helper.ViewData.Model.GetType().TypeName(),
                typeof(TypeContext<T>).TypeName()));
        }

        public static MvcHtmlString NormalPageHeader(this HtmlHelper helper)
        {
            return helper.HiddenRuntimeInfo((TypeContext)helper.ViewData.Model);
        }
    }
}
