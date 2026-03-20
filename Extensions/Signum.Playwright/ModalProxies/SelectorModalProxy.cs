using Microsoft.Playwright;

namespace Signum.Playwright.ModalProxies;

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

    public async Task<ILocator> SelectAndCaptureAsync(string value)
    {
        return await CapturePopupAsync(async () => await SelectPrivateAsync(value));
    }

    public async Task<ILocator> SelectAndCaptureAsync(Enum enumValue)
    {
        return await CapturePopupAsync(async () => await SelectPrivateAsync(enumValue.ToString()));
    }

    public async Task<ILocator> SelectAndCaptureAsync(Lite<Entity> lite)
    {
        return await CapturePopupAsync(async () => await SelectPrivateAsync(lite.Key()));
    }

    public async Task<ILocator> SelectAndCaptureAsync<T>()
    {
        return await CapturePopupAsync(async () => await SelectPrivateAsync(TypeLogic.GetCleanName(typeof(T))));
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

    private async Task SelectPrivateAsync(string name)
    {
        var button = Modal.Locator($"button[name='{name}']");
        await button.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await button.ClickAsync();
    }

    private async Task<ILocator> CapturePopupAsync(Func<Task> action)
    {
        // Einfacher Wrapper für Playwright-Popups, z.B. neues Modal, Alert oder Window
        await action();
        // Annahme: Popup erscheint innerhalb des gleichen Pages, Locator muss ggf. angepasst werden
        var popup = Modal.Page.Locator(".sf-popup:visible");
        await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        return popup;
    }

    private async Task WaitNotVisibleAsync()
    {
        await Modal.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
    }
}

public static class SelectorModalExtensions
{
    public static SelectorModalProxy AsSelectorModal(this ILocator modal)
    {
        return new SelectorModalProxy(modal);
    }
}
