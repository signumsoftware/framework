using Signum.Playwright.Frames;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.Search;

/// <summary>
/// Proxy for SearchValueLine.tsx
/// </summary>
public class SearchValueLineProxy
{
    public ILocator Element { get; private set; }

    public SearchValueLineProxy(ILocator element)
    {
        this.Element = element;
    }

    public ILocator CountSearch => Element.Locator(".count-search");

    public ILocator FindButton => Element.Locator(".sf-line-button.sf-find");

    public ILocator CreateButton => Element.Locator(".sf-line-button.sf-create");

    public async Task<FrameModalProxy<T>> CreateAsync<T>() where T : ModifiableEntity
    {
        var popup = await CapturePopupAsync(CreateButton, async () => await CreateButton.ClickAsync());

        if (await SelectorModalProxy.IsSelectorAsync(popup))
        {
            var selector = popup.AsSelectorModal();
            popup = await selector.SelectAndCaptureAsync<T>();
        }

        return await new FrameModalProxy<T>(popup).WaitLoadedAsync();
    }

    private async Task<ILocator> CapturePopupAsync(ILocator trigger, Func<Task> action)
    {
        // Playwright Popup Capture Simulation
        await action();
        // Annahme: Das neue Modal wird als sichtbares Element auf der Page angezeigt
        var popup = Element.Page.Locator(".sf-popup:visible");
        await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        return popup;
    }
}
