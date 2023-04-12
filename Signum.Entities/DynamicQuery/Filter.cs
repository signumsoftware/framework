using Signum.Entities.DynamicQuery.Tokens;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;

namespace Signum.Entities.DynamicQuery;

[InTypeScript(true), DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description)]
public enum FilterGroupOperation
{
    And,
    Or,
}

public abstract class Filter
{
    public abstract Expression GetExpression(BuildExpressionContext ctx);

    public abstract IEnumerable<Filter> GetAllFilters();

    public abstract IEnumerable<QueryToken> GetTokens();

    public abstract Filter ToFullText();

    public abstract bool IsAggregate();

    protected Expression GetExpressionWithAnyAll(BuildExpressionContext ctx, CollectionAnyAllToken anyAll)
    {
        var ept = MListElementPropertyToken.AsMListEntityProperty(anyAll.Parent!);
        if (ept != null)
        {
            Expression collection = MListElementPropertyToken.BuildMListElements(ept, ctx);
            Type mleType = collection.Type.ElementType()!;

            var p = Expression.Parameter(mleType, mleType.Name.Substring(0, 1).ToLower());
            ctx.Replacements.Add(anyAll, new ExpressionBox(p, mlistElementRoute: anyAll.GetPropertyRoute()));
            var body = GetExpression(ctx);
            ctx.Replacements.Remove(anyAll);

            return anyAll.BuildAnyAll(collection, p, body);
        }
        else
        {
            Expression collection = anyAll.Parent!.BuildExpression(ctx);

            Type elementType = collection.Type.ElementType()!;

            var p = Expression.Parameter(elementType, elementType.Name.Substring(0, 1).ToLower());
            ctx.Replacements.Add(anyAll, new ExpressionBox(p.BuildLiteNullifyUnwrapPrimaryKey(new[] { anyAll.GetPropertyRoute()! })));
            var body = GetExpression(ctx);
            ctx.Replacements.Remove(anyAll);

            return anyAll.BuildAnyAll(collection, p, body);
        }
    }

    public static void SetIsTable(List<Filter> filters, IEnumerable<QueryToken> allTokens)
    {
        var fullTextOrders = allTokens.OfType<FullTextRankToken>().Select(a => a.Parent!);

        foreach (var fft in filters.OfType<FilterFullText>())
        {
            fft.IsTable = fft.Tokens.Any(t => fullTextOrders.Contains(t));
            fft.TableOuterJoin = false;
        }

        foreach (var fg in filters.OfType<FilterGroup>())
        {
            fg.SetIsTable(fullTextOrders, false);
        }
    }
}

public class FilterGroup : Filter
{
    public FilterGroupOperation GroupOperation { get; }
    public QueryToken? Token { get; }
    public List<Filter> Filters { get; }

    public FilterGroup(FilterGroupOperation groupOperation, QueryToken? token, List<Filter> filters)
    {
        this.GroupOperation = groupOperation;
        this.Token = token;
        this.Filters = filters;
    }

    public override IEnumerable<Filter> GetAllFilters()
    {
        return Filters.SelectMany(a => a.GetAllFilters()).PreAnd(this);
    }

    public override IEnumerable<QueryToken> GetTokens()
    {
        if (Token != null)
            yield return Token;
    }



    public override Expression GetExpression(BuildExpressionContext ctx)
    {
        var anyAll = Token?.Follow(a => a.Parent)
                .OfType<CollectionAnyAllToken>()
                .TakeWhile(c => !ctx.Replacements.ContainsKey(c))
                .LastOrDefault();

        if (anyAll == null)
            return this.GroupOperation == FilterGroupOperation.And ?
                Filters.Select(f => f.GetExpression(ctx)).AggregateAnd() :
                Filters.Select(f => f.GetExpression(ctx)).AggregateOr();

        return GetExpressionWithAnyAll(ctx, anyAll);
    }

    public override Filter ToFullText()
    {
        if (this.GroupOperation == FilterGroupOperation.Or)
        {
            var filters = Filters.ToList();

            var fullTextFilter = filters
            .OfType<FilterCondition>()
            .Where(fc => fc.Operation.IsFullTextFilterOperation())
            .ToList();

            var groups = fullTextFilter.GroupBy(a => new
            {
                ParentToken = a.Token.Follow(a => a.Parent).FirstOrDefault(a => a is MListElementPropertyToken || a.Type.IsLite()),
                Operation = a.Operation.ToFullTextFilterOperation(),
                a.Value,
            })
            .Select(gr => new FilterFullText(gr.Key.Operation, gr.Select(a => a.Token).ToList(), (string)gr.Key.Value!))
            .ToList();

            filters.RemoveAll(aa => fullTextFilter.Contains(aa));

            if (filters.Count == 0 && groups.Count == 1)
                return groups.SingleEx();

            filters.AddRange(groups);

            return new FilterGroup(FilterGroupOperation.Or, this.Token, filters);
        }
        else
        {
            return new FilterGroup(FilterGroupOperation.And, this.Token, this.Filters.Select(a => a.ToFullText()).ToList());
        }
    }


    public override string ToString()
    {
        return $@"{this.GroupOperation}{(this.Token != null ? $" ({this.Token})" : null)}
{Filters.ToString("\r\n").Indent(4)}";
    }

    public override bool IsAggregate()
    {
        return this.Filters.Any(f => f.IsAggregate());
    }

    internal void SetIsTable(IEnumerable<QueryToken> fullTextOrders, bool isOuter)
    {
        isOuter |= this.GroupOperation == FilterGroupOperation.Or && this.Filters.Count > 1;

        foreach (var fft in this.Filters.OfType<FilterFullText>())
        {
            fft.IsTable = fft.Tokens.Any(t => fullTextOrders.Contains(t));
            fft.TableOuterJoin = isOuter;
        }

        foreach (var fg in this.Filters.OfType<FilterGroup>())
        {
            fg.SetIsTable(fullTextOrders, isOuter);
        }
    }
}


public class FilterCondition : Filter
{
    public QueryToken Token { get; }
    public FilterOperation Operation { get; }
    public object? Value { get; }

    public FilterCondition(QueryToken token, FilterOperation operation, object? value)
    {
        this.Token = token;
        this.Operation = operation;
        this.Value = ReflectionTools.ChangeType(value, operation.IsList() ? typeof(IEnumerable<>).MakeGenericType(Token.Type.Nullify()) : Token.Type);
    }

    public override IEnumerable<Filter> GetAllFilters()
    {
        yield return this;
    }

    public override IEnumerable<QueryToken> GetTokens()
    {
        yield return Token;
    }

    public override Filter ToFullText()
    {
        if (Operation.IsFullTextFilterOperation())
            return new FilterFullText(Operation.ToFullTextFilterOperation(), new List<QueryToken> { Token }, (string?)Value!);

        return this;
    }

    static MethodInfo miContainsEnumerable = ReflectionTools.GetMethodInfo((IEnumerable<int> s) => s.Contains(2)).GetGenericMethodDefinition();

    public override Expression GetExpression(BuildExpressionContext ctx)
    {
        CollectionAnyAllToken? anyAll = Token.Follow(a => a.Parent)
              .OfType<CollectionAnyAllToken>()
              .TakeWhile(c => !ctx.Replacements.ContainsKey(c))
              .LastOrDefault();

        if (anyAll == null)
            return GetConditionExpressionBasic(ctx);

        return GetExpressionWithAnyAll(ctx, anyAll);

    }

   

    public static Func<bool> ToLowerString = () => false;

    private Expression GetConditionExpressionBasic(BuildExpressionContext context)
    {
        Expression left = Token.BuildExpression(context);

        if (Operation.IsList())
        {
            if (Value == null)
                return Expression.Constant(false);

            IList clone = (IList)Activator.CreateInstance(Value.GetType(), Value)!;

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

                if (ToLowerString())
                {
                    clone = clone.Cast<string>().Select(a => a.ToLower()).ToList();
                    left = Expression.Call(left, miToLower);
                }

                if (hasNull)
                {
                    clone.Add("");
                    left = Expression.Coalesce(left, Expression.Constant(""));
                }
            }

            Expression right = Expression.Constant(clone, typeof(IEnumerable<>).MakeGenericType(Token.Type.Nullify()));
            var contains = Expression.Call(miContainsEnumerable.MakeGenericMethod(Token.Type.Nullify()), right, left.Nullify());

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
            if (Token.Type == typeof(string) && (val == null || val is string str && string.IsNullOrEmpty(str)))
            {
                val ??= "";
                left = Expression.Coalesce(left, Expression.Constant(""));
            }


            if (Token.Type == typeof(string) && ToLowerString())
            {
                Expression right = Expression.Constant(((string)val!).ToLower(), Token.Type);
                return QueryUtils.GetCompareExpression(Operation, Expression.Call(left, miToLower), right);
            }
            else if (Token.Type == typeof(bool) && val == null)
            {
                return QueryUtils.GetCompareExpression(Operation, left, Expression.Constant(false, typeof(bool)));
            }
            else
            {
                Expression right = Expression.Constant(val, Token.Type);
                return QueryUtils.GetCompareExpression(Operation, left, right);
            }
        }
    }

    static MethodInfo miToLower = ReflectionTools.GetMethodInfo(() => "".ToLower());

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
    
    ComplexCondition, //Full Text Search
    FreeText, //Full Text Search
}

[InTypeScript(true)]
public enum FilterType
{
    Integer,
    Decimal,
    String,
    DateTime,
    Time,
    Lite,
    Embedded,
    Boolean,
    Enum,
    Guid,
}

public class FilterFullText : Filter
{
    public FilterFullText(FullTextFilterOperation operation, List<QueryToken> tokens, string value)
    {
        if (tokens.IsNullOrEmpty())
            throw new ArgumentNullException(nameof(tokens));

        if (value.IsNullOrEmpty())
            throw new ArgumentNullException(nameof(tokens));

        Tokens = tokens;
        Operation = operation;
        SearchCondition = value;
    }

    public List<QueryToken> Tokens { get; }

    public FullTextFilterOperation Operation { get; }
    public bool IsTable { get; set; }
    public bool TableOuterJoin { get; set; }

    public string SearchCondition { get; }

    public override Expression GetExpression(BuildExpressionContext ctx)
    {
        if (this.IsTable)
        {
            var rnk = ctx.Replacements.GetOrThrow(new FullTextRankToken(Tokens.FirstEx()));

            return Expression.NotEqual(rnk.RawExpression.Nullify(), Expression.Constant(null, typeof(int?)));
        }
        else
        {
            return Expression.Call(
                Operation == FullTextFilterOperation.ComplexCondition ? miContains : miFreeText,
                Expression.NewArrayInit(typeof(string),
                    Tokens.Select(t => t.BuildExpression(ctx, false)).ToArray()
                ),
                Expression.Constant(SearchCondition)
                );

        }
    }

    public static MethodInfo miContains = null!;
    public static MethodInfo miFreeText = null!;


    public override IEnumerable<Filter> GetAllFilters()
    {
        yield return this;
    }

    public override IEnumerable<QueryToken> GetTokens()
    {
        return Tokens;
    }

    public override bool IsAggregate()
    {
        return false;
    }

    public override Filter ToFullText()
    {
        throw new InvalidOperationException("Already FilterFullText!");
    }

    public static List<FilterFullText> TableFilters(List<Filter> filters)
    {
        return filters.SelectMany(a => a.GetAllFilters()).OfType<FilterFullText>().Where(a => a.IsTable).ToList();
    }
}

public static class FullTextFilterOperationExtensions
{

    public static FullTextFilterOperation ToFullTextFilterOperation(this FilterOperation fo)
    {
        return fo switch
        {
            FilterOperation.ComplexCondition => FullTextFilterOperation.ComplexCondition,
            FilterOperation.FreeText => FullTextFilterOperation.FreeText,
            _ => throw new UnexpectedValueException(fo)
        };
    }

    public static bool IsFullTextFilterOperation(this FilterOperation fo) => fo is FilterOperation.ComplexCondition or FilterOperation.FreeText;
}

public enum FullTextFilterOperation
{
    ComplexCondition, //Full Text Search
    FreeText, //Full Text Searc
}

[InTypeScript(true)]
public enum UniqueType
{
    First,
    FirstOrDefault,
    Single,
    SingleOrDefault,
    Only
}

[InTypeScript(true), DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description)]
public enum PinnedFilterActive
{
    Always,
    WhenHasValue,
    [Description("Checkbox (start checked)")]
    Checkbox_StartChecked,
    [Description("Checkbox (start unchecked)")]
    Checkbox_StartUnchecked,
    [Description("Not Checkbox (start checked)")]
    NotCheckbox_StartChecked,
    [Description("Not Checkbox (start unchecked)")]
    NotCheckbox_StartUnchecked,
}

[InTypeScript(true), DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description)]
public enum DashboardBehaviour
{
    //ShowAsPartFilter = 0, //Pinned Filter shown in the Part Widget
    PromoteToDasboardPinnedFilter = 1, //Pinned Filter promoted to dashboard
    UseAsInitialSelection, //Filters other parts in the same interaction group as if the user initially selected
    UseWhenNoFilters
}
