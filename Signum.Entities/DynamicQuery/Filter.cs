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
            this.value = ReflectionTools.ChangeType(value, operation.IsList() ? typeof(IEnumerable<>).MakeGenericType(Token.Type.Nullify()) : Token.Type);
        }

        static MethodInfo miContainsEnumerable = ReflectionTools.GetMethodInfo((IEnumerable<int> s) => s.Contains(2)).GetGenericMethodDefinition();
     
        public void GenerateCondition(FilterBuildExpressionContext filterContext)
        {
            List<CollectionElementToken> allAny = Token.Follow(a => a.Parent)
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

            if (Operation.IsList())
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

                if (token.Type == typeof(string))
                {
                    while (clone.Contains(""))
                    {
                        clone.Remove("");
                        hasNull = true;
                    }

                    if (hasNull)
                    {
                        clone.Add("");
                        left = Expression.Coalesce(left, Expression.Constant(""));
                    }
                }


                Expression right = Expression.Constant(clone, typeof(IEnumerable<>).MakeGenericType(Token.Type.Nullify()));
                var contains =  Expression.Call(miContainsEnumerable.MakeGenericMethod(Token.Type.Nullify()), right, left.Nullify());


                var result = !hasNull || token.Type == typeof(string) ? (Expression)contains :
                        Expression.Or(Expression.Equal(left, Expression.Constant(null, Token.Type.Nullify())), contains);


                if (Operation == FilterOperation.IsIn)
                    return result;

                if (Operation == FilterOperation.IsNotIn)
                    return Expression.Not(result);

                throw new InvalidOperationException("Unexpected operation");
            }
            else
            {
                var val = Value;
                if (token.Type == typeof(string) && (val == null || val is string && string.IsNullOrEmpty((string)val)))
                {
                    val = val ?? "";
                    left = Expression.Coalesce(left, Expression.Constant(""));
                }

                Expression right = Expression.Constant(val, Token.Type);

                return QueryUtils.GetCompareExpression(Operation, left, right);
            }
        }


        public override string ToString()
        {
            return "{0} {1} {2}".FormatWith(Token.FullKey(), Operation, Value);
        }
    }

    [InTypeScript(true), DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description)]
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
        IsNotIn,
    }

    [InTypeScript(true)]
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

    [InTypeScript(true)]
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
