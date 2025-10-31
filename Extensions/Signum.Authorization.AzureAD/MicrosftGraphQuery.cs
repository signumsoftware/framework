using Signum.DynamicQuery.Tokens;

namespace Signum.Authorization.AzureAD;

public enum GraphFieldUsage
{
    Filter,
    Search,
    Select,
    Order,
}

public class MicrosoftGraphQueryConverter
{
    public virtual string[]? GetOrderBy(IEnumerable<Order> orders)
    {
        return orders.Select(c => ToGraphField(c.Token, GraphFieldUsage.Order) + " " + (c.OrderType == OrderType.Ascending ? "asc" : "desc")).ToArray();
    }

    
    public virtual string ToGraphField(QueryToken token, GraphFieldUsage usage)
    {
        var field = token.Follow(a => a.Parent).Reverse().ToString(a => a.Key.FirstLower(), "/");

        if (usage == GraphFieldUsage.Select && field.StartsWith("onPremisesExtensionAttributes/"))
            return "onPremisesExtensionAttributes";

        return field;
    }

    public virtual string ToStringValue(object? value)
    {
        return value is null ? "null" :
            value is string str ? $"'{str}'" :
            value is DateOnly date ? $"{date.ToIsoString()}" :
            value is DateTime dt ? $"{dt.ToIsoString()}" :
            value is DateTimeOffset dto ? $"{dto.DateTime.ToIsoString()}" :
            value is Guid guid ? $"'{guid}'" :
            value is bool b ? b.ToString().ToLower() :
            value?.ToString() ?? "";
    }

    public virtual string? GetFilters(List<Filter> filters)
    {
        return filters.Select(f => ToFilter(f)).Combined(FilterGroupOperation.And);
    }

    public virtual string? ToFilter(Filter f)
    {
        if (f is FilterCondition fc)
        {

            if (fc.Operation == FilterOperation.Contains)
                return null;

            var field = ToGraphField(fc.Token, GraphFieldUsage.Filter);

            switch (fc.Operation)
            {
                case FilterOperation.IsIn: return "(" + ((object[])fc.Value!).ToString(a => field + " eq " + ToStringValue(a), " OR ") + ")";
                case FilterOperation.IsNotIn: return "not (" + ((object[])fc.Value!).ToString(a => field + " eq " + ToStringValue(a), " OR ") + ")";
                case FilterOperation.Like:
                case FilterOperation.NotLike:
                     throw new InvalidOperationException(fc.Operation + " is not implemented in Microsoft Graph API");
                default: break;
            }

            var value = ToStringValue(fc.Value);

            return BuildCondition(field, fc.Operation, value);
        }
        else if (f is FilterGroup fg)
        {
            return fg.Filters.Select(f2 => ToFilter(f2)).Combined(fg.GroupOperation);
        }
        else
            throw new UnexpectedValueException(f);
    }

    public virtual string? BuildCondition(string field, FilterOperation operation, string value)
    {
        return operation switch
        {
            FilterOperation.EqualTo => field + " eq " + value,
            FilterOperation.DistinctTo => field + " ne " + value,
            FilterOperation.GreaterThan => field + " gt " + value,
            FilterOperation.GreaterThanOrEqual => field + " ge " + value,
            FilterOperation.LessThan => field + " lt " + value,
            FilterOperation.LessThanOrEqual => field + " le " + value,
            FilterOperation.Contains => null,
            FilterOperation.NotContains => "NOT (" + field + ":" + value + ")",
            FilterOperation.StartsWith => "startswith(" + field + "," + value + ")",
            FilterOperation.EndsWith => "endswith(" + field + "," + value + ")",
            FilterOperation.NotStartsWith => "not startswith(" + field + "," + value + ")",
            FilterOperation.NotEndsWith => "not endswith(" + field + "," + value + ")",
            _ => throw new UnexpectedValueException(operation)
        };
    }

    public virtual string? GetSearch(List<Filter> filters)
    {
        return filters.Select(f => ToSearch(f)).Combined(FilterGroupOperation.And);
    }

    public virtual string? ToSearch(Filter f)
    {
        if (f is FilterCondition fc)
        {
            return fc.Operation switch
            {
                FilterOperation.Contains => "\"" + ToGraphField(fc.Token, GraphFieldUsage.Search) + ":" + fc.Value?.ToString()?.Replace(@"""", @"\""") + "\"",
                _ => null
            };
        }
        else if (f is FilterGroup fg)
        {
            return fg.Filters.Select(f2 => ToSearch(f2)).Combined(fg.GroupOperation);
        }
        else
            throw new UnexpectedValueException(f);
    }




    public virtual string[]? GetSelect(IEnumerable<Column> columns)
    {
        return columns.Select(c => ToGraphField(c.Token, GraphFieldUsage.Select)).Distinct().ToArray();
    }

    public virtual int? GetTop(Pagination pagination)
    {
        var top = pagination switch
        {
            Pagination.All => (int?)null,
            Pagination.Firsts f => f.TopElements,
            Pagination.Paginate p => p.ElementsPerPage * p.CurrentPage,
            _ => throw new UnexpectedValueException(pagination)
        };

        return top;
    }
}

public static class MicrosoftGraphConverterExtensions
{
    public static string? Combined(this IEnumerable<string?> filterEnumerable, FilterGroupOperation groupOperation)
    {
        var filters = filterEnumerable.ToList();
        var cleanFilters = filters.NotNull().ToList();

        if (groupOperation == FilterGroupOperation.And)
        {
            if (cleanFilters.IsEmpty())
                return null;

            return cleanFilters.ToString(" AND ");
        }
        else
        {
            if (cleanFilters.IsEmpty())
                return null;

            if (cleanFilters.Count != filters.Count)
                throw new InvalidOperationException("Unable to convert filter (mix $filter and $search in an OR");

            if (cleanFilters.Count == 1)
                return cleanFilters.SingleEx();

            return "(" + cleanFilters.ToString(" OR ") + ")";
        }
    }
}
