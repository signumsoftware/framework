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

    public abstract WebElementLocator InputLocator { get; }

    public void SetValue(string? value)
    {
        InputLocator.Find().SafeSendKeys(value);
    }

    public string GetValue()
    {
        var textLine = InputLocator.Find(); 

        return /*textLine.GetAttribute("data-value") ??*/ textLine.GetAttribute("value");
    }

    public bool IsReadonly()
    {
        var element = InputLocator.Find();

        return element.HasClass("readonly") || element.HasClass("form-control-plaintext") || element.GetAttribute("readonly") != null;
    }
}

public class TextBoxLineProxy : TextBoxBaseLineProxy
{
    public TextBoxLineProxy(IWebElement element, PropertyRoute route) : base(element, route)
    {
    }

    public override WebElementLocator InputLocator => this.Element.WithLocator(By.CssSelector("input[type=text]"));
}

public class PasswordBoxLineProxy : TextBoxBaseLineProxy
{
    public PasswordBoxLineProxy(IWebElement element, PropertyRoute route) : base(element, route)
    {
    }

    public override WebElementLocator InputLocator => this.Element.WithLocator(By.CssSelector("input[type=password]"));
}


public class ColorBoxLineProxy : TextBoxBaseLineProxy
{
    public ColorBoxLineProxy(IWebElement element, PropertyRoute route) : base(element, route)
    {
    }

    public override WebElementLocator InputLocator => this.Element.WithLocator(By.CssSelector("input[type=color]"));
}
