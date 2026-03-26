namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for EnumLine.tsx
/// </summary>
public class EnumLineProxy : BaseLineProxy
{
    public EnumLineProxy(ILocator element, PropertyRoute route)
        : base(element, route)
    {
    }

    protected ILocator InputLocator => Element.Locator("select.form-control, select");

    public override async Task SetValueUntypedAsync(object? value)
    {
        if (value == null)
        {
            await SelectByIndexAsync(0); // Select first (empty) option
            return;
        }

        var enumValue = value is Enum ? value.ToString() : value.ToString();
        await SelectByValueAsync(enumValue!);
    }

    public override async Task<object?> GetValueUntypedAsync()
    {
        var select = InputLocator.First;
        var selectedValue = await select.EvaluateAsync<string?>("el => el.value");

        if (string.IsNullOrEmpty(selectedValue))
            return null;

        var enumType = Route.Type.UnNullify();
        if (enumType.IsEnum)
        {
            return Enum.Parse(enumType, selectedValue);
        }

        // For nullable bool (Yes/No dropdown)
        if (enumType == typeof(bool))
        {
            return bool.Parse(selectedValue);
        }

        return selectedValue;
    }

    public async Task SelectByValueAsync(string value)
    {
        var select = InputLocator.First;
        await select.SelectOptionAsync(value);
    }

    public async Task SelectByTextAsync(string text)
    {
        var select = InputLocator.First;
        await select.SelectOptionAsync(new SelectOptionValue { Label = text });
    }

    public async Task SelectByIndexAsync(int index)
    {
        var select = InputLocator.First;
        await select.SelectOptionAsync(new SelectOptionValue { Index = index });
    }

    public override async Task<bool> IsReadonlyAsync()
    {
        var select = InputLocator.First;
        return !await select.IsEnabledAsync();
    }
}
