using Microsoft.Playwright;

namespace Signum.Playwright.Search;

public class ResultTableProxy
{
    public ILocator Element { get; }
    public SearchControlProxy SearchControl { get; }
    public IPage Page => SearchControl.Page;

    public ResultTableProxy(ILocator element, SearchControlProxy searchControl)
    {
        Element = element;
        SearchControl = searchControl;
    }

    public ILocator Table => Element.Locator("table.sf-search-results, table");
    public ILocator Rows => Table.Locator("tbody tr");
    public ILocator HeaderCells => Table.Locator("thead th");

    public async Task<int> GetRowCountAsync()
    {
        return await Rows.CountAsync();
    }

    public ILocator GetRow(int index)
    {
        return Rows.Nth(index);
    }

    public async Task<FramePageProxy<T>> EntityClickAsync<T>(int rowIndex) where T : Entity
    {
        var row = GetRow(rowIndex);
        var link = row.Locator("td a").First;
        
        await link.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        return new FramePageProxy<T>(Page);
    }

    public async Task EntityClickAsync(int rowIndex)
    {
        var row = GetRow(rowIndex);
        var link = row.Locator("td a").First;
        
        await link.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task<string?> GetCellTextAsync(int rowIndex, int columnIndex)
    {
        var row = GetRow(rowIndex);
        var cell = row.Locator("td").Nth(columnIndex);
        return await cell.TextContentAsync();
    }

    public async Task<string?> GetCellTextAsync(int rowIndex, string columnName)
    {
        var columnIndex = await GetColumnIndexAsync(columnName);
        if (columnIndex < 0)
            throw new InvalidOperationException($"Column '{columnName}' not found");

        return await GetCellTextAsync(rowIndex, columnIndex);
    }

    public ILocator CellElement(int rowIndex, string token)
    {
        // Find the column index for the token
        return GetRow(rowIndex).Locator($"td[data-column-name='{token}'], td[data-token='{token}']");
    }

    public ILocator HeaderCellElement(string token)
    {
        return Table.Locator($"thead th[data-column-name='{token}'], thead th[data-token='{token}']");
    }

    public async Task<int> GetColumnIndexAsync(string columnName)
    {
        var headerCount = await HeaderCells.CountAsync();
        
        for (int i = 0; i < headerCount; i++)
        {
            var headerText = await HeaderCells.Nth(i).TextContentAsync();
            if (headerText?.Trim() == columnName)
                return i;
        }

        return -1;
    }

    public async Task<List<string[]>> GetAllDataAsync()
    {
        var data = new List<string[]>();
        var rowCount = await GetRowCountAsync();

        for (int i = 0; i < rowCount; i++)
        {
            var row = GetRow(i);
            var cells = row.Locator("td");
            var cellCount = await cells.CountAsync();

            var rowData = new string[cellCount];
            for (int j = 0; j < cellCount; j++)
            {
                rowData[j] = (await cells.Nth(j).TextContentAsync())?.Trim() ?? "";
            }

            data.Add(rowData);
        }

        return data;
    }

    public async Task SelectRowAsync(int rowIndex)
    {
        var row = GetRow(rowIndex);
        var checkbox = row.Locator("input[type='checkbox']");
        await checkbox.CheckAsync();
    }

    public async Task<bool> IsRowSelectedAsync(int rowIndex)
    {
        var row = GetRow(rowIndex);
        var checkbox = row.Locator("input[type='checkbox']");
        return await checkbox.IsCheckedAsync();
    }

    public async Task<List<int>> GetSelectedRowsAsync()
    {
        var selectedRows = new List<int>();
        var rowCount = await GetRowCountAsync();

        for (int i = 0; i < rowCount; i++)
        {
            if (await IsRowSelectedAsync(i))
            {
                selectedRows.Add(i);
            }
        }

        return selectedRows;
    }
}
