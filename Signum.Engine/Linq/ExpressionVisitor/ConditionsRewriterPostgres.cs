using System;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Collections.ObjectModel;
using Signum.Utilities.ExpressionTrees;
using System.Data.SqlTypes;

namespace Signum.Engine.Linq
{
    internal class ConditionsRewriterPostgres: DbExpressionVisitor
    {
        public static Expression Rewrite(Expression expression)
        {
            return new ConditionsRewriterPostgres().Visit(expression);
        }

        protected internal override Expression VisitSqlCast(SqlCastExpression castExpr)
        {
            var expression = Visit(castExpr.Expression);

            if(expression.Type.UnNullify() == typeof(bool) && castExpr.Type.UnNullify() != typeof(int))
                return new SqlCastExpression(castExpr.Type, new SqlCastExpression(typeof(int), expression), castExpr.DbType);

            if (expression != castExpr.Expression)
                return new SqlCastExpression(castExpr.Type, expression, castExpr.DbType);

            return castExpr;
        }
    }
}
