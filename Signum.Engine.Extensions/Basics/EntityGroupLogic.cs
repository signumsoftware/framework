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
    public static class EntityGroupLogic
    {
        static Dictionary<Type, Dictionary<Enum, LambdaExpression>> infos = new Dictionary<Type, Dictionary<Enum, LambdaExpression>>();

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
            infos.GetOrCreate(typeof(T))[entityGroupKey] = isInGroup;
        }

        [MethodExpander(typeof(IsInGroupExpander))]
        public static bool IsInGroup(this IdentifiableEntity entity, Enum entityGroupKey)
        {
            throw new InvalidProgramException("IsInGroup is meant to be used in database only");
        }


        class IsInGroupExpander : IMethodExpander
        {
            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                Expression entity = arguments[0];
                Enum eg = (Enum)ExpressionEvaluator.Eval(arguments[1]);

                var exp = GetEntityGroupExpression(eg, entity.Type);

                return Expression.Invoke(exp, entity);
            }
        }


        [MethodExpander(typeof(WhereInGroupExpander))]
        public static IQueryable<T> WhereInGroup<T>(this IQueryable<T> query, Enum entityGroupKey)
            where T : IdentifiableEntity
        {
            Expression<Func<T, bool>> exp = (Expression<Func<T, bool>>)GetEntityGroupExpression(entityGroupKey, typeof(T));

            if (exp.SimpleValue() == true)
                return query;

            return query.Where(exp);
        }

        class WhereInGroupExpander : IMethodExpander
        {
            static MethodInfo miWhere = ReflectionTools.GetMethodInfo(() => Queryable.Where<int>(null, i => i == 0)).GetGenericMethodDefinition();

            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {  
                Type type = mi.GetGenericArguments()[0];

                Expression query = arguments[0];
                Enum group = (Enum)ExpressionEvaluator.Eval(arguments[1]);
                
                LambdaExpression exp = GetEntityGroupExpression(group, type);

                if (exp.SimpleValue() == true)
                    return query;

                return Expression.Call(null, miWhere.MakeGenericMethod(type), query, exp);
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

        internal static LambdaExpression GetEntityGroupExpression(Enum entityGroupKey, Type type)
        {
            LambdaExpression exp = infos
               .GetOrThrow(type, "There's no EntityGroup expression registered for type {0}")
               .GetOrThrow(entityGroupKey, "There's no EntityGroup expression registered for type {0} with key {{0}}".Formato(type));
            return exp;
        }

        internal static LambdaExpression TryEntityGroupExpression(Enum entityGroupKey, Type type)
        {
            LambdaExpression exp = infos.TryGetC(type).TryGetC(entityGroupKey);
            return exp;
        }

        internal static bool? SimpleValue(this Expression exp)
        {
            var ce = exp as ConstantExpression;

            if (ce == null)
                return null;

            return (bool)ce.Value;
        }
    }
}
