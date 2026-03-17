using Microsoft.Playwright;
using Signum.Basics;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.Playwright.LineProxies;

public class DateTimeLineProxy : BaseLineProxy
{
    public DateTimeLineProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    public ILocator InputLocator => this.Element.Locator("div.rw-date-picker input[type=text]");
    public ILocator InputReadonlyLocator => this.Element.Locator("input.sf-readonly-date");

    public async Task SetValueAsync(IFormattable? value, string? format = null)
    {
        format ??= Reflector.GetFormatString(this.Route);

        var str = value == null ? null : value.ToString(format, null);

        await InputLocator.FillAsync(str ?? "");
    }

    public async Task<IFormattable?> GetValueAsync()
    {
        var readonlyVisible = await InputReadonlyLocator.CountAsync() > 0;

        var locator = readonlyVisible ? InputReadonlyLocator : InputLocator;

        var strValue = await locator.InputValueAsync();

        return strValue == null ? null :
            (IFormattable?)ReflectionTools.Parse(strValue, this.Route.Type);
    }

    public override async Task<object?> GetValueUntypedAsync() => await GetValueAsync();
    public override async Task SetValueUntypedAsync(object? value) => await SetValueAsync((IFormattable?)value);

    public override async Task<bool> IsReadonlyAsync()
        => await InputReadonlyLocator.CountAsync() > 0;
}
