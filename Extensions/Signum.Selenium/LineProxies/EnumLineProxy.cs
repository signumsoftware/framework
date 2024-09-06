using OpenQA.Selenium;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Selenium.LineProxies;
public class EnumLineProxy : BaseLineProxy
{
    public EnumLineProxy(IWebElement element, PropertyRoute route)
       : base(element, route)
    {
    }


    public override object? GetValueUntyped() => this.GetValue();
    public override void SetValueUntyped(object? value) => this.SetValue(value);

    public  WebElementLocator SelectLocator => this.Element.WithLocator(By.CssSelector("select, input, div"));
    public  WebElementLocator WidgetLocator => this.Element.WithLocator(By.CssSelector("div.rw-dropdown-list"));

    public void SetValue(object? value)
    {
        if(value is bool b)
            value = b ? BooleanEnum.True : BooleanEnum.False;

        var strValue = 
            value == null ? "" : 
            value is Enum e ? e.ToString() : 
            throw new UnexpectedValueException(value);

        var rw = WidgetLocator.TryFind();
        if (rw != null)
        {
            var popup = rw.TryFindElement(By.CssSelector(".rw-popup-container"));
            if (popup == null || !popup.Displayed)
            {
                rw.FindElement(By.CssSelector(".rw-dropdown-list-value")).Click();
                popup = rw.WaitElementVisible(By.CssSelector(".rw-popup-container"));
            }

            popup.FindElement(By.CssSelector($"[data-value='{strValue}']")).Click();
        }
        else
            SelectLocator.Find().SelectElement().SelectByValue(strValue);
    }

    public object? GetValue()
    {
        var rw = WidgetLocator.TryFind();
        string? strValue = null;
        if (rw != null)
        {
            strValue = rw.FindElement(By.CssSelector("[data-value]")).GetAttribute("data-value");
        }
        else
        {
            var elem = this.SelectLocator.Find();

            strValue = elem.TagName == "select" ? elem.SelectElement().SelectedOption.GetAttribute("value").ToString() :
                elem.GetAttribute("data-value");

        }

        if(strValue.IsNullOrEmpty())
            return null;

        if (Route.Type.UnNullify() == typeof(bool))
            return ReflectionTools.Parse<BooleanEnum>(strValue) == BooleanEnum.True;

        return ReflectionTools.Parse(strValue, Route.Type);

    }
}
