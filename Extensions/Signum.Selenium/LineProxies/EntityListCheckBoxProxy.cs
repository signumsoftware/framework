using OpenQA.Selenium;

namespace Signum.Selenium;

public class EntityListCheckBoxProxy : EntityBaseProxy
{
    public EntityListCheckBoxProxy(IWebElement element, PropertyRoute route)
        : base(element, route)
    {
    }

    public override object? GetValueUntyped() => throw new NotImplementedException();
    public override void SetValueUntyped(object? value) => throw new NotImplementedException();
    public override bool IsReadonly() => throw new NotImplementedException();

    public WebElementLocator CheckBoxElement(Lite<Entity> lite)
    {
        return this.Element.WithLocator(By.CssSelector("input[name='{0}']".FormatWith(lite.Key())));
    }

    public List<Lite<Entity>> GetDataElements()
    {
        return this.Element.WithLocator(By.CssSelector("label.sf-checkbox-element")).FindElements().Select(e =>
        {
            var lite = Lite.Parse(e.FindElement(By.CssSelector("input[type=checkbox]")).GetDomAttributeOrThrow("name"));
            lite.SetModel(e.FindElement(By.CssSelector("span.sf-entitStrip-link")).Text);
            return lite;
        }).ToList();
    }

    public void SetChecked(Lite<Entity> lite, bool isChecked)
    {
        CheckBoxElement(lite).Find().SetChecked(isChecked);
    }

    public void AssertDataElements(Lite<Entity>[] list, bool orderIndependent = false)
    {
        this.Element.GetDriver().Wait(() =>
        {
            var options = this.GetDataElements();

            if (orderIndependent)
                return options.OrderBy(a => a.Id).SequenceEqual(list.OrderBy(a => a.Id));
            else
                return options.SequenceEqual(list);
        });
    }
}
