using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Entities;
using Signum.Web.Properties;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace Signum.Web
{
    public static class TypeContextHelper
    {
        public static TypeContext<T> TypeContext<T>(this HtmlHelper helper) where T : ModifiableEntity
        {
            TypeContext<T> tc = helper.ViewData.Model as TypeContext<T>;
            if (tc != null)
                return tc;

            T element = helper.ViewData.Model as T;
            if (element != null)
                return new TypeContext<T>(element, null);

            throw new InvalidCastException("Impossible to convert object {0} from {1} to {2}".Formato(
                helper.ViewData.Model,
                helper.ViewData.Model.GetType().TypeName(),
                typeof(TypeContext<T>).TypeName()));
        }

        public static void WritePageHeader(this HtmlHelper helper)
        {
            if (helper.ViewData.ContainsKey(ViewDataKeys.Reactive))
                helper.Write(helper.Hidden(ViewDataKeys.Reactive));

            helper.WriteEntityInfo((TypeContext)helper.ViewData.Model);
        }

        public static void WritePopupHeader(this HtmlHelper helper)
        {
            if (helper.ViewData.ContainsKey(ViewDataKeys.Reactive))
                helper.Write(helper.Hidden(ViewDataKeys.Reactive));

            if (helper.ViewData.ContainsKey(ViewDataKeys.WriteSFInfo))
            {
                helper.WriteEntityInfo((TypeContext)helper.ViewData.Model);
                helper.ViewData.Remove(ViewDataKeys.WriteSFInfo);
            }
        }
    }
}
