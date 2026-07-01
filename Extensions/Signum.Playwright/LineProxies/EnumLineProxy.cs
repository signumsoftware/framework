using Signum.Utilities.Reflection;

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

    protected ILocator SelectLocator => Element.Locator("select.form-select, .form-control, .form-control-plaintext");
    protected ILocator WidgetLocator => Element.Locator("div.rw-dropdown-list");

    public override async Task SetValueUntypedAsync(object? value)
    {
        if (value is bool b)
            value = b ? BooleanEnum.True : BooleanEnum.False;

        var strValue =
            value == null ? "" :
            value is Enum e ? e.ToString() :
            throw new UnexpectedValueException(value);

        var rw = WidgetLocator.First;
        var rwCount = await rw.CountAsync();

        if (rwCount > 0)
        {
            // Handle React Widget dropdown
            var popup = rw.Locator(".rw-popup-container");

            if (!await popup.IsVisibleAsync())
            {
                await rw.Locator(".rw-dropdown-list-value").ClickAsync();
                await popup.WaitVisibleAsync();
            }

            await popup.Locator($"[data-value='{strValue}']").ClickAsync();
        }
        else
        {
            // Handle standard select element
            var select = SelectLocator.First;
            await select.SelectOptionAsync(strValue);
        }
    }

    public override async Task<object?> GetValueUntypedAsync()
    {
        var rw = WidgetLocator.First;
        var rwCount = await rw.CountAsync();
        string? strValue = null;

        if (rwCount > 0)
        {
            // Handle React Widget dropdown
            strValue = await rw.Locator("[data-value]").GetAttributeAsync("data-value");
        }
        else
        {
            // Handle standard select element
            var elem = SelectLocator.First;
            var tagName = await elem.EvaluateAsync<string>("el => el.tagName.toLowerCase()");

            if (tagName == "select")
            {
                strValue = await elem.EvaluateAsync<string?>("el => el.value");
            }
            else
            {
                strValue = await elem.GetAttributeAsync("data-value");
            }
        }

        if (string.IsNullOrEmpty(strValue))
            return null;

        if (Route.Type.UnNullify() == typeof(bool))
            return ReflectionTools.Parse<BooleanEnum>(strValue) == BooleanEnum.True;

        return ReflectionTools.Parse(strValue, Route.Type);
    }



    public override async Task<bool> IsReadonlyAsync()
    {
        var readonlyInput = Element.Locator("input[readonly]");
        var count = await readonlyInput.CountAsync();
        return count > 0;
    }
}
