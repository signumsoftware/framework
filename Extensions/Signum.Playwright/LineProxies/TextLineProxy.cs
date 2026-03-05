using Microsoft.Playwright;
using Signum.Basics;

namespace Signum.Playwright.LineProxies;

/// <summary>
/// Base proxy for text input controls
/// </summary>
public abstract class TextBoxBaseLineProxy : BaseLineProxy
{
    protected TextBoxBaseLineProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    public override async Task<object?> GetValueUntypedAsync() => await GetValueAsync();
    
    public override async Task SetValueUntypedAsync(object? value) => await SetValueAsync((string?)value);

    public override async Task<bool> IsReadonlyAsync()
    {
        var input = InputLocator.First;
        
        var isDisabled = !await input.IsEnabledAsync();
        if (isDisabled) return true;

        var hasReadonlyClass = await input.EvaluateAsync<bool>(
            "el => el.classList.contains('readonly') || el.classList.contains('form-control-plaintext') || el.hasAttribute('readonly')");
        
        return hasReadonlyClass;
    }

    public async Task SetValueAsync(string? value)
    {
        await SetInputValueAsync(value);
    }

    public async Task<string?> GetValueAsync()
    {
        return await GetInputValueAsync();
    }
}

/// <summary>
/// Proxy for single-line text input (TextBox)
/// Equivalent to Selenium's TextBoxLineProxy
/// </summary>
public class TextBoxLineProxy : TextBoxBaseLineProxy
{
    public TextBoxLineProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }
}

/// <summary>
/// Proxy for multi-line text input (TextArea)
/// Equivalent to Selenium's TextAreaLineProxy
/// </summary>
public class TextAreaLineProxy : TextBoxBaseLineProxy
{
    public TextAreaLineProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    protected override ILocator InputLocator => Element.Locator("textarea.form-control, textarea");
}

/// <summary>
/// Proxy for password input
/// </summary>
public class PasswordBoxLineProxy : TextBoxBaseLineProxy
{
    public PasswordBoxLineProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    protected override ILocator InputLocator => Element.Locator("input[type='password']");
}
