using Microsoft.Playwright;

namespace Signum.Playwright.Search;

public class FiltersProxy
{
    public ILocator Element { get; }
    public IPage Page { get; }
    public object QueryName { get; }

    public FiltersProxy(ILocator element, IPage page, object queryName)
    {
        Element = element;
        Page = page;
        QueryName = queryName;
    }

    public ILocator FilterRows => Element.Locator(".sf-filter-row");

    public async Task<int> GetFilterCountAsync()
    {
        return await FilterRows.CountAsync();
    }

    public async Task AddFilterAsync(string columnName)
    {
        var addButton = Element.Locator(".sf-filter-button-add, button:has-text('Add filter')");
        await addButton.ClickAsync();

        // Select the column
        var columnSelect = Element.Locator(".sf-filter-column select").Last;
        await columnSelect.SelectOptionAsync(new SelectOptionValue { Label = columnName });
    }

    public FilterConditionProxy GetFilter(int index)
    {
        var filterRow = FilterRows.Nth(index);
        return new FilterConditionProxy(filterRow, Page, QueryName);
    }

    public async Task<FilterConditionProxy> GetNewFilterAsync(Func<Task> action)
    {
        var initialCount = await GetFilterCountAsync();
        await action();
        
        // Wait for new filter to appear
        await Page.WaitForTimeoutAsync(300);
        var newCount = await GetFilterCountAsync();
        
        if (newCount > initialCount)
        {
            return GetFilter(newCount - 1);
        }

        throw new InvalidOperationException("No new filter was added");
    }

    public async Task RemoveFilterAsync(int index)
    {
        var filter = GetFilter(index);
        await filter.RemoveAsync();
    }

    public async Task ClearFiltersAsync()
    {
        var clearButton = Element.Locator(".sf-filter-button-clear, button:has-text('Clear')");
        if (await clearButton.IsPresentAsync())
        {
            await clearButton.ClickAsync();
        }
    }
}

public class FilterConditionProxy
{
    public ILocator Element { get; }
    public IPage Page { get; }
    public object QueryName { get; }

    public FilterConditionProxy(ILocator element, IPage page, object queryName)
    {
        Element = element;
        Page = page;
        QueryName = queryName;
    }

    public ILocator OperationSelect => Element.Locator(".sf-filter-operation select");
    public ILocator ValueInput => Element.Locator(".sf-filter-value input, .sf-filter-value select");
    public ILocator RemoveButton => Element.Locator(".sf-line-button.sf-remove");

    public async Task SetOperationAsync(string operation)
    {
        await OperationSelect.SelectOptionAsync(new SelectOptionValue { Label = operation });
    }

    public async Task SetValueAsync(string value)
    {
        await ValueInput.First.FillAsync(value);
    }

    public async Task<string?> GetValueAsync()
    {
        return await ValueInput.First.InputValueAsync();
    }

    public async Task RemoveAsync()
    {
        await RemoveButton.ClickAsync();
    }
}
