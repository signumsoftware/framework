
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for NumberLine.tsx
/// </summary>
public class NumberLineProxy : BaseLineProxy
{
    public NumberLineProxy(ILocator element, PropertyRoute route)
        : base(element, route)
    {
    }

    public ILocator InputLocator => Element.Locator("input[type=text].numeric");
    public ILocator ReadonlyInputLocator => Element.Locator("input.numeric[readonly]");
    public ILocator ReadonlyDivLocator => Element.Locator("div.readonly.numeric");
    public ILocator AnyReadonlyLocator => ReadonlyInputLocator.Or(ReadonlyDivLocator);
    public ILocator AnyInputLocator => AnyReadonlyLocator.Or(InputLocator);

    public override async Task<object?> GetValueUntypedAsync()
        => await GetValueAsync();

    public override async Task SetValueUntypedAsync(object? value)
        => await SetValueAsync((IFormattable?)value);

    public override async Task<bool> IsReadonlyAsync()
    {
        if (await AnyReadonlyLocator.CountAsync() > 0)
            return true;

        var input = InputLocator;
        if (await input.CountAsync() > 0)
        {
            return await input.IsDisabledAsync() ||
                   await input.EvaluateAsync<bool>("e => e.hasAttribute('readonly')");
        }

        return false;
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

        await input.WaitVisibleAsync();

        await input.FillAsync(str ?? "");

        await input.BlurAsync();
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

        return string.IsNullOrWhiteSpace(strValue)
            ? null
            : (IFormattable?)ReflectionTools.Parse(strValue, this.Route.Type);
    }

    public async Task AssertValueAsync(string expectedValue)
    {
        if (!string.IsNullOrEmpty(expectedValue))
        {
            await Assertions.Expect(AnyInputLocator.First).ToHaveValueAsync(expectedValue);
            return;
        }

        await (await IsReadonlyAsync() && await ReadonlyDivLocator.CountAsync() > 0
            ? Assertions.Expect(ReadonlyDivLocator).ToHaveTextAsync("")
            : Assertions.Expect(AnyInputLocator.First).ToHaveValueAsync(""));
    }
}
