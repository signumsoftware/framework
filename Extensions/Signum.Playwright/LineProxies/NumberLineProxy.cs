using Microsoft.Playwright;
using Signum.Basics;
using Signum.Entities.Reflection;

namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for numeric input controls
/// Equivalent to Selenium's NumberLineProxy
/// </summary>
public class NumberLineProxy : BaseLineProxy
{
    public NumberLineProxy(ILocator element, PropertyRoute route, IPage page)
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

        var type = Route.Type.UnNullify();

        if (type == typeof(int)) return int.Parse(stringValue);
        if (type == typeof(long)) return long.Parse(stringValue);
        if (type == typeof(short)) return short.Parse(stringValue);
        if (type == typeof(byte)) return byte.Parse(stringValue);
        if (type == typeof(decimal)) return decimal.Parse(stringValue);
        if (type == typeof(double)) return double.Parse(stringValue);
        if (type == typeof(float)) return float.Parse(stringValue);

        return stringValue;
    }

    public override async Task<bool> IsReadonlyAsync()
    {
        var input = InputLocator.First;
        return !await input.IsEnabledAsync() || await input.EvaluateAsync<bool>("el => el.hasAttribute('readonly')");
    }
}
