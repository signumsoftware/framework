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

namespace Signum.Engine.Basics
{
    public static class EntityGroupLogic
    {
        interface IEntityGroupInfo
        {
            bool IsInGroup(IdentifiableEntity entity);
            LambdaExpression UntypedExpression { get; }
        }

        class EntityGroupInfo<T> : IEntityGroupInfo
            where T : IdentifiableEntity
        {
            public EntityGroupInfo(Expression<Func<T, bool>> expression)
            {
                Expression = (Expression<Func<T, bool>>)DataBaseTransformer.ToDatabase(expression);
                FuncExpression = (Expression<Func<T, bool>>)MemoryTransformer.ToDatabase(expression);
                Func = FuncExpression.Compile(); 
            }

            public readonly Expression<Func<T, bool>> Expression;
            
            readonly Expression<Func<T, bool>> FuncExpression; //debugging purposes only
            public readonly Func<T, bool> Func;

            public bool IsInGroup(IdentifiableEntity entity)
            {
                return Func((T)entity);
            }

            public LambdaExpression UntypedExpression
            {
                get { return Expression; }
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
            infos.GetOrCreate(typeof(T))[entityGroupKey] = new EntityGroupInfo<T>(isInGroup);
        }

        [MethodExpander(typeof(IsInGroupExpander))]
        public static bool IsInGroup(this IdentifiableEntity entity, Enum entityGroupKey)
        {
            IEntityGroupInfo info = GetEntityGroupInfo(entityGroupKey, entity.GetType());

            return info.IsInGroup(entity);
        }

        class IsInGroupExpander : IMethodExpander
        {
            public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
            {
                Expression entity = arguments[0];
                Enum eg = (Enum)ExpressionEvaluator.Eval(arguments[1]);

                return Expression.Invoke(GetInGroupExpression(entity.Type, eg), entity);
            }
        }

        [MethodExpander(typeof(WhereInGroupExpander))]
        public static IQueryable<T> WhereInGroup<T>(this IQueryable<T> query, Enum entityGroupKey)
            where T : IdentifiableEntity
        {
            EntityGroupInfo<T> info = (EntityGroupInfo<T>)GetEntityGroupInfo(entityGroupKey, typeof(T));

            return query.Where(info.Expression);
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

                IEntityGroupInfo info = GetEntityGroupInfo((Enum)((ConstantExpression)group).Value, type);

                return Expression.Call(null, miWhere.MakeGenericMethod(type), arguments[0], info.UntypedExpression);
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

        static IEntityGroupInfo GetEntityGroupInfo(Enum entityGroupKey, Type type)
        {
            IEntityGroupInfo info = infos
               .GetOrThrow(type, "There's no expression registered for type {{0}} on group {0}".Formato(entityGroupKey))
               .GetOrThrow(entityGroupKey, "There's no EntityGroup registered with key {0}");
            return info;
        }

        public static Expression<Func<T, bool>> GetInGroupExpression<T>(Enum entityGroupKey)
            where T : IdentifiableEntity
        {
            var expression = ((EntityGroupInfo<T>)GetEntityGroupInfo(entityGroupKey, typeof(T))).Expression;
            return expression;
        }

        public static LambdaExpression GetInGroupExpression(Type type, Enum entityGroupKey)
        {
            return GetEntityGroupInfo(entityGroupKey, type).UntypedExpression;
        }


        static MethodInfo miSmartRetrieve = ReflectionTools.GetMethodInfo(() => SmartRetrieve<IdentifiableEntity>(null)).GetGenericMethodDefinition();

        public static T SmartRetrieve<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            throw new InvalidOperationException("This methid is ment to be used only in declaration of entity groups"); 
        }

        internal class DataBaseTransformer : ExpressionVisitor
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

        class MemoryNominator : ExpressionVisitor
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


        internal class MemoryTransformer : ExpressionVisitor
        {
            Dictionary<Expression, List<SrChain>> nominations;

            public static Expression ToDatabase(Expression exp)
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
                AliasGenerator ag = new AliasGenerator();

                var dict = liteAsumptions.ToDictionary(sr => sr, sr => Expression.Parameter(sr.Type, ag.GetNextTableAlias(sr.Type))); 

                var body = DataBaseTransformer.ToDatabase(SrReplacer.Replace(exp,dict)); 

                return dict.Aggregate(body, (acum, kvp)=>
                    Expression.Call(miAny.MakeGenericMethod(kvp.Key.Type),
                        Expression.Call(miInDB.MakeGenericMethod(kvp.Key.Type),
                         DataBaseTransformer.ToDatabase(kvp.Key.Arguments[0])),
                         Expression.Lambda(acum , kvp.Value)));
            }
        }

        public class SrReplacer : ExpressionVisitor
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

// Ej 1
// p => p.Coche.SR().Color == Rojo
// p => p.Coche.EoN == null? p.Coche.InDB().Any(c=>c.Color == Rojo)
//      p.Coche.E.Color == Rojo

// Ej 2
// p => p.Coche.SR().Color == Rojo && IsWeekend()
// p => (p.Coche.EoN == null ? p.Coche.InDB().Any(c=>c.Color == Rojo)
//       p.Coche.E.Color == Rojo) && && IsWeekend()

// Ej 3
// p => p.Coche.SR().Marca.SR().Nombre == "M"
// p => p.Coche.EoN == null? p.Coche.InDB().Any(c=>c.Marca.E.Nombre == "M")
//      p.Coche.E.Marca.EoN == null? p.Coche.E.Marca.InDB().Any(m=>m.Nombre == "M")
//      p.Coche.E.Marca.E.Nombre == "M"

// Ej 4
// p => p.Coche.SR().Marca.SR().Nombre == "M"
// p => p.Coche.EoN == null? p.Coche.InDB().Any(c=>c.Marca.E.Nombre == "M")
//      p.Coche.E.Marca.EoN == null? p.Coche.E.Marca.InDB().Any(m=>m.Nombre == "M")
//      p.Coche.E.Marca.E.Nombre == "M"

// Ej 5
// p => p.Coche.SR().Marca == Yo.Coche.SR().Marca
// p => p.Coche.EoN == null? (Yo.Coche.EoN == null ? p.Coche.InDB().SM(Yo.Coche.InDB(),(c1, c2)=>c1.Marca == c2.Marca).Any()
//              p.Coche.InDb().Any(c=>c.Marca == yo.Coche.E.Marca)) : 
//             (Yo.Coche.Eon == null ? Yo.Coche.InDB().Any(c => c.Marca == p.Coche.E.Marca)
//                      p.Coche.E.Marca == Yo.Coche.E.Marca)   

    }
}
