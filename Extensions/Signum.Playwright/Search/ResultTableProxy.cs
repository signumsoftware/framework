using Microsoft.Playwright;

namespace Signum.Playwright.Search;

public class ResultTableProxy
{
    public IPage Page { get; private set; }
    public ILocator Element { get; private set; }
    private SearchControlProxy SearchControl;

    public ResultTableProxy(ILocator element, SearchControlProxy searchControl, IPage page)
    {
        this.Page = page;
        this.Element = element;
        this.SearchControl = searchControl;
    }

    public ILocator ResultTableElement => Element.Locator("table.sf-search-results");
    public ILocator RowsLocator => Element.Locator("table.sf-search-results > tbody > tr");

    public async Task<List<ResultRowProxy>> AllRowsAsync()
    {
        var elements = await RowsLocator.AllAsync();
        return elements.Select(e => new ResultRowProxy(e, Page)).ToList();
    }

    public ResultRowProxy RowElement(int rowIndex)
    {
        return new ResultRowProxy(RowElementLocator(rowIndex), Page);
    }

    private ILocator RowElementLocator(int rowIndex)
    {
        return Element.Locator($"tr[data-row-index='{rowIndex}']");
    }

    public ResultRowProxy RowElement(Lite<IEntity> lite)
    {
        return new ResultRowProxy(RowElementLocator(lite), Page);
    }

    private ILocator RowElementLocator(Lite<IEntity> lite)
    {
        return Element.Locator($"tr[data-entity='{lite.Key()}']");
    }

    public async Task<List<Lite<IEntity>>> SelectedEntitiesAsync()
    {
        var rows = await RowsLocator.AllAsync();
        var entities = new List<Lite<IEntity>>();
        foreach (var row in rows)
        {
            if (await row.Locator("input.sf-td-selection:checked").CountAsync() > 0)
                entities.Add(Lite.Parse<IEntity>(await row.GetAttributeAsync("data-entity")));
        }
        return entities;
    }

    public ILocator CellElement(int rowIndex, string token)
    {
        var columnIndex = GetColumnIndex(token);
        return RowElement(rowIndex).CellElement(columnIndex);
    }

    public ILocator CellElement(Lite<IEntity> lite, string token)
    {
        var columnIndex = GetColumnIndex(token);
        return RowElement(lite).CellElement(columnIndex);
    }

    public int GetColumnIndex(string token)
    {
        var tokens = GetColumnTokens();
        var index = Array.IndexOf(tokens, token);
        if (index == -1)
            throw new InvalidOperationException($"Token {token} not found between {string.Join(", ", tokens)}");
        return index;
    }

    public void SelectRow(int rowIndex) => RowElement(rowIndex).SelectedCheckbox.ClickAsync().GetAwaiter().GetResult();
    public void SelectRow(params int[] rowIndexes)
    {
        foreach (var index in rowIndexes)
            SelectRow(index);
    }
    public void SelectRow(Lite<IEntity> lite) => RowElement(lite).SelectedCheckbox.ClickAsync().GetAwaiter().GetResult();

    public ILocator HeaderElement => Element.Locator("thead > tr > th");

    public string[] GetColumnTokens()
    {
        var ths = Element.Locator("thead > tr > th").AllAsync().GetAwaiter().GetResult();
        return ths.Select(a => a.GetAttributeAsync("data-column-name").GetAwaiter().GetResult() ?? "").ToArray();
    }

    public ILocator HeaderCellElement(string token) => HeaderElement.Locator($"[data-column-name='{token}']");

    public int RowsCount() => Element.Locator("tbody > tr[data-entity]").CountAsync().GetAwaiter().GetResult();
}

public class ResultRowProxy
{
    public ILocator RowElement { get; private set; }
    private IPage Page;

    public ResultRowProxy(ILocator rowElement, IPage page)
    {
        RowElement = rowElement;
        Page = page;
    }

    public ILocator SelectedCheckbox => RowElement.Locator("input.sf-td-selection");

    public ILocator CellElement(int columnIndex) => RowElement.Locator($"td:nth-child({columnIndex + 1})");

    public ILocator EntityLink(int entityColumnIndex) => CellElement(entityColumnIndex).Locator("> a");
}
