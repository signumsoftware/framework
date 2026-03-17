using Signum.Playwright.LineProxies;

namespace Signum.Playwright.Search;

public abstract class FilterProxy { }

public class FilterGroupProxy : FilterProxy
{
    public ILocator Element { get; }
    public IPage Page { get; }
    readonly object queryName;

    public FilterGroupProxy(ILocator element, object queryName, IPage page)
    {
        Element = element;
        this.queryName = queryName;
        Page = page;
    }
}

public class FilterConditionProxy : FilterProxy
{
    public ILocator Element { get; }
    public IPage Page { get; }
    readonly object QueryName;

    public FilterConditionProxy(ILocator element, object queryName, IPage page)
    {
        Element = element;
        QueryName = queryName;
        Page = page;
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
        return new EntityLineProxy(ValueElement, null!, Page);
    }

    internal async Task SetValueAsync(object? value)
    {
        var fullKey = await QueryToken.FullKeyAsync();
        var qt = QueryUtils.Parse(fullKey!, QueryLogic.Queries.QueryDescription(QueryName),
            SubTokensOptions.CanElement | SubTokensOptions.CanAggregate | SubTokensOptions.CanAnyAll);

        var al = BaseLineProxy.AutoLine(ValueElement, qt.GetPropertyRoute()!, Page);

        if (value is PrimaryKey pk)
            await al.SetValueUntypedAsync(pk.Object);
        else
            await al.SetValueUntypedAsync(value);
    }
}
