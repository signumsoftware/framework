using OpenQA.Selenium;

namespace Signum.Selenium.LineProxies;

public class GuidBoxLineProxy : BaseLineProxy
{


    public GuidBoxLineProxy(IWebElement element, PropertyRoute route)
         : base(element, route)
    {
    }

    public WebElementLocator InputLocator => this.Element.WithLocator(By.CssSelector("input[type=text]"));

    public override object? GetValueUntyped() => this.GetValue();
    public override void SetValueUntyped(object? value) => this.SetValue((Guid?)value);


    public void SetValue(Guid? value)
    {
        InputLocator.Find().SafeSendKeys(value?.ToString() ?? "");
    }

    public Guid? GetValue()
    {
        var textLine = InputLocator.Find();

        var str = textLine.GetDomProperty("value");

        if (str.HasText())
            return Guid.Parse(str);

        return null;
    }

    public override bool IsReadonly()
    {
        var element = InputLocator.Find();

        return element.HasClass("readonly") || element.HasClass("form-control-plaintext") || element.IsDomDisabled() || element.IsDomReadonly();
    }
}
