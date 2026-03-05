using Microsoft.Playwright;

namespace Signum.Playwright.ModalProxies;

public class SelectorModalProxy : ModalProxy
{
    public SelectorModalProxy(IPage page) : base(page)
    {
    }

    public SelectorModalProxy(IPage page, ILocator modalElement) : base(modalElement, page)
    {
    }

    public async Task SelectAsync(string value)
    {
        await SelectPrivateAsync(value);
        await WaitForCloseAsync();
    }

    public async Task SelectAsync(Enum enumValue)
    {
        await SelectPrivateAsync(enumValue.ToString());
        await WaitForCloseAsync();
    }

    public async Task SelectAsync(Lite<Entity> lite)
    {
        await SelectPrivateAsync(lite.Key());
        await WaitForCloseAsync();
    }

    public async Task SelectAsync<T>()
    {
        await SelectPrivateAsync(TypeLogic.GetCleanName(typeof(T)));
        await WaitForCloseAsync();
    }

    public static async Task<ModalProxy> SelectAndCaptureAsync(IPage page, Func<Task> clickAction, string value)
    {
        var modal = await ModalProxy.CaptureAsync(page, clickAction);
        var selector = new SelectorModalProxy(page, modal.Modal);
        await selector.SelectPrivateAsync(value);
        
        // Wait for new modal
        await Task.Delay(300);
        var newModal = await ModalProxy.CaptureAsync(page, async () => await Task.CompletedTask);
        return newModal;
    }

    public static async Task<ModalProxy> SelectAndCaptureAsync(IPage page, Func<Task> clickAction, Enum enumValue)
    {
        return await SelectAndCaptureAsync(page, clickAction, enumValue.ToString());
    }

    public static async Task<ModalProxy> SelectAndCaptureAsync(IPage page, Func<Task> clickAction, Lite<Entity> lite)
    {
        return await SelectAndCaptureAsync(page, clickAction, lite.Key());
    }

    public static async Task<ModalProxy> SelectAndCaptureAsync<T>(IPage page, Func<Task> clickAction)
    {
        return await SelectAndCaptureAsync(page, clickAction, TypeLogic.GetCleanName(typeof(T)));
    }

    public async Task<string[]> GetButtonNamesAsync()
    {
        var buttons = await Modal.Locator("button[name]").AllAsync();
        var names = new List<string>();
        
        foreach (var button in buttons)
        {
            var name = await button.GetAttributeAsync("name");
            if (name != null)
                names.Add(name);
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
        await button.WaitVisibleAsync();
        await button.ClickAsync();
    }
}

public static class SelectorModalExtensions
{
    public static SelectorModalProxy AsSelectorModal(this ModalProxy modal)
    {
        return new SelectorModalProxy(modal.Page, modal.Modal);
    }
}
