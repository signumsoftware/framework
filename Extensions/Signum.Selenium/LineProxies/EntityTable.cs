using OpenQA.Selenium;

namespace Signum.Selenium;

public class EntityTableProxy : EntityBaseProxy
{
    public override PropertyRoute ItemRoute => base.ItemRoute.Add("Item");

    public EntityTableProxy(IWebElement element, PropertyRoute route)
        : base(element, route)
    {
    }


    public override object? GetValueUntyped() => throw new NotImplementedException();
    public override void SetValueUntyped(object? value) => throw new NotImplementedException();
    public override bool IsReadonly() => throw new NotImplementedException();

    public virtual WebElementLocator TableElement
    {
        get { return this.Element.WithLocator(By.CssSelector(".sf-table")); }
    }

    public virtual WebElementLocator RowElement(int index)
    {
        return this.TableElement.CombineCss(" > tbody > tr:nth-child({0})".FormatWith(index + 1));
    }

    public void WaitItemLoaded(int index)
    {
        RowElement(index).WaitPresent();
    }

    public virtual int RowsCount()
    {
        return this.TableElement.CombineCss(" > tbody > tr").FindElements().Count;
    }

    public EntityTableRow<T> Row<T>(int index) where T : ModifiableEntity
    {
        return new EntityTableRow<T>(this, RowElement(index).WaitPresent(), this.ItemRoute);
    }

    internal int IndexOf(IWebElement row)
    {
        return this.TableElement.CombineCss(" > tbody > tr").FindElements().IndexOf(row);
    }

    public IWebElement RemoveRowButton(int index)
    {
        return RowElement(index).CombineCss(" .sf-remove").Find();
    }

    public void Remove(int index)
    {
        this.RemoveRowButton(index).Click();
    }

    public EntityInfoProxy? EntityInfo(int index)
    {
        return EntityInfoInternal(index);
    }

    public EntityTableRow<T> CreateRow<T>() where T : ModifiableEntity
    {
        CreateEmbedded<T>();
        return this.LastRow<T>();
    }

    public EntityTableRow<T> LastRow<T>() where T : ModifiableEntity
    {
        return this.Row<T>(this.RowsCount() - 1);
    }
}

public class EntityTableRow<T> : LineContainer<T>
    where T : ModifiableEntity
{
    public EntityTableProxy EntityTable { get; private set; }

    public int Index => this.EntityTable.IndexOf(this.Element);

    public EntityTableRow(EntityTableProxy entityTable, IWebElement element, PropertyRoute? route = null) : base(element, route)
    {
        this.EntityTable = entityTable;
    }

    public EntityTableRow<T> WaitRefresh(Action action)
    {
        var selenium = this.EntityTable.Element.GetDriver();
        var index = this.Index;
        action();
        EntityTableRow<T> result = null!;
        selenium.Wait(() =>
        {
            result = this.EntityTable.Row<T>(index);
            return !result.Element.Equals(this.Element);
        });

        return result;
    }
}
