using Microsoft.Extensions.Primitives;
using NpgsqlTypes;
using Signum.DynamicQuery.Tokens;
using Signum.Entities.TsVector;
using Signum.Utilities.Reflection;
using System.Collections;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Signum.DynamicQuery;

[InTypeScript(true), DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description)]
public enum FilterGroupOperation
{
    And,
    Or,
}

public abstract class Filter
{
    public abstract Expression GetExpression(BuildExpressionContext ctx, bool inMemory);

    public abstract IEnumerable<Filter> GetAllFilters();

    public abstract IEnumerable<QueryToken> GetTokens();

    public abstract Filter? ToFullText();

    public abstract bool IsAggregate();
    public abstract bool IsTimeSeries();

    public abstract IEnumerable<string> GetKeywords();

    protected Expression GetExpressionWithAnyAll(BuildExpressionContext ctx, CollectionAnyAllToken anyAll, bool inMemory)
    {
        var ept = MListElementPropertyToken.AsMListEntityProperty(anyAll.Parent!);
        if (ept != null)
        {
            Expression collection = MListElementPropertyToken.BuildMListElements(ept, ctx);
            Type mleType = collection.Type.ElementType()!;

            var p = Expression.Parameter(mleType, mleType.Name.Substring(0, 1).ToLower());
            ctx.Replacements.Add(anyAll, new ExpressionBox(p, mlistElementRoute: anyAll.GetPropertyRoute()));
            var body = GetExpression(ctx, inMemory);
            ctx.Replacements.Remove(anyAll);

            return anyAll.BuildAnyAll(collection, p, body);
        }
        else
        {
            Expression collection = anyAll.Parent!.BuildExpression(ctx);

            Type elementType = collection.Type.ElementType()!;

            var p = Expression.Parameter(elementType, elementType.Name.Substring(0, 1).ToLower());
            ctx.Replacements.Add(anyAll, new ExpressionBox(p.BuildLiteNullifyUnwrapPrimaryKey(new[] { anyAll.GetPropertyRoute()! })));
            var body = GetExpression(ctx, inMemory);
            ctx.Replacements.Remove(anyAll);

            return anyAll.BuildAnyAll(collection, p, body);
        }
    }

    public static void SetIsTable(List<Filter> filters, IEnumerable<QueryToken> allTokens)
    {
        var fullTextOrders = allTokens.OfType<FullTextRankToken>().Select(a => a.Parent!);

        foreach (var fft in filters.OfType<FilterSqlServerFullText>())
        {
            fft.IsTable = fft.Tokens.Any(t => fullTextOrders.Contains(t));
            fft.TableOuterJoin = false;
        }

        foreach (var fg in filters.OfType<FilterGroup>())
        {
            fg.SetIsTable(fullTextOrders, false);
        }
    }

    internal CollectionNestedToken? GetDeepestNestedToken()
    {
        var nested = GetTokens().Select(b => b.HasNested()).NotNull();


        return nested.Aggregate((CollectionNestedToken?)null, (acum, token) =>
        {
            if (acum == null)
                return token;

            var acumFullKey = acum.FullKey();
            var tokenFullKey = token.FullKey();
            if (acumFullKey.StartsWith(tokenFullKey))
                return token;

            if (tokenFullKey.StartsWith(acumFullKey))
                return acum;

            throw new InvalidOperationException($"""
                Unable to use independent nested tokens in the same filter: 
                 {acumFullKey}
                 {tokenFullKey}
                """);
        });
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

        foreach (var item in Filters)
        {
            foreach (var t in item.GetTokens())
            {
                yield return t;
            }
        }
    }



    public override Expression GetExpression(BuildExpressionContext ctx, bool inMemory)
    {
        var anyAll = Token?.Follow(a => a.Parent)
                .OfType<CollectionAnyAllToken>()
                .TakeWhile(c => !ctx.Replacements.ContainsKey(c))
                .LastOrDefault();

        if (anyAll == null)
            return this.GroupOperation == FilterGroupOperation.And ?
                Filters.Select(f => f.GetExpression(ctx, inMemory)).AggregateAnd() :
                Filters.Select(f => f.GetExpression(ctx, inMemory)).AggregateOr();

        return GetExpressionWithAnyAll(ctx, anyAll, inMemory);
    }

    public override Filter? ToFullText()
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
            .Where(a => a.Key.Value is string s && s.Length > 0)
            .Select(gr => new FilterSqlServerFullText(gr.Key.Operation, gr.Select(a => a.Token).ToList(), (string)gr.Key.Value!))
            .ToList();

            filters.RemoveAll(aa => fullTextFilter.Contains(aa));

            if (filters.Count == 0 && groups.Count == 0)
                return null;

            if (filters.Count == 0 && groups.Count == 1)
                return groups.SingleEx();

            filters.AddRange(groups);

            return new FilterGroup(FilterGroupOperation.Or, this.Token, filters);
        }
        else
        {
            var filters = this.Filters.Select(a => a.ToFullText()).NotNull().ToList();

            if (filters.Count == 0)
                return null;

            if (filters.Count == 1)
                return filters.SingleEx();

            return  new FilterGroup(FilterGroupOperation.And, this.Token, filters);
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

    public override bool IsTimeSeries()
    {
        return this.Filters.Any(f => f.IsTimeSeries());
    }

    internal void SetIsTable(IEnumerable<QueryToken> fullTextOrders, bool isOuter)
    {
        isOuter |= this.GroupOperation == FilterGroupOperation.Or && this.Filters.Count > 1;

        foreach (var fft in this.Filters.OfType<FilterSqlServerFullText>())
        {
            fft.IsTable = fft.Tokens.Any(t => fullTextOrders.Contains(t));
            fft.TableOuterJoin = isOuter;
        }

        foreach (var fg in this.Filters.OfType<FilterGroup>())
        {
            fg.SetIsTable(fullTextOrders, isOuter);
        }
    }

    public override IEnumerable<string> GetKeywords() => Enumerable.Empty<string>();
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
        this.Value = ReflectionTools.ChangeType(value, GetValueType(Token, operation));
    }

    public static Type GetValueType(QueryToken token, FilterOperation operation)
    {
        if (operation.IsTsQuery())
            return typeof(string);

        if (operation.IsList())
            return typeof(IEnumerable<>).MakeGenericType(token.Type.Nullify());
        
        return token.Type;
    }

    public override IEnumerable<Filter> GetAllFilters()
    {
        yield return this;
    }

    public override IEnumerable<QueryToken> GetTokens()
    {
        yield return Token;
    }

    public override Filter? ToFullText()
    {
        if (Operation.IsFullTextFilterOperation())
        {
            if (Value is string s && s.Length > 0)
                return new FilterSqlServerFullText(Operation.ToFullTextFilterOperation(), new List<QueryToken> { Token }, s);
            return null;
        }

        return this;
    }

    static MethodInfo miContainsEnumerable = ReflectionTools.GetMethodInfo((IEnumerable<int> s) => s.Contains(2)).GetGenericMethodDefinition();


    public override Expression GetExpression(BuildExpressionContext ctx, bool inMemory)
    {
        CollectionAnyAllToken? anyAll = Token.Follow(a => a.Parent)
              .OfType<CollectionAnyAllToken>()
              .TakeWhile(c => !ctx.Replacements.ContainsKey(c))
              .LastOrDefault();

        if (anyAll == null)
            return GetConditionExpressionBasic(ctx, inMemory);

        return GetExpressionWithAnyAll(ctx, anyAll, inMemory);

    }



    public static Func<bool> ToLowerString = () => false;

    private Expression GetConditionExpressionBasic(BuildExpressionContext context, bool inMemory)
    {
        Expression left = Token.BuildExpression(context);

        if (Operation.IsList())
        {
            if (Value == null)
                return Expression.Constant(false);

            IList clone = (IList)Activator.CreateInstance(Value.GetType(), Value)!;

            if (clone.Contains(null))
                throw new InvalidOperationException("Filtering by null using IsIn / IsNotIn is no longer supported");

            //bool hasNull = false;
            //while (clone.Contains(null))
            //{
            //    clone.Remove(null);
            //    hasNull = true;
            //}

            if (Token.Type == typeof(string))
            {
                //while (clone.Contains(""))
                //{
                //    clone.Remove("");
                //    hasNull = true;
                //}

                if (ToLowerString())
                {
                    clone = clone.Cast<string>().Select(a => a.ToLower()).ToList();
                    left = Expression.Call(left, miToLower);
                }

                //if (hasNull)
                //{
                //    clone.Add("");
                //    left = Expression.Coalesce(left, Expression.Constant(""));
                //}
            }

            Expression right = Expression.Constant(clone, typeof(IEnumerable<>).MakeGenericType(Token.Type.Nullify()));
            var result = Expression.Call(miContainsEnumerable.MakeGenericMethod(Token.Type.Nullify()), right, left.Nullify());

            //var result = !hasNull || Token.Type == typeof(string) ? (Expression)contains :
            //        Expression.Or(Expression.Equal(left, Expression.Constant(null, Token.Type.Nullify())), contains);

            if (Operation == FilterOperation.IsIn)
                return result;

            if (Operation == FilterOperation.IsNotIn)
                return Expression.Not(result);

            throw new InvalidOperationException("Unexpected operation");
        }
        else if (Operation.IsTsQuery())
        {
            var strValue = (string?)Value;

            if (!strValue.HasText())
                return Expression.Constant(true);

            MethodInfo mi = TsVectorExtensions.GetTsQueryMethodInfo(Operation);

            var query = Expression.Call(mi, Expression.Constant(Value));

            return Expression.Call(TsVectorExtensions.miMatches, left, query);
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
                return QueryUtils.GetCompareExpression(Operation, Expression.Call(left, miToLower), right, inMemory);
            }
            else if (Token.Type == typeof(bool) && val == null)
            {
                return QueryUtils.GetCompareExpression(Operation, left, Expression.Constant(false, typeof(bool)), inMemory);
            }
            else
            {
                Expression right = Expression.Constant(val, Token.Type);
                return QueryUtils.GetCompareExpression(Operation, left, right, inMemory);
            }
        }
    }

  

    static MethodInfo miToLower = ReflectionTools.GetMethodInfo(() => "".ToLower());

    public override bool IsAggregate()
    {
        return this.Token is AggregateToken;
    }

    public override bool IsTimeSeries()
    {
        return this.Token is TimeSeriesToken;
    }

    public override string ToString()
    {
        return "{0} {1} {2}".FormatWith(Token.FullKey(), Operation, Value);
    }

    public override IEnumerable<string> GetKeywords()
    {
        switch (this.Operation)
        {
            case FilterOperation.EqualTo:
            case FilterOperation.Contains:
            case FilterOperation.StartsWith:
            case FilterOperation.EndsWith:
                {
                    if(this.Value is string s)
                        return new[] { s };

                    break;
                }
            case FilterOperation.Like:
                {
                    if (this.Value is string s)
                        return s.SplitNoEmpty("%");

                    break;
                }
            case FilterOperation.IsIn:
                return ((IEnumerable)this.Value!).OfType<string>();

            case FilterOperation.TsQuery:
                {
                    if (this.Value is string s)
                        return Regex.Split(s, @"&|\||!|<->|<\d>|\(|\)|\*")
                    .Select(a => a.Trim(' ', '\r', '\n', '\t').Trim('"'))
                    .Where(a => a.Length > 0);

                    break;
                }
            case FilterOperation.TsQuery_WebSearch:
                {
                    if (this.Value is string s)
                        return Regex.Split(s, @"(OR|-)")
                        .Select(a => a.Trim(' ', '\r', '\n', '\t').Trim('"'))
                        .Where(a => a.Length > 0);

                    break;
                }
            case FilterOperation.TsQuery_Phrase:
            case FilterOperation.TsQuery_Plain:
                {
                    if (this.Value is string s)
                        return new[] { s };

                    break;
                }
        }

        return Enumerable.Empty<string>();
    }
}



[InTypeScript(true), DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description)]
public enum FilterOperation
{
    [Description("equal to")]
    EqualTo,
    [Description("distinct to")]
    DistinctTo,
    [Description("greater than")]
    GreaterThan,
    [Description("greater than or equal")]
    GreaterThanOrEqual,
    [Description("less than")]
    LessThan,
    [Description("less than or equal")]
    LessThanOrEqual,
    [Description("contains")]
    Contains,
    [Description("starts with")]
    StartsWith,
    [Description("ends with")]
    EndsWith,
    [Description("like")]
    Like,
    [Description("not contains")]
    NotContains,
    [Description("not starts with")]
    NotStartsWith,
    [Description("not ends with")]
    NotEndsWith,
    [Description("not like")]
    NotLike,
    [Description("is in")]
    IsIn,
    [Description("is not in")]
    IsNotIn,
    
    [Description("complex condition")]
    ComplexCondition, //Full Text Search SQL Server
    [Description("free text")]
    FreeText, //Full Text Search SQL Server


    [Description("tsquery")]
    TsQuery,
    [Description("tsquery (plain)")]
    TsQuery_Plain,
    [Description("tsquery (phrase)")]
    TsQuery_Phrase,
    [Description("tsquery (web search)")]
    TsQuery_WebSearch,
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
    Model,
    Boolean,
    Enum,
    Guid,
    TsVector,
}

public class FilterSqlServerFullText : Filter
{
    public FilterSqlServerFullText(FullTextFilterOperation operation, List<QueryToken> tokens, string value)
    {
        if (tokens.IsNullOrEmpty())
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

    public override Expression GetExpression(BuildExpressionContext ctx, bool inMemory)
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

    public override bool IsTimeSeries()
    {
        return false;
    }

    public override Filter ToFullText()
    {
        throw new InvalidOperationException("Already FilterFullText!");
    }

    public static List<FilterSqlServerFullText> TableFilters(List<Filter> filters)
    {
        return filters.SelectMany(a => a.GetAllFilters()).OfType<FilterSqlServerFullText>().Where(a => a.IsTable).ToList();
    }

    //Keep in sync with Finder.tsx extractComplexConditions
    public override IEnumerable<string> GetKeywords()
    {
        if (this.Operation == FullTextFilterOperation.FreeText)
            return this.SearchCondition.Split(" ");

        return this.SearchCondition.Split(new string[] { "AND", "OR", "NOT", "NEAR", "(", ")", "*" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim(' ', '\r', '\n', '\t').Trim('"'))
            .Where(a => a.Length > 0);
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
    [Description("Checkbox (checked)")]
    Checkbox_Checked,
    [Description("Checkbox (unchecked)")]
    Checkbox_Unchecked,
    [Description("Not Checkbox (checked)")]
    NotCheckbox_Checked,
    [Description("Not Checkbox (unchecked)")]
    NotCheckbox_Unchecked,
}

[InTypeScript(true), DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description)]
public enum DashboardBehaviour
{
    //ShowAsPartFilter = 0, //Pinned Filter shown in the Part Widget
    PromoteToDasboardPinnedFilter = 1, //Pinned Filter promoted to dashboard
    UseAsInitialSelection, //Filters other parts in the same interaction group as if the user initially selected
    UseWhenNoFilters
}
