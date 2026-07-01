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
        var popup = await CreateButton.CaptureOnClickAsync();

        if (await SelectorModalProxy.IsSelectorAsync(popup))
        {
            var selector = popup.AsSelectorModal();
            popup = await selector.SelectAndCaptureAsync<T>();
        }

        return await FrameModalProxy<T>.NewAsync(popup);
    }
}
