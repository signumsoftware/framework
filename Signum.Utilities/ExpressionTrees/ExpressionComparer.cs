using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities.DataStructures;
using System.Collections.ObjectModel;
using System.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.Utilities.ExpressionTrees
{
    public class ExpressionComparer
    {
        internal ScopedDictionary<ParameterExpression, ParameterExpression> parameterScope;
        bool checkParameterNames = false;

        protected IDisposable ParameterScope()
        {
            var saved = parameterScope;
            parameterScope = new ScopedDictionary<ParameterExpression, ParameterExpression>(parameterScope);
            return new Disposable(() => parameterScope = saved);
        }

        protected ExpressionComparer(ScopedDictionary<ParameterExpression, ParameterExpression> parameterScope, bool checkParameterNames)
        {
            this.parameterScope = parameterScope;
            this.checkParameterNames = checkParameterNames;
        }

        public static bool AreEqual( Expression a, Expression b, ScopedDictionary<ParameterExpression, ParameterExpression> parameterScope = null, bool checkParameterNames = false)
        {
            return new ExpressionComparer(parameterScope, checkParameterNames).Compare(a, b);
        }

        protected virtual bool Compare(Expression a, Expression b)
        {
            if (a == b)
                return true;
            if (a == null || b == null)
                return false;
            if (a.NodeType != b.NodeType)
                return false;
            if (a.Type != b.Type)
                return false;
            switch (a.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                case ExpressionType.UnaryPlus:
                    return CompareUnary((UnaryExpression)a, (UnaryExpression)b);
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.Power:
                    return CompareBinary((BinaryExpression)a, (BinaryExpression)b);
                case ExpressionType.TypeIs:
                    return CompareTypeIs((TypeBinaryExpression)a, (TypeBinaryExpression)b);
                case ExpressionType.Conditional:
                    return CompareConditional((ConditionalExpression)a, (ConditionalExpression)b);
                case ExpressionType.Constant:
                    return CompareConstant((ConstantExpression)a, (ConstantExpression)b);
                case ExpressionType.Parameter:
                    return CompareParameter((ParameterExpression)a, (ParameterExpression)b);
                case ExpressionType.MemberAccess:
                    return CompareMemberAccess((MemberExpression)a, (MemberExpression)b);
                case ExpressionType.Call:
                    return CompareMethodCall((MethodCallExpression)a, (MethodCallExpression)b);
                case ExpressionType.Lambda:
                    return CompareLambda((LambdaExpression)a, (LambdaExpression)b);
                case ExpressionType.New:
                    return CompareNew((NewExpression)a, (NewExpression)b);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return CompareNewArray((NewArrayExpression)a, (NewArrayExpression)b);
                case ExpressionType.Invoke:
                    return CompareInvocation((InvocationExpression)a, (InvocationExpression)b);
                case ExpressionType.MemberInit:
                    return CompareMemberInit((MemberInitExpression)a, (MemberInitExpression)b);
                case ExpressionType.ListInit:
                    return CompareListInit((ListInitExpression)a, (ListInitExpression)b);
                default:
                    throw new Exception(string.Format("Unhandled expression type: '{0}'", a.NodeType));
            }
        }

        protected virtual bool CompareUnary(UnaryExpression a, UnaryExpression b)
        {
            return a.NodeType == b.NodeType
                && ReflectionTools.MethodEqual(a.Method, b.Method)
                && a.IsLifted == b.IsLifted
                && a.IsLiftedToNull == b.IsLiftedToNull
                && Compare(a.Operand, b.Operand);
        }

        protected virtual bool CompareBinary(BinaryExpression a, BinaryExpression b)
        {
            return a.NodeType == b.NodeType
                && ReflectionTools.MethodEqual(a.Method, b.Method)
                && a.IsLifted == b.IsLifted
                && a.IsLiftedToNull == b.IsLiftedToNull
                && Compare(a.Left, b.Left)
                && Compare(a.Right, b.Right);
        }

        protected virtual bool CompareTypeIs(TypeBinaryExpression a, TypeBinaryExpression b)
        {
            return a.TypeOperand == b.TypeOperand
                && Compare(a.Expression, b.Expression);
        }

        protected virtual bool CompareConditional(ConditionalExpression a, ConditionalExpression b)
        {
            return Compare(a.Test, b.Test)
                && Compare(a.IfTrue, b.IfTrue)
                && Compare(a.IfFalse, b.IfFalse);
        }

        protected virtual bool CompareConstant(ConstantExpression a, ConstantExpression b)
        {
            return object.Equals(a.Value, b.Value);
        }

        protected virtual bool CompareParameter(ParameterExpression a, ParameterExpression b)
        {
            if (parameterScope != null)
            {
                if (parameterScope.TryGetValue(a, out ParameterExpression mapped))
                    return mapped == b;
            }
            return a == b;
        }

        protected virtual bool CompareMemberAccess(MemberExpression a, MemberExpression b)
        {
            return ReflectionTools.MemeberEquals(a.Member, b.Member)
                && Compare(a.Expression, b.Expression);
        }

        protected virtual bool CompareMethodCall(MethodCallExpression a, MethodCallExpression b)
        {
            return ReflectionTools.MethodEqual(a.Method, b.Method)
                && Compare(a.Object, b.Object)
                && CompareList(a.Arguments, b.Arguments, Compare);
        }

        protected virtual bool CompareLambda(LambdaExpression a, LambdaExpression b)
        {
            int n = a.Parameters.Count;
            if (b.Parameters.Count != n)
                return false;
            // all must have same type
            for (int i = 0; i < n; i++)
            {
                if (a.Parameters[i].Type != b.Parameters[i].Type)
                    return false;

                if (checkParameterNames &&
                    a.Parameters[i].Name != b.Parameters[i].Name)
                    return false;
            }

            using (ParameterScope())
            {
                for (int i = 0; i < n; i++)
                    parameterScope.Add(a.Parameters[i], b.Parameters[i]);

                return Compare(a.Body, b.Body);
            }
        }

        protected virtual bool CompareNew(NewExpression a, NewExpression b)
        {
            return ReflectionTools.MemeberEquals(a.Constructor, b.Constructor)
                && CompareList(a.Arguments, b.Arguments, Compare)
                && CompareList(a.Members, b.Members, ReflectionTools.MemeberEquals);
        }

        protected virtual bool CompareNewArray(NewArrayExpression a, NewArrayExpression b)
        {
            return CompareList(a.Expressions, b.Expressions, Compare);
        }

        protected virtual bool CompareInvocation(InvocationExpression a, InvocationExpression b)
        {
            return Compare(a.Expression, b.Expression)
                && CompareList(a.Arguments, b.Arguments, Compare);
        }

        protected virtual bool CompareMemberInit(MemberInitExpression a, MemberInitExpression b)
        {
            return Compare(a.NewExpression, b.NewExpression)
                && CompareList(a.Bindings, b.Bindings, CompareBinding);
        }

        protected virtual bool CompareBinding(MemberBinding a, MemberBinding b)
        {
            if (a == b)
                return true;
            if (a == null || b == null)
                return false;
            if (a.BindingType != b.BindingType)
                return false;
            if (!ReflectionTools.MemeberEquals(a.Member, b.Member))
                return false;
            switch (a.BindingType)
            {
                case MemberBindingType.Assignment:
                    return CompareMemberAssignment((MemberAssignment)a, (MemberAssignment)b);
                case MemberBindingType.ListBinding:
                    return CompareMemberListBinding((MemberListBinding)a, (MemberListBinding)b);
                case MemberBindingType.MemberBinding:
                    return CompareMemberMemberBinding((MemberMemberBinding)a, (MemberMemberBinding)b);
                default:
                    throw new Exception(string.Format("Unhandled binding type: '{0}'", a.BindingType));
            }
        }

        protected virtual bool CompareMemberAssignment(MemberAssignment a, MemberAssignment b)
        {
            return ReflectionTools.MemeberEquals(a.Member, b.Member)
                && Compare(a.Expression, b.Expression);
        }

        protected virtual bool CompareMemberListBinding(MemberListBinding a, MemberListBinding b)
        {
            return ReflectionTools.MemeberEquals(a.Member, b.Member)
                && CompareList(a.Initializers, b.Initializers, CompareElementInit);
        }

        protected virtual bool CompareMemberMemberBinding(MemberMemberBinding a, MemberMemberBinding b)
        {
            return ReflectionTools.MemeberEquals(a.Member, b.Member)
                 && CompareList(a.Bindings, b.Bindings, CompareBinding);
        }

        protected virtual bool CompareListInit(ListInitExpression a, ListInitExpression b)
        {
            return Compare(a.NewExpression, b.NewExpression)
                && CompareList(a.Initializers, b.Initializers, CompareElementInit);
        }

        protected static bool CompareList<T>(ReadOnlyCollection<T> a, ReadOnlyCollection<T> b, Func<T, T, bool> comparer)
        {
            if (a == b)
                return true;
            if (a == null || b == null)
                return false;
            if (a.Count != b.Count)
                return false;
            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!comparer(a[i], b[i]))
                    return false;
            }
            return true;
        }

        protected static bool CompareDictionaries<K, V>(ReadOnlyDictionary<K, V> a, ReadOnlyDictionary<K, V> b, Func<V, V, bool> comparer)
        {
            if (a == b)
                return true;
            if (a == null || b == null)
                return false;
            if (a.Count != b.Count)
                return false;

            var keys = a.Keys.Intersect(b.Keys).ToList();

            if (keys.Count != a.Count)
                return false;

            foreach (var k in keys)
            {
                if (!comparer(a[k], b[k]))
                    return false;
            }

            return true;
        }

        protected virtual bool CompareElementInit(ElementInit a, ElementInit b)
        {
            return ReflectionTools.MethodEqual(a.AddMethod, b.AddMethod)
                && CompareList(a.Arguments, b.Arguments, Compare);
        }

        public static IEqualityComparer<E> GetComparer<E>(bool checkParameterNames) where E : Expression
        {
            return new ExpressionsEqualityComparer<E>(checkParameterNames);
        }

        class ExpressionsEqualityComparer<E> : IEqualityComparer<E> where E : Expression
        {
            bool checkParameterNames; 
            public ExpressionsEqualityComparer(bool checkParameterNames)
            {
                this.checkParameterNames = checkParameterNames;
            }

            public bool Equals(E x, E y)
            {
                return ExpressionComparer.AreEqual(x, y, parameterScope: null, checkParameterNames: checkParameterNames);
            }

            public int GetHashCode(E obj)
            {
                return obj.Type.GetHashCode() ^ obj.NodeType.GetHashCode();
            }
        }
    }

}
