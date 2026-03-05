using Microsoft.Playwright;

namespace Signum.Playwright.ModalProxies;

public class ValueLineModalProxy : ModalProxy
{
    private PropertyRoute route;

    public ValueLineModalProxy(IPage page, PropertyRoute route) : base(page)
    {
        this.route = route;
    }

    public ValueLineModalProxy(IPage page, ILocator modalElement, PropertyRoute route) : base(modalElement, page)
    {
        this.route = route;
    }

    public async Task<BaseLineProxy> GetAutoLineAsync()
    {
        var formGroup = Modal.Locator("div.modal-body div.form-group");
        await formGroup.WaitVisibleAsync();
        
        return await LineProxyHelpers.AutoLineAsync(formGroup.First, route, Page);
    }
}

public static class ValueLineModalProxyExtensions
{
    public static ValueLineModalProxy AsValueLineModal(this ModalProxy modal, PropertyRoute pr)
    {
        return new ValueLineModalProxy(modal.Page, modal.Modal, pr);
    }

    public static ValueLineModalProxy AsValueLineModal<T, V>(this ModalProxy modal, Expression<Func<T, V>> propertyRoute)
        where T : IRootEntity
    {
        return new ValueLineModalProxy(modal.Page, modal.Modal, PropertyRoute.Construct(propertyRoute));
    }
}
