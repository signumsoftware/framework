using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.React.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Signum.React.Selenium
{

    public class BaseLineProxy
    {
        public IWebElement Element { get; private set; }

        public PropertyRoute Route { get; private set; }

        public BaseLineProxy(IWebElement element, PropertyRoute route)
        {
            this.Element = element;
            this.Route = route;
        }

        protected static string ToVisible(bool visible)
        {
            return visible ? "visible" : "not visible";
        }
    }

    public class ValueLineProxy : BaseLineProxy
    {
        public ValueLineProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }
        

        public void SetStringValue(string value)
        {
            IWebElement checkBox = this.Element.TryFindElement(By.CssSelector("input[type=checkbox]"));
            if (checkBox != null)
            {
                checkBox.SetChecked(bool.Parse(value));
                return;
            }

            IWebElement dateTimePicker = this.Element.TryFindElement(By.CssSelector("div.rw-datetimepicker input[type=text]"));
            if(dateTimePicker != null)
            {
                var js = this.Element.GetDriver() as IJavaScriptExecutor;

                var script = 
$@"arguments[0].value = '{value}'; 
arguments[0].dispatchEvent(new Event('input', {{ bubbles: true }}));
arguments[0].dispatchEvent(new Event('blur'));";

                js.ExecuteScript(script, dateTimePicker);
                return;
            }

            IWebElement textOrTextArea = this.Element.TryFindElement(By.CssSelector(" input[type=text], textarea"));
            if (textOrTextArea != null)
            {
                textOrTextArea.SafeSendKeys(value);
                return;
            }

            IWebElement select = this.Element.TryFindElement(By.CssSelector("select"));
            if (select != null)
            {
                select.SelectElement().SelectByValue(value);
                return;
            }

            throw new InvalidOperationException("No ValueLine input element for  {0} found".FormatWith(Route));
        }

        private string GetStringValue()
        {
            IWebElement checkBox = this.Element.TryFindElement(By.CssSelector("input[type=checkbox]"));
            if (checkBox != null)
                return checkBox.Selected.ToString();

            IWebElement textOrTextArea = this.Element.TryFindElement(By.CssSelector("input[type=text], textarea"));
            if (textOrTextArea != null)
                return textOrTextArea.GetAttribute("value");
            
            IWebElement readonlyField = this.Element.TryFindElement(By.CssSelector("p.form-control, p.form-control-static"));
            if (readonlyField != null)
                return readonlyField.Text;

            throw new InvalidOperationException("Element {0} not found".FormatWith(Route.PropertyString()));
        }

        public bool IsReadonly()
        {
            return this.Element.IsElementPresent(By.CssSelector("p.form-control"));
        }

        public WebElementLocator EditableElement
        {
            get { return this.Element.WithLocator(By.CssSelector("input, textarea")); }
        }

        public object GetValue()
        {
            return this.GetValue(Route.Type);
        }
      
        public object GetValue(Type type)
        {
            return ReflectionTools.Parse(GetStringValue(), type);
        }

        public T GetValue<T>()
        {
            return ReflectionTools.Parse<T>(GetStringValue()); 
        }

        public void SetValue(object value)
        {
            var format = Reflector.FormatString(Route);
            this.SetValue(value, format);
        }

        public void SetValue(object value, string format)
        {
            var str = value == null ? null :
                    value is IFormattable ? ((IFormattable)value).ToString(format, null) :
                    value.ToString();

            SetStringValue(str);
        }
    }

    public abstract class EntityBaseProxy : BaseLineProxy
    {
        public EntityBaseProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public virtual PropertyRoute ItemRoute => this.Route;

        public WebElementLocator CreateButton
        {
            get { return this.Element.WithLocator(By.CssSelector("a.sf-create")); }
        }

        

        protected void CreateEmbedded<T>()
        {
            WaitChanges(() =>
            {
                var imp = this.ItemRoute.TryGetImplementations();
                if (imp != null && imp.Value.Types.Count() != 1)
                {
                    var popup = this.CreateButton.Find().CaptureOnClick();
                    ChooseType(typeof(T), popup);
                }
                else
                {
                    this.CreateButton.Find().Click();
                }
            }, "create clicked");
        }

        public PopupFrame<T> CreatePopup<T>() where T : ModifiableEntity
        {
         
            string changes = GetChanges();

            var popup = this.CreateButton.Find().CaptureOnClick();

            popup = ChooseTypeCapture(typeof(T), popup);

            return new PopupFrame<T>(popup, this.ItemRoute)
            {
                Disposing = okPressed => { WaitNewChanges(changes, "create dialog closed"); }
            };
        }

        public WebElementLocator ViewButton
        {
            get { return this.Element.WithLocator(By.CssSelector("a.sf-view")); }
        }
        
        protected PopupFrame<T> ViewInternal<T>() where T : ModifiableEntity
        {
            var newElement = this.ViewButton.Find().CaptureOnClick();
            string changes = GetChanges();
            
            return new PopupFrame<T>(newElement, this.ItemRoute)
            {
                Disposing = okPressed => WaitNewChanges(changes, "create dialog closed")
            };
        }

        public WebElementLocator FindButton
        {
            get { return this.Element.WithLocator(By.CssSelector("a.sf-find")); }
        }

        public WebElementLocator RemoveButton
        {
            get { return this.Element.WithLocator(By.CssSelector("a.sf-remove")); }
        }

        public void Remove()
        {
            WaitChanges(() => this.RemoveButton.Find().Click(), "removing");
        }
      
        public SearchPopupProxy Find(Type selectType = null)
        {
            string changes = GetChanges();
            var popup = FindButton.Find().CaptureOnClick();

            popup = ChooseTypeCapture(selectType, popup);

            return new SearchPopupProxy(popup)
            {
                Disposing = okPressed => { WaitNewChanges(changes, "create dialog closed"); }
            };
        }

        private void ChooseType(Type selectType, IWebElement element)
        {
            if (!SelectorModal.IsSelector(element))
                return;

            if (selectType == null)
                throw new InvalidOperationException("No type to choose from selected");

            SelectorModal.Select(this.Element, TypeLogic.GetCleanName(selectType));
        }

        private IWebElement ChooseTypeCapture(Type selectType, IWebElement element)
        {
            if (!SelectorModal.IsSelector(element))
                return element;

            if (selectType == null)
                throw new InvalidOperationException("No type to choose from selected");

            var newElement = element.GetDriver().CapturePopup(() =>
                SelectorModal.Select(this.Element, TypeLogic.GetCleanName(selectType)));

            return newElement;
        }

        public void WaitChanges(Action action, string actionDescription)
        {
            var changes = GetChanges();

            action();

            WaitNewChanges(changes, actionDescription);
        }

        public void WaitNewChanges(string changes, string actionDescription)
        {
            Element.GetDriver().Wait(() => GetChanges() != changes, () => "Waiting for changes after {0} in {1}".FormatWith(actionDescription, this.Route.ToString()));
        }

        public string GetChanges()
        {
            return this.Element.GetAttribute("data-changes");
        }

        protected EntityInfoProxy EntityInfoInternal(int? index)
        {
            var element = index == null ? Element :
                this.Element.FindElements(By.CssSelector("[data-entity]")).ElementAt(index.Value);

            return EntityInfoProxy.Parse(element.GetAttribute("data-entity"));
        }

        public void AutoCompleteWaitChanges(IWebElement autoCompleteElement, Lite<IEntity> lite)
        {
            WaitChanges(() =>
            {
                AutoCompleteBasic(autoCompleteElement, lite);

            }, "autocomplete selection");
        }
        public static void AutoCompleteBasic(IWebElement autoCompleteElement, Lite<IEntity> lite)
        {
            autoCompleteElement.FindElement(By.CssSelector("input")).SafeSendKeys(lite.Id.ToString());
            //Selenium.FireEvent(autoCompleteLocator, "keyup");

            var listLocator = By.CssSelector("ul.typeahead.dropdown-menu");

            autoCompleteElement.WaitElementVisible(listLocator);
            IWebElement itemElement = autoCompleteElement.FindElement(By.CssSelector("[data-entity-key='{0}']".FormatWith(lite.Key())));

            itemElement.Click();
        }
    }

    public class EntityInfoProxy
    {
        public bool IsNew { get; set; }
        public string TypeName { get; set; }

        public Type EntityType;
        public PrimaryKey? IdOrNull { get; set; }


        public Lite<Entity> ToLite(string toString = null)
        {
            return Lite.Create(this.EntityType, this.IdOrNull.Value, toString);
        }

        public static EntityInfoProxy Parse(string dataEntity)
        {
            if (dataEntity == "null" || dataEntity == "undefined")
                return null;

            var parts = dataEntity.Split(';');

            var typeName = parts[0];
            var id = parts[1];
            var isNew = parts[2];

            var type = TypeLogic.TryGetType(typeName);

            return new EntityInfoProxy
            {
                TypeName = typeName,
                EntityType = type,
                IdOrNull = id.HasText() ? PrimaryKey.Parse(id, type) : (PrimaryKey?)null,
                IsNew = isNew.HasText() && bool.Parse(isNew)
            };
        }
    }

    public class EntityLineProxy : EntityBaseProxy
    {
        public EntityLineProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }
      
        public void SetLite(Lite<IEntity> value)
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

        public Lite<Entity> GetLite()
        {
            return EntityInfo()?.ToLite();
        }

        public WebElementLocator AutoCompleteElement
        {
            get { return this.Element.WithLocator(By.CssSelector(".sf-typeahead")); }
        }

        public void AutoComplete(Lite<IEntity> lite)
        {
            base.AutoCompleteWaitChanges(AutoCompleteElement.Find(), lite);
        }

        public void AutoCompleteBasic(Lite<IEntity> lite)
        {
            AutoCompleteBasic(AutoCompleteElement.Find(), lite);
        }

        public PopupFrame<T> View<T>() where T : ModifiableEntity
        {
            return base.ViewInternal<T>();
        }

        public EntityInfoProxy EntityInfo()
        {
            return EntityInfoInternal(null);
        }

      
    }

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

        public Lite<IEntity> LiteValue
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

        public List<Lite<Entity>> Options()
        {
            return this.ComboElement.Options
                .Select(o => Lite.Parse(o.GetAttribute("value"))?.Do(l => l.SetToString(o.Text)))
                .ToList();
        }

        public PopupFrame<T> View<T>() where T : ModifiableEntity
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

        public EntityInfoProxy EntityInfo()
        {
            return EntityInfoInternal(null);
        }
        public void AssertOptions(Lite<Entity>[] list, bool removeNullElement = true, bool orderIndependent = false)
        {
            this.Element.GetDriver().Wait(() =>
            {
                var options = this.Options();
                if (removeNullElement)
                    options = options.NotNull().ToList();

                if (orderIndependent)
                    return options.OrderBy(a => a.Id).SequenceEqual(list.OrderBy(a => a.Id));
                else
                    return options.SequenceEqual(list);
            });
        }
    }

    public class EntityDetailProxy : EntityBaseProxy
    {
        public EntityDetailProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public Lite<IEntity> Lite
        {
            get { return EntityInfo()?.ToLite(); }
            set
            {
                if (this.EntityInfo() != null)
                    this.Remove();

                if (this.FindButton.IsVisible())
                    this.Find().SelectLite(value);

                throw new NotImplementedException("AutoComplete");
            }
        }

        public LineContainer<T> Details<T>() where T : ModifiableEntity
        {
            return new LineContainer<T>(this.Element.FindElement(By.CssSelector("div[data-propertypath]")), Route);
        }

        public EntityInfoProxy EntityInfo()
        {
            return EntityInfoInternal(null);
        }

        public ILineContainer<T> GetOrCreateDetailControl<T>() where T : ModifiableEntity
        {
            if (this.EntityInfo() !=null)
                return this.Details<T>();

            CreateEmbedded<T>();

            return this.Details<T>();
        }
    }

    public class EntityListProxy : EntityBaseProxy
    {
        public override PropertyRoute ItemRoute => base.ItemRoute.Add("Item");

        public EntityListProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public WebElementLocator OptionElement(int index)
        {
            return this.ListElement.Find().WithLocator(By.CssSelector("option:nth-child({0})".FormatWith(index)));
        }

        public WebElementLocator ListElement
        {
            get { return this.Element.WithLocator(By.CssSelector("select.form-control")); }
        }

        public void Select(int index)
        {
            this.OptionElement(index).Find().Click();
        }

        public PopupFrame<T> View<T>(int index) where T : ModifiableEntity
        {
            Select(index);

            return base.ViewInternal<T>();
        }
        
        public int ItemsCount()
        {
            return this.ListElement.Find().FindElements(By.CssSelector("option")).Count;
        }

        public EntityInfoProxy EntityInfo(int index)
        {
            return EntityInfoInternal(index);
        }
        
        public void DoubleClick(int index)
        {
            Select(index);
            OptionElement(index).Find().DoubleClick();
        }
    }
    

    public class EntityRepeaterProxy : EntityBaseProxy
    {
        public override PropertyRoute ItemRoute => base.ItemRoute.Add("Item");

        public EntityRepeaterProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public virtual WebElementLocator ItemsContainerElement
        {
            get { return this.Element.WithLocator(By.CssSelector(".sf-repater-elements")); }
        }

        public virtual WebElementLocator ItemElement(int index)
        {
            return this.ItemsContainerElement.CombineCss(" > fieldset.sf-repeater-element:nth-child({0})".FormatWith(index));
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
            return this.ItemsContainerElement.CombineCss(" > fieldset.sf-repeater-element§").FindElements().Count;
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

        public EntityInfoProxy EntityInfo(int index)
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
            return this.Details<T>(this.ItemsCount() + 1);
        }
    }

    public class EntityTabRepeaterProxy : EntityRepeaterProxy
    {
        public EntityTabRepeaterProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }
    }

    public class EntityStripProxy : EntityBaseProxy
    {
        public EntityStripProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public WebElementLocator ItemsContainerElement
        {
            get { return this.Element.WithLocator(By.CssSelector("ul.sf-strip")); }
        }

        public WebElementLocator StripItemSelector(int index)
        {
            return this.ItemsContainerElement.CombineCss(" > li.sf-strip-element:nth-child({0})".FormatWith(index));
        }

        public int ItemsCount()
        {
            return this.ItemsContainerElement.CombineCss(" > li.sf-strip-element").FindElements().Count;
        }

        public WebElementLocator ViewElementIndex(int index)
        {
            return StripItemSelector(index).CombineCss(" > a.sf-entitStrip-link");
        }

        public WebElementLocator RemoveElementIndex(int index)
        {
            return StripItemSelector(index).CombineCss(" > a.sf-remove");
        }

        public void Remove(int index)
        {
            RemoveElementIndex(index).Find().Click();
        }

        public EntityInfoProxy EntityInfo(int index)
        {
            return EntityInfoInternal(index);
        }

        public WebElementLocator AutoCompleteElement
        {
            get { return this.Element.WithLocator(By.CssSelector(".sf-typeahead")); }
        }

        public void AutoComplete(Lite<IEntity> lite)
        {
            base.AutoCompleteWaitChanges(AutoCompleteElement.Find(), lite);
        }

        public PopupFrame<T> View<T>(int index) where T : ModifiableEntity
        {
            var changes = this.GetChanges();
            var popup = ViewElementIndex(index).Find().CaptureOnClick();

            return new PopupFrame<T>(popup, this.ItemRoute)
            {
                Disposing = okPressed => WaitNewChanges(changes, "create dialog closed")
            };
        }
    }

    public class EntityListCheckBoxProxy : EntityBaseProxy
    {
        public EntityListCheckBoxProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public WebElementLocator CheckBoxElement(Lite<Entity> lite)
        {
            return this.Element.WithLocator(By.CssSelector("input[name='{0}']".FormatWith(lite.Key())));
        }

        public List<Lite<Entity>> GetDataElements()
        {
            return this.Element.WithLocator(By.CssSelector("label.sf-checkbox-element")).FindElements().Select(e =>
            {
                var lite = Lite.Parse(e.FindElement(By.CssSelector("input[type=checkbox]")).GetAttribute("name"));
                lite.SetToString(e.FindElement(By.CssSelector("span.sf-entitStrip-link")).Text);
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


    public class FileLineProxy : BaseLineProxy
    {
        public FileLineProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
            
        }

        public void SetPath(string path)
        {
            FileElement.Find().SendKeys(path);
            FileElement.WaitNoPresent();
        }

        private WebElementLocator FileElement
        {
            get { return this.Element.WithLocator(By.CssSelector("input[type=file]")); }
        }
    }

    public static class FileExtensions
    {
        public static LineContainer<T> SetPath<T>(this EntityRepeaterProxy repeater, string path) where T: ModifiableEntity
        {
            var count = repeater.ItemsCount();

            var input = repeater.Element.FindElement(By.CssSelector("input[type=file]"));
            input.SendKeys(path);
            
            return repeater.Details<T>(count + 1);
        }
    }
}
