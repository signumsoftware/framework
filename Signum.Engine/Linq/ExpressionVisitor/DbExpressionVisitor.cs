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
        protected internal virtual Expression VisitCommandAggregate(CommandAggregateExpression cea)
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

        protected internal virtual Expression VisitDelete(DeleteExpression delete)
        {
            var source = VisitSource(delete.Source);
            var where = Visit(delete.Where);
            if (source != delete.Source || where != delete.Where)
                return new DeleteExpression(delete.Table, (SourceWithAliasExpression)source, where);
            return delete;
        }

        protected internal virtual Expression VisitUpdate(UpdateExpression update)
        {
            var source = VisitSource(update.Source); 
            var where = Visit(update.Where);
            var assigments = update.Assigments.NewIfChange(VisitColumnAssigment);
            if(source != update.Source || where != update.Where || assigments != update.Assigments)
                return new UpdateExpression(update.Table, (SourceWithAliasExpression)source, where, assigments);
            return update;
        }

        protected internal virtual Expression VisitInsertSelect(InsertSelectExpression insertSelect)
        {
            var source = VisitSource(insertSelect.Source);
            var assigments = insertSelect.Assigments.NewIfChange(VisitColumnAssigment);
            if (source != insertSelect.Source ||  assigments != insertSelect.Assigments)
                return new InsertSelectExpression(insertSelect.Table, (SourceWithAliasExpression)source, assigments);
            return insertSelect;
        }

        protected internal virtual ColumnAssignment VisitColumnAssigment(ColumnAssignment c)
        {
            var exp = Visit(c.Expression);
            if (exp != c.Expression)
                return new ColumnAssignment(c.Column, exp);
            return c;
        }

        protected internal virtual Expression VisitSelectRowCount(SelectRowCountExpression src)
        {
            return src;
        }

        protected internal virtual Expression VisitLiteReference(LiteReferenceExpression lite)
        {
            var newRef = Visit(lite.Reference);
            var newToStr = Visit(lite.CustomToStr);
            if (newRef != lite.Reference || newToStr != lite.CustomToStr)
                return new LiteReferenceExpression(lite.Type,  newRef, newToStr);
            return lite;
        }

        protected internal virtual Expression VisitLiteValue(LiteValueExpression lite)
        {
            var newTypeId = Visit(lite.TypeId);
            var newId = Visit(lite.Id);
            var newToStr = Visit(lite.ToStr);
            if (newTypeId != lite.TypeId || newId != lite.Id || newToStr != lite.ToStr)
                return new LiteValueExpression(lite.Type, newTypeId, newId, newToStr);
            return lite;
        }

        protected internal virtual Expression VisitTypeFieldInit(TypeEntityExpression typeFie)
        {
            var externalId = Visit(typeFie.ExternalId);

            if (externalId != typeFie.ExternalId)
                return new TypeEntityExpression(externalId, typeFie.TypeValue);

            return typeFie;
        }

        protected internal virtual Expression VisitTypeImplementedBy(TypeImplementedByExpression typeIb)
        {
            var implementations = typeIb.TypeImplementations.NewIfChange(eid => Visit(eid));

            if (implementations != typeIb.TypeImplementations)
                return new TypeImplementedByExpression(implementations);
            return typeIb;
        }

        protected internal virtual Expression VisitTypeImplementedByAll(TypeImplementedByAllExpression typeIba)
        {
            var column = Visit(typeIba.TypeColumn);

            if (column != typeIba.TypeColumn)
                return new TypeImplementedByAllExpression(column);

            return typeIba;
        }

        protected internal virtual Expression VisitMList(MListExpression ml)
        {
            var newBackID = Visit(ml.BackID);
            if (newBackID != ml.BackID)
                return new MListExpression(ml.Type, newBackID, ml.TableMList);
            return ml;
        }

        protected internal virtual Expression VisitMListProjection(MListProjectionExpression mlp)
        {
            var proj = (ProjectionExpression)Visit(mlp.Projection);
            if (proj != mlp.Projection)
                return new MListProjectionExpression(mlp.Type, proj);
            return mlp;
        }

        protected internal virtual Expression VisitMListElement(MListElementExpression mle)
        {
            var rowId = Visit(mle.RowId);
            var parent = (EntityExpression)Visit(mle.Parent);
            var order = Visit(mle.Order);
            var element = Visit(mle.Element);
            if (rowId != mle.RowId || parent != mle.Parent || order != mle.Order || element != mle.Element)
                return new MListElementExpression(rowId, parent, order, element, mle.Table);
            return mle;
        }

        protected internal virtual Expression VisitSqlEnum(SqlEnumExpression sqlEnum)
        {
            return sqlEnum;
        }

        protected internal virtual Expression VisitSqlCast(SqlCastExpression castExpr)
        {
            var expression = Visit(castExpr.Expression);
            if (expression != castExpr.Expression)
                return new SqlCastExpression(castExpr.Type, expression,castExpr.SqlDbType);
            return castExpr;
        }

        protected internal virtual Expression VisitTable(TableExpression table)
        {
            return table;
        }

        protected internal virtual Expression VisitColumn(ColumnExpression column)
        {
            return column;
        }

        protected internal virtual Expression VisitImplementedByAll(ImplementedByAllExpression iba)
        {
            var id = Visit(iba.Id);
            var typeId = (TypeImplementedByAllExpression)Visit(iba.TypeId);

            if (id != iba.Id || typeId != iba.TypeId)
                return new ImplementedByAllExpression(iba.Type, id, typeId);
            return iba;
        }

        protected internal virtual Expression VisitImplementedBy(ImplementedByExpression ib)
        {
            var implementations = ib.Implementations.NewIfChange(v => (EntityExpression)Visit(v));

            if (implementations != ib.Implementations)
                return new ImplementedByExpression(ib.Type, ib.Strategy, implementations);
            return ib;
        }

        protected internal virtual Expression VisitEntity(EntityExpression ee)
        {
            var bindings = ee.Bindings.NewIfChange(VisitFieldBinding);
            var mixins = ee.Mixins.NewIfChange(VisitMixinEntity);

            var id = Visit(ee.ExternalId);

            if (ee.Bindings != bindings || ee.ExternalId != id || ee.Mixins != mixins)
                return new EntityExpression(ee.Type, id, ee.TableAlias, bindings, mixins, ee.AvoidExpandOnRetrieving);

            return ee;
        }

        protected internal virtual Expression VisitEmbeddedEntity(EmbeddedEntityExpression eee)
        {
            var bindings = eee.Bindings.NewIfChange(VisitFieldBinding);
            var hasValue = Visit(eee.HasValue);

            if (eee.Bindings != bindings || eee.HasValue != hasValue)
            {
                return new EmbeddedEntityExpression(eee.Type, hasValue, bindings, eee.FieldEmbedded);
            }
            return eee;
        }

        protected internal virtual MixinEntityExpression VisitMixinEntity(MixinEntityExpression me)
        {
            var bindings = me.Bindings.NewIfChange(VisitFieldBinding);

            if (me.Bindings != bindings)
            {
                return new MixinEntityExpression(me.Type, bindings, me.FieldMixin);
            }
            return me;
        }

        protected internal virtual FieldBinding VisitFieldBinding(FieldBinding fb)
        {
            var r = Visit(fb.Binding);

            if(r == fb.Binding)
                return fb; 

            return new FieldBinding(fb.FieldInfo, r); 
        }

        protected internal virtual Expression VisitLike(LikeExpression like)
        {
            Expression exp = Visit(like.Expression);
            Expression pattern = Visit(like.Pattern);
            if (exp != like.Expression || pattern != like.Pattern)
                return new LikeExpression(exp, pattern);
            return like;
        }

        protected internal virtual Expression VisitSubquery(SubqueryExpression subquery)
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

        protected internal virtual Expression VisitScalar(ScalarExpression scalar)
        {
            var select = (SelectExpression)this.Visit(scalar.Select);
            if (select != scalar.Select)
                return new ScalarExpression(scalar.Type, select);
            return scalar;
        }

        protected internal virtual Expression VisitExists(ExistsExpression exists)
        {
            var select = (SelectExpression)this.Visit(exists.Select);
            if (select != exists.Select)
                return new ExistsExpression(select);
            return exists;
        }

        protected internal virtual Expression VisitIn(InExpression @in)
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

        protected internal virtual Expression VisitIsNull(IsNullExpression isNull)
        {
            var newExpr = Visit(isNull.Expression);
            if (newExpr != isNull.Expression)
                return new IsNullExpression(newExpr);
            return isNull;
        }

        protected internal virtual Expression VisitIsNotNull(IsNotNullExpression isNotNull)
        {
            var newExpr = Visit(isNotNull.Expression);
            if (newExpr != isNotNull.Expression)
                return new IsNotNullExpression(newExpr);
            return isNotNull;
        }

        protected internal virtual Expression VisitRowNumber(RowNumberExpression rowNumber)
        {
            var orderBys = rowNumber.OrderBy.NewIfChange(VisitOrderBy);
            if (orderBys != rowNumber.OrderBy)
                return new RowNumberExpression(orderBys);
            return rowNumber;
        }

        protected internal virtual Expression VisitAggregate(AggregateExpression aggregate)
        {
            Expression source = Visit(aggregate.Source);
            if (source != aggregate.Source)
                return new AggregateExpression(aggregate.Type, source, aggregate.AggregateFunction);
            return aggregate;
        }

        protected internal virtual Expression VisitAggregateRequest(AggregateRequestsExpression request)
        {
            var ag = (AggregateExpression)this.Visit(request.Aggregate);
            if (ag != request.Aggregate)
                return new AggregateRequestsExpression(request.GroupByAlias, ag);

            return request;
        }

        protected internal virtual Expression VisitSelect(SelectExpression select)
        {
            Expression top = this.Visit(select.Top);
            SourceExpression from = this.VisitSource(select.From);
            Expression where = this.Visit(select.Where);
            ReadOnlyCollection<ColumnDeclaration> columns = select.Columns.NewIfChange(VisitColumnDeclaration);
            ReadOnlyCollection<OrderExpression> orderBy = select.OrderBy.NewIfChange(VisitOrderBy);
            ReadOnlyCollection<Expression> groupBy = select.GroupBy.NewIfChange(Visit);

            if (top != select.Top || from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy)
                return new SelectExpression(select.Alias, select.IsDistinct, top, columns, from, where, orderBy, groupBy, select.SelectOptions);

            return select;
        }

        protected internal virtual Expression VisitJoin(JoinExpression join)
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

        protected internal virtual Expression VisitSetOperator(SetOperatorExpression set)
        {
            SourceWithAliasExpression left = (SourceWithAliasExpression)this.VisitSource(set.Left);
            SourceWithAliasExpression right = (SourceWithAliasExpression)this.VisitSource(set.Right);
            if (left != set.Left || right != set.Right)
            {
                return new SetOperatorExpression(set.Operator, left, right, set.Alias);
            }
            return set;
        }

        protected internal virtual SourceExpression VisitSource(SourceExpression source)
        {
            return (SourceExpression)this.Visit(source);
        }

        protected internal virtual Expression VisitProjection(ProjectionExpression proj)
        {
            SelectExpression source = (SelectExpression)this.Visit(proj.Select);
            Expression projector = this.Visit(proj.Projector);

            if (source != proj.Select || projector != proj.Projector)
                return new ProjectionExpression(source, projector, proj.UniqueFunction, proj.Type);

            return proj;
        }

        protected internal virtual Expression VisitChildProjection(ChildProjectionExpression child)
        {
            ProjectionExpression proj = (ProjectionExpression)this.Visit(child.Projection);
            Expression key = this.Visit(child.OuterKey);

            if (proj != child.Projection || key != child.OuterKey)
            {
                return new ChildProjectionExpression(proj, key, child.IsLazyMList, child.Type, child.Token);
            }
            return child;
        }

        protected internal virtual Expression VisitSqlFunction(SqlFunctionExpression sqlFunction)
        {
            Expression obj = Visit(sqlFunction.Object);
            ReadOnlyCollection<Expression> args = sqlFunction.Arguments.NewIfChange(a => Visit(a));
            if (args != sqlFunction.Arguments || obj != sqlFunction.Object)
                return new SqlFunctionExpression(sqlFunction.Type, obj, sqlFunction.SqlFunction, args); 
            return sqlFunction;
        }

        protected internal virtual Expression VisitSqlTableValuedFunction(SqlTableValuedFunctionExpression sqlFunction)
        {
            ReadOnlyCollection<Expression> args = sqlFunction.Arguments.NewIfChange(a => Visit(a));
            if (args != sqlFunction.Arguments)
                return new SqlTableValuedFunctionExpression(sqlFunction.SqlFunction, sqlFunction.Table, sqlFunction.Alias, args);
            return sqlFunction;
        }

        protected internal virtual Expression VisitSqlConstant(SqlConstantExpression sce)
        {
            return sce;
        }

        protected internal virtual Expression VisitCase(CaseExpression cex)
        {
            var newWhens = cex.Whens.NewIfChange(w => VisitWhen(w));
            var newDefault = Visit(cex.DefaultValue);

            if (newWhens != cex.Whens || newDefault != cex.DefaultValue)
                return new CaseExpression(newWhens, newDefault);
            return cex;
        }

        protected internal virtual When VisitWhen(When when)
        {
            var newCondition = Visit(when.Condition);
            var newValue = Visit(when.Value);
            if (when.Condition != newCondition || newValue != when.Value)
                return new When(newCondition, newValue);
            return when;
        }

        protected internal virtual ColumnDeclaration VisitColumnDeclaration(ColumnDeclaration c)
        {
            var e = Visit(c.Expression);
            if (e == c.Expression)
                return c;

            return new ColumnDeclaration(c.Name, e);
        }

        protected internal virtual OrderExpression VisitOrderBy(OrderExpression o)
        {
            var e = Visit(o.Expression);
            if (e == o.Expression)
                return o;

            return new OrderExpression(o.OrderType, e);
        }
    }
}
