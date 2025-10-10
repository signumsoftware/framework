using OpenQA.Selenium;
using System.Collections.ObjectModel;

namespace Signum.Selenium;

public class EntityRepeaterProxy : EntityBaseProxy
{
    public override PropertyRoute ItemRoute => base.ItemRoute.Add("Item");

    public EntityRepeaterProxy(IWebElement element, PropertyRoute route)
        : base(element, route)
    {
    }


    public override object? GetValueUntyped() => throw new NotImplementedException();
    public override void SetValueUntyped(object? value) => throw new NotImplementedException();
    public override bool IsReadonly() => throw new NotImplementedException();

    public virtual WebElementLocator ItemsContainerElement
    {
        get { return this.Element.WithLocator(By.CssSelector(".sf-repater-elements")); }
    }

    public virtual WebElementLocator ItemElement(int index)
    {
        return this.ItemsContainerElement.CombineCss(" div:nth-child({0}) > fieldset.sf-repeater-element".FormatWith(index + 1));
    }

    public void WaitItemLoaded(int index)
    {
        ItemElement(index).WaitPresent();
    }

    public virtual void MoveUp(int index)
    {
        ItemElement(index).CombineCss(" a.move-up").Find().Click();
    }

    public virtual void MoveDown(int index)
    {
        ItemElement(index).CombineCss(" a.move-down").Find().Click();
    }

    public virtual int ItemsCount()
    {
        return this.Items().Count;
    }

    public virtual ReadOnlyCollection<IWebElement> Items()
    {
        return this.ItemsContainerElement.CombineCss(" fieldset.sf-repeater-element").FindElements();
    }

    public LineContainer<T> Details<T>(int index) where T : ModifiableEntity
    {
        return new LineContainer<T>(ItemElement(index).WaitPresent(), this.ItemRoute);
    }

    public IWebElement RemoveElementIndex(int index)
    {
        return ItemElement(index).CombineCss(" a.remove").Find();
    }

    public void Remove(int index)
    {
        this.RemoveElementIndex(index).Click();
    }

    public EntityInfoProxy? EntityInfo(int index)
    {
        return EntityInfoInternal(index);
    }

    public LineContainer<T> CreateElement<T>() where T : ModifiableEntity
    {
        var count = this.ItemsCount();

        CreateEmbedded<T>();

        return this.Details<T>(count + 1);
    }

    public LineContainer<T> LastDetails<T>() where T : ModifiableEntity
    {
        return this.Details<T>(this.ItemsCount() - 1);
    }
}
