using Signum.Playwright.Frames;

namespace Signum.Playwright.Search;

/// <summary>
/// Proxy for the result table part in SearchControlLoaded.tsx
/// </summary>
public class ResultTableProxy
{
    public ILocator Element { get; }
    private SearchControlProxy SearchControl;

    public ResultTableProxy(ILocator element, SearchControlProxy searchControl)
    {
        Element = element;
        SearchControl = searchControl;
    }

    public ILocator ResultTableElement => Element.Locator("table.sf-search-results");
    public ILocator RowsLocator => Element.Locator("table.sf-search-results > tbody > tr");

    // ---------------- ROWS ----------------

    public async Task<List<ResultRowProxy>> AllRowsAsync()
    {
        var rows = await RowsLocator.AllAsync();
        return rows.Select(r => new ResultRowProxy(r)).ToList();
    }

    public ResultRowProxy Row(int rowIndex)
        => new ResultRowProxy(RowElementLocator(rowIndex));

    private ILocator RowElementLocator(int rowIndex)
        => Element.Locator($"tr[data-row-index='{rowIndex}']");

    public ResultRowProxy Row(Lite<IEntity> lite, int? subRowIndex)
        => new ResultRowProxy(RowLocator(lite, subRowIndex));

    private ILocator RowLocator(Lite<IEntity> lite, int? subRowIndex)
    {
        var locator = Element.Locator($"tr[data-entity='{lite.Key()}']");

        if (subRowIndex != null)
            return locator.Nth(subRowIndex.Value);

        return locator;
    }

    public async Task<int> RowsCountAsync()
        => await Element.Locator("tbody > tr[data-entity]").CountAsync();

    // ---------------- SELECTION ----------------

    public async Task<List<Lite<IEntity>>> SelectedEntitiesAsync()
    {
        var rows = await RowsLocator.AllAsync();
        var result = new List<Lite<IEntity>>();

        foreach (var row in rows)
        {
            if (await row.Locator("input.sf-td-selection:checked").CountAsync() > 0)
            {
                var entity = await row.GetAttributeAsync("data-entity");
                if (entity != null)
                    result.Add(Lite.Parse<IEntity>(entity));
            }
        }

        return result;
    }

    public async Task SelectRowAsync(int rowIndex)
        => await Row(rowIndex).SelectedCheckbox.ClickAsync();

    public async Task SelectRowsAsync(params int[] indexes)
    {
        foreach (var i in indexes)
            await SelectRowAsync(i);
    }

    public async Task SelectRowAsync(Lite<IEntity> lite, int? subRowIndex = null)
        => await Row(lite, subRowIndex).SelectedCheckbox.ClickAsync();


    public async Task SelectAllRowsAsync()
    {
        var rowCount = await RowsCountAsync();  

        await SelectRowsAsync(0.To(rowCount).ToArray());
    }

    // ---------------- CELLS ----------------

    public async Task<int> GetColumnIndexAsync(string token)
    {
        var tokens = await GetColumnTokensAsync();
        var index = Array.IndexOf(tokens, token);

        if (index == -1)
            throw new InvalidOperationException($"Token {token} not found between {string.Join(", ", tokens)}");

        return index;
    }

    public async Task<string[]> GetColumnTokensAsync()
    {
        var headers = await Element.Locator("thead > tr > th").AllAsync();
        var result = new List<string>();

        foreach (var h in headers)
            result.Add(await h.GetAttributeAsync("data-column-name") ?? "");

        return result.ToArray();
    }

    public async Task<ILocator> CellElementAsync(int rowIndex, string token)
    {
        var col = await GetColumnIndexAsync(token);
        return Row(rowIndex).CellElement(col);
    }

    public async Task<ILocator> CellElementAsync(Lite<IEntity> lite, string token, int? subRowIndex = null)
    {
        var col = await GetColumnIndexAsync(token);
        return Row(lite, subRowIndex).CellElement(col);
    }

    // ---------------- HEADER ----------------

    public ILocator HeaderElement => Element.Locator("thead > tr");

    public ILocator HeaderCellElement(string token)
        => HeaderElement.Locator($"th[data-column-name='{token}']");

    public async Task<bool> HasColumnAsync(string token)
        => await HeaderCellElement(token).CountAsync() > 0;

    public async Task RemoveColumnAsync(string token)
    {
        var header = HeaderCellElement(token);

        await header.ClickAsync(new() { Button = MouseButton.Right });
        var menu = await SearchControl.WaitContextMenuAsync();

        await menu.Locator(".sf-remove-header").ClickAsync();

        await header.WaitForAsync(new() { State = WaitForSelectorState.Detached });
    }

    // ---------------- SORTING ----------------

    public async Task OrderByAsync(string token, bool descending = false, bool thenBy = false)
    {
        var header = HeaderCellElement(token);

        do
        {
            var page = this.Element.Page;
            await SearchControl.WaitSearchCompletedAsync(async () =>
            {
                if (thenBy)
                    await page.Keyboard.DownAsync("Shift");

                await header.ClickAsync();

                if (thenBy)
                    await page.Keyboard.UpAsync("Shift");
            });
        }
        while (!await IsHeaderMarkedSortedAsync(token, descending));
    }

    public async Task<bool> IsHeaderMarkedSortedAsync(string token, bool descending)
    {
        var selector = descending
            ? "span.sf-header-sort.desc"
            : "span.sf-header-sort.asc";

        return await HeaderCellElement(token)
            .Locator(selector)
            .CountAsync() > 0;
    }

    // ---------------- ENTITY CLICK ----------------

    public async Task<FrameModalProxy<T>> EntityClickAsync<T>(int rowIndex)
    where T : Entity
    {
        var link = await EntityLinkAsync(rowIndex);
        var popup = await link.CaptureOnClickAsync();
        return await FrameModalProxy<T>.NewAsync(popup);
    }

    public async Task<FrameModalProxy<T>> EntityClickAsync<T>(Lite<T> lite, int? subRowIndex = null)
        where T : Entity
    {
        var link = await EntityLinkAsync(lite, subRowIndex);
        var popup = await link.CaptureOnClickAsync();
        return await FrameModalProxy<T>.NewAsync(popup);
    }

    public async Task<FramePageProxy<T>> EntityClickInPlaceAsync<T>(int rowIndex)
    where T : Entity
    {
        var link = await EntityLinkAsync(rowIndex);
        await link.ClickAsync();
        return await FramePageProxy<T>.NewAsync(this.Element.Page);
    }

    public async Task<FramePageProxy<T>> EntityClickInPlaceAsync<T>(Lite<T> lite, int? subRowIndex = null)
        where T : Entity
    {
        var link = await EntityLinkAsync(lite, subRowIndex);
        await link.ClickAsync();
        return await FramePageProxy<T>.NewAsync(this.Element.Page);
    }

    public async Task<ILocator> EntityLinkAsync(Lite<IEntity> lite, int? subRowIndex = null)
    {
        var col = await GetColumnIndexAsync("Entity");
        return Row(lite, subRowIndex).EntityLink(col);
    }

    public async Task<ILocator> EntityLinkAsync(int rowIndex)
    {
        var col = await GetColumnIndexAsync("Entity");
        return Row(rowIndex).EntityLink(col);
    }

    // ---------------- CONTEXT MENU ----------------

    public SearchContextMenu ContextMenu(int rowIndex, string columnToken = "Entity") =>
        new SearchContextMenu(ContextMenuAsync_Private(rowIndex, columnToken), SearchControl);
    async Task<ILocator> ContextMenuAsync_Private(int rowIndex, string columnToken)
    {
        var cell = await CellElementAsync(rowIndex, columnToken);

        await cell.ScrollIntoViewIfNeededAsync();
        await cell.ClickAsync(new() { Button = MouseButton.Right });

        var menu = await SearchControl.WaitContextMenuAsync();

        return menu;
    }

    public SearchContextMenu ContextMenu(Lite<Entity> lite, string columnToken = "Entity", int? subRowIndex = null) =>
        new SearchContextMenu(ContextMenu_Private(lite, columnToken, subRowIndex), SearchControl);
    async Task<ILocator> ContextMenu_Private(Lite<Entity> lite, string columnToken, int? subRowIndex)
    {
        var cell = await CellElementAsync(lite, columnToken, subRowIndex);
        await cell.ScrollIntoViewIfNeededAsync();
        await cell.ClickAsync(new() { Button = MouseButton.Right });
        var menu = await this.SearchControl.WaitContextMenuAsync();
        return menu;
    }

    // ---------------- WAIT HELPERS ----------------

    public async Task WaitRowsAsync(int rows)
    {
        await this.Element.Page.WaitForFunctionAsync(
            @"([table, locator, count]) => table.querySelectorAll(locator).length === count",
            new object[] { await this.Element.ElementHandleAsync(),  "tbody > tr[data-entity]", rows });
    }

    public async Task WaitSuccessAsync(List<Lite<IEntity>> lites)
    {
        foreach (var lite in lites)
        {
            await RowLocator(lite, null).WaitHasClassAsync("sf-entity-ctxmenu-success", true);
        }
    }

    public async Task WaitNoVisibleAsync(List<Lite<IEntity>> lites)
    {
        foreach (var lite in lites)
        {
            await RowLocator(lite, null)
                .WaitForAsync(new() { State = WaitForSelectorState.Detached });
        }
    }
}

public class ResultRowProxy
{
    public ILocator Locator { get; private set; }

    public ResultRowProxy(ILocator rowElement)
    {
        Locator = rowElement;
    }

    public ILocator SelectedCheckbox => Locator.Locator("input.sf-td-selection");

    public ILocator CellElement(int columnIndex) => Locator.Locator($"td:nth-child({columnIndex + 1})");

    public ILocator EntityLink(int entityColumnIndex) => CellElement(entityColumnIndex).Locator("> a");
}


public class SearchContextMenu
{
    public SearchControlProxy SearchControl { get; private set; }
    public Task<ILocator> LocatorTask { get; private set; }

    public SearchContextMenu(Task<ILocator> locatorTask, SearchControlProxy search)
    {
        SearchControl = search;
        LocatorTask = locatorTask;
    }


    public async Task WaitRowsSuccess(Func<ContextMenuProxy, Task> action)
    {
        var menu = new ContextMenuProxy(await LocatorTask);

        var selectedEntities = await SearchControl.Results.SelectedEntitiesAsync();

        await action(menu);

        await SearchControl.Results.WaitSuccessAsync(selectedEntities);
    }

    public async Task WaitRowsNoVisible(Func<ContextMenuProxy, Task> action)
    {
        var menu = new ContextMenuProxy(await LocatorTask);

        var selectedEntities = await SearchControl.Results.SelectedEntitiesAsync();

        await action(menu);

        await SearchControl.Results.WaitNoVisibleAsync(selectedEntities);
    }


    public async Task WaitParentEntityReload(IEntityButtonContainer container, Func<ContextMenuProxy, Task> action)
    {
        var menu = new ContextMenuProxy(await LocatorTask);

        await container.WaitReloadAsync(() => action(menu));
    }

    public async Task WaitCustom(Func<ContextMenuProxy, SearchControlProxy, Task> actionAndCheck)
    {
        var menu = new ContextMenuProxy(await LocatorTask);

        await actionAndCheck(menu, SearchControl);
    }

}
