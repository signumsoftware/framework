using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Concurrent;
using Signum.Utilities.ExpressionTrees;
using System.Collections.ObjectModel;

namespace Signum.Utilities.Reflection
{
    public class GenericInvoker<T>
    {
        readonly ConcurrentDictionary<object, T> executor = new ConcurrentDictionary<object, T>();
        readonly Expression<T> expression;
        readonly int numParams;
        readonly Func<Type[], object> getKey;

        public GenericInvoker(Expression<T> expression)
        {
            this.expression = expression;
            this.numParams = GenericParametersVisitor.GenericParameters(expression);

            ParameterExpression tp = Expression.Parameter(typeof(Type[]));

            this.getKey = Expression.Lambda<Func<Type[], object>>(TupleReflection.TupleChainConstructor(0.To(numParams)
                                                         .Select(i => Expression.ArrayAccess(tp, Expression.Constant(i)))), tp).Compile();
        }

        public T GetInvoker(params Type[] types)
        {
            return executor.GetOrAdd(getKey(types), (object o) =>
            {
                if (types.Length != numParams)
                    throw new InvalidOperationException("Invalid generic arguments ({0} instead of {1})".Formato(types.Length, numParams));

                return GeneratorVisitor.GetGenerator<T>(expression, types).Compile();
            });
        }
    }

    internal class GenericParametersVisitor : SimpleExpressionVisitor
    {
        int? parameters;

        public static int GenericParameters(LambdaExpression expression)
        {
            var gpv = new GenericParametersVisitor();

            gpv.Visit(expression);

            if (gpv.parameters == null)
                throw new InvalidOperationException("No generic method or constructor found on expression:\r\n{0}".Formato(expression.NiceToString()));

            return gpv.parameters.Value;
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if(!m.Method.IsGenericMethod)
                throw new InvalidOperationException("The method '{0}' should be generic".Formato(m.Method.MethodName()));

            parameters = m.Method.GetGenericMethodDefinition().GetGenericArguments().Length;

            return m;
        }

        protected override NewExpression VisitNew(NewExpression nex)
        {
            if (!nex.Type.IsGenericType)
                throw new InvalidOperationException("The constructor of {0} should be generic".Formato(nex.Type.TypeName()));

            parameters = nex.Type.GetGenericArguments().Length;

            return nex;
        }
    }

    class GeneratorVisitor : SimpleExpressionVisitor
    {
        Type[] types;

        public static Expression<T> GetGenerator<T>(Expression<T> expression, Type[] types)
        {
            return (Expression<T>)new GeneratorVisitor { types = types }.Visit(expression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            MethodInfo mi = m.Method.GetGenericMethodDefinition().MakeGenericMethod(types); 
            var result = Expression.Call(m.Object, mi, m.Arguments.Zip(mi.GetParameters(), (e,p)=>Convert(e, p.ParameterType)));
            return result; 
        }

        protected override NewExpression VisitNew(NewExpression nex)
        {
            ConstructorInfo ci = nex.Constructor.GetGenericConstructorDefinition().MakeGenericConstructor(types);
            var result = Expression.New(ci, nex.Arguments.Zip(ci.GetParameters(), (e, p) => Convert(e, p.ParameterType)));
            return result;
        }

        protected override Expression VisitLambda(LambdaExpression lambda)
        {
            var returnType = lambda.Type.GetMethod("Invoke").ReturnType;

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
