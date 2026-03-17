using Microsoft.Playwright;
using Signum.Playwright.LineProxies;

namespace Signum.Playwright.ModalProxies;

public class AutoLineModalProxy : ModalProxy
{
    private PropertyRoute route;
    public ILocator Element { get; }
    public IPage Page { get; }

    public AutoLineModalProxy(ILocator element, IPage page, PropertyRoute route) : base(element, page)
    {
        this.Element = element;
        this.Page = page;
        this.route = route;
    }

    public async Task<BaseLineProxy> GetAutoLineAsync()
    {
        // Warten bis die form-group sichtbar ist
        var formGroup = Element.Locator("div.modal-body div.form-group");
        await formGroup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        return BaseLineProxy.AutoLine(formGroup, route, Page);
    }
}

public static class ValueLineModalProxyExtensions
{
    public static AutoLineModalProxy AsAutoLineModal(this ILocator element, IPage page, PropertyRoute pr)
    {
        return new AutoLineModalProxy(element, page, pr);
    }

    public static AutoLineModalProxy AsValueLineModal<T, V>(this ILocator element, IPage page, Expression<Func<T, V>> propertyRoute)
        where T : IRootEntity
    {
        return new AutoLineModalProxy(element, page, PropertyRoute.Construct(propertyRoute));
    }
}
