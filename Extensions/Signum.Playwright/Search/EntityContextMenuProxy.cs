using Microsoft.Playwright;

namespace Signum.Playwright.Search;

public class EntityContextMenuProxy
{
    public ResultTableProxy Results { get; }
    public ILocator Element { get; }
    public IPage Page => Results.Page;

    public EntityContextMenuProxy(ResultTableProxy results, ILocator element)
    {
        Results = results;
        Element = element;
    }

    public ILocator MenuItems => Element.Locator(".dropdown-item, a.dropdown-item");

    public async Task<int> GetMenuItemCountAsync()
    {
        return await MenuItems.CountAsync();
    }

    public async Task ClickMenuItemAsync(string text)
    {
        var item = Element.Locator($".dropdown-item:has-text('{text}')");
        await item.ClickAsync();
    }

    public async Task<List<string>> GetMenuItemTextsAsync()
    {
        var texts = new List<string>();
        var count = await MenuItems.CountAsync();

        for (int i = 0; i < count; i++)
        {
            var text = await MenuItems.Nth(i).TextContentAsync();
            if (!string.IsNullOrEmpty(text))
                texts.Add(text.Trim());
        }

        return texts;
    }

    public async Task<bool> HasMenuItemAsync(string text)
    {
        var item = Element.Locator($".dropdown-item:has-text('{text}')");
        return await item.CountAsync() > 0;
    }
}
