using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using Signum.Utilities;
using System.Diagnostics;
using Signum.Entities;

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
            return Visit(commands, c => (CommandExpression)Visit(c));
        }

        protected internal virtual Expression VisitDelete(DeleteExpression delete)
        {
            var source = VisitSource(delete.Source);
            var where = Visit(delete.Where);
            if (source != delete.Source || where != delete.Where)
                return new DeleteExpression(delete.Table, delete.UseHistoryTable, (SourceWithAliasExpression)source, where, delete.ReturnRowCount);
            return delete;
        }

        protected internal virtual Expression VisitUpdate(UpdateExpression update)
        {
            var source = VisitSource(update.Source);
            var where = Visit(update.Where);
            var assigments = Visit(update.Assigments, VisitColumnAssigment);
            if(source != update.Source || where != update.Where || assigments != update.Assigments)
                return new UpdateExpression(update.Table, update.UseHistoryTable, (SourceWithAliasExpression)source, where, assigments, update.ReturnRowCount);
            return update;
        }

        protected internal virtual Expression VisitInsertSelect(InsertSelectExpression insertSelect)
        {
            var source = VisitSource(insertSelect.Source);
            var assigments = Visit(insertSelect.Assigments, VisitColumnAssigment);
            if (source != insertSelect.Source ||  assigments != insertSelect.Assigments)
                return new InsertSelectExpression(insertSelect.Table, insertSelect.UseHistoryTable, (SourceWithAliasExpression)source, assigments, insertSelect.ReturnRowCount);
            return insertSelect;
        }

        protected internal virtual ColumnAssignment VisitColumnAssigment(ColumnAssignment c)
        {
            var exp = Visit(c.Expression);
            if (exp != c.Expression)
                return new ColumnAssignment(c.Column, exp);
            return c;
        }

        protected internal virtual Expression VisitLiteReference(LiteReferenceExpression lite)
        {
            var newRef = Visit(lite.Reference);
            var newToStr = Visit(lite.CustomToStr);
            if (newRef != lite.Reference || newToStr != lite.CustomToStr)
                return new LiteReferenceExpression(lite.Type,  newRef, newToStr, lite.LazyToStr, lite.EagerEntity);
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

        protected internal virtual Expression VisitTypeEntity(TypeEntityExpression typeFie)
        {
            var externalId = (PrimaryKeyExpression)Visit(typeFie.ExternalId);

            if (externalId != typeFie.ExternalId)
                return new TypeEntityExpression(externalId, typeFie.TypeValue);

            return typeFie;
        }

        [DebuggerStepThrough]
        protected static ReadOnlyDictionary<K, V> Visit<K, V>(ReadOnlyDictionary<K, V> dictionary, Func<V, V> newValue)
            where K : notnull
            where V : class
        {
            Dictionary<K, V>? alternate = null;
            foreach (var k in dictionary.Keys)
            {
                V item = dictionary[k];
                V newItem = newValue(item);
                if (alternate == null && item != newItem)
                {
                    alternate = new Dictionary<K, V>();
                    foreach (var k2 in dictionary.Keys.TakeWhile(k2 => !k2.Equals(k)))
                        alternate.Add(k2, dictionary[k2]);
                }
                if (alternate != null && newItem != null)
                {
                    alternate.Add(k, newItem);
                }
            }
            if (alternate != null)
            {
                return alternate.ToReadOnly();
            }
            return dictionary;
        }

        protected internal virtual Expression VisitTypeImplementedBy(TypeImplementedByExpression typeIb)
        {
            var implementations = Visit(typeIb.TypeImplementations, eid => (PrimaryKeyExpression)Visit(eid));

            if (implementations != typeIb.TypeImplementations)
                return new TypeImplementedByExpression(implementations);
            return typeIb;
        }

        protected internal virtual Expression VisitTypeImplementedByAll(TypeImplementedByAllExpression typeIba)
        {
            var column = (PrimaryKeyExpression)Visit(typeIba.TypeColumn);

            if (column != typeIba.TypeColumn)
                return new TypeImplementedByAllExpression(column);

            return typeIba;
        }

        protected internal virtual Expression VisitMList(MListExpression ml)
        {
            var newBackID = (PrimaryKeyExpression)Visit(ml.BackID);
            var externalPeriod = (IntervalExpression?)Visit(ml.ExternalPeriod);
            if (newBackID != ml.BackID || externalPeriod != ml.ExternalPeriod)
                return new MListExpression(ml.Type, newBackID, externalPeriod, ml.TableMList);
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
            var rowId = (PrimaryKeyExpression)Visit(mle.RowId);
            var parent = (EntityExpression)Visit(mle.Parent);
            var order = Visit(mle.Order);
            var element = Visit(mle.Element);
            var period = (IntervalExpression?)Visit(mle.TablePeriod);
            if (rowId != mle.RowId || parent != mle.Parent || order != mle.Order || element != mle.Element || period != mle.TablePeriod)
                return new MListElementExpression(rowId, parent, order, element, period, mle.Table, mle.Alias);
            return mle;
        }

        protected internal virtual Expression VisitAdditionalField(AdditionalFieldExpression ml)
        {
            var newBackID = (PrimaryKeyExpression)Visit(ml.BackID);
            var mlistRowId = (PrimaryKeyExpression?)Visit(ml.MListRowId);
            var externalPeriod = (IntervalExpression?)Visit(ml.ExternalPeriod);
            if (newBackID != ml.BackID || mlistRowId != ml.MListRowId || externalPeriod != ml.ExternalPeriod)
                return new AdditionalFieldExpression(ml.Type, newBackID, mlistRowId, externalPeriod, ml.Route);
            return ml;
        }

        protected internal virtual Expression VisitSqlLiteral(SqlLiteralExpression sqlEnum)
        {
            return sqlEnum;
        }

        protected internal virtual Expression VisitSqlCast(SqlCastExpression castExpr)
        {
            var expression = Visit(castExpr.Expression);
            if (expression != castExpr.Expression)
                return new SqlCastExpression(castExpr.Type, expression,castExpr.DbType);
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
            var externalPeriod = (IntervalExpression?)Visit(iba.ExternalPeriod);

            if (id != iba.Id || typeId != iba.TypeId || externalPeriod != iba.ExternalPeriod)
                return new ImplementedByAllExpression(iba.Type, id, typeId, externalPeriod);
            return iba;
        }

        protected internal virtual Expression VisitImplementedBy(ImplementedByExpression ib)
        {
            var implementations = Visit(ib.Implementations, v => (EntityExpression)Visit(v));

            if (implementations != ib.Implementations)
                return new ImplementedByExpression(ib.Type, ib.Strategy, implementations);
            return ib;
        }

        protected internal virtual Expression VisitEntity(EntityExpression ee)
        {
            var bindings = Visit(ee.Bindings!, VisitFieldBinding);
            var mixins = Visit(ee.Mixins!, VisitMixinEntity);

            var externalId = (PrimaryKeyExpression)Visit(ee.ExternalId);
            var externalPeriod = (IntervalExpression?)Visit(ee.ExternalPeriod);

            var period = (IntervalExpression?)Visit(ee.TablePeriod);

            if (ee.Bindings != bindings || ee.ExternalId != externalId || ee.ExternalPeriod != externalPeriod || ee.Mixins != mixins || ee.TablePeriod != period)
                return new EntityExpression(ee.Type, externalId, externalPeriod, ee.TableAlias, bindings, mixins, period, ee.AvoidExpandOnRetrieving);

            return ee;
        }

        protected internal virtual EntityContextInfo VisitEntityContextInfo(EntityContextInfo entityContext)
        {
            var entityId = (PrimaryKeyExpression)Visit(entityContext.EntityId);
            var rowId = (PrimaryKeyExpression?)Visit(entityContext.MListRowId);

            if(entityId != entityContext.EntityId || rowId != entityContext.MListRowId)
                return new EntityContextInfo(entityId, rowId);

            return entityContext;
        }

        protected internal virtual Expression VisitEmbeddedEntity(EmbeddedEntityExpression eee)
        {
            var bindings = Visit(eee.Bindings, VisitFieldBinding);
            var mixins = eee.Mixins == null ? null : Visit(eee.Mixins, VisitMixinEntity);
            var hasValue = Visit(eee.HasValue);
            var entityContext = eee.EntityContext == null ? null : VisitEntityContextInfo(eee.EntityContext);

            if (eee.Bindings != bindings || eee.Mixins != mixins ||  eee.HasValue != hasValue || eee.EntityContext != entityContext)
            {
                return new EmbeddedEntityExpression(eee.Type, hasValue, bindings, mixins, eee.FieldEmbedded, eee.ViewTable, entityContext);
            }
            return eee;
        }

        protected internal virtual MixinEntityExpression VisitMixinEntity(MixinEntityExpression me)
        {
            var bindings = Visit(me.Bindings, VisitFieldBinding);
            var entityContext = me.EntityContext == null ? null : VisitEntityContextInfo(me.EntityContext);

            if (me.Bindings != bindings || me.EntityContext != entityContext)
            {
                return new MixinEntityExpression(me.Type, bindings, me.MainEntityAlias, me.FieldMixin, entityContext);
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

        protected internal virtual Expression VisitScalar(ScalarExpression scalar)
        {
            var select = (SelectExpression)this.Visit(scalar.Select)!;
            if (select != scalar.Select)
                return new ScalarExpression(scalar.Type, select);
            return scalar;
        }

        protected internal virtual Expression VisitExists(ExistsExpression exists)
        {
            var select = (SelectExpression)this.Visit(exists.Select)!;
            if (select != exists.Select)
                return new ExistsExpression(select);
            return exists;
        }

        protected internal virtual Expression VisitIn(InExpression @in)
        {
            var expression = this.Visit(@in.Expression);
            var select = (SelectExpression?)this.Visit(@in.Select);
            if (expression != @in.Expression || select != @in.Select)
            {
                if (select != null)
                    return new InExpression(expression, select);
                else
                    return InExpression.FromValues(expression, @in.Values!);
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
            var orderBys = Visit(rowNumber.OrderBy, VisitOrderBy);
            if (orderBys != rowNumber.OrderBy)
                return new RowNumberExpression(orderBys);
            return rowNumber;
        }

        protected internal virtual Expression VisitAggregate(AggregateExpression aggregate)
        {
            var expressions = Visit(aggregate.Arguments);
            if (expressions != aggregate.Arguments)
                return new AggregateExpression(aggregate.Type, aggregate.AggregateFunction, expressions);
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
            Expression? top = this.Visit(select.Top);
            SourceExpression from = this.VisitSource(select.From!);
            Expression? where = this.Visit(select.Where);
            ReadOnlyCollection<ColumnDeclaration> columns = Visit(select.Columns, VisitColumnDeclaration);
            ReadOnlyCollection<OrderExpression> orderBy = Visit(select.OrderBy, VisitOrderBy);
            ReadOnlyCollection<Expression> groupBy = Visit(select.GroupBy, Visit);

            if (top != select.Top || from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy)
                return new SelectExpression(select.Alias, select.IsDistinct, top, columns, from, where, orderBy, groupBy, select.SelectOptions);

            return select;
        }

        protected internal virtual Expression VisitJoin(JoinExpression join)
        {
            SourceExpression left = this.VisitSource(join.Left);
            SourceExpression right = this.VisitSource(join.Right);
            Expression? condition = this.Visit(join.Condition);
            if (left != join.Left || right != join.Right || condition != join.Condition)
            {
                return new JoinExpression(join.JoinType, left, right, condition);
            }
            return join;
        }

        protected internal virtual Expression VisitSetOperator(SetOperatorExpression set)
        {
            SourceWithAliasExpression left = (SourceWithAliasExpression)this.VisitSource(set.Left)!;
            SourceWithAliasExpression right = (SourceWithAliasExpression)this.VisitSource(set.Right)!;
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
            Expression? obj = Visit(sqlFunction.Object);
            ReadOnlyCollection<Expression> args = Visit(sqlFunction.Arguments);
            if (args != sqlFunction.Arguments || obj != sqlFunction.Object)
                return new SqlFunctionExpression(sqlFunction.Type, obj, sqlFunction.SqlFunction, args);
            return sqlFunction;
        }

        protected internal virtual Expression VisitSqlTableValuedFunction(SqlTableValuedFunctionExpression sqlFunction)
        {
            ReadOnlyCollection<Expression> args = Visit(sqlFunction.Arguments);
            if (args != sqlFunction.Arguments)
                return new SqlTableValuedFunctionExpression(sqlFunction.SqlFunction, sqlFunction.ViewTable, sqlFunction.SingleColumnType, sqlFunction.Alias, args);
            return sqlFunction;
        }

        protected internal virtual Expression VisitSqlConstant(SqlConstantExpression sce)
        {
            return sce;
        }

        protected internal virtual Expression VisitSqlVariable(SqlVariableExpression sve)
        {
            return sve;
        }

        protected internal virtual Expression VisitCase(CaseExpression cex)
        {
            var newWhens = Visit(cex.Whens, w => VisitWhen(w));
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

        protected internal virtual Expression VisitToDayOfWeek(ToDayOfWeekExpression toDayOfWeek)
        {
            var exp = Visit(toDayOfWeek.Expression);
            if (exp == toDayOfWeek.Expression)
                return toDayOfWeek;

            return new ToDayOfWeekExpression(exp);
        }

        protected internal virtual OrderExpression VisitOrderBy(OrderExpression o)
        {
            var e = Visit(o.Expression);
            if (e == o.Expression)
                return o;

            return new OrderExpression(o.OrderType, e);
        }

        protected internal virtual Expression VisitPrimaryKey(PrimaryKeyExpression pk)
        {
            var e = Visit(pk.Value);
            if (e == pk.Value)
                return pk;

            return new PrimaryKeyExpression(e);
        }

        protected internal virtual Expression VisitPrimaryKeyString(PrimaryKeyStringExpression pk)
        {
            var typeId = Visit(pk.TypeId);
            var id = Visit(pk.Id);
            if (typeId == pk && pk.Id == id)
                return pk;

            return new PrimaryKeyStringExpression(id, (TypeImplementedByAllExpression)typeId);
        }

        protected internal virtual Expression VisitInterval(IntervalExpression interval)
        {
            Expression? min = Visit(interval.Min);
            Expression? max = Visit(interval.Max);
            Expression? postgresRange = Visit(interval.PostgresRange);
            if (min != interval.Min || max != interval.Max || postgresRange != interval.PostgresRange)
                return new IntervalExpression(interval.Type, min, max, postgresRange, interval.AsUtc);
            return interval;
        }
    }
}
