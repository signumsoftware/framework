using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Diagnostics;
using Signum.Utilities.ExpressionTrees; 


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
                case DbExpressionType.AggregateSubquery:
                    return this.VisitAggregateSubquery((AggregateSubqueryExpression)exp);
                case DbExpressionType.SqlEnum:
                    return this.VisitSqlEnum((SqlEnumExpression)exp);
                case DbExpressionType.SqlFunction:
                    return this.VisitSqlFunction((SqlFunctionExpression)exp);
                case DbExpressionType.SqlConstant:
                    return this.VisitSqlConstant((SqlConstantExpression)exp); 
                case DbExpressionType.Case:
                    return this.VisitCase((CaseExpression)exp); 
                case DbExpressionType.RowNumber:
                    return this.VisitRowNumber((RowNumberExpression)exp);
                case DbExpressionType.Like:
                    return this.VisitLike((LikeExpression)exp);
                case DbExpressionType.In:
                case DbExpressionType.Scalar:
                case DbExpressionType.Exists:
                    return this.VisitSubquery((SubqueryExpression)exp);
                case DbExpressionType.IsNull:
                    return this.VisitIsNull((IsNullExpression)exp);
                case DbExpressionType.IsNotNull:
                    return this.VisitIsNotNull((IsNotNullExpression)exp);
                case DbExpressionType.Delete:
                    return this.VisitDelete((DeleteExpression)exp);
                case DbExpressionType.Update:
                    return this.VisitUpdate((UpdateExpression)exp);
                case DbExpressionType.CommandAggregate:
                    return this.VisitCommandAggregate((CommandAggregateExpression)exp);
                case DbExpressionType.SelectRowCount:
                    return this.VisitSelectRowCount((SelectRowCountExpression)exp);
                case DbExpressionType.FieldInit:
                    return this.VisitFieldInit((FieldInitExpression)exp);
                case DbExpressionType.EmbeddedFieldInit:
                    return this.VisitEmbeddedFieldInit((EmbeddedFieldInitExpression)exp); 
                case DbExpressionType.ImplementedBy:
                    return this.VisitImplementedBy((ImplementedByExpression)exp);
                case DbExpressionType.ImplementedByAll:
                    return this.VisitImplementedByAll((ImplementedByAllExpression)exp);     
                case  DbExpressionType.LiteReference:
                    return this.VisitLiteReference((LiteReferenceExpression)exp);
                case DbExpressionType.MList:
                    return this.VisitMList((MListExpression)exp);

                default:
                    return base.Visit(exp);
            }
        }

        protected virtual Expression VisitCommandAggregate(CommandAggregateExpression cea)
        {
            var commands = VisitCommands(cea.Commands);
            if (cea.Commands != commands)
                return new CommandAggregateExpression(commands);
            return cea; 
        }

        protected IEnumerable<CommandExpression> VisitCommands(ReadOnlyCollection<CommandExpression> commands)
        {
            return commands.NewIfChange(c => (CommandExpression)Visit(c));
        }

        protected virtual Expression VisitDelete(DeleteExpression delete)
        {
            var source = Visit(delete.Source);
            var where = Visit(delete.Where);
            if (source != delete.Source || where != delete.Where)
                return new DeleteExpression(delete.Table, (SourceExpression)source, where);
            return delete;
        }

        protected virtual Expression VisitUpdate(UpdateExpression update)
        {
            var source = Visit(update.Source); 
            var where = Visit(update.Where);
            var assigments = VisitColumnAssigments(update.Assigments);
            if(source != update.Source || where != update.Where || assigments != update.Assigments)
                return new UpdateExpression(update.Table, (SourceExpression)source, where, assigments);
            return update;
        }

        protected IEnumerable<ColumnAssignment> VisitColumnAssigments(ReadOnlyCollection<ColumnAssignment> columns)
        {
            return columns.NewIfChange(c =>
                {
                    var col = (ColumnExpression)Visit(c.Column);
                    var exp = Visit(c.Expression);
                    if (col != c.Column || exp != c.Expression)
                        return new ColumnAssignment(col, exp);
                    return c;
                });
        }

        protected virtual Expression VisitSelectRowCount(SelectRowCountExpression src)
        {
            return src;
        }

        protected virtual Expression VisitMList(MListExpression ml)
        {
            var newBackID = Visit(ml.BackID);
            if (newBackID != ml.BackID)
                return new MListExpression(ml.Type, newBackID, ml.RelationalTable);
            return ml;
        }

        protected virtual Expression VisitLiteReference(LiteReferenceExpression lite)
        {
            var newRef = Visit(lite.Reference);
            var newToStr = Visit(lite.ToStr);
            var newId = Visit(lite.Id);
            var newTypeId = Visit(lite.TypeId);
            if (newRef != lite.Reference || newToStr != lite.ToStr || newId != lite.Id || newTypeId != lite.TypeId)
                return new LiteReferenceExpression(lite.Type, newRef, newId, newToStr, newTypeId);
            return lite;
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

        protected virtual Expression VisitImplementedByAll(ImplementedByAllExpression reference)
        {
            var id = Visit(reference.Id);
            var typeId = Visit(reference.TypeId);
            var implementations = reference.Implementations
               .NewIfChange(ri => Visit(ri.Field).Map(r => r == ri.Field ? ri : new ImplementationColumnExpression(ri.Type, (FieldInitExpression)r)));

            var token = VisitProjectionToken(reference.Token);

            if (id != reference.Id || typeId != reference.TypeId || implementations != reference.Implementations || token != reference.Token)
                return new ImplementedByAllExpression(reference.Type, id, typeId, token) { Implementations = implementations };
            return reference;
        }

        protected virtual Expression VisitImplementedBy(ImplementedByExpression reference)
        {
            var implementations = reference.Implementations
                .NewIfChange(ri => Visit(ri.Field).Map(r => r == ri.Field ? ri : new ImplementationColumnExpression(ri.Type, (FieldInitExpression)r)));

            var propertyBindings = reference.PropertyBindings.NewIfChange(pb => Visit(pb.Binding).Map(r => r == pb.Binding ? pb : new PropertyBinding(pb.PropertyInfo, r)));

            if (implementations != reference.Implementations || propertyBindings != reference.PropertyBindings)
                return new ImplementedByExpression(reference.Type, implementations) { PropertyBindings = propertyBindings };
            return reference;
        }

        protected virtual ProjectionToken VisitProjectionToken(ProjectionToken token)
        {
            return token;
        }

        protected virtual Expression VisitFieldInit(FieldInitExpression fie)
        {
            var bindings = fie.Bindings.NewIfChange(fb => Visit(fb.Binding).Map(r => r == fb.Binding ? fb : new FieldBinding(fb.FieldInfo, r)));
          
            var id = Visit(fie.ExternalId);
            var typeId = Visit(fie.TypeId);
            var other = Visit(fie.OtherCondition);

            var token = VisitProjectionToken(fie.Token);

            if (fie.Bindings != bindings || fie.ExternalId != id || fie.OtherCondition != other || fie.Token != token || fie.TypeId != typeId)
                return new FieldInitExpression(fie.Type, fie.TableAlias, id, typeId, other, token) { Bindings = bindings };

            return fie;
        }

        protected virtual Expression VisitEmbeddedFieldInit(EmbeddedFieldInitExpression efie)
        {
            var bindings = efie.Bindings.NewIfChange(fb => Visit(fb.Binding).Map(r => r == fb.Binding ? fb : new FieldBinding(fb.FieldInfo, r)));
          
            if (efie.Bindings != bindings)
            {
                return new EmbeddedFieldInitExpression(efie.Type, bindings);
            }
            return efie;
        }

        protected virtual Expression VisitLike(LikeExpression like)
        {
            Expression exp = Visit(like.Expression);
            Expression pattern = Visit(like.Pattern);
            if (exp != like.Expression || pattern != like.Pattern)
                return new LikeExpression(exp, pattern);
            return like;
        }

        protected virtual Expression VisitSubquery(SubqueryExpression subquery)
        {
            switch ((DbExpressionType)subquery.NodeType)
            {
                case DbExpressionType.Scalar:
                    return this.VisitScalar((ScalarExpression)subquery);
                case DbExpressionType.Exists:
                    return this.VisitExists((ExistsExpression)subquery);
                case DbExpressionType.In:
                    return this.VisitIn((InExpression)subquery);
            }
            return subquery;
        }

        protected virtual Expression VisitScalar(ScalarExpression scalar)
        {
            var select = (SelectExpression)this.Visit(scalar.Select);
            if (select != scalar.Select)
                return new ScalarExpression(scalar.Type, select);
            return scalar;
        }

        protected virtual Expression VisitExists(ExistsExpression exists)
        {
            var select = (SelectExpression)this.Visit(exists.Select);
            if (select != exists.Select)
                return new ExistsExpression(select);
            return exists;
        }

        protected virtual Expression VisitIn(InExpression @in)
        {
            var expression = this.Visit(@in.Expression);
            var select = (SelectExpression)this.Visit(@in.Select);
            if (expression != @in.Expression || select != @in.Select)
            {
                if (select != null)
                    return new InExpression(expression, select);
                else
                    return InExpression.FromValues(expression, @in.Values);
            }
            return @in;
        }

        protected virtual Expression VisitIsNull(IsNullExpression isNull)
        {
            var newExpr = Visit(isNull.Expression);
            if (newExpr != isNull.Expression)
                return new IsNullExpression(newExpr);
            return isNull;
        }

        protected virtual Expression VisitIsNotNull(IsNotNullExpression isNotNull)
        {
            var newExpr = Visit(isNotNull.Expression);
            if (newExpr != isNotNull.Expression)
                return new IsNotNullExpression(newExpr);
            return isNotNull;
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

        protected virtual Expression VisitAggregateSubquery(AggregateSubqueryExpression aggregate)
        {
            var subquery = (ScalarExpression)this.Visit(aggregate.AggregateAsSubquery);
            if (subquery != aggregate.AggregateAsSubquery)
                return new AggregateSubqueryExpression(aggregate.GroupByAlias, aggregate.AggregateInGroupSelect, subquery);
            return aggregate;
        }

        protected virtual Expression VisitSelect(SelectExpression select)
        {
            Expression top = this.Visit(select.Top);
            SourceExpression from = this.VisitSource(select.From);
            Expression where = this.Visit(select.Where);
            ReadOnlyCollection<ColumnDeclaration> columns = this.VisitColumnDeclarations(select.Columns);
            ReadOnlyCollection<OrderExpression> orderBy = this.VisitOrderBy(select.OrderBy);
            ReadOnlyCollection<Expression> groupBy = this.VisitGroupBy(select.GroupBy);

            if (top != select.Top || from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy)
                return new SelectExpression(select.Alias, select.Distinct, top, columns, from, where, orderBy, groupBy);

            return select;
        }

        protected virtual Expression VisitJoin(JoinExpression join)
        {
            SourceExpression left = this.VisitSource(join.Left);
            SourceExpression right = this.VisitSource(join.Right);
            Expression condition = this.Visit(join.Condition);
            if (left != join.Left || right != join.Right || condition != join.Condition)
            {
                return new JoinExpression(join.JoinType, left, right, condition);
            }
            return join;
        }

        protected virtual SourceExpression VisitSource(SourceExpression source)
        {
            return (SourceExpression)this.Visit(source);
        }

        protected virtual Expression VisitProjection(ProjectionExpression proj)
        {
            SelectExpression source = (SelectExpression)this.Visit(proj.Source);
            Expression projector = this.Visit(proj.Projector);
            ProjectionToken token = VisitProjectionToken(proj.Token);

            if (source != proj.Source || projector != proj.Projector || token != proj.Token)
            {
                return new ProjectionExpression(source, projector, proj.UniqueFunction, token);
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

        protected virtual Expression VisitSqlConstant(SqlConstantExpression sce)
        {
            return sce;
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
