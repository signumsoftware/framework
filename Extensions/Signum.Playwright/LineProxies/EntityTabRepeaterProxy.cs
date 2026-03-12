using Microsoft.Playwright;
using Signum.Basics;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for EntityTabRepeater control (tabbed list of embedded entities)
/// Equivalent to Selenium's EntityTabRepeaterProxy
/// </summary>
public class EntityTabRepeaterProxy : EntityBaseProxy
{
    public EntityTabRepeaterProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    public ILocator TabHeaders => Element.Locator(".nav-tabs .nav-link");
    public ILocator TabContent => Element.Locator(".tab-content");

    public override async Task SetValueUntypedAsync(object? value)
    {
        throw new NotImplementedException("EntityTabRepeater SetValue not yet implemented");
    }

    public override async Task<object?> GetValueUntypedAsync()
    {
        var count = await GetTabCountAsync();
        var items = new List<EntityInfoProxy?>();

        for (int i = 0; i < count; i++)
        {
            items.Add(await GetEntityInfoAsync(i));
        }

        return items;
    }

    public override async Task<bool> IsReadonlyAsync()
    {
        return await Element.IsDomDisabledAsync();
    }

    public async Task<int> GetTabCountAsync()
    {
        return await TabHeaders.CountAsync();
    }

    public async Task AddTabAsync<T>() where T : ModifiableEntity
    {
        await CreateModalAsync<T>();
    }

    public async Task RemoveTabAsync(int index)
    {
        await SelectTabAsync(index);
        var activeTab = TabContent.Locator(".tab-pane.active");
        var removeButton = activeTab.Locator(".sf-line-button.sf-remove");
        await removeButton.ClickAsync();
    }

    public async Task SelectTabAsync(int index)
    {
        var tab = TabHeaders.Nth(index);
        await tab.ClickAsync();
        await Task.Delay(300); // Wait for tab content to be visible
    }

    public async Task<string?> GetTabTitleAsync(int index)
    {
        var tab = TabHeaders.Nth(index);
        return await tab.TextContentAsync();
    }

    public async Task<ModalProxy> ViewTabAsync<T>(int index) where T : ModifiableEntity
    {
        await SelectTabAsync(index);
        var activeTab = TabContent.Locator(".tab-pane.active");
        var viewButton = activeTab.Locator(".sf-line-button.sf-view");
        
        return await ModalProxy.CaptureAsync(Page, async () =>
        {
            await viewButton.ClickAsync();
        });
    }
}
