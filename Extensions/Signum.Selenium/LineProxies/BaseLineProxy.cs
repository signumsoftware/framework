using OpenQA.Selenium;

namespace Signum.Selenium;

public class BaseLineProxy
{
    public IWebElement Element { get; private set; }

    public PropertyRoute Route { get; private set; }

    public BaseLineProxy(IWebElement element, PropertyRoute route)
    {
        this.Element = element;
        this.Route = route;
    }

    protected static string ToVisible(bool visible)
    {
        return visible ? "visible" : "not visible";
    }
}
