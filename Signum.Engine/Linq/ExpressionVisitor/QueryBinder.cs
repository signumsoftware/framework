using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using Signum.Engine.Maps;
using Signum.Entities.Reflection;
using Signum.Engine.Properties;

namespace Signum.Engine.Linq
{
    /// <summary>
    /// QueryBinder is a visitor that converts method calls to LINQ operations into 
    /// custom DbExpression nodes and references to class members into references to columns
    /// </summary>
    internal class QueryBinder : ExpressionVisitor
    {
        Dictionary<ParameterExpression, Expression> map = new Dictionary<ParameterExpression, Expression>();

        Dictionary<string, HashSet<TableCondition>> requests = new Dictionary<string, HashSet<TableCondition>>();

        internal class TableCondition
        {
            public TableExpression Table;
            public FieldInitExpression FieldInit;
        }

        int aliasCount = 0;
        int specialValueCount = 0; 

        private QueryBinder() { }

        static internal Expression Bind(Expression expression)
        {
            QueryBinder qb = new QueryBinder();
            return qb.Visit(expression);
        }


        private string GetNextAlias()
        {
            return "t" + (aliasCount++);
        }

        public string GetNextColumn()
        {
            return "val" + (specialValueCount++);
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Queryable) ||
                m.Method.DeclaringType == typeof(Enumerable))
            {
                switch (m.Method.Name)
                {
                    case "Where":
                        return this.BindWhere(m.Type, m.GetArgument("source"), m.GetArgument("predicate").StripQuotes());
                    case "Select":
                        return this.BindSelect(m.Type, m.GetArgument("source"), m.GetArgument("selector").StripQuotes());
                    case "SelectMany":
                        return this.BindSelectMany(m.Type, m.GetArgument("source"), m.GetArgument("selector").StripQuotes());
                    case "Join":
                        return this.BindJoin(
                            m.Type, m.GetArgument("outer"), m.GetArgument("inner"),
                            m.GetArgument("outerKeySelector").StripQuotes(),
                            m.GetArgument("innerKeySelector").StripQuotes(),
                            m.GetArgument("resultSelector").StripQuotes());
                    case "OrderBy":
                        return this.BindOrderBy(m.Type, m.GetArgument("source"), m.GetArgument("keySelector").StripQuotes(), OrderType.Ascending);
                    case "OrderByDescending":
                        return this.BindOrderBy(m.Type, m.GetArgument("source"), m.GetArgument("keySelector").StripQuotes(), OrderType.Descending);
                    case "ThenBy":
                        return this.BindThenBy(m.GetArgument("source"), m.GetArgument("keySelector").StripQuotes(), OrderType.Ascending);
                    case "ThenByDescending":
                        return this.BindThenBy(m.GetArgument("source"), m.GetArgument("keySelector").StripQuotes(), OrderType.Descending);
                    case "GroupBy":
                        return this.BindGroupBy(m.Type, m.GetArgument("source"),
                            m.GetArgument("keySelector").StripQuotes(),
                            m.GetArgument("elementSelector").StripQuotes());
                    case "Count":
                        return this.BindCount(m.Type, m.GetArgument("source"));
                    case "DefaultIfEmpty":
                        return Expression.Call(m.Method, Visit(m.GetArgument("source")));
                    case "Any":
                        return this.BindAny(m.Type, m.GetArgument("source")); 
                    case "All":
                        return this.BindAll(m.Type, m.GetArgument("source"), m.GetArgument("predicate").StripQuotes()); 
                    case "Contains":
                        return this.BindContains(m.Type, m.GetArgument("source"), m.TryGetArgument("item") ?? m.GetArgument("value"));   
                    case "Sum":
                    case "Min":
                    case "Max":
                    case "Average":
                        return this.BindAggregate(m.Type, m.Method.Name.ToEnum<AggregateFunction>(),
                            m.GetArgument("source"), m.TryGetArgument("selector").StripQuotes());
                    case "First":
                    case "FirstOrDefault":
                    case "Single":
                    case "SingleOrDefault":
                        return BindUniqueRow(m.Type, m.Method.Name.ToEnum<UniqueFunction>(),
                            m.GetArgument("source"), m.TryGetArgument("predicate").StripQuotes());
                    case "Distinct":
                        return BindDistinct(m.Type, m.GetArgument("source")); 
                    case "Take":
                        return BindTake(m.Type, m.GetArgument("source"), m.GetArgument("count"));
                    case "Skip":
                        return BindSkip(m.Type, m.GetArgument("source"), m.GetArgument("count"));
                    case "Union":
                    case "Concat": 
                    case "Except":
                    case "Intersect":
                        return BindSetOperation(m.Type, m.Method.Name.ToEnum<SetOperation>(),
                            m.TryGetArgument("first") ?? m.GetArgument("source1"),
                            m.TryGetArgument("second") ?? m.GetArgument("source2")); 
                }
            }
            else if (m.Method.DeclaringType == typeof(LazyUtils) && m.Method.Name == "ToLazy")
            {
                var entity = Visit(m.GetArgument("entity"));

                return new LazyReferenceExpression(m.Type, entity);
            }
            else if (m.Method.DeclaringType == typeof(object) && m.Method.Name == "ToString" && typeof(IdentifiableEntity).IsAssignableFrom(m.Object.Type))
            {
                return Visit(Expression.MakeMemberAccess(m.Object, ReflectionTools.GetFieldInfo<IdentifiableEntity>(ei => ei.toStr)));
            }
            else if (
                m.Method.DeclaringType.IsInstantiationOf(typeof(EnumProxy<>)) &&
                m.Method.Name == "ToEnum")
            {
                FieldInitExpression fi = (FieldInitExpression)Visit(m.Object);

                return new EnumExpression(m.Method.DeclaringType.GetGenericArguments()[0], (ColumnExpression)fi.ID);            
            }
            else if (m.Object != null && typeof(IList).IsAssignableFrom(m.Object.Type) && m.Method.Name == "Contains" && m.Object is ConstantExpression)
            {
                IList values = (IList)((ConstantExpression)m.Object).Value;

                Expression arg = Visit(m.Arguments[0]);

                var expList = values.Cast<object>().Select(a => Expression.Equal(arg, Visit(Expression.Constant(a, arg.Type)))).ToList();

                Expression expr = expList.Count == 0 ? Expression.Constant(false) : (Expression)expList.Aggregate((e1, e2) => Expression.Or(e1, e2));

                return expr;
            }
            return base.VisitMethodCall(m);
        }

        private Expression RetryMapAndVisit(LambdaExpression lambda, ref ProjectionExpression p1)
        {
            map.Add(lambda.Parameters[0], p1.Projector);

            var result = Visit(lambda.Body);

            if (ApplyExpansions(ref p1))
            {
                map[lambda.Parameters[0]] = p1.Projector;

                result = Visit(lambda.Body);
            }

            map.Remove(lambda.Parameters[0]);

            return result;
        }

        private Expression OneWayMapAndVisit(LambdaExpression lambda, params ProjectionExpression[] projs)
        {
            map.SetRange(lambda.Parameters, projs.Select(a => a.Projector));
            return Visit(lambda.Body); 
        }

        private void Clean(LambdaExpression lambda)
        {
            map.RemoveRange(lambda.Parameters); 
        }

        private bool ApplyExpansions(ref ProjectionExpression projection)
        {
            if (!requests.ContainsKey(projection.Source.Alias))
                return false;

            HashSet<TableCondition> allProjections = AllProjections(projection.Source.Alias).ToHashSet();

            Type type = projection.Type;
            string newAlias = GetNextAlias();

            string[] oldAliases = allProjections.Select(p => p.Table.Alias).And(projection.Source.Alias).ToArray();

            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, newAlias, oldAliases);

            JoinExpression source = (JoinExpression)allProjections.Aggregate((Expression)projection.Source, (e, p) =>
                new JoinExpression(type, JoinType.LeftOuterJoin, e, p.Table,
                  SmartEqualizer.EqualNullable(p.FieldInit.ID, p.FieldInit.Bindings.IDColumn()),
                true));

            projection = new ProjectionExpression(
                new SelectExpression(projection.Source.Type, newAlias, false, null, pc.Columns, source, null, null, null, null),
                pc.Projector, null);

            return true;
        }

        IEnumerable<TableCondition> AllProjections(string alias)
        {
            if (!requests.ContainsKey(alias))
                return new HashSet<TableCondition>();

            return requests.Extract(alias).SelectMany(tc => AllProjections(tc.Table.Alias).PreAnd(tc));
        }

        private ProjectionExpression VisitCastProjection(Expression source)
        {
            var visit = Visit(source);
            return AsProjection(visit);
        }

        private ProjectionExpression AsProjection(Expression result)
        {
            if (result.NodeType == ExpressionType.Call)
            {
                MethodCallExpression mca = (MethodCallExpression)result;
                if (mca.Method.Name == "New" && mca.Method.ReturnType.IsInstantiationOf(typeof(IGrouping<,>)))
                    return (ProjectionExpression)mca.Arguments[1];
            }
            else if (result.NodeType == (ExpressionType)DbExpressionType.MList)
            {
                return MListProjection((MListExpression)result);
            }

            return (ProjectionExpression)result;
        }

        private ProjectionExpression MListProjection(MListExpression mle)
        {
            RelationalTable tr = mle.RelationalTable;

            string tableAlias = GetNextAlias();
            TableExpression tableExpression = new TableExpression(tr.Field.FieldType, tableAlias, tr.Name);

            Expression expr = tr.CampoExpression(tableAlias);

            string selectAlias = GetNextAlias();

            ColumnExpression ce = tr.BackColumnExpression(tableAlias);

            ProjectedColumns pc = ColumnProjector.ProjectColumns(expr, selectAlias, tableAlias);

            var proj = new ProjectionExpression(
                new SelectExpression(mle.Type, selectAlias, false, null, pc.Columns, tableExpression,
                    SmartEqualizer.EqualNullable(mle.BackID, ce), null, null, null),
                 pc.Projector, null);

            return proj;
        }


        private Expression BindSetOperation(Type resultType, SetOperation setOperation, Expression left, Expression right)
        {
            ProjectionExpression leftProjection = this.VisitCastProjection(left);
            ProjectionExpression rightProjection = this.VisitCastProjection(right);

            leftProjection = SingleCellOptimizer.Optimize(AggregateOptimizer.Optimize(leftProjection));
            rightProjection = SingleCellOptimizer.Optimize(AggregateOptimizer.Optimize(rightProjection));

            if (ProjectionExpressionFinder.HasProjections(leftProjection.Projector) || ProjectionExpressionFinder.HasProjections(leftProjection.Projector))
                throw new NotImplementedException(Signum.Engine.Properties.Resources.SetOperationsAreNotAllowedOnQueriesWithNonSingleCellProjections); 

            string setAlias = GetNextAlias();
            string alias = GetNextAlias();

            SetOperationExpression setOper = new SetOperationExpression(resultType, setAlias, setOperation, leftProjection.Source, rightProjection.Source);

            ProjectedColumns dummyPC = ColumnProjector.ProjectColumns(leftProjection.Projector, setAlias, leftProjection.Source.Alias);
            ProjectedColumns pc = ColumnProjector.ProjectColumns(dummyPC.Projector, alias, setAlias);
        
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, false, null, pc.Columns, setOper, null, null, null, null),
                pc.Projector, null);
        }

        private Expression BindTake(Type resultType, Expression source, Expression count)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            string alias = this.GetNextAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, false, count, pc.Columns, projection.Source, null, null, null, null),
                pc.Projector, null);
        }

        private Expression BindSkip(Type resultType, Expression source, Expression count)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            string alias = this.GetNextAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, projection.Source.Alias);

            RowNumberExpression rne = new RowNumberExpression(
                pc.Columns.Select((c, i) =>
                    new OrderExpression(OrderType.Ascending, c.Expression)));

            ColumnDeclaration cd = new ColumnDeclaration("RowNumber", rne);

            SelectExpression se = new SelectExpression(resultType, alias, false, null, pc.Columns.PreAnd(cd), projection.Source, null, null, null, null);

            string alias2 = this.GetNextAlias();
            ProjectedColumns pc2 = ColumnProjector.ProjectColumns(pc.Projector, alias2, alias);

            Expression where = Expression.GreaterThan(new ColumnExpression(cd.Expression.Type, alias, cd.Name), count);

            return new ProjectionExpression(
                new SelectExpression(resultType, alias2, false, null, pc2.Columns, se, where, null, null, null),
                pc2.Projector, null);
        }

        private Expression BindUniqueRow(Type resultType, UniqueFunction function, Expression source, LambdaExpression predicate)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            Expression where = null;
            if (predicate != null)
                where = Nominator.FullNominate(RetryMapAndVisit(predicate, ref projection), true);

            string alias = this.GetNextAlias();
            Expression top = function == UniqueFunction.First || function ==  UniqueFunction.FirstOrDefault? Expression.Constant(1):null;

            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, false, top, pc.Columns, projection.Source, where, null, null, null),
                pc.Projector, function);
        }

        private Expression BindDistinct(Type resultType, Expression source)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            string alias = this.GetNextAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, true, null, pc.Columns, projection.Source, null, null, null, null),
                pc.Projector, null);
        }

        private Expression BindCount(Type resultType, Expression source)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            SelectExpression sourceSelect = projection.Source;
          
            string alias = this.GetNextAlias();
            ColumnDeclaration cd = new ColumnDeclaration(GetNextColumn(), new AggregateExpression(resultType, null, AggregateFunction.Count));

            return new ProjectionExpression(
                new SelectExpression(resultType, alias, false, null, new[] { cd }, sourceSelect, null, null, null, null),
                ColumnProjector.SingleProjection(cd, alias, resultType), UniqueFunction.Single);
        }

        private Expression BindAll(Type resultType, Expression source, LambdaExpression predicate)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            Expression where = Nominator.FullNominate(RetryMapAndVisit(predicate, ref projection), true);

            SelectExpression sourceSelect = projection.Source;

            string alias = this.GetNextAlias();
            ColumnDeclaration cd = new ColumnDeclaration(GetNextColumn(), new AggregateExpression(typeof(int), null, AggregateFunction.Count));

            return new ProjectionExpression(typeof(bool),
                new SelectExpression(typeof(int), alias, false, null, new[] { cd }, sourceSelect, Expression.Not(where), null, null, null),
                ColumnProjector.SingleProjection(cd, alias, typeof(int)), UniqueFunction.SingleIsZero);
        }

        private Expression BindAny(Type resultType, Expression source)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            SelectExpression sourceSelect = projection.Source;

            string alias = this.GetNextAlias();
            ColumnDeclaration cd = new ColumnDeclaration(GetNextColumn(), new AggregateExpression(typeof(int), null, AggregateFunction.Count));

            return new ProjectionExpression(typeof(bool),
                new SelectExpression(typeof(int), alias, false, null, new[] { cd }, sourceSelect, null, null, null, null),
                ColumnProjector.SingleProjection(cd, alias, typeof(int)), UniqueFunction.SingleGreaterThanZero);
        }

        private Expression BindContains(Type resultType, Expression source, Expression item)
        {
            Expression newItem = Visit(item);

            if (source.NodeType == ExpressionType.Constant && typeof(IEnumerable).IsAssignableFrom(source.Type) && !typeof(IQueryable).IsAssignableFrom(source.Type))
            {
                ConstantExpression ce = (ConstantExpression)source;
                IEnumerable ie = (IEnumerable)ce.Value;
                
                return new InExpression(newItem, ie== null ? new object[0]: ie.Cast<object>().ToArray());
            }

            ProjectionExpression projection = this.VisitCastProjection(source);
            

            Expression where = Nominator.FullNominate(SmartEqualizer.PolymorphicEqual(projection.Projector, newItem), true);

            string alias = this.GetNextAlias();
            ColumnDeclaration cd = new ColumnDeclaration(GetNextColumn(), new AggregateExpression(typeof(int), null, AggregateFunction.Count));

            return new ProjectionExpression(typeof(bool),
                new SelectExpression(typeof(int), alias, false, null, new[] { cd }, projection.Source, where, null, null, null),
                ColumnProjector.SingleProjection(cd, alias, typeof(int)), UniqueFunction.SingleGreaterThanZero);
        }

        private Expression BindAggregate(Type resultType, AggregateFunction aggregateFunction, Expression source, LambdaExpression selector)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            Expression aggregateSelector = null;
            if (selector != null)
            {
                aggregateSelector = Nominator.FullNominate(RetryMapAndVisit(selector, ref projection), false);
            }
            else
            {
                ColumnDeclaration cdAux = projection.Source.Columns.Single();
                aggregateSelector = new ColumnExpression(resultType, projection.Source.Alias, cdAux.Name);
            }

            ColumnDeclaration cd = new ColumnDeclaration(GetNextColumn(), new AggregateExpression(resultType, aggregateSelector, aggregateFunction));
            string alias = this.GetNextAlias();
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, false, null, new[] { cd }, projection.Source, null, null, null, null),
               ColumnProjector.SingleProjection(cd, alias, resultType), UniqueFunction.Single);
        }

        private Expression BindWhere(Type resultType, Expression source, LambdaExpression predicate)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            Expression where = Nominator.FullNominate(RetryMapAndVisit(predicate, ref projection), true);

            string alias = this.GetNextAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, false, null, pc.Columns, projection.Source, where, null, null, null),
                pc.Projector, null);
        }

        private Expression BindSelect(Type resultType, Expression source, LambdaExpression selector)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            Expression expression = RetryMapAndVisit(selector, ref projection);

            string alias = this.GetNextAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(expression, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, false, null, pc.Columns, projection.Source, null, null, null, null),
                pc.Projector, null);
        }

        protected virtual Expression BindSelectMany(Type resultType, Expression source, LambdaExpression collectionSelector)
        {
            ProjectionExpression oldProjection;
            ProjectionExpression projection = oldProjection = this.VisitCastProjection(source);

            Expression collectionExpression = RetryMapAndVisit(collectionSelector, ref projection);
            bool outer = OverloadingSimplifier.ExtractDefaultIfEmpty(ref collectionExpression);

            ProjectionExpression collectionProjection = AsProjection(collectionExpression);

            string alias = this.GetNextAlias();
            ProjectedColumns pc  = ColumnProjector.ProjectColumns(collectionProjection.Projector, alias, projection.Source.Alias, collectionProjection.Source.Alias);

            JoinType joinType = IsTable(collectionSelector.Body) ? JoinType.CrossJoin :
                                outer ? JoinType.OuterApply:
                                JoinType.CrossApply;

            JoinExpression join = new JoinExpression(resultType, joinType, projection.Source, collectionProjection.Source, null, false);

            return new ProjectionExpression(
                new SelectExpression(resultType, alias, false, null, pc.Columns, join, null, null, null, null),
                pc.Projector, null);
        }

        protected virtual Expression BindJoin(Type resultType, Expression outerSource, Expression innerSource, LambdaExpression outerKey, LambdaExpression innerKey, LambdaExpression resultSelector)
        {
            bool rightOuter = OverloadingSimplifier.ExtractDefaultIfEmpty(ref outerSource);
            bool leftOuter = OverloadingSimplifier.ExtractDefaultIfEmpty(ref innerSource);


            ProjectionExpression outerProjection = this.VisitCastProjection(outerSource);
            ProjectionExpression innerProjection = this.VisitCastProjection(innerSource);

            Expression outerKeyExpr = OneWayMapAndVisit(outerKey,  outerProjection);
            Expression innerKeyExpr = OneWayMapAndVisit(innerKey,  innerProjection);
            Expression resultExpr = OneWayMapAndVisit(resultSelector,  outerProjection,  innerProjection);

            if (ApplyExpansions(ref outerProjection) | ApplyExpansions(ref innerProjection))
            {
                outerKeyExpr = OneWayMapAndVisit(outerKey, outerProjection);
                innerKeyExpr = OneWayMapAndVisit(innerKey, innerProjection);
                resultExpr = OneWayMapAndVisit(resultSelector, outerProjection, innerProjection);
            }

            Clean(outerKey);
            Clean(innerKey);
            Clean(resultSelector);

            Expression condition = Nominator.FullNominate(SmartEqualizer.EqualNullable(outerKeyExpr, innerKeyExpr), true);

            JoinType jt = rightOuter && leftOuter ? JoinType.FullOuterJoin :
                          rightOuter ? JoinType.RightOuterJoin :
                          leftOuter ? JoinType.LeftOuterJoin :
                          JoinType.InnerJoin;

            JoinExpression join = new JoinExpression(resultType, jt, outerProjection.Source, innerProjection.Source, condition, false);
            string alias = this.GetNextAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(resultExpr, alias, outerProjection.Source.Alias, innerProjection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, false, null, pc.Columns, join, null, null, null, null),
                pc.Projector, null);
        }

        private Expression BindGroupBy(Type resultType, Expression source, LambdaExpression keySelector, LambdaExpression elementSelector)
        {
            Type keyType = keySelector.Body.Type;
            Type elementType = elementSelector.TryCC(es => es.Body.Type) ?? Reflector.CollectionType(source.Type);

            ProjectionExpression projection = this.VisitCastProjection(source);

            Expression getKey = Nominator.FullNominate(ProjectionCleaner.Clean(RetryMapAndVisit(keySelector, ref projection)), false);

            Expression elementProjector;
            if (elementSelector != null)
                elementProjector = RetryMapAndVisit(elementSelector, ref projection);
            else
                elementProjector = projection.Projector;

            string groupAlias = GetNextAlias();
            ProjectedColumns pcKey = ColumnProjector.ProjectColumns(getKey, groupAlias, projection.Source.Alias);

            string elementAlias = GetNextAlias();
            ProjectedColumns pcElements = ColumnProjector.ProjectColumns(elementProjector, elementAlias, projection.Source.Alias);

            MethodInfo mi = typeof(Grouping<,>).MakeGenericType(keyType, elementType).GetMethod("New");

            Type groupingType = typeof(IGrouping<,>).MakeGenericType(keyType, elementType);
            Type queryType = typeof(IQueryable<>).MakeGenericType(elementType);

            Expression groupingWhere = pcKey.Columns.Select(cd =>
                Expression.Equal(new ColumnExpression(cd.Expression.Type, groupAlias, cd.Name), cd.Expression))
                .Aggregate((a, b) => Expression.And(a, b));

            SelectExpression selectKey = new SelectExpression(resultType, groupAlias, false, null, pcKey.Columns, projection.Source, null, null, pcKey.Columns.Select(c => c.Expression), null);
            SelectExpression selectElements = new SelectExpression(queryType, elementAlias, false, null, pcElements.Columns, projection.Source, groupingWhere, null, null, groupAlias);

            ProjectionExpression elementsProjection = new ProjectionExpression(selectElements, pcElements.Projector, null);

            return new ProjectionExpression(selectKey,
                Expression.Call(mi, pcKey.Projector.TryConvert(keyType), elementsProjection), null);
        }
   
        List<OrderExpression> thenBys;
        protected virtual Expression BindOrderBy(Type resultType, Expression source, LambdaExpression orderSelector, OrderType orderType)
        {
            List<OrderExpression> myThenBys = this.thenBys;
            this.thenBys = null;
            ProjectionExpression projection = this.VisitCastProjection(source);

            List<OrderExpression> orderings = new List<OrderExpression>();
            orderings.Add(new OrderExpression(orderType, Nominator.FullNominate(RetryMapAndVisit(orderSelector, ref projection), false)));

            if (myThenBys != null)
            {
                for (int i = myThenBys.Count - 1; i >= 0; i--)
                {
                    OrderExpression tb = myThenBys[i];
                    orderings.Add(new OrderExpression(tb.OrderType, Nominator.FullNominate(RetryMapAndVisit((LambdaExpression)tb.Expression, ref projection), false)));
                }
            }

            string alias = this.GetNextAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, false, null, pc.Columns, projection.Source, null, orderings.AsReadOnly(), null, null),
                pc.Projector, null);
        }

        protected virtual Expression BindThenBy(Expression source, LambdaExpression orderSelector, OrderType orderType)
        {
            if (this.thenBys == null)
            {
                this.thenBys = new List<OrderExpression>();
            }
            this.thenBys.Add(new OrderExpression(orderType, orderSelector));
            return this.Visit(source);
        }

        private bool IsTable(Expression expression)
        {
            ConstantExpression c = expression as ConstantExpression;
            return c != null && TableType(c.Value) != null;
        }

        private Type TableType(object value)
        {
            return value.TryCC(v => v.GetType()
                .Map(t => t.IsInstantiationOf(typeof(Query<>)) ? t.GetGenericArguments()[0] : null));
        }

        private void FillFie(FieldInitExpression fie)
        {
            if (fie == null)
                throw new ArgumentException("fie");

            if (fie.Bindings != null)
                throw new ApplicationException(Resources.FieldInitExpressionBindingsShouldBeEmpty); 

            Table table = ConnectionScope.Current.Schema.Table(fie.Type);

            string tableAlias = this.GetNextAlias();
            var bindings = table.CreateBindings(tableAlias);

            fie.Bindings = bindings;

            TableExpression tableExpression = new TableExpression(fie.Type, tableAlias, table.Name);
            requests.GetOrCreate(fie.Alias).Add(new TableCondition { FieldInit = fie, Table = tableExpression });
        }


        private ProjectionExpression GetTableProjection(Type type)
        {
            Table table;
            if (typeof(IdentifiableEntity).IsAssignableFrom(type))
            {
                table = ConnectionScope.Current.Schema.Table(type);
            }
            else
            {
                ViewBuilder vb = new ViewBuilder(Schema.Current);
                table = vb.NewView(type);
            }

            string tableAlias = this.GetNextAlias();
            Type resultType = typeof(IEnumerable<>).MakeGenericType(type);
            TableExpression tableExpression = new TableExpression(resultType, tableAlias, table.Name);

            var bindings = table.CreateBindings(tableAlias);
            FieldInitExpression fie = new FieldInitExpression(type, tableAlias, table.IsView ? null : bindings.IDColumn()) { Bindings = bindings };

            string selectAlias = this.GetNextAlias();

            ProjectedColumns pc = ColumnProjector.ProjectColumns(fie, selectAlias, tableAlias);
            var projection = new ProjectionExpression(
                new SelectExpression(resultType, selectAlias, false, null, pc.Columns,
               tableExpression, null, null, null, null),
            pc.Projector, null);

            return projection;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            Type type = TableType(c.Value);
            if (type != null)
                return GetTableProjection(type);
            if (typeof(IIdentifiable).IsAssignableFrom(c.Type))
            {
                IdentifiableEntity ei = (IdentifiableEntity)c.Value; // podria ser null y lo meteriamos igualmente
                if (ei == null)
                    return new FieldInitExpression(c.Type, null, Expression.Constant(null, typeof(int?)));

                return new FieldInitExpression(ei.GetType(), null, Expression.Constant(ei.Id));
            }
            else if (typeof(Lazy).IsAssignableFrom(c.Type))
            {
                Lazy lazy = (Lazy)c.Value;

                if (lazy == null)
                    return new LazyReferenceExpression(c.Type, new FieldInitExpression(Reflector.ExtractLazy(c.Type), null, Expression.Constant(null, typeof(int?)))); //puede dar problemas con lazy de tipo interface

                return new LazyReferenceExpression(c.Type, new FieldInitExpression(lazy.RuntimeType, null, Expression.Constant(lazy.IdOrNull ?? int.MinValue)));
            }
            return c;
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            return map.TryGetC(p) ?? p;
        }

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            Expression source = Visit(m.Expression); 
            if(source.NodeType ==  (ExpressionType)DbExpressionType.Projection)
            {
                ProjectionExpression proj = ((ProjectionExpression)source);
                if (proj.UniqueFunction.HasValue)
                {
                    source = proj.Projector;
                }
            }

            switch (source.NodeType)
            {
                case ExpressionType.MemberInit:
                    return ((MemberInitExpression)source).Bindings
                        .OfType<MemberAssignment>()
                        .Single(a => ReflectionTools.MemeberEquals(a.Member, m.Member)).Expression;
                case ExpressionType.New:
                    NewExpression nex = (NewExpression)source;
                    MethodInfo mi = ((PropertyInfo)m.Member).GetGetMethod();
                    return nex.Members.Zip(nex.Arguments).Single(p => ReflectionTools.MethodEqual((MethodInfo)p.First, mi)).Second; 
                case ExpressionType.Call:
                    MethodCallExpression mca = (MethodCallExpression)source;
                    if (mca.Method.DeclaringType.IsInstantiationOf(typeof(Grouping<,>)) && 
                       mca.Method.Name == "New" && m.Member.Name == "Key")
                        return mca.Arguments[0];
                    break;
                case (ExpressionType)DbExpressionType.FieldInit:
                    FieldInitExpression fie = (FieldInitExpression)source;
                    FieldInfo fi = m.Member as FieldInfo ?? Reflector.FindFieldInfo((PropertyInfo)m.Member);
                    if (fi == null)
                        throw new ApplicationException(Resources.NoFieldFoundForMember0.Formato(m.Member.MemberName()));

                    if (fi.FieldEquals<IdentifiableEntity>(ei => ei.id))
                        return fie.ID;

                    if (fie.Bindings == null)
                         FillFie(fie);

                    FieldBinding binding = fie.Bindings.SingleOrDefault(b => ReflectionTools.FieldEquals(b.FieldInfo, fi));
                    if (binding == null)
                        throw new ApplicationException(Resources.TheField0IsNotIncluded.Formato(m.Member.MemberName()));

                    return binding.Binding;
                case (ExpressionType)DbExpressionType.LazyReference:
                    if ((m.Member as PropertyInfo).TryCS(pi => pi.Name == "EntityOrNull") ?? false)
                        return ((LazyReferenceExpression)source).Reference;
                    else
                        throw new ApplicationException("The member {0} of Lazy is no accesible on queries, use EntityOrNull instead".Formato(m.Member));
            }
      
            return MakeMemberAccess(source, m.Member);
        }

        protected override Expression VisitTypeIs(TypeBinaryExpression b)
        {
            Expression operand = Visit(b.Expression);
            if (operand.NodeType == (ExpressionType)DbExpressionType.ImplementedBy)
            {
                ImplementedByExpression rib = (ImplementedByExpression)operand;
                FieldInitExpression fie = rib.Implementations.Where(ri => ri.Type == b.TypeOperand).Single(Resources.TheFieldHasNoImplementationForType0.Formato(b.Type.TypeName())).Field;
                return Expression.NotEqual(fie.ID, Expression.Constant(null, fie.ID.Type));
            }
            else if (operand.NodeType == (ExpressionType)DbExpressionType.ImplementedByAll)
            {
                ImplementedByAllExpression riba = (ImplementedByAllExpression)operand;
                int idType = Schema.Current.IDsForType.GetOrThrow(b.TypeOperand, Resources.TheType0IsNotInTheTypesTable.Formato(b.Type.TypeName()));
                return SmartEqualizer.EqualNullable(riba.TypeID, Expression.Constant(idType));
            }
            return base.VisitTypeIs(b); 
        }
       
        protected override Expression VisitUnary(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.TypeAs || u.NodeType == ExpressionType.Convert)
            {
                Expression operand = Visit(u.Operand);

                if (operand.NodeType == (ExpressionType)DbExpressionType.ImplementedBy)
                {
                    ImplementedByExpression rib = (ImplementedByExpression)operand;
                    return rib.Implementations.Where(ri => ri.Type == u.Type).Single(Resources.TheFieldHasNoImplementationForType0.Formato(u.Type.TypeName())).Field;
                }
                else if (operand.NodeType == (ExpressionType)DbExpressionType.ImplementedByAll)
                {
                    ImplementedByAllExpression riba = (ImplementedByAllExpression)operand;
                    ImplementationColumnExpression imp = riba.Implementations.SingleOrDefault(ri => ri.Type == u.Type);

                    if (imp == null)
                    {
                        int idType = Schema.Current.IDsForType.GetOrThrow(u.Type, Resources.TheType0IsNotInTheTypesTable.Formato(u.Type.TypeName()));

                        Expression column = new CaseExpression(
                                        new[] { new When(SmartEqualizer.EqualNullable(riba.TypeID, Expression.Constant(idType)), riba.ID) },
                                        Expression.Constant(null, typeof(int?)));

                        FieldInitExpression result = new FieldInitExpression(u.Type, riba.TypeID.Alias, column);
                        riba.Implementations.Add(new ImplementationColumnExpression(u.Type, result));
                        return result;
                    }
                    else
                        return imp.Field;
                }
                else if (typeof(IIdentifiable).IsAssignableFrom(u.Type))
                    return operand;
                else if (operand != u.Operand)
                    return Expression.MakeUnary(u.NodeType, operand, u.Type, u.Method);
                else
                    return u;
            }

            return base.VisitUnary(u); 
        }

        internal static Expression MakeMemberAccess(Expression source, MemberInfo mi)
        {
            FieldInfo fi = mi as FieldInfo;
            if (fi != null)
            {
                return Expression.Field(source, fi);
            }
            PropertyInfo pi = (PropertyInfo)mi;
            return Expression.Property(source, pi);
        }

        //On Sql, nullability has no sense
        protected override Expression VisitBinary(BinaryExpression b)
        {
            Expression left = this.Visit(b.Left);
            Expression right = this.Visit(b.Right);
            Expression conversion = this.Visit(b.Conversion);
            if (left != b.Left || right != b.Right || conversion != b.Conversion)
            {
                if (b.NodeType == ExpressionType.Coalesce && b.Conversion != null)
                    return Expression.Coalesce(left, right, conversion as LambdaExpression);
                else
                {
                    if (left.Type.IsNullable() == right.Type.IsNullable())
                        return Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
                    else
                        return Expression.MakeBinary(b.NodeType, left.Nullify(), right.Nullify());
                }
            }
            return b;
        }
    }
}
