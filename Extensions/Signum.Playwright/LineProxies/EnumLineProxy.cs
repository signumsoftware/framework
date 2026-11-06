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
    protected ILocator RadioLocator => Element.Locator("input[type=radio]");

    public override async Task SetValueUntypedAsync(object? value)
    {
        if (value is bool b)
            value = b ? BooleanEnum.True : BooleanEnum.False;

        var strValue =
            value == null ? "" :
            value is Enum e ? e.ToString() :
            throw new UnexpectedValueException(value);

        if (await RadioLocator.CountAsync() > 0)
        {
            // Handle lineType="RadioGroup"
            await Element.Locator($"input[type=radio][value='{strValue}' i]").First.CheckAsync();
        }
        else if (await WidgetLocator.CountAsync() > 0)
        {
            // Handle React Widget dropdown
            var rw = WidgetLocator.First;
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
        string? strValue = null;

        if (await RadioLocator.CountAsync() > 0)
        {
            // Handle lineType="RadioGroup"
            var checkedRadio = Element.Locator("input[type=radio]:checked");

            strValue = await checkedRadio.CountAsync() == 0 ? null : await checkedRadio.First.GetAttributeAsync("value");
        }
        else if (await WidgetLocator.CountAsync() > 0)
        {
            // Handle React Widget dropdown
            strValue = await WidgetLocator.First.Locator("[data-value]").GetAttributeAsync("data-value");
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
        => await FormControlReadonlyLocator.CountAsync() > 0;
}
