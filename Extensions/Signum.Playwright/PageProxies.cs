using Microsoft.Playwright;

namespace Signum.Playwright;

/// <summary>
/// Playwright equivalent of Selenium's SearchPageProxy
/// </summary>
public class SearchPageProxy
{
    public IPage Page { get; }
    public SearchControlProxy SearchControl { get; }

    public SearchPageProxy(IPage page)
    {
        Page = page;
        SearchControl = new SearchControlProxy(page);
    }

    public async Task<PlaywrightResultTableProxy> GetResultsAsync()
    {
        return SearchControl.Results;
    }

    public async Task ClickCreateAsync()
    {
        await Page.Locator("a.sf-create, .sf-query-button-bar a:has-text('Create')").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.Locator(".sf-main-control").WaitVisibleAsync();
    }

    public async Task SearchAsync(string searchText)
    {
        await SearchControl.SearchAsync(searchText);
    }
}

/// <summary>
/// Playwright equivalent of Selenium's FramePageProxy
/// </summary>
public class FramePageProxy<T> where T : Entity
{
    public IPage Page { get; }
    public ILocator MainControl { get; }

    public FramePageProxy(IPage page)
    {
        Page = page;
        MainControl = page.Locator(".sf-main-control");
    }

    public async Task<EntityProxy<T>> GetEntityAsync()
    {
        return new EntityProxy<T>(MainControl, Page);
    }

    public async Task<T> RetrieveAsync()
    {
        var url = Page.Url;
        var match = System.Text.RegularExpressions.Regex.Match(url, @"/view/\w+/(\d+)");
        
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var id))
            throw new InvalidOperationException("Cannot extract entity ID from URL");

        return Database.Retrieve<T>(id);
    }
}

/// <summary>
/// Entity proxy for working with entity forms
/// </summary>
public class EntityProxy<T> where T : Entity
{
    public ILocator Element { get; }
    public IPage Page { get; }

    public EntityProxy(ILocator element, IPage page)
    {
        Element = element;
        Page = page;
    }

    public async Task SetFieldValueAsync(string fieldName, string value)
    {
        var selectors = new[]
        {
            $"[data-member='{fieldName}'] .form-control",
            $"[data-property-route='{fieldName}'] .form-control",
            $"input[name='{fieldName}']",
            $"#{fieldName}"
        };

        ILocator? field = null;
        foreach (var selector in selectors)
        {
            var locator = Element.Locator(selector);
            if (await locator.IsPresentAsync())
            {
                field = locator.First;
                break;
            }
        }

        if (field == null)
            throw new InvalidOperationException($"Could not find field '{fieldName}'");

        await field.FillAsync(value);
    }

    public async Task<string?> GetFieldValueAsync(string fieldName)
    {
        var selectors = new[]
        {
            $"[data-member='{fieldName}'] .form-control",
            $"input[name='{fieldName}']"
        };

        foreach (var selector in selectors)
        {
            var locator = Element.Locator(selector);
            if (await locator.IsPresentAsync())
            {
                return await locator.First.InputValueAsync();
            }
        }

        return null;
    }

    public async Task SaveAsync()
    {
        await Element.Locator("button.sf-entity-button-save, button:has-text('Save')").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task ExecuteOperationAsync(string operationName)
    {
        await Element.Locator($"button:has-text('{operationName}'), .sf-operation-button:has-text('{operationName}')").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task ClickTabAsync(string tabName)
    {
        await Element.Locator($"a.nav-link:has-text('{tabName}'), ul.nav-tabs a:has-text('{tabName}')").ClickAsync();
        await Task.Delay(500);
    }
}
