using OpenQA.Selenium;

namespace Signum.Selenium;

public class ValueLineModalProxy : ModalProxy
{
    PropertyRoute? route;
    public ValueLineModalProxy(IWebElement element, PropertyRoute? route = null) : base(element)
    {
        this.route = route;
    }

    public ValueLineProxy ValueLine
    {
        get
        {
            var formGroup = this.Element.FindElement(By.CssSelector("div.modal-body div.form-group"));
            return new ValueLineProxy(formGroup, route!);
        }
    }
}

public static class ValueLineModalProxyExtensions
{
    public static ValueLineModalProxy AsValueLineModal(this IWebElement element)
    {
        return new ValueLineModalProxy(element);
    }

    public static ValueLineModalProxy AsValueLineModal<T, V>(this IWebElement element, Expression<Func<T, V>> propertyRoute)
        where T : IRootEntity
    {
        return new ValueLineModalProxy(element, PropertyRoute.Construct(propertyRoute));
    }
}
