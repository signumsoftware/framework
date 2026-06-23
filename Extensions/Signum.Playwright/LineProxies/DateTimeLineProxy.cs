using Microsoft.Playwright;
using Signum.Basics;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for DateTimeLine.tsx
/// </summary>
public class DateTimeLineProxy : BaseLineProxy
{
    public DateTimeLineProxy(ILocator element, PropertyRoute route)
        : base(element, route)
    {
    }

    public ILocator InputLocator => this.Element.Locator("div.rw-date-picker input[type=text]");
    public ILocator ReadonlyInputLocator => this.Element.Locator("input.sf-readonly-date");
    public ILocator ReadonlyDivLocator => this.Element.Locator("div.sf-readonly-date");
    public ILocator AnyReadonlyLocator => ReadonlyInputLocator.Or(ReadonlyDivLocator);
    public ILocator AnyInputLocator => ReadonlyInputLocator.Or(InputLocator);

    public async Task SetValueAsync(IFormattable? value, string? format = null)
    {
        format ??= Reflector.GetFormatString(this.Route);

        var str = value == null ? null : value.ToString(format, null);

        await InputLocator.FillAsync(str ?? "");
    }

    public async Task<IFormattable?> GetValueAsync()
        => await ExtractValueAsync(AnyInputLocator.First);

    public async Task<IFormattable?> GetValueReadonlyAsync()
        => await ExtractValueAsync(AnyReadonlyLocator.First);

    private async Task<IFormattable?> ExtractValueAsync(ILocator locator)
    {
        await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached });

        var tagName = await locator.EvaluateAsync<string>("e => e.tagName");
        var strValue = tagName == "DIV"
            ? await locator.InnerTextAsync()
            : await locator.InputValueAsync();

        return string.IsNullOrWhiteSpace(strValue) ? null :
            (IFormattable?)ReflectionTools.Parse(strValue, this.Route.Type);
    }

    public override async Task<object?> GetValueUntypedAsync() => await GetValueAsync();
    public override async Task SetValueUntypedAsync(object? value) => await SetValueAsync((IFormattable?)value);

    public override async Task<bool> IsReadonlyAsync()
        => await AnyReadonlyLocator.CountAsync() > 0;

    public async Task AssertValueAsync(string expectedValue)
    {
        if (!string.IsNullOrEmpty(expectedValue))
        {
            await Assertions.Expect(AnyInputLocator.First).ToHaveValueAsync(expectedValue);
            return;
        }

        await (await IsReadonlyAsync()
            ? Assertions.Expect(ReadonlyDivLocator).ToHaveTextAsync("")
            : Assertions.Expect(AnyInputLocator.First).ToHaveValueAsync(""));
    }
}
