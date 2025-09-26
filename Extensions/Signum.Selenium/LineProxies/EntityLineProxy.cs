using OpenQA.Selenium;

namespace Signum.Selenium;

public class EntityLineProxy : EntityBaseProxy
{
    public EntityLineProxy(IWebElement element, PropertyRoute route)
        : base(element, route)
    {
    }

    public override object? GetValueUntyped() => this.GetLite();
    public override void SetValueUntyped(object? value) => this.SetLite(value is Entity e ? e.ToLite() : (Lite<Entity>?)value);

    public void SetLite(Lite<IEntity>? value)
    {
        if (this.EntityInfo() != null)
            this.Remove();

        if (value != null)
        {
            if (AutoCompleteElement.IsVisible())
                AutoComplete(value);
            else if (FindButton != null)
                this.Find().SelectLite(value);
            else
                throw new NotImplementedException("AutoComplete");
        }
    }

    public Lite<Entity>? GetLite()
    {
        return EntityInfo()?.ToLite();
    }

    public WebElementLocator AutoCompleteElement
    {
        get { return this.Element.WithLocator(By.CssSelector(".sf-entity-autocomplete")); }
    }

    public void AutoComplete(Lite<IEntity> lite)
    {
        base.AutoCompleteWaitChanges(AutoCompleteElement.Find(), Element,  lite);
    }

    public void AutoCompleteBasic(Lite<IEntity> lite)
    {
        AutoCompleteBasic(AutoCompleteElement.Find(), Element, lite);
    }

    public void AutoComplete(string beginning)
    {
        base.AutoCompleteWaitChanges(AutoCompleteElement.Find(), Element, beginning);
    }

    public void AutoCompleteBasic(string beginning)
    {
        AutoCompleteBasic(AutoCompleteElement.Find(), Element, beginning);
    }

    public FrameModalProxy<T> View<T>() where T : ModifiableEntity
    {
        return base.ViewInternal<T>();
    }

    public EntityInfoProxy? EntityInfo()
    {
        return EntityInfoInternal(null);
    }

    public override bool IsReadonly()
    {
        return Element.TryFindElement(By.CssSelector(".form-control[readonly]")) != null || this.Element.IsDomDisabled() || this.Element.IsDomReadonly();
    }
}
