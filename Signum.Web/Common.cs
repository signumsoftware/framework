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

namespace Signum.Web
{
    public delegate void CommonRouteTask(EntityBase eb, Type parent, TypeContext context);

    public static class Common
    {
        public static event CommonRouteTask CommonTask;

        static Common()
        {
            CommonTask += new CommonRouteTask(TaskSetLabelText);
            CommonTask += new CommonRouteTask(TaskSetImplementations);
        }

        internal static void FireCommonTasks(EntityBase eb, Type parent, TypeContext context)
        {
            CommonTask(eb, parent, context);
        }

#region Tasks
        public static void TaskSetLabelText(EntityBase eb, Type parent, TypeContext context)
        {
            if (eb!=null)
                eb.LabelText = context.PropertyName;
        }

        public static void TaskSetImplementations(EntityBase eb, Type parent, TypeContext context)
        {
            if (eb != null)
            {
                List<PropertyInfo> path = context.GetPath();

                if (eb is EntityList)
                    path.Add(path.Last().PropertyType.GetProperty("Item"));

                eb.Implementations = Schema.Current.FindImplementations(parent, path.Cast<MemberInfo>().ToArray());
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
                PropertyPack pp = ModifiableEntity.GetPropertyValidators(pi.DeclaringType).TryGetC(pi.Name);
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
