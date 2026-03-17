using Microsoft.Playwright;

namespace Signum.Playwright.Search;

public class FiltersProxy
{
    public ILocator Element { get; }
    public IPage Page { get; }
    public object QueryName { get; }

    public FiltersProxy(ILocator element, object queryName, IPage page)
    {
        Element = element;
        QueryName = queryName;
        Page = page;
    }

    public async Task<List<FilterProxy>> FiltersAsync()
    {
        var rows = await Element.Locator("table > tbody > tr").ElementHandlesAsync();
        var list = new List<FilterProxy>();

        foreach (var row in rows)
        {
            var locator = row; // ILocator wrapper in Playwright
            var className = await locator.GetAttributeAsync("class") ?? "";

            if (className.Contains("sf-filter-condition"))
                list.Add(new FilterConditionProxy(locator, QueryName, Page));
            else if (className.Contains("sf-filter-group"))
                list.Add(new FilterGroupProxy(locator, QueryName, Page));
        }

        return list;
    }

    public async Task<FilterProxy> GetNewFilterAsync(Func<Task> action)
    {
        var oldFilters = await FiltersAsync();
        await action();
        ILocator? newFilter = null;

        await Element.Page.WaitForFunctionAsync(async () =>
        {
            var currentFilters = await FiltersAsync();
            if (currentFilters.Count > oldFilters.Count)
            {
                newFilter = currentFilters.Skip(oldFilters.Count).FirstOrDefault();
                return true;
            }
            return false;
        });

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
        fo.Operation = operation;
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
