using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System.Collections;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class Filter
    {
        QueryToken token;
        public QueryToken Token { get { return token; } }

        FilterOperation operation;
        public FilterOperation Operation { get { return operation; } }

        object value;
        public object Value { get { return value; } }

        public Filter(QueryToken token, FilterOperation operation, object value)
        {
            this.token = token;
            this.operation = operation;
            this.value = ReflectionTools.ChangeType(value, operation == FilterOperation.IsIn ? typeof(IEnumerable<>).MakeGenericType(Token.Type.Nullify()) : Token.Type);
        }

        static MethodInfo miContainsEnumerable = ReflectionTools.GetMethodInfo((IEnumerable<int> s) => s.Contains(2)).GetGenericMethodDefinition();
        static MethodInfo miContains = ReflectionTools.GetMethodInfo((string s) => s.Contains(s));
        static MethodInfo miStartsWith = ReflectionTools.GetMethodInfo((string s) => s.StartsWith(s));
        static MethodInfo miEndsWith = ReflectionTools.GetMethodInfo((string s) => s.EndsWith(s));
        static MethodInfo miLike = ReflectionTools.GetMethodInfo((string s) => s.Like(s));

        public void GenerateCondition(FilterBuildExpressionContext filterContext)
        {
            List<CollectionElementToken> allAny = Token.FollowC(a => a.Parent)
                .OfType<CollectionElementToken>()
                .Where(a => !a.CollectionElementType.IsElement())
                .Reverse()
                .ToList();

            List<IFilterExpression> filters = filterContext.Filters;
            foreach (var ct in allAny)
            {
                var allAnyFilter = filterContext.AllAnyFilters.GetOrCreate(ct, () =>
                {
                    var newAllAnyFilter = new AnyAllFilter(ct);

                    filterContext.Replacemens.Add(ct, ct.CreateExpression(newAllAnyFilter.Parameter));

                    filters.Add(newAllAnyFilter);

                    return newAllAnyFilter;
                });

                filters = allAnyFilter.Filters;
            }

            var exp = GetConditionBasic(filterContext);

            filters.Add(new FilterExpression(exp));
        }

        private Expression GetConditionBasic(BuildExpressionContext context)
        {
            Expression left = Token.BuildExpression(context);

            if (Operation == FilterOperation.IsIn)
            {
                if (Value == null)
                    return Expression.Constant(false);

                IList clone = (IList)Activator.CreateInstance(Value.GetType(), Value);

                bool hasNull = false;
                while (clone.Contains(null))
                {
                    clone.Remove(null);
                    hasNull = true;
                }

                Expression right = Expression.Constant(clone, typeof(IEnumerable<>).MakeGenericType(Token.Type.Nullify()));
                var contains =  Expression.Call(miContainsEnumerable.MakeGenericMethod(Token.Type.Nullify()), right, left.Nullify());

                if (!hasNull)
                    return contains;

                return Expression.Or(Expression.Equal(left, Expression.Constant(null, Token.Type.Nullify())), contains);
            }
            else
            {
                Expression right = Expression.Constant(Value, Token.Type);

                switch (Operation)
                {
                    case FilterOperation.EqualTo: return Expression.Equal(left, right);
                    case FilterOperation.DistinctTo: return Expression.NotEqual(left, right);
                    case FilterOperation.GreaterThan: return Expression.GreaterThan(left, right);
                    case FilterOperation.GreaterThanOrEqual: return Expression.GreaterThanOrEqual(left, right);
                    case FilterOperation.LessThan: return Expression.LessThan(left, right);
                    case FilterOperation.LessThanOrEqual: return Expression.LessThanOrEqual(left, right);
                    case FilterOperation.Contains: return Expression.Call(left, miContains, right);
                    case FilterOperation.StartsWith: return Expression.Call(left, miStartsWith, right);
                    case FilterOperation.EndsWith: return Expression.Call(left, miEndsWith, right);
                    case FilterOperation.Like: return Expression.Call(miLike, left, right);
                    case FilterOperation.NotContains: return Expression.Not(Expression.Call(left, miContains, right));
                    case FilterOperation.NotStartsWith: return Expression.Not(Expression.Call(left, miStartsWith, right));
                    case FilterOperation.NotEndsWith: return Expression.Not(Expression.Call(left, miEndsWith, right));
                    case FilterOperation.NotLike: return Expression.Not(Expression.Call(miLike, left, right));
                    default:
                        throw new InvalidOperationException("Unknown operation {0}".Formato(Operation));
                }
            }
        }

        public override string ToString()
        {
            return "{0} {1} {2}".Formato(Token.FullKey(), Operation, Value);
        }
    }

    public enum FilterOperation
    {
        EqualTo,
        DistinctTo,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        StartsWith,
        EndsWith,
        Like,
        NotContains,
        NotStartsWith,
        NotEndsWith,
        NotLike,
        IsIn,
    }

    public enum FilterType
    {
        Integer,
        Decimal,
        String, 
        DateTime,
        Lite,
        Embedded,
        Boolean, 
        Enum,
        Guid,
    }

    public enum UniqueType
    {
        First,
        FirstOrDefault,
        Single,
        SingleOrDefault,
        SingleOrMany, 
        Only
    }
}
