namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for GUID input controls
/// Equivalent to Selenium's GuidBoxLineProxy
/// </summary>
public class GuidBoxLineProxy : BaseLineProxy
{
    public GuidBoxLineProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    public override async Task SetValueUntypedAsync(object? value)
    {
        await SetInputValueAsync(value?.ToString());
    }

    public override async Task<object?> GetValueUntypedAsync()
    {
        var stringValue = await GetInputValueAsync();
        if (string.IsNullOrWhiteSpace(stringValue))
            return null;

        return Guid.Parse(stringValue);
    }

    public override async Task<bool> IsReadonlyAsync()
    {
        var input = InputLocator.First;
        return !await input.IsEnabledAsync();
    }
}
