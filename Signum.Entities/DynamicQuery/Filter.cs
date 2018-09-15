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
    [InTypeScript(true), DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description)]
    public enum FilterGroupOperation
    {
        And,
        Or,
    }

    public abstract class Filter
    {
        public abstract Expression GetExpression(BuildExpressionContext ctx);

        public abstract IEnumerable<FilterCondition> GetFilterConditions();
        
        public abstract bool IsAggregate(); 
    }

    [Serializable]
    public class FilterGroup : Filter
    {
        public FilterGroupOperation GroupOperation { get; }
        public QueryToken Token { get; }
        public List<Filter> Filters { get; }

        public FilterGroup(FilterGroupOperation groupOperation, QueryToken token, List<Filter> filters)
        {
            this.GroupOperation = groupOperation;
            this.Token = token;
            this.Filters = filters;
        }

        public override IEnumerable<FilterCondition> GetFilterConditions()
        {
            return Filters.SelectMany(a => a.GetFilterConditions());
        }
        
        public override Expression GetExpression(BuildExpressionContext ctx)
        {
            var anyAll = Token as CollectionAnyAllToken;
            if(anyAll == null)
            {
                return this.GroupOperation == FilterGroupOperation.And ?
                    Filters.Select(f => f.GetExpression(ctx)).AggregateAnd() :
                    Filters.Select(f => f.GetExpression(ctx)).AggregateOr();
            }
            else
            {
                Expression collection = anyAll.Parent.BuildExpression(ctx);
                Type elementType = collection.Type.ElementType();

                var p = Expression.Parameter(elementType, elementType.Name.Substring(0, 1).ToLower());
                ctx.Replacemens.Add(anyAll, p);

                var body = this.GroupOperation == FilterGroupOperation.And ?
                    Filters.Select(f => f.GetExpression(ctx)).AggregateAnd() :
                    Filters.Select(f => f.GetExpression(ctx)).AggregateOr();

                ctx.Replacemens.Remove(anyAll);

                return anyAll.BuildAnyAll(collection, p, body);
            }
        }

        public override string ToString()
        {
            return $@"{this.GroupOperation}{(this.Token != null ? $" ({this.Token})": null)}
{Filters.ToString("\r\n").Indent(4)}";
        }

        public override bool IsAggregate()
        {
            return this.Filters.Any(f => f.IsAggregate());
        }
    }


    [Serializable]
    public class FilterCondition : Filter
    {
        public QueryToken Token { get; }
        public FilterOperation Operation { get; }
        public object Value { get; }

        public FilterCondition(QueryToken token, FilterOperation operation, object value)
        {
            this.Token = token;
            this.Operation = operation;
            this.Value = ReflectionTools.ChangeType(value, operation.IsList() ? typeof(IEnumerable<>).MakeGenericType(Token.Type.Nullify()) : Token.Type);
        }

        public override IEnumerable<FilterCondition> GetFilterConditions()
        {
            yield return this;
        }

        static MethodInfo miContainsEnumerable = ReflectionTools.GetMethodInfo((IEnumerable<int> s) => s.Contains(2)).GetGenericMethodDefinition();

        public override Expression GetExpression(BuildExpressionContext ctx)
        {
            CollectionAnyAllToken anyAll = Token.Follow(a => a.Parent)
                  .OfType<CollectionAnyAllToken>()
                  .TakeWhile(c => !ctx.Replacemens.ContainsKey(c))
                  .LastOrDefault();

            if (anyAll == null)
                return GetConditionExpressionBasic(ctx);

            Expression collection = anyAll.Parent.BuildExpression(ctx);
            Type elementType = collection.Type.ElementType();

            var p = Expression.Parameter(elementType, elementType.Name.Substring(0, 1).ToLower());
            ctx.Replacemens.Add(anyAll, p.BuildLiteNulifyUnwrapPrimaryKey(new[] { anyAll.GetPropertyRoute() }));
            var body = GetExpression(ctx);
            ctx.Replacemens.Remove(anyAll);

            return anyAll.BuildAnyAll(collection, p, body);
        }

        private Expression GetConditionExpressionBasic(BuildExpressionContext context)
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

                if (Token.Type == typeof(string))
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


                var result = !hasNull || Token.Type == typeof(string) ? (Expression)contains :
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
                if (Token.Type == typeof(string) && (val == null || val is string && string.IsNullOrEmpty((string)val)))
                {
                    val = val ?? "";
                    left = Expression.Coalesce(left, Expression.Constant(""));
                }

                Expression right = Expression.Constant(val, Token.Type);

                return QueryUtils.GetCompareExpression(Operation, left, right);
            }
        }

        public override bool IsAggregate()
        {
            return this.Token is AggregateToken;
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
