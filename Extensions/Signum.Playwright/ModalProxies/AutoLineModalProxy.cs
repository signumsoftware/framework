using DocumentFormat.OpenXml.Office2010.Drawing;
using Microsoft.Playwright;
using Signum.Playwright.LineProxies;

namespace Signum.Playwright.ModalProxies;

/// <summary>
/// Proxy for AutoLineModal.tsx
/// </summary>
public class AutoLineModalProxy : ModalProxy
{
    private PropertyRoute route;

    public AutoLineModalProxy(ILocator element, PropertyRoute route) : base(element)
    {
        this.route = route;
    }

    public async Task<BaseLineProxy> GetAutoLineAsync()
    {
        // Warten bis die form-group sichtbar ist
        var formGroup = this.Modal.Locator("div.modal-body div.form-group");
        await formGroup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        return BaseLineProxy.AutoLine(formGroup, route);
    }

    public async Task SetValueOk(string kommentar)
    {
        using (this)
        {
            await this.SetValueAsync(kommentar);
            await this.OkWaitClosedAsync();
        }
    }

    public async Task SetValueAsync(object? value)
    {
        await (await GetAutoLineAsync()).SetValueUntypedAsync(value);
    }
}

public static class ValueLineModalProxyExtensions
{
    public static AutoLineModalProxy AsAutoLineModal(this ILocator element, PropertyRoute pr)
    {
        return new AutoLineModalProxy(element, pr);
    }

    public static AutoLineModalProxy AsValueLineModal<T, V>(this ILocator element, Expression<Func<T, V>> propertyRoute)
        where T : IRootEntity
    {
        return new AutoLineModalProxy(element, PropertyRoute.Construct(propertyRoute));
    }
}
