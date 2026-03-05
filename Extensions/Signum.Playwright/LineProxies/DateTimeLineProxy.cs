using Microsoft.Playwright;
using Signum.Basics;

namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for DateTime/DateOnly input controls
/// Equivalent to Selenium's DateTimeLineProxy
/// </summary>
public class DateTimeLineProxy : BaseLineProxy
{
    public DateTimeLineProxy(ILocator element, PropertyRoute route, IPage page)
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
        if (value is DateTime dt)
        {
            stringValue = dt.ToString("yyyy-MM-ddTHH:mm");
        }
        else if (value is DateOnly d)
        {
            stringValue = d.ToString("yyyy-MM-dd");
        }
        else if (value is DateTimeOffset dto)
        {
            stringValue = dto.ToString("yyyy-MM-ddTHH:mm");
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

        if (type == typeof(DateTime))
            return DateTime.Parse(stringValue);
        
        if (type == typeof(DateOnly))
            return DateOnly.Parse(stringValue);
        
        if (type == typeof(DateTimeOffset))
            return DateTimeOffset.Parse(stringValue);

        return stringValue;
    }

    public override async Task<bool> IsReadonlyAsync()
    {
        var input = InputLocator.First;
        return !await input.IsEnabledAsync();
    }

    /// <summary>
    /// Set date value
    /// </summary>
    public async Task SetDateAsync(DateTime date)
    {
        await SetValueUntypedAsync(date);
    }

    /// <summary>
    /// Get date value
    /// </summary>
    public async Task<DateTime?> GetDateAsync()
    {
        return (DateTime?)await GetValueUntypedAsync();
    }
}
