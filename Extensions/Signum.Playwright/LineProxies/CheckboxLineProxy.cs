namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for CheckboxLine.tsx
/// </summary>
public class CheckboxLineProxy : BaseLineProxy
{
    public CheckboxLineProxy(ILocator element, PropertyRoute route)
        : base(element, route)
    {
    }

    public ILocator CheckboxLocator => this.Element.Locator("input[type=checkbox]");

    public async Task SetValueAsync(bool value)
    {
        if (value)
            await CheckboxLocator.CheckAsync();
        else
            await CheckboxLocator.UncheckAsync();
    }

    public async Task<bool> GetValueAsync()
    {
        return await CheckboxLocator.IsCheckedAsync();
    }

    public override async Task<object?> GetValueUntypedAsync()
        => await GetValueAsync();

    public override async Task SetValueUntypedAsync(object? value)
        => await SetValueAsync((bool)value!);

    public override async Task<bool> IsReadonlyAsync()
    {
        return await CheckboxLocator.IsDisabledAsync()
            || (await CheckboxLocator.GetAttributeAsync("readonly")) != null;
    }
}
