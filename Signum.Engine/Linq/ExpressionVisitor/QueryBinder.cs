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
using Signum.Entities.DynamicQuery;

namespace Signum.Engine.Linq
{
    /// <summary>
    /// QueryBinder is a visitor that converts method calls to LINQ operations into 
    /// custom DbExpression nodes and references to class members into references to columns
    /// </summary>
    internal class QueryBinder : ExpressionVisitor
    {
        Dictionary<ParameterExpression, Expression> map = new Dictionary<ParameterExpression, Expression>();
        Dictionary<ProjectionToken, GroupByInfo> groupByMap = new Dictionary<ProjectionToken, GroupByInfo>();
        Dictionary<ProjectionToken, HashSet<TableCondition>> requests = new Dictionary<ProjectionToken, HashSet<TableCondition>>();
        Expression root;

        AliasGenerator aliasGenerator = new AliasGenerator();

        internal static readonly PropertyInfo ToStrProperty = ReflectionTools.GetPropertyInfo((IIdentifiable ii) => ii.ToStr);
        internal static readonly PropertyInfo IdProperty = ReflectionTools.GetPropertyInfo((IIdentifiable ii) => ii.Id); 

        internal class TableCondition
        {
            public TableExpression Table;
            public FieldInitExpression FieldInit;
        }

        internal QueryBinder() { }

        static internal Expression Bind(Expression expression)
        {
            QueryBinder qb = new QueryBinder { root = expression };
            return qb.Visit(expression);
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
                        if (m.Arguments.Count == 2)
                            return this.BindSelectMany(m.Type, m.GetArgument("source"), m.GetArgument("selector").StripQuotes(), null);
                        else
                            return this.BindSelectMany(m.Type, m.GetArgument("source"), m.GetArgument("collectionSelector").StripQuotes(), m.TryGetArgument("resultSelector").StripQuotes());
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
                    case "Any":
                        return this.BindAnyAll(m.Type, m.GetArgument("source"), m.TryGetArgument("predicate").StripQuotes(), m.Method, m == root); 
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
            else if (m.Method.DeclaringType == typeof(LiteUtils) && m.Method.Name == "ToLite")
            {
                Expression toStr = Visit(m.TryGetArgument("toStr")); //could be null

                if (m.Method.GetParameters().First().ParameterType == typeof(Lite))
                {
                    LiteReferenceExpression liteRef = (LiteReferenceExpression)Visit(m.GetArgument("lite"));

                    return new LiteReferenceExpression(m.Type, liteRef.Reference, liteRef.Id, toStr ?? liteRef.ToStr, liteRef.TypeId);
                }
                else
                {
                    var entity = Visit(m.GetArgument("entity"));
                    return MakeLite(m.Type, entity, toStr);
                }
            }
            else if (m.Method.DeclaringType == typeof(object) && m.Method.Name == "ToString" && typeof(IdentifiableEntity).IsAssignableFrom(m.Object.Type))
            {
                return Visit(Expression.MakeMemberAccess(m.Object, ReflectionTools.GetFieldInfo((IdentifiableEntity ei) =>ei.toStr)));
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

        internal Expression Coalesce(Type type, IEnumerable<Expression> exp)
        {
            var list = exp.ToList();

            if (list.Count == 0)
                return Expression.Constant(null, type);

            if (list.Count == 1)
                return list[0]; //Not regular, but usefull

            return new SqlFunctionExpression(type, SqlFunction.COALESCE.ToString(), list);  
        }

        private Expression MapAndVisitExpand(LambdaExpression lambda, ref ProjectionExpression p)
        {
            map.Add(lambda.Parameters[0], p.Projector);

            Expression result = Visit(lambda.Body);

            p = ApplyExpansions(p);

            map.Remove(lambda.Parameters[0]);

            return result;
        }

        private ProjectionExpression ApplyExpansions(ProjectionExpression projection)
        {
            if (!requests.ContainsKey(projection.Token))
                return projection; 

            HashSet<TableCondition> allProjections = requests.Extract(projection.Token);

            string newAlias = aliasGenerator.GetNextSelectAlias();
            string[] oldAliases = allProjections.Select(p => p.Table.Alias).And(projection.Source.Alias).ToArray();

            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, newAlias, oldAliases, new ProjectionToken[0]); //Do not replace tokens

            JoinExpression source = (JoinExpression)allProjections.Aggregate((SourceExpression)projection.Source, (e, p) =>
            {
                var externalID = DbExpressionNominator.FullNominate(p.FieldInit.ExternalId, false);

                Expression equal = SmartEqualizer.EqualNullable(externalID, p.FieldInit.GetFieldBinding(FieldInitExpression.IdField));
                Expression condition = p.FieldInit.OtherCondition == null ? equal : Expression.And(p.FieldInit.OtherCondition, equal);
                return new JoinExpression(JoinType.SingleRowLeftOuterJoin, e, p.Table, condition);
            });

            return new ProjectionExpression(
                    new SelectExpression(newAlias, false, null, pc.Columns, source, null, null, null),
                    pc.Projector, null, projection.Token);
        }

        private ProjectionExpression VisitCastProjection(Expression source)
        {
            var visit = Visit(source);
            return AsProjection(visit);
        }

        private ProjectionExpression AsProjection(Expression expression)
        {
            expression = RemoveGroupByConvert(expression);

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

        private static Expression RemoveGroupByConvert(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert && expression.Type.IsInstantiationOf(typeof(IGrouping<,>)))
                expression = ((UnaryExpression)expression).Operand;
            return expression;
        }

        private ProjectionExpression MListProjection(MListExpression mle)
        {
            RelationalTable tr = mle.RelationalTable;

            string tableAlias = aliasGenerator.GetNextSelectAlias();
            TableExpression tableExpression = new TableExpression(tableAlias, tr.Name);

            ProjectionToken token = new ProjectionToken();

            Expression expr = tr.FieldExpression(token, tableAlias, this);

            string selectAlias = aliasGenerator.GetNextSelectAlias();

            ColumnExpression ce = tr.BackColumnExpression(tableAlias);

            ProjectedColumns pc = ColumnProjector.ProjectColumns(expr, selectAlias, tableExpression.KnownAliases, new ProjectionToken[0]); // no Token

            var proj = new ProjectionExpression(
                new SelectExpression(selectAlias, false, null, pc.Columns, tableExpression, SmartEqualizer.EqualNullable(mle.BackID, ce), null, null),
                 pc.Projector, null, token);

            proj = ApplyExpansions(proj);

            return proj;
        }

        private Expression BindTake(Type resultType, Expression source, Expression count)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            string alias = aliasGenerator.GetNextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection, alias);
            return new ProjectionExpression(
                new SelectExpression(alias, false, count, pc.Columns, projection.Source, null, null, null),
                pc.Projector, null, pc.Token);
        }

        private Expression BindSkip(Type resultType, Expression source, Expression count)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            string alias = aliasGenerator.GetNextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection, alias);

            RowNumberExpression rne = new RowNumberExpression(
                pc.Columns.Select((c, i) =>
                    new OrderExpression(OrderType.Ascending, c.Expression)));

            ColumnDeclaration cd = new ColumnDeclaration("RowNumber", rne);

            SelectExpression se = new SelectExpression(alias, false, null, pc.Columns.PreAnd(cd), projection.Source, null, null, null);

            string alias2 = aliasGenerator.GetNextSelectAlias();
            ProjectedColumns pc2 = ColumnProjector.ProjectColumns(pc.Projector, alias2, new[] { alias }, new[] { pc.Token });

            Expression where = Expression.GreaterThan(new ColumnExpression(cd.Expression.Type, alias, cd.Name), count);

            return new ProjectionExpression(
                new SelectExpression(alias2, false, null, pc2.Columns, se, where, null, null),
                pc2.Projector, null, pc2.Token);
        }

        private Expression BindUniqueRow(Type resultType, UniqueFunction function, Expression source, LambdaExpression predicate)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            Expression where = null;
            if (predicate != null)
                where = DbExpressionNominator.FullNominate(MapAndVisitExpand(predicate, ref projection), true);

            string alias = aliasGenerator.GetNextSelectAlias();
            Expression top = function == UniqueFunction.First || function ==  UniqueFunction.FirstOrDefault? Expression.Constant(1):null;

            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection , alias);
            return new ProjectionExpression(
                new SelectExpression(alias, false, top, pc.Columns, projection.Source, where, null, null),
                pc.Projector, function, pc.Token);
        }

        private Expression BindDistinct(Type resultType, Expression source)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            string alias = aliasGenerator.GetNextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection, alias);
            return new ProjectionExpression(
                new SelectExpression(alias, true, null, pc.Columns, projection.Source, null, null, null),
                pc.Projector, null, pc.Token);
        }


        private Expression BindAggregate(Type resultType, AggregateFunction aggregateFunction, Expression source, LambdaExpression selector, bool isRoot)
        {
            bool coalesceTrick = !resultType.IsNullable() && aggregateFunction == AggregateFunction.Sum;

            ProjectionExpression projection = this.VisitCastProjection(source);
            Expression exp =
                aggregateFunction == AggregateFunction.Count ? null :
                selector != null ? MapAndVisitExpand(selector, ref projection):
                DbExpressionNominator.FullNominate(projection.Projector, false);

            if (coalesceTrick)
                exp = Expression.Convert(exp, resultType.Nullify());

            if(exp != null)
                exp = DbExpressionNominator.FullNominate(exp, false);

            string alias = aliasGenerator.GetNextSelectAlias();
            var aggregate = !coalesceTrick ? new AggregateExpression(resultType, exp, aggregateFunction) :
                (Expression)Expression.Coalesce(
                    new AggregateExpression(resultType.Nullify(), exp, aggregateFunction),
                    new SqlConstantExpression(Activator.CreateInstance(resultType), resultType));

            ColumnDeclaration cd = new ColumnDeclaration("a", aggregate);

            SelectExpression select = new SelectExpression(alias, false, null, new[] { cd }, projection.Source, null, null, null);

            if (isRoot)
                return new ProjectionExpression(select, ColumnProjector.SingleProjection(cd, alias, resultType), UniqueFunction.Single, null);

            ScalarExpression subquery = new ScalarExpression(resultType, select);

            GroupByInfo info = groupByMap.TryGetC(projection.Token);
            if (info != null)
            {
                Expression exp2 =
                     aggregateFunction == AggregateFunction.Count ? null :
                     selector != null ? DbExpressionNominator.FullNominate(MapAndVisitExpand(selector, ref info.Projection), false) :
                     info.Projection.Projector;

                return new AggregateSubqueryExpression(info.Alias, new AggregateExpression(resultType, exp2, aggregateFunction), subquery);
            }

            return subquery;
        }
           

        private Expression BindAnyAll(Type resultType, Expression source, LambdaExpression predicate, MethodInfo method, bool isRoot)
        {
            bool isAll = method.Name == "All";

            if (source is ParameterExpression)
                source = VisitParameter((ParameterExpression)source); 

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

            if (source is ParameterExpression)
                source = VisitParameter((ParameterExpression)source); 

            if (source.NodeType == ExpressionType.Constant && !typeof(IQueryable).IsAssignableFrom(source.Type)) //!isRoot
            {
                ConstantExpression ce = (ConstantExpression)source;
                IEnumerable col = (IEnumerable)ce.Value;

                Type colType = source.Type.ElementType();
                if (typeof(IIdentifiable).IsAssignableFrom(colType))
                    return SmartEqualizer.EntityIn(newItem, col.Cast<IIdentifiable>().Select(ie => ToFieldInitExpression(ie)).ToArray());
                else if (typeof(Lite).IsAssignableFrom(colType))
                    return SmartEqualizer.EntityIn(newItem, col.Cast<Lite>().Select(lite => ToLiteReferenceExpression(colType, lite)).ToArray());
                else
                    return InExpression.FromValues(newItem, col == null ? new object[0] : col.Cast<object>().ToArray());
            }
            else
            {
                ProjectionExpression projection = this.VisitCastProjection(source);

                string alias = aliasGenerator.GetNextSelectAlias();
                var pc = ColumnProjector.ProjectColumns(projection, alias);

                SubqueryExpression se = null;
                if (pc.Columns.Count == 1 && !typeof(IIdentifiable).IsAssignableFrom(newItem.Type))
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

            var alias = aliasGenerator.GetNextSelectAlias();
            Expression exprAsValue = ConditionsRewriter.MakeSqlValue(expr);
            SelectExpression select = new SelectExpression(alias, false, null, new[] { new ColumnDeclaration("value", exprAsValue) }, null, null, null, null);
            return new ProjectionExpression(select, new ColumnExpression(expr.Type, alias, "value"), uniqueFunction, null);
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
            Expression where = DbExpressionNominator.FullNominate(MapAndVisitExpand(predicate, ref projection), true);

            string alias = aliasGenerator.GetNextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection, alias);
            return new ProjectionExpression(
                new SelectExpression(alias, false, null, pc.Columns, projection.Source, where, null, null),
                pc.Projector, null, pc.Token);
        }

        private Expression BindSelect(Type resultType, Expression source, LambdaExpression selector)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            Expression expression = MapAndVisitExpand(selector, ref projection);

            string alias = aliasGenerator.GetNextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(expression, alias, projection.Source.KnownAliases, new[] { projection.Token });
            return new ProjectionExpression(
                new SelectExpression(alias, false, null, pc.Columns, projection.Source, null, null, null),
                pc.Projector, null, pc.Token);
        }

        protected virtual Expression BindSelectMany(Type resultType, Expression source, LambdaExpression collectionSelector, LambdaExpression resultSelector)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            map.Add(collectionSelector.Parameters[0], projection.Projector);

            Expression collectionBody = collectionSelector.Body;
            bool outer = OverloadingSimplifier.ExtractDefaultIfEmpty(ref collectionBody);
            Expression collectionExpression = Visit(collectionBody);
            projection = ApplyExpansions(projection);

            map.Remove(collectionSelector.Parameters[0]);

            ProjectionExpression collectionProjection = AsProjection(collectionExpression);

            ProjectedColumns pc;
            string alias = aliasGenerator.GetNextSelectAlias();
            if (resultSelector == null)
            {
                pc = ColumnProjector.ProjectColumns(collectionProjection.Projector, alias,
                    projection.Source.KnownAliases.Concat(collectionProjection.Source.KnownAliases).ToArray(), new[] { projection.Token, collectionProjection.Token });
            }
            else
            {
                map.SetRange(resultSelector.Parameters, new[] { projection.Projector, collectionProjection.Projector });
                Expression resultProjector = Visit(resultSelector.Body);
                map.RemoveRange(resultSelector.Parameters);

                projection = ApplyExpansions(projection);
                collectionProjection = ApplyExpansions(collectionProjection);

                pc = ColumnProjector.ProjectColumns(resultProjector, alias,
                    projection.Source.KnownAliases.Concat(collectionProjection.Source.KnownAliases).ToArray(), new[] { projection.Token, collectionProjection.Token });
            }

            JoinType joinType = IsTable(collectionSelector.Body) ? JoinType.CrossJoin :
                                outer ? JoinType.OuterApply :
                                JoinType.CrossApply;

            JoinExpression join = new JoinExpression(joinType, projection.Source, collectionProjection.Source, null);

            return new ProjectionExpression(
                new SelectExpression(alias, false, null, pc.Columns, join, null, null, null),
                pc.Projector, null, pc.Token);
        }

        protected virtual Expression BindJoin(Type resultType, Expression outerSource, Expression innerSource, LambdaExpression outerKey, LambdaExpression innerKey, LambdaExpression resultSelector)
        {
            bool rightOuter = OverloadingSimplifier.ExtractDefaultIfEmpty(ref outerSource);
            bool leftOuter = OverloadingSimplifier.ExtractDefaultIfEmpty(ref innerSource);

            ProjectionExpression outerProj = VisitCastProjection(outerSource);
            ProjectionExpression innerProj = VisitCastProjection(innerSource);


            map.Add(outerKey.Parameters[0], outerProj.Projector);
            Expression outerKeyExpr = Visit(outerKey.Body);
            map.Remove(outerKey.Parameters[0]);

            map.Add(innerKey.Parameters[0], innerProj.Projector);
            Expression innerKeyExpr = Visit(innerKey.Body);
            map.Remove(innerKey.Parameters[0]);

            Expression condition = DbExpressionNominator.FullNominate(SmartEqualizer.EqualNullable(outerKeyExpr, innerKeyExpr), true);

            JoinType jt = rightOuter && leftOuter ? JoinType.FullOuterJoin :
                          rightOuter ? JoinType.RightOuterJoin :
                          leftOuter ? JoinType.LeftOuterJoin :
                          JoinType.InnerJoin;

            string alias = aliasGenerator.GetNextSelectAlias();
            
            map.SetRange(resultSelector.Parameters,new []{outerProj.Projector, innerProj.Projector});
            Expression resultExpr = Visit(resultSelector.Body);
            map.RemoveRange(resultSelector.Parameters);

            outerProj = ApplyExpansions(outerProj);
            innerProj = ApplyExpansions(innerProj);

            JoinExpression join = new JoinExpression(jt, outerProj.Source, innerProj.Source, condition);

            ProjectedColumns pc = ColumnProjector.ProjectColumns(resultExpr, alias,
                outerProj.Source.KnownAliases.Concat(innerProj.Source.KnownAliases).ToArray(),
                new[] { outerProj.Token, innerProj.Token });
            ProjectionExpression result = new ProjectionExpression(
                new SelectExpression(alias, false, null, pc.Columns, join, null, null, null),
                pc.Projector, null, pc.Token);
            return result; 
        }

        private Expression BindGroupBy(Type resultType, Expression source, LambdaExpression keySelector, LambdaExpression elementSelector)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            Expression keyExpr = this.MapAndVisitExpand(keySelector, ref projection);
            // Use ProjectColumns to get group-by expressions from key expression
            ProjectedColumns keyProjection = ColumnProjector.ProjectColumns(keyExpr, projection.Source.Alias, projection.Source.KnownAliases, new[] { projection.Token });
            IEnumerable<Expression> groupExprs = keyProjection.Columns.Select(c => c.Expression);

            // make duplicate of source query as basis of element subquery by visiting the source again
            ProjectionExpression subqueryBasis = this.VisitCastProjection(source);
            // recompute key columns for group expressions relative to subquery (need these for doing the correlation predicate)
            Expression subqueryKey = MapAndVisitExpand(keySelector, ref subqueryBasis);
            // use same projection trick to get group-by expressions based on subquery
            ProjectedColumns subqueryKeyPC = ColumnProjector.ProjectColumns(subqueryKey, subqueryBasis.Source.Alias, subqueryBasis.Source.KnownAliases, new[] { subqueryBasis.Token });

            IEnumerable<Expression> subqueryGroupExprs = subqueryKeyPC.Columns.Select(c => c.Expression);
            Expression subqueryCorrelation =
                subqueryGroupExprs.Zip(groupExprs, (e1, e2) => SmartEqualizer.EqualNullable(e1, e2))
                .Aggregate((a, b) => Expression.And(a, b));


            Expression elemExpr = null;
            if (elementSelector != null)
                elemExpr = this.MapAndVisitExpand(elementSelector, ref projection);
            else
                elemExpr = projection.Projector;

            // compute element based on duplicated subquery
            Expression subqueryElemExpr = null;
            if (elementSelector != null)
                subqueryElemExpr = this.MapAndVisitExpand(elementSelector, ref subqueryBasis);
            else
                subqueryElemExpr = subqueryBasis.Projector;

            // build subquery that projects the desired element
            var elementAlias = aliasGenerator.GetNextSelectAlias();
            ProjectedColumns elementPC = ColumnProjector.ProjectColumns(subqueryElemExpr, elementAlias, subqueryBasis.Source.KnownAliases, new[] { subqueryBasis.Token });
            ProjectionExpression elementSubquery =
                new ProjectionExpression(
                    new SelectExpression(elementAlias, false, null, elementPC.Columns, subqueryBasis.Source, subqueryCorrelation, null, null),
                    elementPC.Projector, null, elementPC.Token);

            var alias = aliasGenerator.GetNextSelectAlias();

            // make it possible to tie aggregates back to this group-by

            //this.groupByMap.Add(elementSubquery, info);

            Expression resultExpr = Expression.Convert(
                Expression.New(typeof(Grouping<,>).MakeGenericType(keyExpr.Type, subqueryElemExpr.Type).GetConstructors()[1],
                        new Expression[] { keyExpr, elementSubquery }), typeof(IGrouping<,>).MakeGenericType(keyExpr.Type, subqueryElemExpr.Type));

            ProjectedColumns pc = ColumnProjector.ProjectColumns(resultExpr, alias, projection.Source.KnownAliases, new[] { projection.Token });

            // make it possible to tie aggregates back to this group-by
            NewExpression newResult = (NewExpression)RemoveGroupByConvert(pc.Projector);
            GroupByInfo info = new GroupByInfo { Alias = alias, Projection = new ProjectionExpression(projection.Source, elemExpr, null, projection.Token) };
            
            this.groupByMap.Add(((ProjectionExpression)newResult.Arguments[1]).Token, info);

            return new ProjectionExpression(
                new SelectExpression(alias, false, null, pc.Columns, projection.Source, null, null, groupExprs),
                pc.Projector, null, pc.Token);
        }

   
        List<OrderExpression> thenBys;
        protected virtual Expression BindOrderBy(Type resultType, Expression source, LambdaExpression orderSelector, OrderType orderType)
        {
            List<OrderExpression> myThenBys = this.thenBys;
            this.thenBys = null;
            ProjectionExpression projection = this.VisitCastProjection(source);

            List<OrderExpression> orderings = new List<OrderExpression>();
            orderings.Add(new OrderExpression(orderType, GetOrderExpression(orderSelector, ref projection)));

            if (myThenBys != null)
            {
                for (int i = myThenBys.Count - 1; i >= 0; i--)
                {
                    OrderExpression tb = myThenBys[i];
                    orderings.Add(new OrderExpression(tb.OrderType, GetOrderExpression((LambdaExpression)tb.Expression, ref projection)));
                }
            }

            string alias = aliasGenerator.GetNextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection, alias);
            return new ProjectionExpression(
                new SelectExpression(alias, false, null, pc.Columns, projection.Source, null, orderings.AsReadOnly(), null),
                pc.Projector, null, pc.Token);
        }

        private Expression GetOrderExpression(LambdaExpression lambda, ref ProjectionExpression projection)
        {
            map.Add(lambda.Parameters[0], projection.Projector);

            Expression expr = Visit(lambda.Body);

            if (expr is LiteReferenceExpression)
            {
                expr = ((LiteReferenceExpression)expr).ToStr ?? ((LiteReferenceExpression)expr).Id;
            }
            else if (expr is FieldInitExpression || expr is ImplementedByExpression)
            {
                expr = BindMemberAccess(Expression.MakeMemberAccess(expr, ToStrProperty));
            }
            else if (expr is ImplementedByAllExpression)
            {
                expr = ((ImplementedByAllExpression)expr).Id;
            }

            projection = ApplyExpansions(projection);

            map.Remove(lambda.Parameters[0]);

            return DbExpressionNominator.FullNominate(expr, false);
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

        public Type TableType(object value)
        {
            if (value == null)
                return null;

            Type type = value.GetType();
            if (!typeof(IQueryable).IsAssignableFrom(type))
                return null;

            IQueryable query = (IQueryable)value;

            if (!type.IsInstantiationOf(typeof(Query<>)))
                throw new InvalidOperationException(Resources.BelongsToAnotherKindOkLinqProvider.Formato(type.TypeName()));

            if (!query.IsBase())
                throw new InvalidOperationException(Resources.ConstantExpressionWithAComplexIQueryableUnexpectedAt);

            return query.ElementType;
        }

        private ProjectionExpression GetTableProjection(Type type)
        {
            string tableAlias = aliasGenerator.GetNextTableAlias(type);
            Type resultType = typeof(IEnumerable<>).MakeGenericType(type);
            ProjectionToken token = new ProjectionToken();

            Expression exp; 
            Table table;
            TableExpression tableExpression; 
            if (typeof(IdentifiableEntity).IsAssignableFrom(type))
            {
                Schema.Current.AssertAllowed(type);

                table = ConnectionScope.Current.Schema.Table(type);

                tableExpression = new TableExpression(tableAlias, table.Name);

                Expression id = table.CreateBinding(token, tableAlias, FieldInitExpression.IdField, this);
                Expression typeId = TypeSqlConstant(type);
                exp = new FieldInitExpression(type, tableAlias, id, typeId, null, token)
                {
                    Bindings = { new FieldBinding(FieldInitExpression.IdField, id) }
                };
            }
            else
            {
                ViewBuilder vb = new ViewBuilder(Schema.Current);
                table = vb.NewView(type);

                tableExpression = new TableExpression(tableAlias, table.Name);

                exp = table.GetViewExpression(token, tableAlias, this); 
            }
            string selectAlias = aliasGenerator.GetNextSelectAlias();

            ProjectedColumns pc = ColumnProjector.ProjectColumns(exp, selectAlias, new[] { tableAlias }, new[] { token });
            var projection = new ProjectionExpression(
                new SelectExpression(selectAlias, false, null, pc.Columns, tableExpression, null, null, null),
            pc.Projector, null, pc.Token);

            return projection;
        }

        internal Expression VisitConstant(object value, Type type)
        {
            return VisitConstant(Expression.Constant(value, type));
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            Type tableType = TableType(c.Value);
            if(tableType != null)
                return GetTableProjection(tableType);

            if (c.Value == null)
                return c; 

            if (typeof(IIdentifiable).IsAssignableFrom(c.Type))
            {
                return ToFieldInitExpression((IdentifiableEntity)c.Value);// podria ser null y lo meteriamos igualmente
            }
            else if (typeof(Lite).IsAssignableFrom(c.Type))
            {
                return ToLiteReferenceExpression(c.Type, (Lite)c.Value);
            }
            return c;
        }

        static Expression ToLiteReferenceExpression(Type liteType, Lite lite)
        {
            Expression id = Expression.Constant(lite.IdOrNull ?? int.MinValue);
            Expression typeId = TypeConstant(lite.RuntimeType);

            return new LiteReferenceExpression(liteType,
                new FieldInitExpression(lite.RuntimeType, null, id, typeId, null, ProjectionToken.External),
                id, Expression.Constant(lite.ToStr), typeId);
        }

        static Expression ToFieldInitExpression(IIdentifiable ei)
        {
            return new FieldInitExpression(
                ei.GetType(),
                null,
                Expression.Constant(ei.IdOrNull ?? int.MinValue),
                TypeConstant(ei.GetType()),
                null,
                ProjectionToken.External);
        }

        static bool IsNewId(Expression expression)
        {
            ConstantExpression ce = expression as ConstantExpression;
            return ce != null && ce.Type.UnNullify() == typeof(int) && int.MinValue.Equals(ce.Value);
        }

        static SqlConstantExpression NullId = new SqlConstantExpression(null, typeof(int?));

        protected override Expression VisitParameter(ParameterExpression p)
        {
            return map.TryGetC(p) ?? p; //i.e. trycc
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
        {
            Expression e = this.Visit(assignment.Expression);
            if (e != assignment.Expression)
            {
                if (e.Type != assignment.Member.ReturningType())
                    e = Expression.Convert(e, assignment.Member.ReturningType());

                return Expression.Bind(assignment.Member, e);
            }
            return assignment;
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

            LambdaExpression lambda = ExpressionExpander.GetExpansion(source.Type, m.Member);
            if (lambda != null) //new expansions discovered
            {
                ParameterExpression temp = Expression.Parameter(source.Type, "temp");

                Expression cleanedLambda = DbQueryProvider.Clean( Expression.Invoke(lambda, temp));

                map.Add(temp, source);
                Expression result = Visit(cleanedLambda);
                map.Remove(temp);

                return result;
            }
            
            if(source.NodeType ==  (ExpressionType)DbExpressionType.Projection)
            {
                ProjectionExpression proj = ((ProjectionExpression)source);
                if (proj.UniqueFunction.HasValue)
                {
                    source = proj.Projector;
                }
            }
            
            
            source = RemoveGroupByConvert(source);

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

                    if (nex.Type.IsInstantiationOf(typeof(Expandable<>)) && m.Member.Name == "Value")
                        return nex.Arguments[0];

                    MethodInfo mi = ((PropertyInfo)m.Member).GetGetMethod();
                    return nex.Members.Zip(nex.Arguments).Single(p => ReflectionTools.MethodEqual((MethodInfo)p.First, mi)).Second;
                }
                case (ExpressionType)DbExpressionType.FieldInit:
                {
                    FieldInitExpression fie = (FieldInitExpression)source;
                    FieldInfo fi = Reflector.FindFieldInfo(fie.Type, m.Member, false);

                    if (fi != null && fi.FieldEquals((IdentifiableEntity ie) => ie.id))
                        return fie.ExternalId.UnNullify();

                    if (fie.TableAlias == null)
                    {
                        fie.TableAlias = aliasGenerator.GetNextTableAlias(fie.Type);
                        if (!fie.Table.IsView)
                            fie.GetOrCreateFieldBinding(fie.Token, FieldInitExpression.IdField, this);
                        requests.GetOrCreate(fie.Token).Add(new TableCondition
                        {
                            FieldInit = fie,
                            Table = new TableExpression(fie.TableAlias, fie.Table.Name)
                        });
                    }

                    if (fi == null)
                        throw new InvalidOperationException(Resources.TheMember0Of1IsNoAccesibleOnQueries.Formato(m.Member.Name, fie.Type.TypeName()));

                    Expression result = fie.GetOrCreateFieldBinding(fie.Token, fi, this);
                    return result;
                }
                case (ExpressionType)DbExpressionType.EmbeddedFieldInit:
                {
                    EmbeddedFieldInitExpression efie = (EmbeddedFieldInitExpression)source;
                    FieldInfo fi = Reflector.FindFieldInfo(efie.Type, m.Member, true);

                    if (fi == null)
                        throw new InvalidOperationException(Resources.TheMember0Of1IsNoAccesibleOnQueries.Formato(m.Member.Name, efie.Type.TypeName()));

                    Expression result = efie.GetBinding(fi);
                    return result;
                }
                case (ExpressionType)DbExpressionType.LiteReference:
                {
                    LiteReferenceExpression liteRef = (LiteReferenceExpression)source;
                    PropertyInfo pi = m.Member as PropertyInfo;
                    if (pi != null)
                    {
                        if (pi.Name == "Id")
                            return liteRef.Id;
                        if (pi.Name == "EntityOrNull" || pi.Name == "Entity")
                            return liteRef.Reference;
                        if (pi.Name == "ToStr")
                        {
                            if (liteRef.ToStr == null)
                                throw new InvalidOperationException(Resources.ToStrIsNoAccesibleOnQueriesInImplementedByAll);
                            return liteRef.ToStr;
                        }
                    }

                    throw new InvalidOperationException(Resources.TheMember0OfLiteIsNoAccesibleOnQueriesUseEntityInstead.Formato(m.Member));
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
                case (ExpressionType)DbExpressionType.ImplementedByAll:
                {
                    ImplementedByAllExpression iba = (ImplementedByAllExpression)source;
                    FieldInfo fi = Reflector.FindFieldInfo(iba.Type, m.Member, false);
                    if (fi != null && fi.FieldEquals((IdentifiableEntity ie) => ie.id))
                        return iba.Id;

                    throw new InvalidOperationException(Resources.TheMember0OfImplementedByAllIsNoAccesibleOnQueries.Formato(m.Member));
                }
            }
      
            return Expression.MakeMemberAccess(source, m.Member);
        }

        private Expression Collapse(List<Expression> list, Type returnType)
        {
            if(list.Count == 0)
                return Expression.Constant(null, returnType.Nullify());

            if (list.All(e => e is LiteReferenceExpression))
            {
                Expression entity = Collapse(list.Select(exp => ((LiteReferenceExpression)exp).Reference).ToList(), Reflector.ExtractLite(returnType));

                return MakeLite(returnType, entity, null); 
            }

            if(list.Any(e=>e is ImplementedByAllExpression))
            {
                Expression id = Coalesce(typeof(int?), list.Select(e => GetId(e)));
                Expression typeId = Coalesce(typeof(int?), list.Select(e => GetTypeId(e)));

                return new ImplementedByAllExpression(returnType, id, typeId, CoalesceToken(list.Select(a => GetToken(a)))); 
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

                var groups = fies.Concat(ibs).AgGroupToDictionary(a => a.Type, g => g.Select(a => a.Fie).ToList());

                var implementations = groups.Select(g =>
                    new ImplementationColumnExpression(g.Key,
                        new FieldInitExpression(g.Key, null,
                            Coalesce(typeof(int?),
                            g.Value.Select(fie => fie.ExternalId)),
                            TypeSqlConstant(g.Key),
                            null, CoalesceToken(g.Value.Select(f => f.Token))))).ToReadOnly();

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

                FieldEmbedded fe = ((EmbeddedFieldInitExpression)list.First()).FieldEmbedded;

                return new EmbeddedFieldInitExpression(returnType,
                    Coalesce(typeof(bool), list.Select(e => ((EmbeddedFieldInitExpression)e).HasValue ?? new SqlConstantExpression(true))),
                    groups.Select(k => new FieldBinding(k.Key, Coalesce(k.Key.FieldType.Nullify(), k.Value))), fe);
            }

            if(list.Any(e=>e is MListExpression))
                throw new InvalidOperationException(Resources.MListOnImplementedByAreNotSupportedYet);

            return Coalesce(returnType.Nullify(), list);
        }

        Expression GetId(Expression expression)
        {
            if (expression is FieldInitExpression)
                return ((FieldInitExpression)expression).ExternalId;

            if (expression is ImplementedByExpression)
                return Coalesce(typeof(int?), ((ImplementedByExpression)expression).Implementations.Select(imp => imp.Field.ExternalId).ToList());

            if (expression is ImplementedByAllExpression)
                return ((ImplementedByAllExpression)expression).Id;

            throw new NotSupportedException(); 
        }

        Expression GetTypeId(Expression expression)
        {
            if (expression is FieldInitExpression)
                return ((FieldInitExpression)expression).TypeId;

            if (expression is ImplementedByExpression)
            {
                ImplementedByExpression ib = (ImplementedByExpression)expression;

                if (ib.Implementations.Count == 0)
                    return NullId;

                if (ib.Implementations.Count == 1)
                    return ib.Implementations[0].Field.TypeId;//Not regular, but usefull

                Expression aggregate = ib.Implementations.Aggregate((Expression)NullId,
                    (old, imp) => Expression.Condition(new IsNotNullExpression(imp.Field.ExternalId), imp.Field.TypeId, old));

                return DbExpressionNominator.FullNominate(aggregate, false);
            }

            if (expression is ImplementedByAllExpression)
                return ((ImplementedByAllExpression)expression).TypeId;

            throw new NotSupportedException();
        }

        ProjectionToken CoalesceToken(IEnumerable<ProjectionToken> tokens)
        {
            return tokens.NotNull().Distinct().SingleOrDefault("Different ProjectionTokens");
        }

        ProjectionToken GetToken(Expression expression)
        {
            if (expression is FieldInitExpression)
                return ((FieldInitExpression)expression).Token;

            if (expression is ImplementedByExpression)
                return CoalesceToken(((ImplementedByExpression)expression).Implementations.Select(im => im.Field.Token));

            if (expression is ImplementedByAllExpression)
                return ((ImplementedByAllExpression)expression).Token;

            throw new NotSupportedException();
        }

        private static Expression RemoveEntityConvert(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert && typeof(IIdentifiable).IsAssignableFrom(expression.Type))
                expression = ((UnaryExpression)expression).Operand;
            return expression;
        }

        internal Expression MakeLite(Type type, Expression entity, Expression toStr)
        {
            entity = RemoveEntityConvert(entity);

            if (toStr == null && !(entity is ImplementedByAllExpression))
                toStr = BindMemberAccess(Expression.MakeMemberAccess(entity, ToStrProperty));

            Expression id = GetId(entity);
            Expression typeId = GetTypeId(entity);
            return new LiteReferenceExpression(type, entity, id, toStr, typeId);
        }

        protected override Expression VisitInvocation(InvocationExpression iv)
        {
            LambdaExpression lambda = iv.Expression as LambdaExpression;
            if (lambda != null)
            {
                for (int i = 0, n = lambda.Parameters.Count; i < n; i++)
                    this.map[lambda.Parameters[i]] = iv.Arguments[i];

                Expression result = this.Visit(lambda.Body);

                for (int i = 0, n = lambda.Parameters.Count; i < n; i++)
                    this.map.Remove(lambda.Parameters[i]);

                return result; 
            }
            return base.VisitInvocation(iv);
        }

        protected override Expression VisitTypeIs(TypeBinaryExpression b)
        {
            Expression operand = Visit(b.Expression);
            if (operand.NodeType == (ExpressionType)DbExpressionType.ImplementedBy)
            {
                ImplementedByExpression rib = (ImplementedByExpression)operand;
                FieldInitExpression fie = rib.Implementations.Where(ri => ri.Type == b.TypeOperand).Single(Resources.TheFieldHasNoImplementationForType0.Formato(b.TypeOperand.TypeName())).Field;
                return new IsNotNullExpression(fie.ExternalId);
            }
            else if (operand.NodeType == (ExpressionType)DbExpressionType.ImplementedByAll)
            {
                ImplementedByAllExpression riba = (ImplementedByAllExpression)operand;
                int idType = Schema.Current.IDsForType.GetOrThrow(b.TypeOperand, Resources.TheType0IsNotInTheTypesTable.Formato(b.TypeOperand.TypeName()));
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

                       FieldInitExpression result = new FieldInitExpression(u.Type, null, TypeSqlConstant(u.Type), riba.Id, other, riba.Token); //Delay riba.TypeID to FillFie to make the SQL more clean
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

        internal static SqlConstantExpression TypeSqlConstant(Type type)
        {
            return new SqlConstantExpression(Schema.Current.IDsForType[type], typeof(int?));
        }

        internal static ConstantExpression TypeConstant(Type type)
        {
            return Expression.Constant(Schema.Current.IDsForType[type], typeof(int?));
        }

        //On Sql, nullability has no sense
        protected override Expression VisitBinary(BinaryExpression b)
        {
            Expression left = this.Visit(b.Left);
            Expression right = this.Visit(b.Right);
            if (left != b.Left || right != b.Right)
            {
                if (b.NodeType == ExpressionType.Coalesce)
                    return Expression.Coalesce(left, right, b.Conversion);
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

        internal CommandExpression BindDelete(Expression source)
        {
            ProjectionExpression pr = VisitCastProjection(source);
            FieldInitExpression fie = (FieldInitExpression)pr.Projector;
            Expression id = fie.Table.CreateBinding(null, fie.Table.Name, FieldInitExpression.IdField, null);

            DeleteExpression delete = new DeleteExpression(fie.Table, pr.Source, SmartEqualizer.EqualNullable(id, fie.ExternalId));

            CommandExpression[] relationalDeletes = fie.Table.Fields.Values.Select(ef => ef.Field).OfType<FieldMList>().Select(f =>
            {
                Expression backId = f.RelationalTable.BackColumnExpression(f.RelationalTable.Name);
                return new DeleteExpression(f.RelationalTable, pr.Source,
                    SmartEqualizer.EqualNullable(backId, fie.ExternalId));
            }).Cast<CommandExpression>().ToArray();

            return new CommandAggregateExpression(relationalDeletes.And(delete).And(new SelectRowCountExpression()));
        }

        internal CommandExpression BindUpdate(Expression source, LambdaExpression set)
        {
            ProjectionExpression pr = VisitCastProjection(source);
            FieldInitExpression fie = (FieldInitExpression)pr.Projector;
            Expression id = fie.Table.CreateBinding(null, fie.Table.Name, FieldInitExpression.IdField, null);

            MemberInitExpression mie = (MemberInitExpression)set.Body;
            ParameterExpression param = set.Parameters[0];

            map.Add(param, pr.Projector);
            List<ColumnAssignment> assigments = mie.Bindings.SelectMany(m => ColumnAssigments(param, m)).ToList();

            pr = ApplyExpansions(pr);
            fie = (FieldInitExpression)pr.Projector;
            map.Remove(param);

            return new CommandAggregateExpression(
                new CommandExpression[]
                { 
                    new UpdateExpression(fie.Table, pr.Source, SmartEqualizer.EqualNullable(id, fie.ExternalId), assigments),
                    new SelectRowCountExpression()
                });
        }

        private ColumnAssignment[] ColumnAssigments(Expression obj, MemberBinding m)
        {
            if (m is MemberAssignment)
            {
                MemberAssignment ma = (MemberAssignment)m;
                Expression colExpression = Visit(Expression.MakeMemberAccess(obj, ma.Member));
                Expression expression = Visit(DbQueryProvider.Clean(ma.Expression));
                return Assign(colExpression, expression);
            }
            else if (m is MemberMemberBinding)
            {
                MemberMemberBinding mmb = (MemberMemberBinding)m;
                if(!typeof(EmbeddedEntity).IsAssignableFrom(m.Member.ReturningType()))
                    throw new InvalidOperationException(Resources.DoesNotInheritFromEmbeddedEntity.Formato(m.Member.ReturningType()));

                Expression obj2 = Expression.MakeMemberAccess(obj, mmb.Member);

                return mmb.Bindings.SelectMany(m2 => ColumnAssigments(obj2, m2)).ToArray();
            }
            
            throw new NotImplementedException(m.ToString()); 
        }

        ColumnAssignment AssignColumn(Expression column, Expression expression)
        {
            if (!(column is ColumnExpression))
                throw new InvalidOperationException(Resources.DoesNotRepresentAColumn.Formato(column.NiceToString()));

            return new ColumnAssignment((ColumnExpression)column, DbExpressionNominator.FullNominate(expression, false));
        }

        private ColumnAssignment[] Assign(Expression colExpression, Expression expression)
        {
            if (expression.IsNull())
                return AssignNull(colExpression);

            if (expression is FieldInitExpression && IsNewId(((FieldInitExpression)expression).ExternalId))
                throw new InvalidOperationException(Resources.TheEntityIsNew);

            if (colExpression is ColumnExpression)
            {
                return new[] { AssignColumn(colExpression, expression) };
            }
            else if (colExpression.NodeType == ExpressionType.Convert && colExpression.Type.UnNullify().IsEnum && ((UnaryExpression)colExpression).Operand is ColumnExpression)
            {
                return new[] { AssignColumn(((UnaryExpression)colExpression).Operand, expression) };
            }
            else if (colExpression is LiteReferenceExpression)
            {
                Expression reference = ((LiteReferenceExpression)colExpression).Reference;
                if (expression is LiteReferenceExpression)
                    return Assign(reference, ((LiteReferenceExpression)expression).Reference);

            }
            else if (colExpression is EmbeddedFieldInitExpression)
            {
                EmbeddedFieldInitExpression efie = (EmbeddedFieldInitExpression)colExpression;

                ConstantExpression ce = (ConstantExpression)expression;

                EmbeddedFieldInitExpression efie2 = efie.FieldEmbedded.GetConstantExpression(ce.Value, this);

                var bindings = efie.Bindings.SelectMany(b => Assign(b.Binding,
                    efie2.Bindings.Single(b2 => ReflectionTools.FieldEquals(b.FieldInfo, b2.FieldInfo)).Binding));

                if(efie.HasValue != null)
                {
                    var setValue = AssignColumn(efie.HasValue, Expression.Constant(true));
                    bindings = bindings.PreAnd(setValue);
                }

                return bindings.ToArray();
            }
            else if (colExpression is FieldInitExpression)
            {
                FieldInitExpression colFie = (FieldInitExpression)colExpression;
                if (expression is FieldInitExpression)
                    return new[] { AssignColumn(colFie.ExternalId, ((FieldInitExpression)expression).ExternalId) };

            }
            else if (colExpression is ImplementedByExpression)
            {
                ImplementedByExpression colIb = (ImplementedByExpression)colExpression;
                if (expression is FieldInitExpression)
                {
                    FieldInitExpression fie = (FieldInitExpression)expression;

                    if (!colIb.Implementations.Any(i => i.Type == fie.Type))
                        throw new InvalidOperationException(Resources.Type0IsNotIn1.Formato(fie.Type.Name, colIb.Implementations.ToString(i => i.Type.Name, ", ")));

                    return colIb.Implementations.Select(imp => (AssignColumn(imp.Field.ExternalId,
                       imp.Type == fie.Type ? fie.ExternalId : NullId))).ToArray();
                }
                else if (expression is ImplementedByExpression)
                {
                    ImplementedByExpression ib = (ImplementedByExpression)expression;

                    Type[] types = ib.Implementations.Select(i => i.Type).Except(colIb.Implementations.Select(i => i.Type)).ToArray();
                    if (types.Any())
                        throw new InvalidOperationException(Resources.NoImplementationForTypeS0.Formato(types.ToString(t => t.Name, ", ")));

                    return colIb.Implementations.Select(cImp => AssignColumn(cImp.Field.ExternalId,
                            ib.Implementations.SingleOrDefault(imp => imp.Type == cImp.Type).TryCC(imp => imp.Field.ExternalId) ?? NullId)).ToArray();
                }

            }
            else if (colExpression is ImplementedByAllExpression)
            {
                ImplementedByAllExpression colIba = (ImplementedByAllExpression)colExpression;
                if (expression is FieldInitExpression)
                {
                    FieldInitExpression fie = (FieldInitExpression)expression;
                    return new[]
                    {
                        AssignColumn(colIba.Id, fie.ExternalId),
                        AssignColumn(colIba.TypeId, TypeConstant(fie.Type))
                    };
                }

                if (expression is ImplementedByExpression)
                {
                    ImplementedByExpression ib = (ImplementedByExpression)expression;
                    return new[]
                    {
                        AssignColumn(colIba.Id, Coalesce(typeof(int?), ib.Implementations.Select(e => e.Field.ExternalId))),
                        AssignColumn(colIba.TypeId, 
                            new CaseExpression(ib.Implementations.Select(i => 
                            new When(new IsNotNullExpression(i.Field.ExternalId), TypeConstant(i.Type))), NullId))
                    };
                }

                if (expression is ImplementedByAllExpression)
                {
                    ImplementedByAllExpression iba = (ImplementedByAllExpression)expression;
                    return new[]
                    {
                        AssignColumn(colIba.Id, iba.Id),
                        AssignColumn(colIba.TypeId, iba.TypeId)
                    };
                }
            }

            throw new NotImplementedException(Resources.CanNotBeAssignedFrom1.Formato(colExpression.Type.Name, expression.Type.Name)); 
        }

        private ColumnAssignment[] AssignNull(Expression colExpression)
        {
            if (colExpression is ColumnExpression)
            {
                return new[] { AssignColumn(colExpression, new SqlConstantExpression(null, colExpression.Type)) };
            }
            else if (colExpression.NodeType == ExpressionType.Convert && colExpression.Type.UnNullify().IsEnum && ((UnaryExpression)colExpression).Operand is ColumnExpression)
            {
                ColumnExpression col2 = (ColumnExpression)((UnaryExpression)colExpression).Operand;

                return new[] { AssignColumn(col2, new SqlConstantExpression(null, colExpression.Type)) };
            }
            else if (colExpression is LiteReferenceExpression)
            {
                Expression reference = ((LiteReferenceExpression)colExpression).Reference;
                return AssignNull(reference); 
            }
            else if (colExpression is EmbeddedFieldInitExpression)
            {
                EmbeddedFieldInitExpression efie = (EmbeddedFieldInitExpression)colExpression;
                if (efie.HasValue == null)
                    throw new InvalidOperationException("The EmbeddedField doesn't accept null values");

                var setNull = new ColumnAssignment((ColumnExpression)efie.HasValue, Expression.Constant(false));

                return efie.Bindings.SelectMany(b => AssignNull(b.Binding)).PreAnd(setNull).ToArray();
            }
            else if (colExpression is FieldInitExpression)
            {
                FieldInitExpression colFie = (FieldInitExpression)colExpression;
                return new[] { AssignColumn(colFie.ExternalId, NullId) };
            }
            else if (colExpression is ImplementedByExpression)
            {
                ImplementedByExpression colIb = (ImplementedByExpression)colExpression;
                return colIb.Implementations.Select(imp => (AssignColumn(imp.Field.ExternalId, NullId))).ToArray();
            }
            else if (colExpression is ImplementedByAllExpression)
            {
                ImplementedByAllExpression colIba = (ImplementedByAllExpression)colExpression;

                return new[]
                {
                    AssignColumn(colIba.Id, NullId),
                    AssignColumn(colIba.TypeId, NullId)
                };
            }
            else if (colExpression is EmbeddedFieldInitExpression)
            {
                EmbeddedFieldInitExpression colEfie = (EmbeddedFieldInitExpression)colExpression;
                ColumnAssignment ca = AssignColumn(colEfie.HasValue, new SqlConstantExpression(true, typeof(bool)));
                return colEfie.Bindings.SelectMany(fb => AssignNull(fb.Binding)).PreAnd(ca).ToArray();
            }

            throw new NotImplementedException(Resources.CanNotBeAssignedToNull.Formato(colExpression.Type.Name)); 
        }
    }
}
