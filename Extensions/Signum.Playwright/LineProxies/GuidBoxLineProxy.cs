namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for GuidLine in TextBoxLine.tsx
/// </summary>
public class GuidBoxLineProxy : BaseLineProxy
{
    public GuidBoxLineProxy(ILocator element, PropertyRoute route)
        : base(element, route)
    {
    }

    public ILocator InputLocator => Element.Locator("input[type=text]");

    public override async Task<object?> GetValueUntypedAsync()
        => await GetValueAsync();

    public override async Task SetValueUntypedAsync(object? value)
        => await SetValueAsync((Guid?)value);

    public async Task SetValueAsync(Guid? value)
    {
        var input = InputLocator;

        await input.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });

        // Playwright Best Practice: zuerst leeren
        await input.FillAsync("");

        if (value.HasValue)
            await input.FillAsync(value.Value.ToString());
    }

    public async Task<Guid?> GetValueAsync()
    {
        var input = InputLocator;

        await input.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached
        });

        var str = await input.InputValueAsync();

        if (!string.IsNullOrWhiteSpace(str))
            return Guid.Parse(str);

        return null;
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
