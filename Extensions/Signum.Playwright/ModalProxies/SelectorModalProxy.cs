using Microsoft.Playwright;

namespace Signum.Playwright.ModalProxies;

/// <summary>
/// Proxy for SelectorModal.tsx
/// </summary>
public class SelectorModalProxy : ModalProxy
{
    public SelectorModalProxy(ILocator element)
        : base(element)
    {
    }

    public async Task SelectAsync(string value)
    {
        await SelectPrivateAsync(value);
        await WaitNotVisibleAsync();
    }

    public async Task SelectAsync(Enum enumValue)
    {
        await SelectPrivateAsync(enumValue.ToString());
        await WaitNotVisibleAsync();
    }

    public async Task SelectAsync(Lite<Entity> lite)
    {
        await SelectPrivateAsync(lite.Key());
        await WaitNotVisibleAsync();
    }

    public async Task SelectAsync<T>()
    {
        await SelectPrivateAsync(TypeLogic.GetCleanName(typeof(T)));
        await WaitNotVisibleAsync();
    }

    public async Task SelectByIndexAsync(int index)
    {
        await SelectPrivateByIndexAsync(index);
        await WaitNotVisibleAsync();
    }

    public async Task<ILocator> SelectAndCaptureAsync(string value)
    {
        return await Modal.Page.CaptureModalAsync(async () => await SelectPrivateAsync(value));
    }

    public async Task<ILocator> SelectAndCaptureAsync(Enum enumValue)
    {
        return await Modal.Page.CaptureModalAsync(async () => await SelectPrivateAsync(enumValue.ToString()));
    }

    public async Task<ILocator> SelectAndCaptureAsync(Lite<Entity> lite)
    {
        return await Modal.Page.CaptureModalAsync(async () => await SelectPrivateAsync(lite.Key()));
    }

    public async Task<ILocator> SelectAndCaptureAsync<T>()
    {
        return await Modal.Page.CaptureModalAsync(async () => await SelectPrivateAsync(TypeLogic.GetCleanName(typeof(T))));
    }

    public async Task<ILocator> SelectByIndexAndCaptureAsync(int index)
    {
        return await Modal.Page.CaptureModalAsync(async () => await SelectPrivateByIndexAsync(index));
    }

    public async Task<string[]> ButtonNamesAsync()
    {
        var buttons = Modal.Locator("button[name]");
        var count = await buttons.CountAsync();
        var names = new List<string>();

        for (int i = 0; i < count; i++)
        {
            names.Add(await buttons.Nth(i).GetAttributeAsync("name") ?? "");
        }

        return names.ToArray();
    }

    public static async Task<bool> IsSelectorAsync(ILocator element)
    {
        return await element.Locator(".sf-selector-modal").CountAsync() > 0;
    }

    private async Task SelectPrivateByIndexAsync(int index)
    {
        var button = Modal.Locator($"button[name]").Nth(index).ClickAsync();
    }

    private async Task SelectPrivateAsync(string name)
    {
        var button = Modal.Locator($"button[name='{name}']").ClickAsync();
    }

    private async Task WaitNotVisibleAsync()
    {
        await Modal.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
    }
}

public static class SelectorModalExtensions
{
    public static async Task<SelectorModalProxy> Await_AsSelectorModal(this Task<ILocator> modal)
    {
        return new SelectorModalProxy(await modal);
    }

    public static SelectorModalProxy AsSelectorModal(this ILocator modal)
    {
        return new SelectorModalProxy(modal);
    }
}
