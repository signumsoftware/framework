using OpenQA.Selenium;

namespace Signum.Selenium.LineProxies;
public class CheckboxLineProxy : BaseLineProxy
{
    public CheckboxLineProxy(IWebElement element, PropertyRoute route)
       : base(element, route)
    {
    }

    public  WebElementLocator CheckboxLocator => this.Element.WithLocator(By.CssSelector("input[type=checkbox]"));

    public void SetValue(bool value)
    {
        CheckboxLocator.Find().SetChecked(value);
    }

    public bool GetValue()
    {
        return CheckboxLocator.Find().Selected;
    }

    public override object? GetValueUntyped() => this.GetValue();
    public override void SetValueUntyped(object? value) => SetValue((bool)value!);
    public override bool IsReadonly() => CheckboxLocator.Find().Let(e => e.IsDomDisabled() || e.IsDomReadonly());
}
