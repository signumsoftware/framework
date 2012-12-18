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
using System.Collections.ObjectModel;
using Microsoft.SqlServer.Server;

namespace Signum.Engine.Linq
{
    /// <summary>
    /// QueryBinder is a visitor that converts method calls to LINQ operations into 
    /// custom DbExpression nodes and references to class members into references to columns
    /// </summary>
    internal class QueryBinder : SimpleExpressionVisitor
    {
        Dictionary<ParameterExpression, Expression> map = new Dictionary<ParameterExpression, Expression>();
        Dictionary<Alias, GroupByInfo> groupByMap = new Dictionary<Alias, GroupByInfo>();

        public class GroupByInfo
        {
            public Alias GroupAlias;
            public Expression Projector;
            public SourceExpression Source;
        }

        Expression root;

        public Expression BindQuery(Expression expression)
        {
            this.root = expression;
            return Visit(expression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Queryable) ||
                m.Method.DeclaringType == typeof(Enumerable) ||
                m.Method.DeclaringType == typeof(EnumerableUniqueExtensions))
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
                            m.GetArgument("source"), m.TryGetArgument("predicate").StripQuotes(), m == root);

                    case "FirstEx":
                    case "SingleEx":
                    case "SingleOrDefaultEx":
                        return BindUniqueRow(m.Type, m.Method.Name.RemoveRight(2).ToEnum<UniqueFunction>(),
                           m.GetArgument("collection"), m.TryGetArgument("predicate").StripQuotes(), m == root);
                    case "Distinct":
                        return BindDistinct(m.Type, m.GetArgument("source"));
                    case "Reverse":
                        return BindReverse(m.Type, m.GetArgument("source"));
                    case "Take":
                        return BindTake(m.Type, m.GetArgument("source"), m.GetArgument("count"));
                }
            }
            else if (m.Method.DeclaringType == typeof(LiteUtils) && m.Method.Name == "ToLite")
            {
                Expression toStr = Visit(m.TryGetArgument("toStr")); //could be null

                if (m.Method.GetParameters().FirstEx().ParameterType == typeof(Lite))
                {
                    LiteExpression liteRef = (LiteExpression)Visit(m.GetArgument("lite"));

                    Expression entity = EntityCasting(liteRef.Reference, Lite.Extract(m.Type));

                    return MakeLite(m.Type, entity, toStr);
                }
                else
                {
                    var entity = Visit(m.GetArgument("entity"));
                    var converted = EntityCasting(entity, Lite.Extract(m.Type));
                    return MakeLite(m.Type, converted, toStr);
                }
            }
            else if (m.Method.DeclaringType.IsInstantiationOf(typeof(EnumEntity<>)) && m.Method.Name == "ToEnum")
            {
                EntityExpression fi = (EntityExpression)Visit(m.Object);

                return Expression.Convert((ColumnExpression)fi.ExternalId, m.Method.DeclaringType.GetGenericArguments()[0]);
            }
            else if (m.Object != null && typeof(IEnumerable).IsAssignableFrom(m.Method.DeclaringType) && typeof(string) != m.Method.DeclaringType && m.Method.Name == "Contains")
            {
                return this.BindContains(m.Type, m.Object, m.Arguments[0], m == root);
            }
            else if (m.Object != null && m.Method.Name == "GetType")
            {
                var expression = Visit(m.Object);

                return GetEntityType(expression) ?? Expression.Constant(expression.Type, typeof(Type));
            }

            MethodCallExpression result = (MethodCallExpression)base.VisitMethodCall(m);
            return BindMethodCall(result);
        }

        

        private Expression MapVisitExpand(LambdaExpression lambda, Expression projector, ref SourceExpression newSource)
        {
            map.Add(lambda.Parameters[0], projector);
            Expression result = Visit(lambda.Body);
            map.Remove(lambda.Parameters[0]);

            newSource = ApplyExpansions(newSource);

            return result;
        }

        private Expression MapVisitExpandWithIndex(LambdaExpression lambda, Expression projector, ref SourceExpression newSource)
        {
            var orders = ((SelectExpression)newSource).FollowC(a => a.From as SelectExpression).TakeWhile(s => !s.IsDistinct && s.GroupBy.IsNullOrEmpty()).Select(s => s.OrderBy).FirstOrDefault(o => !o.IsNullOrEmpty());

            RowNumberExpression rne = new RowNumberExpression(orders); //if its null should be filled in a later stage

            //if (hasOrder) // remove order
            //    p = new ProjectionExpression(new SelectExpression(p.Source.Alias, p.Source.Distinct, p.Source.Reverse, p.Source.Top, p.Source.Columns, p.Source.From, p.Source.Where, null, p.Source.GroupBy),
            //        p.Projector, p.UniqueFunction, p.Token, p.Type);

            ColumnDeclaration cd = new ColumnDeclaration("_rowNum", Expression.Subtract(rne, new SqlConstantExpression(1)));

            Alias alias = NextSelectAlias();

            ColumnExpression ce = new ColumnExpression(cd.Expression.Type, alias, cd.Name);

            map.Add(lambda.Parameters[1], ce);

            Expression result = MapVisitExpand(lambda, projector, ref newSource);

            map.Remove(lambda.Parameters[1]);

            ProjectedColumns pc = ColumnProjector.ProjectColumns(projector, alias, newSource.KnownAliases);

            newSource = new SelectExpression(alias, false, false, null, pc.Columns.PreAnd(cd), newSource, null, null, null);

            return result;
        }
      
        private ProjectionExpression VisitCastProjection(Expression source)
        {
            if (source is MethodCallExpression && IsTableValuedFunction((MethodCallExpression)source))
            {
                var oldInTVF = inTableValuedFunction;
                inTableValuedFunction = true;

                var visit = Visit(source);

                inTableValuedFunction = oldInTVF;

                return GetTableValuedFunctionProjection((MethodCallExpression)visit);
            }
            else
            {

                var visit = Visit(source);
                return AsProjection(visit);
            }
        }

        private ProjectionExpression AsProjection(Expression expression)
        {
            if (expression is ProjectionExpression)
                return (ProjectionExpression)expression;

            expression = RemoveProjectionConvert(expression);

            if (expression is ProjectionExpression)
                return (ProjectionExpression)expression;

            if (expression.NodeType == ExpressionType.New && expression.Type.IsInstantiationOf(typeof(Grouping<,>)))
            {
                NewExpression nex = (NewExpression)expression;
                return (ProjectionExpression)nex.Arguments[1];
            }

            if (expression is MethodCallExpression && IsTableValuedFunction((MethodCallExpression)expression))
            {
              
            }

            throw new InvalidOperationException("Impossible to convert in ProjectionExpression: \r\n" + expression.NiceToString()); 
        }

        private static Expression RemoveProjectionConvert(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert && (expression.Type.IsInstantiationOf(typeof(IGrouping<,>)) ||
                                                                  expression.Type.IsInstantiationOf(typeof(IEnumerable<>)) || 
                                                                  expression.Type.IsInstantiationOf(typeof(IQueryable<>)) ))
                expression = ((UnaryExpression)expression).Operand;
            return expression;
        }

        private Expression BindTake(Type resultType, Expression source, Expression count)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            Alias alias = NextSelectAlias();

            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, projection.Select.KnownAliases);

            return new ProjectionExpression(
                new SelectExpression(alias, false, false, count, pc.Columns, projection.Select, null, null, null),
                pc.Projector, null, resultType);
        }

        //Avoid self referencing SQL problems
        bool inTableValuedFunction = false;

        private Expression BindUniqueRow(Type resultType, UniqueFunction function, Expression source, LambdaExpression predicate, bool isRoot)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            SourceExpression newSource = projection.Select;

            Expression where = predicate == null ? null : DbExpressionNominator.FullNominate(MapVisitExpand(predicate, projection.Projector, ref newSource));

            Alias alias = NextSelectAlias();
            Expression top = function == UniqueFunction.First || function == UniqueFunction.FirstOrDefault ? Expression.Constant(1) : null;

            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, newSource.KnownAliases);

            if (!isRoot && !inTableValuedFunction && pc.Projector is ColumnExpression && (function == UniqueFunction.First || function == UniqueFunction.FirstOrDefault))
                return new ScalarExpression(pc.Projector.Type,
                    new SelectExpression(alias, false, false, top, new[] { new ColumnDeclaration("val", pc.Projector) }, newSource, where, null, null));

            var newProjector = new ProjectionExpression(
                new SelectExpression(alias, false, false, top, pc.Columns, newSource, where, null, null),
                pc.Projector, function, resultType);

            if (isRoot)
                return newProjector;

            var proj = uniqueFunctionReplacements.GetOrCreate(newProjector, () =>
            {
                requests.Add(new UniqueRequest
                {
                    Select = newProjector.Select,
                    OuterApply = function == UniqueFunction.SingleOrDefault || function == UniqueFunction.FirstOrDefault,
                    ExternalAliases = ExternalAliasGatherer.Externals(newProjector.Select, AliasGatherer.Gather(newProjector.Select))
                });

                return newProjector.Projector;
            });

            return proj;
        }

     
        private Expression BindDistinct(Type resultType, Expression source)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            Alias alias = NextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, projection.Select.KnownAliases, selectTrivialColumns: true);
            return new ProjectionExpression(
                new SelectExpression(alias, true, false, null, pc.Columns, projection.Select, null, null, null),
                pc.Projector, null, resultType);
        }

        private Expression BindReverse(Type resultType, Expression source)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            Alias alias = NextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, projection.Select.KnownAliases);
            return new ProjectionExpression(
                new SelectExpression(alias, false, true, null, pc.Columns, projection.Select, null, null, null),
                pc.Projector, null, resultType);
        }

        private Expression BindAggregate(Type resultType, AggregateFunction aggregateFunction, Expression source, LambdaExpression selector, bool isRoot)
        {
            Type enumType = ExtractEnum(ref resultType, aggregateFunction);

            ProjectionExpression projection = this.VisitCastProjection(source);
            
            SourceExpression newSource = projection.Select;

            Expression exp = aggregateFunction == AggregateFunction.Count ? null :
                selector != null ? MapVisitExpand(selector, projection.Projector, ref newSource) :
                projection.Projector;

            Expression aggregate;
            if (!resultType.IsNullable() && aggregateFunction == AggregateFunction.Sum)
            {
                var nominated = DbExpressionNominator.FullNominate(Expression.Convert(exp, resultType.Nullify()));

                aggregate = (Expression)Expression.Coalesce(
                    new AggregateExpression(resultType.Nullify(), nominated, aggregateFunction),
                    new SqlConstantExpression(Activator.CreateInstance(resultType), resultType));
            }
            else
            {
                var nominated = DbExpressionNominator.FullNominate(exp);

                aggregate = new AggregateExpression(resultType, nominated, aggregateFunction);
            }


            Alias alias = NextSelectAlias();

            ColumnDeclaration cd = new ColumnDeclaration("a", aggregate);

            SelectExpression select = new SelectExpression(alias, false, false, null, new[] { cd }, newSource, null, null, null);

            if (isRoot)
                return new ProjectionExpression(select,
                   RestoreEnum(ColumnProjector.SingleProjection(cd, alias, resultType), enumType),
                   UniqueFunction.Single, enumType ?? resultType);

            ScalarExpression subquery = new ScalarExpression(resultType, select);

            GroupByInfo info = groupByMap.TryGetC(projection.Select.Alias);
            if (info != null)
            {
                SourceExpression newsSource2 = info.Source;
                Expression exp2 = aggregateFunction == AggregateFunction.Count ? null : 
                    selector != null ? MapVisitExpand(selector, info.Projector, ref newsSource2):
                    info.Projector;

                var nominated2 = DbExpressionNominator.FullNominate(exp2);

                var result = new AggregateSubqueryExpression(info.GroupAlias,
                    new AggregateExpression(resultType, nominated2, aggregateFunction),
                    subquery);

                return RestoreEnum(result, enumType);
            }

            return RestoreEnum(subquery, enumType);
        }

        static Type ExtractEnum(ref Type resultType, AggregateFunction aggregateFunction)
        {
            if (resultType.UnNullify().IsEnum)
            {
                if (aggregateFunction != AggregateFunction.Min && aggregateFunction != AggregateFunction.Max)
                    throw new InvalidOperationException("{0} is not allowed for {1}".Formato(aggregateFunction, resultType));

                Type result = resultType;
                resultType = resultType.IsNullable() ? typeof(int?) : typeof(int);
                return result;
            }

            return null;
        }

        static Expression RestoreEnum(Expression expression, Type enumType)
        {
            if (enumType == null)
                return expression;

            return Expression.Convert(expression, enumType);
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

                Expression where = isAll ? exp.AggregateAnd() : exp.AggregateOr();

                return this.Visit(where);
            }
            else
            {
                if (isAll)
                    predicate = Expression.Lambda(Expression.Not(predicate.Body), predicate.Parameters.ToArray());

                if (predicate != null)
                    source = Expression.Call(typeof(Enumerable), "Where", method.GetGenericArguments(), source, predicate);

                ProjectionExpression projection = this.VisitCastProjection(source);
                Expression result = new ExistsExpression(projection.Select);
                if (isAll)
                    result = Expression.Not(result);

                if (isRoot)
                    return GetUniqueProjection(resultType, result, UniqueFunction.SingleOrDefault);
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

                if (newItem.Type == typeof(Type))
                    return SmartEqualizer.TypeIn(newItem, col == null ? Enumerable.Empty<Type>() : col.Cast<Type>().ToList());

                switch ((DbExpressionType)newItem.NodeType)
                {
                    case DbExpressionType.Lite: return SmartEqualizer.EntityIn((LiteExpression)newItem, col == null ? Enumerable.Empty<Lite>() : col.Cast<Lite>().ToList());
                    case DbExpressionType.Entity:
                    case DbExpressionType.ImplementedBy:
                    case DbExpressionType.ImplementedByAll: return SmartEqualizer.EntityIn(newItem, col == null ? Enumerable.Empty<IdentifiableEntity>() : col.Cast<IdentifiableEntity>().ToList());
                    default:
                        return InExpression.FromValues(newItem, col == null ? new object[0] : col.Cast<object>().ToArray());
                }
            }
            else
            {
                ProjectionExpression projection = this.VisitCastProjection(source);

                Alias alias = NextSelectAlias();
                var pc = ColumnProjector.ProjectColumns(projection.Projector, alias, projection.Select.KnownAliases, aggresiveNomination: false, selectTrivialColumns: true);

                SubqueryExpression se = null;
                if (Schema.Current.Settings.IsDbType(pc.Projector.Type))
                    se = new InExpression(newItem, projection.Select);
                else
                {
                    Expression where = DbExpressionNominator.FullNominate(SmartEqualizer.PolymorphicEqual(projection.Projector, newItem));
                    se = new ExistsExpression(new SelectExpression(alias, false, false, null, pc.Columns, projection.Select, where, null, null));
                }

                if (isRoot)
                    return this.GetUniqueProjection(resultType, se, UniqueFunction.SingleOrDefault);
                else
                    return se;
            }
        }

        private ProjectionExpression GetUniqueProjection(Type resultType, Expression expr, UniqueFunction uniqueFunction)
        {
            if (expr.Type != typeof(bool))
                throw new ArgumentException("expr");

            var alias = NextSelectAlias();
            SelectExpression select = new SelectExpression(alias, false, false, null, new[] { new ColumnDeclaration("value", expr) }, null, null, null, null);
            return new ProjectionExpression(select, new ColumnExpression(expr.Type, alias, "value"), uniqueFunction, resultType);
        }

        private Expression BindWhere(Type resultType, Expression source, LambdaExpression predicate)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            SourceExpression newSource = projection.Select;
            Expression exp = predicate.Parameters.Count == 1 ? 
                MapVisitExpand(predicate, projection.Projector, ref newSource) :
                MapVisitExpandWithIndex(predicate, projection.Projector, ref newSource);

            if (exp.NodeType == ExpressionType.Constant && ((bool)((ConstantExpression)exp).Value))
                return projection;

            Expression where = DbExpressionNominator.FullNominate(exp);

            Alias alias = NextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, newSource.KnownAliases);
            return new ProjectionExpression(
                new SelectExpression(alias, false, false, null, pc.Columns, newSource, where, null, null),
                pc.Projector, null, resultType);
        }

        private Expression BindSelect(Type resultType, Expression source, LambdaExpression selector)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            SourceExpression newSource = projection.Select;
            Expression expression = selector.Parameters.Count == 1 ? 
                MapVisitExpand(selector, projection.Projector, ref newSource) :
                MapVisitExpandWithIndex(selector, projection.Projector, ref newSource);

            Alias alias = NextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(expression, alias, newSource.KnownAliases);
            return new ProjectionExpression(
                new SelectExpression(alias, false, false, null, pc.Columns, newSource, null, null, null),
                pc.Projector, null, resultType);
        }

        protected virtual Expression BindSelectMany(Type resultType, Expression source, LambdaExpression collectionSelector, LambdaExpression resultSelector)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            bool outer = OverloadingSimplifier.ExtractDefaultIfEmpty(ref collectionSelector);

            JoinType joinType = IsTable(collectionSelector.Body) ? JoinType.CrossJoin :
                                outer ? JoinType.OuterApply :
                                JoinType.CrossApply;

            SourceExpression newSource = projection.Select;
            Expression collectionExpression = collectionSelector.Parameters.Count == 1 ?
                MapVisitExpand(collectionSelector, projection.Projector, ref newSource) :
                MapVisitExpandWithIndex(collectionSelector, projection.Projector, ref newSource);

            ProjectionExpression collectionProjection = AsProjection(collectionExpression);

            Alias alias = NextSelectAlias();
            if (resultSelector == null)
            {
                ProjectedColumns pc = ColumnProjector.ProjectColumns(collectionProjection.Projector, alias,
                    newSource.KnownAliases.Concat(collectionProjection.Select.KnownAliases).ToArray());

                JoinExpression join = new JoinExpression(joinType, newSource, collectionProjection.Select, null);

                var result = new ProjectionExpression(
                    new SelectExpression(alias, false, false, null, pc.Columns, join, null, null, null),
                    pc.Projector, null, resultType);

                return result;
            }
            else
            {
                map.SetRange(resultSelector.Parameters, new[] { projection.Projector, collectionProjection.Projector });
                Expression resultProjector = Visit(resultSelector.Body);
                map.RemoveRange(resultSelector.Parameters);

                ProjectedColumns pc = ColumnProjector.ProjectColumns(resultProjector, alias,
                    newSource.KnownAliases.Concat(collectionProjection.Select.KnownAliases).ToArray());

                JoinExpression join = new JoinExpression(joinType, newSource, collectionProjection.Select, null);

                var newJoinSource = ApplyExpansions(join);

                var result = new ProjectionExpression(
                    new SelectExpression(alias, false, false, null, pc.Columns, newJoinSource, null, null, null),
                    pc.Projector, null, resultType);

                return result;
            }
        }

        protected virtual Expression BindJoin(Type resultType, Expression outerSource, Expression innerSource, LambdaExpression outerKey, LambdaExpression innerKey, LambdaExpression resultSelector)
        {
            bool rightOuter = OverloadingSimplifier.ExtractDefaultIfEmpty(ref outerSource);
            bool leftOuter = OverloadingSimplifier.ExtractDefaultIfEmpty(ref innerSource);

            ProjectionExpression outerProj = VisitCastProjection(outerSource);
            ProjectionExpression innerProj = VisitCastProjection(innerSource);

            SourceExpression newOuterSource = outerProj.Select;
            SourceExpression newInnerSource = innerProj.Select;

            Expression outerKeyExpr = MapVisitExpand(outerKey, outerProj.Projector, ref newOuterSource);
            Expression innerKeyExpr = MapVisitExpand(innerKey, innerProj.Projector, ref newInnerSource);

            Expression condition = DbExpressionNominator.FullNominate(SmartEqualizer.EqualNullable(outerKeyExpr, innerKeyExpr));

            JoinType jt = rightOuter && leftOuter ? JoinType.FullOuterJoin :
                          rightOuter ? JoinType.RightOuterJoin :
                          leftOuter ? JoinType.LeftOuterJoin :
                          JoinType.InnerJoin;

            Alias alias = NextSelectAlias();

            map.SetRange(resultSelector.Parameters, new[] { outerProj.Projector, innerProj.Projector });
            Expression resultExpr = Visit(resultSelector.Body);
            map.RemoveRange(resultSelector.Parameters);

            JoinExpression join = new JoinExpression(jt, newOuterSource, newInnerSource, condition);

            ProjectedColumns pc = ColumnProjector.ProjectColumns(resultExpr, alias,
                newOuterSource.KnownAliases.Concat(newInnerSource.KnownAliases).ToArray());

            var newSource = ApplyExpansions(join);

            ProjectionExpression result = new ProjectionExpression(
                new SelectExpression(alias, false, false, null, pc.Columns, newSource, null, null, null),
                pc.Projector, null, resultType);

            return result;
        }

        private Expression BindGroupBy(Type resultType, Expression source, LambdaExpression keySelector, LambdaExpression elementSelector)
        {
            ProjectionExpression projection = VisitCastProjection(source);
            ProjectionExpression subqueryProjection = VisitCastProjection(source); // make duplicate of source query as basis of element subquery by visiting the source again

            Alias alias = NextSelectAlias();

            SourceExpression newSource = projection.Select;
            Expression key = GroupEntityCleaner.Clean(MapVisitExpand(keySelector, projection.Projector, ref newSource));
            ProjectedColumns keyPC = ColumnProjector.ProjectColumns(key, alias, newSource.KnownAliases, aggresiveNomination: true, selectTrivialColumns: true);  // Use ProjectColumns to get group-by expressions from key expression
            Expression elemExpr = elementSelector == null ? projection.Projector : MapVisitExpand(elementSelector, projection.Projector, ref newSource);

            SourceExpression subQueryNewSource = subqueryProjection.Select;
            Expression subqueryKey = GroupEntityCleaner.Clean(MapVisitExpand(keySelector, subqueryProjection.Projector, ref subQueryNewSource));// recompute key columns for group expressions relative to subquery (need these for doing the correlation predicate
            ProjectedColumns subqueryKeyPC = ColumnProjector.ProjectColumns(subqueryKey, Alias.Raw("basura"), subQueryNewSource.KnownAliases, aggresiveNomination: true, selectTrivialColumns: true); // use same projection trick to get group-by expressions based on subquery
            Expression subqueryElemExpr = elementSelector == null ? subqueryProjection.Projector : MapVisitExpand(elementSelector, subqueryProjection.Projector, ref subQueryNewSource); // compute element based on duplicated subquery

            Expression subqueryCorrelation = keyPC.Columns.IsEmpty() ? null : 
                keyPC.Columns.Zip(subqueryKeyPC.Columns, (c1, c2) => SmartEqualizer.EqualNullableGroupBy(new ColumnExpression(c1.Expression.Type, alias, c1.Name), c2.Expression))
                    .AggregateAnd();

            // build subquery that projects the desired element
            Alias elementAlias = NextSelectAlias();
            ProjectedColumns elementPC = ColumnProjector.ProjectColumns(subqueryElemExpr, elementAlias, subQueryNewSource.KnownAliases);
            ProjectionExpression elementSubquery =
                new ProjectionExpression(
                    new SelectExpression(elementAlias, false, false, null, elementPC.Columns, subQueryNewSource, subqueryCorrelation, null, null),
                    elementPC.Projector, null, typeof(IEnumerable<>).MakeGenericType(elementPC.Projector.Type));

            NewExpression newResult = Expression.New(typeof(Grouping<,>).MakeGenericType(key.Type, subqueryElemExpr.Type).GetConstructors()[1],
                        new Expression[] { keyPC.Projector, elementSubquery });

            Expression resultExpr = Expression.Convert(newResult
                , typeof(IGrouping<,>).MakeGenericType(key.Type, subqueryElemExpr.Type));

            this.groupByMap.Add(elementAlias, new GroupByInfo
            {
                GroupAlias = alias,
                Projector = elemExpr,
                Source = newSource,
            });

            var result = new ProjectionExpression(
                new SelectExpression(alias, false, false, null, keyPC.Columns, newSource, null, null, keyPC.Columns.Select(c => c.Expression)),
                resultExpr, null, resultType.GetGenericTypeDefinition().MakeGenericType(resultExpr.Type));

            return result;
        }

   
        List<OrderExpression> thenBys;
        protected virtual Expression BindOrderBy(Type resultType, Expression source, LambdaExpression orderSelector, OrderType orderType)
        {
            List<OrderExpression> myThenBys = this.thenBys;
            this.thenBys = null;
            ProjectionExpression projection = this.VisitCastProjection(source);

            SourceExpression newSource = projection.Select;

            List<OrderExpression> orderings = new List<OrderExpression>();
            orderings.Add(new OrderExpression(orderType, GetOrderExpression(orderSelector, projection.Projector, ref newSource)));

            if (myThenBys != null)
            {
                for (int i = myThenBys.Count - 1; i >= 0; i--)
                {
                    OrderExpression tb = myThenBys[i];
                    orderings.Add(new OrderExpression(tb.OrderType, GetOrderExpression((LambdaExpression)tb.Expression, projection.Projector, ref newSource)));
                }
            }

            Alias alias = NextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, newSource.KnownAliases);
            return new ProjectionExpression(
                new SelectExpression(alias, false, false, null, pc.Columns, newSource, null, orderings.AsReadOnly(), null),
                pc.Projector, null, resultType);
        }

        private Expression GetOrderExpression(LambdaExpression lambda, Expression projector, ref SourceExpression source)
        {
            map.Add(lambda.Parameters[0], projector);
            Expression expr = Visit(lambda.Body);
            map.Remove(lambda.Parameters[0]);

            if (expr is LiteExpression)
            {
                LiteExpression lite = (LiteExpression)expr;
                expr = lite.Reference is ImplementedByAllExpression ? ((LiteExpression)expr).Id :
                      BindMethodCall(Expression.Call(lite.Reference, EntityExpression.ToStringMethod));
            }
            else if (expr is EntityExpression || expr is ImplementedByExpression)
            {
                expr = BindMethodCall(Expression.Call(expr, EntityExpression.ToStringMethod));
            }
            else if (expr is ImplementedByAllExpression)
            {
                expr = ((ImplementedByAllExpression)expr).Id;
            }
            else if (expr is MethodCallExpression && ReflectionTools.MethodEqual(((MethodCallExpression)expr).Method, miToUserInterface))
            {
                expr = ((MethodCallExpression)expr).Arguments[0];
            }
            else if (expr.Type == typeof(Type))
            {
                expr = ExtractTypeId(expr);
            }

            source = ApplyExpansions(source);

            return DbExpressionNominator.FullNominate(expr);
        }

        static MethodInfo miToUserInterface = ReflectionTools.GetMethodInfo(() => DateTime.MinValue.ToUserInterface()); 

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
            return c != null && IsTable(c.Value);
        }

        public bool IsTable(object value)
        {
            IQueryable query = value as IQueryable;
            if (query == null)
                return false;

            if (!query.IsBase())
                throw new InvalidOperationException("Constant Expression with complex IQueryable not expected at this stage");

            Type type = value.GetType();

            if (!type.IsInstantiationOf(typeof(SignumTable<>)) && !type.IsInstantiationOf(typeof(Query<>)))
                throw new InvalidOperationException("{0} belongs to another kind of Linq Provider".Formato(type.TypeName()));

            return true;
        }

        public bool IsTableValuedFunction(MethodCallExpression mce)
        {
            return typeof(IQueryable).IsAssignableFrom(mce.Method.ReturnType) &&
                mce.Method.SingleAttribute<SqlMethodAttribute>() != null;
        }

        private ProjectionExpression GetTableProjection(IQueryable query)
        { 
            ITable table = query is ISignumTable ? ((ISignumTable)query).Table : new ViewBuilder(Schema.Current).NewView(query.ElementType);

            Alias tableAlias = NextTableAlias(table.Name);

            Expression exp = table is Table ? 
                ((Table)table).GetProjectorExpression(tableAlias, this) :
                ((RelationalTable)table).GetProjectorExpression(tableAlias, this);

            Type resultType = typeof(IQueryable<>).MakeGenericType(query.ElementType);
            TableExpression tableExpression = new TableExpression(tableAlias, table);

            Alias selectAlias = NextSelectAlias();

            ProjectedColumns pc = ColumnProjector.ProjectColumns(exp, selectAlias, new[] { tableAlias });

            ProjectionExpression projection = new ProjectionExpression(
                new SelectExpression(selectAlias, false, false, null, pc.Columns, tableExpression, null, null, null),
            pc.Projector, null, resultType);

            return projection;
        }

        private ProjectionExpression GetTableValuedFunctionProjection(MethodCallExpression mce)
        {
            Type returnType = mce.Method.ReturnType;
            var type = returnType.GetGenericArguments()[0];

            Table table = new ViewBuilder(Schema.Current).NewView(type);

            Alias tableAlias = NextTableAlias(table.Name);

            Expression exp = table.GetProjectorExpression(tableAlias, this);

            var functionName = mce.Method.SingleAttribute<SqlMethodAttribute>().Name ?? mce.Method.Name;

            var argumens  = mce.Arguments.Select(DbExpressionNominator.FullNominate).ToList();

            SqlTableValuedFunctionExpression tableExpression = new SqlTableValuedFunctionExpression(functionName, table, tableAlias, argumens);

            Alias selectAlias = NextSelectAlias();

            ProjectedColumns pc = ColumnProjector.ProjectColumns(exp, selectAlias, new[] { tableAlias });

            ProjectionExpression projection = new ProjectionExpression(
                new SelectExpression(selectAlias, false, false, null, pc.Columns, tableExpression, null, null, null),
            pc.Projector, null, returnType);

            return projection;
        }

        internal Expression VisitConstant(object value, Type type)
        {
            return VisitConstant(Expression.Constant(value, type));
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if(IsTable(c.Value))
                return GetTableProjection((IQueryable)c.Value);
           
            return c;
        }

        static bool IsNewId(Expression expression)
        {
            ConstantExpression ce = expression as ConstantExpression;
            return ce != null && ce.Type.UnNullify() == typeof(int) && int.MinValue.Equals(ce.Value);
        }

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

        public Expression BindMethodCall(MethodCallExpression m)
        {
            Expression source = m.Method.IsExtensionMethod() ? m.Arguments[0]: m.Object;

            if (source == null || m.Method.Name == "InSql")
                return m;

            if (source.NodeType == ExpressionType.Conditional)
            {
                var con = (ConditionalExpression)source;
                return DispatchConditional(m, con.Test, con.IfTrue, con.IfFalse);
            }

            if (source.NodeType == ExpressionType.Coalesce)
            {
                var bin = (BinaryExpression)source;
                return DispatchConditional(m, Expression.NotEqual(bin.Left, Expression.Constant(null, bin.Left.Type)), bin.Left, bin.Right); 
            }

            if (ExpressionCleaner.HasExpansions(source.Type, m.Method) && source is EntityExpression) //new expansions discovered
            {
                Dictionary<ParameterExpression, Expression> replacements = new Dictionary<ParameterExpression, Expression>();
                Func<Expression, ParameterInfo, Expression> replace = (e, pi) =>
                {
                    if (e == null || e.NodeType == ExpressionType.Quote || e.NodeType == ExpressionType.Lambda || pi != null && pi.HasAttribute<EagerBindingAttribute>())
                        return e;
                    ParameterExpression pe = Expression.Parameter(e.Type, "p" + replacements.Count);
                    replacements.Add(pe, e);
                    return pe;
                };

                var parameters = m.Method.GetParameters();

                MethodCallExpression simple = Expression.Call(replace(m.Object, null), m.Method, m.Arguments.Select((a, i) => replace(a, parameters[i])).ToArray());

                Expression binded = ExpressionCleaner.BindMethodExpression(simple, true);

                Expression cleanedSimple = DbQueryProvider.Clean(binded, true, null);
                map.AddRange(replacements);
                Expression result = Visit(cleanedSimple);
                map.RemoveRange(replacements.Keys);
                return result;
            }

            if (source.NodeType == (ExpressionType)DbExpressionType.ImplementedBy)
            {
                ImplementedByExpression ib = (ImplementedByExpression)source;

                if (m.Method.IsExtensionMethod())
                {
                    List<When> whens = ib.Implementations.Select(
                        imp => new When(imp.Reference.ExternalId.NotEqualsNulll(),
                            BindMethodCall(Expression.Call(null, m.Method, m.Arguments.Skip(1).PreAnd(imp.Reference))))).ToList();

                    return CombineWhens(whens, m.Type);
                }
                else
                {
                    List<When> whens = ib.Implementations.Select(
                       imp => new When(imp.Reference.ExternalId.NotEqualsNulll(),
                           BindMethodCall(Expression.Call(imp.Reference, m.Method, m.Arguments)))).ToList();

                    return CombineWhens(whens, m.Type);
                }
            }

            if (m.Method.Name == "ToString" && m.Method.GetParameters().Length == 0)
            {
                if (source.NodeType == (ExpressionType)DbExpressionType.Entity)
                {
                    EntityExpression ee = (EntityExpression)source;

                    if (Schema.Current.Table(ee.Type).ToStrColumn != null)
                    {
                        return Completed(ee).GetBinding(EntityExpression.ToStrField);
                    }

                    throw new InvalidOperationException("ToString expression should already been expanded at this stage");
                }
                else if (source.NodeType == (ExpressionType)DbExpressionType.Lite)
                {
                    LiteExpression lite = (LiteExpression)source;

                    var toStr = BindMethodCall(Expression.Call(lite.Reference, EntityExpression.ToStringMethod));

                    return toStr;
                }
            }

            return m;
        }


        private ConditionalExpression DispatchConditional(MethodCallExpression m, Expression test, Expression ifTrue, Expression ifFalse)
        {
            if (m.Method.IsExtensionMethod())
            {
                return Expression.Condition(test,
                    BindMethodCall(Expression.Call(m.Method, m.Arguments.Skip(1).PreAnd(ifTrue))),
                    BindMethodCall(Expression.Call(m.Method, m.Arguments.Skip(1).PreAnd(ifFalse))));
            }
            else
            {
                return Expression.Condition(test,
                    BindMethodCall(Expression.Call(ifTrue, m.Method, m.Arguments)),
                    BindMethodCall(Expression.Call(ifFalse, m.Method, m.Arguments)));
            }
        }

        public Expression BindMemberAccess(MemberExpression m)
        {
            Expression source = m.Expression;

            if (source.NodeType == ExpressionType.Conditional)
            {
                var con = (ConditionalExpression)source;
                return Expression.Condition(con.Test,
                    BindMemberAccess(Expression.MakeMemberAccess(con.IfTrue, m.Member)),
                    BindMemberAccess(Expression.MakeMemberAccess(con.IfFalse, m.Member)));
            }

            if (source.NodeType == ExpressionType.Coalesce)
            {
                var bin = (BinaryExpression)source;
                return Expression.Condition(
                    Expression.NotEqual(bin.Left, Expression.Constant(null, bin.Left.Type)),
                    BindMemberAccess(Expression.MakeMemberAccess(bin.Left, m.Member)),
                    BindMemberAccess(Expression.MakeMemberAccess(bin.Right, m.Member)));
            }

            if (source != null && m.Member is PropertyInfo && ExpressionCleaner.HasExpansions(source.Type, (PropertyInfo)m.Member) && source is EntityExpression) //new expansions discovered
            {
                ParameterExpression parameter = Expression.Parameter(m.Expression.Type, "temp");
                MemberExpression simple = Expression.MakeMemberAccess(parameter, m.Member);

                Expression binded = ExpressionCleaner.BindMemberExpression(simple, true);

                Expression cleanedSimple = DbQueryProvider.Clean(binded, true, null);
                map.Add(parameter, source);
                Expression result = Visit(cleanedSimple);
                map.Remove(parameter);
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

            source = RemoveProjectionConvert(source);

            switch (source.NodeType)
            {
                case ExpressionType.MemberInit:
                    return ((MemberInitExpression)source).Bindings
                        .OfType<MemberAssignment>()
                        .SingleEx(a => ReflectionTools.MemeberEquals(a.Member, m.Member)).Expression;
                case ExpressionType.New:
                    {
                        NewExpression nex = (NewExpression)source;

                        if (nex.Type.IsInstantiationOf(typeof(Grouping<,>)))
                        {
                            if (m.Member.Name == "Key")
                                return nex.Arguments[0];
                        }
                        else if (TupleReflection.IsTuple(nex.Type))
                        {
                            int index = TupleReflection.TupleIndex((PropertyInfo)m.Member);
                            return nex.Arguments[index];
                        }
                        else
                        {
                            if (nex.Members == null)
                                throw new InvalidOperationException("Impossible to bind '{0}' on '{1}'".Formato(m.Member.Name, nex.Constructor.ConstructorSignature()));

                            PropertyInfo pi = (PropertyInfo)m.Member;
                            return nex.Members.Zip(nex.Arguments).SingleEx(p => ReflectionTools.PropertyEquals((PropertyInfo)p.Item1, pi)).Item2;
                        }
                        break; 
                    }
                case (ExpressionType)DbExpressionType.Entity:
                {
                    EntityExpression ee = (EntityExpression)source;
                    FieldInfo fi = m.Member as FieldInfo ?? Reflector.TryFindFieldInfo(ee.Type, (PropertyInfo)m.Member);

                    if (fi == null)
                        throw new InvalidOperationException("The member {0} of {1} is not accesible on queries".Formato(m.Member.Name, ee.Type.TypeName()));
                    
                    if (fi != null && fi.FieldEquals((IdentifiableEntity ie) => ie.id))
                        return ee.ExternalId.UnNullify();

                    Expression result = Completed(ee).GetBinding(fi);

                    if (result is MListExpression)
                        return MListProjection((MListExpression)result);
                    
                    return result;
                }
                case (ExpressionType)DbExpressionType.EmbeddedInit:
                {
                    EmbeddedEntityExpression eee = (EmbeddedEntityExpression)source;
                    FieldInfo fi = m.Member as FieldInfo ?? Reflector.FindFieldInfo(eee.Type, (PropertyInfo)m.Member);

                    if (fi == null)
                        throw new InvalidOperationException("The member {0} of {1} is not accesible on queries".Formato(m.Member.Name, eee.Type.TypeName()));

                    Expression result = eee.GetBinding(fi);
                    return result;
                }
                case (ExpressionType)DbExpressionType.Lite:
                {
                    LiteExpression liteRef = (LiteExpression)source;
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
                                throw new InvalidOperationException("ToStr is not accesible on queries for ImplementedByAll");
                            return liteRef.ToStr;
                        }
                    }

                    if (pi.Name == "RuntimeType")
                        return liteRef.TypeId;
                    
                    throw new InvalidOperationException("The member {0} of Lite is not accessible on queries".Formato(m.Member));
                }
                case (ExpressionType)DbExpressionType.ImplementedBy:
                {
                    ImplementedByExpression ib = (ImplementedByExpression)source;
                    List<When> whens = ib.Implementations.Select(imp => new When(
                        imp.Reference.ExternalId.NotEqualsNulll(),
                        BindMemberAccess(Expression.MakeMemberAccess(imp.Reference, m.Member)))).ToList();

                    var result = CombineWhens(whens, m.Member.ReturningType());

                    return result;
                }
                case (ExpressionType)DbExpressionType.ImplementedByAll:
                {
                    ImplementedByAllExpression iba = (ImplementedByAllExpression)source;
                    FieldInfo fi = m.Member as FieldInfo ?? Reflector.FindFieldInfo(iba.Type, (PropertyInfo)m.Member);
                    if (fi != null && fi.FieldEquals((IdentifiableEntity ie) => ie.id))
                        return iba.Id;

                    throw new InvalidOperationException("The member {0} of ImplementedByAll is not accesible on queries".Formato(m.Member));
                }
                case (ExpressionType)DbExpressionType.MListElement:
                {
                    MListElementExpression mle = (MListElementExpression)source;

                    switch (m.Member.Name)
	                {
                        case "RowId": return mle.RowId;
                        case "Parent": return mle.Parent;
                        case "Element": return mle.Element;
                        default: 
                             throw new InvalidOperationException("The member {0} of MListElement is not accesible on queries".Formato(m.Member));
	                }
                }
            }
      
            return Expression.MakeMemberAccess(source, m.Member);
        }

        private Expression CombineWhens(List<When> whens, Type returnType)
        {
            if(whens.Count == 0)
                return Expression.Constant(null, returnType.Nullify());

            if (whens.Count == 1)
                return whens.SingleEx().Value;

            if (whens.All(e => e.Value is LiteExpression))
            {
                Expression entity = CombineWhens(whens.Select(w => new When(w.Condition,
                    ((LiteExpression)w.Value).Reference)).ToList(), Lite.Extract(returnType));

                return MakeLite(returnType, entity, null);
            }

            if (whens.Any(e => e.Value is ImplementedByAllExpression))
            {
                Expression id = whens.Select(w => new When(w.Condition, GetId(w.Value))).ToCondition(typeof(int?));
                TypeImplementedByAllExpression typeId = (TypeImplementedByAllExpression)CombineWhens(
                    whens.Select(w => new When(w.Condition, GetEntityType(w.Value))).ToList(), typeof(Type));

                return new ImplementedByAllExpression(returnType, id, typeId);
            }

            if(whens.All(e=>e.Value is EntityExpression || e.Value is ImplementedByExpression))
            {
                var fies = (from e in whens
                            where e.Value is EntityExpression
                            select new {e.Value.Type, Fie = (EntityExpression)e.Value, e.Condition }).ToList();

                var ibs = (from e in whens
                            where e.Value is ImplementedByExpression
                            from imp in ((ImplementedByExpression)e.Value).Implementations
                            select new {imp.Type, Fie = imp.Reference, Condition = (Expression)Expression.And(e.Condition, 
                                Expression.NotEqual(imp.Reference.ExternalId, Expression.Constant(null, typeof(int?)))) }).ToList();

                var groups = fies.Concat(ibs).GroupToDictionary(a => a.Type);

                var implementations = groups.Select(g =>
                    new ImplementationColumn(g.Key,
                        new EntityExpression(g.Key,
                            CombineWhens(g.Value.Select(w => new When(w.Condition, w.Fie.ExternalId)).ToList(), typeof(int?)), null, null))).ToReadOnly();

                if(implementations.Count == 1)
                    return implementations[0].Reference;

                return new ImplementedByExpression(returnType, implementations);    
            }

            if(whens.All(e=>e.Value is EmbeddedEntityExpression))
            {
                var lc = new LambdaComparer<FieldInfo, string>(fi=>fi.Name);

                var groups = (from w in whens
                             from b in ((EmbeddedEntityExpression)w.Value).Bindings
                             group new When(w.Condition, b.Binding) by b.FieldInfo into g
                             select KVP.Create(g.Key, g.ToList())).ToDictionary(); 

                var hasValue = whens.All(w => ((EmbeddedEntityExpression)w.Value).HasValue == null) ? null :
                    CombineWhens(whens.Select(w => new When(w.Condition, ((EmbeddedEntityExpression)w.Value).HasValue ?? new SqlConstantExpression(true))).ToList(), typeof(bool));

                return new EmbeddedEntityExpression(returnType,
                    hasValue, 
                    groups.Select(k => new FieldBinding(k.Key, CombineWhens(k.Value, k.Key.FieldType))), null);
            }

            if (whens.Any(e => e.Value is MListExpression))
                throw new InvalidOperationException("MList on ImplementedBy are not supported yet");

            if (whens.Any(e => e.Value is TypeImplementedByAllExpression))
            {
                return new TypeImplementedByAllExpression(whens.Select(w => new When(w.Condition, ExtractTypeId(w.Value))).ToCondition(typeof(int?)));
            }

            return whens.ToCondition(returnType);
        }

        internal static Expression ExtractTypeId(Expression exp)
        {
            if (exp.NodeType == ExpressionType.Convert)
                exp = ((UnaryExpression)exp).Operand;

            if(exp is TypeImplementedByAllExpression)
                return ((TypeImplementedByAllExpression)exp).TypeColumn;

            if (exp is TypeEntityExpression)
            {
                TypeEntityExpression typeFie = (TypeEntityExpression)exp;

                return Expression.Condition(Expression.NotEqual(typeFie.ExternalId.Nullify(), NullId),
                    TypeConstant(((TypeEntityExpression)exp).TypeValue), NullId);
            }

            if (exp is TypeImplementedByExpression)
            {
                var typeIb = (TypeImplementedByExpression)exp;

                return typeIb.TypeImplementations.Reverse().Aggregate((Expression)NullId, (acum, imp) =>
                    Expression.Condition(Expression.NotEqual(imp.ExternalId.Nullify(), NullId),
                    TypeConstant(imp.Type), acum));
            }

            throw new InvalidOperationException("Impossible to extract TypeId from {0}".Formato(exp.NiceToString()));
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
            if (operand.NodeType == (ExpressionType)DbExpressionType.Entity)
            {
                EntityExpression ee = (EntityExpression)operand;
                if (b.TypeOperand.IsAssignableFrom(ee.Type)) // upcasting
                {
                    return new IsNotNullExpression(ee.ExternalId); //Usefull mainly for Shy<T>
                }
                else
                {
                    return Expression.Constant(false);
                }
            }
            if (operand.NodeType == (ExpressionType)DbExpressionType.ImplementedBy)
            {
                ImplementedByExpression ib = (ImplementedByExpression)operand;

                EntityExpression[] fies = ib.Implementations.Where(imp => b.TypeOperand.IsAssignableFrom(imp.Type)).Select(imp=>imp.Reference).ToArray();

                return fies.Select(f => (Expression)Expression.NotEqual(f.ExternalId.Nullify(), NullId)).AggregateOr();
            }
            else if (operand.NodeType == (ExpressionType)DbExpressionType.ImplementedByAll)
            {
                ImplementedByAllExpression riba = (ImplementedByAllExpression)operand;
                return SmartEqualizer.EqualNullable(riba.TypeId.TypeColumn, TypeConstant(b.TypeOperand));
            }
            return base.VisitTypeIs(b); 
        }
       
        protected override Expression VisitUnary(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.TypeAs || u.NodeType == ExpressionType.Convert)
            {
                Expression operand = Visit(u.Operand);

                var result = EntityCasting(operand, u.Type);
                if (result != null)
                    return result;
                else if (operand != u.Operand)
                    return SimplifyRedundandConverts(Expression.MakeUnary(u.NodeType, operand, u.Type, u.Method));
                else
                    return SimplifyRedundandConverts(u);
            }

            return base.VisitUnary(u); 
        }

        protected override Expression VisitLambda(LambdaExpression lambda)
        {
            return lambda; //not touch until invoke
        }

        public Expression SimplifyRedundandConverts(UnaryExpression unary)
        {
            //(int)(object)3 --> 3

            if (unary.NodeType == ExpressionType.Convert && unary.Operand.NodeType == ExpressionType.Convert &&
                unary.Type == (((UnaryExpression)unary.Operand).Operand).Type)
                return ((UnaryExpression)unary.Operand).Operand;

            return unary;
        }

        private Expression EntityCasting(Expression operand, Type uType)
        {
            if (operand == null)
                return null;

            if (operand.Type == uType)
                return operand;

            if (operand.NodeType == (ExpressionType)DbExpressionType.Entity)
            {
                EntityExpression ee = (EntityExpression)operand;

                if (uType.IsAssignableFrom(ee.Type)) // upcasting
                {
                    return new ImplementedByExpression(uType, new[] { new ImplementationColumn(operand.Type, ee) }.ToReadOnly());
                }
                else
                {
                    return new EntityExpression(uType, Expression.Constant(null, typeof(int?)), null, null);
                }               
            }
            if (operand.NodeType == (ExpressionType)DbExpressionType.ImplementedBy)
            {
                ImplementedByExpression ib = (ImplementedByExpression)operand;

                EntityExpression[] fies = ib.Implementations.Where(imp => uType.IsAssignableFrom(imp.Type)).Select(imp => imp.Reference).ToArray();

                if (fies.IsEmpty())
                {
                    return new EntityExpression(uType, Expression.Constant(null, typeof(int?)), null, null);
                }
                if (fies.Length == 1 && fies[0].Type == uType)
                    return fies[0];

                return new ImplementedByExpression(uType, fies.Select(f => new ImplementationColumn(f.Type, f)).ToReadOnly()); 
            }
            else if (operand.NodeType == (ExpressionType)DbExpressionType.ImplementedByAll)
            {
                ImplementedByAllExpression iba = (ImplementedByAllExpression)operand;

                if (uType.IsAssignableFrom(iba.Type))
                    return new ImplementedByAllExpression(uType, iba.Id, iba.TypeId);

                var conditionalId = Expression.Condition(SmartEqualizer.EqualNullable(iba.TypeId.TypeColumn, TypeConstant(uType)), iba.Id, NullId);
                return new EntityExpression(uType, conditionalId, null, null);
            }
            
            return null;
        }

        internal static ConstantExpression TypeConstant(Type type)
        {
            int id = TypeId(type);

            return Expression.Constant(id, typeof(int?));
        }

        internal static int TypeId(Type type)
        {
            return Schema.Current.TypeToId.GetOrThrow(type, "The type {0} is not registered in the database as a concrete table");
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
                    //if (left is ProjectionExpression && !((ProjectionExpression)left).IsOneCell  ||
                    //    right is ProjectionExpression && !((ProjectionExpression)right).IsOneCell)
                    //    throw new InvalidOperationException("Comparing {0} and {1} is not valid in SQL".Formato(b.Left.NiceToString(), b.Right.NiceToString())); 

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
            List<CommandExpression> commands = new List<CommandExpression>(); 

            ProjectionExpression pr = VisitCastProjection(source);

            if (pr.Projector is EntityExpression)
            {
                EntityExpression ee = (EntityExpression)pr.Projector;
                Expression id = ee.Table.GetIdExpression(Alias.Raw(ee.Table.Name));

                commands.AddRange(ee.Table.Fields.Values.Select(ef => ef.Field).OfType<FieldMList>().Select(f =>
                {
                    Expression backId = f.RelationalTable.BackColumnExpression(Alias.Raw(f.RelationalTable.Name));
                    return new DeleteExpression(f.RelationalTable, pr.Select,
                        SmartEqualizer.EqualNullable(backId, ee.ExternalId));
                }));

                commands.Add(new DeleteExpression(ee.Table, pr.Select, SmartEqualizer.EqualNullable(id, ee.ExternalId))); 
            }
            else if (pr.Projector is MListElementExpression)
            {
                MListElementExpression mlee = (MListElementExpression)pr.Projector;

                Expression id = mlee.Table.RowIdExpression(Alias.Raw(mlee.Table.Name));

                commands.Add(new DeleteExpression(mlee.Table, pr.Select, SmartEqualizer.EqualNullable(id, mlee.RowId)));
            }
            else
                throw new InvalidOperationException("Delete not supported for {0}".Formato(pr.Projector.GetType().TypeName())); 

            commands.Add(new SelectRowCountExpression()); 

            return new CommandAggregateExpression(commands);
        }

        internal CommandExpression BindUpdate(Expression source, LambdaExpression entitySelector, LambdaExpression updateConstructor)
        {
            ProjectionExpression pr = VisitCastProjection(source);

            Expression entity = pr.Projector;
            if (entitySelector == null)
                entity = pr.Projector;
            else
            {
                var cleanedSelector = (LambdaExpression)DbQueryProvider.Clean(entitySelector, false, null);
                map.Add(cleanedSelector.Parameters[0], pr.Projector);
                entity = Visit(cleanedSelector.Body);
                map.Remove(cleanedSelector.Parameters[0]);

                pr = ApplyExpansionsProjection(pr);
            }

            ITable table = entity is EntityExpression ? 
                (ITable)((EntityExpression)entity).Table : 
                (ITable)((MListElementExpression)entity).Table;

            Alias alias = Alias.Raw(table.Name);

            Expression toUpdate = table is Table ? 
                ((Table)table).GetProjectorExpression(alias, this) : 
                ((RelationalTable)table).GetProjectorExpression(alias, this);

            ParameterExpression param = updateConstructor.Parameters[0];
            ParameterExpression toUpdateParam = Expression.Parameter(toUpdate.Type, "toUpdate");

            List<ColumnAssignment> assignments = new List<ColumnAssignment>();
            map.Add(param, pr.Projector);
            map.Add(toUpdateParam, toUpdate);
            FillColumnAssigments(assignments, toUpdateParam, updateConstructor.Body);
            map.Remove(toUpdateParam);
            map.Remove(param);

            pr = ApplyExpansionsProjection(pr);

            Expression condition;

            if (entity is EntityExpression)
            {
                EntityExpression ee = (EntityExpression)entity;

                Expression id = ee.Table.GetIdExpression(Alias.Raw(ee.Table.Name));

                condition = SmartEqualizer.EqualNullable(id, ee.ExternalId);
                table = ee.Table;
            }
            else if (entity is MListElementExpression)
            {
                MListElementExpression mlee = (MListElementExpression)entity;

                Expression id = mlee.Table.RowIdExpression(Alias.Raw(mlee.Table.Name));

                condition = SmartEqualizer.EqualNullable(id, mlee.RowId);
                table = mlee.Table;
            }
            else
                throw new InvalidOperationException("Update not supported for {0}".Formato(entity.GetType().TypeName()));

            return new CommandAggregateExpression(new CommandExpression[]
            { 
                new UpdateExpression(table, pr.Select, condition, assignments),
                new SelectRowCountExpression()
            });
        }

        static readonly MethodInfo miSetReadonly = ReflectionTools.GetMethodInfo(() => Administrator.SetReadonly(null, (IdentifiableEntity a) => a.Id, 1)).GetGenericMethodDefinition();

        public void FillColumnAssigments(List<ColumnAssignment> assignments, Expression param, Expression body)
        {
            if (body is MethodCallExpression)
            {
                var mce = (MethodCallExpression)body;

                if (!mce.Method.IsInstantiationOf(miSetReadonly))
                    throw InvalidBody();

                var prev = mce.Arguments[0];
                if (prev.NodeType != ExpressionType.New)
                    FillColumnAssigments(assignments, param, prev);

                var pi = ReflectionTools.BasePropertyInfo(mce.Arguments[1].StripQuotes());

                Expression colExpression = Visit(Expression.MakeMemberAccess(param, Reflector.FindFieldInfo(body.Type, pi)));
                Expression cleaned = DbQueryProvider.Clean(mce.Arguments[2], true, null);
                Expression expression = Visit(cleaned);
                assignments.AddRange(Assign(colExpression, expression));

            }
            else if (body is MemberInitExpression)
            {
                var mie = (MemberInitExpression)body;
                assignments.AddRange(mie.Bindings.SelectMany(m => ColumnAssigments(param, m)));

            }else
            {
                throw InvalidBody();
            }
        }

        private Exception InvalidBody()
        {
            throw new InvalidOperationException("The only allowed expressions on UnsafeUpdate are: object initializers, or calling Administrator.SetReadonly");
        }

        private ColumnAssignment[] ColumnAssigments(Expression obj, MemberBinding m)
        {
            if (m is MemberAssignment)
            {
                MemberAssignment ma = (MemberAssignment)m;
                Expression colExpression = Visit(Expression.MakeMemberAccess(obj, ma.Member));
                Expression cleaned = DbQueryProvider.Clean(ma.Expression, true, null);
                Expression expression = Visit(cleaned);
                return Assign(colExpression, expression);
            }
            else if (m is MemberMemberBinding)
            {
                MemberMemberBinding mmb = (MemberMemberBinding)m;
                if(!m.Member.ReturningType().IsEmbeddedEntity())
                    throw new InvalidOperationException("{0} does not inherit from EmbeddedEntity".Formato(m.Member.ReturningType()));

                Expression obj2 = Expression.MakeMemberAccess(obj, mmb.Member);

                return mmb.Bindings.SelectMany(m2 => ColumnAssigments(obj2, m2)).ToArray();
            }
            
            throw new NotImplementedException(m.ToString()); 
        }

        ColumnAssignment AssignColumn(Expression column, Expression expression)
        {
            var col = column as ColumnExpression;

            if (col == null)
                throw new InvalidOperationException("{0} does not represent a column".Formato(column.NiceToString()));

            return new ColumnAssignment(col.Name, DbExpressionNominator.FullNominate(expression));
        }

        private ColumnAssignment[] Assign(Expression colExpression, Expression expression)
        {
            if (expression.IsNull())
                return AssignNull(colExpression);

            expression = SmartEqualizer.ConstantToEntity(expression) ?? SmartEqualizer.ConstantToLite(expression) ?? expression;

            if (expression is EntityExpression && IsNewId(((EntityExpression)expression).ExternalId))
                throw new InvalidOperationException("The entity is new");

            if (colExpression is ColumnExpression)
            {
                return new[] { AssignColumn(colExpression, expression) };
            }
            else if (colExpression.NodeType == ExpressionType.Convert && colExpression.Type.UnNullify().IsEnum && ((UnaryExpression)colExpression).Operand is ColumnExpression)
            {
                return new[] { AssignColumn(((UnaryExpression)colExpression).Operand, expression) };
            }
            else if (colExpression is LiteExpression)
            {
                Expression reference = ((LiteExpression)colExpression).Reference;
                if (expression is LiteExpression)
                    return Assign(reference, ((LiteExpression)expression).Reference);

            }
            else if (colExpression is EmbeddedEntityExpression)
            {
                EmbeddedEntityExpression eee = (EmbeddedEntityExpression)colExpression;

                EmbeddedEntityExpression eee2;
                if(expression is ConstantExpression)
                    eee2 = eee.FieldEmbedded.FromConstantExpression((ConstantExpression)expression, this);
                else if(expression is MemberInitExpression)
                    eee2 = eee.FieldEmbedded.FromMemberInitiExpression(((MemberInitExpression)expression), this);
                else throw new InvalidOperationException("Impossible to assign to {0} the expression {1}".Formato(colExpression.NiceToString(), expression.NiceToString()));

                var bindings = eee.Bindings.SelectMany(b => Assign(b.Binding,
                    eee2.Bindings.SingleEx(b2 => ReflectionTools.FieldEquals(b.FieldInfo, b2.FieldInfo)).Binding));

                if(eee.HasValue != null)
                {
                    var setValue = AssignColumn(eee.HasValue, Expression.Constant(true));
                    bindings = bindings.PreAnd(setValue);
                }

                return bindings.ToArray();
            }
            else if (colExpression is EntityExpression)
            {
                EntityExpression colFie = (EntityExpression)colExpression;
                if (expression is EntityExpression)
                    return new[] { AssignColumn(colFie.ExternalId, ((EntityExpression)expression).ExternalId) };

            }
            else if (colExpression is ImplementedByExpression)
            {
                ImplementedByExpression colIb = (ImplementedByExpression)colExpression;
                if (expression is EntityExpression)
                {
                    EntityExpression ee = (EntityExpression)expression;

                    if (!colIb.Implementations.Any(i => i.Type == ee.Type))
                        throw new InvalidOperationException("Type {0} is not in {1}".Formato(ee.Type.Name, colIb.Implementations.ToString(i => i.Type.Name, ", ")));

                    return colIb.Implementations.Select(imp => (AssignColumn(imp.Reference.ExternalId,
                       imp.Type == ee.Type ? ee.ExternalId : NullId))).ToArray();
                }
                else if (expression is ImplementedByExpression)
                {
                    ImplementedByExpression ib = (ImplementedByExpression)expression;

                    Type[] types = ib.Implementations.Select(i => i.Type).Except(colIb.Implementations.Select(i => i.Type)).ToArray();
                    if (types.Any())
                        throw new InvalidOperationException("No implementation for type(s) {0} found".Formato(types.ToString(t => t.Name, ", ")));

                    return colIb.Implementations.Select(cImp => AssignColumn(cImp.Reference.ExternalId,
                            ib.Implementations.SingleOrDefaultEx(imp => imp.Type == cImp.Type).TryCC(imp => imp.Reference.ExternalId) ?? NullId)).ToArray();
                }

            }
            else if (colExpression is ImplementedByAllExpression)
            {
                ImplementedByAllExpression colIba = (ImplementedByAllExpression)colExpression;
                if (expression is EntityExpression)
                {
                    EntityExpression ee = (EntityExpression)expression;
                    return new[]
                    {
                        AssignColumn(colIba.Id, ee.ExternalId),
                        AssignColumn(colIba.TypeId.TypeColumn, TypeConstant(ee.Type))
                    };
                }

                if (expression is ImplementedByExpression)
                {
                    ImplementedByExpression ib = (ImplementedByExpression)expression;
                    return new[]
                    {
                        AssignColumn(colIba.Id, Coalesce(typeof(int?), ib.Implementations.Select(e => e.Reference.ExternalId))),
                        AssignColumn(colIba.TypeId.TypeColumn, ib.Implementations.Select(imp => 
                            new When(imp.Reference.ExternalId.NotEqualsNulll(), TypeConstant(imp.Type))).ToList().ToCondition(typeof(int?)))
                    };
                }

                if (expression is ImplementedByAllExpression)
                {
                    ImplementedByAllExpression iba = (ImplementedByAllExpression)expression;
                    return new[]
                    {
                        AssignColumn(colIba.Id, iba.Id),
                        AssignColumn(colIba.TypeId.TypeColumn, iba.TypeId.TypeColumn)
                    };
                }
            }

            throw new NotImplementedException("{0} can not be assigned from {1}".Formato(colExpression.Type.Name, expression.Type.Name)); 
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
            else if (colExpression is LiteExpression)
            {
                Expression reference = ((LiteExpression)colExpression).Reference;
                return AssignNull(reference); 
            }
            else if (colExpression is EmbeddedEntityExpression)
            {
                EmbeddedEntityExpression eee = (EmbeddedEntityExpression)colExpression;
                if (eee.HasValue == null)
                    throw new InvalidOperationException("The EmbeddedField doesn't accept null values");

                var setNull = AssignColumn(eee.HasValue, Expression.Constant(false));

                return eee.Bindings.SelectMany(b => AssignNull(b.Binding)).PreAnd(setNull).ToArray();
            }
            else if (colExpression is EntityExpression)
            {
                EntityExpression colFie = (EntityExpression)colExpression;
                return new[] { AssignColumn(colFie.ExternalId, NullId) };
            }
            else if (colExpression is ImplementedByExpression)
            {
                ImplementedByExpression colIb = (ImplementedByExpression)colExpression;
                return colIb.Implementations.Select(imp => (AssignColumn(imp.Reference.ExternalId, NullId))).ToArray();
            }
            else if (colExpression is ImplementedByAllExpression)
            {
                ImplementedByAllExpression colIba = (ImplementedByAllExpression)colExpression;

                return new[]
                {
                    AssignColumn(colIba.Id, NullId),
                    AssignColumn(colIba.TypeId.TypeColumn, NullId)
                };
            }
            else if (colExpression is EmbeddedEntityExpression)
            {
                EmbeddedEntityExpression colEfie = (EmbeddedEntityExpression)colExpression;
                ColumnAssignment ca = AssignColumn(colEfie.HasValue, new SqlConstantExpression(true, typeof(bool)));
                return colEfie.Bindings.SelectMany(fb => AssignNull(fb.Binding)).PreAnd(ca).ToArray();
            }

            throw new NotImplementedException("{0} can not be assigned to null".Formato(colExpression.Type.Name));
        }


        #region BinderTools

        public Dictionary<ProjectionExpression, Expression> uniqueFunctionReplacements = new Dictionary<ProjectionExpression, Expression>(DbExpressionComparer.GetComparer<ProjectionExpression>());
        public Dictionary<EntityExpression, EntityExpression> entityReplacements = new Dictionary<EntityExpression, EntityExpression>(DbExpressionComparer.GetComparer<EntityExpression>());

        public List<ExpansionRequest> requests = new List<ExpansionRequest>();

        public EntityExpression Completed(EntityExpression entity)
        {
            if (entity.TableAlias != null)
                return entity;

            EntityExpression completed = entityReplacements.GetOrCreate(entity, () =>
            {
                var table = entity.Table;
                var newAlias = NextTableAlias(table.Name);
                var bindings = table.Bindings(newAlias, this);

                var result = new EntityExpression(entity.Type, entity.ExternalId, newAlias, bindings);

                requests.Add(new TableRequest
                {
                    CompleteEntity = result,
                    Table = new TableExpression(newAlias, table),
                    ExternalAliases = ExternalAliasGatherer.Externals(result.ExternalId, new HashSet<Alias>()),
                });

                return result;
            });

            return completed;
        }

        public ProjectionExpression ApplyExpansionsProjection(ProjectionExpression projection)
        {
            SourceExpression source = ApplyExpansions(projection.Select);

            if (source == projection.Select)
                return projection;

            Alias newAlias = Alias.NextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, newAlias, source.KnownAliases); //Do not replace tokens

            return new ProjectionExpression(
                    new SelectExpression(newAlias, false, false, null, pc.Columns, source, null, null, null),
                    pc.Projector, projection.UniqueFunction, projection.Type);
        }

        SourceExpression ApplyExpansions(SourceExpression source)
        {
            for (int i = 0; i < requests.Count; i++)
            {
                var r = requests[i];

                if (r.ExternalAliases.Any() && !source.KnownAliases.Intersect(r.ExternalAliases).Any())
                    continue;

                if (r is TableRequest)
                {
                    TableRequest tr = r as TableRequest;

                    var externalID = DbExpressionNominator.FullNominate(tr.CompleteEntity.ExternalId);

                    Expression equal = SmartEqualizer.EqualNullable(externalID, tr.CompleteEntity.GetBinding(EntityExpression.IdField));
                    source = new JoinExpression(JoinType.SingleRowLeftOuterJoin, source, tr.Table, equal);
                }
                else
                {
                    UniqueRequest ur = (UniqueRequest)r;

                    source = new JoinExpression(ur.OuterApply ? JoinType.OuterApply : JoinType.CrossApply, source, ur.Select, null);
                }

                requests.RemoveAt(i);
                i--;
            }
            return source;
        }

        internal static SqlConstantExpression NullId = new SqlConstantExpression(null, typeof(int?));

        public Expression MakeLite(Type type, Expression entity, Expression customToStr)
        {
            Expression id = GetId(entity);
            Expression typeId = GetEntityType(entity);
            return new LiteExpression(type, entity, id, customToStr, typeId, customToStr != null);
        }

        public Expression GetId(Expression expression)
        {
            if (expression is EntityExpression)
                return ((EntityExpression)expression).ExternalId;

            if (expression is ImplementedByExpression)
            {
                ImplementedByExpression ib = (ImplementedByExpression)expression;

                Expression aggregate = Coalesce(typeof(int?), ib.Implementations.Select(imp => imp.Reference.ExternalId));

                return aggregate;
            }
            if (expression is ImplementedByAllExpression)
                return ((ImplementedByAllExpression)expression).Id;

            if (expression.NodeType == ExpressionType.Conditional)
            {
                var con = (ConditionalExpression)expression;
                return Condition(con.Test, GetId(con.IfTrue), GetId(con.IfFalse));
            }

            if (expression.NodeType == ExpressionType.Coalesce)
            {
                var bin = (BinaryExpression)expression;

                return Condition(Expression.NotEqual(GetId(bin.Left).Nullify(), NullId), GetId(bin.Left), GetId(bin.Right));
            }

            if (expression.IsNull())
                return NullId;

            throw new NotSupportedException("Id for {0}".Formato(expression.NiceToString()));
        }

        public Expression GetEntityType(Expression expression)
        {
            if (expression is EntityExpression)
            {
                EntityExpression ee = (EntityExpression)expression;

                return new TypeEntityExpression(ee.ExternalId, ee.Type);
            }

            if (expression is ImplementedByExpression)
            {
                ImplementedByExpression ib = (ImplementedByExpression)expression;

                if (ib.Implementations.Count == 0)
                    return Expression.Constant(null, typeof(Type));

                return new TypeImplementedByExpression(ib.Implementations.Select(imp => new TypeImplementationColumnExpression(imp.Type, imp.Reference.ExternalId)).ToReadOnly());
            }

            if (expression is ImplementedByAllExpression)
            {
                return ((ImplementedByAllExpression)expression).TypeId;
            }

            if (expression.NodeType == ExpressionType.Conditional)
            {
                var con = (ConditionalExpression)expression;
                return Condition(con.Test, GetEntityType(con.IfTrue), GetEntityType(con.IfFalse));
            }

            if (expression.NodeType == ExpressionType.Coalesce)
            {
                var bin = (BinaryExpression)expression;
                return Condition(Expression.NotEqual(GetId(bin.Left).Nullify(), NullId),
                    GetEntityType(bin.Left), GetEntityType(bin.Right));
            }

            if (expression.IsNull())
                return new TypeImplementedByExpression(new List<TypeImplementationColumnExpression>().ToReadOnly());

            throw new NotSupportedException("Id for {0}".Formato(expression.NiceToString()));
        }

        private static Expression Condition(Expression test, Expression ifTrue, Expression ifFalse)
        {
            if (ifTrue.Type.IsNullable() || ifFalse.Type.IsNullable())
            {
                ifTrue = ifTrue.Nullify();
                ifFalse = ifFalse.Nullify();
            }
            return Expression.Condition(test, ifTrue, ifFalse);
        }

        public static Expression Coalesce(Type type, IEnumerable<Expression> exp)
        {
            var list = exp.ToList();

            if (list.IsEmpty())
                return Expression.Constant(null, type);

            if (list.Count() == 1)
                return list[0]; //Not regular, but usefull

            return exp.Reverse().Aggregate((ac, e) => Expression.Coalesce(e, ac));
        }

        internal ProjectionExpression MListProjection(MListExpression mle)
        {
            RelationalTable relationalTable = mle.RelationalTable;

            Alias tableAlias = NextTableAlias(mle.RelationalTable.Name);
            TableExpression tableExpression = new TableExpression(tableAlias, relationalTable);

            Expression projector = relationalTable.FieldExpression(tableAlias, this);

            Alias sourceAlias = NextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projector, sourceAlias, tableExpression.KnownAliases); // no Token

            var where = SmartEqualizer.EqualNullable(mle.BackID, relationalTable.BackColumnExpression(tableAlias));
            var proj = new ProjectionExpression(
                new SelectExpression(sourceAlias, false, false, null, pc.Columns, tableExpression, where, null, null),
                 pc.Projector, null, mle.Type);

            return proj;
        }

        internal Alias NextSelectAlias()
        {
            return Alias.NextSelectAlias();
        }

        internal Alias NextTableAlias(string tableName)
        {
            return Alias.NextTableAlias(tableName);
        }
        
        #endregion
    }




    class ExpansionRequest
    {
        public HashSet<Alias> ExternalAliases;

        public override string ToString()
        {
            return "(for {0}) ".Formato(ExternalAliases.ToString(", "));
        }
    }

    class UniqueRequest : ExpansionRequest
    {
        public bool OuterApply;
        public SelectExpression Select;

        public override string ToString()
        {
            return base.ToString() + (OuterApply ? " OUTER APPLY" : " CROSS APPLY") + " with " + Select.NiceToString();
        }
    }

    class TableRequest : ExpansionRequest
    {
        public TableExpression Table;
        public EntityExpression CompleteEntity;

        public override string ToString()
        {
            return base.ToString() + " LEFT OUTER JOIN with " + Table.NiceToString();
        }
    }
}
