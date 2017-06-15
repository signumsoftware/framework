using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using System.Diagnostics;

namespace Signum.Utilities.ExpressionTrees
{
    /// <summary>
    /// A default implementation of IQueryable for use with QueryProvider
    /// </summary>
    public class Query<T> : IQueryable<T>, IQueryable, IEnumerable<T>, IEnumerable, IOrderedQueryable<T>, IOrderedQueryable
    {
        QueryProvider provider;
        Expression expression;

        public Query(QueryProvider provider)
        {
            this.provider = provider ?? throw new ArgumentNullException("provider");
            this.expression = Expression.Constant(this);
        }

        public Query(QueryProvider provider, Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentOutOfRangeException("expression");
            }
            this.provider = provider ?? throw new ArgumentNullException("provider");
            this.expression = expression;
        }

        public Expression Expression
        {
            get { return this.expression; }
        }

        public Type ElementType
        {
            get { return typeof(T); }
        }

        public IQueryProvider Provider
        {
            get { return this.provider; }
        }

        [DebuggerStepThrough]
        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)this.provider.Execute(this.expression)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public override string ToString()
        {
            if (expression is ConstantExpression ce && ce.Value == this)
                return this.GetType().TypeName().CleanIdentifiers();
            else
                return expression.ToString();
        }

        public string QueryText
        {
            get
            {
                try
                {
                    return provider.GetQueryText(expression);
                }
                catch (Exception)
                {
                    return "Unavailable";
                }
            }
        }
    }
}
