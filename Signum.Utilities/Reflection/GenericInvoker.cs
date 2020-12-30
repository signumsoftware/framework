using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Concurrent;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Utilities.Reflection
{
    public class GenericInvoker<T>
    {
        readonly ConcurrentDictionary<Type[], T> executor = new ConcurrentDictionary<Type[], T>(TypeArrayEqualityComparer.Instance);
        readonly Expression<T> expression;
        readonly int numParams;

        public GenericInvoker(Expression<T> expression)
        {
            this.expression = expression;
            this.numParams = GenericParametersVisitor.GenericParameters(expression);

            ParameterExpression tp = Expression.Parameter(typeof(Type[]));
        }

        public T GetInvoker(params Type[] types)
        {
            if (types.Length != numParams)
                throw new InvalidOperationException("Invalid generic arguments ({0} instead of {1})".FormatWith(types.Length, numParams));

            return executor.GetOrAdd(types, (ts) =>
                 GeneratorVisitor.GetGenerator<T>(expression, ts).Compile());
        }
    }


    class TypeArrayEqualityComparer : IEqualityComparer<Type[]>
    {
        public static readonly TypeArrayEqualityComparer Instance = new TypeArrayEqualityComparer();
        public bool Equals(Type[]? x, Type[]? y)
        {
            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return false;

            for (int i = 0; i < x.Length; i++)
            {
                if (!x[i].Equals(y[i]))
                    return false;
            }

            return true;
        }

        public int GetHashCode(Type[] types)
        {
            int result = 0;
            for (int i = 0; i < types.Length; i++)
                result ^= types[i].GetHashCode() >> i;

            return result;
        }
    }


    internal class GenericParametersVisitor : ExpressionVisitor
    {
        int? parameters;

        public static int GenericParameters(LambdaExpression expression)
        {
            var gpv = new GenericParametersVisitor();

            gpv.Visit(expression);

            if (gpv.parameters == null)
                throw new InvalidOperationException("No generic method or constructor found on expression:\r\n{0}".FormatWith(expression.ToString()));

            return gpv.parameters.Value;
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (!m.Method.IsGenericMethod)
                throw new InvalidOperationException("The method '{0}' should be generic".FormatWith(m.Method.MethodName()));

            parameters = m.Method.GetGenericMethodDefinition().GetGenericArguments().Length;

            return m;
        }

        protected override Expression VisitNew(NewExpression nex)
        {
            if (!nex.Type.IsGenericType)
                throw new InvalidOperationException("The constructor of {0} should be generic".FormatWith(nex.Type.TypeName()));

            parameters = nex.Type.GetGenericArguments().Length;

            return nex;
        }
    }

    class GeneratorVisitor : ExpressionVisitor
    {
        readonly Type[] types;

        public GeneratorVisitor(Type[] types)
        {
            this.types = types;
        }

        public static Expression<T> GetGenerator<T>(Expression<T> expression, Type[] types)
        {
            return (Expression<T>)new GeneratorVisitor(types).Visit(expression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            MethodInfo mi = m.Method.GetGenericMethodDefinition().MakeGenericMethod(types);
            var result = Expression.Call(m.Object, mi, m.Arguments.Zip(mi.GetParameters(), (e, p) => Convert(e, p.ParameterType)));
            return result;
        }

        protected override Expression VisitNew(NewExpression nex)
        {
            ConstructorInfo ci = nex.Constructor!.GetGenericConstructorDefinition().MakeGenericConstructor(types);
            var result = Expression.New(ci, nex.Arguments.Zip(ci.GetParameters(), (e, p) => Convert(e, p.ParameterType)));
            return result;
        }

        protected override Expression VisitLambda<T>(Expression<T> lambda)
        {
            var returnType = lambda.Type.GetMethod("Invoke")!.ReturnType;

            Expression body = Convert(this.Visit(lambda.Body), returnType);

            if (body != lambda.Body)
            {
                return Expression.Lambda(lambda.Type, body, lambda.Parameters);
            }
            return lambda;
        }

        private Expression Convert(Expression result, Type type)
        {
            if (result.Type == type)
                return result;

            if (result.NodeType == ExpressionType.Convert)
                result = ((UnaryExpression)result).Operand;

            return Expression.Convert(result, type);
        }
    }
}
