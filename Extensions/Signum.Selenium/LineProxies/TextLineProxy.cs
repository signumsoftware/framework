using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Selenium.LineProxies;
public abstract class TextBoxBaseLineProxy : BaseLineProxy
{
    public TextBoxBaseLineProxy(IWebElement element, PropertyRoute route)
       : base(element, route)
    {
    }


    public override object? GetValueUntyped() => this.GetValue();
    public override void SetValueUntyped(object? value) => this.SetValue((string?)value);

    public WebElementLocator InputLocator => this.Element.WithLocator(By.CssSelector(".form-control, .form-control-readonly"));

    public void SetValue(string? value)
    {
        InputLocator.Find().SafeSendKeys(value);
    }

    public string GetValue()
    {
        var textLine = InputLocator.Find(); 

        return /*textLine.GetDomAttribute("data-value") ??*/ textLine.GetDomProperty("value")!;
    }

    public override bool IsReadonly()
    {
        var element = InputLocator.Find();

        return element.HasClass("readonly") || element.HasClass("form-control-plaintext") || element.IsDomDisabled() || element.IsDomReadonly();
    }
}

public class TextBoxLineProxy : TextBoxBaseLineProxy
{
    public TextBoxLineProxy(IWebElement element, PropertyRoute route) : base(element, route)
    {
    }
}

public class PasswordBoxLineProxy : TextBoxBaseLineProxy
{
    public PasswordBoxLineProxy(IWebElement element, PropertyRoute route) : base(element, route)
    {
    }
}


public class ColorBoxLineProxy : TextBoxBaseLineProxy
{
    public ColorBoxLineProxy(IWebElement element, PropertyRoute route) : base(element, route)
    {
    }
}
