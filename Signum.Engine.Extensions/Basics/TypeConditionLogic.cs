using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Signum.Entities.Basics;
using Signum.Entities;
using Signum.Utilities;
using Signum.Engine.Maps;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Basics
{
    public class TypeConditionPair
    {
        public TypeConditionPair(LambdaExpression condition, Delegate? inMemoryCondition)
        {
            if (condition == null)
                throw new ArgumentNullException("lambda");

            this.Condition = condition;
            this.InMemoryCondition = inMemoryCondition;
        }

        public LambdaExpression Condition;
        public Delegate? InMemoryCondition;
    }

    public static class TypeConditionLogic
    {
        static Dictionary<Type, Dictionary<TypeConditionSymbol, TypeConditionPair>> infos = new Dictionary<Type, Dictionary<TypeConditionSymbol, TypeConditionPair>>();


        static readonly Variable<Dictionary<Type, Dictionary<TypeConditionSymbol, LambdaExpression>>?> tempConditions =
            Statics.ThreadVariable<Dictionary<Type, Dictionary<TypeConditionSymbol, LambdaExpression>>?>("tempConditions");

        public static IDisposable ReplaceTemporally<T>(TypeConditionSymbol typeAllowed, Expression<Func<T, bool>> condition)
            where T : Entity
        {
            var dic = tempConditions.Value ?? (tempConditions.Value = new Dictionary<Type, Dictionary<TypeConditionSymbol, LambdaExpression>>());

            var subDic = dic.GetOrCreate(typeof(T));

            subDic.Add(typeAllowed, condition);

            return new Disposable(() =>
            {
                subDic.Remove(typeAllowed);

                if (subDic.Count == 0)
                    dic.Remove(typeof(T));

                if (dic.Count == 0)
                    tempConditions.Value = null;
            });
        }

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
              where T : Entity
        {
            Register<T>(typeCondition, condition, null);
        }

        public static void RegisterCompile<T>(TypeConditionSymbol typeCondition, Expression<Func<T, bool>> condition)
              where T : Entity
        {
            Register<T>(typeCondition, condition, condition.Compile());
        }

        public static void Register<T>(TypeConditionSymbol typeCondition, Expression<Func<T, bool>> condition, Func<T, bool>? inMemoryCondition)
            where T : Entity
        {
            if (typeCondition == null)
                throw AutoInitAttribute.ArgumentNullException(typeof(TypeConditionSymbol), nameof(typeCondition));

            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            infos.GetOrCreate(typeof(T))[typeCondition] = new TypeConditionPair(condition, inMemoryCondition);
        }

        [MethodExpander(typeof(InConditionExpander))]
        public static bool InCondition(this Entity entity, TypeConditionSymbol typeCondition)
        {
            throw new InvalidProgramException("InCondition is meant to be used in database only");
        }

        class InConditionExpander : IMethodExpander
        {
            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                Expression entity = arguments[0];
                TypeConditionSymbol typeCondition = (TypeConditionSymbol)ExpressionEvaluator.Eval(arguments[1])!;

                var exp = GetCondition(entity.Type, typeCondition);

                return Expression.Invoke(exp, entity);
            }
        }


        [MethodExpander(typeof(WhereConditionExpander))]
        public static IQueryable<T> WhereCondition<T>(this IQueryable<T> query, TypeConditionSymbol typeCondition)
            where T : Entity
        {
            Expression<Func<T, bool>> exp = (Expression<Func<T, bool>>)GetCondition(typeof(T), typeCondition);

            return query.Where(exp);
        }

        class WhereConditionExpander : IMethodExpander
        {
            static MethodInfo miWhere = ReflectionTools.GetMethodInfo(() => Queryable.Where<int>(null, i => i == 0)).GetGenericMethodDefinition();

            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                Type type = mi.GetGenericArguments()[0];

                Expression query = arguments[0];
                TypeConditionSymbol typeCondition = (TypeConditionSymbol)ExpressionEvaluator.Eval(arguments[1])!;

                LambdaExpression exp = GetCondition(type, typeCondition);

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

        public static LambdaExpression GetCondition(Type type, TypeConditionSymbol typeCondition)
        {
            var tempExpr = tempConditions.Value?.TryGetC(type)?.TryGetC(typeCondition);
            if (tempExpr != null)
                return tempExpr;

            var pair = infos.GetOrThrow(type, "There's no TypeCondition registered for type {0}").GetOrThrow(typeCondition);

            return pair.Condition;
        }

        public static Func<T, bool>? GetInMemoryCondition<T>(TypeConditionSymbol typeCondition)
            where T : Entity
        {
            var pair = infos.GetOrThrow(typeof(T), "There's no TypeCondition registered for type {0}").GetOrThrow(typeCondition);

            return (Func<T, bool>?)pair.InMemoryCondition;
        }

        public static bool IsDefined(Type type, TypeConditionSymbol typeCondition)
        {
            return infos.TryGetC(type)?.TryGetC(typeCondition) != null;
        }
    }
}
