using OpenQA.Selenium;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Selenium.LineProxies;
public class DateTimeLineProxy : BaseLineProxy
{
    public DateTimeLineProxy(IWebElement element, PropertyRoute route)
       : base(element, route)
    {
    }

    public WebElementLocator InputLocator => this.Element.WithLocator(By.CssSelector("div.rw-date-picker input[type=text]"));

    public void SetValue(IFormattable? value, string? format = null)
    {
        format ??= Reflector.FormatString(this.Route);

        var str = value == null ? null : value.ToString(format, null);

        InputLocator.Find().SafeSendKeys(str);
    }

    public IFormattable? GetValue()
    {
        var textLine = InputLocator.Find();

        var strValue = textLine.GetAttribute("value");

        return strValue == null ? null : (IFormattable?)ReflectionTools.Parse(strValue, this.Route.Type);
    }


    public override object? GetValueUntyped() => this.GetValue();
    public override void SetValueUntyped(object? value) => SetValue((IFormattable?)value);
}
