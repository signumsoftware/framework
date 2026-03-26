using Microsoft.Playwright;

namespace Signum.Playwright.Search;

/// <summary>
/// Proxy for FilterBuilder.tsx
/// </summary>
public class FiltersProxy
{
    public ILocator Element { get; }
    public object QueryName { get; }

    public FiltersProxy(ILocator element, object queryName)
    {
        Element = element;
        QueryName = queryName;
    }

    public async Task<List<FilterProxy>> FiltersAsync()
    {
        var handles = await Element.Locator("table > tbody > tr").ElementHandlesAsync();
        var list = new List<FilterProxy>();

        foreach (var handle in handles)
        {
            var className = await handle.GetAttributeAsync("class") ?? "";

            ILocator locator = (ILocator)handle;

            if (className.Contains("sf-filter-condition"))
                list.Add(new FilterConditionProxy(locator, QueryName));
            else if (className.Contains("sf-filter-group"))
                list.Add(new FilterGroupProxy(locator, QueryName));
        }

        return list;
    }

    public async Task<FilterProxy> GetNewFilterAsync(Func<Task> action)
    {
        var oldFilters = await FiltersAsync();
        await action();

        FilterProxy? newFilter = null;

        await Element.Page.WaitForFunctionAsync(
            @"([table, oldCount]) => table.querySelectorAll('tbody > tr').length > oldCount",
            new object[] { await Element.ElementHandleAsync(), oldFilters.Count }
        );

        var currentFilters = await FiltersAsync();
        newFilter = currentFilters.Skip(oldFilters.Count).First();

        return newFilter!;
    }

    public ILocator AddFilterButton => Element.Locator(".sf-line-button.sf-create-condition");
    public ILocator AddGroupButton => Element.Locator(".sf-line-button.sf-create-group");
    public ILocator RemoveAllButton => Element.Locator("thead th .sf-remove");

    public async Task<FilterConditionProxy> AddFilterAsync()
    {
        return (FilterConditionProxy)await GetNewFilterAsync(async () => await AddFilterButton.ClickAsync());
    }

    public async Task<FilterGroupProxy> AddGroupAsync()
    {
        return (FilterGroupProxy)await GetNewFilterAsync(async () => await AddGroupButton.ClickAsync());
    }

    public async Task AddFilterAsync(string token, FilterOperation operation, object? value)
    {
        var fo = await AddFilterAsync();
        await fo.QueryToken.SelectTokenAsync(token);
        await fo.SetOperationAsync(operation);
        await fo.SetValueAsync(value);
    }

    public async Task RemoveAllAsync()
    {
        await RemoveAllButton.ClickAsync();
    }

    public async Task<bool> IsAddFilterEnabledAsync()
    {
        return await AddFilterButton.Locator(":not([disabled])").IsVisibleAsync();
    }

    public async Task<FilterProxy> GetFilterAsync(int index)
    {
        var filters = await FiltersAsync();
        return filters[index];
    }
}
