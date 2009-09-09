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
        Dictionary<Expression, GroupByInfo> groupByMap = new Dictionary<Expression, GroupByInfo>();
        Dictionary<string, HashSet<TableCondition>> requests = new Dictionary<string, HashSet<TableCondition>>();
        Expression root;

        internal static readonly PropertyInfo ToStrProperty = ReflectionTools.GetPropertyInfo<IIdentifiable>(ii => ii.ToStr);
        internal static readonly PropertyInfo IdProperty = ReflectionTools.GetPropertyInfo<IIdentifiable>(ii => ii.Id); 

        internal class TableCondition
        {
            public TableExpression Table;
            public FieldInitExpression FieldInit;
        }

        private QueryBinder() { }

        static internal Expression Bind(Expression expression)
        {
            QueryBinder qb = new QueryBinder { root = expression };
            return qb.Visit(expression);
        }

        int selectAliasCount = 0;
        private string GetNextSelectAlias()
        {
            return "s" + (selectAliasCount++);
        }

        int tableAliasCount = 0;
        private string GetNextTableAlias()
        {
            return "t" + (tableAliasCount++);
        }

        int specialValueCount = 0; 
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
                    case "DefaultIfEmpty":
                        return Expression.Call(m.Method, Visit(m.GetArgument("source")));
                    case "Any":
                    case "All":
                        return this.BindAnyAll(m.Type, m.GetArgument("source"), m.GetArgument("predicate").StripQuotes(), m.Method, m == root); 
                    case "Contains":
                        return this.BindContains(m.Type, m.GetArgument("source"), m.TryGetArgument("item") ?? m.GetArgument("value"), m == root);
                    case "Count":
                    case "Sum":
                    case "Min":
                    case "Max":
                    case "Average":
                        return this.BindAggregate(m.Type, m.Method.Name.ToEnum<AggregateFunction>(),
                            m.GetArgument("source"), m.TryGetArgument("selector").StripQuotes(), m == root);
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
                }
            }
            else if (m.Method.DeclaringType == typeof(LazyUtils) && m.Method.Name == "ToLazy")
            {
                if (m.Method.GetParameters().First().ParameterType == typeof(Lazy))
                {
                    LazyReferenceExpression lazyRef = (LazyReferenceExpression)Visit(m.GetArgument("lazy"));

                    return new LazyReferenceExpression(m.Type, lazyRef.Reference, lazyRef.Id, lazyRef.ToStr, lazyRef.TypeId);
                }
                else
                {
                    var entity = Visit(m.GetArgument("entity"));
                    return MakeLazy(m.Type, entity);
                }
            }
            else if (m.Method.DeclaringType == typeof(object) && m.Method.Name == "ToString" && typeof(IdentifiableEntity).IsAssignableFrom(m.Object.Type))
            {
                return Visit(Expression.MakeMemberAccess(m.Object, ReflectionTools.GetFieldInfo<IdentifiableEntity>(ei => ei.toStr)));
            }
            else if ( m.Method.DeclaringType.IsInstantiationOf(typeof(EnumProxy<>)) &&
                m.Method.Name == "ToEnum")
            {
                FieldInitExpression fi = (FieldInitExpression)Visit(m.Object);

                return Expression.Convert((ColumnExpression)fi.ExternalId, m.Method.DeclaringType.GetGenericArguments()[0]);            
            }
            else if (m.Object != null && typeof(IList).IsAssignableFrom(m.Object.Type) && m.Method.Name == "Contains")
            {
                return this.BindContains(m.Type, m.Object, m.Arguments[0], m == root);   
            }
            return base.VisitMethodCall(m);
        }



        internal static SqlConstantExpression TypeConstant(Type type)
        {
            return new SqlConstantExpression(Schema.Current.IDsForType[type], typeof(int?));
        }

        internal Expression Coalesce(Type type, IEnumerable<Expression> exp)
        {
            var list = exp.ToList();

            if (list.Count == 0)
                return Expression.Constant(null, type);

            if (list.Count == 1)
                return list[0]; //Not regular, but usefull

            return new SqlFunctionExpression(type, SqlFunction.COALESCE.ToString(), list);  
        }

        private Expression RetryMapAndVisit(LambdaExpression lambda, ref ProjectionExpression p1)
        {
            map.Add(lambda.Parameters[0], p1.Projector);

            Expression result;
            using (Alias(p1.Source.Alias))
                result = Visit(lambda.Body);

            if (ApplyExpansions(ref p1))
            {
                map[lambda.Parameters[0]] = p1.Projector;

                result = Visit(lambda.Body);
            }

            map.Remove(lambda.Parameters[0]);

            return result;
        }

        public string CurrentAlias {get;set;}
        public IDisposable Alias(string alias)
        {
            string oldAlias = CurrentAlias;
            CurrentAlias = alias;
            return new Disposable(() => CurrentAlias = oldAlias); 
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
            string newAlias = GetNextSelectAlias();

            string[] oldAliases = allProjections.Select(p => p.Table.Alias).And(projection.Source.Alias).ToArray();

            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, newAlias, oldAliases);

            JoinExpression source = (JoinExpression)allProjections.Aggregate((SourceExpression)projection.Source, (e, p) =>
            {
                var externalID = DbExpressionNominator.FullNominate(p.FieldInit.ExternalId, false);

                Expression equal = SmartEqualizer.EqualNullable(externalID, p.FieldInit.GetFieldBinding(FieldInitExpression.IdField));
                Expression condition = p.FieldInit.OtherCondition == null ? equal : Expression.And(p.FieldInit.OtherCondition, equal);
                return new JoinExpression(type, JoinType.SingleRowLeftOuterJoin, e, p.Table, condition);
            });

            projection = new ProjectionExpression(
                new SelectExpression(newAlias, false, null, pc.Columns, source, null, null, null),
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

        private ProjectionExpression AsProjection(Expression expression)
        {
            expression = RemoveConvert(expression);

            if (expression.NodeType == ExpressionType.New)
            {
                NewExpression nex = (NewExpression)expression;
                if (nex.Type.IsInstantiationOf(typeof(Grouping<,>)))
                    return (ProjectionExpression)nex.Arguments[1]; 
            }
            else if (expression.NodeType == (ExpressionType)DbExpressionType.MList)
            {
                return MListProjection((MListExpression)expression);
            }

            return (ProjectionExpression)expression;
        }

        private static Expression RemoveConvert(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert)
                expression = ((UnaryExpression)expression).Operand;
            return expression;
        }

        private ProjectionExpression MListProjection(MListExpression mle)
        {
            RelationalTable tr = mle.RelationalTable;

            string tableAlias = GetNextTableAlias();
            TableExpression tableExpression = new TableExpression(tr.Field.FieldType, tableAlias, tr.Name);

            Expression expr = tr.FieldExpression(tableAlias, this);

            string selectAlias = GetNextSelectAlias();

            ColumnExpression ce = tr.BackColumnExpression(tableAlias);

            ProjectedColumns pc = ColumnProjector.ProjectColumns(expr, selectAlias, tableExpression.KnownAliases);

            var proj = new ProjectionExpression(
                new SelectExpression(selectAlias, false, null, pc.Columns, tableExpression, SmartEqualizer.EqualNullable(mle.BackID, ce), null, null),
                 pc.Projector, null);

            return proj;
        }

        private Expression BindTake(Type resultType, Expression source, Expression count)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            string alias = this.GetNextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, projection.Source.KnownAliases);
            return new ProjectionExpression(
                new SelectExpression(alias, false, count, pc.Columns, projection.Source, null, null, null),
                pc.Projector, null);
        }

        private Expression BindSkip(Type resultType, Expression source, Expression count)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            string alias = this.GetNextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, projection.Source.KnownAliases);

            RowNumberExpression rne = new RowNumberExpression(
                pc.Columns.Select((c, i) =>
                    new OrderExpression(OrderType.Ascending, c.Expression)));

            ColumnDeclaration cd = new ColumnDeclaration("RowNumber", rne);

            SelectExpression se = new SelectExpression(alias, false, null, pc.Columns.PreAnd(cd), projection.Source, null, null, null);

            string alias2 = this.GetNextSelectAlias();
            ProjectedColumns pc2 = ColumnProjector.ProjectColumns(pc.Projector, alias2, alias);

            Expression where = Expression.GreaterThan(new ColumnExpression(cd.Expression.Type, alias, cd.Name), count);

            return new ProjectionExpression(
                new SelectExpression(alias2, false, null, pc2.Columns, se, where, null, null),
                pc2.Projector, null);
        }

        private Expression BindUniqueRow(Type resultType, UniqueFunction function, Expression source, LambdaExpression predicate)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            Expression where = null;
            if (predicate != null)
                where = DbExpressionNominator.FullNominate(RetryMapAndVisit(predicate, ref projection), true);

            string alias = this.GetNextSelectAlias();
            Expression top = function == UniqueFunction.First || function ==  UniqueFunction.FirstOrDefault? Expression.Constant(1):null;

            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, projection.Source.KnownAliases);
            return new ProjectionExpression(
                new SelectExpression(alias, false, top, pc.Columns, projection.Source, where, null, null),
                pc.Projector, function);
        }

        private Expression BindDistinct(Type resultType, Expression source)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            string alias = this.GetNextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, projection.Source.KnownAliases);
            return new ProjectionExpression(
                new SelectExpression(alias, true, null, pc.Columns, projection.Source, null, null, null),
                pc.Projector, null);
        }


        private Expression BindAggregate(Type resultType, AggregateFunction aggregateFunction, Expression source, LambdaExpression selector, bool isRoot)
        {
            resultType = resultType.Nullify(); //SQL is Ork's language!

            ProjectionExpression projection = this.VisitCastProjection(source);
            Expression exp =
                aggregateFunction == AggregateFunction.Count ? null :
                selector != null ? DbExpressionNominator.FullNominate(RetryMapAndVisit(selector, ref projection), false) :
                projection.Projector;

            string alias = this.GetNextSelectAlias();
            ColumnDeclaration cd = new ColumnDeclaration("a",
                new AggregateExpression(resultType, exp, aggregateFunction));

            SelectExpression select = new SelectExpression(alias, false, null, new[] { cd }, projection.Source, null, null, null);

            if (isRoot)
                return new ProjectionExpression(select, ColumnProjector.SingleProjection(cd, alias, resultType), UniqueFunction.Single);

            ScalarExpression subquery = new ScalarExpression(resultType, select);

            GroupByInfo info = groupByMap.TryGetC(projection);
            if (info != null)
            {
                Expression exp2 =
                     aggregateFunction == AggregateFunction.Count ? null :
                     selector != null ? DbExpressionNominator.FullNominate(RetryMapAndVisit(selector, ref info.Projection), false) :
                     info.Projection.Projector;

                return new AggregateSubqueryExpression(info.Alias, new AggregateExpression(resultType, exp2, aggregateFunction), subquery);
            }

            return subquery;
        }
           

        private Expression BindAnyAll(Type resultType, Expression source, LambdaExpression predicate, MethodInfo method, bool isRoot)
        {
            bool isAll = method.Name == "All";
            ConstantExpression constSource = source as ConstantExpression;
            if (constSource != null && !typeof(IQueryable).IsAssignableFrom(constSource.Type))
            {
                System.Diagnostics.Debug.Assert(!isRoot);
                Type oType = predicate.Parameters[0].Type;
                Expression[] exp = ((IEnumerable)constSource.Value).Cast<object>().Select(o => Expression.Invoke(predicate, Expression.Constant(o, oType))).ToArray();

                Expression where = isAll ? exp.Aggregate((a, b) => Expression.And(a, b)) :
                                           exp.Aggregate((a, b) => Expression.Or(a, b));

                return this.Visit(where);
            }
            else
            {
                if (isAll)
                    predicate = Expression.Lambda(Expression.Not(predicate.Body), predicate.Parameters.ToArray());
                
                if (predicate != null)
                    source = Expression.Call(typeof(Enumerable), "Where", method.GetGenericArguments(), source, predicate);

                ProjectionExpression projection = this.VisitCastProjection(source);
                Expression result = new ExistsExpression(projection.Source);
                if (isAll)
                    result = Expression.Not(result);

                if (isRoot)
                    return GetUniqueProjection(result, UniqueFunction.SingleOrDefault);
                else
                    return result;
            }
        }

        private Expression BindContains(Type resultType, Expression source, Expression item, bool isRoot)
        {
            Expression newItem = Visit(item);

            if (source.NodeType == ExpressionType.Constant && !typeof(IQueryable).IsAssignableFrom(source.Type)) //!isRoot
            {
                ConstantExpression ce = (ConstantExpression)source;
                IEnumerable col = (IEnumerable)ce.Value;

                Type colType = ReflectionTools.CollectionType(source.Type);
                if (typeof(IIdentifiable).IsAssignableFrom(colType))
                    return SmartEqualizer.EntityIn(newItem, col.Cast<IIdentifiable>().Select(ie => ToFieldInitExpression(colType, ie)).ToArray());
                else if (typeof(Lazy).IsAssignableFrom(colType))
                    return SmartEqualizer.EntityIn(newItem, col.Cast<Lazy>().Select(lazy => ToLazyReferenceExpression(colType, lazy)).ToArray());
                else
                    return new InExpression(newItem, col == null ? new object[0] : col.Cast<object>().ToArray());
            }
            else
            {
                ProjectionExpression projection = this.VisitCastProjection(source);

                string alias = this.GetNextSelectAlias();
                var pc = ColumnProjector.ProjectColumns(projection.Projector, alias, projection.Source.KnownAliases);

                SubqueryExpression se = null;
                if (pc.Columns.Count == 1)
                    se = new InExpression(newItem, projection.Source);
                else
                {
                    Expression where = DbExpressionNominator.FullNominate(SmartEqualizer.PolymorphicEqual(projection.Projector, newItem), true);
                    se = new ExistsExpression(new SelectExpression(alias, false, null, pc.Columns, projection.Source, where, null, null));
                }

                if (isRoot)
                    return this.GetUniqueProjection(se, UniqueFunction.SingleOrDefault);
                else
                    return se;
            }
        }

        private ProjectionExpression GetUniqueProjection(Expression expr, UniqueFunction uniqueFunction)
        {
            if (expr.Type != typeof(bool))
                throw new ArgumentException("expr");

            var alias = this.GetNextSelectAlias();
            Expression exprAsValue = ConditionsRewriter.MakeSqlValue(expr);
            SelectExpression select = new SelectExpression(alias, false, null, new[] { new ColumnDeclaration("value", exprAsValue) }, null, null, null, null);
            return new ProjectionExpression(select, new ColumnExpression(expr.Type, alias, "value"), uniqueFunction);
        }

        class GroupByInfo
        {
            internal string Alias ;
            internal ProjectionExpression Projection;

            public override string ToString()
            {
                return "Alias {0} \tProjection {1}".Formato(Alias, Projection.NiceToString());
            }
        }

        private Expression BindWhere(Type resultType, Expression source, LambdaExpression predicate)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            Expression where = DbExpressionNominator.FullNominate(RetryMapAndVisit(predicate, ref projection), true);

            string alias = this.GetNextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, projection.Source.KnownAliases);
            return new ProjectionExpression(
                new SelectExpression(alias, false, null, pc.Columns, projection.Source, where, null, null),
                pc.Projector, null);
        }

        private Expression BindSelect(Type resultType, Expression source, LambdaExpression selector)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            Expression expression = RetryMapAndVisit(selector, ref projection);

            string alias = this.GetNextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(expression, alias, projection.Source.KnownAliases);
            return new ProjectionExpression(
                new SelectExpression(alias, false, null, pc.Columns, projection.Source, null, null, null),
                pc.Projector, null);
        }

        protected virtual Expression BindSelectMany(Type resultType, Expression source, LambdaExpression collectionSelector)
        {
            ProjectionExpression oldProjection;
            ProjectionExpression projection = oldProjection = this.VisitCastProjection(source);

            Expression collectionExpression = RetryMapAndVisit(collectionSelector, ref projection);
            bool outer = OverloadingSimplifier.ExtractDefaultIfEmpty(ref collectionExpression);

            ProjectionExpression collectionProjection = AsProjection(collectionExpression);

            string alias = this.GetNextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(collectionProjection.Projector, alias, projection.Source.KnownAliases.Concat(collectionProjection.Source.KnownAliases).ToArray());

            JoinType joinType = IsTable(collectionSelector.Body) ? JoinType.CrossJoin :
                                outer ? JoinType.OuterApply:
                                JoinType.CrossApply;

            JoinExpression join = new JoinExpression(resultType, joinType, projection.Source, collectionProjection.Source, null);

            return new ProjectionExpression(
                new SelectExpression(alias, false, null, pc.Columns, join, null, null, null),
                pc.Projector, null);
        }

        protected virtual Expression BindJoin(Type resultType, Expression outerSource, Expression innerSource, LambdaExpression outerKey, LambdaExpression innerKey, LambdaExpression resultSelector)
        {
            bool rightOuter = OverloadingSimplifier.ExtractDefaultIfEmpty(ref outerSource);
            bool leftOuter = OverloadingSimplifier.ExtractDefaultIfEmpty(ref innerSource);

            ProjectionExpression outerProj = VisitCastProjection(outerSource);
            ProjectionExpression innerProj = VisitCastProjection(innerSource);

            Expression outerKeyExpr = RetryMapAndVisit(outerKey, ref outerProj);
            Expression innerKeyExpr = RetryMapAndVisit(innerKey, ref innerProj);

            Expression condition = DbExpressionNominator.FullNominate(SmartEqualizer.EqualNullable(outerKeyExpr, innerKeyExpr), true);

            JoinType jt = rightOuter && leftOuter ? JoinType.FullOuterJoin :
                          rightOuter ? JoinType.RightOuterJoin :
                          leftOuter ? JoinType.LeftOuterJoin :
                          JoinType.InnerJoin;

            string alias = this.GetNextSelectAlias();
            Expression resultExpr;
            using (Alias(alias))
                resultExpr = OneWayMapAndVisit(resultSelector, outerProj, innerProj);

            JoinExpression join = new JoinExpression(resultType, jt, outerProj.Source, innerProj.Source, condition);

            ProjectedColumns pc = ColumnProjector.ProjectColumns(resultExpr, alias, outerProj.Source.KnownAliases.Concat(innerProj.Source.KnownAliases).ToArray());
            ProjectionExpression result = new ProjectionExpression(
                new SelectExpression(alias, false, null, pc.Columns, join, null, null, null),
                pc.Projector, null);

            ApplyExpansions(ref result);
            return result; 
        }

        private Expression BindGroupBy(Type resultType, Expression source, LambdaExpression keySelector, LambdaExpression elementSelector)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            Expression keyExpr = this.RetryMapAndVisit(keySelector, ref projection);
            // Use ProjectColumns to get group-by expressions from key expression
            ProjectedColumns keyProjection = ColumnProjector.ProjectColumns(keyExpr, projection.Source.Alias, projection.Source.KnownAliases);
            IEnumerable<Expression> groupExprs = keyProjection.Columns.Select(c => c.Expression);

            // make duplicate of source query as basis of element subquery by visiting the source again
            ProjectionExpression subqueryBasis = this.VisitCastProjection(source);
            // recompute key columns for group expressions relative to subquery (need these for doing the correlation predicate)
            Expression subqueryKey = RetryMapAndVisit(keySelector, ref subqueryBasis);
            // use same projection trick to get group-by expressions based on subquery
            ProjectedColumns subqueryKeyPC = ColumnProjector.ProjectColumns(subqueryKey, subqueryBasis.Source.Alias, subqueryBasis.Source.KnownAliases);

            IEnumerable<Expression> subqueryGroupExprs = subqueryKeyPC.Columns.Select(c => c.Expression);
            Expression subqueryCorrelation =
                subqueryGroupExprs.Zip(groupExprs, (e1, e2) => SmartEqualizer.EqualNullable(e1, e2))
                .Aggregate((a, b) => Expression.And(a, b));


            Expression elemExpr = null;
            if (elementSelector != null)
                elemExpr = this.RetryMapAndVisit(elementSelector, ref projection);
            else
                elemExpr = projection.Projector;

            // compute element based on duplicated subquery
            Expression subqueryElemExpr = null;
            if (elementSelector != null)
                subqueryElemExpr = this.RetryMapAndVisit(elementSelector, ref subqueryBasis);
            else
                subqueryElemExpr = subqueryBasis.Projector;

            // build subquery that projects the desired element
            var elementAlias = this.GetNextSelectAlias();
            ProjectedColumns elementPC = ColumnProjector.ProjectColumns(subqueryElemExpr, elementAlias, subqueryBasis.Source.KnownAliases);
            ProjectionExpression elementSubquery =
                new ProjectionExpression(
                    new SelectExpression(elementAlias, false, null, elementPC.Columns, subqueryBasis.Source, subqueryCorrelation, null, null),
                    elementPC.Projector, null);

            var alias = this.GetNextSelectAlias();

            // make it possible to tie aggregates back to this group-by

            //this.groupByMap.Add(elementSubquery, info);

            Expression resultExpr = Expression.Convert(
                Expression.New(typeof(Grouping<,>).MakeGenericType(keyExpr.Type, subqueryElemExpr.Type).GetConstructors()[1],
                        new Expression[] { keyExpr, elementSubquery }), typeof(IGrouping<,>).MakeGenericType(keyExpr.Type, subqueryElemExpr.Type));

            ProjectedColumns pc = ColumnProjector.ProjectColumns(resultExpr, alias, projection.Source.KnownAliases);

            // make it possible to tie aggregates back to this group-by
            NewExpression newResult = (NewExpression)RemoveConvert(pc.Projector);
            GroupByInfo info = new GroupByInfo { Alias = alias, Projection = new ProjectionExpression(projection.Source, elemExpr, null) };
            this.groupByMap.Add(newResult.Arguments[1], info);

            return new ProjectionExpression(
                new SelectExpression(alias, false, null, pc.Columns, projection.Source, null, null, groupExprs),
                pc.Projector, null);
        }
   
        List<OrderExpression> thenBys;
        protected virtual Expression BindOrderBy(Type resultType, Expression source, LambdaExpression orderSelector, OrderType orderType)
        {
            List<OrderExpression> myThenBys = this.thenBys;
            this.thenBys = null;
            ProjectionExpression projection = this.VisitCastProjection(source);

            List<OrderExpression> orderings = new List<OrderExpression>();
            orderings.Add(new OrderExpression(orderType, DbExpressionNominator.FullNominate(RetryMapAndVisit(orderSelector, ref projection), false)));

            if (myThenBys != null)
            {
                for (int i = myThenBys.Count - 1; i >= 0; i--)
                {
                    OrderExpression tb = myThenBys[i];
                    orderings.Add(new OrderExpression(tb.OrderType, DbExpressionNominator.FullNominate(RetryMapAndVisit((LambdaExpression)tb.Expression, ref projection), false)));
                }
            }

            string alias = this.GetNextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, projection.Source.KnownAliases);
            return new ProjectionExpression(
                new SelectExpression(alias, false, null, pc.Columns, projection.Source, null, orderings.AsReadOnly(), null),
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

        //private void FillFie(FieldInitExpression fie)
        //{
        //    if (fie == null)
        //        throw new ArgumentException("fie");

        //    if (fie.Bindings != null)
        //        throw new ApplicationException(Resources.FieldInitExpressionBindingsShouldBeEmpty); 

        //    Table table = ConnectionScope.Current.Schema.Table(fie.Type);

        //    string tableAlias = this.GetNextAlias();

        //    fie.Bindings = table.CreateBinding(tableAlias);

        //    TableExpression tableExpression = new TableExpression(fie.Type, tableAlias, table.Name);

        //    requests.GetOrCreate(fie.CurrentAlias).Add(new TableCondition { FieldInit = fie, Table = tableExpression });
        //}

        private ProjectionExpression GetTableProjection(Type type)
        {
            string tableAlias = this.GetNextTableAlias();
            Type resultType = typeof(IEnumerable<>).MakeGenericType(type);

            Expression exp; 
            Table table;
            TableExpression tableExpression; 
            if (typeof(IdentifiableEntity).IsAssignableFrom(type))
            {
                table = ConnectionScope.Current.Schema.Table(type);

                tableExpression = new TableExpression(resultType, tableAlias, table.Name);

                Expression id = table.CreateBinding(tableAlias, FieldInitExpression.IdField, this);
                exp = new FieldInitExpression(type, tableAlias, id, null)
                {
                    Bindings = { new FieldBinding(FieldInitExpression.IdField, id) }
                };
            }
            else
            {
                ViewBuilder vb = new ViewBuilder(Schema.Current);
                table = vb.NewView(type);

                tableExpression = new TableExpression(resultType, tableAlias, table.Name);

                exp = table.GetViewExpression(tableAlias, this); 
            }
            string selectAlias = this.GetNextSelectAlias();

            ProjectedColumns pc = ColumnProjector.ProjectColumns(exp, selectAlias, tableAlias);
            var projection = new ProjectionExpression(
                new SelectExpression(selectAlias, false, null, pc.Columns, tableExpression, null, null, null),
            pc.Projector, null);

            return projection;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            Type type = TableType(c.Value);
            
            if (type != null)
                return GetTableProjection(type);

            if (c.Value == null)
                return c; 

            if (typeof(IIdentifiable).IsAssignableFrom(c.Type))
            {
                return ToFieldInitExpression(c.Type, (IdentifiableEntity)c.Value);// podria ser null y lo meteriamos igualmente
            }
            else if (typeof(Lazy).IsAssignableFrom(c.Type))
            {
                return ToLazyReferenceExpression(c.Type, (Lazy)c.Value);
            }
            return c;
        }

        static Expression ToLazyReferenceExpression(Type lazyType, Lazy lazy)
        {
            Expression id = Expression.Constant(lazy.IdOrNull ?? int.MinValue);

            return new LazyReferenceExpression(lazyType,
                new FieldInitExpression(lazy.RuntimeType, null, id, null),
                id, Expression.Constant(lazy.ToStr), TypeConstant(lazy.RuntimeType));
        }

        static Expression ToFieldInitExpression(Type entityType, IIdentifiable ei)
        {
            return new FieldInitExpression(ei.GetType(), null, Expression.Constant(ei.IdOrNull ?? int.MinValue), null);
        }

        static Expression NullInt()
        {
            return Expression.Constant(null, typeof(int?));
        }


        protected override Expression VisitParameter(ParameterExpression p)
        {
            return map.TryGetC(p) ?? p;
        }

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            Expression ex = base.VisitMemberAccess(m);
            Expression binded = BindMemberAccess((MemberExpression)ex);
            return binded;
        }

        public Expression BindMemberAccess(MemberExpression m)
        {
            Expression source = m.Expression; 
            if(source.NodeType ==  (ExpressionType)DbExpressionType.Projection)
            {
                ProjectionExpression proj = ((ProjectionExpression)source);
                if (proj.UniqueFunction.HasValue)
                {
                    source = proj.Projector;
                }
            }

            source = RemoveConvert(source);

            switch (source.NodeType)
            {
                case ExpressionType.MemberInit:
                    return ((MemberInitExpression)source).Bindings
                        .OfType<MemberAssignment>()
                        .Single(a => ReflectionTools.MemeberEquals(a.Member, m.Member)).Expression;
                case ExpressionType.New:
                    {
                        NewExpression nex = (NewExpression)source;
                        if (nex.Type.IsInstantiationOf(typeof(Grouping<,>)) && m.Member.Name == "Key")
                            return nex.Arguments[0];
                        MethodInfo mi = ((PropertyInfo)m.Member).GetGetMethod();
                        return nex.Members.Zip(nex.Arguments).Single(p => ReflectionTools.MethodEqual((MethodInfo)p.First, mi)).Second;
                    }
                case (ExpressionType)DbExpressionType.FieldInit:
                    {
                        FieldInitExpression fie = (FieldInitExpression)source;
                        FieldInfo fi = Reflector.FindFieldInfo(fie.Type, m.Member, false);

                        if (fi != null && fi.FieldEquals<IdentifiableEntity>(ei => ei.id))
                            return fie.ExternalId;

                        if (fie.TableAlias == null)
                        {
                            fie.TableAlias = GetNextTableAlias();
                            if (!fie.Table.IsView)
                                fie.GetOrCreateFieldBinding(FieldInitExpression.IdField, this);
                            requests.GetOrCreate(CurrentAlias).Add(new TableCondition
                            {
                                FieldInit = fie,
                                Table = new TableExpression(fie.Type, fie.TableAlias, fie.Table.Name)
                            });
                        }

                        Expression binding = fie.GetOrCreateFieldBinding(fi, this);

                        return binding;
                    }
                case (ExpressionType)DbExpressionType.EmbeddedFieldInit:
                    {
                        EmbeddedFieldInitExpression efie = (EmbeddedFieldInitExpression)source;
                        FieldInfo fi = Reflector.FindFieldInfo(efie.Type, m.Member, true);

                        Expression binding =  efie.GetBinding(fi);
                        return binding;
                    }
                case (ExpressionType)DbExpressionType.LazyReference:
                    {
                        LazyReferenceExpression lazyRef = (LazyReferenceExpression)source;
                        PropertyInfo pi = m.Member as PropertyInfo;
                        if (pi != null)
                        {
                            if (pi.Name == "Id")
                                return lazyRef.Id;
                            if (pi.Name == "EntityOrNull")
                                return lazyRef.Reference;
                            if (pi.Name == "ToStr")
                                return lazyRef.ToStr.ThrowIfNullC("ToStr is no accesible on queries in ImplementedByAll");
                        }

                        throw new ApplicationException("The member {0} of Lazy is no accesible on queries, use EntityOrNull instead".Formato(m.Member));
                    }
                case (ExpressionType)DbExpressionType.ImplementedBy:
                    {
                        ImplementedByExpression ib = (ImplementedByExpression)source;

                        PropertyInfo pi = (PropertyInfo)m.Member;

                        Expression result = ib.TryGetPropertyBinding(pi); 

                        if(result == null)
                        {
                            List<Expression> list = ib.Implementations
                                .Select(imp => BindMemberAccess(Expression.MakeMemberAccess(imp.Field, m.Member))).ToList();

                            result = Collapse(list, m.Member.ReturningType());

                            ib.AddPropertyBinding(pi, result); 
                        }

                        return result; 
                    }
            }
      
            return Expression.MakeMemberAccess(source, m.Member);
        }

        private Expression Collapse(List<Expression> list, Type returnType)
        {
            if (list.All(e => e is LazyReferenceExpression))
            {
                Expression entity = Collapse(list.Select(exp => ((LazyReferenceExpression)exp).Reference).ToList(), Reflector.ExtractLazy(returnType));

                return MakeLazy(returnType, entity); 
            }

            if(list.Any(e=>e is ImplementedByAllExpression))
            {
                Expression id = Coalesce(typeof(int?), list.Select(e => GetId(e)));
                Expression typeId = Coalesce(typeof(int?), list.Select(e => GetTypeId(e)));

                return new ImplementedByAllExpression(returnType, id, typeId); 
            }

            if(list.All(e=>e is FieldInitExpression || e is ImplementedByExpression))
            {
                var fies = (from e in list
                            where e is FieldInitExpression
                            select new {e.Type, Fie = (FieldInitExpression)e }).ToList();

                var ibs = (from e in list
                            where e is ImplementedByExpression
                            from imp in ((ImplementedByExpression)e).Implementations
                            select new {imp.Type, Fie = imp.Field }).ToList();

                var groups = fies.Concat(ibs).AgGroupToDictionary(a => a.Type, g => g.Select(a => a.Fie.ExternalId).ToList());

                var implementations = groups.Select(g => new ImplementationColumnExpression(g.Key,
                    new FieldInitExpression(g.Key, null, Coalesce(typeof(int?), g.Value), null))).ToReadOnly();

                if(implementations.Count == 1)
                    return implementations[0].Field;

                return new ImplementedByExpression(returnType, implementations);    
            }

            if(list.All(e=>e is EmbeddedFieldInitExpression))
            {
                var lc = new LambdaComparer<FieldInfo, string>(fi=>fi.Name);

                var groups = list
                    .SelectMany(e => ((EmbeddedFieldInitExpression)e).Bindings, (e, b) => new { e, b })
                    .GroupBy(p => p.b.FieldInfo, p => p.e, lc)
                    .ToDictionary(g => g.Key, g => g.ToList(), lc);

                return new EmbeddedFieldInitExpression(returnType,
                    groups.Select(k => new FieldBinding(k.Key, Coalesce(k.Key.FieldType.Nullify(), k.Value))));
            }

            if(list.Any(e=>e is MListExpression))
                throw new ApplicationException("MList on ImplementedBy are not supported jet");

            return Coalesce(returnType.Nullify(), list);
        }

        private Expression GetId(Expression expression)
        {
            expression = RemoveConvert(expression); 

            if (expression is FieldInitExpression)
                return ((FieldInitExpression)expression).ExternalId;

            if (expression is ImplementedByExpression)
                return Coalesce(typeof(int?), ((ImplementedByExpression)expression).Implementations.Select(imp => imp.Field.ExternalId).ToList());

            if (expression is ImplementedByAllExpression)
                return ((ImplementedByAllExpression)expression).Id;

            throw new NotSupportedException(); 
        }

        private Expression GetTypeId(Expression expression)
        {
            expression = RemoveConvert(expression); 

            if (expression is FieldInitExpression)
                return TypeConstant(expression.Type);

            if (expression is ImplementedByExpression)
            {
                ImplementedByExpression ib = (ImplementedByExpression)expression;

                if (ib.Implementations.Count == 0)
                    return NullInt();

                if (ib.Implementations.Count == 1)
                    return TypeConstant(ib.Implementations[0].Type);//Not regular, but usefull

                Expression aggregate = ib.Implementations.Aggregate(NullInt(),
                    (old, imp) => Expression.Condition(new IsNotNullExpression(imp.Field.ExternalId), TypeConstant(imp.Type), old));

                return DbExpressionNominator.FullNominate(aggregate, false);
            }

            if (expression is ImplementedByAllExpression)
                return ((ImplementedByAllExpression)expression).TypeId;

            throw new NotSupportedException();
        }

        internal Expression MakeLazy(Type type, Expression entity)
        {
            var toStr = (entity is ImplementedByAllExpression) ? null :
                BindMemberAccess(Expression.MakeMemberAccess(entity, ToStrProperty));

            Expression id = GetId(entity);
            Expression typeId = GetTypeId(entity);
            return new LazyReferenceExpression(type, entity, id, toStr, typeId);
        }

        protected override Expression VisitInvocation(InvocationExpression iv)
        {
            LambdaExpression lambda = iv.Expression as LambdaExpression;
            if (lambda != null)
            {
                for (int i = 0, n = lambda.Parameters.Count; i < n; i++)
                {
                    this.map[lambda.Parameters[i]] = iv.Arguments[i];
                }
                return this.Visit(lambda.Body);
            }
            return base.VisitInvocation(iv);
        }

        protected override Expression VisitTypeIs(TypeBinaryExpression b)
        {
            Expression operand = Visit(b.Expression);
            if (operand.NodeType == (ExpressionType)DbExpressionType.ImplementedBy)
            {
                ImplementedByExpression rib = (ImplementedByExpression)operand;
                FieldInitExpression fie = rib.Implementations.Where(ri => ri.Type == b.TypeOperand).Single(Resources.TheFieldHasNoImplementationForType0.Formato(b.Type.TypeName())).Field;
                return new IsNotNullExpression(fie.ExternalId);
            }
            else if (operand.NodeType == (ExpressionType)DbExpressionType.ImplementedByAll)
            {
                ImplementedByAllExpression riba = (ImplementedByAllExpression)operand;
                int idType = Schema.Current.IDsForType.GetOrThrow(b.TypeOperand, Resources.TheType0IsNotInTheTypesTable.Formato(b.Type.TypeName()));
                return SmartEqualizer.EqualNullable(riba.TypeId, Expression.Constant(idType));
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

                       Expression other = SmartEqualizer.EqualNullable(riba.TypeId, Expression.Constant(idType));

                       FieldInitExpression result = new FieldInitExpression(u.Type, null, riba.Id, other); //Delay riba.TypeID to FillFie to make the SQL more clean
                       riba.Implementations.Add(new ImplementationColumnExpression(u.Type, result));
                       return result;
                    }
                    else
                        return imp.Field;
                }
                else if (operand != u.Operand)
                    return Expression.MakeUnary(u.NodeType, operand, u.Type, u.Method);
                else
                    return u;
            }

            return base.VisitUnary(u); 
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
