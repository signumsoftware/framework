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
    public static class TypeConditionLogic
    {
        static Dictionary<Type, Dictionary<TypeConditionSymbol, LambdaExpression>> infos = new Dictionary<Type, Dictionary<TypeConditionSymbol, LambdaExpression>>();

        public static IEnumerable<Type> Types
        {
            get { return infos.Keys; }
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                SymbolLogic<TypeConditionSymbol>.Start(sb, () => infos.SelectMany(a => a.Value.Keys).ToHashSet());
            }
        }

        public static void Register<T>(TypeConditionSymbol typeCondition, Expression<Func<T, bool>> condition)
            where T : IdentifiableEntity
        {
            if (typeCondition == null)
                throw new ArgumentNullException("typeCondition");

            if (condition == null)
                throw new ArgumentNullException("condition");

            infos.GetOrCreate(typeof(T))[typeCondition] = condition;
        }

        [MethodExpander(typeof(InConditionExpander))]
        public static bool InCondition(this IdentifiableEntity entity, TypeConditionSymbol typeCondition)
        {
            throw new InvalidProgramException("InCondition is meant to be used in database only");
        }

        class InConditionExpander : IMethodExpander
        {
            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                Expression entity = arguments[0];
                TypeConditionSymbol typeCondition = (TypeConditionSymbol)ExpressionEvaluator.Eval(arguments[1]);

                var exp = GetExpression(entity.Type, typeCondition);

                return Expression.Invoke(exp, entity);
            }
        }


        [MethodExpander(typeof(WhereConditionExpander))]
        public static IQueryable<T> WhereCondition<T>(this IQueryable<T> query, TypeConditionSymbol typeCondition)
            where T : IdentifiableEntity
        {
            Expression<Func<T, bool>> exp = (Expression<Func<T, bool>>)GetExpression(typeof(T), typeCondition);

            return query.Where(exp);
        }

        class WhereConditionExpander : IMethodExpander
        {
            static MethodInfo miWhere = ReflectionTools.GetMethodInfo(() => Queryable.Where<int>(null, i => i == 0)).GetGenericMethodDefinition();

            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {  
                Type type = mi.GetGenericArguments()[0];

                Expression query = arguments[0];
                TypeConditionSymbol typeCondition = (TypeConditionSymbol)ExpressionEvaluator.Eval(arguments[1]);

                LambdaExpression exp = GetExpression(type, typeCondition);

                return Expression.Call(null, miWhere.MakeGenericMethod(type), query, exp);
            }
        }

        public static IEnumerable<TypeConditionSymbol> ConditionsFor(Type type)
        {
            var dic = infos.TryGetC(type);
            if (dic == null)
                return Enumerable.Empty<TypeConditionSymbol>();

            return dic.Keys;
        }

        public static LambdaExpression GetExpression(Type type, TypeConditionSymbol typeCondition)
        {
            var expression = infos.GetOrThrow(type, "There's no TypeCondition registered for type {0}").TryGetC(typeCondition);

            if (expression == null)
                throw new KeyNotFoundException("There's no TypeCondition registered for type {0} with key {1}".Formato(type, typeCondition));

            return expression;
        }

        public static bool IsDefined(Type type, TypeConditionSymbol typeCondition)
        {
            return infos.TryGetC(type).TryGetC(typeCondition) != null;
        }
    }
}
