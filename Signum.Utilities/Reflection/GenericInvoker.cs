using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Concurrent;

namespace Signum.Utilities.Reflection
{
    public delegate object InvokeDelegate(params object[] parameters);

    public class GenericInvoker
    {
        ConcurrentDictionary<object, InvokeDelegate> executor = new ConcurrentDictionary<object, InvokeDelegate>();

        MethodInfo methodInfo;
        Func<Type[], object> GetKey;

        private GenericInvoker(MethodInfo methodInfo)
        {
            if (!methodInfo.IsGenericMethod)
                throw new ArgumentException("Argument mi should be a generic method definition");

            if (!methodInfo.IsGenericMethodDefinition)
                this.methodInfo = methodInfo.GetGenericMethodDefinition();
            else
                this.methodInfo = methodInfo;

            GetKey = GetTupleConsturctor(this.methodInfo.GetGenericArguments().Length);
        }

        Func<Type[], object> GetTupleConsturctor(int numParams)
        {
            switch (numParams)
            {
                case 1: return t => t[0];
                case 2: return t => Tuple.Create(t[0], t[1]);
                case 3: return t => Tuple.Create(t[0], t[1], t[2]);
                case 4: return t => Tuple.Create(t[0], t[1], t[2], t[3]);
                case 5: return t => Tuple.Create(t[0], t[1], t[2], t[3], t[4]);
                case 6: return t => Tuple.Create(t[0], t[1], t[2], t[3], t[4], t[5]);
                case 7: return t => Tuple.Create(t[0], t[1], t[2], t[3], t[4], t[5], t[6]);
                default: throw new InvalidOperationException("8 or more generic parameters not supported");
            }
        }

        public InvokeDelegate GetInvoker(params Type[] types)
        {
            return executor.GetOrAdd(GetKey(types), (object o) => CreateInvoker(types));
        }

        InvokeDelegate CreateInvoker(Type[] types)
        {
            var args = Expression.Parameter(typeof(object[]), "args");

            MethodInfo mi = methodInfo.MakeGenericMethod(types);

            Expression body = mi.IsStatic ?
                     Expression.Call(mi, mi.GetParameters().Select(p => Access(args, p.Position, p.ParameterType))) :
                     Expression.Call(Access(args, 0, mi.DeclaringType), mi,
                    mi.GetParameters().Select(p => Access(args, p.Position + 1, p.ParameterType)));

            if (mi.ReturnType == typeof(void))
            {
                body = Expression.Block(body, Expression.Constant(null, typeof(object)));
            }
            else if (mi.ReturnType.IsValueType || mi.ReturnType.IsNullable())
            {
                body = Expression.Convert(body, typeof(object)); 
            }

            return Expression.Lambda<InvokeDelegate>(body, args).Compile();
        }

        Expression Access(ParameterExpression args, int index, Type type)
        {
            return Expression.Convert(Expression.ArrayIndex(args, Expression.Constant(index)), type);
        }

        public static GenericInvoker Create(MethodInfo mi)
        {
            return new GenericInvoker(mi);
        }

        public static GenericInvoker Create(Expression<Action> expression)
        {
            return new GenericInvoker(ReflectionTools.GetMethodInfo(expression));
        }

        public static GenericInvoker Create<T>(Expression<Action<T>> expression)
        {
            return new GenericInvoker(ReflectionTools.GetMethodInfo(expression));
        }

        public static GenericInvoker Create<R>(Expression<Func<R>> expression)
        {
            return new GenericInvoker(ReflectionTools.GetMethodInfo(expression));
        }

        public static GenericInvoker Create<T, R>(Expression<Func<T, R>> expression)
        {
            return new GenericInvoker(ReflectionTools.GetMethodInfo(expression));
        }
    }
}
