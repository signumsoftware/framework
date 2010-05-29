using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Entities.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.DataStructures;
using Signum.Engine;

namespace Signum.Web
{
    public delegate void CommonTask(BaseLine eb);

    public static class Common
    {
        public static event CommonTask CommonTask;

        static Common()
        {
            CommonTask += new CommonTask(TaskSetLabelText);
            CommonTask += new CommonTask(TaskSetFormatText);
            CommonTask += new CommonTask(TaskSetImplementations);
            CommonTask += new CommonTask(TaskSetReadOnly);
            CommonTask += new CommonTask(TaskSetHtmlProperties);
            CommonTask += new CommonTask(TaskSetReloadOnChange);
        }

        public static void FireCommonTasks(BaseLine eb)
        {
            CommonTask(eb);
        }

        #region Tasks
        public static void TaskSetLabelText(BaseLine bl)
        {
            if (bl != null && bl.PropertyRoute.PropertyRouteType == PropertyRouteType.Property)
                bl.LabelText = bl.PropertyRoute.PropertyInfo.NiceName();
        }

        static void TaskSetUnitText(BaseLine bl)
        {
            ValueLine vl = bl as ValueLine;
            if (vl != null && vl.PropertyRoute.PropertyRouteType == PropertyRouteType.Property)
            {
                UnitAttribute ua = bl.PropertyRoute.PropertyInfo.SingleAttribute<UnitAttribute>();
                if (ua != null)
                    vl.UnitText = ua.UnitName;
            }
        }

        static void TaskSetFormatText(BaseLine bl)
        {
            ValueLine vl = bl as ValueLine;
            if (vl != null && bl.PropertyRoute.PropertyRouteType == PropertyRouteType.Property)
            {
                string format = Reflector.FormatString(bl.PropertyRoute);
                if (format != null)
                    vl.Format = format;
            }
        }

        public static void TaskSetImplementations(BaseLine bl)
        {
            EntityBase eb = bl as EntityBase;
            if (eb != null)
            {
                PropertyRoute route = bl.PropertyRoute;

                if (Reflector.IsMList(bl.Type))
                    route = route.Add("Item");

                eb.Implementations = Schema.Current.FindImplementations(route);

                if (eb.Implementations != null && eb.Implementations.IsByAll)
                {
                    EntityLine el = eb as EntityLine;
                    if (el != null)
                        el.Autocomplete = false;
                }
            }
        }

        public static void TaskSetReadOnly(BaseLine bl)
        {
            if (bl != null && bl.PropertyRoute.PropertyRouteType == PropertyRouteType.Property)
            {
                if (bl.PropertyRoute.PropertyInfo.IsReadOnly() || bl.ReadOnly)
                {
                    bl.ReadOnly = true;
                }
            }
        }

        public static void TaskSetHtmlProperties(BaseLine bl)
        {
            ValueLine vl = bl as ValueLine;
            if (vl != null && bl.PropertyRoute.PropertyRouteType == PropertyRouteType.Property)
            {
                var atribute = bl.PropertyRoute.PropertyInfo.SingleAttribute<StringLengthValidatorAttribute>();
                if (atribute != null)
                {
                    int max = atribute.Max; //-1 if not set
                    if (max != -1)
                    {
                        vl.ValueHtmlProps.Add("maxlength", max);
                        vl.ValueHtmlProps.Add("size", max);
                    }
                }
            }
        }

        public static void TaskSetReloadOnChange(BaseLine bl)
        {
            if (bl != null)
            {
                var atribute = bl.PropertyRoute.PropertyInfo.SingleAttribute<ReloadEntityOnChange>();
                if (atribute != null)
                    bl.ReloadOnChange = true;
            }
        }
#endregion

        static MethodInfo mi = ReflectionTools.GetMethodInfo(() => Common.WalkExpression<TypeDN, TypeDN>(null, null)).GetGenericMethodDefinition();
        internal static TypeContext UntypedWalkExpression(TypeContext tc, LambdaExpression lambda)
        {
            Type returnType = lambda.Body.Type;
            return (TypeContext)mi.GenericInvoke(new[] { tc.Type, returnType }, null, new object[] { tc, lambda });
        }

        public static TypeContext<S> WalkExpression<T, S>(TypeContext<T> tc, Expression<Func<T, S>> lambda)
        {
            return MemberAccessGatherer.WalkExpression(tc, lambda);
        }
    }
}
