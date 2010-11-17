using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Signum.Entities.Basics;
using Signum.Entities;
using Signum.Utilities;
using Signum.Engine.Maps;
using System.Linq.Expressions;
using Signum.Entities.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using Signum.Utilities.DataStructures;
using Signum.Engine.Linq;
using Signum.Engine.Authorization;

namespace Signum.Engine.Basics
{
    internal interface IEntityGroupInfo
    {
        IPolyLambda IsInGroup { get; }
        IPolyLambda IsApplicable { get; }
    }

    internal interface IPolyLambda
    {
        bool Evaluate(IdentifiableEntity entity);
        LambdaExpression UntypedExpression { get; } 
        bool? Resume { get; }
    }

    public static class EntityGroupLogic
    {
        internal class EntityGroupInfo<T> : IEntityGroupInfo
            where T : IdentifiableEntity
        {
            public readonly PolyLambda IsInGroup;
            public readonly PolyLambda IsApplicable;

            IPolyLambda IEntityGroupInfo.IsInGroup { get { return IsInGroup; } }
            IPolyLambda IEntityGroupInfo.IsApplicable { get { return IsApplicable; } }

            public EntityGroupInfo(Expression<Func<T, bool>> isInGroup, Expression<Func<T, bool>> isApplicable)
            {
                if (isInGroup == null)
                    throw new ArgumentNullException("isInGroup");

                IsInGroup = new PolyLambda(isInGroup);

                if (isApplicable != null)
                    IsApplicable = new PolyLambda(isApplicable);
            }

            internal class PolyLambda : IPolyLambda
            {
                public readonly Expression<Func<T, bool>> Expression;
                readonly Expression<Func<T, bool>> FuncExpression; //debugging purposes only
                readonly Func<T, bool> Func;
                public bool? Resume { get; private set; }

                public PolyLambda(Expression<Func<T, bool>> expression)
                {
                    Resume = MakeResume(expression);
                    Expression = (Expression<Func<T, bool>>)DataBaseTransformer.ToDatabase(expression);

                    if (Resume == null)
                    {
                        FuncExpression = (Expression<Func<T, bool>>)MemoryTransformer.ToMemory(expression);
                        Func = FuncExpression.Compile();
                    }
                }

                static bool? MakeResume(LambdaExpression expression)
                {
                    if (expression.Body.NodeType == ExpressionType.Constant)
                        return (bool)((ConstantExpression)expression.Body).Value;

                    return null;
                }

                public bool Evaluate(IdentifiableEntity entity)
                {
                    if (Resume.HasValue)
                        return Resume.Value;

                    using (EntityGroupAuthLogic.DisableQueries())
                        return Func((T)entity);
                }

                public LambdaExpression UntypedExpression
                {
                    get { return Expression; }
                }
            }
        }

        static Dictionary<Type, Dictionary<Enum, IEntityGroupInfo>> infos = new Dictionary<Type, Dictionary<Enum, IEntityGroupInfo>>();

        public static HashSet<Enum> groups;
        public static HashSet<Enum> Groups
        {
            get { return groups ?? (groups = infos.SelectMany(a => a.Value.Keys).ToHashSet()); }
        }

        public static IEnumerable<Type> Types
        {
            get { return infos.Keys; }
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                EnumLogic<EntityGroupDN>.Start(sb, () => Groups);
            }
        }

        public static void Register<T>(Enum entityGroupKey, Expression<Func<T, bool>> isInGroup)
            where T : IdentifiableEntity
        {
            infos.GetOrCreate(typeof(T))[entityGroupKey] = new EntityGroupInfo<T>(isInGroup, null);
        }

        public static void Register<T>(Enum entityGroupKey, Expression<Func<T, bool>> isInGroup, Expression<Func<T, bool>> isApplicable)
            where T : IdentifiableEntity
        {
            infos.GetOrCreate(typeof(T))[entityGroupKey] = new EntityGroupInfo<T>(isInGroup, isApplicable);
        }


        [MethodExpander(typeof(IsInGroupExpander))]
        public static bool IsInGroup(this IdentifiableEntity entity, Enum entityGroupKey)
        {
            IEntityGroupInfo info = GetEntityGroupInfo(entityGroupKey, entity.GetType());

            return info.IsInGroup.Evaluate(entity);
        }


        class IsInGroupExpander : IMethodExpander
        {
            public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
            {
                Expression entity = arguments[0];
                Enum eg = (Enum)ExpressionEvaluator.Eval(arguments[1]);

                return Expression.Invoke(GetEntityGroupInfo(eg, entity.Type).IsInGroup.UntypedExpression, entity);
            }
        }


        [MethodExpander(typeof(IsApplicableExpander))]
        public static bool IsApplicable(this IdentifiableEntity entity, Enum entityGroupKey)
        {
            IEntityGroupInfo info = GetEntityGroupInfo(entityGroupKey, entity.GetType());

            return info.IsApplicable.Evaluate(entity);
        }


        class IsApplicableExpander : IMethodExpander
        {
            public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
            {
                Expression entity = arguments[0];
                Enum eg = (Enum)ExpressionEvaluator.Eval(arguments[1]);

                return Expression.Invoke(GetEntityGroupInfo(eg, entity.Type).IsApplicable.UntypedExpression, entity);
            }
        }

        [MethodExpander(typeof(WhereInGroupExpander))]
        public static IQueryable<T> WhereInGroup<T>(this IQueryable<T> query, Enum entityGroupKey)
            where T : IdentifiableEntity
        {
            EntityGroupInfo<T> info = (EntityGroupInfo<T>)GetEntityGroupInfo(entityGroupKey, typeof(T));

            if (info.IsInGroup.Resume == true)
                return query;

            return query.Where(info.IsInGroup.Expression);
        }

        class WhereInGroupExpander : IMethodExpander
        {
            static MethodInfo miWhere = ReflectionTools.GetMethodInfo(() => Queryable.Where<int>(null, i => i == 0)).GetGenericMethodDefinition();

            public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
            {
                Expression group = arguments[1];
                if (group.NodeType == ExpressionType.Convert)
                    group = ((UnaryExpression)group).Operand;

                Type type = typeArguments[0];

                Expression query = arguments[0]; 

                IEntityGroupInfo info = GetEntityGroupInfo((Enum)((ConstantExpression)group).Value, type);

                if (info.IsInGroup.Resume == true)
                    return query;

                return Expression.Call(null, miWhere.MakeGenericMethod(type), query, info.IsInGroup.UntypedExpression);
            }
        }

        [MethodExpander(typeof(WhereIsApplicableExpander))]
        public static IQueryable<T> WhereIsApplicable<T>(this IQueryable<T> query, Enum entityGroupKey)
            where T : IdentifiableEntity
        {
            EntityGroupInfo<T> info = (EntityGroupInfo<T>)GetEntityGroupInfo(entityGroupKey, typeof(T));

            if (info.IsApplicable.Resume == true)
                return query;

            return query.Where(info.IsApplicable.Expression);
        }

        class WhereIsApplicableExpander : IMethodExpander
        {
            static MethodInfo miWhere = ReflectionTools.GetMethodInfo(() => Queryable.Where<int>(null, i => i == 0)).GetGenericMethodDefinition();

            public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
            {
                Expression group = arguments[1];
                if (group.NodeType == ExpressionType.Convert)
                    group = ((UnaryExpression)group).Operand;

                Type type = typeArguments[0];

                Expression query = arguments[0];

                IEntityGroupInfo info = GetEntityGroupInfo((Enum)((ConstantExpression)group).Value, type);

                if (info.IsApplicable.Resume == true)
                    return query;

                return Expression.Call(null, miWhere.MakeGenericMethod(type), query, info.IsApplicable.UntypedExpression);
            }
        }

        public static IEnumerable<Enum> GroupsFor(Type type)
        {
            var dic = infos.TryGetC(type);
            if (dic != null)
                return dic.Keys;
            return NoGroups;
        }

        static readonly Enum[] NoGroups = new Enum[0];

        internal static IEntityGroupInfo GetEntityGroupInfo(Enum entityGroupKey, Type type)
        {
            IEntityGroupInfo info = infos
               .GetOrThrow(type, "There's no EntityGroup expression registered for type {0}")
               .GetOrThrow(entityGroupKey, "There's no EntityGroup expression registered for type {0} with key {{0}}".Formato(type));
            return info;
        }

        static MethodInfo miSmartRetrieve = ReflectionTools.GetMethodInfo(() => SmartRetrieve<IdentifiableEntity>(null)).GetGenericMethodDefinition();

        public static T SmartRetrieve<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            throw new InvalidOperationException("This method is ment to be used only in declaration of entity groups"); 
        }

        static MethodInfo miSmartTypeIs = ReflectionTools.GetMethodInfo(() => SmartTypeIs<IdentifiableEntity>(null)).GetGenericMethodDefinition();

        public static bool SmartTypeIs<T>(this Lite lite)
        {
            throw new InvalidOperationException("This method is ment to be used only in declaration of entity groups");
        }

        internal class DataBaseTransformer : SimpleExpressionVisitor
        {
            public static Expression ToDatabase(Expression exp)
            {
                DataBaseTransformer dbt = new DataBaseTransformer();
                return dbt.Visit(exp);
            }

            protected override Expression VisitMethodCall(MethodCallExpression m)
            {
                if (m.Method.IsInstantiationOf(miSmartRetrieve))
                {
                    return Expression.Property(base.Visit(m.Arguments[0]), "Entity");
                }
                else if (m.Method.IsInstantiationOf(miSmartTypeIs))
                {
                    return Expression.TypeIs(Expression.Property(base.Visit(m.Arguments[0]), "Entity"), m.Method.GetGenericArguments()[0]);
                }

                return base.VisitMethodCall(m);
            }
        }

        class SrChain
        {
            public SrChain(MethodCallExpression expression, SrChain parent)
            {
                this.Expression = expression;
                this.Parent = parent; 
            }

            public readonly MethodCallExpression Expression;
            public readonly SrChain Parent; 
        }

        class MemoryNominator : SimpleExpressionVisitor
        {
            class NominatorResult
            {
                public List<SrChain> FreeSRs = new List<SrChain>();
                public Dictionary<Expression, List<SrChain>> MINs = new Dictionary<Expression, List<SrChain>>();
            }

            NominatorResult data = new NominatorResult();

            public static Dictionary<Expression, List<SrChain>> Nominate(Expression exp)
            {
                MemoryNominator mn = new MemoryNominator();
                mn.Visit(exp);

                if (mn.data.FreeSRs.Any())
                    throw new InvalidOperationException("Impossible to transform SmartRetrieves in expression: {0}".Formato(exp.NiceToString()));
                    
                return mn.data.MINs;
            }

            protected override Expression Visit(Expression exp)
            {
                NominatorResult old = data;
                data = new NominatorResult();

                base.Visit(exp);

                if (data.MINs.Any())
                {
                    if (data.FreeSRs.Any())
                        throw new InvalidOperationException("Impossible to transform SmartRetrieves in expression: {0}".Formato(exp.NiceToString()));
                    else
                    {
                        old.MINs.AddRange(data.MINs);
                    }
                }
                else
                {
                    if (data.FreeSRs.Any())
                    {
                        if (exp.Type == typeof(bool))
                        {
                            old.MINs.Add(exp, data.FreeSRs);
                        }
                        else
                        {
                            if (old.FreeSRs.Count > 1) //Se nos mezclan cadenas
                                throw new InvalidOperationException("Impossible to transform SmartRetrieves in expression: {0}".Formato(exp.NiceToString()));

                            old.FreeSRs.AddRange(data.FreeSRs);
                        }
                    }
                    else
                    {
                        //nothing to do
                    }
                }
                
                data = old;

                return exp; 
            }

            protected override Expression VisitMethodCall(MethodCallExpression m)
            {
                base.VisitMethodCall(m);

                if (m.Method.IsInstantiationOf(miSmartRetrieve))
                {
                    if (data.FreeSRs.Count == 0)
                        data.FreeSRs.Add(new SrChain(m, null));
                    else //there should be only one
                        data.FreeSRs[0] = new SrChain(m, data.FreeSRs[0]);
                }

                return m; 
            }

        }

        internal class MemoryOnlyTransformer : SimpleExpressionVisitor
        {
            public static Expression ToMemory(Expression exp)
            {
                MemoryOnlyTransformer dbt = new MemoryOnlyTransformer();
                return dbt.Visit(exp);
            }

            protected override Expression VisitMethodCall(MethodCallExpression m)
            {
                if (m.Method.IsInstantiationOf(miSmartRetrieve))
                {
                    return Expression.Property(base.Visit(m.Arguments[0]), "Entity");
                }
                else if (m.Method.IsInstantiationOf(miSmartTypeIs))
                {
                    return Expression.Equal(Expression.Property(base.Visit(m.Arguments[0]), "RuntimeType"), Expression.Constant(m.Method.GetGenericArguments()[0], typeof(Type)));
                }

                return base.VisitMethodCall(m);
            }
        }

        internal class MemoryTransformer : SimpleExpressionVisitor
        {
            Dictionary<Expression, List<SrChain>> nominations;

            public static Expression ToMemory(Expression exp)
            {
                MemoryTransformer tr = new MemoryTransformer(){ nominations =  MemoryNominator.Nominate(exp)};

                return tr.Visit(exp);
            }

            protected override Expression Visit(Expression exp)
            {
                if (exp == null)
                    return null;

                List<SrChain> chains = nominations.TryGetC(exp);
                if (chains != null)
                    return Dispatch(exp, ImmutableStack<MethodCallExpression>.Empty, chains);

                return base.Visit(exp);
            }

            protected override Expression VisitMethodCall(MethodCallExpression m)
            {
                if (m.Method.IsInstantiationOf(miSmartTypeIs))
                {
                    return Expression.Equal(Expression.Property(base.Visit(m.Arguments[0]), "RuntimeType"), Expression.Constant(m.Method.GetGenericArguments()[0], typeof(Type)));
                }

                return base.VisitMethodCall(m);
            }

            static Expression Dispatch(Expression exp, ImmutableStack<MethodCallExpression> liteAssumptions,  IEnumerable<SrChain> chains)
            {
                if (chains.Empty())
                    return CaseBody(exp, liteAssumptions);


                return chains.First().FollowC(c => c.Parent).Select(c => c.Expression).Aggregate(Dispatch(exp, liteAssumptions, chains.Skip(1)),
                        (ac, sr) => Expression.Condition(LiteIsThin(DataBaseTransformer.ToDatabase(sr.Arguments[0])), Dispatch(exp, liteAssumptions.Push(sr), chains.Skip(1)), ac));

                //3 chains joinng in a Min not considered
            }

            static Expression LiteIsThin(Expression liteExpression)
            {
                return Expression.Equal(Expression.Property(liteExpression, "EntityOrNull"), 
                    Expression.Constant(null)); 
            }

            static MethodInfo miInDB = ReflectionTools.GetMethodInfo(() => Database.InDB<IdentifiableEntity>((Lite<IdentifiableEntity>)null)).GetGenericMethodDefinition();
            static MethodInfo miAny = ReflectionTools.GetMethodInfo(() => Queryable.Any<IdentifiableEntity>(null, null)).GetGenericMethodDefinition();

            static Expression CaseBody(Expression exp, ImmutableStack<MethodCallExpression> liteAsumptions)
            {
                if (liteAsumptions.Empty())
                    return MemoryOnlyTransformer.ToMemory(exp);

                AliasGenerator ag = new AliasGenerator();

                var dict = liteAsumptions.ToDictionary(sr => sr, sr => Expression.Parameter(sr.Type, ag.GetNextTableAlias(sr.Type)));

                var body = DataBaseTransformer.ToDatabase(SrReplacer.Replace(exp, dict)); // The only difference is the way SmartTypeIs works

                return dict.Aggregate(body, (acum, kvp)=>
                    Expression.Call(miAny.MakeGenericMethod(kvp.Key.Type),
                        Expression.Call(miInDB.MakeGenericMethod(kvp.Key.Type),
                         DataBaseTransformer.ToDatabase(kvp.Key.Arguments[0])),
                         Expression.Lambda(acum , kvp.Value)));
            }
        }

        public class SrReplacer : SimpleExpressionVisitor
        {
            Dictionary<MethodCallExpression, ParameterExpression> replacements = new Dictionary<MethodCallExpression, ParameterExpression>();

            public static Expression Replace(Expression expression, Dictionary<MethodCallExpression, ParameterExpression> replacements)
            {
                var replacer = new SrReplacer()
                {
                    replacements = replacements
                };

                return replacer.Visit(expression);
            }

            protected override Expression VisitMethodCall(MethodCallExpression m)
            {
                return replacements.TryGetC(m) ?? base.VisitMethodCall(m);
            }
        }
    }
}
