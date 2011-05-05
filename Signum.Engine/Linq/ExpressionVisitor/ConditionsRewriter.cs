using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Engine.Properties;
using System.Collections.ObjectModel;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Linq
{
    internal class ConditionsRewriter: DbExpressionVisitor
    {
        public static Expression Rewrite(Expression expression)
        {
            return new ConditionsRewriter().Visit(expression);
        }

        static Expression MakeSqlCondition(Expression exp)
        {
            if (exp == null)
                return null;

            if (!IsBooleanExpression(exp) || IsSqlCondition(exp))
                return exp;
            return Expression.Equal(exp, new SqlConstantExpression(true));
        }

        static Expression MakeSqlValue(Expression exp)
        {
            if (exp == null)
                return null;

            if (!IsBooleanExpression(exp) || !IsSqlCondition(exp))
                return exp;
            return new CaseExpression(new[] { new When(exp, new SqlConstantExpression(true)) }, new SqlConstantExpression(false));
        }

        static bool IsBooleanExpression(Expression expr)
        {
            return expr.Type.UnNullify() == typeof(bool);
        }

        static bool IsSqlCondition(Expression expression)
        {
            if (!IsBooleanExpression(expression))
                throw new InvalidOperationException("Expected boolean expression: {0}".Formato(expression.ToString()));

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
                    Expression operand = ((UnaryExpression)expression).Operand;
                    return IsBooleanExpression(operand) && IsSqlCondition(operand);

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
                case DbExpressionType.SqlConstant:
                    return false;
            }

            throw new InvalidOperationException("Expected expression: {0}".Formato(expression.ToString()));
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

        protected override Expression VisitSqlFunction(SqlFunctionExpression sqlFunction)
        {
            ReadOnlyCollection<Expression> args = sqlFunction.Arguments.NewIfChange(a => MakeSqlValue(Visit(a)));
            if (args != sqlFunction.Arguments)
                return new SqlFunctionExpression(sqlFunction.Type, sqlFunction.SqlFunction, args);
            return sqlFunction;
        }

        protected override When VisitWhen(When when)
        {
            var newCondition = MakeSqlCondition(Visit(when.Condition));
            var newValue = MakeSqlValue(Visit(when.Value));
            if (when.Condition != newCondition || newValue != when.Value)
                return new When(newCondition, newValue);
            return when;
        }

        protected override Expression VisitCase(CaseExpression cex)
        {
            var newWhens = cex.Whens.NewIfChange(w => VisitWhen(w));
            var newDefault = MakeSqlValue(Visit(cex.DefaultValue));

            if (newWhens != cex.Whens || newDefault != cex.DefaultValue)
                return new CaseExpression(newWhens, newDefault);
            return cex;
        }

        protected override Expression VisitAggregate(AggregateExpression aggregate)
        {
            Expression source = MakeSqlValue(Visit(aggregate.Source));
            if (source != aggregate.Source)
                return new AggregateExpression(aggregate.Type, source, aggregate.AggregateFunction);
            return aggregate;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            Expression top = this.Visit(select.Top);
            SourceExpression from = this.VisitSource(select.From);
            Expression where = MakeSqlCondition(this.Visit(select.Where));
            ReadOnlyCollection<ColumnDeclaration> columns = select.Columns.NewIfChange(c => MakeSqlValue(Visit(c.Expression)).Map(e => e == c.Expression ? c : new ColumnDeclaration(c.Name, e)));
            ReadOnlyCollection<OrderExpression> orderBy = select.OrderBy.NewIfChange(o => MakeSqlValue(Visit(o.Expression)).Map(e => e == o.Expression ? o : new OrderExpression(o.OrderType, e)));
            ReadOnlyCollection<Expression> groupBy = select.GroupBy.NewIfChange(e => MakeSqlValue(Visit(e)));

            if (top != select.Top || from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy)
                return new SelectExpression(select.Alias, select.Distinct, top, columns, from, where, orderBy, groupBy);

            return select;
        }

        protected override Expression VisitUpdate(UpdateExpression update)
        {
            var source = Visit(update.Source);
            var where = Visit(update.Where);
            var assigments = update.Assigments.NewIfChange(c =>
            {
                var exp = MakeSqlValue(Visit(c.Expression));
                if (exp != c.Expression)
                    return new ColumnAssignment(c.Column, exp);
                return c;
            });
            if (source != update.Source || where != update.Where || assigments != update.Assigments)
                return new UpdateExpression(update.Table, (SourceExpression)source, where, assigments);
            return update;
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            SourceExpression left = this.VisitSource(join.Left);
            SourceExpression right = this.VisitSource(join.Right);
            Expression condition = MakeSqlCondition(this.Visit(join.Condition));
            if (left != join.Left || right != join.Right || condition != join.Condition)
            {
                return new JoinExpression(join.JoinType, left, right, condition);
            }
            return join;
        }
    }
}
