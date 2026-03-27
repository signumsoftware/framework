using Signum.Playwright.Search;

namespace Signum.Playwright;

/// <summary>
/// Proxy for the left-side ToolbarRenderer sidebar.
/// Covers ToolbarSwitcher (RightCaretDropdown) and entity-type menu item selection.
/// </summary>
public class ToolbarSidebarProxy
{
    public IPage Page { get; }

    public ToolbarSidebarProxy(IPage page)
    {
        Page = page;
    }

    ILocator SidebarInner => Page.Locator(".sidebar .sidebar-inner");

    /// <summary>
    /// Ensures the sidebar is in Wide mode (visible). If the sidebar is hidden or narrow,
    /// clicks the toggle button in the top navbar to expand it.
    /// </summary>
    public async Task EnsureSidebarWideAsync()
    {
        // Check if sidebar-inner is visible (sidebar is Wide)
        var sidebarInner = SidebarInner;
        bool isVisible = false;
        try
        {
            await sidebarInner.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 3000 });
            isVisible = true;
        }
        catch (TimeoutException) { }

        if (!isVisible)
        {
            // Click the main sidebar toggle button in the navbar to expand it
            var toggleBtn = Page.Locator(".main-sidebar-button");
            await toggleBtn.ClickAsync();
            await sidebarInner.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        }
    }

    /// <summary>
    /// Opens the ToolbarSwitcher dropdown and selects the option matching <paramref name="optionLabel"/>.
    /// The option is identified by its aria-label attribute (set from the ToolbarMenu's label).
    /// </summary>
    public async Task SelectSwitcherOptionAsync(string optionLabel)
    {
        await EnsureSidebarWideAsync();

        // The RightCaretDropdown toggle is inside the switcher Nav.Item ([data-toolbar-content])
        // Note: Nav.Item renders as <div> not <li> inside nested ul/li structure
        var toggle = SidebarInner.Locator("[data-toolbar-content] .dropdown-toggle");
        await toggle.ClickAsync();

        // The flyout menu appears as .menu-right-of-caret outside the sidebar (React portal)
        var option = Page.Locator($".menu-right-of-caret .switcher-item[aria-label='{optionLabel}']");
        await option.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await option.ClickAsync();
    }

    /// <summary>
    /// Selects the first available entity from the EntityLine autocomplete rendered inside
    /// the active ToolbarSwitcher option (ToolbarMenuItemsEntityType).
    ///
    /// If the autocomplete shows no suggestions because the "only my project" filter is active
    /// (MyMember != null), falls back to the Find modal where all filter conditions are removed
    /// before selecting the first result.
    /// </summary>
    public async Task SelectFirstEntityInSidebarAsync()
    {
        var autocompleteInput = SidebarInner.Locator(".sf-entity-autocomplete");
        await autocompleteInput.ClickAsync();

        // Wait briefly for the typeahead dropdown to appear (minLength: 0 means it opens immediately)
        var dropdown = SidebarInner.Locator(".typeahead.dropdown-menu");
        bool hasItems = false;
        try
        {
            await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 3000 });
            hasItems = await dropdown.Locator("[data-entity-key]").CountAsync() > 0;
        }
        catch (TimeoutException) { }

        if (hasItems)
        {
            await dropdown.Locator("[data-entity-key]").First.ClickAsync();
            return;
        }

        // No results — the "only my project" filter (Entity.MyMember DistinctTo null) is active.
        // Dismiss the autocomplete and open the Find modal instead.
        await autocompleteInput.PressAsync("Escape");
        await SelectFirstEntityViaFindAsync();
    }

    private async Task SelectFirstEntityViaFindAsync()
    {
        // Click the find button directly — bypass EntityLineProxy.FindAsync which requires
        // data-changes attribute; the sidebar EntityLine is not a form line and lacks it.
        var findButton = SidebarInner.Locator(".sf-entity-line a.sf-find");
        var popup = await findButton.CaptureOnClickAsync();
        var searchModal = new SearchModalProxy(popup);
        await searchModal.Initialize(waitInitialSearch: true);

        // Remove the "only my project" filter if the list is still empty
        if (await searchModal.Results.RowsCountAsync() == 0)
        {
            if (!await searchModal.SearchControl.FiltersVisibleAsync())
                await searchModal.SearchControl.ToggleFiltersAsync(true);

            var filters = await searchModal.SearchControl.Filters.FiltersAsync();
            foreach (var filter in filters.OfType<FilterConditionProxy>())
                await filter.DeleteAsync();

            await searchModal.SearchControl.SearchAsync();
        }

        await searchModal.SelectByPositionAsync(0);
    }

    /// <summary>
    /// Clicks the sidebar nav link whose .nav-item-text matches <paramref name="label"/> exactly.
    /// Use this after a project has been selected to navigate to sub-items like "Status Reports".
    /// </summary>
    public async Task ClickNavItemAsync(string label)
    {
        var navLink = SidebarInner.Locator($".nav-link:has(.nav-item-text:text-is('{label}'))");
        await navLink.ClickAsync();
    }
}
