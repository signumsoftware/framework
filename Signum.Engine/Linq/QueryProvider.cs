using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Entities.Reflection;

namespace Signum.Engine.Linq
{

    /// <summary>
    /// A basic abstract LINQ query provider
    /// </summary>
    public abstract class QueryProvider : IQueryProvider
    {
        protected QueryProvider()
        {
        }

        IQueryable<S> IQueryProvider.CreateQuery<S>(Expression expression)
        {
            return new Query<S>(this, expression);
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            Type elementType = Reflector.CollectionType(expression.Type) ?? expression.Type; 
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(Query<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        S IQueryProvider.Execute<S>(Expression expression)
        {
            return (S)this.ExecutePrivate<S>(expression);
        }

        MethodInfo mi = typeof(QueryProvider).GetMethod("ExecutePrivate");

        object IQueryProvider.Execute(Expression expression)
        {
            return mi.MakeGenericMethod(expression.Type).Invoke(null, new[] { expression });
        }

        public abstract string GetQueryText(Expression expression);
        public abstract object ExecutePrivate<S>(Expression expression);
    }

  
}
