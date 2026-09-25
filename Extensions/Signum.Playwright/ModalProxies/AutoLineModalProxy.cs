using Signum.Playwright.LineProxies;

namespace Signum.Playwright.ModalProxies;

/// <summary>
/// Proxy for AutoLineModal.tsx
/// </summary>
public class AutoLineModalProxy : ModalProxy
{
    public readonly PropertyRoute Route;

    public AutoLineModalProxy(ILocator element, PropertyRoute route) : base(element)
    {
        this.Route = route;
    }

    public async Task<BaseLineProxy> GetAutoLineAsync()
    {
        // Warten bis die form-group sichtbar ist
        ILocator formGroup = await GetLocator();

        return BaseLineProxy.AutoLine(formGroup, Route);
    }

    public async Task<ILocator> GetLocator()
    {
        var formGroup = this.Modal.Locator("div.modal-body div.form-group");
        await formGroup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        return formGroup;
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
