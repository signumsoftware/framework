using Microsoft.Playwright;
using Signum.Playwright.Frames;
using Signum.Utilities.Synchronization;

namespace Signum.Playwright.Search;

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

    public ResultRowProxy RowElement(int rowIndex)
        => new ResultRowProxy(RowElementLocator(rowIndex));

    private ILocator RowElementLocator(int rowIndex)
        => Element.Locator($"tr[data-row-index='{rowIndex}']");

    public ResultRowProxy RowElement(Lite<IEntity> lite)
        => new ResultRowProxy(RowElementLocator(lite));

    private ILocator RowElementLocator(Lite<IEntity> lite)
        => Element.Locator($"tr[data-entity='{lite.Key()}']");

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
        => await RowElement(rowIndex).SelectedCheckbox.ClickAsync();

    public async Task SelectRowsAsync(params int[] indexes)
    {
        foreach (var i in indexes)
            await SelectRowAsync(i);
    }

    public async Task SelectRowAsync(Lite<IEntity> lite)
        => await RowElement(lite).SelectedCheckbox.ClickAsync();


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
        return RowElement(rowIndex).CellElement(col);
    }

    public async Task<ILocator> CellElementAsync(Lite<IEntity> lite, string token)
    {
        var col = await GetColumnIndexAsync(token);
        return RowElement(lite).CellElement(col);
    }

    // ---------------- HEADER ----------------

    public ILocator HeaderElement => Element.Locator("thead > tr > th");

    public ILocator HeaderCellElement(string token)
        => HeaderElement.Locator($"[data-column-name='{token}']");

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
        return new FrameModalProxy<T>(popup);
    }

    public async Task<FrameModalProxy<T>> EntityClickAsync<T>(Lite<T> lite)
        where T : Entity
    {
        var link = await EntityLinkAsync(lite);
        var popup = await link.CaptureOnClickAsync();
        return new FrameModalProxy<T>(popup);
    }

    public async Task<FramePageProxy<T>> EntityClickInPlaceAsync<T>(int rowIndex)
    where T : Entity
    {
        var link = await EntityLinkAsync(rowIndex);
        await link.ClickAsync();
        return await FramePageProxy<T>.CreateAsync(this.Element.Page);
    }

    public async Task<FramePageProxy<T>> EntityClickInPlaceAsync<T>(Lite<T> lite)
        where T : Entity
    {
        var link = await EntityLinkAsync(lite);
        await link.ClickAsync();
        return await FramePageProxy<T>.CreateAsync(this.Element.Page);
    }

    public async Task<ILocator> EntityLinkAsync(Lite<IEntity> lite)
    {
        var col = await GetColumnIndexAsync("Entity");
        return RowElement(lite).EntityLink(col);
    }

    public async Task<ILocator> EntityLinkAsync(int rowIndex)
    {
        var col = await GetColumnIndexAsync("Entity");
        return RowElement(rowIndex).EntityLink(col);
    }

    // ---------------- CONTEXT MENU ----------------

    public async Task<EntityContextMenuProxy> EntityContextMenuAsync(int rowIndex, string columnToken = "Entity")
    {
        var cell = await CellElementAsync(rowIndex, columnToken);

        await cell.ScrollIntoViewIfNeededAsync();
        await cell.ClickAsync(new() { Button = MouseButton.Right });

        var menu = await SearchControl.WaitContextMenuAsync();

        return new EntityContextMenuProxy(this, menu);
    }

    public async Task<EntityContextMenuProxy> EntityContextMenuAsync(Lite<Entity> lite, string columnToken = "Entity")
    {
        var cell = CellElementAsync(lite, columnToken).ResultSafe();
        await cell.ScrollIntoViewIfNeededAsync();

        var box = await cell.BoundingBoxAsync();
        if (box == null)
            throw new Exception("Cell element not visible");


        await cell.ClickAsync(new() { Button = MouseButton.Right });
        var menu = await this.SearchControl.WaitContextMenuAsync();

        return new EntityContextMenuProxy(this, menu);
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
            await RowElementLocator(lite)
                .Locator(".sf-entity-ctxmenu-success")
                .WaitForAsync(new() { State = WaitForSelectorState.Visible });
        }
    }

    public async Task WaitNoVisibleAsync(List<Lite<IEntity>> lites)
    {
        foreach (var lite in lites)
        {
            await RowElementLocator(lite)
                .WaitForAsync(new() { State = WaitForSelectorState.Detached });
        }
    }
}

public class ResultRowProxy
{
    public ILocator RowElement { get; private set; }

    public ResultRowProxy(ILocator rowElement)
    {
        RowElement = rowElement;
    }

    public ILocator SelectedCheckbox => RowElement.Locator("input.sf-td-selection");

    public ILocator CellElement(int columnIndex) => RowElement.Locator($"td:nth-child({columnIndex + 1})");

    public ILocator EntityLink(int entityColumnIndex) => CellElement(entityColumnIndex).Locator("> a");
}
