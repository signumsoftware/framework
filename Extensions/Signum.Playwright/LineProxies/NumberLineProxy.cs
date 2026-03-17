
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.Playwright.LineProxies;

public class NumberLineProxy : BaseLineProxy
{
    public ILocator Element { get; }
    public IPage Page { get; }

    public NumberLineProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
        this.Element = element;
        this.Page = page;
    }

    public ILocator InputLocator => Element.Locator("input[type=text].numeric");

    public override async Task<object?> GetValueUntypedAsync()
        => await GetValueAsync();

    public override async Task SetValueUntypedAsync(object? value)
        => await SetValueAsync((IFormattable?)value);

    public override async Task<bool> IsReadonlyAsync()
    {
        var input = InputLocator;

        await input.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached
        });

        return await input.IsDisabledAsync() ||
               await input.EvaluateAsync<bool>("e => e.hasAttribute('readonly')");
    }

    public async Task SetValueAsync(IFormattable? value, string? format = null)
    {
        format ??= Reflector.GetFormatString(this.Route);

        var str = value == null ? null : value.ToString(format, null);

        // Prozentformat-Sonderfall
        if (!string.IsNullOrWhiteSpace(str) &&
            !string.IsNullOrWhiteSpace(format) &&
            format.ToUpper() == "P")
        {
            str = str.Replace("%", "").Trim();
        }

        var input = InputLocator;

        await input.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });

        // Playwright Best Practice: erst leeren
        await input.FillAsync("");

        if (!string.IsNullOrEmpty(str))
            await input.FillAsync(str);
    }

    public async Task<IFormattable?> GetValueAsync()
    {
        var input = InputLocator;

        await input.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached
        });

        var strValue = await input.InputValueAsync();

        return string.IsNullOrWhiteSpace(strValue)
            ? null
            : (IFormattable?)ReflectionTools.Parse(strValue, this.Route.Type);
    }
}
