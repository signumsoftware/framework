using Microsoft.Playwright;
using Signum.Basics;
using Signum.Playwright.Frames;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.LineProxies;

public class EntityTabRepeaterProxy : EntityBaseProxy
{
    public override PropertyRoute ItemRoute => base.ItemRoute.Add("Item");

    public EntityTabRepeaterProxy(ILocator element, PropertyRoute route)
        : base(element, route)
    {
    }

    public override Task<object?> GetValueUntypedAsync() => throw new NotImplementedException();

    public override Task SetValueUntypedAsync(object? value) => throw new NotImplementedException();

    public override Task<bool> IsReadonlyAsync() => throw new NotImplementedException();


    public ILocator Tab(int index) => Element.Locator($".nav-tabs .nav-item .nav-link[data-rr-ui-event-key=\"{index}\"]");

    public ILocator ElementPanel() => Element.Locator(".sf-repeater-element.active");

    public async Task<LineContainer<T>> SelectTabAsync<T>(int index) where T : ModifiableEntity
    {
        await Tab(index).ClickAsync();

        await Page.WaitForFunctionAsync(
            @"({ container, idx }) => {
            const panel = container.querySelector('.sf-repeater-element.active');
            return panel && panel.id && panel.id.endsWith('-' + idx);
        }",
            new { container = Element, idx = index });

        return await DetailsAsync<T>();
    }

    public async Task<int> SelectedTabIndexAsync()
    {
        var active = Element.Locator(".nav-tabs .nav-item .nav-link.active");

        await active.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });

        var value = await active.GetAttributeAsync("data-rr-ui-event-key");

        return int.Parse(value!);
    }

    public async Task<LineContainer<T>> DetailsAsync<T>()
        where T : ModifiableEntity
    {
        var panel = ElementPanel();

        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });

        return new LineContainer<T>(panel, this.ItemRoute);
    }
}
