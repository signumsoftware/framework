using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Collections.ObjectModel;
using Signum.Utilities.ExpressionTrees;
using System.Data.SqlTypes;

namespace Signum.Engine.Linq
{
    internal class ConditionsRewriter: DbExpressionVisitor
    {
        public static Expression Rewrite(Expression expression)
        {
            return new ConditionsRewriter().Visit(expression);
        }

        public bool inSql = false;

        public IDisposable InSql()
        {
            var oldInSelect = inSql;
            inSql = true;
            return new Disposable(() => inSql = oldInSelect); 
        }

        static BinaryExpression TrueCondition = Expression.Equal(new SqlConstantExpression(1), new SqlConstantExpression(1));
        static BinaryExpression FalseCondition = Expression.Equal(new SqlConstantExpression(1), new SqlConstantExpression(0));

        Expression MakeSqlCondition(Expression exp)
        {
            if (exp == null)
                return null;

            if (!inSql || !IsBooleanExpression(exp))
                return exp;

            if (exp.NodeType == ExpressionType.Constant)
            {
                bool? value = ((bool?)((ConstantExpression)exp).Value);

                return value == true ? TrueCondition : FalseCondition;
            }

            if (IsSqlCondition(exp))
                return exp;

            var result = Expression.Equal(exp, exp.Type.IsNullable() ? new SqlConstantExpression(true).Nullify() : new SqlConstantExpression(true));

            return exp.Type.IsNullable() ? result.Nullify() : result;
        }
 

        Expression MakeSqlValue(Expression exp)
        {
            if (exp == null)
                return null;

            if (!inSql || !IsBooleanExpression(exp))
                return exp;

            if (exp.NodeType == ExpressionType.Constant)
            {
                switch (((bool?)((ConstantExpression)exp).Value))
                {
                    case false: return new SqlConstantExpression(0, exp.Type);
                    case true: return new SqlConstantExpression(1, exp.Type);
                    case null: return new SqlConstantExpression(null, exp.Type);
                }
                throw new InvalidOperationException("Entity");
            }

            if (!IsSqlCondition(exp))
                return exp;

            var result =  new CaseExpression(new[] { new When(exp, new SqlConstantExpression(true)) }, new SqlConstantExpression(false));

            return exp.Type.IsNullable() ? result.Nullify() : result;
        }

        static bool IsBooleanExpression(Expression expr)
        {
            return expr.Type.UnNullify() == typeof(bool) || expr.Type.UnNullify() == typeof(SqlBoolean);
        }

        static bool IsSqlCondition(Expression expression)
        {
            if (!IsBooleanExpression(expression))
                throw new InvalidOperationException("Expected boolean expression: {0}".FormatWith(expression.ToString()));

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

            var exp = expression as DbExpression;
            switch (exp.DbNodeType)
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

            throw new InvalidOperationException("Expected expression: {0}".FormatWith(expression.ToString()));
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.Not)
            {
                var op = this.Visit(u.Operand);

                if (IsTrue(op))
                    return FalseCondition;

                if (IsFalse(op))
                    return TrueCondition;

                Expression operand = MakeSqlCondition(op);
                if (operand != u.Operand)
                {
                    return Expression.Not(operand);
                }
            }
            return base.VisitUnary(u);
        }

        protected internal override Expression VisitSqlCast(SqlCastExpression castExpr)
        {
            var expression = MakeSqlValue(Visit(castExpr.Expression));
            if (expression != castExpr.Expression)
                return new SqlCastExpression(castExpr.Type, expression, castExpr.SqlDbType);
            return castExpr;
        }


        private bool IsTrue(Expression operand)
        {
            return operand == TrueCondition || (operand is SqlConstantExpression && ((SqlConstantExpression)operand).Value.Equals(1));
        }

        private bool IsFalse(Expression operand)
        {
            return operand == FalseCondition || (operand is SqlConstantExpression && ((SqlConstantExpression)operand).Value.Equals(0));
        }

        static ConstantExpression False = Expression.Constant(false);
        static ConstantExpression True = Expression.Constant(true);

        Expression SmartAnd(Expression left, Expression right, bool sortCircuit)
        {
            if (IsFalse(left) || IsFalse(right))
                return False;

            if (IsTrue(left))
                return right;

            if (IsTrue(right))
                return left;

            if (sortCircuit)
                return CombineConditions(left, right, Expression.AndAlso);
            else
                return CombineConditions(left, right, Expression.And);
        }

        Expression SmartOr(Expression left, Expression right, bool sortCircuit)
        {
            if (IsTrue(left) || IsTrue(right))
                return True;

            if (IsFalse(left))
                return right;

            if (IsFalse(right))
                return left;

            if(sortCircuit)
                return CombineConditions(left, right, Expression.OrElse);
            else
                return CombineConditions(left, right, Expression.Or);
        }

        BinaryExpression CombineConditions(Expression left, Expression right, Func<Expression, Expression, BinaryExpression> combinator)
        {
            var nLeft = MakeSqlCondition(left);
            var nRight = MakeSqlCondition(right);

            if (nLeft.Type.IsNullable() || nRight.Type.IsNullable())
            {
                nLeft = nLeft.Nullify();
                nRight = nRight.Nullify(); 
            }

            return combinator(nLeft, nRight); 
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            if (b.NodeType == ExpressionType.And || b.NodeType == ExpressionType.AndAlso)
            {
                return SmartAnd(this.Visit(b.Left), this.Visit(b.Right), b.NodeType == ExpressionType.AndAlso);
            }
            else if (b.NodeType == ExpressionType.Or || b.NodeType == ExpressionType.OrElse)
            {
                return SmartOr(this.Visit(b.Left), this.Visit(b.Right), b.NodeType == ExpressionType.OrElse);
            }
            else if (b.NodeType == ExpressionType.ExclusiveOr)
            {
                return Expression.ExclusiveOr(MakeSqlCondition(Visit(b.Left)), MakeSqlCondition(Visit(b.Right)));
            }
            else if (
                b.NodeType == ExpressionType.Equal ||
                b.NodeType == ExpressionType.NotEqual ||
                b.NodeType == ExpressionType.GreaterThan ||
                b.NodeType == ExpressionType.GreaterThanOrEqual ||
                b.NodeType == ExpressionType.LessThan ||
                b.NodeType == ExpressionType.LessThanOrEqual)
            {
                Expression left = MakeSqlValue(this.Visit(b.Left));
                Expression right = MakeSqlValue(this.Visit(b.Right));
                if (left != b.Left || right != b.Right)
                {
                    return Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
                }
                return b;
            }
            else if (b.NodeType == ExpressionType.Coalesce)
            {
                Expression left = MakeSqlValue(this.Visit(b.Left));
                Expression right = MakeSqlValue(this.Visit(b.Right));
                if (left != b.Left || right != b.Right)
                {
                    return Expression.Coalesce(left, right);
                }
                return b;
            }

            return base.VisitBinary(b);
        }

        protected internal override Expression VisitSqlFunction(SqlFunctionExpression sqlFunction)
        {
            Expression obj = MakeSqlValue(Visit(sqlFunction.Object));
            ReadOnlyCollection<Expression> args = Visit(sqlFunction.Arguments, a=>MakeSqlValue(Visit(a)));
            if (args != sqlFunction.Arguments || obj != sqlFunction.Object)
                return new SqlFunctionExpression(sqlFunction.Type, obj, sqlFunction.SqlFunction, args);
            return sqlFunction;
        }

        protected internal override Expression VisitSqlTableValuedFunction(SqlTableValuedFunctionExpression sqlFunction)
        {
            ReadOnlyCollection<Expression> args = Visit(sqlFunction.Arguments, a => MakeSqlValue(Visit(a)));
            if (args != sqlFunction.Arguments)
                return new SqlTableValuedFunctionExpression(sqlFunction.SqlFunction, sqlFunction.Table, sqlFunction.Alias, args);
            return sqlFunction;
        }

        protected internal override Expression VisitCase(CaseExpression cex)
        {
            var newWhens = Visit(cex.Whens, w => VisitWhen(w));
            var newDefault = MakeSqlValue(Visit(cex.DefaultValue));

            if (newWhens != cex.Whens || newDefault != cex.DefaultValue)
                return new CaseExpression(newWhens, newDefault);
            return cex;
        }

        protected internal override When VisitWhen(When when)
        {
            var newCondition = MakeSqlCondition(Visit(when.Condition));
            var newValue = MakeSqlValue(Visit(when.Value));
            if (when.Condition != newCondition || newValue != when.Value)
                return new When(newCondition, newValue);
            return when;
        }

        protected internal override Expression VisitAggregate(AggregateExpression aggregate)
        {
            Expression source = MakeSqlValue(Visit(aggregate.Expression));
            if (source != aggregate.Expression)
                return new AggregateExpression(aggregate.Type, source, aggregate.AggregateFunction, aggregate.Distinct);
            return aggregate;
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            Expression top = this.Visit(select.Top);
            SourceExpression from = this.VisitSource(select.From);
            Expression where = MakeSqlCondition(this.Visit(select.Where));
            ReadOnlyCollection<ColumnDeclaration> columns = Visit(select.Columns, VisitColumnDeclaration);
            ReadOnlyCollection<OrderExpression> orderBy = Visit(select.OrderBy, VisitOrderBy);
            ReadOnlyCollection<Expression> groupBy = Visit(select.GroupBy, e => MakeSqlValue(Visit(e)));

            if (top != select.Top || from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy)
                return new SelectExpression(select.Alias, select.IsDistinct, top, columns, from, where, orderBy, groupBy, select.SelectOptions);

            return select;
        }

        protected internal override Expression VisitIsNull(IsNullExpression isNull)
        {
            var newExpr = MakeSqlValue(Visit(isNull.Expression));
            if (newExpr != isNull.Expression)
                return new IsNullExpression(newExpr);
            return isNull;
        }

        protected internal override Expression VisitIsNotNull(IsNotNullExpression isNotNull)
        {
            var newExpr = MakeSqlValue(Visit(isNotNull.Expression));
            if (newExpr != isNotNull.Expression)
                return new IsNotNullExpression(newExpr);
            return isNotNull;
        }

        protected internal override ColumnDeclaration VisitColumnDeclaration(ColumnDeclaration c)
        {
            var e = MakeSqlValue(Visit(c.Expression));
            if (e == c.Expression)
                return c;

            return new ColumnDeclaration(c.Name, e);
        }

        protected internal override OrderExpression VisitOrderBy(OrderExpression o)
        {
            var e = MakeSqlValue(Visit(o.Expression));
            if (e == o.Expression)
                return o;

            return new OrderExpression(o.OrderType, e);
        }

        protected internal override Expression VisitUpdate(UpdateExpression update)
        {
            var source = Visit(update.Source);
            var where = Visit(update.Where);
            var assigments = Visit(update.Assigments, c =>
            {
                var exp = MakeSqlValue(Visit(c.Expression));
                if (exp != c.Expression)
                    return new ColumnAssignment(c.Column, exp);
                return c;
            });
            if (source != update.Source || where != update.Where || assigments != update.Assigments)
                return new UpdateExpression(update.Table, update.UseHistoryTable, (SelectExpression)source, where, assigments);
            return update;
        }

        protected internal override Expression VisitJoin(JoinExpression join)
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

        protected internal override Expression VisitProjection(ProjectionExpression proj)
        {
            SelectExpression source;
            using (InSql())
            {
                source = (SelectExpression)this.Visit(proj.Select);
            }
            Expression projector = this.Visit(proj.Projector);

            if (source != proj.Select || projector != proj.Projector)
            {
                return new ProjectionExpression(source, projector, proj.UniqueFunction, proj.Type);
            }
            return proj;
        }

        protected internal override Expression VisitCommandAggregate(CommandAggregateExpression cea)
        {
            using (InSql())
                return base.VisitCommandAggregate(cea);
        }
    }
}
