using Signum.Playwright.Frames;

namespace Signum.Playwright.LineProxies;

public class EntityStripProxy : EntityBaseProxy
{
    public EntityStripProxy(ILocator element, PropertyRoute route)
        : base(element, route)
    {
    }

    public override Task<object?> GetValueUntypedAsync() => throw new NotImplementedException();
    public override Task SetValueUntypedAsync(object? value) => throw new NotImplementedException();
    public override Task<bool> IsReadonlyAsync() => throw new NotImplementedException();

    public ILocator ItemsContainerElement => this.Element.Locator("ul.sf-strip");

    public ILocator StripItemSelector(int index)
        => ItemsContainerElement.Locator($"> li.sf-strip-element:nth-child({index + 1})");

    public async Task<int> ItemsCountAsync()
        => await ItemsContainerElement.Locator("> li.sf-strip-element").CountAsync();

    public ILocator ViewElementIndex(int index)
        => StripItemSelector(index).Locator("> a.sf-entitStrip-link");

    public ILocator RemoveElementIndex(int index)
        => StripItemSelector(index).Locator("> a.sf-remove");

    public async Task RemoveAsync(int index)
        => await RemoveElementIndex(index).ClickAsync();

    public async Task<FrameModalProxy<T>> ViewAsync<T>(int index) where T : ModifiableEntity
    {
        var changes = await GetChangesAsync();
        var popup = await CaptureOnClickAsync(ViewElementIndex(index));

        var modal = await FrameModalProxy<T>.NewAsync(popup, this.ItemRoute);
        modal.Disposing = async okPressed => await WaitNewChangesAsync(changes);
        return modal;
    }
}
