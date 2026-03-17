using Signum.Playwright.Frames;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.LineProxies;

public class EntityRepeaterProxy : EntityBaseProxy
{
    public override PropertyRoute ItemRoute => base.ItemRoute.Add("Item");

    public EntityRepeaterProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    public override Task<object?> GetValueUntypedAsync() => throw new NotImplementedException();
    public override Task SetValueUntypedAsync(object? value) => throw new NotImplementedException();
    public override Task<bool> IsReadonlyAsync() => throw new NotImplementedException();

    public virtual ILocator ItemsContainerElement => Element.Locator(".sf-repater-elements");

    public virtual ILocator ItemElement(int index)
    {
        return ItemsContainerElement.Locator($"div:nth-child({index + 1}) > fieldset.sf-repeater-element");
    }

    public async Task WaitItemLoadedAsync(int index)
    {
        await ItemElement(index).WaitForAsync();
    }

    public virtual async Task MoveUpAsync(int index)
    {
        await ItemElement(index).Locator("a.move-up").ClickAsync();
    }

    public virtual async Task MoveDownAsync(int index)
    {
        await ItemElement(index).Locator("a.move-down").ClickAsync();
    }

    public virtual async Task<int> ItemsCountAsync()
    {
        return await ItemsContainerElement.Locator("fieldset.sf-repeater-element").CountAsync();
    }

    public virtual ILocator Items()
    {
        return ItemsContainerElement.Locator("fieldset.sf-repeater-element");
    }

    public LineContainer<T> Details<T>(int index) where T : ModifiableEntity
    {
        return new LineContainer<T>(ItemElement(index), this.Page, this.ItemRoute);
    }

    public ILocator RemoveElementIndex(int index)
    {
        return ItemElement(index).Locator("a.remove");
    }

    public async Task RemoveAsync(int index)
    {
        await RemoveElementIndex(index).ClickAsync();
    }

    public async Task<EntityInfoProxy?> EntityInfoAsync(int index)
    {
        return await EntityInfoInternalAsync(index);
    }

    public async Task<LineContainer<T>> CreateElementAsync<T>() where T : ModifiableEntity
    {
        var count = await ItemsCountAsync();

        await CreateEmbeddedAsync<T>();

        return Details<T>(count + 1);
    }

    public async Task<LineContainer<T>> LastDetailsAsync<T>() where T : ModifiableEntity
    {
        var count = await ItemsCountAsync();
        return Details<T>(count - 1);
    }
}
