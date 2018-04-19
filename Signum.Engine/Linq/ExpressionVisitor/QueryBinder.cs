using Microsoft.SqlServer.Server;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Signum.Engine.Linq
{
    /// <summary>
    /// QueryBinder is a visitor that converts method calls to LINQ operations into 
    /// custom DbExpression nodes and references to class members into references to columns
    /// </summary>
    internal class QueryBinder : ExpressionVisitor
    {
        Dictionary<ParameterExpression, Expression> map = new Dictionary<ParameterExpression, Expression>();
        Dictionary<Alias, GroupByInfo> groupByMap = new Dictionary<Alias, GroupByInfo>();

        internal AliasGenerator aliasGenerator;

        internal SystemTime systemTime;

        public QueryBinder(AliasGenerator aliasGenerator)
        {
            this.systemTime = SystemTime.Current;
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

            var expandedResult = QueryJoinExpander.ExpandJoins(result, this, cleanRequests: true);

            return expandedResult;
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Queryable) ||
                m.Method.DeclaringType == typeof(Enumerable) ||
                m.Method.DeclaringType == typeof(EnumerableUniqueExtensions) ||
                m.Method.DeclaringType == typeof(StandartDeviationExtensions) ||
                m.Method.DeclaringType == typeof(StandartDeviationPopulationExtensions))
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
                        return this.BindAggregate(m.Type, m.Method.Name.ToEnum<AggregateSqlFunction>(),
                          m.GetArgument("source"), m.TryGetArgument("predicate").StripQuotes(), m == root);
                    case "Sum":
                    case "Min":
                    case "Max":
                    case "Average":
                    case "StdDev":
                    case "StdDevP":
                        return this.BindAggregate(m.Type, m.Method.Name.ToEnum<AggregateSqlFunction>(),
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
            else if (m.Method.DeclaringType == typeof(LinqHintsExpand))
            {
                if(m.Method.Name == nameof(LinqHintsExpand.ExpandLite))
                    return BindExpandLite(m.Type, m.GetArgument("source"), m.GetArgument("liteSelector").StripQuotes(), (ExpandLite)((ConstantExpression)m.GetArgument("expandLite")).Value);

                if (m.Method.Name == nameof(LinqHintsExpand.ExpandEntity))
                    return BindExpandEntity(m.Type, m.GetArgument("source"), m.GetArgument("entitySelector").StripQuotes(), (ExpandEntity)((ConstantExpression)m.GetArgument("expandEntity")).Value);
            }
            else if (m.Method.DeclaringType == typeof(LinqHints))
            {
                if (m.Method.Name == "OrderAlsoByKeys")
                    return BindOrderAlsoByKeys(m.Type, m.GetArgument("source"));

                if (m.Method.Name == "WithHint")
                    return BindWithHints(m.GetArgument("source"), (ConstantExpression)m.GetArgument("hint"));
            }
            else if (m.Method.DeclaringType == typeof(Database) && (m.Method.Name == "Retrieve" || m.Method.Name == "RetrieveAndForget"))
            {
                throw new InvalidOperationException("{0} is not supported on queries. Consider using Lite<T>.Entity instead.".FormatWith(m.Method.MethodName()));
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
                    throw new InvalidOperationException("Method {0} is only meant to be used on {1}".FormatWith(m.Method.Name, typeof(ImplementedByExpression).Name));

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

                return Expression.Convert((ColumnExpression)fi.ExternalId.Value, m.Method.DeclaringType.GetGenericArguments()[0]);
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



        private Expression BindExpandEntity(Type type, Expression source, LambdaExpression entitySelector, ExpandEntity expandEntity)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            var members = Reflector.GetMemberListUntyped(entitySelector);

            var newProjector = ChangeProjector(0, members, projection.Projector, 
                e =>
                {
                    if (e is EntityExpression ee)
                        return ee.WithExpandEntity(expandEntity);

                    if (e is ImplementedByExpression ib)
                        return new ImplementedByExpression(ib.Type, ib.Strategy, ib.Implementations.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.WithExpandEntity(expandEntity)));

                    throw new NotImplementedException("Expand Entity not supported for " + e.GetType());
                });

            return new ProjectionExpression(projection.Select, newProjector, projection.UniqueFunction, projection.Type);
        }

        private Expression BindExpandLite(Type type, Expression source, LambdaExpression entitySelector, ExpandLite expandLite)
        {
            ProjectionExpression projection = this.VisitCastProjection(source);

            var members = Reflector.GetMemberListUntyped(entitySelector);

            var newProjector = ChangeProjector(0, members,  projection.Projector,
                e => ((LiteReferenceExpression)e).WithExpandLite(expandLite));

            return new ProjectionExpression(projection.Select, newProjector, projection.UniqueFunction, projection.Type);
        }

        

        private Expression ChangeProjector(int index, MemberInfo[] members, Expression projector, Func<Expression, Expression> changeExpression)
        {
            if (members.Length == index)
                return changeExpression(projector);
        
            var m = members[index];

            if (m is Type t)
            {
                if (projector is EntityExpression ee)
                {
                    return ChangeProjector(index + 1, members, ee, changeExpression);
                }
                else if (projector is LiteReferenceExpression lr)
                {
                    return ChangeProjector(index + 1, members, lr, changeExpression);
                }
                else if (projector is ImplementedByExpression ib)
                {
                    var imp = ib.Implementations.GetOrThrow(t);

                    var newImp = (EntityExpression)ChangeProjector(index + 1, members, imp, changeExpression);

                    var newImplementations = ib.Implementations.Select(kvp => kvp.Key == t ? KVP.Create(kvp.GetType(), newImp) : kvp).ToDictionary();

                    return new ImplementedByExpression(ib.Type, ib.Strategy, newImplementations);
                }
            }
            else
            {
                if (projector is EntityExpression ee)
                {
                    ee = Completed(ee);


                    var fi = m as FieldInfo ?? Reflector.FindFieldInfo(m.DeclaringType, (PropertyInfo)m);

                    var newBinding = ChangeProjector(index + 1, members, ee.GetBinding(fi), changeExpression);

                    var binding = ee.Bindings.Select(fb =>
                    !ReflectionTools.FieldEquals(fb.FieldInfo, fi) ? fb :
                    new FieldBinding(fi, newBinding));

                    return new EntityExpression(ee.Type, ee.ExternalId, ee.ExternalPeriod, ee.TableAlias, binding, ee.Mixins, ee.TablePeriod, ee.AvoidExpandOnRetrieving);
                }
                else if (projector is NewExpression ne)
                {
                    var p = (PropertyInfo)m;

                    var mIndex = ne.Members.IndexOf(pi => ReflectionTools.PropertyEquals((PropertyInfo)pi, p));

                    var newArg = ChangeProjector(index + 1, members, ne.Arguments[mIndex], changeExpression);

                    var arguments = ne.Arguments.Select((oldArg, i) => i != index ? oldArg : newArg);

                    return Expression.New(ne.Constructor, arguments, ne.Members);
                }
                else if (projector is MListExpression me)
                {
                    var proj = MListProjection(me, true);
                    using (SetCurrentSource(proj.Select))
                    {
                        var mle = (NewExpression) proj.Projector;

                        var paramIndex = mle.Constructor.GetParameters().IndexOf(p => p.Name == "value");

                        var newElement = ChangeProjector(index + 1, members, mle.Arguments[paramIndex], changeExpression);

                        var newMle = Expression.New(mle.Constructor,
                            mle.Arguments.Select((a, i) => paramIndex == i ? newElement : a));

                        var newProjection = new ProjectionExpression(proj.Select, newMle, proj.UniqueFunction, proj.Type);

                        return new MListProjectionExpression(me.Type, newProjection);
                    }
                }
            }

            throw new NotImplementedException($"ChangeProjector not implemented for projector of type {projector.Type} and member {m}");
        }

        string currentTableHint;
        private Expression BindWithHints(Expression source, ConstantExpression hint)
        {
            string oldHint = currentTableHint;
            try
            {
                currentTableHint = (string)hint.Value;

                ProjectionExpression projection = this.VisitCastProjection(source);

                if (currentTableHint != null)
                    throw new InvalidOperationException("Hint {0} not applied".FormatWith(currentTableHint));

                return projection;

            }
            finally
            {
                currentTableHint = oldHint;
            }
        }


        static MethodInfo miSplitCase = ReflectionTools.GetMethodInfo((Entity e) => e.CombineCase()).GetGenericMethodDefinition();
        static MethodInfo miSplitUnion = ReflectionTools.GetMethodInfo((Entity e) => e.CombineUnion()).GetGenericMethodDefinition();
        private CombineStrategy GetStrategy(MethodInfo methodInfo)
        {
            if (methodInfo.IsInstantiationOf(miSplitCase))
                return CombineStrategy.Case;

            if (methodInfo.IsInstantiationOf(miSplitUnion))
                return CombineStrategy.Union;

            throw new InvalidOperationException("Method {0} not expected".FormatWith(methodInfo.Name));
        }

        private Expression MapVisitExpand(LambdaExpression lambda, ProjectionExpression projection)
        {
            return MapVisitExpand(lambda, projection.Projector, projection.Select);
        }

        internal Expression MapVisitExpand(LambdaExpression lambda, Expression projector, SourceExpression source)
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
            projection = WithIndex(projection, out ColumnExpression index);

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
            
            throw new InvalidOperationException("Impossible to convert in ProjectionExpression: \r\n" + expression.ToString());
        }

        private static Expression RemoveProjectionConvert(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert && (expression.Type.IsInstantiationOf(typeof(IGrouping<,>)) ||
                                                                  expression.Type.IsInstantiationOf(typeof(IEnumerable<>)) ||
                                                                  expression.Type.IsInstantiationOf(typeof(IQueryable<>))))
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
        public Dictionary<ProjectionExpression, Expression> uniqueFunctionReplacements = new Dictionary<ProjectionExpression, Expression>(DbExpressionComparer.GetComparer<ProjectionExpression>(false));
        private Expression BindUniqueRow(Type resultType, UniqueFunction function, Expression source, LambdaExpression predicate, bool isRoot)
        {
            ProjectionExpression rawProjector = this.VisitCastProjection(source);

            var expandedProjector = QueryJoinExpander.ExpandJoins(rawProjector, this, cleanRequests: false);

            ProjectionExpression projection = (ProjectionExpression)AliasReplacer.Replace(expandedProjector, this.aliasGenerator);

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
            var proj = DistinctEntityCleaner.Clean(projection.Projector);
            ProjectedColumns pc = ColumnProjector.ProjectColumns(proj, alias, isGroupKey: true, selectTrivialColumns: true);
            return new ProjectionExpression(
                new SelectExpression(alias, true, null, pc.Columns, projection.Select, null, null, null, 0),
                proj, null, resultType);
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

            var set = DbExpressionNominator.Nominate(projection.Projector, out Expression nominated, isGroupKey: true);

            if (!set.Contains(nominated))
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

        
        (Expression newSource, LambdaExpression selector, bool distinct) DisassembleAggregate(AggregateSqlFunction aggregate, Expression source, LambdaExpression selectorOrPredicate, bool isRoot)
        {
            if(aggregate == AggregateSqlFunction.Count)
            {
                if (selectorOrPredicate != null)
                {
                    var miWhere = source.Type.IsInstanceOfType(typeof(Queryable)) ?
         OverloadingSimplifier.miWhereQ :
         OverloadingSimplifier.miWhereE;

                    source = Expression.Call(miWhere.MakeGenericMethod(source.Type.ElementType()), source, selectorOrPredicate);
                    selectorOrPredicate = null;
                }
                
                //Select Distinct NotNull
                {
                    if (ExtractWhere(source, out var inner, out var predicate) &&
                        ExtractDistinct(inner, out var inner2) &&
                        ExtractSelect(inner2, out var inner3, out var selector) &&
                        IsNotNull(predicate.Body, out var p) && p == predicate.Parameters.SingleEx())
                        return (inner3, selector, true);
                }

                //Select NotNull Distinct
                {
                    if (ExtractDistinct(source, out var inner) &&
                        ExtractWhere(inner, out var inner2, out var predicate) &&
                        ExtractSelect(inner2, out var inner3, out var selector) &&
                        IsNotNull(predicate.Body, out var p) && p == predicate.Parameters.SingleEx())
                        return (inner3, selector, true);
                }

                //NotNull Select Distinct
                {
                    if (ExtractDistinct(source, out var inner) &&
                        ExtractSelect(inner, out var inner2, out var selector) &&
                        ExtractWhere(inner2, out var inner3, out var predicate) &&
                        IsNotNull(predicate.Body, out var p) && ExpressionComparer.AreEqual(p, selector.Body, 
                        new ScopedDictionary<ParameterExpression, ParameterExpression>(null) { { predicate.Parameters.Single(), selector.Parameters.Single() } }))
                        return (inner3, selector, true);
                }
                
                if(!isRoot)
                {
                    //Preferring Count(predicate) 
                    //instead of Count (*) Where predicate
                    //is tricky
                    if (ExtractWhere(source, out var inner, out var predicate) && 
                        inner is ParameterExpression p && p.Type.IsInstantiationOf(typeof(IGrouping<,>)) &&
                        SimplePredicateVisitor.IsSimple(predicate))
                        return (inner, predicate, false);
                }

                return (source, null, false);
            }
            else
            {
                if(selectorOrPredicate != null)
                    return (source, selectorOrPredicate, false);
                else if(ExtractSelect(source, out var innerSource, out var selector))
                    return (innerSource, selector, false);
                else
                    return (source, null, false);
            }
        }

        class SimplePredicateVisitor : ExpressionVisitor
        {
            public ParameterExpression[] AllowedParameters;
            public bool HasExternalParameter = false;
            public static bool IsSimple(LambdaExpression lambda)
            {
                var extParam = new SimplePredicateVisitor
                {
                    AllowedParameters = lambda.Parameters.ToArray()
                };
                extParam.Visit(lambda.Body);
                return !extParam.HasExternalParameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (!AllowedParameters.Contains(node))
                    this.HasExternalParameter = true;

                return base.VisitParameter(node);
            }
        }

        private bool IsNotNull(Expression body, out Expression p)
        {
            if(body is BinaryExpression b && b.NodeType == ExpressionType.NotEqual)
            {
                p = b.Left.IsNull() ? b.Right :
                    b.Right.IsNull() ? b.Left :
                    null;

                return p != null;
            }

            p = null;

            return false;

        }

        bool ExtractSelect(Expression source, out Expression innerSource, out LambdaExpression selector)
        {
            if(source is MethodCallExpression mc &&
                    (mc.Method.IsInstantiationOf(OverloadingSimplifier.miSelectE) ||
                    mc.Method.IsInstantiationOf(OverloadingSimplifier.miSelectQ)))
            {
                innerSource = mc.Arguments[0];
                selector = (LambdaExpression)mc.Arguments[1].StripQuotes();
                return true;
            }
            else
            {
                innerSource = null;
                selector = null;
                return false;
            }
        }

        bool ExtractWhere(Expression source, out Expression innerSource, out LambdaExpression predicate)
        {
            if (source is MethodCallExpression mc &&
                    (mc.Method.IsInstantiationOf(OverloadingSimplifier.miWhereE) ||
                    mc.Method.IsInstantiationOf(OverloadingSimplifier.miWhereQ)))
            {
                innerSource = mc.Arguments[0];
                predicate = (LambdaExpression)mc.Arguments[1].StripQuotes();
                return true;
            }
            else
            {
                innerSource = null;
                predicate = null;
                return false;
            }
        }

        bool ExtractDistinct(Expression source, out Expression innerSource)
        {
            if (source is MethodCallExpression mc &&
                    (mc.Method.IsInstantiationOf(OverloadingSimplifier.miDistinctE) ||
                    mc.Method.IsInstantiationOf(OverloadingSimplifier.miDistinctQ)))
            {
                innerSource = mc.Arguments[0];
                return true;
            }
            else
            {
                innerSource = null;
                return false;
            }
        }


        private Expression BindAggregate(Type resultType, AggregateSqlFunction aggregateFunction, Expression source, LambdaExpression selectorOrPredicate, bool isRoot)
        {
            var (newSource, selector, distinct) = DisassembleAggregate(aggregateFunction, source, selectorOrPredicate, isRoot);
            
            ProjectionExpression projection = VisitCastProjection(newSource);
            
            GroupByInfo info = groupByMap.TryGetC(projection.Select.Alias);
            if (info != null) 
            {
                Expression exp = aggregateFunction == AggregateSqlFunction.Count && selector == null ? null : //Count(*)
                    aggregateFunction == AggregateSqlFunction.Count && !distinct ? MapVisitExpand(ToNotNullPredicate(selector), info.Projector, info.Source) :
                    selector != null ? MapVisitExpand(selector, info.Projector, info.Source) : //Sum(Amount), Avg(Amount), ...
                    info.Projector;

                exp = exp == null ? null : SmartEqualizer.UnwrapPrimaryKey(exp);

                var nominated = aggregateFunction == AggregateSqlFunction.Count ? DbExpressionNominator.FullNominateNotNullable(exp) :
                    DbExpressionNominator.FullNominate(exp);

                var result = new AggregateRequestsExpression(info.GroupAlias,
                    new AggregateExpression(aggregateFunction == AggregateSqlFunction.Count ? typeof(int) : GetBasicType(nominated), 
                    nominated, aggregateFunction, 
                    distinct));

                return RestoreWrappedType(result, resultType);
            }
            else //Complicated SubQuery
            {
                Expression exp = aggregateFunction == AggregateSqlFunction.Count && selector == null ? null :
                    aggregateFunction == AggregateSqlFunction.Count && !distinct ? MapVisitExpand(ToNotNullPredicate(selector), projection) :
                    selector != null ? MapVisitExpand(selector, projection) :
                    projection.Projector;

                exp = exp == null ? null : SmartEqualizer.UnwrapPrimaryKey(exp);

                Expression aggregate;
                if (aggregateFunction == AggregateSqlFunction.Sum && !resultType.IsNullable())
                {
                    var nominated = DbExpressionNominator.FullNominate(exp).Nullify();

                    aggregate = (Expression)Expression.Coalesce(
                        new AggregateExpression(GetBasicType(nominated), nominated, aggregateFunction, distinct),
                        new SqlConstantExpression(Activator.CreateInstance(nominated.Type.UnNullify())));
                }
                else
                {
                    var nominated = aggregateFunction == AggregateSqlFunction.Count ? DbExpressionNominator.FullNominateNotNullable(exp) :
                        DbExpressionNominator.FullNominate(exp);

                    aggregate = new AggregateExpression(aggregateFunction == AggregateSqlFunction.Count ? typeof(int) : GetBasicType(nominated), 
                        nominated, 
                        aggregateFunction,
                        distinct);
                }

                Alias alias = NextSelectAlias();

                ColumnDeclaration cd = new ColumnDeclaration("a", aggregate);

                SelectExpression select = new SelectExpression(alias, false, null, new[] { cd }, projection.Select, null, null, null, 0);

                if (isRoot)
                    return new ProjectionExpression(select,
                       RestoreWrappedType(ColumnProjector.SingleProjection(cd, alias, aggregate.Type), resultType),
                       UniqueFunction.Single, resultType);

                ScalarExpression subquery = new ScalarExpression(aggregate.Type, select);

                return RestoreWrappedType(subquery, resultType);
            }
        }

        private LambdaExpression ToNotNullPredicate(LambdaExpression predicate)
        {
            if(predicate.Body is BinaryExpression be && be.NodeType == ExpressionType.NotEqual)
            {
                var exp =
                    be.Left.IsNull() ? be.Right :
                    be.Right.IsNull() ? be.Left : null;

                if (exp != null)
                {
                    return Expression.Lambda(exp, predicate.Parameters);
                }
            }
            else
            {
                var conditional = Expression.Condition(predicate.Body, Expression.Constant("placeholder"), Expression.Constant(null, typeof(string)));

                return Expression.Lambda(conditional, predicate.Parameters);
            }

            return null;
        }

        private Type GetBasicType(Expression nominated)
        {
            if (nominated == null)
                return typeof(int);

            if (nominated.Type.UnNullify().IsEnum)
                return nominated.Type.IsNullable() ? typeof(int?) : typeof(int);

            return nominated.Type;
        }

        static Expression RestoreWrappedType(Expression expression, Type wrapType)
        {
            if (wrapType == expression.Type)
                return expression;

            if (wrapType == typeof(PrimaryKey))
                return new PrimaryKeyExpression(expression.Nullify()).UnNullify();

            if (wrapType == typeof(PrimaryKey?))
                return new PrimaryKeyExpression(expression.Nullify());

            return Expression.Convert(expression, wrapType);
        }


        private Expression BindAnyAll(Type resultType, Expression source, LambdaExpression predicate, MethodInfo method, bool isRoot)
        {
            bool isAll = method.Name == "All";

            if (source is ParameterExpression)
                source = VisitParameter((ParameterExpression)source);

            if (source is ConstantExpression constSource && !typeof(IQueryable).IsAssignableFrom(constSource.Type))
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
                IEnumerable col = (IEnumerable)ce.Value ?? new object[0];

                if (newItem.Type == typeof(Type))
                    return SmartEqualizer.TypeIn(newItem, col.Cast<Type>().ToList());

                if (newItem is LiteReferenceExpression)
                    return SmartEqualizer.EntityIn((LiteReferenceExpression)newItem, col.Cast<Lite<IEntity>>().ToList());

                if (newItem is EntityExpression || newItem is ImplementedByExpression || newItem is ImplementedByAllExpression)
                    return SmartEqualizer.EntityIn(newItem, col.Cast<Entity>().ToList());

                if (newItem.Type.UnNullify() == typeof(PrimaryKey))
                    return SmartEqualizer.InPrimaryKey(newItem, col.Cast<PrimaryKey>().ToArray());

                return SmartEqualizer.In(newItem, col.Cast<object>().ToArray());
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

            JoinType joinType = IsTable(collectionSelector.Body) && !outer ? JoinType.CrossJoin :
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

            var select = projection.Select;

            if (keyPC.Columns.Any(c => ContainsAggregateVisitor.ContainsAggregate(c.Expression))) //SQL Server doesn't like to use aggregates (like Count) as grouping keys, and a intermediate query is necessary
            {
                select = new SelectExpression(alias, false, null, keyPC.Columns, projection.Select, null, null, null, 0);
                alias = NextSelectAlias();
                ColumnGenerator cg = new ColumnGenerator();
                var newColumns = keyPC.Columns.Select(cd => cg.MapColumn(cd.GetReference(select.Alias))).ToReadOnly();
                var dic = newColumns.ToDictionary(cd => (ColumnExpression)cd.Expression, cd => cd.GetReference(alias));
                var newProjector = ColumnReplacerVisitor.ReplaceColumns(dic, keyPC.Projector);
                keyPC = new ProjectedColumns(newProjector, newColumns);
            }

            Expression elemExpr = MapVisitExpand(elementSelector, projection);

            Expression subqueryKey = GroupEntityCleaner.Clean(MapVisitExpand(keySelector, subqueryProjection));// recompute key columns for group expressions relative to subquery (need these for doing the correlation predicate
            ProjectedColumns subqueryKeyPC = ColumnProjector.ProjectColumns(subqueryKey, aliasGenerator.Raw("basura"), isGroupKey: true, selectTrivialColumns: true); // use same projection trick to get group-by expressions based on subquery
            Expression subqueryElemExpr = MapVisitExpand(elementSelector, subqueryProjection); // compute element based on duplicated subquery

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
                Source = select,
            });

            var result = new ProjectionExpression(
                new SelectExpression(alias, false, null, keyPC.Columns, select, null, null, keyPC.Columns.Select(c => c.Expression), 0),
                resultExpr, null, resultType.GetGenericTypeDefinition().MakeGenericType(resultExpr.Type));

            return result;
        }

        class ContainsAggregateVisitor : DbExpressionVisitor
        {
            bool hasAggregate;

            public static bool ContainsAggregate(Expression exp)
            {
                var cav = new ContainsAggregateVisitor();
                cav.Visit(exp);
                return cav.hasAggregate;
            }

            protected internal override Expression VisitAggregate(AggregateExpression aggregate)
            {
                hasAggregate = true;
                return base.VisitAggregate(aggregate);
            }
        }

        class ColumnReplacerVisitor : DbExpressionVisitor
        {
            Dictionary<ColumnExpression, ColumnExpression> Replacements;

            public static Expression ReplaceColumns(Dictionary<ColumnExpression, ColumnExpression> replacements, Expression exp)
            {
                return new ColumnReplacerVisitor { Replacements = replacements }.Visit(exp);
            }

            protected internal override Expression VisitColumn(ColumnExpression column)
            {
                return Replacements.TryGetC(column) ?? column;
            }
        }


        List<OrderExpression> thenBys;
        protected virtual Expression BindOrderBy(Type resultType, Expression source, LambdaExpression orderSelector, OrderType orderType)
        {
            List<OrderExpression> myThenBys = this.thenBys;
            this.thenBys = null;
            ProjectionExpression projection = this.VisitCastProjection(source);

            List<OrderExpression> orderings = new List<OrderExpression>
            {
                new OrderExpression(orderType, GetOrderExpression(orderSelector, projection))
            };
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

                if (expr is LiteReferenceExpression lite)
                {
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

                if (expr.Type.UnNullify() == typeof(PrimaryKey))
                {
                    expr = SmartEqualizer.UnwrapPrimaryKey(expr);
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
                throw new InvalidOperationException("{0} belongs to another kind of Linq Provider".FormatWith(type.TypeName()));

            return true;
        }

        public bool IsTableValuedFunction(MethodCallExpression mce)
        {
            return typeof(IQueryable).IsAssignableFrom(mce.Method.ReturnType) &&
                mce.Method.GetCustomAttribute<SqlMethodAttribute>() != null;
        }

        private ProjectionExpression GetTableProjection(IQueryable query)
        {
            ITable table = ((ISignumTable)query).Table;

            Alias tableAlias = NextTableAlias(table.Name);

            Expression exp = table is Table ?
                ((Table)table).GetProjectorExpression(tableAlias, this) :
                ((TableMList)table).GetProjectorExpression(tableAlias, this);

            Type resultType = typeof(IQueryable<>).MakeGenericType(query.ElementType);
            TableExpression tableExpression = new TableExpression(tableAlias, table, table.SystemVersioned != null ? this.systemTime : null, currentTableHint);
            currentTableHint = null;

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

            Table table = Schema.Current.ViewBuilder.NewView(type);

            Alias tableAlias = NextTableAlias(table.Name);

            Expression exp = table.GetProjectorExpression(tableAlias, this);

            var functionName = mce.Method.GetCustomAttribute<SqlMethodAttribute>().Name ?? mce.Method.Name;

            var argumens = mce.Arguments.Select(DbExpressionNominator.FullNominate).ToList();

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
            if (IsTable(c.Value))
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
            return map.TryGetC(p) ?? p; //i.e. Try
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

        protected override Expression VisitMember(MemberExpression m)
        {
            Expression ex = base.VisitMember(m);
            Expression binded = BindMemberAccess((MemberExpression)ex);
            return binded;
        }

        public Expression BindMethodCall(MethodCallExpression m)
        {
            Expression source = m.Method.IsExtensionMethod() ? m.Arguments[0] : m.Object;

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

            if (source is ImplementedByExpression ib)
            {
                if (m.Method.IsExtensionMethod())
                {
                    return DispatchIb(ib, m.Type, ee =>
                        BindMethodCall(Expression.Call(null, m.Method, m.Arguments.Skip(1).PreAnd(ee))));
                }
                else
                {
                    return DispatchIb(ib, m.Type, ee =>
                         BindMethodCall(Expression.Call(ee, m.Method, m.Arguments)));
                }
            }

            if (m.Method.Name == "ToString" && m.Method.GetParameters().Length == 0)
            {
                if (source is EntityExpression)
                {
                    EntityExpression ee = (EntityExpression)source;

                    if (Schema.Current.Table(ee.Type).ToStrColumn != null)
                    {
                        return Completed(ee).GetBinding(EntityExpression.ToStrField);
                    }

                    throw new InvalidOperationException("ToString expression should already been expanded at this stage");
                }
                else if (source is LiteReferenceExpression lite)
                {
                    var toStr = lite.CustomToStr ?? BindMethodCall(Expression.Call(lite.Reference, EntityExpression.ToStringMethod));

                    return toStr;
                }
                else if (source.NodeType == ExpressionType.Convert && source.Type.UnNullify().IsEnum)
                {
                    var table = Schema.Current.Table(EnumEntity.Generate(source.Type.UnNullify()));

                    if (table != null)
                    {
                        var ee = new EntityExpression(EnumEntity.Generate(source.Type.UnNullify()), 
                            new PrimaryKeyExpression(((UnaryExpression)source).Operand.Nullify()), null, null, null, null, null, false);

                        return Completed(ee).GetBinding(EntityExpression.ToStrField);
                    }
                }
            }

            if (m.Method.Name == "Mixin" && source is EntityExpression && m.Method.GetParameters().Length == 0)
            {
                EntityExpression ee = (EntityExpression)source;

                var mixinType = m.Method.GetGenericArguments().SingleEx();

                Expression result = Completed(ee)
                    .Mixins
                    .EmptyIfNull()
                    .Where(mx => mx.Type == mixinType)
                    .SingleEx(() => "{0} on {1}".FormatWith(mixinType.Name, source.Type.Name));

                return result;
            }

            if(m.Method.DeclaringType == typeof(SystemTimeExtensions) && m.Method.Name.StartsWith(nameof(SystemTimeExtensions.SystemPeriod)))
            {
                var tablePeriod =
                    source is EntityExpression e ? Completed(e).Let(ec => ec.TablePeriod ?? ec.Table.GenerateSystemPeriod(ec.TableAlias, this, force: true)) :
                    source is MListElementExpression mle ? mle.TablePeriod ?? mle.Table.GenerateSystemPeriod(mle.Alias, this, force: true) :
                    throw new InvalidOperationException("Unexpected source");

                if (tablePeriod == null)
                    throw new InvalidOperationException("No System Period found in " + source);

                return tablePeriod;
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

        static readonly PropertyInfo piIdClass = ReflectionTools.GetPropertyInfo((Entity e) => e.Id);
        static readonly PropertyInfo piIdInterface = ReflectionTools.GetPropertyInfo((IEntity e) => e.Id);

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
                    BindMemberAccess(Expression.MakeMemberAccess(bin.Left.UnNullify(), m.Member)),
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

            if (source.IsNull())
            {
                return Expression.Constant(null, m.Type.Nullify()).TryConvert(m.Type);
            }

            if (source is ProjectionExpression proj)
            {
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
                            {
                                int index = nex.Constructor.GetParameters().IndexOf(p => p.Name.Equals(m.Member.Name, StringComparison.InvariantCultureIgnoreCase));

                                if (index == -1)
                                    throw new InvalidOperationException("Impossible to bind '{0}' on '{1}'".FormatWith(m.Member.Name, nex.Constructor.ConstructorSignature()));

                                return nex.Arguments[index].TryConvert(m.Member.ReturningType());
                            }

                            PropertyInfo pi = (PropertyInfo)m.Member;
                            return nex.Members.Zip(nex.Arguments).SingleEx(p => ReflectionTools.PropertyEquals((PropertyInfo)p.first, pi)).second;
                        }
                        break;
                    }
                default:
                    {
                        if (source is DbExpression db)
                        {
                            switch (db.DbNodeType)
                            {
                                case DbExpressionType.Entity:
                                    {
                                        EntityExpression ee = (EntityExpression)source;
                                        var pi = m.Member as PropertyInfo;
                                        if (pi != null && ReflectionTools.PropertyEquals(pi, EntityExpression.IdOrNullProperty))
                                            return ee.ExternalId;

                                        FieldInfo fi = m.Member as FieldInfo ?? Reflector.TryFindFieldInfo(ee.Type, pi);

                                        if (fi == null)
                                            throw new InvalidOperationException("The member {0} of {1} is not accessible on queries".FormatWith(m.Member.Name, ee.Type.TypeName()));

                                        if (fi != null && ReflectionTools.FieldEquals(fi, EntityExpression.IdField))
                                            return ee.ExternalId.UnNullify();

                                        Expression result = Completed(ee).GetBinding(fi);

                                        if (result is MListExpression me)
                                            return MListProjection(me, withRowId: false);

                                        if (result is AdditionalFieldExpression afe)
                                            return BindAdditionalField(afe, entityCompleter: false);

                                        return result;
                                    }
                                case DbExpressionType.EmbeddedInit:
                                    {
                                        EmbeddedEntityExpression eee = (EmbeddedEntityExpression)source;
                                        FieldInfo fi = m.Member as FieldInfo ?? Reflector.TryFindFieldInfo(eee.Type, (PropertyInfo)m.Member);

                                        if (fi == null)
                                            throw new InvalidOperationException("The member {0} of {1} is not accesible on queries".FormatWith(m.Member.Name, eee.Type.TypeName()));

                                        Expression result = eee.GetBinding(fi);

                                        if (result is MListExpression)
                                            return MListProjection((MListExpression)result, withRowId: false);

                                        if (result is AdditionalFieldExpression afe)
                                            return BindAdditionalField(afe, entityCompleter: false);

                                        return result;
                                    }
                                case DbExpressionType.MixinInit:
                                    {
                                        MixinEntityExpression mee = (MixinEntityExpression)source;

                                        PropertyInfo pi = m.Member as PropertyInfo;
                                        if (pi.Name == "MainEntity")
                                            return mee.FieldMixin.MainEntityTable.GetProjectorExpression(mee.MainEntityAlias, this);

                                        FieldInfo fi = m.Member as FieldInfo ?? Reflector.FindFieldInfo(mee.Type, (PropertyInfo)m.Member);

                                        if (fi == null)
                                            throw new InvalidOperationException("The member {0} of {1} is not accesible on queries".FormatWith(m.Member.Name, mee.Type.TypeName()));

                                        Expression result = mee.GetBinding(fi);

                                        if (result is MListExpression)
                                            return MListProjection((MListExpression)result, withRowId: false);

                                        if (result is AdditionalFieldExpression afe)
                                            return BindAdditionalField(afe, entityCompleter: false);

                                        return result;
                                    }
                                case DbExpressionType.LiteReference:
                                    {
                                        LiteReferenceExpression liteRef = (LiteReferenceExpression)source;
                                        PropertyInfo pi = m.Member as PropertyInfo;
                                        if (pi != null)
                                        {
                                            if (pi.Name == "Id")
                                                return BindMemberAccess(Expression.Property(liteRef.Reference, liteRef.Reference.Type.IsInterface ? piIdInterface : piIdClass));
                                            if (pi.Name == "EntityOrNull" || pi.Name == "Entity")
                                                return liteRef.Reference;
                                        }

                                        if (pi.Name == "EntityType")
                                            return GetEntityType(liteRef.Reference);

                                        throw new InvalidOperationException("The member {0} of Lite is not accessible on queries".FormatWith(m.Member));
                                    }
                                case DbExpressionType.ImplementedBy:
                                    {
                                        var ib = (ImplementedByExpression)source;

                                        return DispatchIb(ib, m.Member.ReturningType(), ee =>
                                            BindMemberAccess(Expression.MakeMemberAccess(ee, m.Member)));
                                    }
                                case DbExpressionType.ImplementedByAll:
                                    {
                                        ImplementedByAllExpression iba = (ImplementedByAllExpression)source;
                                        FieldInfo fi = m.Member as FieldInfo ?? Reflector.FindFieldInfo(iba.Type, (PropertyInfo)m.Member);
                                        if (fi != null && fi.FieldEquals((Entity ie) => ie.id))
                                            return new PrimaryKeyStringExpression(iba.Id, iba.TypeId).UnNullify();

                                        throw new InvalidOperationException("The member {0} of ImplementedByAll is not accesible on queries".FormatWith(m.Member));
                                    }
                                case DbExpressionType.MListElement:
                                    {
                                        MListElementExpression mle = (MListElementExpression)source;

                                        switch (m.Member.Name)
                                        {
                                            case "RowId": return mle.RowId.UnNullify();
                                            case "Parent": return mle.Parent;
                                            case "Order": return mle.Order.ThrowIfNull(() => "{0} has no {1}".FormatWith(mle.Table.Name, m.Member.Name));
                                            case "Element": return mle.Element;
                                            default:
                                                throw new InvalidOperationException("The member {0} of MListElement is not accesible on queries".FormatWith(m.Member));
                                        }
                                    }
                            }
                        }
                        break;
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

            if (ib.Strategy == CombineStrategy.Case)
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
                    .Select(kvp => new When(Expression.NotEqual(kvp.Value.ExternalId, NullId(kvp.Value.ExternalId.ValueType)), implementations[kvp.Key]))
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

                var id = new PrimaryKeyExpression(CombineImplementations(strategy, expressions.SelectDictionary(imp => ((EntityExpression)imp).ExternalId.Value),
                    expressions.Values.Select(imp => ((EntityExpression)imp).ExternalId.ValueType.Nullify()).Distinct().SingleEx()));

                return new EntityExpression(returnType, id, null, null, null, null, null, avoidExpandOnRetrieving);
            }

            if (expressions.Any(e => e.Value is ImplementedByAllExpression))
            {
                Expression id = CombineImplementations(strategy, expressions.SelectDictionary(w => GetIdString(w)), typeof(string));
                TypeImplementedByAllExpression typeId = (TypeImplementedByAllExpression)
                    CombineImplementations(strategy, expressions.SelectDictionary(w => GetEntityType(w)), typeof(Type));

                return new ImplementedByAllExpression(returnType, id, typeId, null);
            }

            if (expressions.All(e => e.Value is EntityExpression || e.Value is ImplementedByExpression))
            {
                var hs = expressions.Values.SelectMany(exp => exp is EntityExpression ?
                    (IEnumerable<Type>)new[] { ((EntityExpression)exp).Type } :
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

                        return new EntityExpression(t, new PrimaryKeyExpression(new SqlConstantExpression(null, PrimaryKey.Type(t).Nullify())), null, null, null, null, null, false);
                    }), t));

                var stra = expressions.Values.OfType<ImplementedByExpression>().Select(a => a.Strategy).Distinct().Only(); //Default Union

                return new ImplementedByExpression(returnType, stra, newImplementations);
            }

            if (expressions.All(e => e.Value is EmbeddedEntityExpression))
            {
                var bindings = (from w in expressions
                                from b in ((EmbeddedEntityExpression)w.Value).Bindings
                                group KVP.Create(w.Key, b.Binding) by b.FieldInfo into g
                                select new FieldBinding(g.Key,
                                    CombineImplementations(strategy, g.ToDictionary(), g.Key.FieldType))).ToList();

                var hasValue = CombineImplementations(strategy, expressions.SelectDictionary(w => ((EmbeddedEntityExpression)w).HasValue ?? new SqlConstantExpression(true)), typeof(bool));

                return new EmbeddedEntityExpression(returnType, hasValue, bindings, null, null);
            }

            if (expressions.All(e => e.Value is MixinEntityExpression))
            {
                var bindings = (from w in expressions
                                from b in ((MixinEntityExpression)w.Value).Bindings
                                group KVP.Create(w.Key, b.Binding) by b.FieldInfo into g
                                select new FieldBinding(g.Key,
                                  CombineImplementations(strategy, g.ToDictionary(), g.Key.FieldType))).ToList();

                return new MixinEntityExpression(returnType, bindings, null, null);
            }

            if (expressions.Any(e => e.Value is MListExpression))
                throw new InvalidOperationException("MList on ImplementedBy are not supported yet");

            if (expressions.Any(e => e.Value is AdditionalFieldExpression))
                throw new InvalidOperationException("MList on ImplementedBy are not supported yet");

            if (expressions.Any(e => e.Value is TypeImplementedByAllExpression || e.Value is TypeImplementedByExpression || e.Value is TypeEntityExpression))
            {
                var typeId = CombineImplementations(strategy, expressions.SelectDictionary(exp => ExtractTypeId(exp).Value), PrimaryKey.Type(typeof(TypeEntity)).Nullify());

                return new TypeImplementedByAllExpression(new PrimaryKeyExpression(typeId));
            }

            if (expressions.All(i => i.Value.NodeType == ExpressionType.Convert))
            {
                var convertType = expressions.Select(i => i.Value.Type).Distinct().SingleEx();

                var dic = expressions.SelectDictionary(exp => ((UnaryExpression)exp).Operand);

                var value = CombineImplementations(strategy, dic, dic.Values.Select(t => t.Type).Distinct().SingleEx());

                return Expression.Convert(value, convertType);
            }

            if (expressions.All(i => i.Value is PrimaryKeyExpression))
            {
                var type = expressions.Select(i => i.Value.Type).Distinct().SingleEx();

                var dic = expressions.SelectDictionary(exp => ((PrimaryKeyExpression)exp).Value);

                var value = CombineImplementations(strategy, dic, dic.Values.Select(t => t.Type).Distinct().SingleEx());

                return new PrimaryKeyExpression(value);
            }

            if (expressions.All(i => i.Value is PrimaryKeyStringExpression))
            {
                var type = expressions.Select(i => i.Value.Type).Distinct().SingleEx();

                var dicType = expressions.SelectDictionary(exp => (Expression)((PrimaryKeyStringExpression)exp).TypeId);

                var valueType = (TypeImplementedByAllExpression)CombineImplementations(strategy, dicType, typeof(Type));

                var dicId = expressions.SelectDictionary(exp => ((PrimaryKeyStringExpression)exp).Id);

                var valueId = CombineImplementations(strategy, dicId, typeof(string));

                return new PrimaryKeyStringExpression(valueId, valueType);
            }

            if (!Schema.Current.Settings.IsDbType(returnType.UnNullify()))
                throw new InvalidOperationException("Impossible to CombineImplementations of {0}".FormatWith(returnType.TypeName()));


            return strategy.CombineValues(expressions, returnType);
        }

        static ConstantExpression NullTypeId = Expression.Constant(null, PrimaryKey.Type(typeof(TypeEntity)).Nullify());

        internal static PrimaryKeyExpression ExtractTypeId(Expression exp)
        {
            if (exp.NodeType == ExpressionType.Convert)
                exp = ((UnaryExpression)exp).Operand;

            if (exp is TypeImplementedByAllExpression)
                return ((TypeImplementedByAllExpression)exp).TypeColumn;

            if (exp is TypeEntityExpression typeFie)
            {
                var typeId = TypeConstant(((TypeEntityExpression)exp).TypeValue);

                return new PrimaryKeyExpression(Expression.Condition(
                    Expression.NotEqual(typeFie.ExternalId.Value.Nullify(), Expression.Constant(null, typeFie.ExternalId.ValueType.Nullify())),
                    typeId.Nullify(), NullTypeId));
            }

            if (exp is TypeImplementedByExpression typeIb)
            {
                return new PrimaryKeyExpression(
                    typeIb.TypeImplementations.Reverse().Aggregate((Expression)NullTypeId, (acum, imp) =>
                    Expression.Condition(Expression.NotEqual(imp.Value.Value.Nullify(), Expression.Constant(null, imp.Value.ValueType.Nullify())),
                    TypeConstant(imp.Key).Nullify(), acum)));
            }

            throw new InvalidOperationException("Impossible to extract TypeId from {0}".FormatWith(exp.ToString()));
        }

        protected override Expression VisitInvocation(InvocationExpression iv)
        {
            if (iv.Expression is LambdaExpression lambda)
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

        protected override Expression VisitTypeBinary(TypeBinaryExpression b)
        {
            Expression operand = Visit(b.Expression);
            Type type = b.TypeOperand;

            if (operand is LiteReferenceExpression)
            {
                if (!type.IsLite())
                    throw new InvalidCastException("Impossible the type {0} (non-lite) with the expression {1}".FormatWith(type.TypeName(), b.Expression.ToString()));

                operand = ((LiteReferenceExpression)(operand)).Reference;
                type = type.CleanType();
            }

            if (operand is EntityExpression ee)
            {
                if (type.IsAssignableFrom(ee.Type)) // upcasting
                {
                    return new IsNotNullExpression(ee.ExternalId); //Usefull mainly for Shy<T>
                }
                else
                {
                    return Expression.Constant(false);
                }
            }
            if (operand is ImplementedByExpression ib)
            {
                EntityExpression[] fies = ib.Implementations.Where(imp => type.IsAssignableFrom(imp.Key)).Select(imp => imp.Value).ToArray();

                return fies.Select(f => (Expression)Expression.NotEqual(f.ExternalId.Nullify(), NullId(f.ExternalId.ValueType))).AggregateOr();
            }
            else if (operand is ImplementedByAllExpression riba)
            {
                return SmartEqualizer.EqualNullable(riba.TypeId.TypeColumn.Value, TypeConstant(type));
            }
            return base.VisitTypeBinary(b);
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

        protected override Expression VisitLambda<T>(Expression<T> lambda)
        {
            return lambda; //not touch until invoke
        }

        public Expression SimplifyRedundandConverts(UnaryExpression unary)
        {
            if (unary.Operand.NodeType == ExpressionType.Convert)
            {
                var newOperant = SimplifyRedundandConverts((UnaryExpression)unary.Operand);
                unary = Expression.Convert(newOperant, unary.Type);
            }

            //(int)(object)3 --> 3
            if (unary.NodeType == ExpressionType.Convert && unary.Operand.NodeType == ExpressionType.Convert &&
                unary.Type == (((UnaryExpression)unary.Operand).Operand).Type)
                return ((UnaryExpression)unary.Operand).Operand;


            //(int)(PrimaryKey)new PrimaryKey(3) --> (int)3
            if (unary.NodeType == ExpressionType.Convert && unary.Type.UnNullify() != typeof(PrimaryKey) &&
                unary.Operand.NodeType == ExpressionType.Convert && unary.Operand.Type.UnNullify() == typeof(PrimaryKey) &&
                (((UnaryExpression)unary.Operand).Operand is PrimaryKeyExpression))
                return Expression.Convert(((PrimaryKeyExpression)(((UnaryExpression)unary.Operand).Operand)).Value, unary.Type);

            //(int)(PrimaryKey)new PrimaryKey(3)
            if (unary.NodeType == ExpressionType.Convert && unary.Type.UnNullify() != typeof(PrimaryKey) &&
                unary.Operand is PrimaryKeyExpression)
                return Expression.Convert(((PrimaryKeyExpression)unary.Operand).Value, unary.Type);

            //(PrimaryKey)(PrimaryKey)
            if (unary.NodeType == ExpressionType.Convert &&
                unary.Type == unary.Operand.Type)
                return unary.Operand;

            return unary;
        }

        private Expression EntityCasting(Expression operand, Type uType)
        {
            if (operand == null)
                return null;

            if (operand.Type == uType)
                return operand;

            if (operand.Type.IsLite() != uType.IsLite())
            {
                if (uType.IsAssignableFrom(operand.Type)) //(object)
                    return null;

                throw new InvalidCastException("Impossible to convert {0} to {1}".FormatWith(operand.Type.TypeName(), uType.TypeName()));
            }

            if (operand is EntityExpression ee)
            {
                if (uType.IsAssignableFrom(ee.Type)) // upcasting
                {
                    return new ImplementedByExpression(uType, CombineStrategy.Case, new Dictionary<Type, EntityExpression> { { operand.Type, ee } }.ToReadOnly());
                }
                else
                {
                    return new EntityExpression(uType, new PrimaryKeyExpression(Expression.Constant(null, PrimaryKey.Type(uType).Nullify())), null, null, null, null, null, ee.AvoidExpandOnRetrieving);
                }
            }
            if (operand is ImplementedByExpression ib)
            {
                EntityExpression[] fies = ib.Implementations.Where(imp => uType.IsAssignableFrom(imp.Key)).Select(imp => imp.Value).ToArray();

                if (fies.IsEmpty())
                {
                    return new EntityExpression(uType, new PrimaryKeyExpression(Expression.Constant(null, PrimaryKey.Type(uType).Nullify())), null, null, null, null, null, avoidExpandOnRetrieving: true);
                }
                if (fies.Length == 1 && fies[0].Type == uType)
                    return fies[0];

                return new ImplementedByExpression(uType, ib.Strategy, fies.ToDictionary(f => f.Type));
            }
            else if (operand is ImplementedByAllExpression iba)
            {
                if (uType.IsAssignableFrom(iba.Type))
                    return new ImplementedByAllExpression(uType, iba.Id, iba.TypeId, iba.ExternalPeriod);

                var conditionalId = new PrimaryKeyExpression(
                    Expression.Condition(SmartEqualizer.EqualNullable(iba.TypeId.TypeColumn.Value, TypeConstant(uType)),
                    new SqlCastExpression(PrimaryKey.Type(uType).Nullify(), iba.Id),
                    Expression.Constant(null, PrimaryKey.Type(uType).Nullify())));

                return new EntityExpression(uType, conditionalId, iba.ExternalPeriod, null, null, null, null, avoidExpandOnRetrieving: false);
            }

            else if (operand is LiteReferenceExpression lite)
            {
                if (!uType.IsLite())
                    throw new InvalidCastException("Impossible to convert an expression of type {0} to {1}".FormatWith(lite.Type.TypeName(), uType.TypeName()));

                Expression entity = EntityCasting(lite.Reference, Lite.Extract(uType));

                return MakeLite(entity, lite.CustomToStr);
            }

            return null;
        }

        internal static ConstantExpression TypeConstant(Type type)
        {
            return Expression.Constant(TypeId(type).Object);
        }

        internal static PrimaryKey TypeId(Type type)
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
                    //    throw new InvalidOperationException("Comparing {0} and {1} is not valid in SQL".FormatWith(b.Left.ToString(), b.Right.ToString())); 

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
            var isHistory = this.systemTime is SystemTime.HistoryTable;

            List<CommandExpression> commands = new List<CommandExpression>();

            ProjectionExpression pr = (ProjectionExpression)QueryJoinExpander.ExpandJoins(VisitCastProjection(source), this, cleanRequests: true);

            if (pr.Projector is EntityExpression ee)
            {
                Expression id = ee.Table.GetIdExpression(aliasGenerator.Table(ee.Table.GetName(isHistory)));

                commands.AddRange(ee.Table.TablesMList().Select(t =>
                {
                    Expression backId = t.BackColumnExpression(aliasGenerator.Table(t.GetName(isHistory)));
                    return new DeleteExpression(t, isHistory && t.SystemVersioned != null, pr.Select, SmartEqualizer.EqualNullable(backId, ee.ExternalId));
                }));

                commands.Add(new DeleteExpression(ee.Table, isHistory && ee.Table.SystemVersioned != null, pr.Select, SmartEqualizer.EqualNullable(id, ee.ExternalId)));
            }
            else if (pr.Projector is MListElementExpression mlee)
            {
                Expression id = mlee.Table.RowIdExpression(aliasGenerator.Table(mlee.Table.GetName(isHistory)));

                commands.Add(new DeleteExpression(mlee.Table, isHistory && mlee.Table.SystemVersioned != null, pr.Select, SmartEqualizer.EqualNullable(id, mlee.RowId)));
            }
            else if (pr.Projector is EmbeddedEntityExpression eee)
            {
                Expression id = eee.ViewTable.GetIdExpression(aliasGenerator.Table(eee.ViewTable.Name));

                commands.Add(new DeleteExpression(eee.ViewTable, false, pr.Select, SmartEqualizer.EqualNullable(id, eee.GetViewId())));
            }
            else
                throw new InvalidOperationException("Delete not supported for {0}".FormatWith(pr.Projector.GetType().TypeName()));

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

                entity = MapVisitExpand(cleanedSelector, pr);
            }

            ITable table = 
                entity is EntityExpression entEx ? (ITable)entEx.Table :
                entity is EmbeddedEntityExpression eeEx ? (ITable)eeEx.ViewTable:
                entity is MListElementExpression mlistEx ? (ITable)mlistEx.Table : 
                throw new InvalidOperationException();

            Alias alias = aliasGenerator.Table(table.Name);

            Expression toUpdate = table is Table ?
                ((Table)table).GetProjectorExpression(alias, this) :
                ((TableMList)table).GetProjectorExpression(alias, this);

            List<ColumnAssignment> assignments = new List<ColumnAssignment>();
            using (SetCurrentSource(pr.Select.From))
            {
                foreach (var setter in setterExpressions)
                {
                    Expression colExpression;
                    try
                    {
                        var toUpdateParam = setter.PropertyExpression.Parameters.Single();
                        map.Add(toUpdateParam, toUpdate);
                        colExpression = Visit(setter.PropertyExpression.Body);
                        map.Remove(toUpdateParam);
                    }
                    catch (CurrentSourceNotFoundException e)
                    {
                        throw new InvalidOperationException("The expression '{0}' can not be used as a propertyExpression. Consider using UnsafeUpdatePart"
                            .FormatWith(setter.PropertyExpression.ToString()),
                            e);
                    }

                    var param = setter.ValueExpression.Parameters.Single();
                    map.Add(param, pr.Projector);
                    var cleanedValue = DbQueryProvider.Clean(setter.ValueExpression.Body, true, null);
                    Expression valExpression = Visit(cleanedValue);
                    map.Remove(param);

                    assignments.AddRange(AdaptAssign(colExpression, valExpression));
                }
            }
            
            var isHistory = this.systemTime is SystemTime.HistoryTable;
            Expression condition;

            if (entity is EntityExpression ee)
            {
                Expression id = ee.Table.GetIdExpression(aliasGenerator.Table(ee.Table.GetName(isHistory)));

                condition = SmartEqualizer.EqualNullable(id, ee.ExternalId);
                table = ee.Table;
            }
            else if (entity is MListElementExpression mlee)
            {
                Expression id = mlee.Table.RowIdExpression(aliasGenerator.Table(mlee.Table.GetName(isHistory)));

                condition = SmartEqualizer.EqualNullable(id, mlee.RowId);
                table = mlee.Table;
            }
            else if (entity is EmbeddedEntityExpression eee)
            {
                Expression id = eee.ViewTable.GetIdExpression(aliasGenerator.Table(eee.ViewTable.GetName(isHistory)));

                condition = SmartEqualizer.EqualNullable(id, eee.GetViewId());
                table = eee.ViewTable;
            }
            else
                throw new InvalidOperationException("Update not supported for {0}".FormatWith(entity.GetType().TypeName()));


            var result = new CommandAggregateExpression(new CommandExpression[]
            {
                new UpdateExpression(table, isHistory && table.SystemVersioned != null, pr.Select, condition, assignments),
                new SelectRowCountExpression()
            });

            return (CommandAggregateExpression)QueryJoinExpander.ExpandJoins(result, this, cleanRequests: true);
        }

        internal CommandExpression BindInsert(Expression source, LambdaExpression constructor, ITable table)
        {
            ProjectionExpression pr = VisitCastProjection(source);

            Alias alias = aliasGenerator.Table(table.Name);

            Expression toInsert = table is Table ?
                ((Table)table).GetProjectorExpression(alias, this) :
                ((TableMList)table).GetProjectorExpression(alias, this);

            ParameterExpression param = constructor.Parameters[0];
            ParameterExpression toInsertParam = Expression.Parameter(toInsert.Type, "toInsert");

            List<ColumnAssignment> assignments = new List<ColumnAssignment>();
            using (SetCurrentSource(pr.Select))
            {
                map.Add(param, pr.Projector);
                map.Add(toInsertParam, toInsert);
                var cleanedConstructor = DbQueryProvider.Clean(constructor.Body, false, null);
                FillColumnAssigments(assignments, toInsertParam, cleanedConstructor, e =>
                {
                    var cleaned = DbQueryProvider.Clean(e, true, null);
                    Expression expression = Visit(cleaned);
                    return expression;
                });
                map.Remove(toInsertParam);
                map.Remove(param);
            }

            if (table is Table entityTable && entityTable.Ticks != null && assignments.None(b => b.Column == entityTable.Ticks.Name))
            {
                assignments.Add(new ColumnAssignment(entityTable.Ticks.Name, Expression.Constant(0L, typeof(long))));
            }

            var isHistory = this.systemTime is SystemTime.HistoryTable;

            var result = new CommandAggregateExpression(new CommandExpression[]
            {
                new InsertSelectExpression(table, isHistory && table.SystemVersioned != null, pr.Select, assignments),
                new SelectRowCountExpression()
            });

            return (CommandAggregateExpression)QueryJoinExpander.ExpandJoins(result, this, cleanRequests: true);
        }

        static readonly MethodInfo miSetReadonly = ReflectionTools.GetMethodInfo(() => UnsafeEntityExtensions.SetReadonly(null, (Entity a) => a.Id, 1)).GetGenericMethodDefinition();
        static readonly MethodInfo miSetMixin = ReflectionTools.GetMethodInfo(() => ((Entity)null).SetMixin((CorruptMixin m) => m.Corrupt, true)).GetGenericMethodDefinition();

        public void FillColumnAssigments(List<ColumnAssignment> assignments, ParameterExpression toInsert, Expression body, Func<Expression, Expression> visitValue)
        {
            if (body is ParameterExpression)
            {
                body = this.map.GetOrThrow((ParameterExpression)body);
                visitValue = e => e;
            }

            if (body is MethodCallExpression mce)
            {
                var prev = mce.Arguments[0];
                FillColumnAssigments(assignments, toInsert, prev, visitValue);

                if (mce.Method.IsInstantiationOf(miSetReadonly))
                {
                    var pi = ReflectionTools.BasePropertyInfo(mce.Arguments[1].StripQuotes());

                    Expression colExpression = Visit(Expression.MakeMemberAccess(toInsert, Reflector.FindFieldInfo(body.Type, pi)));
                    Expression expression = visitValue(mce.Arguments[2]);
                    assignments.AddRange(AdaptAssign(colExpression, expression));
                }
                else if (mce.Method.IsInstantiationOf(miSetMixin))
                {
                    Type mixinType = mce.Method.GetGenericArguments()[1];

                    var mi = ReflectionTools.BaseMemberInfo(mce.Arguments[1].StripQuotes());

                    Expression mixin = Expression.Call(toInsert, MixinDeclarations.miMixin.MakeGenericMethod(mixinType));

                    Expression colExpression = Visit(Expression.MakeMemberAccess(mixin, mi));
                    Expression expression = visitValue(mce.Arguments[2]);
                    assignments.AddRange(AdaptAssign(colExpression, expression));
                }
                else
                    throw InvalidBody();
            }
            else if (body is MemberInitExpression mie)
            {
                assignments.AddRange(mie.Bindings.SelectMany(m =>
                {
                    MemberAssignment ma = (MemberAssignment)m;
                    Expression colExpression = Visit(Expression.MakeMemberAccess(toInsert, ma.Member));
                    Expression expression = visitValue(ma.Expression);
                    return AdaptAssign(colExpression, expression);
                }));
            }
            else if (body is NewExpression)
            {
                if (((NewExpression)body).Arguments.Any())
                    throw InvalidBody();

                return;
            }
            else
            {
                throw InvalidBody();
            }
        }

        private Exception InvalidBody()
        {
            throw new InvalidOperationException("The only allowed expressions on UnsafeInsert are: object initializers, calling method 'SetMixin', or or calling 'Administrator.SetReadonly'");
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
            else if (colExpression.Type.UnNullify() == typeof(PrimaryKey) && expression.Type.UnNullify() == typeof(PrimaryKey))
            {
                return new[] { AssignColumn(SmartEqualizer.UnwrapPrimaryKey(colExpression), SmartEqualizer.UnwrapPrimaryKey(expression)) };
            }
            else if (colExpression.NodeType == ExpressionType.Convert && colExpression.Type == ((UnaryExpression)colExpression).Operand.Type.UnNullify())
            {
                return new[] { AssignColumn(((UnaryExpression)colExpression).Operand, expression) };
            }
            else if (colExpression.NodeType == ExpressionType.Convert && colExpression.Type.UnNullify().IsEnum && ((UnaryExpression)colExpression).Operand is ColumnExpression)
            {
                return new[] { AssignColumn(((UnaryExpression)colExpression).Operand, expression) };
            }
            else if (colExpression is LiteReferenceExpression && expression is LiteReferenceExpression)
            {
                return Assign(
                    ((LiteReferenceExpression)colExpression).Reference,
                    ((LiteReferenceExpression)expression).Reference);
            }
            else if (colExpression is EmbeddedEntityExpression && expression is EmbeddedEntityExpression)
            {
                EmbeddedEntityExpression cEmb = (EmbeddedEntityExpression)colExpression;
                EmbeddedEntityExpression expEmb = (EmbeddedEntityExpression)expression;

                var bindings = cEmb.Bindings.SelectMany(b => AdaptAssign(b.Binding, expEmb.GetBinding(b.FieldInfo)));

                if (cEmb.FieldEmbedded.HasValue != null)
                {
                    var setValue = AssignColumn(cEmb.HasValue, expEmb.HasValue);
                    bindings = bindings.PreAnd(setValue);
                }

                return bindings.ToArray();
            }
            else if (colExpression is EntityExpression && expression is EntityExpression)
            {
                return new[] { AssignColumn(
                        ((EntityExpression)colExpression).ExternalId.Value,
                        ((EntityExpression)expression).ExternalId.Value) };

            }
            else if (colExpression is ImplementedByExpression && expression is ImplementedByExpression)
            {
                ImplementedByExpression colIb = (ImplementedByExpression)colExpression;
                ImplementedByExpression expIb = (ImplementedByExpression)expression;

                return colIb.Implementations.Select(cImp => AssignColumn(
                    cImp.Value.ExternalId.Value,
                    expIb.Implementations.GetOrThrow(cImp.Key).ExternalId.Value)).ToArray();
            }
            else if (colExpression is ImplementedByAllExpression && expression is ImplementedByAllExpression)
            {
                ImplementedByAllExpression colIba = (ImplementedByAllExpression)colExpression;
                ImplementedByAllExpression expIba = (ImplementedByAllExpression)expression;

                return new[]
                {
                    AssignColumn(colIba.Id, expIba.Id),
                    AssignColumn(colIba.TypeId.TypeColumn.Value, expIba.TypeId.TypeColumn.Value)
                };
            }

            throw new NotImplementedException("{0} can not be assigned from expression:\n{1}".FormatWith(colExpression.Type.TypeName(), expression.ToString()));
        }


        ColumnAssignment AssignColumn(Expression column, Expression expression)
        {
            var col = column as ColumnExpression;

            if (col == null)
                throw new InvalidOperationException("{0} does not represent a column".FormatWith(column.ToString()));

            return new ColumnAssignment(col.Name, DbExpressionNominator.FullNominate(expression));
        }
        #region BinderTools

        Dictionary<ImplementedByExpression, UnionAllRequest> implementedByReplacements = new Dictionary<ImplementedByExpression, UnionAllRequest>(DbExpressionComparer.GetComparer<ImplementedByExpression>(false));
        public UnionAllRequest Completed(ImplementedByExpression ib)
        {
            return implementedByReplacements.GetOrCreate(ib, () =>
            {
                UnionAllRequest result = new UnionAllRequest(ib)
                {
                    UnionAlias = aliasGenerator.NextTableAlias("Union" + ib.Type.Name),

                    Implementations = ib.Implementations.SelectDictionary(k => k, ee =>
                    {
                        var alias = NextTableAlias(ee.Table.Name);

                        return new UnionEntity
                        {
                            Table = new TableExpression(alias, ee.Table, ee.Table.SystemVersioned != null ? this.systemTime : null, null),
                            Entity = (EntityExpression)ee.Table.GetProjectorExpression(alias, this),
                        };
                    }).ToReadOnly()
                };
                List<Expression> equals = new List<Expression>();
                foreach (var unionEntity in result.Implementations.Values)
                {
                    ColumnExpression expression = result.AddIndependentColumn(
                        result.Implementations.Keys.Select(t => PrimaryKey.Type(t).Nullify()).Distinct().SingleEx(),
                        "Id_" + Reflector.CleanTypeName(unionEntity.Entity.Type),
                        unionEntity.Entity.Type, unionEntity.Entity.ExternalId);

                    unionEntity.UnionExternalId = new PrimaryKeyExpression(expression);
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
            var external = req.ExternalAlias(this);

            var result = currentSource.FirstOrDefault(s => //could be more than one on GroupBy aggregates
            {
                if (external.IsEmpty())
                    return true;

                var knownAliases = KnownAliases(s);

                return external.Intersect(knownAliases).Any();
            });

            if (result == null)
                throw new CurrentSourceNotFoundException("Impossible to get current source for aliases " + external.ToString(", "));

            return result;
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
            var list = requests.Where(r => r.Key.KnownAliases.All(result.Contains)).SelectMany(a => a.Value).ToList();

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

        Dictionary<EntityExpression, EntityExpression> entityReplacements = new Dictionary<EntityExpression, EntityExpression>(DbExpressionComparer.GetComparer<EntityExpression>(false));
        public EntityExpression Completed(EntityExpression entity)
        {
            if (entity.TableAlias != null)
                return entity;
            
            EntityExpression completed = entityReplacements.GetOrCreate(entity, () =>
            {
                var table = entity.Table;
                var newAlias = NextTableAlias(table.Name);
                var id = table.GetIdExpression(newAlias);
                var period = table.GenerateSystemPeriod(newAlias, this);
                var intersect = period.Intesection(entity.ExternalPeriod);
                var bindings = table.GenerateBindings(newAlias, this, id, period);
                var mixins = table.GenerateMixins(newAlias, this, id, period);

                var result = new EntityExpression(entity.Type, entity.ExternalId, entity.ExternalPeriod, newAlias, bindings, mixins, period, avoidExpandOnRetrieving: false);
                
                AddRequest(new TableRequest
                {
                    CompleteEntity = result,
                    Table = new TableExpression(newAlias, table, table.SystemVersioned != null ? this.systemTime : null, null),
                });

                return result;
            });

            return completed;
        }





        internal static PrimaryKeyExpression NullId(Type type)
        {
            return new PrimaryKeyExpression(new SqlConstantExpression(null, type.Nullify()));
        }

        public Expression MakeLite(Expression entity, Expression customToStr)
        {
            return new LiteReferenceExpression(Lite.Generate(entity.Type), entity, customToStr, false, false);
        }

        public PrimaryKeyExpression GetId(Expression expression)
        {
            if (expression is EntityExpression)
                return ((EntityExpression)expression).ExternalId;

            if (expression is ImplementedByExpression ib)
            {
                var type = ib.Implementations.Select(imp => imp.Value.ExternalId.ValueType.Nullify()).Distinct().SingleOrDefaultEx() ?? typeof(int?);

                var aggregate = new PrimaryKeyExpression(Coalesce(type, ib.Implementations.Select(imp => imp.Value.ExternalId.Value)));

                return aggregate;
            }

            if (expression.NodeType == ExpressionType.Conditional)
            {
                var con = (ConditionalExpression)expression;

                return new PrimaryKeyExpression(Condition(con.Test,
                    GetId(con.IfTrue).Value.Nullify(),
                    GetId(con.IfFalse).Value.Nullify()));
            }

            if (expression.NodeType == ExpressionType.Coalesce)
            {
                var bin = (BinaryExpression)expression;

                var left = GetId(bin.Left);
                var right = GetId(bin.Right);

                return new PrimaryKeyExpression(Condition(Expression.NotEqual(left.Nullify(), NullId(left.ValueType)),
                    left.Value.Nullify(),
                    right.Value.Nullify()));
            }

            if (expression.IsNull())
                return new PrimaryKeyExpression(Expression.Constant(null, PrimaryKey.Type(expression.Type).Nullify()));

            throw new NotSupportedException("Id for {0}".FormatWith(expression.ToString()));
        }

        public Expression GetIdString(Expression expression)
        {
            if (expression is EntityExpression)
                return Expression.Convert(((EntityExpression)expression).ExternalId.Value, typeof(string));

            if (expression is ImplementedByExpression ib)
            {
                var aggregate = Coalesce(typeof(string),
                    ib.Implementations.Select(imp => new SqlCastExpression(typeof(string), imp.Value.ExternalId.Value)));

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

                var left = GetIdString(bin.Left);
                var right = GetIdString(bin.Right);

                return Condition(Expression.NotEqual(left.Nullify(), Expression.Constant(null, typeof(string))),
                    left.Nullify(),
                    right.Nullify());
            }

            if (expression.IsNull())
                return Expression.Constant(null, typeof(string));

            throw new NotSupportedException("Id for {0}".FormatWith(expression.ToString()));
        }

        public Expression GetEntityType(Expression expression)
        {
            if (expression is EntityExpression ee)
            {
                return new TypeEntityExpression(ee.ExternalId, ee.Type);
            }

            if (expression is ImplementedByExpression ib)
            {
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
                var id = GetId(bin.Left);

                return Condition(Expression.NotEqual(id.Nullify(), NullId(id.ValueType)),
                    GetEntityType(bin.Left), GetEntityType(bin.Right));
            }

            if (expression.IsNull())
                return new TypeImplementedByExpression(new Dictionary<Type, PrimaryKeyExpression>());

            throw new NotSupportedException("Id for {0}".FormatWith(expression.ToString()));
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

        internal ProjectionExpression MListProjection(MListExpression mle, bool withRowId)
        {
            TableMList relationalTable = mle.TableMList;

            Alias tableAlias = NextTableAlias(mle.TableMList.Name);
            TableExpression tableExpression = new TableExpression(tableAlias, relationalTable, 
                relationalTable.SystemVersioned != null ? this.systemTime : null, null);

            Expression projector = relationalTable.FieldExpression(tableAlias, this, mle.ExternalPeriod, withRowId);

            Alias sourceAlias = NextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projector, sourceAlias); // no Token

            var where = DbExpressionNominator.FullNominate(
                SmartEqualizer.EqualNullable(mle.BackID, relationalTable.BackColumnExpression(tableAlias))
                .And(mle.ExternalPeriod.Overlaps(relationalTable.GenerateSystemPeriod(tableAlias, this)))
                );

            var projectType = withRowId ?
                typeof(IEnumerable<>).MakeGenericType(typeof(MList<>.RowIdElement).MakeGenericType(mle.Type.ElementType())) :
                mle.Type;

            var proj = new ProjectionExpression(
                new SelectExpression(sourceAlias, false, null, pc.Columns, tableExpression, where, null, null, 0),
                 pc.Projector, null, projectType);

            return proj;
        }

        internal Expression BindAdditionalField(AdditionalFieldExpression af, bool entityCompleter)
        {
            var lambda = Schema.Current.GetAdditionalQueryBinding(af.Route, entityCompleter);

            if (lambda == null)
                return null;

            var cleanLambda = (LambdaExpression)DbQueryProvider.Clean(lambda, filter: true, log: null);

            var parentEntity = new EntityExpression(af.Route.RootType, af.BackID, af.ExternalPeriod, null, null, null, null, false);

            var expression = this.MapVisitExpand(cleanLambda, parentEntity, null);

            if(expression is MethodCallExpression mce)
            {
                if (mce.Method.DeclaringType == typeof(VirtualMList) && (
                    mce.Method.Name == nameof(VirtualMList.ToVirtualMList) ||
                    mce.Method.Name == nameof(VirtualMList.ToVirtualMListWithOrder)))
                {
                    var proj = (ProjectionExpression)mce.Arguments[0];

                    if (!entityCompleter)
                        return proj;

                    var preserveOrder = mce.Method.Name == nameof(VirtualMList.ToVirtualMListWithOrder);

                    var ee = (EntityExpression)proj.Projector;

                    var type = mce.Method.GetGenericArguments()[0];

                    var mlistType = typeof(MList<>.RowIdElement).MakeGenericType(type);

                    var ci = mlistType.GetConstructor(new[] { type, typeof(PrimaryKey), typeof(int?) });

                    var order = preserveOrder ?
                        ee.GetBinding(Reflector.FindFieldInfo(type, GetOrderColumn(type))) :
                        (Expression)Expression.Constant(null, typeof(int?));

                    var newExp = Expression.New(ci, ee, ee.ExternalId.UnNullify(), order.Nullify());

                    return new ProjectionExpression(proj.Select, newExp, proj.UniqueFunction, typeof(IQueryable<>).MakeGenericType(mlistType));
                }
            }

            return expression;
        }

        private static PropertyInfo GetOrderColumn(Type type)
        {
            if (!typeof(ICanBeOrdered).IsAssignableFrom(type))
                throw new InvalidOperationException($"Type '{type.Name}' should implement '{nameof(ICanBeOrdered)}'");

            var pi = type.GetProperty(nameof(ICanBeOrdered.Order), BindingFlags.Instance | BindingFlags.Public);

            if (pi == null)
                throw new InvalidOperationException("Order Property not found");

            return pi;
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
            return base.ToString() + (OuterApply ? " OUTER APPLY" : " CROSS APPLY") + " with " + Select.ToString();
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
            return base.ToString() + " LEFT OUTER JOIN with " + Table.ToString();
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
            var nullValue = type.IsValueType ? Expression.Constant(null, type.Nullify()).UnNullify() : Expression.Constant(null, type);

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
                var union = Implementations[kvp.Key].UnionExternalId;

                var condition = Expression.NotEqual(union, QueryBinder.NullId(union.ValueType));

                if (kvp.Value is Expression exp)
                {
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

            var nominations = DbExpressionNominator.Nominate(exp, out Expression newExp);

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
        public PrimaryKeyExpression UnionExternalId;
        public EntityExpression Entity;
        public TableExpression Table;
    }

    class QueryJoinExpander : DbExpressionVisitor
    {
        Dictionary<SourceExpression, List<ExpansionRequest>> requests;
        AliasGenerator aliasGenerator;

        public static Expression ExpandJoins(Expression expression, QueryBinder binder, bool cleanRequests)
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
            
            if (cleanRequests)
                binder.requests.Clear();

            return result;
        }

        protected internal override SourceExpression VisitSource(SourceExpression source)
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

                    var eq = SmartEqualizer.EqualNullable(tr.CompleteEntity.ExternalId, tr.CompleteEntity.GetBinding(EntityExpression.IdField))
                        .And(tr.CompleteEntity.ExternalPeriod.Overlaps(tr.CompleteEntity.TablePeriod));

                    Expression equal = DbExpressionNominator.FullNominate(eq);
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

        protected internal override Expression VisitProjection(ProjectionExpression proj)
        {
            SourceExpression source = (SourceExpression)this.VisitSource(proj.Select);
            Expression projector = this.Visit(proj.Projector);

            if (source == proj.Select && projector == proj.Projector)
                return proj;

            if (source is SelectExpression)
                return new ProjectionExpression((SelectExpression)source, projector, proj.UniqueFunction, proj.Type);

            Alias newAlias = aliasGenerator.NextSelectAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projector, newAlias); //Do not replace tokens

            var newSelect = new SelectExpression(newAlias, false, null, pc.Columns, source, null, null, null, 0);

            return new ProjectionExpression(newSelect, pc.Projector, proj.UniqueFunction, proj.Type);
        }

        protected internal override Expression VisitUpdate(UpdateExpression update)
        {
            var source = VisitSource(update.Source);
            var where = Visit(update.Where);
            var assigments = Visit(update.Assigments, VisitColumnAssigment);
            if (source != update.Source || where != update.Where || assigments != update.Assigments)
            {
                var select = (source as SourceWithAliasExpression) ?? WrapSelect(source);
                return new UpdateExpression(update.Table, update.UseHistoryTable, select, where, assigments);
            }
            return update;
        }

        protected internal override Expression VisitInsertSelect(InsertSelectExpression insertSelect)
        {
            var source = VisitSource(insertSelect.Source);
            var assigments = Visit(insertSelect.Assigments, VisitColumnAssigment);
            if (source != insertSelect.Source || assigments != insertSelect.Assigments)
            {
                var select = (source as SourceWithAliasExpression) ?? WrapSelect(source);
                return new InsertSelectExpression(insertSelect.Table, insertSelect.UseHistoryTable, select, assigments);
            }
            return insertSelect;
        }

        private SelectExpression WrapSelect(SourceExpression source)
        {
            Alias newAlias = aliasGenerator.NextSelectAlias();
            var select = new SelectExpression(newAlias, false, null, new ColumnDeclaration[0] /*Rebinder*/, source, null, null, null, 0);
            return select;
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
                        return new LiteReferenceExpression(Lite.Generate(entity.Type), entity, null, false, false);
                    }
                });
            }

            return CombineConditional(test, ifTrue, ifFalse) ?? c;
        }

        private Expression CombineConditional(Expression test, Expression ifTrue, Expression ifFalse)
        {
            if (colExpression is EntityExpression)
                return Combiner<EntityExpression>(ifTrue, ifFalse, (col, t, f) =>
                    new EntityExpression(col.Type,
                        new PrimaryKeyExpression(ConditionFlexible(test, t.ExternalId.Value.Nullify(), f.ExternalId.Value.Nullify())),
                        null, null, null, null, null, false));

            if (colExpression is ImplementedByExpression)
                return Combiner<ImplementedByExpression>(ifTrue, ifFalse, (col, t, f) =>
                    new ImplementedByExpression(col.Type,
                        col.Strategy,
                        col.Implementations.ToDictionary(a => a.Key, a => new EntityExpression(a.Key,
                            new PrimaryKeyExpression(ConditionFlexible(test,
                            t.Implementations[a.Key].ExternalId.Value.Nullify(),
                            f.Implementations[a.Key].ExternalId.Value.Nullify())), null, null, null, null, null, false))));

            if (colExpression is ImplementedByAllExpression)
                return Combiner<ImplementedByAllExpression>(ifTrue, ifFalse, (col, t, f) =>
                    new ImplementedByAllExpression(col.Type,
                        Expression.Condition(test, t.Id.Nullify(), f.Id.Nullify()),
                        new TypeImplementedByAllExpression(
                            new PrimaryKeyExpression(ConditionFlexible(test,
                                t.TypeId.TypeColumn.Value.Nullify(),
                                f.TypeId.TypeColumn.Value.Nullify()))), null));

            if (colExpression is EmbeddedEntityExpression)
                return Combiner<EmbeddedEntityExpression>(ifTrue, ifFalse, (col, t, f) =>
                   new EmbeddedEntityExpression(col.Type,
                       Expression.Condition(test, t.HasValue, f.HasValue),
                       col.Bindings.Select(bin => GetBinding(bin.FieldInfo, Expression.Condition(test, t.GetBinding(bin.FieldInfo).Nullify(), f.GetBinding(bin.FieldInfo).Nullify()), bin.Binding)),
                       col.FieldEmbedded,
                       col.ViewTable));

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
                            var entity = CombineCoalesce(l.Reference, r.Reference);
                            return new LiteReferenceExpression(Lite.Generate(entity.Type), entity, null, false, false);
                        }
                    });
                }

                return CombineCoalesce(left, right) ?? b;
            }

            return b;
        }

        private ConditionalExpression ConditionFlexible(Expression condition, Expression left, Expression right)
        {
            if (left.Type == right.Type)
            {
                return Expression.Condition(condition, left, right);
            }
            else if (left.Type.UnNullify() == right.Type.UnNullify())
            {
                return Expression.Condition(condition, left.Nullify(), right.Nullify());
            }
            else if (left.IsNull() && right.IsNull())
            {
                Type type = left.Type.Nullify();
                var newRight = DbExpressionNominator.ConvertNull(right, left.Type.Nullify());

                return Expression.Condition(condition, left, newRight);
            }
            else if (left.IsNull() || right.IsNull())
            {
                var newLeft = left.IsNull() ? DbExpressionNominator.ConvertNull(left, right.Type.Nullify()) : left.Nullify();
                var newRight = right.IsNull() ? DbExpressionNominator.ConvertNull(right, left.Type.Nullify()) : right.Nullify();

                return Expression.Condition(condition, newLeft, newRight);
            }

            throw new InvalidOperationException();
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType != ExpressionType.Convert && node.NodeType != ExpressionType.TypeAs)
                return base.VisitUnary(node);
            else
            {
                var operand = Visit(node.Operand);

                if (operand.Type == node.Type)
                    return operand;

                if (node.Operand == operand)
                    return node;

                return Expression.MakeUnary(node.NodeType, operand, node.Type);
            }
        }

        private Expression CombineCoalesce(Expression left, Expression right)
        {
            if (colExpression is EntityExpression)
                return Combiner<EntityExpression>(left, right, (col, l, r) =>
                    new EntityExpression(col.Type, new PrimaryKeyExpression(CoallesceFlexible(
                        l.ExternalId.Value.Nullify(),
                        r.ExternalId.Value.Nullify())), null, null, null, null, null, false));

            if (colExpression is ImplementedByExpression)
                return Combiner<ImplementedByExpression>(left, right, (col, l, r) =>
                    new ImplementedByExpression(col.Type,
                        col.Strategy,
                        col.Implementations.ToDictionary(a => a.Key, a => new EntityExpression(col.Type,
                            new PrimaryKeyExpression(CoallesceFlexible(
                            l.Implementations[a.Key].ExternalId.Value.Nullify(),
                            r.Implementations[a.Key].ExternalId.Value.Nullify())), null, null, null, null, null, false))));

            if (colExpression is ImplementedByAllExpression)
                return Combiner<ImplementedByAllExpression>(left, right, (col, l, r) =>
                    new ImplementedByAllExpression(col.Type,
                        Expression.Coalesce(l.Id, r.Id),
                        new TypeImplementedByAllExpression(new PrimaryKeyExpression(CoallesceFlexible(
                            l.TypeId.TypeColumn.Value.Nullify(),
                            r.TypeId.TypeColumn.Value.Nullify()))), null));

            if (colExpression is EmbeddedEntityExpression)
                return Combiner<EmbeddedEntityExpression>(left, right, (col, l, r) =>
                   new EmbeddedEntityExpression(col.Type,
                       Expression.Or(l.HasValue, r.HasValue),
                       col.Bindings.Select(bin => GetBinding(bin.FieldInfo, Expression.Coalesce(
                           l.GetBinding(bin.FieldInfo).Nullify(),
                           r.GetBinding(bin.FieldInfo).Nullify()), bin.Binding)),
                       col.FieldEmbedded,
                       col.ViewTable));

            return null;
        }


        private BinaryExpression CoallesceFlexible(Expression left, Expression right)
        {
            if (left.Type.UnNullify() == right.Type.UnNullify())
            {
                return Expression.Coalesce(left.Nullify(), right.Nullify());
            }
            else if (left.IsNull() || right.IsNull())
            {
                var newLeft = left.IsNull() ? DbExpressionNominator.ConvertNull(left, right.Type.Nullify()) : left.Nullify();
                var newRight = right.IsNull() ? DbExpressionNominator.ConvertNull(right, left.Type.Nullify()) : right.Nullify();

                return Expression.Coalesce(newLeft, newRight);
            }

            throw new InvalidOperationException();
        }

        public T Combiner<T>(Expression e1, Expression e2, Func<T, T, T, T> combiner) where T : Expression
        {
            Debug.Assert(e1.Type == colExpression.Type);
            Debug.Assert(e2.Type == colExpression.Type);

            var result = combiner((T)colExpression, (T)e1, (T)e2);

            Debug.Assert(result.Type == colExpression.Type);

            return result;
        }

        protected internal override Expression VisitLiteReference(LiteReferenceExpression lite)
        {
            if (!(colExpression is LiteReferenceExpression))
                throw new InvalidOperationException("colExpression should be a LiteReferenceExpression in this stage");

            var reference = ((LiteReferenceExpression)colExpression).Reference;

            var newRef = this.OverrideColExpression(reference).Using(_ => Visit(lite.Reference));
            if (newRef != lite.Reference)
                return new LiteReferenceExpression(Lite.Generate(newRef.Type), newRef, null, false, false);

            return lite;
        }

        protected internal override Expression VisitEntity(EntityExpression ee)
        {
            if (colExpression is ImplementedByAllExpression)
                return new ImplementedByAllExpression(colExpression.Type,
                    new SqlCastExpression(typeof(string), ee.ExternalId.Value),
                    new TypeImplementedByAllExpression(new PrimaryKeyExpression(
                        Expression.Condition(Expression.Equal(ee.ExternalId.Value.Nullify(), new SqlConstantExpression(null, ee.ExternalId.ValueType.Nullify())),
                        new SqlConstantExpression(null, PrimaryKey.Type(typeof(TypeEntity)).Nullify()),
                        QueryBinder.TypeConstant(ee.Type).Nullify()))), null);

            if (colExpression is ImplementedByExpression ib)
            {
                return new ImplementedByExpression(colExpression.Type, ib.Strategy, ib.Implementations.ToDictionary(kvp => kvp.Key, kvp =>
                    kvp.Key == ee.Type ? ee :
                    new EntityExpression(kvp.Key, QueryBinder.NullId(PrimaryKey.Type(kvp.Key).Nullify()), null, null, null, null, null, false)));
            }

            return ee;
        }

        protected internal override Expression VisitEmbeddedEntity(EmbeddedEntityExpression eee)
        {
            if (colExpression is EmbeddedEntityExpression)
                return eee;

            throw new NotImplementedException();
        }

        protected internal override Expression VisitImplementedBy(ImplementedByExpression ib)
        {
            if (colExpression is ImplementedByAllExpression)
            {
                return new ImplementedByAllExpression(colExpression.Type,
                    new PrimaryKeyExpression(QueryBinder.Coalesce(ib.Implementations.Values.Select(a => a.ExternalId.ValueType.Nullify()).Distinct().SingleEx(), ib.Implementations.Select(e => e.Value.ExternalId))),
                    new TypeImplementedByAllExpression(new PrimaryKeyExpression(
                     ib.Implementations.Select(imp => new When(imp.Value.ExternalId.NotEqualsNulll(), QueryBinder.TypeConstant(imp.Key))).ToList()
                     .ToCondition(PrimaryKey.Type(typeof(TypeEntity)).Nullify()))), null);
            }

            if (colExpression is ImplementedByExpression colId)
            {
                if (colId.Implementations.Keys.SequenceEqual(ib.Implementations.Keys))
                    return ib;

                return new ImplementedByExpression(colId.Type, ib.Strategy, colId.Implementations.ToDictionary(kvp => kvp.Key, kvp =>
                    ib.Implementations.TryGetC(kvp.Key) ??
                    new EntityExpression(kvp.Key, new PrimaryKeyExpression(Expression.Constant(null, PrimaryKey.Type(kvp.Key).Nullify())), null, null, null, null, null, false)));
            }

            return ib;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (colExpression is EntityExpression ||
                colExpression is ImplementedByExpression ||
                colExpression is ImplementedByAllExpression)
            {
                var ident = (Entity)c.Value;

                Type type = ident?.Let(a => PrimaryKey.Type(a.GetType()).Nullify()) ?? typeof(object);

                return GetEntityConstant(
                    ident == null ? Expression.Constant(null, type) : Expression.Constant(ident.Id.Object, type),
                    ident?.GetType());
            }

            if (colExpression is EmbeddedEntityExpression)
                return EmbeddedFromConstant(c);

            if (colExpression is LiteReferenceExpression colLite)
            {
                var lite = (Lite<IEntity>)c.Value;

                using (OverrideColExpression(colLite.Reference))
                {
                    Type type = lite?.Let(a => PrimaryKey.Type(a.EntityType).Nullify()) ?? typeof(object);

                    var entity = GetEntityConstant(
                        lite == null ? Expression.Constant(null, type) : Expression.Constant(lite.Id.Object, type),
                        lite?.GetType().CleanType());

                    return new LiteReferenceExpression(colLite.Type, entity, null, false, false);
                }
            }

            return c;
        }

        private Expression GetEntityConstant(Expression id, Type type)
        {
            if (colExpression is EntityExpression)
            {
                if (!id.IsNull() && type != colExpression.Type)
                    throw new InvalidOperationException("Impossible to convert {0} to {1}".FormatWith(type.TypeName(), colExpression.Type.TypeName()));

                return new EntityExpression(colExpression.Type, new PrimaryKeyExpression(id), null, null, null, null, null, false);
            }

            if (colExpression is ImplementedByAllExpression)
                return new ImplementedByAllExpression(colExpression.Type,
                    new SqlCastExpression(typeof(string), id),
                    new TypeImplementedByAllExpression(new PrimaryKeyExpression(id.IsNull() ?
                        Expression.Constant(null, PrimaryKey.Type(typeof(TypeEntity)).Nullify()) :
                        QueryBinder.TypeConstant(type).Nullify())), null);

            if (colExpression is ImplementedByExpression ib)
            {
                return new ImplementedByExpression(colExpression.Type, ib.Strategy,
                    ib.Implementations.ToDictionary(kvp => kvp.Key, kvp =>
                      new EntityExpression(kvp.Key, new PrimaryKeyExpression(type != kvp.Key ? Expression.Constant(null, id.Type.Nullify()) : id), null, null, null, null, null, false)));
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

            return new EmbeddedEntityExpression(contant.Type, Expression.Constant(value != null), bindings, embedded.FieldEmbedded, embedded.ViewTable);
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
                                fi.FieldType.IsValueType && !fi.FieldType.IsNullable() ? dic.GetOrThrow(fi.Name, "No value defined for non-nullable field {0}") :
                                (dic.TryGetC(fi.Name) ?? Expression.Constant(null, fi.FieldType)))
                            ).ToReadOnly();

            return new EmbeddedEntityExpression(init.Type, Expression.Constant(true), bindings, embedded.FieldEmbedded, embedded.ViewTable);
        }

        IDisposable OverrideColExpression(Expression newColExpression)
        {
            var save = this.colExpression;
            this.colExpression = newColExpression;

            return new Disposable(() => this.colExpression = save);
        }
    }

    [Serializable]
    public class CurrentSourceNotFoundException : Exception
    {
        public CurrentSourceNotFoundException() { }
        public CurrentSourceNotFoundException(string message) : base(message) { }
        public CurrentSourceNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected CurrentSourceNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
