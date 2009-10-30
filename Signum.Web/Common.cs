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

        public static void FireCommonTasks(BaseLine eb, Type parent, TypeContext context)
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

                    ((EntityBase)eb).Implementations = Schema.Current.FindImplementations(context.LastIdentifiableProperty, path.Cast<MemberInfo>().ToArray());
                }
            }
        }

        public static void TaskSetReadOnly(BaseLine vl, Type parent, TypeContext context)
        {
            if (vl != null)
            {
                if (context.LastProperty.IsReadOnly() || StyleContext.Current.ReadOnly)
                {
                    //if (vl.StyleContext == null)
                    //    vl.StyleContext = new StyleContext();
                    //vl.StyleContext.ReadOnly = true;
                    vl.ReadOnly = true;
                    vl.SetReadOnly();
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
                            ((ValueLine)vl).ValueHtmlProps.AddRangeFromAnonymousType(new
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
            if (eb != null)
            {
                var atribute = context.LastProperty.SingleAttribute<ReloadEntityOnChange>();
                if (atribute != null)
                    eb.ReloadOnChange = true;
            }
        }
#endregion
        static MethodInfo mi = ReflectionTools.GetMethodInfo(() => Common.WalkExpression<TypeDN, TypeDN>(null, null)).GetGenericMethodDefinition();
        //static MethodInfo mi = typeof(Common).GetMethod("WalkExpression", BindingFlags.Static | BindingFlags.Public); 

        internal static TypeContext UntypedTypeContext(TypeContext tc, LambdaExpression lambda, Type returnType)
        {
            return (TypeContext)mi.MakeGenericMethod(tc.ContextType, returnType).Invoke(null, new object[] { tc, lambda });
        }

        public static TypeContext<S> WalkExpression<T, S>(TypeContext<T> tc, Expression<Func<T, S>> lambda)
        {
            PropertyInfo[] pi = MemberAccessGatherer.GetMemberList(lambda).Cast<PropertyInfo>().ToArray();

            S value = lambda.Compile()(tc.Value); 

            return new TypeSubContext<S>(value, tc, pi); 
        }

        //internal static MemberInfo[] GetMemberList(LambdaExpression lambdaToField)
        //{
        //    Expression e = lambdaToField.Body;

        //    UnaryExpression ue = e as UnaryExpression;
        //    if (ue != null && ue.NodeType == ExpressionType.Convert && ue.Type == typeof(object))
        //        e = ue.Operand;

        //    MemberInfo[] result = e.FollowC(NextExpression).Select(a => GetMember(a)).NotNull().ToArray();

        //    return result;
        //}
    }
    
    internal class MemberAccessGatherer : ExpressionVisitor
    {
        ImmutableStack<MemberInfo> members = ImmutableStack<MemberInfo>.Empty;

        internal static MemberInfo[] GetMemberList(LambdaExpression lambdaToField)
        {
            var mag = new MemberAccessGatherer();
            mag.Visit(lambdaToField);
            return mag.members.ToArray(); 
        }

        protected override Expression VisitMemberAccess(MemberExpression me)
        {
            if (!typeof(Lite).IsAssignableFrom(me.Expression.Type) && me.Member.Name != "EntityOrNull")
                members = members.Push(me.Member);

            Expression exp = this.Visit(me.Expression);

            return me; 
        }

        static string[] tryies =new string[]{"TryCC", "TryCS", "TrySS", "TrySC"};

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if(m.Method.DeclaringType == typeof(Extensions) && tryies.Contains(m.Method.Name))
            {
                Visit(m.Arguments[1]);
                Visit(m.Arguments[0]);
                return m;
            }

            return base.VisitMethodCall(m); 
        }
    }
}
