using Microsoft.Playwright;
using Signum.Basics;
using Signum.DynamicQuery;
using Signum.Entities;
using Signum.Playwright;
using Signum.Playwright.LineProxies;
using Signum.Playwright.Search;
using Signum.UserQueries;

namespace Signum.Playwright.Toolbar;

// Proxy for ToolbarRenderer.tsx.
public class ToolbarSidebarProxy
{
    public IPage Page { get; }

    public ToolbarSidebarProxy(IPage page)
    {
        Page = page;
    }

    ILocator SidebarInner => Page.Locator(".sidebar .sidebar-inner");

 
    public MenuItemProxy MenuItemDisplayName(string displayName) =>
        new MenuItemProxy(SidebarInner.Locator($".nav-link:has(.nav-item-text:text-is('{displayName}'))"));


    public async Task OpenMenu(string displayName)
    {
        var subMenu = SidebarInner.Locator($"li:has(.nav-link:has(.nav-item-text:text-is('{displayName}'))) .nav-item-sub-menu").First;
        if (!await subMenu.IsVisibleAsync())
            await MenuItemDisplayName(displayName).ClickAsync();
    }

    public MenuItemProxy MenuItem(Lite<Entity> entityLite) =>
        new MenuItemProxy(SidebarInner.Locator($".nav-link[data-toolbar-content='{entityLite.Key()}']"));

    public MenuItemProxy MenuItemForQuery(object queryName)
    {
        var key = QueryUtils.GetKey(queryName);
        return new MenuItemProxy(SidebarInner.Locator($".nav-link[data-toolbar-content='{key}']"));
    }

    public EntityLineProxy EntitySelector(Type entityType, int selectorIndex = 0) =>
        new EntityLineProxy(SidebarInner.Locator(".form-group:has(.sf-entity-line)").Nth(selectorIndex), PropertyRoute.Root(entityType));

    public Task SelectEntityAsync(Lite<IEntity> entity, int selectorIndex = 0) => EntitySelector(entity.EntityType, selectorIndex).SetLiteAsync(entity);

    public ToolbarSwitcherProxy Switcher(int switcherIndex = 0) =>
        new ToolbarSwitcherProxy(
            SidebarInner.Locator("[data-toolbar-content] .dropdown-toggle").Nth(switcherIndex),
            Page);

    public async Task EnsureSidebarWideAsync()
    {
        if (!await SidebarInner.IsVisibleAsync())
            await Page.Locator(".main-sidebar-button").ClickAsync();

        await SidebarInner.WaitVisibleAsync();
    }

    public async Task SelectSwitcherOptionAsync(string optionLabel)
    {
        await EnsureSidebarWideAsync();
        await Switcher(0).SelectAsync(optionLabel);
    }
}


public class MenuItemProxy
{
    public ILocator Locator { get; }

    public MenuItemProxy(ILocator locator) => Locator = locator;

    public async Task ClickAsync() => await Locator.ClickAsync();

    public async Task<SearchPageProxy> ClickAsSearchPageAsync()
    {
        await Locator.ClickAsync();
        return await SearchPageProxy.NewAsync(Locator.Page);
    }

    public async Task<int> GetCounterAsync()
    {
        var badge = Locator.Locator(".sf-toolbar-count");
        var text = await badge.TextContentAsync();
        return int.TryParse(text, out var n) ? n : 0;
    }

    public async Task WaitCounterAsync(int? expected)
    {
        if(expected == null)
        {
            await Locator.Locator(".sf-toolbar-count").WaitNotVisibleAsync();
        }

        await Locator.Locator(".sf-toolbar-count").WaitVisibleAsync();
        await Locator.Locator(".sf-toolbar-count").WaitContentAsync(expected.ToString());
    }
}

public class ToolbarSwitcherProxy
{
    private ILocator Toggle { get; }
    private IPage Page { get; }

    public ToolbarSwitcherProxy(ILocator toggle, IPage page)
    {
        Toggle = toggle;
        Page = page;
    }

    public async Task SelectAsync(string displayName)
    {
        await Toggle.ClickAsync();
        var option = Page.Locator($".menu-right-of-caret .switcher-item[aria-label='{displayName}']");
        await option.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await option.ClickAsync();
    }
}

