using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;

namespace Signum.Engine.Linq
{
    internal class ConditionsRewriter: DbExpressionVisitor
    {
        public static Expression MakeSqlCondition(Expression expression)
        {
            ConditionsRewriter cr = new ConditionsRewriter();
            var exp = cr.Visit(expression);
            if (!IsBooleanExpression(exp) || IsSqlCondition(exp))
                return exp;
            return Expression.Equal(exp, Expression.Constant(true));
        }

        public static Expression MakeSqlValue(Expression expression)
        {
            ConditionsRewriter cr = new ConditionsRewriter();
            var exp = cr.Visit(expression);
            if (!IsBooleanExpression(exp) || !IsSqlCondition(exp))
                return exp;
            return new CaseExpression(new[] { new When(exp, Expression.Constant(true)) }, Expression.Constant(false));
        }

        public static bool IsBooleanExpression(Expression expr)
        {
            return expr.Type == typeof(bool);
        }

        public static bool IsSqlCondition(Expression expression)
        {
            if (!IsBooleanExpression(expression))
                throw new InvalidOperationException("Testing sql conditioness for non boolean expression : " + expression.ToString());

            switch (expression.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.Not:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.NotEqual:
                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    return true;

                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    Expression o = ((UnaryExpression)expression).Operand;
                    return IsBooleanExpression(o) && IsSqlCondition(o);

                case ExpressionType.Constant:
                case ExpressionType.Coalesce:
                    return false;
            }

            switch ((DbExpressionType)expression.NodeType)
            {
                case DbExpressionType.Exists:
                case DbExpressionType.Like:
                case DbExpressionType.In:
                case DbExpressionType.IsNull:
                case DbExpressionType.IsNotNull:
                    return true;

                case DbExpressionType.SqlFunction:
                case DbExpressionType.Column:
                case DbExpressionType.Projection:
                case DbExpressionType.Case:
                    return false;
            }

            throw new InvalidOperationException("Testing sql conditioness for non boolean expression : " + expression.ToString());
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.Not)
            {
                Expression operand = MakeSqlCondition(u.Operand);
                if (operand != u.Operand)
                {
                    return Expression.Not(operand);
                }
            }

            return base.VisitUnary(u);
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            if (b.NodeType == ExpressionType.And ||
                b.NodeType == ExpressionType.AndAlso ||
                b.NodeType == ExpressionType.Or ||
                b.NodeType == ExpressionType.OrElse ||
                b.NodeType == ExpressionType.ExclusiveOr)
            {
                Expression left = MakeSqlCondition(this.Visit(b.Left));
                Expression right = MakeSqlCondition(this.Visit(b.Right));
                if (left != b.Left || right != b.Right)
                {
                    return Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
                }
                return b;
            }
            else if (
                b.NodeType == ExpressionType.Equal ||
                b.NodeType == ExpressionType.NotEqual ||
                b.NodeType == ExpressionType.GreaterThan ||
                b.NodeType == ExpressionType.GreaterThanOrEqual||
                b.NodeType == ExpressionType.LessThan ||
                b.NodeType == ExpressionType.LessThanOrEqual)
            {
                Expression left = MakeSqlValue(b.Left);
                Expression right = MakeSqlValue(b.Right);
                if (left != b.Left || right != b.Right)
                {
                    return Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
                }
                return b;
            }
            else if (b.NodeType == ExpressionType.Coalesce)
            {
                Expression left = MakeSqlValue(b.Left);
                Expression right = MakeSqlValue(b.Right);
                if (left != b.Left || right != b.Right)
                {
                    return Expression.Coalesce(left, right);
                }
                return b;
            }

            return base.VisitBinary(b);
        }
    }
}
