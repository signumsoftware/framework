using Microsoft.AspNetCore.Routing;

namespace Signum.Playwright.LineProxies;

public abstract class TextBoxBaseLineProxy : BaseLineProxy
{
    public ILocator Element { get; }
    public IPage Page { get; }

    protected TextBoxBaseLineProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
        this.Element = element;
        this.Page = page;
    }

    public ILocator InputLocator => Element.Locator(".form-control, .form-control-readonly");

    public override async Task<object?> GetValueUntypedAsync()
        => await GetValueAsync();

    public override async Task SetValueUntypedAsync(object? value)
        => await SetValueAsync((string?)value);

    public async Task SetValueAsync(string? value)
    {
        var input = InputLocator;

        await input.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });

        // Playwright Best Practice: erst leeren
        await input.FillAsync("");

        if (!string.IsNullOrEmpty(value))
            await input.FillAsync(value);
    }

    public async Task<string> GetValueAsync()
    {
        var input = InputLocator;

        await input.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached
        });

        return await input.InputValueAsync();
    }

    public override async Task<bool> IsReadonlyAsync()
    {
        var input = InputLocator;

        await input.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached
        });

        var hasReadonlyClass = await input.EvaluateAsync<bool>(
            "e => e.classList.contains('readonly') || e.classList.contains('form-control-plaintext')");

        var isDisabled = await input.IsDisabledAsync();
        var hasReadonlyAttr = await input.EvaluateAsync<bool>("e => e.hasAttribute('readonly')");

        return hasReadonlyClass || isDisabled || hasReadonlyAttr;
    }
}

public class TextBoxLineProxy : TextBoxBaseLineProxy
{
    public TextBoxLineProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }
}

public class PasswordBoxLineProxy : TextBoxBaseLineProxy
{
    public PasswordBoxLineProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }
}

public class ColorBoxLineProxy : TextBoxBaseLineProxy
{
    public ColorBoxLineProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }
}
