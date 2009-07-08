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
                if (context.Property.IsReadOnly())
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
                    if (context.Property.HasAttribute<DateOnlyValidatorAttribute>())
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
                    var atribute = context.Property.SingleAttribute<StringLengthValidatorAttribute>();
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
#endregion

        internal static TypeContext<S> WalkExpressionGen<T, S>(TypeContext<T> tc, Expression<Func<T, S>> lambda)
            where S: Modifiable
        {
            return (TypeContext<S>)WalkExpression(tc, GetMemberList(Expression.Lambda<Func<T, object>>(lambda.Body, lambda.Parameters)));
        }

        internal static TypeContext WalkExpression<T>(TypeContext<T> tc, Expression<Func<T, object>> lambda)
        {
            return WalkExpression(tc, GetMemberList(lambda));
        }

        internal static MemberInfo[] GetMemberList<T>(Expression<Func<T, object>> lambdaToField)
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
                case ExpressionType.MemberAccess: return ((MemberExpression)e).Expression;
                case ExpressionType.Parameter: return null;
                default: throw new InvalidCastException("{0} Not Supported".Formato(e.NodeType));
            }
        }

        static MemberInfo GetMember(Expression e)
        {
            switch (e.NodeType)
            {
                case ExpressionType.MemberAccess: return ((MemberExpression)e).Member;
                case ExpressionType.Parameter: return null;
                default: throw new InvalidCastException("{0} Not Supported".Formato(e.NodeType));
            }
        }

        private static TypeContext WalkExpression(TypeContext tc, MemberInfo[] member)
        {
            if (member == null)
                throw new ArgumentNullException("member");

            var properties = member.Cast<PropertyInfo>().ToArray();

            TypeContext result = tc;

            foreach (PropertyInfo pi in properties)
            {
                PropertyPack pp = Reflector.GetPropertyValidators(pi.DeclaringType).TryGetC(pi.Name);
                if (pp != null)
                {
                    Func<object, object> getter = pp.GetValue;
                    result = TypeContext.Create(pi.PropertyType, getter(result.UntypedValue), result, pi);
                }
                else
                {
                    result = TypeContext.Create(pi.PropertyType, pi.GetValue(result.UntypedValue, null), result, pi);
                }
            }

            return result;
        }

        //Not necesary any more: Replaced by TaskSetImplementations
        //public static void FindImplementations<T, S>(EntityBase settings, Expression<Func<T, S>> property) 
        //    where S : ModifiableEntity
        //{
        //    if (settings != null && settings.Implementations != null)
        //        return;

        //    MemberInfo[] memberList = GetMemberList(Expression.Lambda<Func<T, object>>(property.Body, property.Parameters));
        //    settings.Implementations = Schema.Current.FindImplementations(typeof(T), memberList);
        //} 
    }
}
