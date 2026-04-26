using Microsoft.AspNetCore.Routing;

namespace Signum.Playwright.LineProxies;

/// <summary>
/// Abstract Proxy for TextBase.tsx
/// </summary>
public abstract class TextBaseLineProxy : BaseLineProxy
{
    protected TextBaseLineProxy(ILocator element, PropertyRoute route)
        : base(element, route)
    {
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

/// <summary>
/// Proxy for TextBoxLine in TextBoxLine.tsx
/// </summary>
public class TextBoxLineProxy : TextBaseLineProxy
{
    public TextBoxLineProxy(ILocator element, PropertyRoute route)
        : base(element, route)
    {
    }
}

/// <summary>
/// Proxy for PasswordLine in TextBoxLine.tsx
/// </summary>
public class PasswordLineProxy : TextBaseLineProxy
{
    public PasswordLineProxy(ILocator element, PropertyRoute route)
        : base(element, route)
    {
    }
}

/// <summary>
/// Proxy for ColorLine in TextBoxLine.tsx
/// </summary>
public class ColorLineProxy : TextBaseLineProxy
{
    public ColorLineProxy(ILocator element, PropertyRoute route)
        : base(element, route)
    {
    }
}
