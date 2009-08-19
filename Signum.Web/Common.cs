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

namespace Signum.Web
{
    public delegate void CommonTask(BaseLine eb, Type parent, TypeContext context);

    public static class Common
    {
        public static event CommonTask CommonTask;

        static Common()
        {
            CommonTask += new CommonTask(TaskSetLabelText);
            CommonTask += new CommonTask(TaskSetImplementations);
            CommonTask += new CommonTask(TaskSetReadOnly);
            CommonTask += new CommonTask(TaskSetValueLineType);
            CommonTask += new CommonTask(TaskSetHtmlProperties);
            CommonTask += new CommonTask(TaskSetReloadOnChange);
        }

        internal static void FireCommonTasks(BaseLine eb, Type parent, TypeContext context)
        {
            CommonTask(eb, parent, context);
        }

#region Tasks
        public static void TaskSetLabelText(BaseLine eb, Type parent, TypeContext context)
        {
            if (eb!=null)
                eb.LabelText = context.FriendlyName;
        }

        public static void TaskSetImplementations(BaseLine eb, Type parent, TypeContext context)
        {
            if (eb is EntityBase)
            {
                if (eb != null)
                {
                    List<PropertyInfo> path = context.GetPath();

                    if (eb is EntityList)
                        path.Add(path.Last().PropertyType.GetProperty("Item"));

                    ((EntityBase)eb).Implementations = Schema.Current.FindImplementations(parent, path.Cast<MemberInfo>().ToArray());
                }
            }
        }

        public static void TaskSetReadOnly(BaseLine vl, Type parent, TypeContext context)
        {
            if (vl != null)
            {
                if (context.LastProperty.IsReadOnly())
                {
                    if (vl.StyleContext == null)
                        vl.StyleContext = new StyleContext();
                    vl.StyleContext.ReadOnly = true;
                }
            }
        }

        public static void TaskSetValueLineType(BaseLine vl, Type parent, TypeContext context)
        {
            if (vl is ValueLine)
            {
                if (vl != null)
                {
                    if (context.LastProperty.HasAttribute<DateOnlyValidatorAttribute>())
                        ((ValueLine)vl).ValueLineType = ValueLineType.Date;
                }
            }
        }

        public static void TaskSetHtmlProperties(BaseLine vl, Type parent, TypeContext context)
        {
            if (vl is ValueLine)
            {
                if (vl != null)
                {
                    var atribute = context.LastProperty.SingleAttribute<StringLengthValidatorAttribute>();
                    if (atribute != null)
                    {
                        int max = atribute.Max; //-1 if not set
                        if (max != -1)
                        {
                            ((ValueLine)vl).ValueHtmlProps.AddRange(new
                            {
                                maxlength = max,
                                size = max
                            });
                        }
                    }
                }
            }
        }

        public static void TaskSetReloadOnChange(BaseLine eb, Type parent, TypeContext context)
        {
            if (eb is EntityLine)
            {
                if (eb != null)
                {
                    var atribute = context.LastProperty.SingleAttribute<ReloadEntityOnChange>();
                    if (atribute != null)
                        ((EntityLine)eb).ReloadOnChange = true;
                }
            }
        }
#endregion

        internal static TypeContext<S> WalkExpression<T, S>(TypeContext<T> tc, Expression<Func<T, S>> lambda)
        {
            PropertyInfo[] pi = GetMemberList(lambda).Cast<PropertyInfo>().ToArray();

            S value = lambda.Compile()(tc.Value); 

            return new TypeSubContext<S>(value, tc, pi); 
        }

        internal static MemberInfo[] GetMemberList(LambdaExpression lambdaToField)
        {
            Expression e = lambdaToField.Body;

            UnaryExpression ue = e as UnaryExpression;
            if (ue != null && ue.NodeType == ExpressionType.Convert && ue.Type == typeof(object))
                e = ue.Operand;

            MemberInfo[] result = e.FollowC(NextExpression).Select(a => GetMember(a)).NotNull().Reverse().ToArray();

            return result;
        }

        static Expression NextExpression(Expression e)
        {
            switch (e.NodeType)
            {
                case ExpressionType.Convert: return ((UnaryExpression)e).Operand;
                case ExpressionType.MemberAccess: return ((MemberExpression)e).Expression;
                case ExpressionType.Parameter: return null;
                default: throw new InvalidCastException("{0} Not Supported".Formato(e.NodeType));
            }
        }

        static MemberInfo GetMember(Expression e)
        {
            switch (e.NodeType)
            {
                case ExpressionType.MemberAccess:
                    MemberExpression me = (MemberExpression)e;
                    if (typeof(Lazy).IsAssignableFrom(me.Expression.Type) && me.Member.Name == "EntityOrNull")
                        return null;
                    return me.Member;
                case ExpressionType.Parameter: return null;
                case ExpressionType.Convert: return null;
                default: throw new InvalidCastException("{0} Not Supported".Formato(e.NodeType));
            }
        }
    }
}
