using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Linq
{
    internal class ChildProjectionFlattener : DbExpressionVisitor
    {
        SelectExpression? currentSource;
        readonly AliasGenerator aliasGenerator;

        private ChildProjectionFlattener(AliasGenerator aliasGenerator)
        {
            this.aliasGenerator = aliasGenerator;
        }

        static internal ProjectionExpression Flatten(ProjectionExpression proj, AliasGenerator aliasGenerator)
        {
            var result = (ProjectionExpression)new ChildProjectionFlattener(aliasGenerator).Visit(proj);
            if (result == proj)
                return result;

            Expression columnCleaned = UnusedColumnRemover.Remove(result);
            Expression subqueryCleaned = RedundantSubqueryRemover.Remove(columnCleaned);

            return (ProjectionExpression)subqueryCleaned;
        }

        public Type? inMList = null;
        protected internal override Expression VisitMListProjection(MListProjectionExpression mlp)
        {
            var oldInEntity = inMList;
            inMList = mlp.Type;
            var result = VisitProjection(mlp.Projection);
            inMList = oldInEntity;
            return result;
        }
        
        protected internal override Expression VisitProjection(ProjectionExpression proj)
        {
            if (currentSource == null)
            {
                currentSource = WithoutOrder(proj.Select);

                Expression projector = this.Visit(proj.Projector);

                if (projector != proj.Projector)
                    proj = new ProjectionExpression(proj.Select, projector, proj.UniqueFunction, proj.Type);

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
                    ConstructorInfo ciKVP = kvpType.GetConstructor(new[] { key.Type, projector.Type })!;
                    Type projType = proj.UniqueFunction == null ? typeof(IEnumerable<>).MakeGenericType(kvpType) : kvpType;

                    var childProj = new ProjectionExpression(proj.Select,
                        Expression.New(ciKVP, key, projector), proj.UniqueFunction, projType);

                    return new ChildProjectionExpression(childProj,
                        Expression.Constant(0), inMList != null, inMList ?? proj.Type, new LookupToken());

                }
                else
                {
                    SelectExpression external;
                    IEnumerable<ColumnExpression> externalColumns;

                    if (!IsKey(currentSource, columns))
                    {
                        Alias aliasDistinct = aliasGenerator.GetUniqueAlias(currentSource.Alias.Name + "D");
                        ColumnGenerator generatorDistinct = new ColumnGenerator();

                        List<ColumnDeclaration> columnDistinct = columns.Select(ce => generatorDistinct.MapColumn(ce)).ToList();
                        external = new SelectExpression(aliasDistinct, true, null, columnDistinct, currentSource, null, null, null, 0);


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
                    List<ColumnDeclaration> columnsSMInternal = proj.Select.Columns.Select(cd => generatorSM.MapColumn(cd.GetReference(proj.Select.Alias))).ToList();

                    SelectExpression @internal = ExtractOrders(proj.Select, out List<OrderExpression>? innerOrders);

                    Alias aliasSM = aliasGenerator.GetUniqueAlias(@internal.Alias.Name + "SM");
                    SelectExpression selectMany = new SelectExpression(aliasSM, false, null, columnsSMExternal.Concat(columnsSMInternal),
                        new JoinExpression(JoinType.CrossApply,
                            external,
                            @internal, null), null, innerOrders, null, 0);

                    SelectExpression old = currentSource;
                    currentSource = WithoutOrder(selectMany);

                    var selectManyReplacements = selectMany.Columns.ToDictionary(
                           cd => (ColumnExpression)cd.Expression,
                           cd => cd.GetReference(aliasSM));

                    Expression projector = ColumnReplacer.Replace(proj.Projector, selectManyReplacements);

                    projector = Visit(projector);

                    currentSource = old;

                    Expression key = TupleReflection.TupleChainConstructor(columnsSMExternal.Select(cd => MakeEquatable(cd.GetReference(aliasSM))));
                    Type kvpType = typeof(KeyValuePair<,>).MakeGenericType(key.Type, projector.Type);
                    ConstructorInfo ciKVP = kvpType.GetConstructor(new[] { key.Type, projector.Type })!;
                    Type projType = proj.UniqueFunction == null ? typeof(IEnumerable<>).MakeGenericType(kvpType) : kvpType;

                    var childProj = new ProjectionExpression(selectMany,
                        Expression.New(ciKVP, key, projector), proj.UniqueFunction, projType);

                    return new ChildProjectionExpression(childProj,
                        TupleReflection.TupleChainConstructor(columns.Select(a => MakeEquatable(a))), inMList != null, inMList ?? proj.Type, new LookupToken());
                }
            }
        }

        public Expression MakeEquatable(Expression expression)
        {
            if (expression.Type.IsArray)
                return Expression.New(typeof(ArrayBox<>).MakeGenericType(expression.Type.ElementType()!).GetConstructors().SingleEx(), expression);

            return expression.Nullify();
        }

        private SelectExpression WithoutOrder(SelectExpression sel)
        {
            if (sel.Top != null || (sel.OrderBy.Count == 0))
                return sel;

            return new SelectExpression(sel.Alias, sel.IsDistinct, sel.Top, sel.Columns, sel.From, sel.Where, null, sel.GroupBy, sel.SelectOptions);
        }

        private SelectExpression ExtractOrders(SelectExpression sel, out List<OrderExpression>? innerOrders)
        {
            if (sel.Top != null || (sel.OrderBy.Count == 0))
            {
                innerOrders = null;
                return sel;
            }
            else
            {
                ColumnGenerator cg = new ColumnGenerator(sel.Columns);
                Dictionary<OrderExpression, ColumnDeclaration> newColumns = sel.OrderBy.ToDictionary(o => o, o => cg.NewColumn(o.Expression));

                innerOrders = newColumns.Select(kvp => new OrderExpression(kvp.Key.OrderType, kvp.Value.GetReference(sel.Alias))).ToList();

                return new SelectExpression(sel.Alias, sel.IsDistinct, sel.Top, sel.Columns.Concat(newColumns.Values), sel.From, sel.Where, null, sel.GroupBy, sel.SelectOptions);
            }
        }

        private bool IsKey(SelectExpression source, HashSet<ColumnExpression> columns)
        {
            var keys = KeyFinder.Keys(source);

            return keys.All(k => k != null && columns.Contains(k));
        }

        internal static class KeyFinder
        {
            public static IEnumerable<ColumnExpression?> Keys(SourceExpression source)
            {
                if (source is SelectExpression)
                    return KeysSelect((SelectExpression)source);
                if (source is TableExpression)
                    return KeysTable((TableExpression)source);
                if(source is JoinExpression)
                    return KeysJoin((JoinExpression)source);
                if (source is SetOperatorExpression)
                    return KeysSet((SetOperatorExpression)source);

                throw new InvalidOperationException("Unexpected source");
            }

            private static IEnumerable<ColumnExpression?> KeysSet(SetOperatorExpression set)
            {
                return Keys(set.Left).Concat(Keys(set.Right));
            }

            private static IEnumerable<ColumnExpression?> KeysJoin(JoinExpression join)
            {
                switch (join.JoinType)
                {
                    case JoinType.SingleRowLeftOuterJoin:
                        return Keys(join.Left);
                    case JoinType.CrossApply:
                        {
                            var leftKeys = Keys(join.Left);
                            var rightKeys = Keys(join.Right);

                            var onlyLeftKey = leftKeys.Only();

                            if(onlyLeftKey != null &&
                                join.Right is SelectExpression r &&
                                r.Where is BinaryExpression b &&
                                b.NodeType == ExpressionType.Equal &&
                                b.Left is ColumnExpression cLeft &&
                                b.Right is ColumnExpression cRight)
                            {
                                if(cLeft.Equals(onlyLeftKey) ^ cRight.Equals(onlyLeftKey))
                                {
                                    var other = b.Left == onlyLeftKey ? b.Right : b.Left;

                                    if (other is ColumnExpression c && join.Right.KnownAliases.Contains(c.Alias))
                                        return rightKeys;
                                }
                            }

                            return leftKeys.Concat(rightKeys);
                        }
                    case JoinType.CrossJoin:
                    case JoinType.InnerJoin:
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
                if (table.Table is Table t && t.IsView)
                    return t.Columns.Values.Where(c => c.PrimaryKey).Select(c => new ColumnExpression(c.Type, table.Alias, c.Name));
                else
                    return new[] { new ColumnExpression(typeof(int), table.Alias, table.Table.PrimaryKey.Name) };
            }

            private static IEnumerable<ColumnExpression?> KeysSelect(SelectExpression select)
            {
                if (select.GroupBy.Any())
                    return select.GroupBy.Select(ce => select.Columns.FirstOrDefault(cd => cd.Expression.Equals(ce) /*could be improved*/)?.Let(cd => cd.GetReference(select.Alias))).ToList();

                IEnumerable<ColumnExpression?> inner = Keys(select.From!);

                var result = inner.Select(ce => select.Columns.FirstOrDefault(cd => cd.Expression.Equals(ce))?.Let(cd => cd.GetReference(select.Alias))).ToList();

                if (!select.IsDistinct)
                    return result;

                var result2 = select.Columns.Select(cd => (ColumnExpression?)cd.GetReference(select.Alias)).ToList();

                if (result.Any(c => c == null))
                    return result2;

                if (result2.Any(c => c == null))
                    return result;

                return result.Count > result2.Count ? result2 : result;
            }
        }

        internal class ColumnReplacer : DbExpressionVisitor
        {
            readonly Dictionary<ColumnExpression, ColumnExpression> Replacements;

            public ColumnReplacer(Dictionary<ColumnExpression, ColumnExpression> replacements)
            {
                Replacements = replacements;
            }

            public static Expression Replace(Expression expression, Dictionary<ColumnExpression, ColumnExpression> replacements)
            {
                return new ColumnReplacer(replacements).Visit(expression);
            }

            protected internal override Expression VisitColumn(ColumnExpression column)
            {
                return Replacements.TryGetC(column) ?? base.VisitColumn(column);
            }

            protected internal override Expression VisitChildProjection(ChildProjectionExpression child)
            {
                return child;
            }
        }

        internal class ExternalColumnGatherer : DbExpressionVisitor
        {
            readonly Alias externalAlias;
            readonly HashSet<ColumnExpression> columns = new HashSet<ColumnExpression>();

            public ExternalColumnGatherer(Alias externalAlias)
            {
                this.externalAlias = externalAlias;
            }
            
            public static HashSet<ColumnExpression> Gatherer(Expression source, Alias externalAlias)
            {
                ExternalColumnGatherer ap = new ExternalColumnGatherer(externalAlias);

                ap.Visit(source);

                return ap.columns;
            }

            protected internal override Expression VisitColumn(ColumnExpression column)
            {
                if (externalAlias == column.Alias)
                    columns.Add(column);

                return base.VisitColumn(column);
            }
        }

    }

    class ArrayBox<T> : IEquatable<ArrayBox<T>>
    {
        readonly int hashCode;
        public readonly T[]? Array;

        public ArrayBox(T[]? array)
        {
            this.Array = array;
            this.hashCode = 0;
            if(array != null)
            {
                foreach (var item in array)
                {
                    this.hashCode = (this.hashCode << 1) ^ (item == null ? 0 : item.GetHashCode());
                }
            }
        }

        public override int GetHashCode() => hashCode;
        public override bool Equals(object? obj) => obj is ArrayBox<T> a && Equals(a);
        public bool Equals([AllowNull]ArrayBox<T> other) => other != null && Enumerable.SequenceEqual(Array!, other.Array!);
    }
}
