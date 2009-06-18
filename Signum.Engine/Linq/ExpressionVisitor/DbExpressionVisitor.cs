using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Diagnostics; 


namespace Signum.Engine.Linq
{
    /// <summary>
    /// An extended expression visitor including custom DbExpression nodes
    /// </summary>
    internal class DbExpressionVisitor : ExpressionVisitor
    {
        protected override Expression Visit(Expression exp)
        {
            if (exp == null)
            {
                return null;
            }
            switch ((DbExpressionType)exp.NodeType)
            {
                case DbExpressionType.Table:
                    return this.VisitTable((TableExpression)exp);
                case DbExpressionType.Column:
                    return this.VisitColumn((ColumnExpression)exp);
                case DbExpressionType.Select:
                    return this.VisitSelect((SelectExpression)exp);
                case DbExpressionType.Join:
                    return this.VisitJoin((JoinExpression)exp);
                case DbExpressionType.Projection:
                    return this.VisitProjection((ProjectionExpression)exp);
                case DbExpressionType.Aggregate:
                    return this.VisitAggregate((AggregateExpression)exp);
                case DbExpressionType.SqlEnum:
                    return this.VisitSqlEnum((SqlEnumExpression)exp);
                case DbExpressionType.SqlFunction:
                    return this.VisitSqlFunction((SqlFunctionExpression)exp);
                case DbExpressionType.Case:
                    return this.VisitCase((CaseExpression)exp); 
                case DbExpressionType.RowNumber:
                    return this.VisitRowNumber((RowNumberExpression)exp);
                case DbExpressionType.SetOperation:
                    return this.VisitSetOperation((SetOperationExpression)exp);
                case DbExpressionType.Like:
                    return this.VisitLike((LikeExpression)exp);
                case DbExpressionType.In:
                    return this.VisitIn((InExpression)exp); 
                case DbExpressionType.FieldInit:
                    return this.VisitFieldInit((FieldInitExpression)exp);
                case DbExpressionType.ImplementedBy:
                    return this.VisitImplementedBy((ImplementedByExpression)exp);
                case DbExpressionType.ImplementedByAll:
                    return this.VisitImplementedByAll((ImplementedByAllExpression)exp);
                case DbExpressionType.Enum:
                    return this.VisitEnumExpression((EnumExpression)exp);          
                case  DbExpressionType.LazyReference:
                    return this.VisitLazyReference((LazyReferenceExpression)exp);
                case DbExpressionType.LazyLiteral:
                    return this.VisitLazyLiteral((LazyLiteralExpression)exp);
                case DbExpressionType.MList:
                    return this.VisitMList((MListExpression)exp);

                default:
                    return base.Visit(exp);
            }
        }

        protected virtual Expression VisitMList(MListExpression ml)
        {
            var newBackID = Visit(ml.BackID);
            if (newBackID != ml.BackID)
                return new MListExpression(ml.Type, newBackID, ml.RelationalTable);
            return ml;
        }

        protected virtual Expression VisitLazyReference(LazyReferenceExpression lazy)
        {
            var newRef = Visit(lazy.Reference);
            if (newRef != lazy.Reference)
                return new LazyReferenceExpression(lazy.Type, newRef);
            return lazy;
        }


        protected virtual Expression VisitLazyLiteral(LazyLiteralExpression lazy)
        {
            var id = (ColumnExpression)Visit(lazy.ID);
            var toStr = (ColumnExpression)Visit(lazy.ToStr);
            if (id != lazy.ID || toStr != lazy.ToStr)
                return new LazyLiteralExpression(lazy.Type, lazy.RuntimeType, id, toStr);
            return lazy;
        }


        protected virtual Expression VisitSqlEnum(SqlEnumExpression sqlEnum)
        {
            return sqlEnum;
        }

        protected virtual Expression VisitTable(TableExpression table)
        {
            return table;
        }

        protected virtual Expression VisitColumn(ColumnExpression column)
        {
            return column;
        }

        protected virtual Expression VisitEnumExpression(EnumExpression enumExp)
        {
            var id = (ColumnExpression)Visit(enumExp.ID);
            if (id != enumExp.ID)
                return new EnumExpression(enumExp.Type, id);
            return enumExp;
        }

        protected virtual Expression VisitImplementedByAll(ImplementedByAllExpression reference)
        {
            var id = (ColumnExpression)Visit(reference.ID);
            var typeId = (ColumnExpression)Visit(reference.TypeID);
            var newImple = reference.Implementations
               .NewIfChange(ri => Visit(ri.Field).Map(r => r == ri.Field ? ri : new ImplementationColumnExpression(ri.Type, (FieldInitExpression)r)));

            if (id != reference.ID || typeId != reference.TypeID || newImple != reference.Implementations)
                return new ImplementedByAllExpression(reference.Type, id, typeId) {  Implementations = newImple};
            return reference;
        }

        protected virtual Expression VisitImplementedBy(ImplementedByExpression reference)
        {
            var newImple = reference.Implementations
                .NewIfChange(ri => Visit(ri.Field).Map(r => r == ri.Field ? ri : new ImplementationColumnExpression(ri.Type, (FieldInitExpression)r)));

            if (newImple != reference.Implementations)
                return new ImplementedByExpression(reference.Type, newImple);
            return reference;
        }

        protected virtual Expression VisitFieldInit(FieldInitExpression fieldInit)
        {
            var newFields = fieldInit.Bindings.NewIfChange(fb => Visit(fb.Binding).Map(r => r == fb.Binding ? fb : new FieldBinding(fb.FieldInfo, r)));
            var id = Visit(fieldInit.ID);
            var alias = VisitFieldInitAlias(fieldInit.Alias);
            if (fieldInit.Bindings != newFields || fieldInit.ID != id)
            {
                return new FieldInitExpression(fieldInit.Type, alias, id) { Bindings = newFields };
            }
            return fieldInit;
        }

        protected virtual string VisitFieldInitAlias(string alias)
        {
            return alias; 
        }

        protected virtual Expression VisitSetOperation(SetOperationExpression setOperationExp)
        {
            SelectExpression left = (SelectExpression)Visit(setOperationExp.Left);
            SelectExpression right = (SelectExpression)Visit(setOperationExp.Right);
            if (setOperationExp.Left != left || setOperationExp.Right != right)
                return new SetOperationExpression(setOperationExp.Type, setOperationExp.Alias, setOperationExp.SetOperation, left, right); 

            return setOperationExp;
        }

        protected virtual Expression VisitLike(LikeExpression like)
        {
            Expression exp = Visit(like.Expression);
            Expression pattern = Visit(like.Pattern);
            if (exp != like.Expression || pattern != like.Pattern)
                return new LikeExpression(exp, pattern);
            return like;
        }


        protected virtual Expression VisitIn(InExpression inExpression)
        {
            Expression exp = Visit(inExpression.Expression);
            if (exp != inExpression.Expression)
                return new InExpression(exp, inExpression.Values);
            return inExpression;
        }

        protected virtual Expression VisitRowNumber(RowNumberExpression rowNumber)
        {
            var orderBys = VisitOrderBy(rowNumber.OrderBy);
            if (orderBys != rowNumber.OrderBy)
                return new RowNumberExpression(orderBys);
            return rowNumber;
        }

        protected virtual Expression VisitAggregate(AggregateExpression aggregate)
        {
            Expression source = Visit(aggregate.Source);
            if (source != aggregate.Source)
                return new AggregateExpression(aggregate.Type, source, aggregate.AggregateFunction);
            return aggregate;
        }

        protected virtual Expression VisitSelect(SelectExpression select)
        {
            Expression top = this.Visit(select.Top);
            Expression from = this.VisitSource(select.From);
            Expression where = this.Visit(select.Where);
            ReadOnlyCollection<ColumnDeclaration> columns = this.VisitColumnDeclarations(select.Columns);
            ReadOnlyCollection<OrderExpression> orderBy = this.VisitOrderBy(select.OrderBy);
            ReadOnlyCollection<Expression> groupBy = this.VisitGroupBy(select.GroupBy);
                if (top != select.Top ||from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy)
            {
                return new SelectExpression(select.Type, select.Alias, select.Distinct, top, columns, from, where, orderBy, groupBy, select.GroupOf);
            }
            return select;
        }

        protected virtual Expression VisitJoin(JoinExpression join)
        {
            Expression left = this.VisitSource(join.Left);
            Expression right = this.VisitSource(join.Right);
            Expression condition = this.Visit(join.Condition);
            if (left != join.Left || right != join.Right || condition != join.Condition)
            {
                return new JoinExpression(join.Type, join.JoinType, left, right, condition, join.IsSingleRow);
            }
            return join;
        }

        protected virtual Expression VisitSource(Expression source)
        {
            return this.Visit(source);
        }

        protected virtual Expression VisitProjection(ProjectionExpression proj)
        {
            SelectExpression source = (SelectExpression)this.Visit(proj.Source);
            Expression projector = this.Visit(proj.Projector);
            if (source != proj.Source || projector != proj.Projector)
            {
                return new ProjectionExpression(proj.Type, source, projector, proj.UniqueFunction);
            }
            return proj;
        }

        protected virtual Expression VisitSqlFunction(SqlFunctionExpression sqlFunction)
        {
            ReadOnlyCollection<Expression> args = sqlFunction.Arguments.NewIfChange(a => Visit(a));
            if(args != sqlFunction.Arguments)
                return new SqlFunctionExpression(sqlFunction.Type, sqlFunction.SqlFunction, args); 
            return sqlFunction;
        }

        protected virtual Expression VisitCase(CaseExpression cex)
        {
            var newWhens = cex.Whens.NewIfChange(w => VisitWhen(w));
            var newDefault = Visit(cex.DefaultValue);

            if (newWhens != cex.Whens || newDefault != cex.DefaultValue)
                return new CaseExpression(newWhens, newDefault);
            return cex;
        }

        protected virtual When VisitWhen(When when)
        {
            var newCondition = Visit(when.Condition);
            var newValue = Visit(when.Value);
            if (when.Condition != newCondition || newValue != when.Value)
                return new When(newCondition, newValue);
            return when;
        }

        protected ReadOnlyCollection<ColumnDeclaration> VisitColumnDeclarations(ReadOnlyCollection<ColumnDeclaration> columns)
        {
            return columns.NewIfChange(c => Visit(c.Expression).Map(e => e == c.Expression ? c : new ColumnDeclaration(c.Name, e)));
        }

        protected ReadOnlyCollection<OrderExpression> VisitOrderBy(ReadOnlyCollection<OrderExpression> expressions)
        {
            return expressions.NewIfChange(o => Visit(o.Expression).Map(e => e == o.Expression ? o : new OrderExpression(o.OrderType, e)));
        }

        protected ReadOnlyCollection<Expression> VisitGroupBy(ReadOnlyCollection<Expression> expressions)
        {
            return expressions.NewIfChange(e => Visit(e));
        }
    }
}
