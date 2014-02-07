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
            else if (m.Method.DeclaringType == typeof(LinqHints) && m.Method.Name == "OrderAlsoByKeys")
            {
                return BindOrderAlsoByKeys(m.Type, m.GetArgument("source"));
            }
            else if (m.Method.DeclaringType == typeof(EnumerableExtensions) && m.Method.Name == "ToString")
            {
                return this.BindToString(m.GetArgument("source"), m.GetArgument("separator"), m.Method);
            }
            else if (m.Method.DeclaringType == typeof(LinqHintEntities))
            {
                var expression = Visit(m.Arguments[0]) as ImplementedByExpression;

                var ib = expression as ImplementedByExpression;

                if (ib == null)
                    throw new InvalidOperationException("Method {0} is only meant to be used on {1}".Formato(m.Method.Name, typeof(ImplementedByExpression).Name));

                CombineStrategy strategy = GetStrategy(m.Method);

                return new ImplementedByExpression(ib.Type, strategy, expression.Implementations);
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


        static MethodInfo miSplitSwitch = ReflectionTools.GetMethodInfo((IdentifiableEntity e) => e.CombineSwitch()).GetGenericMethodDefinition();
        static MethodInfo miSplitUnion = ReflectionTools.GetMethodInfo((IdentifiableEntity e) => e.CombineUnion()).GetGenericMethodDefinition();
        private CombineStrategy GetStrategy(MethodInfo methodInfo)
        {
            if (methodInfo.IsInstantiationOf(miSplitSwitch))
                return CombineStrategy.Switch;

            if (methodInfo.IsInstantiationOf(miSplitUnion))
                return CombineStrategy.Union;

            throw new InvalidOperationException("Method {0} not expected".Formato(methodInfo.Name));
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

            RowNumberExpression rne = new RowNumberExpression(null); //if its null should be filled in a later stage
            ColumnDeclaration cd = new ColumnDeclaration("_rowNum", Expression.Subtract(rne, new SqlConstantExpression(1)));

            Alias alias = NextSelectAlias();

            index = new ColumnExpression(cd.Expression.Type, alias, cd.Name);

            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias);

            return new ProjectionExpression(
                new SelectExpression(alias, false, null, pc.Columns.PreAnd(cd), source, null, null, null, SelectOptions.HasIndex)
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
                new SelectExpression(alias, false, count, pc.Columns, projection.Select, null, null, null, 0),
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
                    new SelectExpression(alias, false, top, new[] { new ColumnDeclaration("val", pc.Projector) }, projection.Select, where, null, null, 0));

            var newProjector = new ProjectionExpression(
                new SelectExpression(alias, false, top, pc.Columns, projection.Select, where, null, null, 0),
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
                new SelectExpression(alias, true, null, pc.Columns, projection.Select, null, null, null, 0),
                pc.Projector, null, resultType);
        }

        private Expression BindReverse(Type resultType, Expression source)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            Alias alias = NextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias);
            return new ProjectionExpression(
                new SelectExpression(alias, false, null, pc.Columns, projection.Select, null, null, null, SelectOptions.Reverse),
                pc.Projector, null, resultType);
        }

        private Expression BindOrderAlsoByKeys(Type resultType, Expression source)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            Alias alias = NextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, alias);
            return new ProjectionExpression(
                new SelectExpression(alias, false, null, pc.Columns, projection.Select, null, null, null, SelectOptions.OrderAlsoByKeys),
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

            SelectExpression select = new SelectExpression(alias, false, null, new[] { cd }, projection.Select, null, null, null, SelectOptions.ForXmlPathEmpty);

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
    
            GroupByInfo info = groupByMap.TryGetC(projection.Select.Alias);
            if (info != null)
            {
                Expression exp2 = aggregateFunction == AggregateFunction.Count ? null : 
                    selector != null ? MapVisitExpand(selector, info.Projector, info.Source):
                    info.Projector;

                var nominated2 = DbExpressionNominator.FullNominate(exp2);

                var result = new AggregateRequestsExpression(info.GroupAlias,
                    new AggregateExpression(resultType, nominated2, aggregateFunction));

                return RestoreEnum(result, enumType);
            }

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

            SelectExpression select = new SelectExpression(alias, false, null, new[] { cd }, projection.Select, null, null, null, 0);

            if (isRoot)
                return new ProjectionExpression(select,
                   RestoreEnum(ColumnProjector.SingleProjection(cd, alias, resultType), enumType),
                   UniqueFunction.Single, enumType ?? resultType);

            ScalarExpression subquery = new ScalarExpression(resultType, select);

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
                        return SmartEqualizer.In(newItem, col == null ? new object[0] : col.Cast<object>().ToArray());
                }
            }
            else
            {
                ProjectionExpression projection = this.VisitCastProjection(source);

                Alias alias = NextSelectAlias();
                var pc = ColumnProjector.ProjectColumns(projection.Projector, alias, isGroupKey: false, selectTrivialColumns: true);

                SubqueryExpression se = null;
                if (Schema.Current.Settings.IsDbType(pc.Projector.Type))
                    se = new InExpression(newItem, new SelectExpression(alias, false, null, pc.Columns, projection.Select, null, null, null, 0));
                else
                {
                    Expression where = DbExpressionNominator.FullNominate(SmartEqualizer.PolymorphicEqual(projection.Projector, newItem));
                    se = new ExistsExpression(new SelectExpression(alias, false, null, pc.Columns, projection.Select, where, null, null, 0));
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
            SelectExpression select = new SelectExpression(alias, false, null, new[] { new ColumnDeclaration("value", expr) }, null, null, null, null, 0);
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
                new SelectExpression(alias, false, null, pc.Columns, projection.Select, where, null, null, 0),
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
                new SelectExpression(alias, false, null, pc.Columns, projection.Select, null, null, null, 0),
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
                    new SelectExpression(alias, false, null, pc.Columns, join, null, null, null, 0),
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
                    new SelectExpression(alias, false, null, pc.Columns, join, null, null, null, 0),
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
                new SelectExpression(alias, false, null, pc.Columns, join, null, null, null, 0),
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
                    new SelectExpression(elementAlias, false, null, elementPC.Columns, subqueryProjection.Select, subqueryCorrelation, null, null, 0),
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
                new SelectExpression(alias, false, null, keyPC.Columns, projection.Select, null, null, keyPC.Columns.Select(c => c.Expression), 0),
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
                new SelectExpression(alias, false, null, pc.Columns, projection.Select, null, orderings.AsReadOnly(), null, 0),
                pc.Projector, null, resultType);
        }

        private Expression GetOrderExpression(LambdaExpression lambda, ProjectionExpression projection)
        {
            using (SetCurrentSource(projection.Select))
            {
                map.Add(lambda.Parameters[0], projection.Projector);
                Expression expr = Visit(lambda.Body);
                map.Remove(lambda.Parameters[0]);

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
                new SelectExpression(selectAlias, false, null, pc.Columns, tableExpression, null, null, null, 0),
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
                new SelectExpression(selectAlias, false, null, pc.Columns, tableExpression, null, null, null, 0),
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

            if (m.Method.Name == "Mixin" && source.NodeType == (ExpressionType)DbExpressionType.Entity && m.Method.GetParameters().Length == 0)
            {
                EntityExpression ee = (EntityExpression)source;
              
                var mixinType = m.Method.GetGenericArguments().SingleEx();

                Expression result = Completed(ee)
                    .Mixins
                    .EmptyIfNull()
                    .Where(mx => mx.Type == mixinType)
                    .SingleEx(() => "{0} on {1}".Formato(mixinType.Name, source.Type.Name));

                return result;
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

                    if (result is MListExpression)
                        return MListProjection((MListExpression)result);
                    
                    return result;
                }
                case (ExpressionType)DbExpressionType.MixinInit:
                {
                    MixinEntityExpression mee = (MixinEntityExpression)source;
                    FieldInfo fi = m.Member as FieldInfo ?? Reflector.FindFieldInfo(mee.Type, (PropertyInfo)m.Member);

                    if (fi == null)
                        throw new InvalidOperationException("The member {0} of {1} is not accesible on queries".Formato(m.Member.Name, mee.Type.TypeName()));

                    Expression result = mee.GetBinding(fi);

                    if (result is MListExpression)
                        return MListProjection((MListExpression)result);

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

            if (ib.Strategy == CombineStrategy.Switch)
            {
                var dictionary = ib.Implementations.SelectDictionary(ee =>
                {
                    return selector(ee);   
                });

                var strategy = new SwitchStrategy(ib);

                var result = CombineImplementations(strategy, dictionary, resultType);

                return result;
            }
            else
            {
                UnionAllRequest ur = Completed(ib);

                var dictionary = ur.Implementations.SelectDictionary(ue =>
                {
                    using (SetCurrentSource(ue.Table))
                    {
                        return selector(ue.Entity);
                    }
                });

                var result = CombineImplementations(ur, dictionary, resultType);

                return result;
            }
        }


        internal interface ICombineStrategy
        {
            Expression CombineValues(Dictionary<Type, Expression> implementations, Type returnType);
        }


        public class SwitchStrategy : ICombineStrategy
        {
            ImplementedByExpression ImplementedBy;

            public SwitchStrategy(ImplementedByExpression implementedBy)
            {
                this.ImplementedBy = implementedBy;
            }

            public Expression CombineValues(Dictionary<Type, Expression> implementations, Type returnType)
            {
                return ImplementedBy.Implementations
                    .Select(kvp => new When(Expression.NotEqual(kvp.Value.ExternalId, NullId), implementations[kvp.Key]))
                    .ToCondition(returnType);
            }
        }

        private Expression CombineImplementations(ICombineStrategy strategy, Dictionary<Type, Expression> expressions, Type returnType)
        {
            if (expressions.All(e => e.Value is LiteReferenceExpression))
            {
                Expression entity = CombineImplementations(strategy, expressions.SelectDictionary(ex => 
                    ((LiteReferenceExpression)ex).Reference), Lite.Extract(returnType));

                return MakeLite(entity, null);
            }

            if (expressions.All(e => e.Value is EntityExpression))
            {
                var avoidExpandOnRetrieving = expressions.Any(a => ((EntityExpression)a.Value).AvoidExpandOnRetrieving);

                Expression id = CombineImplementations(strategy, expressions.SelectDictionary(imp => ((EntityExpression)imp).ExternalId), typeof(int?));

                return new EntityExpression(returnType, id, null, null, null, avoidExpandOnRetrieving);
            }

            if (expressions.Any(e => e.Value is ImplementedByAllExpression))
            {
                Expression id = CombineImplementations(strategy, expressions.SelectDictionary(w => GetId(w)), typeof(int?));
                TypeImplementedByAllExpression typeId = (TypeImplementedByAllExpression)CombineImplementations(strategy, expressions.SelectDictionary(w => GetEntityType(w)), typeof(Type));

                return new ImplementedByAllExpression(returnType, id, typeId);
            }

            if(expressions.All(e=>e.Value is EntityExpression || e.Value is ImplementedByExpression))
            {
                var hs = expressions.Values.SelectMany(exp => exp is EntityExpression ?
                    new[] { ((EntityExpression)exp).Type } :
                    ((ImplementedByExpression)exp).Implementations.Keys).ToHashSet();


                var newImplementations = hs.ToDictionary(t => t, t =>
                    (EntityExpression)CombineImplementations(strategy, expressions.SelectDictionary(exp =>
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

                        return new EntityExpression(t, NullId, null, null, null, false);
                    }), t));

                var stra = expressions.Values.OfType<ImplementedByExpression>().Select(a=>a.Strategy).Distinct().Only(); //Default Union

                return new ImplementedByExpression(returnType, stra, newImplementations);   
            }

            if(expressions.All(e=>e.Value is EmbeddedEntityExpression))
            {
                var bindings = (from w in expressions
                                from b in ((EmbeddedEntityExpression)w.Value).Bindings
                                group KVP.Create(w.Key, b.Binding) by b.FieldInfo into g
                                select new FieldBinding(g.Key,  
                                    CombineImplementations(strategy, g.ToDictionary(), g.Key.FieldType))).ToList();

                var hasValue = expressions.All(w => ((EmbeddedEntityExpression)w.Value).HasValue == null) ? null :
                    CombineImplementations(strategy, expressions.SelectDictionary(w => ((EmbeddedEntityExpression)w).HasValue ?? new SqlConstantExpression(true)), typeof(bool));

                return new EmbeddedEntityExpression(returnType, hasValue, bindings, null);
            }

            if (expressions.All(e => e.Value is MixinEntityExpression))
            {
                var bindings = (from w in expressions
                                from b in ((MixinEntityExpression)w.Value).Bindings
                                group KVP.Create(w.Key, b.Binding) by b.FieldInfo into g
                                select new FieldBinding(g.Key,
                                  CombineImplementations(strategy, g.ToDictionary(), g.Key.FieldType))).ToList();

                return new MixinEntityExpression(returnType, bindings, null);
            }

            if (expressions.Any(e => e.Value is MListExpression))
                throw new InvalidOperationException("MList on ImplementedBy are not supported yet");
            
            if (expressions.Any(e => e.Value is TypeImplementedByAllExpression || e.Value is TypeImplementedByExpression || e.Value is TypeEntityExpression))
            {
                var typeId = CombineImplementations(strategy, expressions.SelectDictionary(exp => ExtractTypeId(exp)), typeof(int?));

                return new TypeImplementedByAllExpression(typeId);
            }

            if (expressions.All(i => i.Value.NodeType == ExpressionType.Convert && i.Value.Type.UnNullify().IsEnum))
            {
                var enumType = expressions.Select(i => i.Value.Type).Distinct().Only();

                var value = CombineImplementations(strategy, expressions.SelectDictionary(exp => ((UnaryExpression)exp).Operand), typeof(int?));

                return Expression.Convert(value, enumType); 
            }

            if (!Schema.Current.Settings.IsDbType(returnType.UnNullify()))
                throw new InvalidOperationException("Impossible to CombineImplementations of {0}".Formato(returnType.TypeName()));


            return strategy.CombineValues(expressions, returnType);
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

            if (operand.Type.IsLite() != uType.IsLite())
                throw new InvalidCastException("Impossible to convert {0} to {1}".Formato(operand.Type.TypeName(), uType.TypeName())); 

            if (operand.NodeType == (ExpressionType)DbExpressionType.Entity)
            {
                EntityExpression ee = (EntityExpression)operand;

                if (uType.IsAssignableFrom(ee.Type)) // upcasting
                {
                    return new ImplementedByExpression(uType, CombineStrategy.Switch, new Dictionary<Type, EntityExpression> { { operand.Type, ee } }.ToReadOnly());
                }
                else
                {
                    return new EntityExpression(uType, Expression.Constant(null, typeof(int?)), null, null, null, ee.AvoidExpandOnRetrieving);
                }
            }
            if (operand.NodeType == (ExpressionType)DbExpressionType.ImplementedBy)
            {
                ImplementedByExpression ib = (ImplementedByExpression)operand;

                EntityExpression[] fies = ib.Implementations.Where(imp => uType.IsAssignableFrom(imp.Key)).Select(imp => imp.Value).ToArray();

                if (fies.IsEmpty())
                {
                    return new EntityExpression(uType, Expression.Constant(null, typeof(int?)), null, null, null, avoidExpandOnRetrieving: true);
                }
                if (fies.Length == 1 && fies[0].Type == uType)
                    return fies[0];

                return new ImplementedByExpression(uType, ib.Strategy, fies.ToDictionary(f => f.Type));
            }
            else if (operand.NodeType == (ExpressionType)DbExpressionType.ImplementedByAll)
            {
                ImplementedByAllExpression iba = (ImplementedByAllExpression)operand;

                if (uType.IsAssignableFrom(iba.Type))
                    return new ImplementedByAllExpression(uType, iba.Id, iba.TypeId);

                var conditionalId = Expression.Condition(SmartEqualizer.EqualNullable(iba.TypeId.TypeColumn, TypeConstant(uType)), iba.Id.Nullify(), NullId);
                return new EntityExpression(uType, conditionalId, null, null, null, avoidExpandOnRetrieving: false);
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
            return TypeLogic.TypeToId.GetOrThrow(type, "The type {0} is not registered in the database as a concrete table");
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

        internal CommandExpression BindUpdate(Expression source, LambdaExpression partSelector, IEnumerable<SetterExpressions> setterExpressions)
        {
            ProjectionExpression pr = VisitCastProjection(source);

            Expression entity = pr.Projector;
            if (partSelector == null)
                entity = pr.Projector;
            else
            {
                var cleanedSelector = (LambdaExpression)DbQueryProvider.Clean(partSelector, false, null);
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

            List<ColumnAssignment> assignments = new List<ColumnAssignment>();
            using (SetCurrentSource(pr.Select.From))
            {
                foreach (var setter in setterExpressions)
                {
                    var toUpdateParam = setter.PropertyExpression.Parameters.Single();
                    map.Add(toUpdateParam, toUpdate);
                    Expression colExpression = Visit(setter.PropertyExpression.Body);
                    map.Remove(toUpdateParam);

                    var param = setter.ValueExpression.Parameters.Single();
                    map.Add(param, pr.Projector);
                    var cleanedValue = DbQueryProvider.Clean(setter.ValueExpression.Body, false, null);
                    Expression valExpression = Visit(cleanedValue);
                    map.Remove(param);

                    assignments.AddRange(AdaptAssign(colExpression, valExpression));
                }
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

        internal CommandExpression BindInsert(Expression source, LambdaExpression constructor, ITable table)
        {
            ProjectionExpression pr = VisitCastProjection(source);

            Alias alias = aliasGenerator.Table(table.Name);

            Expression toInsert = table is Table ?
                ((Table)table).GetProjectorExpression(alias, this) :
                ((RelationalTable)table).GetProjectorExpression(alias, this);

            ParameterExpression param = constructor.Parameters[0];
            ParameterExpression toInsertParam = Expression.Parameter(toInsert.Type, "toInsert");

            List<ColumnAssignment> assignments = new List<ColumnAssignment>();
            using (SetCurrentSource(pr.Select.From))
            {
                map.Add(param, pr.Projector);
                map.Add(toInsertParam, toInsert);
                var cleanedConstructor = DbQueryProvider.Clean(constructor.Body, false, null);
                FillColumnAssigments(assignments, toInsertParam, cleanedConstructor);
                map.Remove(toInsertParam);
                map.Remove(param);
            }

            var result = new CommandAggregateExpression(new CommandExpression[]
            { 
                new InsertSelectExpression(table, pr.Select, assignments),
                new SelectRowCountExpression()
            });

            return (CommandAggregateExpression)QueryJoinExpander.ExpandJoins(result, this);
        }

        static readonly MethodInfo miSetReadonly = ReflectionTools.GetMethodInfo(() => Administrator.SetReadonly(null, (IdentifiableEntity a) => a.Id, 1)).GetGenericMethodDefinition();
        static readonly MethodInfo miSetMixin = ReflectionTools.GetMethodInfo(() => ((IdentifiableEntity)null).SetMixin((CorruptMixin m) => m.Corrupt, true)).GetGenericMethodDefinition();
    
        public void FillColumnAssigments(List<ColumnAssignment> assignments, ParameterExpression toInsert, Expression body)
        {
            if (body is MethodCallExpression)
            {
                var mce = (MethodCallExpression)body;

                var prev = mce.Arguments[0];
                FillColumnAssigments(assignments, toInsert, prev);

                if (mce.Method.IsInstantiationOf(miSetReadonly))
                {
                    var pi = ReflectionTools.BasePropertyInfo(mce.Arguments[1].StripQuotes());

                    Expression colExpression = Visit(Expression.MakeMemberAccess(toInsert, Reflector.FindFieldInfo(body.Type, pi)));
                    Expression cleaned = DbQueryProvider.Clean(mce.Arguments[2], true, null);
                    Expression expression = Visit(cleaned);
                    assignments.AddRange(AdaptAssign(colExpression, expression));
                }
                else if (mce.Method.IsInstantiationOf(miSetMixin))
                {
                    Type mixinType = mce.Method.GetGenericArguments()[1];

                    var mi = ReflectionTools.BaseMemberInfo(mce.Arguments[1].StripQuotes());

                    Expression mixin = Expression.Call(toInsert, MixinDeclarations.miMixin.MakeGenericMethod(mixinType));

                    Expression cleaned = DbQueryProvider.Clean(mce.Arguments[2], true, null);
                    Expression expression = Visit(cleaned);

                    Expression colExpression = Visit(Expression.MakeMemberAccess(mixin, mi));
                    assignments.AddRange(AdaptAssign(colExpression, expression));
                }
                else
                    throw InvalidBody();
            }
            else if (body is MemberInitExpression)
            {
                var mie = (MemberInitExpression)body;
                assignments.AddRange(mie.Bindings.SelectMany(m =>
                {
                    MemberAssignment ma = (MemberAssignment)m;
                    Expression colExpression = Visit(Expression.MakeMemberAccess(toInsert, ma.Member));
                    Expression cleaned = DbQueryProvider.Clean(ma.Expression, true, null);
                    Expression expression = Visit(cleaned);
                    return AdaptAssign(colExpression, expression);
                }));
            }
            else if (body is NewExpression)
            {
                return;
            }
            else
            {
                throw InvalidBody();
            }
        }

        private Exception InvalidBody()
        {
            throw new InvalidOperationException("The only allowed expressions on UnsafeUpdate are: object initializers, calling method SetMixin, or or calling Administrator.SetReadonly");
        }

        private ColumnAssignment[] AdaptAssign(Expression colExpression, Expression exp)
        {
            var adaped = AssignAdapterExpander.Adapt(exp, colExpression);

            return Assign(colExpression, adaped);
        }
     
        private ColumnAssignment[] Assign(Expression colExpression, Expression expression)
        {
            if (colExpression is ColumnExpression)
            {
                return new[] { AssignColumn(colExpression, expression) };
            }
            else if (colExpression.NodeType == ExpressionType.Convert && colExpression.Type.UnNullify().IsEnum && ((UnaryExpression)colExpression).Operand is ColumnExpression)
            {
                return new[] { AssignColumn(((UnaryExpression)colExpression).Operand, expression) };
            }
            else if (colExpression is LiteReferenceExpression && colExpression is LiteReferenceExpression)
            {
                return Assign(
                    ((LiteReferenceExpression)colExpression).Reference, 
                    ((LiteReferenceExpression)expression).Reference);
            }
            else if (colExpression is EmbeddedEntityExpression && expression is EmbeddedEntityExpression)
            {
                EmbeddedEntityExpression cEmb = (EmbeddedEntityExpression)colExpression;
                EmbeddedEntityExpression expEmb = (EmbeddedEntityExpression)expression;

                var bindings = cEmb.Bindings.SelectMany(b => Assign(b.Binding, expEmb.GetBinding(b.FieldInfo)));

                if (cEmb.HasValue != null)
                {
                    var setValue = AssignColumn(cEmb.HasValue, expEmb.HasValue);
                    bindings = bindings.PreAnd(setValue);
                }

                return bindings.ToArray();
            }
            else if (colExpression is EntityExpression && expression is EntityExpression)
            {
                return new[] { AssignColumn(
                        ((EntityExpression)colExpression).ExternalId, 
                        ((EntityExpression)expression).ExternalId) };

            }
            else if (colExpression is ImplementedByExpression && expression is ImplementedByExpression)
            {
                ImplementedByExpression colIb = (ImplementedByExpression)colExpression;
                ImplementedByExpression expIb = (ImplementedByExpression)expression;

                return colIb.Implementations.Select(cImp => AssignColumn(cImp.Value.ExternalId,
                        expIb.Implementations.GetOrThrow(cImp.Key).ExternalId)).ToArray();
            }
            else if (colExpression is ImplementedByAllExpression && expression is ImplementedByAllExpression)
            {
                ImplementedByAllExpression colIba = (ImplementedByAllExpression)colExpression;
                ImplementedByAllExpression expIba = (ImplementedByAllExpression)expression;

                return new[]
                {
                    AssignColumn(colIba.Id, expIba.Id),
                    AssignColumn(colIba.TypeId.TypeColumn, expIba.TypeId.TypeColumn)
                };
            }

            throw new NotImplementedException("{0} can not be assigned from expression:\n{1}".Formato(colExpression.Type.TypeName(), expression.NiceToString())); 
        }


        ColumnAssignment AssignColumn(Expression column, Expression expression)
        {
            var col = column as ColumnExpression;

            if (col == null)
                throw new InvalidOperationException("{0} does not represent a column".Formato(column.NiceToString()));

            return new ColumnAssignment(col.Name, DbExpressionNominator.FullNominate(expression));
        }
        #region BinderTools

        Dictionary<ImplementedByExpression, UnionAllRequest> implementedByReplacements = new Dictionary<ImplementedByExpression, UnionAllRequest>(DbExpressionComparer.GetComparer<ImplementedByExpression>());
        public UnionAllRequest Completed(ImplementedByExpression ib)
        {
            return implementedByReplacements.GetOrCreate(ib, () =>
            {
                UnionAllRequest result = new UnionAllRequest(ib); 

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
                if (external.IsEmpty())
                    return true;

                var knownAliases = KnownAliases(s);

                return external.Intersect(knownAliases).Any();
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
                var id = table.GetIdExpression(newAlias);
                var bindings = table.GenerateBindings(newAlias, this, id);
                var mixins = table.GenerateMixins(newAlias, this, id);

                var result = new EntityExpression(entity.Type, entity.ExternalId, newAlias, bindings, mixins, avoidExpandOnRetrieving: false); 

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
                new SelectExpression(sourceAlias, false, null, pc.Columns, tableExpression, where, null, null, 0),
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

        public bool Consumed { get; set; }
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

    class UnionAllRequest : ExpansionRequest, QueryBinder.ICombineStrategy
    {
        public ImplementedByExpression OriginalImplementedBy; 

        public ReadOnlyDictionary<Type, UnionEntity> Implementations;

        public Alias UnionAlias; 

        ColumnGenerator columnGenerator = new ColumnGenerator();

        Dictionary<string, Dictionary<Type, Expression>> declarations = new Dictionary<string, Dictionary<Type, Expression>>();

        public UnionAllRequest(ImplementedByExpression ib)
        {
            this.OriginalImplementedBy = ib;
        }

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

        public Expression CombineValues(Dictionary<Type, Expression> implementations, Type returnType)
        {
            var values = implementations.SelectDictionary(t => t, (t, exp) => GetNominableExpression(t, exp));

            if (values.Values.All(o => o is Expression))
                return AddUnionColumn(returnType, GetDefaultName((Expression)values.Values.First()), t => (Expression)values[t]);

            var whens = values.Select(kvp =>
            {
                var condition = Expression.NotEqual(Implementations[kvp.Key].UnionExternalId, QueryBinder.NullId);

                if (kvp.Value is Expression)
                {
                    Expression exp = (Expression)kvp.Value;
                    var newCe = AddIndependentColumn(exp.Type, GetDefaultName(exp), kvp.Key, exp);
                    return new When(condition, newCe);
                }

                var dirty = (DityExpression)kvp.Value;

                var table = Implementations[kvp.Key].Table;

                var projector = ColumnUnionProjector.Project(dirty.projector, dirty.candidates, this, kvp.Key);

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

        object GetNominableExpression(Type type, Expression exp)
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

            //Sometimes group bys elements produce non consumed expansiosn that will be discarded
            //var nonConsumed = binder.requests.SelectMany(r=>r.Value).Where(a => a.Consumed == false).ToList();

            //if (nonConsumed.Any())
            //    throw new InvalidOperationException("All the expansiosn should be consumed at this stage");


            binder.requests.Clear(); 


            return result;
        }

        protected override SourceExpression VisitSource(SourceExpression source)
        {
            if (source == null)
                return null;

            var reqs = requests.TryGetC(source);

            //if (reqs != null)
            //    requests.Remove(source);

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

                        return new SelectExpression(aliasGenerator.NextSelectAlias(), false, null, columns, table, null, null, null, 0);
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

                r.Consumed = true;
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
                    new SelectExpression(newAlias, false, null, pc.Columns, source, null, null, null, 0),
                    pc.Projector, proj.UniqueFunction, proj.Type);
        }
    }


    class AssignAdapterExpander : DbExpressionVisitor
    {
        Expression colExpression; 

        public static Expression Adapt(Expression exp, Expression colExpression)
        {
            return new AssignAdapterExpander { colExpression = colExpression }.Visit(exp);
        }

        protected override Expression VisitConditional(ConditionalExpression c)
        {
            var test = c.Test;
            var ifTrue = Visit(c.IfTrue);
            var ifFalse = Visit(c.IfFalse);

            if (colExpression is LiteReferenceExpression)
            {
                return Combiner<LiteReferenceExpression>(ifTrue, ifFalse, (col, l, r) =>
                {
                    using (this.OverrideColExpression(col.Reference))
                    {
                        var entity = CombineConditional(test, l.Reference, r.Reference);
                        return new LiteReferenceExpression(Lite.Generate(entity.Type), entity, null);
                    }
                });
            }

            return CombineConditional(test, ifTrue, ifFalse) ?? c;
        }

        private Expression CombineConditional(Expression test, Expression ifTrue, Expression ifFalse)
        {
            if (colExpression is EntityExpression)
                return Combiner<EntityExpression>(ifTrue, ifFalse, (col, t, f) =>
                    new EntityExpression(col.Type, Expression.Condition(test, t.ExternalId.Nullify(), f.ExternalId.Nullify()), null, null, null, false));

            if (colExpression is ImplementedByExpression)
                return Combiner<ImplementedByExpression>(ifTrue, ifFalse, (col, t, f) =>
                    new ImplementedByExpression(col.Type,
                        col.Strategy,
                        col.Implementations.ToDictionary(a => a.Key, a => new EntityExpression(a.Key,
                            Expression.Condition(test,
                            t.Implementations[a.Key].ExternalId.Nullify(),
                            f.Implementations[a.Key].ExternalId.Nullify()), null, null, null, false))));

            if (colExpression is ImplementedByAllExpression)
                return Combiner<ImplementedByAllExpression>(ifTrue, ifFalse, (col, t, f) =>
                    new ImplementedByAllExpression(col.Type,
                        Expression.Condition(test, t.Id.Nullify(), f.Id.Nullify()),
                        new TypeImplementedByAllExpression(Expression.Condition(test, t.TypeId.TypeColumn.Nullify(), f.TypeId.TypeColumn.Nullify()))));

            if (colExpression is EmbeddedEntityExpression)
                return Combiner<EmbeddedEntityExpression>(ifTrue, ifFalse, (col, t, f) =>
                   new EmbeddedEntityExpression(col.Type,
                       col.HasValue == null ? null : Expression.Condition(test, t.HasValue, f.HasValue),
                       col.Bindings.Select(bin => GetBinding(bin.FieldInfo, Expression.Condition(test, t.GetBinding(bin.FieldInfo).Nullify(), f.GetBinding(bin.FieldInfo).Nullify()), bin.Binding)),
                       col.FieldEmbedded));

            return null;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            if (b.NodeType == ExpressionType.Coalesce)
            {
                var left = Visit(b.Left);
                var right = Visit(b.Right);

                if (colExpression is LiteReferenceExpression)
                {
                    return Combiner<LiteReferenceExpression>(left, right, (col, l, r) =>
                    {
                        using (this.OverrideColExpression(col.Reference))
                        {
                            var entity =  CombineCoalesce(l.Reference, r.Reference);
                            return new LiteReferenceExpression(Lite.Generate(entity.Type), entity, null);
                        }
                    }); 
                }

                return CombineCoalesce(left, right) ?? b;
            }

            return b;
        }

        private Expression CombineCoalesce(Expression left, Expression right)
        {
            if (colExpression is EntityExpression)
                return Combiner<EntityExpression>(left, right, (col, l, r) =>
                    new EntityExpression(col.Type, Expression.Coalesce(l.ExternalId.Nullify(), r.ExternalId.Nullify()), null, null, null, false));

            if (colExpression is ImplementedByExpression)
                return Combiner<ImplementedByExpression>(left, right, (col, l, r) =>
                    new ImplementedByExpression(col.Type,
                        col.Strategy,
                        col.Implementations.ToDictionary(a => a.Key, a => new EntityExpression(col.Type,
                            Expression.Coalesce(
                            l.Implementations[a.Key].ExternalId.Nullify(),
                            r.Implementations[a.Key].ExternalId.Nullify()), null, null, null, false))));

            if (colExpression is ImplementedByAllExpression)
                return Combiner<ImplementedByAllExpression>(left, right, (col, l, r) =>
                    new ImplementedByAllExpression(col.Type,
                        Expression.Coalesce(l.Id, r.Id),
                        new TypeImplementedByAllExpression(Expression.Coalesce(l.TypeId.TypeColumn.Nullify(), r.TypeId.TypeColumn.Nullify()))));

            if (colExpression is EmbeddedEntityExpression)
                return Combiner<EmbeddedEntityExpression>(left, right, (col, l, r) =>
                   new EmbeddedEntityExpression(col.Type,
                       col.HasValue == null ? null : Expression.Or(l.HasValue, r.HasValue),
                       col.Bindings.Select(bin => GetBinding(bin.FieldInfo, Expression.Coalesce(l.GetBinding(bin.FieldInfo).Nullify(), r.GetBinding(bin.FieldInfo).Nullify()), bin.Binding)),
                       col.FieldEmbedded));

            return null;
        }


        public T Combiner<T>(Expression e1, Expression e2, Func<T, T, T, T> combiner) where T : Expression
        {
            Debug.Assert(e1.Type == colExpression.Type);
            Debug.Assert(e2.Type == colExpression.Type);

            var result = combiner((T)colExpression, (T)e1, (T)e2);

            Debug.Assert(result.Type == colExpression.Type);

            return result;
        }

        protected override Expression VisitEntity(EntityExpression ee)
        {
            if (colExpression is ImplementedByAllExpression)
                return new ImplementedByAllExpression(colExpression.Type,
                    ee.ExternalId,
                    new TypeImplementedByAllExpression(Expression.Condition(Expression.Equal(ee.ExternalId.Nullify(), nullId), nullId, QueryBinder.TypeConstant(ee.Type))));

            if (colExpression is ImplementedByExpression)
            {
                var ib = ((ImplementedByExpression)colExpression);

                return new ImplementedByExpression(colExpression.Type, ib.Strategy, ib.Implementations.ToDictionary(kvp => kvp.Key, kvp =>
                    kvp.Key == ee.Type ? ee :
                      new EntityExpression(kvp.Key, nullId, null, null, null, false)));
            }

            return ee;
        }

        protected override Expression VisitImplementedBy(ImplementedByExpression ib)
        {
            if (colExpression is ImplementedByAllExpression)
            {
                return new ImplementedByAllExpression(colExpression.Type,
                    QueryBinder.Coalesce(typeof(int?), ib.Implementations.Select(e => e.Value.ExternalId)), new TypeImplementedByAllExpression(
                     ib.Implementations.Select(imp => new When(imp.Value.ExternalId.NotEqualsNulll(), QueryBinder.TypeConstant(imp.Key))).ToList().ToCondition(typeof(int?))));
            }

            if (colExpression is ImplementedByExpression)
            {
                var colId = ((ImplementedByExpression)colExpression);

                if (colId.Implementations.Keys.SequenceEqual(ib.Implementations.Keys))
                    return ib;

                return new ImplementedByExpression(colId.Type, ib.Strategy, colId.Implementations.ToDictionary(kvp => kvp.Key, kvp =>
                    ib.Implementations.TryGetC(kvp.Key) ?? new EntityExpression(kvp.Key, nullId, null, null, null, false)));
            }

            return ib;
        }

        static Expression nullId = QueryBinder.NullId;

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (colExpression is EntityExpression ||
                colExpression is ImplementedByExpression ||
                colExpression is ImplementedByAllExpression)
            {
                var ident = (IdentifiableEntity)c.Value;

                return GetEntityConstant(ident == null ? nullId : Expression.Constant(ident.Id), ident == null ? null : ident.GetType());
            }

            if (colExpression is EmbeddedEntityExpression)
                return EmbeddedFromConstant(c);

            if (colExpression is LiteReferenceExpression)
            {
                var colLite =  ((LiteReferenceExpression)colExpression);
                var lite = (Lite<IIdentifiable>)c.Value;

                using(OverrideColExpression(colLite.Reference))
                {
                    var entity = GetEntityConstant(lite == null ? nullId : Expression.Constant(lite.Id), lite == null ? null : lite.GetType().CleanType());
                    return new LiteReferenceExpression(colLite.Type, entity, null);
                }
            }

            return c;
        }

        private Expression GetEntityConstant(Expression id, Type type)
        {
            if (colExpression is EntityExpression)
            {
                if (id != nullId && type != colExpression.Type)
                    throw new InvalidOperationException("Impossible to convert {0} to {1}".Formato(type.TypeName(), colExpression.Type.TypeName()));

                return new EntityExpression(colExpression.Type, id, null, null, null, false);
            }

            if (colExpression is ImplementedByAllExpression)
                return new ImplementedByAllExpression(colExpression.Type,
                    id,
                    new TypeImplementedByAllExpression(id == nullId ? nullId : QueryBinder.TypeConstant(type)));

            if (colExpression is ImplementedByExpression)
            {
                var ib = ((ImplementedByExpression)colExpression);

                return new ImplementedByExpression(colExpression.Type, ib.Strategy, ib.Implementations.ToDictionary(kvp => kvp.Key, kvp =>
                      new EntityExpression(kvp.Key, type != kvp.Key ? nullId : id, null, null, null, false)));
            }

            throw new InvalidOperationException("colExpression is not an entity");
        }

        internal EmbeddedEntityExpression EmbeddedFromConstant(ConstantExpression contant)
        {
            var value = contant.Value;

            var embedded = (EmbeddedEntityExpression)colExpression;

            var bindings = (from kvp in embedded.FieldEmbedded.EmbeddedFields
                            let fi = kvp.Value.FieldInfo
                            let bind = embedded.GetBinding(fi)
                            select GetBinding(fi, value == null ?
                            Expression.Constant(null, fi.FieldType.Nullify()) :
                            Expression.Constant(kvp.Value.Getter(value), fi.FieldType), bind)).ToReadOnly();

            return new EmbeddedEntityExpression(contant.Type, Expression.Constant(value != null), bindings, embedded.FieldEmbedded);
        }

        internal FieldBinding GetBinding(FieldInfo fi, Expression value, Expression binding)
        {
            using (OverrideColExpression(binding))
                return new FieldBinding(fi, Visit(value), allowForcedNull: true);
        }

        protected override Expression VisitMemberInit(MemberInitExpression init)
        {
            var dic = init.Bindings.OfType<MemberAssignment>().ToDictionary(a => (a.Member as FieldInfo ?? Reflector.FindFieldInfo(init.Type, (PropertyInfo)a.Member)).Name, a => a.Expression);

            var embedded = (EmbeddedEntityExpression)colExpression;

            var bindings = (from kvp in embedded.FieldEmbedded.EmbeddedFields
                            let fi = kvp.Value.FieldInfo
                            select new FieldBinding(fi,
                                !(fi.FieldType.IsByRef || fi.FieldType.IsNullable()) ? dic.GetOrThrow(fi.Name, "No value defined for non-nullable field {0}") :
                                (dic.TryGetC(fi.Name) ?? Expression.Constant(null, fi.FieldType)))
                            ).ToReadOnly();

            return new EmbeddedEntityExpression(init.Type, Expression.Constant(true), bindings, embedded.FieldEmbedded);
        }

        IDisposable OverrideColExpression(Expression newColExpression)
        {
            var save = this.colExpression;
            this.colExpression = newColExpression;

            return new Disposable(() => this.colExpression = save);
        }
    }
}
