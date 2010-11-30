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
                public bool? Resume { get; private set; }

                public PolyLambda(Expression<Func<T, bool>> expression)
                {
                    Expression = expression;
                    
                    if (expression.Body.NodeType == ExpressionType.Constant)
                        Resume =  (bool)((ConstantExpression)expression.Body).Value;
                    else 
                        Resume = null;                
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
            throw new InvalidProgramException("IsInGroup is meant to be used in database only");
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
            throw new InvalidOperationException("IsApplicable is meant to be used in database only"); 
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
                Type type = typeArguments[0];

                Expression query = arguments[0];
                Enum group = (Enum)ExpressionEvaluator.Eval(arguments[1]);
                
                IEntityGroupInfo info = GetEntityGroupInfo(group, type);

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
                Type type = typeArguments[0];

                Expression query = arguments[0];
                Enum group = (Enum)ExpressionEvaluator.Eval(arguments[1]);

                IEntityGroupInfo info = GetEntityGroupInfo(group, type);

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
    }
}
