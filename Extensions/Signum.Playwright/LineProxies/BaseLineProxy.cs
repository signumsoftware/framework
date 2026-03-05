using Microsoft.Playwright;
using Signum.Basics;

namespace Signum.Playwright.LineProxies;

/// <summary>
/// Base class for all line proxies in Playwright
/// Equivalent to Selenium's BaseLineProxy
/// </summary>
public abstract class BaseLineProxy
{
    public ILocator Element { get; }
    public PropertyRoute Route { get; }
    public IPage Page { get; }

    protected BaseLineProxy(ILocator element, PropertyRoute route, IPage page)
    {
        Element = element;
        Route = route;
        Page = page;
    }

    /// <summary>
    /// Set value with automatic type handling
    /// </summary>
    public abstract Task SetValueUntypedAsync(object? value);

    /// <summary>
    /// Get value with automatic type handling
    /// </summary>
    public abstract Task<object?> GetValueUntypedAsync();

    /// <summary>
    /// Check if the line is readonly
    /// </summary>
    public abstract Task<bool> IsReadonlyAsync();

    /// <summary>
    /// Factory method to create appropriate line proxy based on property route
    /// Equivalent to Selenium's AutoLine
    /// NOTE: Simplified implementation - expand based on your needs
    /// </summary>
    public static BaseLineProxy AutoLine(ILocator element, PropertyRoute route, IPage page)
    {
        var type = route.Type;
        
        // Remove nullable wrapper if present
        if (Nullable.GetUnderlyingType(type) != null)
            type = Nullable.GetUnderlyingType(type)!;

        // Entity types
        if (typeof(Entity).IsAssignableFrom(type) || typeof(Lite<>).IsAssignableFromGenericDefinition(type))
        {
            // TODO: Add logic to distinguish between EntityLine, EntityCombo, EntityDetail based on implementations
            return new EntityLineProxy(element, route, page);
        }

        // Bool
        if (type == typeof(bool))
        {
            return new CheckboxLineProxy(element, route, page);
        }

        // Enum
        if (type.IsEnum)
        {
            return new EnumLineProxy(element, route, page);
        }

        // DateTime
        if (type == typeof(DateTime) || type == typeof(DateOnly) || type == typeof(DateTimeOffset))
        {
            return new DateTimeLineProxy(element, route, page);
        }

        // TimeSpan
        if (type == typeof(TimeSpan) || type == typeof(TimeOnly))
        {
            return new TimeLineProxy(element, route, page);
        }

        // Guid
        if (type == typeof(Guid))
        {
            return new GuidBoxLineProxy(element, route, page);
        }

        // Numeric types
        if (type.IsNumericType())
        {
            return new NumberLineProxy(element, route, page);
        }

        // String - default to TextBox
        if (type == typeof(string))
        {
            // TODO: Detect multiline from validators
            return new TextBoxLineProxy(element, route, page);
        }

        // Default to text box
        return new TextBoxLineProxy(element, route, page);
    }

    /// <summary>
    /// Get the input element within the line
    /// </summary>
    protected virtual ILocator InputLocator => Element.Locator(".form-control, .form-control-readonly, input, textarea, select");

    /// <summary>
    /// Helper to get value from input
    /// </summary>
    protected async Task<string?> GetInputValueAsync()
    {
        var input = InputLocator.First;
        
        // Check if it's a select element
        var tagName = await input.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
        if (tagName == "select")
        {
            return await input.EvaluateAsync<string>("el => el.options[el.selectedIndex]?.text");
        }

        return await input.InputValueAsync();
    }

    /// <summary>
    /// Helper to set value in input
    /// </summary>
    protected async Task SetInputValueAsync(string? value)
    {
        var input = InputLocator.First;
        await input.FillAsync(value ?? "");
    }
}
