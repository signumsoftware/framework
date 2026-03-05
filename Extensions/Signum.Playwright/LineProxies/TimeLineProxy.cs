namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for TimeSpan/TimeOnly input controls
/// Equivalent to Selenium's TimeLineProxy
/// </summary>
public class TimeLineProxy : BaseLineProxy
{
    public TimeLineProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    public override async Task SetValueUntypedAsync(object? value)
    {
        if (value == null)
        {
            await SetInputValueAsync("");
            return;
        }

        string stringValue;
        if (value is TimeSpan ts)
        {
            stringValue = ts.ToString(@"hh\:mm");
        }
        else if (value is TimeOnly t)
        {
            stringValue = t.ToString("HH:mm");
        }
        else
        {
            stringValue = value.ToString()!;
        }

        await SetInputValueAsync(stringValue);
    }

    public override async Task<object?> GetValueUntypedAsync()
    {
        var stringValue = await GetInputValueAsync();
        if (string.IsNullOrWhiteSpace(stringValue))
            return null;

        var type = Route.Type.UnNullify();

        if (type == typeof(TimeSpan))
            return TimeSpan.Parse(stringValue);
        
        if (type == typeof(TimeOnly))
            return TimeOnly.Parse(stringValue);

        return stringValue;
    }

    public override async Task<bool> IsReadonlyAsync()
    {
        var input = InputLocator.First;
        return !await input.IsEnabledAsync();
    }
}
