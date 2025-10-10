using OpenQA.Selenium;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Selenium.LineProxies;
public class NumberLineProxy : BaseLineProxy
{
    public NumberLineProxy(IWebElement element, PropertyRoute route)
       : base(element, route)
    {
    }

    public override object? GetValueUntyped() => this.GetValue();
    public override void SetValueUntyped(object? value) => this.SetValue((IFormattable?)value);
    public override bool IsReadonly() => this.Element.WithLocator(By.CssSelector("input.numeric")).Find().Let(e => e.IsDomDisabled() || e.IsDomReadonly());
    
    public  WebElementLocator InputLocator => this.Element.WithLocator(By.CssSelector("input[type=text].numeric"));

    public void SetValue(IFormattable? value, string? format = null)
    {
        format ??= Reflector.FormatString(this.Route);

        var str = value == null ? null : value.ToString(format, null);

        if (str.HasText() && format.HasText() && format.ToUpper() == "P")
            str = str.Replace("%", "").Trim();

        InputLocator.Find().SafeSendKeys(str);
    }

    public IFormattable? GetValue()
    {
        var textLine = InputLocator.Find();

        var strValue = textLine.GetDomProperty("value");

        return strValue == null ? null : (IFormattable?)ReflectionTools.Parse(strValue, this.Route.Type);
    }
}
