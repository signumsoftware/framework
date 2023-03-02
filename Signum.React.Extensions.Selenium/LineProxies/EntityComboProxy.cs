using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using Signum.Entities;
using Signum.Utilities;
using OpenQA.Selenium.Support.UI;

namespace Signum.React.Selenium;

public class EntityComboProxy : EntityBaseProxy
{
    public EntityComboProxy(IWebElement element, PropertyRoute route)
        : base(element, route)
    {
    }

    public SelectElement ComboElement
    {
        get { return this.Element.FindElement(By.CssSelector("select")).SelectElement(); }
    }

    public IWebElement DropdownListInput
    {
        get { return this.Element.FindElement(By.CssSelector(".rw-dropdown-list-input")); }
    }


    public Lite<IEntity>? LiteValue
    {
        get
        {
            var ei = EntityInfo();

            if (ei == null)
                return null;

            var text = this.ComboElement.AllSelectedOptions.SingleOrDefaultEx()?.Text;

            return ei.ToLite(text);
        }
        set
        {
            var val = value == null ? "" : value.Key();
            this.Element.GetDriver().Wait(() => this.ComboElement.Options.Any(o => o.GetAttribute("value") == val));
            this.ComboElement.SelectByValue(val);
        }
    }

    public List<Lite<Entity>?> Options()
    {
        return this.ComboElement.Options
            .Select(o => Lite.Parse(o.GetAttribute("value"))?.Do(l => l.SetModel(o.Text)))
            .ToList();
    }

    public FrameModalProxy<T> View<T>() where T : ModifiableEntity
    {
        return base.ViewInternal<T>();
    }

    public void SelectLabel(string label)
    {

        this.Element.GetDriver().Wait(() =>
            this.ComboElement.WrappedElement.FindElements(By.CssSelector("option")).Any(a => a.Text.Contains(label)));

        WaitChanges(() =>
            this.ComboElement.SelectByText(label),
            "ComboBox selected");
    }

    public void SelectIndex(int index)
    {
        this.Element.GetDriver().Wait(() =>
                    this.ComboElement.WrappedElement.FindElements(By.CssSelector("option")).Count > index);

        WaitChanges(() =>
            this.ComboElement.SelectByIndex(index + 1),
            "ComboBox selected");
    }

    public EntityInfoProxy? EntityInfo()
    {
        return EntityInfoInternal(null);
    }
    public void AssertOptions(Lite<Entity>[] list, bool removeNullElement = true, bool orderIndependent = false)
    {
        this.Element.GetDriver().Wait(() =>
        {
            var options = this.Options();
            if (removeNullElement)
                options = options.NotNull().ToList()!;

            if (orderIndependent)
                return options.OrderBy(a => a?.Id).SequenceEqual(list.OrderBy(a => a?.Id));
            else
                return options.SequenceEqual(list);
        });
    }
}
