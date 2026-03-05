namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for EntityRepeater control (list of embedded entities)
/// Equivalent to Selenium's EntityRepeaterProxy
/// </summary>
public class EntityRepeaterProxy : EntityBaseProxy
{
    public EntityRepeaterProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    public ILocator Rows => Element.Locator(".sf-repeater-element");

    public override async Task SetValueUntypedAsync(object? value)
    {
        throw new NotImplementedException("EntityRepeater SetValue not yet implemented");
    }

    public override async Task<object?> GetValueUntypedAsync()
    {
        var count = await GetItemCountAsync();
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

    public async Task<int> GetItemCountAsync()
    {
        return await Rows.CountAsync();
    }

    public async Task AddItemAsync<T>() where T : ModifiableEntity
    {
        await CreateModalAsync<T>();
    }

    public async Task RemoveItemAsync(int index)
    {
        var row = Rows.Nth(index);
        var removeButton = row.Locator(".sf-line-button.sf-remove");
        await removeButton.ClickAsync();
    }

    public async Task<ModalProxy> ViewItemAsync<T>(int index) where T : ModifiableEntity
    {
        var row = Rows.Nth(index);
        var viewButton = row.Locator(".sf-line-button.sf-view");
        
        var popup = await ModalProxy.CaptureAsync(Page, async () =>
        {
            await viewButton.ClickAsync();
        });

        return popup;
    }
}
