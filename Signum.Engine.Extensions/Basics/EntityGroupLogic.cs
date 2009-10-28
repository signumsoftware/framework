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

namespace Signum.Engine.Basics
{
    public static class EntityGroupLogic
    {
        interface IEntityGroupPair
        {
            bool IsInGroup(IdentifiableEntity entity);
            LambdaExpression UntypedExpression { get; }
        }

        class EntityGroupPair<T> : IEntityGroupPair
            where T : IdentifiableEntity
        {
            public EntityGroupPair(Expression<Func<T, bool>> inGroupExpression)
            {
                Expression = inGroupExpression;
                Func = inGroupExpression.Compile();
            }

            public readonly Expression<Func<T, bool>> Expression;
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

        static Dictionary<Type, Dictionary<Enum, IEntityGroupPair>> pairs = new Dictionary<Type, Dictionary<Enum, IEntityGroupPair>>();

        public static HashSet<Enum> groups;
        public static HashSet<Enum> Groups
        {
            get { return groups ?? (groups = pairs.SelectMany(a => a.Value.Keys).ToHashSet()); }
        }

        public static IEnumerable<Type> Types
        {
            get { return pairs.Keys; }
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
            pairs.GetOrCreate(typeof(T))[entityGroupKey] = new EntityGroupPair<T>(isInGroup);
        }

        [MethodExpander(typeof(IsInGroupExpander))]
        public static bool IsInGroup(this IdentifiableEntity entity, Enum entityGroupKey)
        {
            IEntityGroupPair pair = GetEntityGroupPair(entityGroupKey, entity.GetType());

            return pair.IsInGroup(entity);
        }

        class IsInGroupExpander : IMethodExpander
        {
            public Expression Expand(Expression instance, Expression[] arguments)
            {
                Expression entity = arguments[0];
                Enum eg = (Enum)ExpressionEvaluator.Eval(arguments[1]);

                return Expression.Invoke(GetInGroupExpression(entity.Type, eg), entity);
            }
        }

        public static IEnumerable<Enum> GroupsFor(Type type)
        {
            var dic = pairs.TryGetC(type);
            if (dic != null)
                return dic.Keys;
            return NoGroups;
        }

        static readonly Enum[] NoGroups = new Enum[0];

        static IEntityGroupPair GetEntityGroupPair(Enum entityGroupKey, Type type)
        {
            IEntityGroupPair pair = pairs
               .GetOrThrow(type, "There's no expression registered for type {{0}} on group {0}".Formato(entityGroupKey))
               .GetOrThrow(entityGroupKey, "There's no EntityGroup registered with key {0}");
            return pair;
        }

        public static Expression<Func<T, bool>> GetInGroupExpression<T>(Enum entityGroupKey)
            where T : IdentifiableEntity
        {
            var expression = ((EntityGroupPair<T>)GetEntityGroupPair(entityGroupKey, typeof(T))).Expression;
            return expression;
        }

        public static LambdaExpression GetInGroupExpression(Type type, Enum entityGroupKey)
        {
            return GetEntityGroupPair(entityGroupKey, type).UntypedExpression;
        }
    }
}
