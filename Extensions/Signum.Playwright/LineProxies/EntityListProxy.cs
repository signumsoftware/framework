using Signum.Playwright.Frames;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for EntityList.tsx
/// </summary>
public class EntityListProxy : EntityBaseProxy
{
    public override PropertyRoute ItemRoute => base.ItemRoute.Add("Item");

    public EntityListProxy(ILocator element, PropertyRoute route)
        : base(element, route)
    {
    }

    public override Task<object?> GetValueUntypedAsync() => throw new NotImplementedException();
    public override Task SetValueUntypedAsync(object? value) => throw new NotImplementedException();
    public override Task<bool> IsReadonlyAsync() => throw new NotImplementedException();

    public ILocator ListElement => Element.Locator("select.form-control");

    public ILocator OptionElement(int index)
    {
        return ListElement.Locator($"option:nth-child({index + 1})");
    }

    public async Task SelectAsync(int index)
    {
        var option = OptionElement(index);
        var value = await option.GetAttributeAsync("value");
        await ListElement.SelectOptionAsync(value!);
    }

    public async Task<FrameModalProxy<T>> ViewAsync<T>(int index) where T : ModifiableEntity
    {
        await SelectAsync(index);
        return await base.ViewInternalAsync<T>();
    }

    public async Task<int> ItemsCountAsync()
    {
        return await ListElement.Locator("option").CountAsync();
    }

    public async Task<EntityInfoProxy?> EntityInfoAsync(int index)
    {
        return await EntityInfoInternalAsync(index);
    }

    public async Task DoubleClickAsync(int index)
    {
        var option = OptionElement(index);
        await option.ClickAsync();
        await option.DblClickAsync();
    }
}
