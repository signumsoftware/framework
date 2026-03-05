namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for EntityList control (autocomplete list of entities)
/// Equivalent to Selenium's EntityListProxy
/// </summary>
public class EntityListProxy : EntityBaseProxy
{
    public EntityListProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    public ILocator Items => Element.Locator(".sf-search-element");

    public override async Task SetValueUntypedAsync(object? value)
    {
        throw new NotImplementedException("EntityList SetValue not yet implemented");
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
        return await Items.CountAsync();
    }

    public async Task AddItemAsync(Lite<IEntity> lite)
    {
        if (await AutoCompleteElement.IsVisibleAsync())
        {
            await AutoCompleteAsync(lite);
        }
        else if (await FindButton.IsPresentAsync())
        {
            var findModal = await FindModalAsync();
            await findModal.Modal.Locator($"tr[data-entity-key='{lite.Key()}']").ClickAsync();
            await findModal.OkAsync();
        }
    }

    public async Task RemoveItemAsync(int index)
    {
        var item = Items.Nth(index);
        var removeButton = item.Locator(".sf-line-button.sf-remove");
        await removeButton.ClickAsync();
    }

    public async Task<ModalProxy> ViewItemAsync<T>(int index) where T : ModifiableEntity
    {
        var item = Items.Nth(index);
        var viewButton = item.Locator(".sf-line-button.sf-view");
        
        return await ModalProxy.CaptureAsync(Page, async () =>
        {
            await viewButton.ClickAsync();
        });
    }
}
