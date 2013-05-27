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
    internal class DbExpressionVisitor : SimpleExpressionVisitor
    {
        protected override Expression Visit(Expression exp)
        {
            if (exp == null)
                return null;

            switch (exp.NodeType)
            {  
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                case ExpressionType.UnaryPlus:
                    return this.VisitUnary((UnaryExpression)exp);
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.Power:
                    return this.VisitBinary((BinaryExpression)exp);
                case ExpressionType.TypeIs:
                    return this.VisitTypeIs((TypeBinaryExpression)exp);
                case ExpressionType.Conditional:
                    return this.VisitConditional((ConditionalExpression)exp);
                case ExpressionType.Constant:
                    return this.VisitConstant((ConstantExpression)exp);
                case ExpressionType.Parameter:
                    return this.VisitParameter((ParameterExpression)exp);
                case ExpressionType.MemberAccess:
                    return this.VisitMemberAccess((MemberExpression)exp);
                case ExpressionType.Call:
                    return this.VisitMethodCall((MethodCallExpression)exp);
                case ExpressionType.Lambda:
                    return this.VisitLambda((LambdaExpression)exp);
                case ExpressionType.New:
                    return this.VisitNew((NewExpression)exp);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return this.VisitNewArray((NewArrayExpression)exp);
                case ExpressionType.Invoke:
                    return this.VisitInvocation((InvocationExpression)exp);
                case ExpressionType.MemberInit:
                    return this.VisitMemberInit((MemberInitExpression)exp);
                case ExpressionType.ListInit:
                    return this.VisitListInit((ListInitExpression)exp);
               
                case (ExpressionType)DbExpressionType.Table:
                    return this.VisitTable((TableExpression)exp);
                case (ExpressionType)DbExpressionType.Column:
                    return this.VisitColumn((ColumnExpression)exp);
                case (ExpressionType)DbExpressionType.Select:
                    return this.VisitSelect((SelectExpression)exp);
                case (ExpressionType)DbExpressionType.Join:
                    return this.VisitJoin((JoinExpression)exp);
               case (ExpressionType)DbExpressionType.SetOperator:
                    return this.VisitSetOperator((SetOperatorExpression)exp);
                case (ExpressionType)DbExpressionType.Projection:
                    return this.VisitProjection((ProjectionExpression)exp);
                case (ExpressionType)DbExpressionType.ChildProjection:
                    return this.VisitChildProjection((ChildProjectionExpression)exp);
                case (ExpressionType)DbExpressionType.Aggregate:
                    return this.VisitAggregate((AggregateExpression)exp);
                case (ExpressionType)DbExpressionType.AggregateSubquery:
                    return this.VisitAggregateSubquery((AggregateSubqueryExpression)exp);
                case (ExpressionType)DbExpressionType.SqlCast:
                    return this.VisitSqlCast((SqlCastExpression)exp);
                case (ExpressionType)DbExpressionType.SqlEnum:
                    return this.VisitSqlEnum((SqlEnumExpression)exp);
                case (ExpressionType)DbExpressionType.SqlFunction:
                    return this.VisitSqlFunction((SqlFunctionExpression)exp);
                case (ExpressionType)DbExpressionType.SqlTableValuedFunction:
                    return this.VisitSqlTableValuedFunction((SqlTableValuedFunctionExpression)exp);
                case (ExpressionType)DbExpressionType.SqlConstant:
                    return this.VisitSqlConstant((SqlConstantExpression)exp);
                case (ExpressionType)DbExpressionType.Case:
                    return this.VisitCase((CaseExpression)exp);
                case (ExpressionType)DbExpressionType.RowNumber:
                    return this.VisitRowNumber((RowNumberExpression)exp);
                case (ExpressionType)DbExpressionType.Like:
                    return this.VisitLike((LikeExpression)exp);
                case (ExpressionType)DbExpressionType.In:
                case (ExpressionType)DbExpressionType.Scalar:
                case (ExpressionType)DbExpressionType.Exists:
                    return this.VisitSubquery((SubqueryExpression)exp);
                case (ExpressionType)DbExpressionType.IsNull:
                    return this.VisitIsNull((IsNullExpression)exp);
                case (ExpressionType)DbExpressionType.IsNotNull:
                    return this.VisitIsNotNull((IsNotNullExpression)exp);
                case (ExpressionType)DbExpressionType.Delete:
                    return this.VisitDelete((DeleteExpression)exp);
                case (ExpressionType)DbExpressionType.Update:
                    return this.VisitUpdate((UpdateExpression)exp);
                case (ExpressionType)DbExpressionType.CommandAggregate:
                    return this.VisitCommandAggregate((CommandAggregateExpression)exp);
                case (ExpressionType)DbExpressionType.SelectRowCount:
                    return this.VisitSelectRowCount((SelectRowCountExpression)exp);
                case (ExpressionType)DbExpressionType.Entity:
                    return this.VisitEntity((EntityExpression)exp);
                case (ExpressionType)DbExpressionType.EmbeddedInit:
                    return this.VisitEmbeddedEntity((EmbeddedEntityExpression)exp);
                case (ExpressionType)DbExpressionType.ImplementedBy:
                    return this.VisitImplementedBy((ImplementedByExpression)exp);
                case (ExpressionType)DbExpressionType.ImplementedByAll:
                    return this.VisitImplementedByAll((ImplementedByAllExpression)exp);
                case (ExpressionType)DbExpressionType.LiteReference:
                    return this.VisitLiteReference((LiteReferenceExpression)exp);
                case (ExpressionType)DbExpressionType.LiteValue:
                    return this.VisitLiteValue((LiteValueExpression)exp);
                case (ExpressionType)DbExpressionType.TypeEntity:
                    return this.VisitTypeFieldInit((TypeEntityExpression)exp);
                case (ExpressionType)DbExpressionType.TypeImplementedBy:
                    return this.VisitTypeImplementedBy((TypeImplementedByExpression)exp);
                case (ExpressionType)DbExpressionType.TypeImplementedByAll:
                    return this.VisitTypeImplementedByAll((TypeImplementedByAllExpression)exp);
                case (ExpressionType)DbExpressionType.MList:
                    return this.VisitMList((MListExpression)exp);
                case (ExpressionType)DbExpressionType.MListProjection:
                    return this.VisitMListProjection((MListProjectionExpression)exp);
                case (ExpressionType)DbExpressionType.MListElement:
                    return this.VisitMListElement((MListElementExpression)exp);

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
            var assigments = update.Assigments.NewIfChange(VisitColumnAssigment);
            if(source != update.Source || where != update.Where || assigments != update.Assigments)
                return new UpdateExpression(update.Table, (SourceExpression)source, where, assigments);
            return update;
        }

        protected virtual ColumnAssignment VisitColumnAssigment(ColumnAssignment c)
        {
            var exp = Visit(c.Expression);
            if (exp != c.Expression)
                return new ColumnAssignment(c.Column, exp);
            return c;
        }

        protected virtual Expression VisitSelectRowCount(SelectRowCountExpression src)
        {
            return src;
        }

        protected virtual Expression VisitLiteReference(LiteReferenceExpression lite)
        {
            var newRef = Visit(lite.Reference);
            var newToStr = Visit(lite.CustomToStr);
            if (newRef != lite.Reference || newToStr != lite.CustomToStr)
                return new LiteReferenceExpression(lite.Type,  newRef, newToStr);
            return lite;
        }

        protected virtual Expression VisitLiteValue(LiteValueExpression lite)
        {
            var newTypeId = Visit(lite.TypeId);
            var newId = Visit(lite.Id);
            var newToStr = Visit(lite.ToStr);
            if (newTypeId != lite.TypeId || newId != lite.Id || newToStr != lite.ToStr)
                return new LiteValueExpression(lite.Type, newTypeId, newId, newToStr);
            return lite;
        }

        protected virtual Expression VisitTypeFieldInit(TypeEntityExpression typeFie)
        {
            var externalId = Visit(typeFie.ExternalId);

            if (externalId != typeFie.ExternalId)
                return new TypeEntityExpression(externalId, typeFie.TypeValue);

            return typeFie;
        }

        protected virtual Expression VisitTypeImplementedBy(TypeImplementedByExpression typeIb)
        {
            var implementations = typeIb.TypeImplementations.NewIfChange(VisitTypeImplementationColumn);

            if (implementations != typeIb.TypeImplementations)
                return new TypeImplementedByExpression(implementations);
            return typeIb;
        }

        protected virtual TypeImplementationColumnExpression VisitTypeImplementationColumn(TypeImplementationColumnExpression imp)
        {
            var id = Visit(imp.ExternalId);
            if(id == imp.ExternalId)
                return imp;
            
            return new TypeImplementationColumnExpression(imp.Type, id);
        }

        protected virtual Expression VisitTypeImplementedByAll(TypeImplementedByAllExpression typeIba)
        {
            var column = Visit(typeIba.TypeColumn);

            if (column != typeIba.TypeColumn)
                return new TypeImplementedByAllExpression(column);

            return typeIba;
        }

        protected virtual Expression VisitMList(MListExpression ml)
        {
            var newBackID = Visit(ml.BackID);
            if (newBackID != ml.BackID)
                return new MListExpression(ml.Type, newBackID, ml.RelationalTable);
            return ml;
        }

        protected virtual Expression VisitMListProjection(MListProjectionExpression mlp)
        {
            var proj = (ProjectionExpression)Visit(mlp.Projection);
            if (proj != mlp.Projection)
                return new MListProjectionExpression(mlp.Type, proj);
            return mlp;
        }

        protected virtual Expression VisitMListElement(MListElementExpression mle)
        {
            var rowId = Visit(mle.RowId);
            var parent = (EntityExpression)Visit(mle.Parent);
            var element = Visit(mle.Element);
            if (rowId != mle.RowId || parent != mle.Parent || element != mle.Parent)
                return new MListElementExpression(rowId, parent, element, mle.Table);
            return mle;
        }

        protected virtual Expression VisitSqlEnum(SqlEnumExpression sqlEnum)
        {
            return sqlEnum;
        }

        protected virtual Expression VisitSqlCast(SqlCastExpression castExpr)
        {
            var expression = Visit(castExpr.Expression);
            if (expression != castExpr.Expression)
                return new SqlCastExpression(castExpr.Type, expression,castExpr.SqlDbType);
            return castExpr;
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
            var typeId = (TypeImplementedByAllExpression)Visit(reference.TypeId);

            if (id != reference.Id || typeId != reference.TypeId)
                return new ImplementedByAllExpression(reference.Type, id, typeId);
            return reference;
        }

        protected virtual Expression VisitImplementedBy(ImplementedByExpression reference)
        {
            var implementations = reference.Implementations.NewIfChange(VisitImplementationColumn);

            if (implementations != reference.Implementations)
                return new ImplementedByExpression(reference.Type, implementations);
            return reference;
        }

        protected virtual ImplementationColumn VisitImplementationColumn(ImplementationColumn fb)
        {
            var r = Visit(fb.Reference);

            if (r == fb.Reference)
                return fb;

            return new ImplementationColumn(fb.Type, (EntityExpression)r);
        }

        protected virtual Expression VisitEntity(EntityExpression ee)
        {
            var bindings = ee.Bindings.NewIfChange(VisitFieldBinding);

            var id = Visit(ee.ExternalId);

            if (ee.Bindings != bindings || ee.ExternalId != id)
                return new EntityExpression(ee.Type, id, ee.TableAlias, bindings, ee.AvoidExpandOnRetrieving);

            return ee;
        }

        protected virtual Expression VisitEmbeddedEntity(EmbeddedEntityExpression eee)
        {
            var bindings = eee.Bindings.NewIfChange(VisitFieldBinding);
            var hasValue = Visit(eee.HasValue);

            if (eee.Bindings != bindings || eee.HasValue != hasValue)
            {
                return new EmbeddedEntityExpression(eee.Type, hasValue, bindings, eee.FieldEmbedded);
            }
            return eee;
        }

        protected virtual FieldBinding VisitFieldBinding(FieldBinding fb)
        {
            var r = Visit(fb.Binding);

            if(r == fb.Binding)
                return fb; 

            return new FieldBinding(fb.FieldInfo, r); 
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
            var orderBys = rowNumber.OrderBy.NewIfChange(VisitOrderBy);
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
            var subquery = (ScalarExpression)this.Visit(aggregate.Subquery);
            if (subquery != aggregate.Subquery)
                return new AggregateSubqueryExpression(aggregate.GroupByAlias, aggregate.Aggregate, subquery);
            return aggregate;
        }

        protected virtual Expression VisitSelect(SelectExpression select)
        {
            Expression top = this.Visit(select.Top);
            SourceExpression from = this.VisitSource(select.From);
            Expression where = this.Visit(select.Where);
            ReadOnlyCollection<ColumnDeclaration> columns = select.Columns.NewIfChange(VisitColumnDeclaration);
            ReadOnlyCollection<OrderExpression> orderBy = select.OrderBy.NewIfChange(VisitOrderBy);
            ReadOnlyCollection<Expression> groupBy = select.GroupBy.NewIfChange(Visit);

            if (top != select.Top || from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy)
                return new SelectExpression(select.Alias, select.IsDistinct, select.IsReverse, top, columns, from, where, orderBy, groupBy, select.ForXmlPathEmpty);

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

        protected virtual Expression VisitSetOperator(SetOperatorExpression set)
        {
            SourceWithAliasExpression left = (SourceWithAliasExpression)this.VisitSource(set.Left);
            SourceWithAliasExpression right = (SourceWithAliasExpression)this.VisitSource(set.Right);
            if (left != set.Left || right != set.Right)
            {
                return new SetOperatorExpression(set.Operator, left, right, set.Alias);
            }
            return set;
        }

        protected virtual SourceExpression VisitSource(SourceExpression source)
        {
            return (SourceExpression)this.Visit(source);
        }

        protected virtual Expression VisitProjection(ProjectionExpression proj)
        {
            SelectExpression source = (SelectExpression)this.Visit(proj.Select);
            Expression projector = this.Visit(proj.Projector);

            if (source != proj.Select || projector != proj.Projector)
                return new ProjectionExpression(source, projector, proj.UniqueFunction, proj.Type);

            return proj;
        }

        protected virtual Expression VisitChildProjection(ChildProjectionExpression child)
        {
            ProjectionExpression proj = (ProjectionExpression)this.Visit(child.Projection);
            Expression key = this.Visit(child.OuterKey);

            if (proj != child.Projection || key != child.OuterKey)
            {
                return new ChildProjectionExpression(proj, key, child.IsLazyMList, child.Type, child.Token);
            }
            return child;
        }

        protected virtual Expression VisitSqlFunction(SqlFunctionExpression sqlFunction)
        {
            Expression obj = Visit(sqlFunction.Object);
            ReadOnlyCollection<Expression> args = sqlFunction.Arguments.NewIfChange(a => Visit(a));
            if (args != sqlFunction.Arguments || obj != sqlFunction.Object)
                return new SqlFunctionExpression(sqlFunction.Type, obj, sqlFunction.SqlFunction, args); 
            return sqlFunction;
        }

        protected virtual Expression VisitSqlTableValuedFunction(SqlTableValuedFunctionExpression sqlFunction)
        {
            ReadOnlyCollection<Expression> args = sqlFunction.Arguments.NewIfChange(a => Visit(a));
            if (args != sqlFunction.Arguments)
                return new SqlTableValuedFunctionExpression(sqlFunction.SqlFunction, sqlFunction.Table, sqlFunction.Alias, args);
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

        protected virtual ColumnDeclaration VisitColumnDeclaration(ColumnDeclaration c)
        {
            var e = Visit(c.Expression);
            if (e == c.Expression)
                return c;

            return new ColumnDeclaration(c.Name, e);
        }

        protected virtual OrderExpression VisitOrderBy(OrderExpression o)
        {
            var e = Visit(o.Expression);
            if (e == o.Expression)
                return o;

            return new OrderExpression(o.OrderType, e);
        }
    }
}
