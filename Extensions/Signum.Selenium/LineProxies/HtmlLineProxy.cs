using OpenQA.Selenium;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Selenium.LineProxies;
public class HtmlLineProxy : BaseLineProxy
{
    public HtmlLineProxy(IWebElement element, PropertyRoute route)
       : base(element, route)
    {
    }

    public WebElementLocator InputLocator => this.Element.WithLocator(By.CssSelector("div.notranslate.public-DraftEditor-content"));

    public void SetValue(string? value)
    {
        InputLocator.Find().SendKeys(value ?? "");
    }

    public string? GetValue()
    {
        var textLine = InputLocator.Find();

        var strValue = textLine.Text;

        return strValue;
    }

    public override object? GetValueUntyped() => this.GetValue();
    public override void SetValueUntyped(object? value) => SetValue((string?)value);
    public override bool IsReadonly() => throw new NotImplementedException();
}
