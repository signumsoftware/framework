using OpenQA.Selenium;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Selenium.LineProxies;
public class TimeLineProxy : BaseLineProxy
{
    public TimeLineProxy(IWebElement element, PropertyRoute route)
       : base(element, route)
    {
    }

    public  WebElementLocator InputLocator => this.Element.WithLocator(By.CssSelector("input[type=text].numeric"));

    public override object? GetValueUntyped() => this.GetValue();
    public override void SetValueUntyped(object? value) => this.SetValue((IFormattable?)value);
    public override bool IsReadonly() => InputLocator.Find().Let(e => e.IsDomDisabled() || e.IsDomReadonly());

    public void SetValue(IFormattable? value, string? format = null)
    {
        format ??= Reflector.FormatString(this.Route);

        var str = value == null ? null : value.ToString(format, null);

        InputLocator.Find().SafeSendKeys(str);
    }

    public IFormattable? GetValue()
    {
        var textLine = InputLocator.Find();

        var strValue = textLine.GetDomProperty("value");

        return strValue == null ? null : (IFormattable?)ReflectionTools.Parse(strValue, this.Route.Type);
    }
}
