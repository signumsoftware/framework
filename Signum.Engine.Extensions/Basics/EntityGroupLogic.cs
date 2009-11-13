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
            public EntityGroupInfo(Func<T, bool> func, Expression<Func<T, bool>> expression, IQueryable<T> queryable)
            {
                Expression = expression;
                Func = func;
                Queryable = queryable;
            }

            public readonly IQueryable<T> Queryable; 
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
            infos.GetOrCreate(typeof(T))[entityGroupKey] = new EntityGroupInfo<T>(isInGroup.Compile(), isInGroup, null);
        }

        public static void Register<T>(Enum entityGroupKey, IQueryable<T> inGroupElements)
            where T : IdentifiableEntity
        {
            Expression<Func<T, bool>> exp = e => inGroupElements.Contains(e);

            infos.GetOrCreate(typeof(T))[entityGroupKey] = new EntityGroupInfo<T>(exp.Compile(), exp, inGroupElements);
        }

        [MethodExpander(typeof(IsInGroupExpander))]
        public static bool IsInGroup(this IdentifiableEntity entity, Enum entityGroupKey)
        {
            IEntityGroupInfo info = GetEntityGroupInfo(entityGroupKey, entity.GetType());

            return info.IsInGroup((IdentifiableEntity)entity);
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
            public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
            {
                ConstantExpression ce = arguments[1] as ConstantExpression;
                IEntityGroupInfo info = GetEntityGroupInfo((Enum)ce.Value, typeArguments[0]);

                return Expression.Call(arguments[0], "Where", typeArguments, info.UntypedExpression);
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

        public static IQueryable<T> GetInGroupQueryable<T>(Enum entityGroupKey)
            where T : IdentifiableEntity
        {
            var queryable = ((EntityGroupInfo<T>)GetEntityGroupInfo(entityGroupKey, typeof(T))).Queryable;
            return queryable;
        }
    }
}
