using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.ExpressionTrees;
using System.Linq.Expressions;
using System.Diagnostics;
using Signum.Engine.Maps;
using Signum.Entities;
using System.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Linq
{
    public class QueryFilterer : SimpleExpressionVisitor
    {
        static MethodInfo miFilter = ReflectionTools.GetMethodInfo(() => Filter<TypeDN>(null)).GetGenericMethodDefinition();

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (typeof(IQueryable).IsAssignableFrom(c.Type))
            {
                IQueryable query = (IQueryable)c.Value;

                if (query.IsBase() && typeof(IdentifiableEntity).IsAssignableFrom(query.ElementType))
                {
                    IQueryable newQuery = (IQueryable)miFilter.GenericInvoke(c.Type.GetGenericArguments(), null, new object[] { query });

                    if (newQuery != query)
                        return newQuery.Expression;

                    return c;
                }
                else
                {
                    /// <summary>
                    /// Replaces every expression like ConstantExpression{ Type = IQueryable, Value = complexExpr } by complexExpr
                    /// </summary>
                    return DbQueryProvider.Clean(query.Expression);
                }
            }

            return base.VisitConstant(c);
        }

        public static IQueryable Filter<T>(IQueryable query)
            where T:IdentifiableEntity
        {
            return Schema.Current.OnFilterQuery((IQueryable<T>)query);
        }

        internal static Expression Filter(Expression expression)
        {
            return new QueryFilterer().Visit(expression);
        }
    }
}
