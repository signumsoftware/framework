using OpenQA.Selenium;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.React.Selenium
{
    public class EntityTableProxy : EntityBaseProxy
    {
        public override PropertyRoute ItemRoute => base.ItemRoute.Add("Item");

        public EntityTableProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public virtual WebElementLocator TableElement
        {
            get { return this.Element.WithLocator(By.CssSelector(".sf-table")); }
        }

        public virtual WebElementLocator RowElement(int index)
        {
            return this.TableElement.CombineCss(" > tbody > tr:nth-child({0})".FormatWith(index));
        }

        public void WaitItemLoaded(int index)
        {
            RowElement(index).WaitPresent();
        }

        public virtual int RowsCount()
        {
            return this.TableElement.CombineCss(" > tbody > tr").FindElements().Count;
        }

        public LineContainer<T> Row<T>(int index) where T : ModifiableEntity
        {
            return new LineContainer<T>(RowElement(index).WaitPresent(), this.ItemRoute);
        }

        public IWebElement RemoveRowIndex(int index)
        {
            return RowElement(index).CombineCss(" a.remove").Find();
        }

        public void Remove(int index)
        {
            this.RemoveRowIndex(index).Click();
        }

        public EntityInfoProxy EntityInfo(int index)
        {
            return EntityInfoInternal(index);
        }

        public LineContainer<T> CreateRow<T>() where T : ModifiableEntity
        {
            var count = this.RowsCount();

            CreateEmbedded<T>();

            return this.Row<T>(count + 1);
        }

        public LineContainer<T> LastRow<T>() where T : ModifiableEntity
        {
            return this.Row<T>(this.RowsCount() + 1);
        }
    }
}
