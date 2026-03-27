using Signum.Playwright.LineProxies;

namespace Signum.Playwright.Search;


public abstract class FilterProxy { }

/// <summary>
/// Proxy for FilterGroupComponent in FilterBuilder.tsx
/// </summary>
public class FilterGroupProxy : FilterProxy
{
    public ILocator Element { get; }
    readonly object queryName;

    public FilterGroupProxy(ILocator element, object queryName)
    {
        Element = element;
        this.queryName = queryName;
    }
}

/// <summary>
/// Proxy for FilterConditionComponent in FilterBuilder.tsx
/// </summary>
public class FilterConditionProxy : FilterProxy
{
    public ILocator Element { get; }
    readonly object QueryName;

    public FilterConditionProxy(ILocator element, object queryName)
    {
        Element = element;
        QueryName = queryName;
    }

    public ILocator DeleteButton => Element.Locator(".sf-line-button.sf-remove");
    public QueryTokenBuilderProxy QueryToken => new QueryTokenBuilderProxy(Element.Locator(".sf-query-token-builder"));

    public ILocator OperationElement => Element.Locator("td.sf-filter-operation select");
    public ILocator ValueElement => Element.Locator("td.sf-filter-value *");

    public async Task<FilterOperation> GetOperationAsync()
    {
        var value = await OperationElement.EvaluateAsync<string>("el => el.value");
        return value.ToEnum<FilterOperation>();
    }

    public async Task SetOperationAsync(FilterOperation op)
    {
        await OperationElement.SelectOptionAsync(op.ToString());
    }

    public async Task DeleteAsync()
    {
        await DeleteButton.ClickAsync();
    }

    public EntityLineProxy EntityLine()
    {
        return new EntityLineProxy(ValueElement, null!);
    }

    internal async Task SetValueAsync(object? value)
    {
        var fullKey = await QueryToken.FullKeyAsync();
        var qt = QueryUtils.Parse(fullKey!, QueryLogic.Queries.QueryDescription(this.QueryName), SubTokensOptions.CanElement | SubTokensOptions.CanAggregate | SubTokensOptions.CanAnyAll);

        var al = BaseLineProxy.AutoLine(ValueElement, qt.GetPropertyRoute()!);

        if (value is PrimaryKey pk)
            await al.SetValueUntypedAsync(pk.Object);
        else
            await al.SetValueUntypedAsync(value);
    }
}
