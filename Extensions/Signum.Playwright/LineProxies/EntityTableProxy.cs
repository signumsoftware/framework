namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for EntityTable control (table of embedded entities)
/// Equivalent to Selenium's EntityTableProxy
/// </summary>
public class EntityTableProxy : EntityBaseProxy
{
    public EntityTableProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    public ILocator Rows => Element.Locator("table tbody tr");

    public override async Task SetValueUntypedAsync(object? value)
    {
        throw new NotImplementedException("EntityTable SetValue not yet implemented");
    }

    public override async Task<object?> GetValueUntypedAsync()
    {
        var count = await GetRowCountAsync();
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

    public async Task<int> GetRowCountAsync()
    {
        return await Rows.CountAsync();
    }

    public async Task AddRowAsync()
    {
        await CreateButton.ClickAsync();
    }

    public async Task RemoveRowAsync(int index)
    {
        var row = Rows.Nth(index);
        var removeButton = row.Locator(".sf-line-button.sf-remove");
        await removeButton.ClickAsync();
    }

    /// <summary>
    /// Get cell value
    /// </summary>
    public async Task<string?> GetCellValueAsync(int rowIndex, int columnIndex)
    {
        var row = Rows.Nth(rowIndex);
        var cell = row.Locator("td").Nth(columnIndex);
        return await cell.TextContentAsync();
    }

    /// <summary>
    /// Get a field proxy for a specific row
    /// </summary>
    public BaseLineProxy GetFieldProxy(int rowIndex, string fieldName)
    {
        var row = Rows.Nth(rowIndex);
        var fieldRoute = ItemRoute.Add(fieldName);
        var fieldElement = row.Locator($"[data-member='{fieldName}']");
        
        return BaseLineProxy.AutoLine(fieldElement, fieldRoute, Page);
    }
}
