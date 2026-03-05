using Microsoft.Playwright;
using Signum.Basics;

namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for checkbox controls
/// Equivalent to Selenium's CheckboxLineProxy
/// </summary>
public class CheckboxLineProxy : BaseLineProxy
{
    public CheckboxLineProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    protected override ILocator InputLocator => Element.Locator("input[type='checkbox']");

    public override async Task SetValueUntypedAsync(object? value)
    {
        var boolValue = value is bool b && b;
        await SetCheckedAsync(boolValue);
    }

    public override async Task<object?> GetValueUntypedAsync()
    {
        return await GetCheckedAsync();
    }

    public async Task SetCheckedAsync(bool isChecked)
    {
        var checkbox = InputLocator.First;
        var currentState = await checkbox.IsCheckedAsync();

        if (currentState != isChecked)
        {
            await checkbox.ClickAsync();
            
            // Wait for state to change
            await Assertions.Expect(checkbox).ToBeCheckedAsync(new LocatorAssertionsToBeCheckedOptions
            {
                Checked = isChecked
            });
        }
    }

    public async Task<bool> GetCheckedAsync()
    {
        var checkbox = InputLocator.First;
        return await checkbox.IsCheckedAsync();
    }

    public override async Task<bool> IsReadonlyAsync()
    {
        var checkbox = InputLocator.First;
        return !await checkbox.IsEnabledAsync();
    }
}
