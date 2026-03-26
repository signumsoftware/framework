namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for TextAreaLine.tsx
/// </summary>
public class TextAreaLineProxy : BaseLineProxy
{
    public TextAreaLineProxy(ILocator element, PropertyRoute route)
       : base(element, route)
    {
    }

    public override async Task<object?> GetValueUntypedAsync() => await GetValueAsync();
    public override async Task SetValueUntypedAsync(object? value) => await SetValueAsync((string?)value);
    public override async Task<bool> IsReadonlyAsync()
    {
        var element = TextAreaLocator;
        return await element.EvaluateAsync<bool>("e => e.disabled || e.readOnly");
    }

    public ILocator TextAreaLocator => Element.Locator("textarea");

    public async Task SetValueAsync(string? value)
    {
        var element = TextAreaLocator;
        await element.FillAsync(value ?? "");
    }

    public async Task<string> GetValueAsync()
    {
        var element = TextAreaLocator;
        var value = await element.InputValueAsync();
        return value;
    }
}
