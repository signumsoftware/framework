using OpenQA.Selenium;

namespace Signum.Selenium;

public class AutoLineModalProxy : ModalProxy
{
    PropertyRoute route;
    public AutoLineModalProxy(IWebElement element, PropertyRoute route) : base(element)
    {
        this.route = route;
    }

    public BaseLineProxy AutoLine
    {
        get
        {
            var formGroup = this.Element.WaitElementVisible(By.CssSelector("div.modal-body div.form-group"));
            return BaseLineProxy.AutoLine(formGroup, route!);
        }
    }
}

public static class ValueLineModalProxyExtensions
{
    public static AutoLineModalProxy AsAutoLineModal(this IWebElement element, PropertyRoute pr)
    {
        return new AutoLineModalProxy(element, pr);
    }

    public static AutoLineModalProxy AsValueLineModal<T, V>(this IWebElement element, Expression<Func<T, V>> propertyRoute)
        where T : IRootEntity
    {
        return new AutoLineModalProxy(element, PropertyRoute.Construct(propertyRoute));
    }
}
