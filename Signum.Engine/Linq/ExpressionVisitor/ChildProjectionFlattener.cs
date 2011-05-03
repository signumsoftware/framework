using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Linq
{
    internal class ChildProjectionFlattener : DbExpressionVisitor
    {
        SelectExpression currentSource;
        private ChildProjectionFlattener(){}

        static internal ProjectionExpression Flatten(ProjectionExpression proj)
        {
            var result = (ProjectionExpression)new ChildProjectionFlattener().Visit(proj);
            if (result == proj)
                return result;

            Expression columnCleaned = UnusedColumnRemover.Remove(result);
            Expression subqueryCleaned = RedundantSubqueryRemover.Remove(columnCleaned);

            return (ProjectionExpression)subqueryCleaned; 
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            if (currentSource == null)
            {
                currentSource = WithoutOrder(proj.Source);

                Expression projector = this.Visit(proj.Projector);

                if (projector != proj.Projector)
                    proj = new ProjectionExpression(proj.Source, projector, proj.UniqueFunction, proj.Token, proj.Type);

                currentSource = null;
                return proj;
            }
            else
            {
                HashSet<ColumnExpression> columns = ExternalColumnGatherer.Gatherer(proj, currentSource.Alias);

                if (columns.Count == 0)
                {
                    Expression projector = Visit(proj.Projector);

                    ConstantExpression key = Expression.Constant(0);
                    Type kvpType = typeof(KeyValuePair<,>).MakeGenericType(key.Type, projector.Type);
                    ConstructorInfo ciKVP = kvpType.GetConstructor(new[] { key.Type, projector.Type });
                    Type projType = proj.UniqueFunction == null ? proj.Type.GetGenericTypeDefinition().MakeGenericType(kvpType) : kvpType;
                    return new ChildProjectionExpression(new ProjectionExpression(
                            proj.Source, Expression.New(ciKVP, key, projector), proj.UniqueFunction, proj.Token, projType),
                        Expression.Constant(0));

                }
                else
                {
                    SelectExpression external;
                    IEnumerable<ColumnExpression> externalColumns;

                    if (!IsKey(currentSource, columns))
                    {
                        string aliasDistinct = currentSource.Alias + "D";
                        ColumnGenerator generatorDistinct = new ColumnGenerator();

                        List<ColumnDeclaration> columnDistinct = columns.Select(ce => generatorDistinct.MapColumn(ce)).ToList();
                        external = new SelectExpression(aliasDistinct, true, null, columnDistinct, currentSource, null, null, null);


                        Dictionary<ColumnExpression, ColumnExpression> distinctReplacements = columnDistinct.ToDictionary(
                                                    cd => (ColumnExpression)cd.Expression,
                                                    cd => cd.GetReference(aliasDistinct));

                        proj = (ProjectionExpression)ColumnReplacer.Replace(proj, distinctReplacements);

                        externalColumns = distinctReplacements.Values.ToHashSet();
                    }
                    else
                    {
                        external = currentSource;
                        externalColumns = columns;
                    }

                    ColumnGenerator generatorSM = new ColumnGenerator();
                    List<ColumnDeclaration> columnsSMExternal = externalColumns.Select(ce => generatorSM.MapColumn(ce)).ToList();
                    List<ColumnDeclaration> columnsSMInternal = proj.Source.Columns.Select(cd => generatorSM.MapColumn(cd.GetReference(proj.Source.Alias))).ToList();

                    List<OrderExpression> innerOrders;
                    SelectExpression @internal = ExtractOrders(proj.Source, out innerOrders);

                    string aliasSM = @internal.Alias + "SM";
                    SelectExpression selectMany = new SelectExpression(aliasSM, false, null, columnsSMExternal.Concat(columnsSMInternal),
                        new JoinExpression(JoinType.CrossApply,
                            external,
                            @internal, null), null, innerOrders, null);

                    SelectExpression old = currentSource;
                    currentSource = WithoutOrder(selectMany);

                    var selectManyReplacements = selectMany.Columns.ToDictionary(
                           cd => (ColumnExpression)cd.Expression,
                           cd => cd.GetReference(aliasSM));

                    Expression projector = ColumnReplacer.Replace(proj.Projector, selectManyReplacements);

                    projector = Visit(projector);

                    currentSource = old;

                    Expression key = TupleReflection.TupleChainConstructor(columnsSMExternal.Select(cd => cd.GetReference(aliasSM)));
                    Type kvpType = typeof(KeyValuePair<,>).MakeGenericType(key.Type, projector.Type);
                    ConstructorInfo ciKVP = kvpType.GetConstructor(new[] { key.Type, projector.Type });
                    Type projType = proj.UniqueFunction == null ? proj.Type.GetGenericTypeDefinition().MakeGenericType(kvpType) : kvpType;
                    return new ChildProjectionExpression(new ProjectionExpression(
                        selectMany,
                        Expression.New(ciKVP, key, projector), proj.UniqueFunction, proj.Token, projType),
                        TupleReflection.TupleChainConstructor(columns));
                }
            }
        }

        private SelectExpression WithoutOrder(SelectExpression sel)
        {
            if (sel.Top != null || (sel.OrderBy == null || sel.OrderBy.Count == 0))
                return sel;

            return new SelectExpression(sel.Alias, sel.Distinct, sel.Top, sel.Columns, sel.From, sel.Where, null, sel.GroupBy);
        }

        private SelectExpression ExtractOrders(SelectExpression sel, out List<OrderExpression> innerOrders)
        {
            if (sel.Top != null || (sel.OrderBy == null || sel.OrderBy.Count == 0))
            {
                innerOrders = null;
                return sel;
            }
            else
            {
                ColumnGenerator cg = new ColumnGenerator() { columns = sel.Columns.ToList() };
                Dictionary<OrderExpression, ColumnDeclaration> newColumns = sel.OrderBy.ToDictionary(o => o, o => cg.NewColumn(o.Expression));

                innerOrders = newColumns.Select(kvp => new OrderExpression(kvp.Key.OrderType, kvp.Value.GetReference(sel.Alias))).ToList();

                return new SelectExpression(sel.Alias, sel.Distinct, sel.Top, sel.Columns.Concat(newColumns.Values), sel.From, sel.Where, null, sel.GroupBy);
            }
        }

        private bool IsKey(SelectExpression source, HashSet<ColumnExpression> columns)
        {
            var keys = KeyFinder.Keys(source);

            return keys.All(k => k != null && columns.Contains(k));
        }

        internal static class KeyFinder
        {
            public static IEnumerable<ColumnExpression> Keys(SourceExpression source)
            {
                if (source is SelectExpression)
                    return KeysSelect((SelectExpression)source);
                if (source is TableExpression)
                    return KeysTable((TableExpression)source); 
                if(source is JoinExpression)
                    return KeysJoin((JoinExpression)source);

                throw new InvalidOperationException("Unexpected source");
            }

            private static IEnumerable<ColumnExpression> KeysJoin(JoinExpression join)
            {
                switch (join.JoinType)
                {
                    case JoinType.SingleRowLeftOuterJoin:
                        return Keys(join.Left);

                    case JoinType.CrossJoin:
                    case JoinType.InnerJoin:
                    case JoinType.CrossApply:
                    case JoinType.OuterApply:
                    case JoinType.LeftOuterJoin:
                    case JoinType.RightOuterJoin:
                    case JoinType.FullOuterJoin:
                        return Keys(join.Left).Concat(Keys(join.Right));
                    default:
                        break;
                }

                throw new InvalidOperationException("Unexpected Join Type");
            }

            private static IEnumerable<ColumnExpression> KeysTable(TableExpression table)
            {
                yield return new ColumnExpression(typeof(int), table.Alias, SqlBuilder.PrimaryKeyName) ; 
            }

            private static IEnumerable<ColumnExpression> KeysSelect(SelectExpression select)
            {
                if(select.GroupBy != null && select.GroupBy.Count == 0)
                    return select.GroupBy.Select(ce => select.Columns.FirstOrDefault(cd => cd.Expression.Equals(ce) /*could be inproved*/).TryCC(cd => cd.GetReference(select.Alias))).ToList();


                IEnumerable<ColumnExpression> inner = Keys(select.From);

                var result = inner.Select(ce=>select.Columns.FirstOrDefault(cd=>cd.Expression.Equals(ce)).TryCC(cd=>cd.GetReference(select.Alias))).ToList();

                if (!select.Distinct)
                    return result;

                var result2 = select.Columns.Select(cd => cd.GetReference(select.Alias)).ToList();

                if (result.Any(c => c == null))
                    return result2;

                if(result2.Any(c=>c == null))
                    return result;

                return result.Count > result2.Count ? result2 : result; 
            }
        }

        internal class ColumnReplacer : DbExpressionVisitor
        {
            Dictionary<ColumnExpression, ColumnExpression> Replacements;

            public static Expression Replace(Expression expression, Dictionary<ColumnExpression, ColumnExpression> replacements)
            {
                return new ColumnReplacer { Replacements = replacements }.Visit(expression); 
            }

            protected override Expression VisitColumn(ColumnExpression column)
            {
                return Replacements.TryGetC(column) ?? base.VisitColumn(column);
            }

            protected override Expression VisitChildProjection(ChildProjectionExpression child)
            {
                return child;
            }
        }

        internal class ExternalColumnGatherer : DbExpressionVisitor
        {
            string externalAlias;

            HashSet<ColumnExpression> columns = new HashSet<ColumnExpression>();

            private ExternalColumnGatherer() { }

            public static HashSet<ColumnExpression> Gatherer(Expression source, string externalAlias)
            {
                ExternalColumnGatherer ap = new ExternalColumnGatherer()
                {
                    externalAlias = externalAlias
                };

                ap.Visit(source);

                return ap.columns;
            }

            protected override Expression VisitColumn(ColumnExpression column)
            {
                if (externalAlias == column.Alias)
                    columns.Add(column);

                return base.VisitColumn(column);
            }
        }

    }
}