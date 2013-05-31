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
using Signum.Entities.DynamicQuery;
using System.Collections.ObjectModel;
using Microsoft.SqlServer.Server;
using Signum.Engine.Basics;

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

        internal AliasGenerator aliasGenerator;

        public QueryBinder(AliasGenerator aliasGenerator)
        {
            this.aliasGenerator = aliasGenerator;
        }

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

            var result = Visit(expression);

            var expandedResult = QueryJoinExpander.ExpandJoins(result, this);

            return expandedResult;
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
                        return BindUniqueRow(m.Type, m.Method.Name.RemoveEnd(2).ToEnum<UniqueFunction>(),
                           m.GetArgument("collection"), m.TryGetArgument("predicate").StripQuotes(), m == root);
                    case "Distinct":
                        return BindDistinct(m.Type, m.GetArgument("source"));
                    case "Reverse":
                        return BindReverse(m.Type, m.GetArgument("source"));
                    case "Take":
                        return BindTake(m.Type, m.GetArgument("source"), m.GetArgument("count"));
                }
            }
            else if (m.Method.DeclaringType == typeof(EnumerableExtensions) && m.Method.Name == "ToString")
            {
                return this.BindToString(m.GetArgument("source"), m.GetArgument("separator"), m.Method);
            }
            else if (m.Method.DeclaringType == typeof(Lite) && m.Method.Name == "ToLite")
            {
                Expression toStr = Visit(m.TryGetArgument("toStr")); //could be null

                var entity = Visit(m.GetArgument("entity"));
                var converted = EntityCasting(entity, Lite.Extract(m.Type));
                return MakeLite(converted, toStr);
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

        

        private Expression MapVisitExpand(LambdaExpression lambda, ProjectionExpression projection)
        {
            return MapVisitExpand(lambda, projection.Projector, projection.Select);
        }

        private Expression MapVisitExpand(LambdaExpression lambda, Expression projector, SourceExpression source)
        {
            using (SetCurrentSource(source))
            {
                map.Add(lambda.Parameters[0], projector);
                Expression result = Visit(lambda.Body);
                map.Remove(lambda.Parameters[0]);
                return result;
            }
        }

        public ProjectionExpression WithIndex(ProjectionExpression projection, out ColumnExpression index)
        {
            var source = projection.Select;

            var orders = source.FollowC(a => a.From as SelectExpression)
                .TakeWhile(s => !s.IsDistinct && s.GroupBy.IsNullOrEmpty())
                .Select(s => s.OrderBy)
                .FirstOrDefault(o => !o.IsNullOrEmpty());

            RowNumberExpression rne = new RowNumberExpression(orders); //if its null should be filled in a later stage
            ColumnDeclaration cd = new ColumnDeclaration("_rowNum", Expression.Subtract(rne, new SqlConstantExpression(1)));

            Alias alias = NextSelectAlias();

            index = new ColumnExpression(cd.Expression.Type, alias, cd.Name);

            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias);

            return new ProjectionExpression(
                new SelectExpression(alias, false, false, null, pc.Columns.PreAnd(cd), source, null, null, null, false)
                , pc.Projector, null, projection.Type); 
        }

        private Expression MapVisitExpandWithIndex(LambdaExpression lambda, ref ProjectionExpression projection)
        {
            ColumnExpression index;
            projection = WithIndex(projection, out index);

            map.Add(lambda.Parameters[1], index);

            Expression result = MapVisitExpand(lambda, projection);

            map.Remove(lambda.Parameters[1]);

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

            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias);

            return new ProjectionExpression(
                new SelectExpression(alias, false, false, count, pc.Columns, projection.Select, null, null, null, false),
                pc.Projector, null, resultType);
        }

        //Avoid self referencing SQL problems
        bool inTableValuedFunction = false;
        public Dictionary<ProjectionExpression, Expression> uniqueFunctionReplacements = new Dictionary<ProjectionExpression, Expression>(DbExpressionComparer.GetComparer<ProjectionExpression>());
        private Expression BindUniqueRow(Type resultType, UniqueFunction function, Expression source, LambdaExpression predicate, bool isRoot)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            Expression where = predicate == null ? null : DbExpressionNominator.FullNominate(MapVisitExpand(predicate, projection));

            Alias alias = NextSelectAlias();
            Expression top = function == UniqueFunction.First || function == UniqueFunction.FirstOrDefault ? Expression.Constant(1) : null;

            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias);

            if (!isRoot && !inTableValuedFunction && pc.Projector is ColumnExpression && (function == UniqueFunction.First || function == UniqueFunction.FirstOrDefault))
                return new ScalarExpression(pc.Projector.Type,
                    new SelectExpression(alias, false, false, top, new[] { new ColumnDeclaration("val", pc.Projector) }, projection.Select, where, null, null, false));

            var newProjector = new ProjectionExpression(
                new SelectExpression(alias, false, false, top, pc.Columns, projection.Select, where, null, null, false),
                pc.Projector, function, resultType);

            if (isRoot)
                return newProjector;

            var proj = uniqueFunctionReplacements.GetOrCreate(newProjector, () =>
            {
              

                AddRequest(new UniqueRequest
                {
                    Select = newProjector.Select,
                    OuterApply = function == UniqueFunction.SingleOrDefault || function == UniqueFunction.FirstOrDefault
                });

                return newProjector.Projector;
            });

            return proj;
        }

     
        private Expression BindDistinct(Type resultType, Expression source)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            Alias alias = NextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias, selectTrivialColumns: true);
            return new ProjectionExpression(
                new SelectExpression(alias, true, false, null, pc.Columns, projection.Select, null, null, null, false),
                pc.Projector, null, resultType);
        }

        private Expression BindReverse(Type resultType, Expression source)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            Alias alias = NextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias);
            return new ProjectionExpression(
                new SelectExpression(alias, false, true, null, pc.Columns, projection.Select, null, null, null, false),
                pc.Projector, null, resultType);
        }

        private Expression BindToString(Expression source, Expression separator, MethodInfo mi)
        {
            Expression newSource = Visit(source);

            if (!(newSource is ProjectionExpression))
                return Expression.Call(mi, newSource, separator);

            ProjectionExpression projection = (ProjectionExpression)newSource;

            var projector = projection.Projector.Type == typeof(string) ? projection.Projector :
                Expression.Call(projection.Projector, OverloadingSimplifier.miToString);

            Expression nominated;
            var set = DbExpressionNominator.Nominate(projector, out nominated, isGroupKey: true);

            if(!set.Contains(nominated))
                return Expression.Call(mi, projection, separator);

            if (!(separator is ConstantExpression))
                throw new InvalidCastException("The parameter 'separator' from ToString method should be a constant");

            string value = (string)((ConstantExpression)separator).Value;

            ColumnDeclaration cd = new ColumnDeclaration(null, Expression.Add(new SqlConstantExpression(value, typeof(string)), nominated, miStringConcat));

            Alias alias = NextSelectAlias();

            SelectExpression select = new SelectExpression(alias, false, false, null, new[] { cd }, projection.Select, null, null, null, forXmlPathEmpty: true);

            return new SqlFunctionExpression(typeof(string), null, SqlFunction.STUFF.ToString(), new Expression[]
            {
                new ScalarExpression(typeof(string), select),
                new SqlConstantExpression(1), 
                new SqlConstantExpression(value.Length), 
                new SqlConstantExpression("")
            });
        }

        static MethodInfo miStringConcat = ReflectionTools.GetMethodInfo(() => string.Concat("", ""));

        private Expression BindAggregate(Type resultType, AggregateFunction aggregateFunction, Expression source, LambdaExpression selector, bool isRoot)
        {
            Type enumType = ExtractEnum(ref resultType, aggregateFunction);

            ProjectionExpression projection = this.VisitCastProjection(source);
            
            SourceExpression newSource = projection.Select;

            Expression exp = aggregateFunction == AggregateFunction.Count ? null :
                selector != null ? MapVisitExpand(selector, projection) :
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

            SelectExpression select = new SelectExpression(alias, false, false, null, new[] { cd }, projection.Select, null, null, null, false);

            if (isRoot)
                return new ProjectionExpression(select,
                   RestoreEnum(ColumnProjector.SingleProjection(cd, alias, resultType), enumType),
                   UniqueFunction.Single, enumType ?? resultType);

            ScalarExpression subquery = new ScalarExpression(resultType, select);

            GroupByInfo info = groupByMap.TryGetC(projection.Select.Alias);
            if (info != null)
            {
                Expression exp2 = aggregateFunction == AggregateFunction.Count ? null : 
                    selector != null ? MapVisitExpand(selector, info.Projector, info.Source):
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
                    case DbExpressionType.LiteReference: return SmartEqualizer.EntityIn((LiteReferenceExpression)newItem, col == null ? Enumerable.Empty<Lite<IIdentifiable>>() : col.Cast<Lite<IIdentifiable>>().ToList());
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
                var pc = ColumnProjector.ProjectColumns(projection.Projector, alias, isGroupKey: false, selectTrivialColumns: true);

                SubqueryExpression se = null;
                if (Schema.Current.Settings.IsDbType(pc.Projector.Type))
                    se = new InExpression(newItem, new SelectExpression(alias, false, false, null, pc.Columns, projection.Select, null, null, null, false));
                else
                {
                    Expression where = DbExpressionNominator.FullNominate(SmartEqualizer.PolymorphicEqual(projection.Projector, newItem));
                    se = new ExistsExpression(new SelectExpression(alias, false, false, null, pc.Columns, projection.Select, where, null, null, false));
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
            SelectExpression select = new SelectExpression(alias, false, false, null, new[] { new ColumnDeclaration("value", expr) }, null, null, null, null, false);
            return new ProjectionExpression(select, new ColumnExpression(expr.Type, alias, "value"), uniqueFunction, resultType);
        }

        private Expression BindWhere(Type resultType, Expression source, LambdaExpression predicate)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            Expression exp = predicate.Parameters.Count == 1 ? 
                MapVisitExpand(predicate, projection) :
                MapVisitExpandWithIndex(predicate, ref projection);

            if (exp.NodeType == ExpressionType.Constant && ((bool)((ConstantExpression)exp).Value))
                return projection;

            Expression where = DbExpressionNominator.FullNominate(exp);

            Alias alias = NextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias);
            return new ProjectionExpression(
                new SelectExpression(alias, false, false, null, pc.Columns, projection.Select, where, null, null, false),
                pc.Projector, null, resultType);
        }

        private Expression BindSelect(Type resultType, Expression source, LambdaExpression selector)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            Expression expression = selector.Parameters.Count == 1 ? 
                MapVisitExpand(selector, projection) :
                MapVisitExpandWithIndex(selector, ref projection);

            Alias alias = NextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(expression, alias);
            return new ProjectionExpression(
                new SelectExpression(alias, false, false, null, pc.Columns, projection.Select, null, null, null, false),
                pc.Projector, null, resultType);
        }

        protected virtual Expression BindSelectMany(Type resultType, Expression source, LambdaExpression collectionSelector, LambdaExpression resultSelector)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);
            bool outer = OverloadingSimplifier.ExtractDefaultIfEmpty(ref collectionSelector);

            JoinType joinType = IsTable(collectionSelector.Body) ? JoinType.CrossJoin :
                                outer ? JoinType.OuterApply :
                                JoinType.CrossApply;

            Expression collectionExpression = collectionSelector.Parameters.Count == 1 ?
                MapVisitExpand(collectionSelector, projection) :
                MapVisitExpandWithIndex(collectionSelector, ref projection);

            ProjectionExpression collectionProjection = AsProjection(collectionExpression);

            Alias alias = NextSelectAlias();
            if (resultSelector == null)
            {
                ProjectedColumns pc = ColumnProjector.ProjectColumns(collectionProjection.Projector, alias);

                JoinExpression join = new JoinExpression(joinType, projection.Select, collectionProjection.Select, null);

                var result = new ProjectionExpression(
                    new SelectExpression(alias, false, false, null, pc.Columns, join, null, null, null, false),
                    pc.Projector, null, resultType);

                return result;
            }
            else
            {
                JoinExpression join = new JoinExpression(joinType, projection.Select, collectionProjection.Select, null);

                Expression resultProjector;
                using (SetCurrentSource(join))
                {
                    map.SetRange(resultSelector.Parameters, new[] { projection.Projector, collectionProjection.Projector });
                    resultProjector = Visit(resultSelector.Body);
                    map.RemoveRange(resultSelector.Parameters);
                }

                ProjectedColumns pc = ColumnProjector.ProjectColumns(resultProjector, alias);

                var result = new ProjectionExpression(
                    new SelectExpression(alias, false, false, null, pc.Columns, join, null, null, null, false),
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

            Expression outerKeyExpr = MapVisitExpand(outerKey, outerProj);
            Expression innerKeyExpr = MapVisitExpand(innerKey, innerProj);

            Expression condition = DbExpressionNominator.FullNominate(SmartEqualizer.EqualNullable(outerKeyExpr, innerKeyExpr));

            JoinType jt = rightOuter && leftOuter ? JoinType.FullOuterJoin :
                          rightOuter ? JoinType.RightOuterJoin :
                          leftOuter ? JoinType.LeftOuterJoin :
                          JoinType.InnerJoin;

            Alias alias = NextSelectAlias();

            JoinExpression join = new JoinExpression(jt, outerProj.Select, innerProj.Select, condition);

            Expression resultExpr;
            using (SetCurrentSource(join))
            {
                map.SetRange(resultSelector.Parameters, new[] { outerProj.Projector, innerProj.Projector });
                resultExpr = Visit(resultSelector.Body);
                map.RemoveRange(resultSelector.Parameters);
            }

            ProjectedColumns pc = ColumnProjector.ProjectColumns(resultExpr, alias);

            ProjectionExpression result = new ProjectionExpression(
                new SelectExpression(alias, false, false, null, pc.Columns, join, null, null, null, false),
                pc.Projector, null, resultType);

            return result;
        }

        private Expression BindGroupBy(Type resultType, Expression source, LambdaExpression keySelector, LambdaExpression elementSelector)
        {
            ProjectionExpression projection = VisitCastProjection(source);
            ProjectionExpression subqueryProjection = VisitCastProjection(source); // make duplicate of source query as basis of element subquery by visiting the source again

            Alias alias = NextSelectAlias();

            Expression key = GroupEntityCleaner.Clean(MapVisitExpand(keySelector, projection));
            ProjectedColumns keyPC = ColumnProjector.ProjectColumns(key, alias, isGroupKey: true, selectTrivialColumns: true);  // Use ProjectColumns to get group-by expressions from key expression
            Expression elemExpr = elementSelector == null ? projection.Projector : MapVisitExpand(elementSelector, projection);

            Expression subqueryKey = GroupEntityCleaner.Clean(MapVisitExpand(keySelector, subqueryProjection));// recompute key columns for group expressions relative to subquery (need these for doing the correlation predicate
            ProjectedColumns subqueryKeyPC = ColumnProjector.ProjectColumns(subqueryKey, aliasGenerator.Raw("basura"), isGroupKey: true, selectTrivialColumns: true); // use same projection trick to get group-by expressions based on subquery
            Expression subqueryElemExpr = elementSelector == null ? subqueryProjection.Projector : MapVisitExpand(elementSelector, subqueryProjection); // compute element based on duplicated subquery

            Expression subqueryCorrelation = keyPC.Columns.IsEmpty() ? null : 
                keyPC.Columns.Zip(subqueryKeyPC.Columns, (c1, c2) => SmartEqualizer.EqualNullableGroupBy(new ColumnExpression(c1.Expression.Type, alias, c1.Name), c2.Expression))
                    .AggregateAnd();

            // build subquery that projects the desired element
            Alias elementAlias = NextSelectAlias();
            ProjectedColumns elementPC = ColumnProjector.ProjectColumns(subqueryElemExpr, elementAlias);
            ProjectionExpression elementSubquery =
                new ProjectionExpression(
                    new SelectExpression(elementAlias, false, false, null, elementPC.Columns, subqueryProjection.Select, subqueryCorrelation, null, null, false),
                    elementPC.Projector, null, typeof(IEnumerable<>).MakeGenericType(elementPC.Projector.Type));

            NewExpression newResult = Expression.New(typeof(Grouping<,>).MakeGenericType(key.Type, subqueryElemExpr.Type).GetConstructors()[1],
                        new Expression[] { keyPC.Projector, elementSubquery });

            Expression resultExpr = Expression.Convert(newResult
                , typeof(IGrouping<,>).MakeGenericType(key.Type, subqueryElemExpr.Type));

            this.groupByMap.Add(elementAlias, new GroupByInfo
            {
                GroupAlias = alias,
                Projector = elemExpr,
                Source = projection.Select,
            });

            var result = new ProjectionExpression(
                new SelectExpression(alias, false, false, null, keyPC.Columns, projection.Select, null, null, keyPC.Columns.Select(c => c.Expression), false),
                resultExpr, null, resultType.GetGenericTypeDefinition().MakeGenericType(resultExpr.Type));

            return result;
        }

   
        List<OrderExpression> thenBys;
        protected virtual Expression BindOrderBy(Type resultType, Expression source, LambdaExpression orderSelector, OrderType orderType)
        {
            List<OrderExpression> myThenBys = this.thenBys;
            this.thenBys = null;
            ProjectionExpression projection = this.VisitCastProjection(source);

            List<OrderExpression> orderings = new List<OrderExpression>();
            orderings.Add(new OrderExpression(orderType, GetOrderExpression(orderSelector, projection)));

            if (myThenBys != null)
            {
                for (int i = myThenBys.Count - 1; i >= 0; i--)
                {
                    OrderExpression tb = myThenBys[i];
                    orderings.Add(new OrderExpression(tb.OrderType, GetOrderExpression((LambdaExpression)tb.Expression, projection)));
                }
            }

            Alias alias = NextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias);
            return new ProjectionExpression(
                new SelectExpression(alias, false, false, null, pc.Columns, projection.Select, null, orderings.AsReadOnly(), null, false),
                pc.Projector, null, resultType);
        }

        private Expression GetOrderExpression(LambdaExpression lambda, ProjectionExpression projection)
        {
            var expr = MapVisitExpand(lambda, projection);

            if (expr is LiteReferenceExpression)
            {
                LiteReferenceExpression lite = (LiteReferenceExpression)expr;
                expr = lite.Reference is ImplementedByAllExpression ? ((ImplementedByAllExpression)lite.Reference).Id :
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
            ITable table = ((ISignumTable)query).Table;

            Alias tableAlias = NextTableAlias(table.Name);

            Expression exp = table is Table ? 
                ((Table)table).GetProjectorExpression(tableAlias, this) :
                ((RelationalTable)table).GetProjectorExpression(tableAlias, this);

            Type resultType = typeof(IQueryable<>).MakeGenericType(query.ElementType);
            TableExpression tableExpression = new TableExpression(tableAlias, table);

            Alias selectAlias = NextSelectAlias();

            ProjectedColumns pc = ColumnProjector.ProjectColumns(exp, selectAlias);

            ProjectionExpression projection = new ProjectionExpression(
                new SelectExpression(selectAlias, false, false, null, pc.Columns, tableExpression, null, null, null, false),
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

            ProjectedColumns pc = ColumnProjector.ProjectColumns(exp, selectAlias);

            ProjectionExpression projection = new ProjectionExpression(
                new SelectExpression(selectAlias, false, false, null, pc.Columns, tableExpression, null, null, null, false),
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

            if (source == null || m.Method.Name == "InSql" || m.Method.Name == "DisableQueryFilter")
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
                var ib = (ImplementedByExpression)source;
      
                if (m.Method.IsExtensionMethod())
                {
                    return DispatchIb(ib, m.Type, ee=> 
                        BindMethodCall(Expression.Call(null, m.Method, m.Arguments.Skip(1).PreAnd(ee))));
                }
                else
                {
                    return DispatchIb(ib, m.Type, ee=>
                         BindMethodCall(Expression.Call(ee, m.Method, m.Arguments)));
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
                else if (source.NodeType == (ExpressionType)DbExpressionType.LiteReference)
                {
                    LiteReferenceExpression lite = (LiteReferenceExpression)source;

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
                case (ExpressionType)DbExpressionType.LiteReference:
                {
                    LiteReferenceExpression liteRef = (LiteReferenceExpression)source;
                    PropertyInfo pi = m.Member as PropertyInfo;
                    if (pi != null)
                    {
                        if (pi.Name == "Id")
                            return GetId(liteRef.Reference).UnNullify();
                        if (pi.Name == "EntityOrNull" || pi.Name == "Entity")
                            return liteRef.Reference;
                    }

                    if (pi.Name == "EntityType")
                        return GetEntityType(liteRef.Reference);
                    
                    throw new InvalidOperationException("The member {0} of Lite is not accessible on queries".Formato(m.Member));
                }
                case (ExpressionType)DbExpressionType.ImplementedBy:
                {
                    var ib = (ImplementedByExpression)source;

                    return DispatchIb(ib, m.Member.ReturningType(), ee=>
                        BindMemberAccess(Expression.MakeMemberAccess(ee, m.Member)));
                }
                case (ExpressionType)DbExpressionType.ImplementedByAll:
                {
                    ImplementedByAllExpression iba = (ImplementedByAllExpression)source;
                    FieldInfo fi = m.Member as FieldInfo ?? Reflector.FindFieldInfo(iba.Type, (PropertyInfo)m.Member);
                    if (fi != null && fi.FieldEquals((IdentifiableEntity ie) => ie.id))
                        return iba.Id.UnNullify();

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

        public Expression DispatchIb(ImplementedByExpression ib, Type resultType, Func<EntityExpression, Expression> selector)
        {
            if (ib.Implementations.Count == 0)
                return Expression.Constant(null, resultType);

            if (ib.Implementations.Count == 1)
                return selector(ib.Implementations.Values.Single());

            return ib.Implementations.Values
                .Select(ee => new When(Expression.NotEqual(ee.ExternalId, NullId), selector(ee)))
                .ToCondition(resultType);


            UnionAllRequest ur = Completed(ib);

            var dictionary = ur.Implementations.SelectDictionary(ue => 
                {
                    using(SetCurrentSource(ue.Table))
                    {
                        return selector(ue.Entity);
                    }
                });

            var result = CombineImplementations(ur, dictionary, resultType);

            return result;
        }

        private Expression CombineImplementations(UnionAllRequest request, Dictionary<Type, Expression> implementations, Type returnType)
        {
            if (implementations.All(e => e.Value is LiteReferenceExpression))
            {
                Expression entity = CombineImplementations(request, implementations.SelectDictionary(ex => 
                    ((LiteReferenceExpression)ex).Reference), Lite.Extract(returnType));

                return MakeLite(entity, null);
            }

            if (implementations.All(e => e.Value is EntityExpression))
            {
                var avoidExpandOnRetrieving = implementations.Any(a => ((EntityExpression)a.Value).AvoidExpandOnRetrieving);

                Expression id = CombineImplementations(request, implementations.SelectDictionary(imp => ((EntityExpression)imp).ExternalId), typeof(int?));

                return new EntityExpression(returnType, id, null, null, avoidExpandOnRetrieving);
            }

            if (implementations.Any(e => e.Value is ImplementedByAllExpression))
            {
                Expression id = CombineImplementations(request, implementations.SelectDictionary(w => GetId(w)), typeof(int?));
                TypeImplementedByAllExpression typeId = (TypeImplementedByAllExpression)CombineImplementations(request, implementations.SelectDictionary(w => GetEntityType(w)), typeof(Type));

                return new ImplementedByAllExpression(returnType, id, typeId);
            }

            if(implementations.All(e=>e.Value is EntityExpression || e.Value is ImplementedByExpression))
            {
                var hs = implementations.Values.SelectMany(exp => exp is EntityExpression ?
                    new[] { ((EntityExpression)exp).Type } :
                    ((ImplementedByExpression)exp).Implementations.Keys).ToHashSet();


                var newImplementations = hs.ToDictionary(t => t, t =>
                    (EntityExpression)CombineImplementations(request, implementations.SelectDictionary(exp =>
                    {
                        if (exp is EntityExpression)
                        {
                            if (exp.Type == t)
                                return exp;
                        }
                        else
                        {
                            var result = ((ImplementedByExpression)exp).Implementations.TryGetC(t);
                            if (result != null)
                                return result;
                        }

                        return new EntityExpression(t, NullId, null, null, false);
                    }), t));

                return new ImplementedByExpression(returnType, newImplementations);    
            }

            if(implementations.All(e=>e.Value is EmbeddedEntityExpression))
            {
                var bindings = (from w in implementations
                                from b in ((EmbeddedEntityExpression)w.Value).Bindings
                                group KVP.Create(w.Key, b.Binding) by b.FieldInfo into g
                                select new FieldBinding(g.Key,  
                                    CombineImplementations(request, g.ToDictionary(), g.Key.FieldType))).ToList();

                var hasValue = implementations.All(w => ((EmbeddedEntityExpression)w.Value).HasValue == null) ? null :
                    CombineImplementations(request, implementations.SelectDictionary(w => ((EmbeddedEntityExpression)w).HasValue ?? new SqlConstantExpression(true)), typeof(bool));

                return new EmbeddedEntityExpression(returnType, hasValue, bindings, null);
            }

            if (implementations.Any(e => e.Value is MListExpression))
                throw new InvalidOperationException("MList on ImplementedBy are not supported yet");
            
            if (implementations.Any(e => e.Value is TypeImplementedByAllExpression || e.Value is TypeImplementedByExpression || e.Value is TypeEntityExpression))
            {
                var typeId = CombineImplementations(request, implementations.SelectDictionary(exp => ExtractTypeId(exp)), typeof(int?));

                return new TypeImplementedByAllExpression(typeId);
            }

            if (implementations.All(i => i.Value.NodeType == ExpressionType.Convert && i.Value.Type.UnNullify().IsEnum))
            {
                var enumType = implementations.Select(i => i.Value.Type).Distinct().Only();

                var value = CombineImplementations(request, implementations.SelectDictionary(exp => ((UnaryExpression)exp).Operand), typeof(int?));

                return Expression.Convert(value, enumType); 
            }

            if (!Schema.Current.Settings.IsDbType(returnType.UnNullify()))
                throw new InvalidOperationException("Impossible to CombineImplementations of {0}".Formato(returnType.TypeName()));

            var values = implementations.SelectDictionary(t => t, (t, exp) => GetNominableExpression(request, t, exp));

            if (values.Values.All(o => o is Expression))
                return request.AddUnionColumn(returnType, GetDefaultName((Expression)values.Values.First()), t => (Expression)values[t]);

            var whens = values.Select(kvp =>
            {
                var condition = Expression.NotEqual(request.Implementations[kvp.Key].UnionExternalId, NullId);

                if (kvp.Value is Expression)
                {
                    Expression exp = (Expression)kvp.Value;
                    var newCe = request.AddIndependentColumn(exp.Type, GetDefaultName(exp), kvp.Key, exp);
                    return new When(condition, newCe);
                }

                var dirty = (DityExpression)kvp.Value;

                var table = request.Implementations[kvp.Key].Table;

                var projector = ColumnUnionProjector.Project(dirty.projector, dirty.candidates, request, kvp.Key);

                return new When(condition, projector);

            }).ToList();
            
            return whens.ToCondition(returnType);
        }

        static string GetDefaultName(Expression expression)
        {
            if (expression is ColumnExpression)
                return ((ColumnExpression)expression).Name;

            if (expression is UnaryExpression)
                return GetDefaultName(((UnaryExpression)expression).Operand);

            return "val";
        }

        private object GetNominableExpression(UnionAllRequest request, Type type, Expression exp)
        {
            if (exp is ColumnExpression)
                return exp;

            //var knownAliases = KnownAliases(request.Implementations[type].Table);

            Expression newExp;
            var nominations = DbExpressionNominator.Nominate(exp, out newExp);

            if (nominations.Contains(newExp))
                return newExp;

            return new DityExpression { projector = newExp, candidates = nominations };

        }

        class DityExpression
        {
            public Expression projector;
            public HashSet<Expression> candidates; 
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
                    Expression.Condition(Expression.NotEqual(imp.Value.Nullify(), NullId),
                    TypeConstant(imp.Key), acum));
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
            Type type = b.TypeOperand;

            if (operand.NodeType == (ExpressionType)DbExpressionType.LiteReference)
            {
                if (!type.IsLite())
                    throw new InvalidCastException("Impossible the type {0} (non-lite) with the expression {1}".Formato(type.TypeName(), b.Expression.NiceToString()));

                operand = ((LiteReferenceExpression)(operand)).Reference;
                type = type.CleanType();
            }

            if (operand.NodeType == (ExpressionType)DbExpressionType.Entity)
            {
                EntityExpression ee = (EntityExpression)operand;
                if (type.IsAssignableFrom(ee.Type)) // upcasting
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

                EntityExpression[] fies = ib.Implementations.Where(imp => type.IsAssignableFrom(imp.Key)).Select(imp => imp.Value).ToArray();

                return fies.Select(f => (Expression)Expression.NotEqual(f.ExternalId.Nullify(), NullId)).AggregateOr();
            }
            else if (operand.NodeType == (ExpressionType)DbExpressionType.ImplementedByAll)
            {
                ImplementedByAllExpression riba = (ImplementedByAllExpression)operand;
                return SmartEqualizer.EqualNullable(riba.TypeId.TypeColumn, TypeConstant(type));
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
                    return new ImplementedByExpression(uType, new Dictionary<Type, EntityExpression> { { operand.Type, ee } }.ToReadOnly());
                }
                else
                {
                    return new EntityExpression(uType, Expression.Constant(null, typeof(int?)), null, null, ee.AvoidExpandOnRetrieving);
                }
            }
            if (operand.NodeType == (ExpressionType)DbExpressionType.ImplementedBy)
            {
                ImplementedByExpression ib = (ImplementedByExpression)operand;

                EntityExpression[] fies = ib.Implementations.Where(imp => uType.IsAssignableFrom(imp.Key)).Select(imp => imp.Value).ToArray();

                if (fies.IsEmpty())
                {
                    return new EntityExpression(uType, Expression.Constant(null, typeof(int?)), null, null, avoidExpandOnRetrieving: true);
                }
                if (fies.Length == 1 && fies[0].Type == uType)
                    return fies[0];

                return new ImplementedByExpression(uType, fies.ToDictionary(f => f.Type));
            }
            else if (operand.NodeType == (ExpressionType)DbExpressionType.ImplementedByAll)
            {
                ImplementedByAllExpression iba = (ImplementedByAllExpression)operand;

                if (uType.IsAssignableFrom(iba.Type))
                    return new ImplementedByAllExpression(uType, iba.Id, iba.TypeId);

                var conditionalId = Expression.Condition(SmartEqualizer.EqualNullable(iba.TypeId.TypeColumn, TypeConstant(uType)), iba.Id.Nullify(), NullId);
                return new EntityExpression(uType, conditionalId, null, null, avoidExpandOnRetrieving: false);
            }

            else if (operand.NodeType == (ExpressionType)DbExpressionType.LiteReference)
            {
                LiteReferenceExpression lite = (LiteReferenceExpression)operand;

                if (!uType.IsLite())
                    throw new InvalidCastException("Impossible to convert an expression of type {0} to {1}".Formato(lite.Type.TypeName(), uType.TypeName())); 

                Expression entity = EntityCasting(lite.Reference, Lite.Extract(uType));

                return MakeLite(entity, null);
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

            ProjectionExpression pr = (ProjectionExpression)QueryJoinExpander.ExpandJoins(VisitCastProjection(source), this);

            if (pr.Projector is EntityExpression)
            {
                EntityExpression ee = (EntityExpression)pr.Projector;
                Expression id = ee.Table.GetIdExpression(aliasGenerator.Table(ee.Table.Name));

                commands.AddRange(ee.Table.Fields.Values.Select(ef => ef.Field).OfType<FieldMList>().Select(f =>
                {
                    Expression backId = f.RelationalTable.BackColumnExpression(aliasGenerator.Table(f.RelationalTable.Name));
                    return new DeleteExpression(f.RelationalTable, pr.Select,
                        SmartEqualizer.EqualNullable(backId, ee.ExternalId));
                }));

                commands.Add(new DeleteExpression(ee.Table, pr.Select, SmartEqualizer.EqualNullable(id, ee.ExternalId))); 
            }
            else if (pr.Projector is MListElementExpression)
            {
                MListElementExpression mlee = (MListElementExpression)pr.Projector;

                Expression id = mlee.Table.RowIdExpression(aliasGenerator.Table(mlee.Table.Name));

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
            }

            ITable table = entity is EntityExpression ?
                (ITable)((EntityExpression)entity).Table :
                (ITable)((MListElementExpression)entity).Table;

            Alias alias = aliasGenerator.Table(table.Name);

            Expression toUpdate = table is Table ?
                ((Table)table).GetProjectorExpression(alias, this) :
                ((RelationalTable)table).GetProjectorExpression(alias, this);

            ParameterExpression param = updateConstructor.Parameters[0];
            ParameterExpression toUpdateParam = Expression.Parameter(toUpdate.Type, "toUpdate");

            List<ColumnAssignment> assignments = new List<ColumnAssignment>();
            using (SetCurrentSource(pr.Select.From))
            {
                map.Add(param, pr.Projector);
                map.Add(toUpdateParam, toUpdate);
                FillColumnAssigments(assignments, toUpdateParam, updateConstructor.Body);
                map.Remove(toUpdateParam);
                map.Remove(param);
            }

            Expression condition;

            if (entity is EntityExpression)
            {
                EntityExpression ee = (EntityExpression)entity;

                Expression id = ee.Table.GetIdExpression(aliasGenerator.Table(ee.Table.Name));

                condition = SmartEqualizer.EqualNullable(id, ee.ExternalId);
                table = ee.Table;
            }
            else if (entity is MListElementExpression)
            {
                MListElementExpression mlee = (MListElementExpression)entity;

                Expression id = mlee.Table.RowIdExpression(aliasGenerator.Table(mlee.Table.Name));

                condition = SmartEqualizer.EqualNullable(id, mlee.RowId);
                table = mlee.Table;
            }
            else
                throw new InvalidOperationException("Update not supported for {0}".Formato(entity.GetType().TypeName()));

            var result = new CommandAggregateExpression(new CommandExpression[]
            { 
                new UpdateExpression(table, pr.Select, condition, assignments),
                new SelectRowCountExpression()
            });

            return (CommandAggregateExpression)QueryJoinExpander.ExpandJoins(result, this);
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
            else if (colExpression is LiteReferenceExpression)
            {
                Expression reference = ((LiteReferenceExpression)colExpression).Reference;
                if (expression is LiteReferenceExpression)
                    return Assign(reference, ((LiteReferenceExpression)expression).Reference);

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

                    if (!colIb.Implementations.ContainsKey(ee.Type))
                        throw new InvalidOperationException("Type {0} is not in {1}".Formato(ee.Type.Name, colIb.Implementations.ToString(i => i.Key.Name, ", ")));

                    return colIb.Implementations
                        .Select(imp => AssignColumn(imp.Value.ExternalId, imp.Key == ee.Type ? ee.ExternalId : NullId))
                        .ToArray();
                }
                else if (expression is ImplementedByExpression)
                {
                    ImplementedByExpression ib = (ImplementedByExpression)expression;

                    Type[] types = ib.Implementations.Select(i => i.Key).Except(colIb.Implementations.Select(i => i.Key)).ToArray();
                    if (types.Any())
                        throw new InvalidOperationException("No implementation for type(s) {0} found".Formato(types.ToString(t => t.Name, ", ")));

                    return colIb.Implementations.Select(cImp => AssignColumn(cImp.Value.ExternalId,
                            ib.Implementations.TryGetC(cImp.Key).TryCC(imp => imp.ExternalId) ?? NullId)).ToArray();
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
                        AssignColumn(colIba.Id, Coalesce(typeof(int?), ib.Implementations.Select(e => e.Value.ExternalId))),
                        AssignColumn(colIba.TypeId.TypeColumn, ib.Implementations.Select(imp => 
                            new When(imp.Value.ExternalId.NotEqualsNulll(), TypeConstant(imp.Key))).ToList().ToCondition(typeof(int?)))
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

            throw new NotImplementedException("{0} can not be assigned from expression:\n{1}".Formato(colExpression.Type.TypeName(), expression.NiceToString())); 
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
                return colIb.Implementations.Values.Select(imp => (AssignColumn(imp.ExternalId, NullId))).ToArray();
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

        Dictionary<ImplementedByExpression, UnionAllRequest> implementedByReplacements = new Dictionary<ImplementedByExpression, UnionAllRequest>(DbExpressionComparer.GetComparer<ImplementedByExpression>());
        public UnionAllRequest Completed(ImplementedByExpression ib)
        {
            return implementedByReplacements.GetOrCreate(ib, () =>
            {
                UnionAllRequest result = new UnionAllRequest { OriginalImplementedBy = ib }; 

                result.UnionAlias = aliasGenerator.NextTableAlias("Union" + ib.Type.Name);

                result.Implementations = ib.Implementations.SelectDictionary(k => k, ee => 
                {
                    var alias = NextTableAlias(ee.Table.Name); 

                    return new UnionEntity
                    {
                        Table = new TableExpression(alias, ee.Table),
                        Entity = (EntityExpression)ee.Table.GetProjectorExpression(alias, this),
                    };
                }).ToReadOnly();
              
                List<Expression> equals = new List<Expression>(); 
                foreach (var unionEntity in result.Implementations.Values)
                {
                    ColumnExpression expression = result.AddIndependentColumn(typeof(int?), "Id_" + Reflector.CleanTypeName(unionEntity.Entity.Type),
                        unionEntity.Entity.Type, unionEntity.Entity.ExternalId);

                    unionEntity.UnionExternalId = expression; 
                }

                AddRequest(result);

                return result;
            });
        }

        ImmutableStack<SourceExpression> currentSource = ImmutableStack<SourceExpression>.Empty;

        public IDisposable SetCurrentSource(SourceExpression source)
        {
            this.currentSource = currentSource.Push(source);
            return new Disposable(() => currentSource = currentSource.Pop());
        }

        void AddRequest(ExpansionRequest req)
        {
            if (currentSource.IsEmpty)
                throw new InvalidOperationException("currentSource not set");

            var source = GetCurrentSource(req);

            requests.GetOrCreate(source).Add(req);
        }

        private SourceExpression GetCurrentSource(ExpansionRequest req)
        {
#if DEBUG
#else
             if (currentSource.Count() == 1)
                return currentSource.Peek(); 
#endif
            var external = req.ExternalAlias(this);

            return currentSource.First(s => //could be more than one on GroupBy aggregates
            {
                var knownAliases = KnownAliases(s);

                return external.All(knownAliases.Contains);
            });
        }

        HashSet<Alias> KnownAliases(SourceExpression source)
        {
            HashSet<Alias> result = new HashSet<Alias>();
            result.AddRange(source.KnownAliases);

            ExpandKnowAlias(result);

            return result;
        }

        internal void ExpandKnowAlias(HashSet<Alias> result)
        {
            var list = requests.Where(r => r.Key.KnownAliases.All(result.Contains)).SelectMany(a=>a.Value).ToList();

            foreach (ExpansionRequest req in list)
            {
                if (req is TableRequest)
                {
                    var a = ((TableRequest)req).Table.Alias;

                    if (result.Add(a))
                        ExpandKnowAlias(result);
                }
                else if (req is UniqueRequest)
                {
                    foreach (var a in ((UniqueRequest)req).Select.KnownAliases)
                    {
                        if (result.Add(a))
                            ExpandKnowAlias(result);
                    }
                }
                else
                {
                    var a = ((UnionAllRequest)req).UnionAlias;

                    if (result.Add(a))
                        ExpandKnowAlias(result);
                }
            }
        }

        internal Dictionary<SourceExpression, List<ExpansionRequest>> requests = new Dictionary<SourceExpression, List<ExpansionRequest>>();

        Dictionary<EntityExpression, EntityExpression> entityReplacements = new Dictionary<EntityExpression, EntityExpression>(DbExpressionComparer.GetComparer<EntityExpression>());
        public EntityExpression Completed(EntityExpression entity)
        {
            if (entity.TableAlias != null)
                return entity;

            EntityExpression completed = entityReplacements.GetOrCreate(entity, () =>
            {
                var table = entity.Table;
                var newAlias = NextTableAlias(table.Name);
                var bindings = table.Bindings(newAlias, this);

                var result = new EntityExpression(entity.Type, entity.ExternalId, newAlias, bindings, avoidExpandOnRetrieving: false); 

                AddRequest(new TableRequest
                {
                    CompleteEntity = result,
                    Table = new TableExpression(newAlias, table),
                });

                return result;
            });

            return completed;
        }


        
     

        internal static SqlConstantExpression NullId = new SqlConstantExpression(null, typeof(int?));

        public Expression MakeLite(Expression entity, Expression customToStr)
        {
            return new LiteReferenceExpression(Lite.Generate(entity.Type), entity, customToStr);
        }

        public Expression GetId(Expression expression)
        {
            if (expression is EntityExpression)
                return ((EntityExpression)expression).ExternalId;

            if (expression is ImplementedByExpression)
            {
                ImplementedByExpression ib = (ImplementedByExpression)expression;

                Expression aggregate = Coalesce(typeof(int?), ib.Implementations.Select(imp => imp.Value.ExternalId));

                return aggregate;
            }
            if (expression is ImplementedByAllExpression)
                return ((ImplementedByAllExpression)expression).Id;

            if (expression.NodeType == ExpressionType.Conditional)
            {
                var con = (ConditionalExpression)expression;
                return Condition(con.Test, GetId(con.IfTrue).Nullify(), GetId(con.IfFalse).Nullify());
            }

            if (expression.NodeType == ExpressionType.Coalesce)
            {
                var bin = (BinaryExpression)expression;

                return Condition(Expression.NotEqual(GetId(bin.Left).Nullify(), NullId), GetId(bin.Left).Nullify(), GetId(bin.Right).Nullify());
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

                return new TypeImplementedByExpression(ib.Implementations.SelectDictionary(k => k, v => v.ExternalId));
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
                return new TypeImplementedByExpression(new Dictionary<Type, Expression>());

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
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projector, sourceAlias); // no Token

            var where = SmartEqualizer.EqualNullable(mle.BackID, relationalTable.BackColumnExpression(tableAlias));
            var proj = new ProjectionExpression(
                new SelectExpression(sourceAlias, false, false, null, pc.Columns, tableExpression, where, null, null, false),
                 pc.Projector, null, mle.Type);

            return proj;
        }

        internal Alias NextSelectAlias()
        {
            return aliasGenerator.NextSelectAlias();
        }

        internal Alias NextTableAlias(ObjectName tableName)
        {
            return aliasGenerator.NextTableAlias(tableName.Name);
        }
        
        #endregion
    }


    abstract class ExpansionRequest
    {
        public abstract HashSet<Alias> ExternalAlias(QueryBinder binder);
    }

    class UniqueRequest : ExpansionRequest
    {
        public bool OuterApply;
        public SelectExpression Select;

        public override string ToString()
        {
            return base.ToString() + (OuterApply ? " OUTER APPLY" : " CROSS APPLY") + " with " + Select.NiceToString();
        }

        public override HashSet<Alias> ExternalAlias(QueryBinder binder)
        {
            var declared = DeclaredAliasGatherer.GatherDeclared(Select);

            binder.ExpandKnowAlias(declared);

            var used = UsedAliasGatherer.Externals(Select);

            used.ExceptWith(declared);

            return used;
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

        public override HashSet<Alias> ExternalAlias(QueryBinder binder)
        {
            return UsedAliasGatherer.Externals(CompleteEntity.ExternalId);
        }
    }

    class UnionAllRequest : ExpansionRequest
    {
        public ImplementedByExpression OriginalImplementedBy; 

        public ReadOnlyDictionary<Type, UnionEntity> Implementations;

        public Alias UnionAlias; 

        ColumnGenerator columnGenerator = new ColumnGenerator();

        Dictionary<string, Dictionary<Type, Expression>> declarations = new Dictionary<string, Dictionary<Type, Expression>>();

        public override string ToString()
        {
            return base.ToString() + " LEFT OUTER JOIN with " + Implementations.Values.ToString(i => i.Table.ToString(), " UNION ALL ");
        }

        public ColumnExpression AddUnionColumn(Type type, string suggestedName, Func<Type, Expression> getColumnExpression)
        {
            string name = suggestedName == null ? columnGenerator.GetNextColumnName() : columnGenerator.GetUniqueColumnName(suggestedName);

            declarations.Add(name, Implementations.Keys.ToDictionary(k => k, k => getColumnExpression(k)));

            columnGenerator.AddUsedName(name);

            return new ColumnExpression(type, UnionAlias, name);
        }

        public ColumnExpression AddIndependentColumn(Type type, string suggestedName, Type implementation, Expression expression)
        {
            var nullValue = type.IsValueType ? Expression.Constant(null, type.Nullify()).UnNullify(): Expression.Constant(null, type);

            return AddUnionColumn(type, suggestedName, t => t == implementation ? expression : nullValue); 
        }

        public List<ColumnDeclaration> GetDeclarations(Type type)
        {
            return declarations.Select(kvp => new ColumnDeclaration(kvp.Key, kvp.Value[type])).ToList();
        }

        public override HashSet<Alias> ExternalAlias(QueryBinder binder)
        {
            return OriginalImplementedBy.Implementations.Values.SelectMany(ee => UsedAliasGatherer.Externals(ee.ExternalId)).ToHashSet();
        }
    }

    class UnionEntity
    {
        public ColumnExpression UnionExternalId; 
        public EntityExpression Entity;
        public TableExpression Table; 
    }

    class QueryJoinExpander : DbExpressionVisitor
    {
        Dictionary<SourceExpression, List<ExpansionRequest>> requests;
        AliasGenerator aliasGenerator;

        public static Expression ExpandJoins(Expression expression, QueryBinder binder)
        {
            if (binder.requests.IsEmpty())
                return expression;

            QueryJoinExpander expander = new QueryJoinExpander
            {
                requests = binder.requests,
                aliasGenerator = binder.aliasGenerator,
            };

            var result = expander.Visit(expression);

            if (expander.requests.Any())
                throw new InvalidOperationException("There should not be any non-consumed expansion requests at this stage");

            return result;
        }

        protected override SourceExpression VisitSource(SourceExpression source)
        {
            if (source == null)
                return null;

            var reqs = requests.TryGetC(source);

            if (reqs != null)
                requests.Remove(source);

            var result = base.VisitSource(source);

            if (reqs != null)
                result = ApplyExpansions(result, reqs);

            return result;
        }

        SourceExpression ApplyExpansions(SourceExpression source, List<ExpansionRequest> expansions)
        {
            foreach (var r in expansions)
            {
                if (r is TableRequest)
                {
                    TableRequest tr = r as TableRequest;

                    var externalID = DbExpressionNominator.FullNominate(tr.CompleteEntity.ExternalId);

                    Expression equal = SmartEqualizer.EqualNullable(externalID, tr.CompleteEntity.GetBinding(EntityExpression.IdField));
                    source = new JoinExpression(JoinType.SingleRowLeftOuterJoin, source, tr.Table, equal);
                }
                else if (r is UniqueRequest)
                {
                    UniqueRequest ur = (UniqueRequest)r;

                    var newSelect = (SourceExpression)VisitSource(ur.Select);

                    source = new JoinExpression(ur.OuterApply ? JoinType.OuterApply : JoinType.CrossApply, source, newSelect, null);
                }
                else
                {
                    UnionAllRequest ur = (UnionAllRequest)r;

                    var unionAll = ur.Implementations.Select(ue =>
                    {
                        var table = (SourceExpression)VisitSource(ue.Value.Table);

                        var columns = ur.GetDeclarations(ue.Key);

                        return new SelectExpression(aliasGenerator.NextSelectAlias(), false, false, null, columns, table, null, null, null, false);
                    }).Aggregate<SourceWithAliasExpression>((a, b) => new SetOperatorExpression(SetOperator.UnionAll, a, b, ur.UnionAlias));

                    var condition = (from imp in ur.Implementations
                                     let uid = imp.Value.UnionExternalId
                                     let eid = ur.OriginalImplementedBy.Implementations[imp.Key].ExternalId
                                     select Expression.Or(
                                     SmartEqualizer.EqualNullable(uid, eid),
                                     Expression.And(new IsNullExpression(uid), new IsNullExpression(eid))))
                                    .AggregateAnd();

                    source = new JoinExpression(JoinType.SingleRowLeftOuterJoin, source, unionAll, condition);
                }
            }
            return source;
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            SourceExpression source = (SourceExpression)this.VisitSource(proj.Select);
            Expression projector = this.Visit(proj.Projector);

            if (source == proj.Select && projector == proj.Projector)
                return proj;

            if (source is SelectExpression)
                return new ProjectionExpression((SelectExpression)source, projector, proj.UniqueFunction, proj.Type);

            Alias newAlias = aliasGenerator.NextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projector, newAlias); //Do not replace tokens

            return new ProjectionExpression(
                    new SelectExpression(newAlias, false, false, null, pc.Columns, source, null, null, null, false),
                    pc.Projector, proj.UniqueFunction, proj.Type);
        }
    }
}
