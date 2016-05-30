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
        public RemoteWebDriver Selenium { get; private set; }

        public IWebElement Element { get; private set; }

        public PropertyRoute Route { get; private set; }

        public BaseLineProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
        {
            this.Selenium = selenium;
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
        public ValueLineProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, element, route)
        {
        }

        public string StringValue
        {
            get
            {
                IWebElement checkBox = this.Element.FindElement(By.CssSelector("input[type=checkbox]"));
                if (checkBox != null)
                    return checkBox.Selected.ToString();

                IWebElement namedElement = Selenium.TryFindElement(By.CssSelector("input[type=text]"));
                if(namedElement != null)
                    return namedElement.GetAttribute("value");

                IWebElement date = Selenium.TryFindElement(By.Name("Date"));
                IWebElement time = Selenium.TryFindElement(By.Name("Time"));

                if (date != null && time != null)
                    return date.GetAttribute("value") + " " + time.GetAttribute("value");

                if (checkBox != null)
                    return checkBox.Text;
            
                throw new InvalidOperationException("Element {0} not found".FormatWith(this.Route.PropertyString()));
            }

            set
            {

                IWebElement checkBox = this.Element.TryFindElement(By.CssSelector("input[type=checkbox]"));
                if (checkBox != null)
                {
                    checkBox.SetChecked(bool.Parse(value));
                }

                //IWebElement element = Selenium.TryFindElement(By.Id(Prefix));
                //if (element != null)
                //{
                //    if (element != null && element.TagName == "input" && element.GetAttribute("type") == "checkbox")
                //    {
                //        element.SetChecked(bool.Parse(value));
                //        return;
                //    }
                //    else if (element.GetParent().HasClass("input-group", "date"))
                //    {
                //        Selenium.ExecuteScript("$('div.input-group.date>#{0}').parent().datepicker('setDate', '{1}')".FormatWith(Prefix, value)); 
                //        return;
                //    }
                //    else if (element.FindElements(By.CssSelector("div.date")).Any() && element.FindElements(By.CssSelector("div.time")).Any())
                //    {
                //        Selenium.ExecuteScript("$('#{0} > div.date').datepicker('setDate', '{1}')".FormatWith(Prefix, value.TryBefore(" ")));
                //        Selenium.ExecuteScript("$('#{0} > div.time').timepicker('setTime', '{1}')".FormatWith(Prefix, value.TryAfter(" ")));
                //        return;
                //    }
                //}

                IWebElement byName = this.Element.TryFindElement(By.CssSelector("input[type=text]"));
                if (byName != null)
                {
                    if (byName.TagName == "select")
                        byName.SelectElement().SelectByValue(value);
                    else
                        byName.SafeSendKeys(value);
                    return;
                }
                else
                    throw new InvalidOperationException("Element {0} not found".FormatWith(this.Route));
            }
        }

        internal IWebElement MainElement()
        {
            throw new NotImplementedException();
        }

        public object Value
        {
            get { return GetValue(Route.Type); }
            set { SetValue(value, Reflector.FormatString(Route)); }
        }

        public object GetValue(Type type)
        {
            return ReflectionTools.Parse(StringValue, type);
        }

        public T GetValue<T>()
        {
            return ReflectionTools.Parse<T>(StringValue); 
        }

        public void SetValue(object value, string format = null)
        {
            StringValue = value == null ? null :
                    value is IFormattable ? ((IFormattable)value).ToString(format, null) :
                    value.ToString();
        }
    }

    public abstract class EntityBaseProxy : BaseLineProxy
    {
        public EntityBaseProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, element, route)
        {
        }

        public IWebElement CreateButton
        {
            get { return this.Selenium.NotImplemented(); /* TryFindElement(By.CssSelector("#{0}_btnCreate".FormatWith()); */ }
        }

        protected void CreateEmbedded<T>(bool mlist)
        {
            WaitChanges(() =>
            {
                this.CreateButton.Click();

                var route = this.Route;
                if (mlist)
                    route = route.Add("Item");

                var imp = route.TryGetImplementations();
                if (imp != null && imp.Value.Types.Count() != 1)
                    ChooseType(typeof(T), NewIndex());
            }, "create clicked");
        }

        public virtual int? NewIndex()
        {
            return null;
        }

        public PopupControl<T> CreatePopup<T>() where T : ModifiableEntity
        {
            var index = NewIndex();
            string changes = GetChanges();

            this.CreateButton.Click();

            IWebElement element = ChooseType(typeof(T), index);

            PropertyRoute route = index == null ? this.Route : this.Route.Add("Item");

            return new PopupControl<T>(this.Selenium, element, route)
            {
                Disposing = okPressed => { WaitNewChanges(changes, "create dialog closed"); }
            };
        }

        public IWebElement ViewButton
        {
            get { return this.Selenium.NotImplemented();/* ( By.CssSelector("#{0}_btnView".FormatWith(this.Route.PropertyString()));*/ }
        }

        protected PopupControl<T> ViewPopup<T>(int? index) where T : ModifiableEntity
        {
            var newElement = this.Selenium.NotImplemented();
            //string newPrefix = Prefix + (index == null ? "" : ("_" + index));
            string changes = GetChanges();

            PropertyRoute route = index == null ? this.Route : this.Route.Add("Item");

            return new PopupControl<T>(this.Selenium, newElement, route)
            {
                Disposing = okPressed => WaitNewChanges(changes, "create dialog closed")
            };
        }

        public IWebElement FindButton
        {
            get { return this.Selenium.NotImplemented();/* get { return By.CssSelector("#{0}_btnFind".FormatWith(Prefix)); */ }
        }

        public IWebElement RemoveButton
        {
            get { return this.Selenium.NotImplemented();/* get { return By.CssSelector("#{0}_btnRemove".FormatWith(Prefix));  */}
        }

        public void Remove()
        {
            WaitChanges(() => this.RemoveButton.Click(), "removing");
        }
      
        public SearchPopupProxy Find(Type selectType = null)
        {
            string changes = GetChanges();
            Selenium.FindElement(FindLocator).Click();

            ChooseType(selectType, null);

            return new SearchPopupProxy(Selenium, Prefix)
            {
                Disposing = okPressed => { WaitNewChanges(changes, "create dialog closed"); }
            };
        }

        private IWebElement ChooseType(Type selectType, int? index)
        {
            IWebElement newElement = this.Selenium.NotImplemented(); // Prefix + (index == null ? "" : ("_" + index));

            Selenium.Wait(() => Popup.IsPopupVisible(Selenium, newElement) || Popup.IsPopupVisible(Selenium, this.Element), 
                () => "Popup {0} or {1} to be visible".FormatWith(newElement, this.Element));

            if (selectType == null)
            {
                if (ChooserPopup.IsChooser(Selenium, this.Element))
                    throw new InvalidOperationException("TypeChooser found but arugment selectType is not specified");
            }
            else
            {
                if (ChooserPopup.IsChooser(Selenium, this.Element))
                {
                    ChooserPopup.ChooseButton(Selenium, this.Element, TypeLogic.GetCleanName(selectType));

                    Selenium.Wait(() => !ChooserPopup.IsChooser(Selenium, this.Element));
                }
            }

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
            Selenium.Wait(() => GetChanges() != changes, () => "Waiting for changes after {0} in {1}".FormatWith(actionDescription, this.Route.ToString()));
        }

        public string GetChanges()
        {
            throw new NotImplementedException();
            //return (string)Selenium.ExecuteScript("return $('#{0}').attr('changes')".FormatWith(Prefix));
        }

        protected EntityInfoProxy EntityInfoInternal(int? index = null)
        {
            throw new NotImplementedException();
            //return EntityInfoProxy.FromFormValue(Selenium.FindElement(EntityInfoLocatorInternal(index)).GetAttribute("value"));
        }

        internal void AutoCompleteAndSelect(IWebElement autoCompleteElement, Lite<IEntity> lite)
        {
            WaitChanges(() =>
            {
                autoCompleteElement.SafeSendKeys(lite.Id.ToString());
                //Selenium.FireEvent(autoCompleteLocator, "keyup");

                var listLocator = By.CssSelector("ul.typeahead.dropdown-menu");

                Selenium.WaitElementVisible(listLocator);
                IWebElement itemElement = this.Selenium.NotImplemented();//  listLocator.CombineCss(" span[data-type='{0}'][data-id='{1}']".FormatWith(TypeLogic.GetCleanName(lite.EntityType), lite.Id));

                itemElement.Click();

            }, "autocomplete selection");
        }
    }

    public class EntityInfoProxy
    {
        public bool IsNew { get; set; }
        public string TypeName { get; set; }

        public Type EntityType => TypeLogic.GetType(this.TypeName);
        public int? IdOrNull { get; set; }

        public Lite<Entity> ToLite(string toString = null) => Lite.Create(this.EntityType, this.IdOrNull.Value, null);
    }

    public class EntityLineProxy : EntityBaseProxy
    {
        public EntityLineProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, element, route)
        {
        }

        public IWebElement ToStrElement
        {
            get { return this.Selenium.NotImplemented(); /* By.CssSelector("#{0}_sfToStr".FormatWith(Prefix)); */ }
        }

        public IWebElement LinkElement
        {
            get { return this.Selenium.NotImplemented();  /*By.CssSelector("#{0}_sfLink".FormatWith(Prefix));*/ }
        }

        public bool HasEntity()
        {
            return this.LinkElement != null; // Selenium.IsElementVisible(LinkLocator);
        }

        public Lite<IEntity> LiteValue
        {
            get { return EntityInfo().ToLite(); }
            set
            {
                if (HasEntity())
                    this.Remove();

                if (value != null)
                {
                    if (AutoCompleteElement != null)
                        AutoComplete(value);
                    else if (FindButton != null)
                        this.Find().SelectLite(value);
                    else
                        throw new NotImplementedException("AutoComplete");
                }
            }
        }

        public IWebElement AutoCompleteElement
        {
            get { return this.Selenium.NotImplemented(); /* By.CssSelector("#{0}_sfToStr".FormatWith(Prefix));*/ }
        }

        public void AutoComplete(Lite<IEntity> lite)
        {
            base.AutoCompleteAndSelect(AutoCompleteElement, lite);
        }

        public PopupControl<T> View<T>() where T : ModifiableEntity
        {
            this.ViewButton.Click();

            return base.ViewPopup<T>(null);
        }

        public EntityInfoProxy EntityInfo()
        {
            return EntityInfoInternal(null);
        }
    }

    public class EntityComboProxy : EntityBaseProxy
    {
        public EntityComboProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, element, route)
        {
        }

        public SelectElement ComboElement
        {
            get { return this.Selenium.NotImplemented().SelectElement(); /* By.CssSelector("#{0}_sfCombo".FormatWith(Prefix)); */ }
        }

        public Lite<IEntity> LiteValue
        {
            get
            {
                var text =  this.ComboElement.AllSelectedOptions.SingleOrDefaultEx()?.Text;

                return EntityInfo().ToLite(text);
            }
            set
            {
                this.ComboElement.SelectByValue(value == null ? "" : value.Key());
            }
        }

        public List<Lite<Entity>> Options()
        {
            return this.ComboElement.Options
                .Select(o => Lite.Parse(o.GetAttribute("value"))?.Do(l => l.SetToString(o.Text)))
                .ToList();
        }

        public PopupControl<T> View<T>() where T : ModifiableEntity
        {
            this.ViewButton.Click();

            return base.ViewPopup<T>(null);
        }

        public void SelectLabel(string label)
        {
            WaitChanges(() =>
                this.ComboElement.SelectByText(label),
                "ComboBox selected");
        }

        public void SelectIndex(int index)
        {
            WaitChanges(() =>
                this.ComboElement.SelectByIndex(index + 1),
                "ComboBox selected");
        }

        public EntityInfoProxy EntityInfo()
        {
            return EntityInfoInternal(null);
        }
    }

    public class EntityDetailProxy : EntityBaseProxy
    {
        public EntityDetailProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, element, route)
        {
        }

        public IWebElement DivSelector
        {
            get { return this.Selenium.NotImplemented(); /* By.CssSelector("#{0}_sfDetail".FormatWith(Prefix)); */ }
        }

        public bool HasEntity()
        {
            throw new NotImplementedException();
            //return Selenium.IsElementPresent(DivSelector.CombineCss(" *:first-child"));
        }

        public Lite<IEntity> Lite
        {
            get { return EntityInfo().ToLite(); }
            set
            {
                if (HasEntity())
                    this.Remove();

                if (this.FindButton?.Displayed == true)
                    this.Find().SelectLite(value);

                throw new NotImplementedException("AutoComplete");
            }
        }

        public LineContainer<T> Details<T>() where T : ModifiableEntity
        {
            return new LineContainer<T>(Selenium, this.Selenium.NotImplemented(), Route);
        }


        public EntityInfoProxy EntityInfo()
        {
            return EntityInfoInternal(null);
        }

        public ILineContainer<T> GetOrCreateDetailControl<T>() where T : ModifiableEntity
        {
            if (this.HasEntity())
                return this.Details<T>();

            CreateEmbedded<T>(mlist: false);

            return this.Details<T>();
        }
    }

    public class EntityListProxy : EntityBaseProxy
    {
        public EntityListProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, element, route)
        {
        }

        public IWebElement OptionIdElement(int index)
        {
            return this.Selenium.NotImplemented(); // By.CssSelector("#{0}_{1}_sfToStr".FormatWith(Prefix, index));
        }

        public IWebElement ListElement
        {
            get { return this.Selenium.NotImplemented(); /*return By.CssSelector("#{0}_sfList".FormatWith(Prefix));*/ }
        }

        public void Select(int index)
        {
            throw new NotImplementedException();
            //var selectElement = Selenium.FindElement(ListLocator).SelectElement();
            //if (selectElement.IsMultiple)
            //    selectElement.DeselectAll();

            //var id = "{0}_{1}_sfToStr".FormatWith(Prefix, index);
            //selectElement.SelectByPredicate(a => a.GetAttribute("id") == id);
        }

        public PopupControl<T> View<T>(int index) where T : ModifiableEntity
        {
            Select(index);

            this.ViewButton.Click();

            return base.ViewPopup<T>(index);
        }

        public bool HasEntity(int index)
        {
            return OptionIdElement(index)?.Displayed == true;
        }


        public int ItemsCount()
        {
            throw new InvalidOperationException();
            //return (int)(long)Selenium.ExecuteScript("return $('#{0}_sfList option').length".FormatWith(Prefix));
        }

        public override int? NewIndex()
        {

            throw new InvalidOperationException();

            //string result = (string)Selenium.ExecuteScript("return $('#{0}_sfList option').get().map(function(a){{return parseInt(a.id.substr('{0}'.length + 1));}}).join()".FormatWith(Prefix));

            //return string.IsNullOrEmpty(result) ? 0 : result.Split(',').Select(int.Parse).Max() + 1;
        }
        

        public EntityInfoProxy EntityInfo(int index)
        {
            return EntityInfoInternal(index);
        }


        public void DoubleClick(int index)
        {
            Select(index);
            OptionIdElement(index).DoubleClick();
        }
    }
    

    public class EntityRepeaterProxy : EntityBaseProxy
    {
        public EntityRepeaterProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, element, route)
        {
        }

        public IWebElement ItemsContainerElement
        {
            get { return this.Selenium.NotImplemented(); /* By.CssSelector("#{0}_sfItemsContainer".FormatWith(Prefix)); */}
        }

        public virtual By RepeaterItemSelector(int index)
        {
            throw new NotImplementedException();
            //return ItemsContainerLocator.CombineCss(" > #{0}_{1}_sfRepeaterItem".FormatWith(Prefix, index));
        }

        public void WaitItemLoaded(int index)
        {
            Selenium.WaitElementPresent(RepeaterItemSelector(index));
        }

        public virtual void MoveUp(int index)
        {
            this.Element.FindElement(By.CssSelector("{1}_btnUp".FormatWith(index))).Click();
        }

        public virtual void MoveDown(int index)
        {
            Selenium.FindElement(By.CssSelector("{1}_btnDown".FormatWith(index))).Click();
        }

        public bool HasEntity(int index)
        {
            return Selenium.IsElementPresent(RepeaterItemSelector(index)); ;
        }

        public virtual int ItemsCount()
        {
            throw new InvalidOperationException();
            //return (int)(long)Selenium.ExecuteScript("return $('{0} fieldset:not(.hidden)').length".FormatWith(ItemsContainerLocator.CssSelector()));
        }

        public virtual int HiddenItemsCount()
        {
            throw new InvalidOperationException();
            //return (int)(long)Selenium.ExecuteScript("return $('{0} fieldset.hidden').length".FormatWith(ItemsContainerLocator.CssSelector()));
        }

        public override int? NewIndex()
        {
            throw new InvalidOperationException();

            //string result = (string)Selenium.ExecuteScript("return $('#{0}_sfItemsContainer fieldset').get().map(function(a){{return parseInt(a.id.substr('{0}'.length + 1));}}).join()".FormatWith(Prefix));

            //return string.IsNullOrEmpty(result) ? 0 : result.Split(',').Select(int.Parse).Max() + 1;
        }

        public LineContainer<T> Details<T>(int index) where T : ModifiableEntity
        {
            throw new NotImplementedException();
            //return new LineContainer<T>(Selenium, Prefix + "_" + index, Route.Add("Item"));
        }

        public IWebElement RemoveElementIndex(int index)
        {
            throw new NotImplementedException();
            //return By.CssSelector("#{0}_{1}_btnRemove".FormatWith(Prefix, index));
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
            var index = NewIndex();

            CreateEmbedded<T>(mlist: true);

            return this.Details<T>(index.Value);
        }

        public LineContainer<T> LastDetails<T>() where T : ModifiableEntity
        {
            return this.Details<T>(this.ItemsCount() + this.HiddenItemsCount() - 1);
        }
    }

    public class EntityTabRepeaterProxy : EntityRepeaterProxy
    {
        public EntityTabRepeaterProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, element, route)
        {
        }

     
        public override int ItemsCount()
        {
            throw new InvalidOperationException();
            //return (int)(long)Selenium.ExecuteScript("return $('#{0}_sfItemsContainer li').length".FormatWith(ItemsContainerLocator));
        }

        public override int? NewIndex()
        {
            throw new InvalidOperationException();
            //string result = (string)Selenium.ExecuteScript("return $('#{0}_sfItemsContainer li').get().map(function(a){{return parseInt(a.id.substr('{0}'.length + 1));}}).join()".FormatWith(Prefix));

            //return string.IsNullOrEmpty(result) ? 0 : result.Split(',').Select(int.Parse).Max() + 1;
        }
    }

    public class EntityStripProxy : EntityBaseProxy
    {
        public EntityStripProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, element, route)
        {
        }

        public IWebElement ItemsContainerElement
        {
            get { return Selenium.NotImplemented(); /* By.CssSelector("#{0}_sfItemsContainer".FormatWith(Prefix)); */ }
        }

        public By StripItemSelector(int index)
        {
            throw new NotImplementedException();
            //return ItemsContainerLocator.CombineCss(" > #{0}_{1}_sfStripItem".FormatWith(Prefix, index));
        }

        public void WaitItemLoaded(int index)
        {
            Selenium.WaitElementPresent(StripItemSelector(index));
        }

        public bool HasEntity(int index)
        {
            return Selenium.IsElementPresent(StripItemSelector(index));
        }

        public int ItemsCount()
        {
            throw new NotImplementedException();
            //return (int)(long)Selenium.ExecuteScript("return $('#{0}_sfItemsContainer li.sf-strip-element').length".FormatWith(ItemsContainerLocator));
        }

        public override int? NewIndex()
        {
            throw new NotImplementedException(); 
            //var result = (string)Selenium.ExecuteScript("return $('#{0}_sfItemsContainer li.sf-strip-element').get().map(function(a){{return parseInt(a.id.substr('{0}'.length + 1));}}).join()".FormatWith(Prefix));

            //return string.IsNullOrEmpty(result) ? 0 : result.Split(',').Select(int.Parse).Max() + 1;
        }


        public IWebElement ViewElementIndex(int index)
        {
            throw new InvalidOperationException();
            //return By.CssSelector("#{0}_{1}_btnView".FormatWith(Prefix, index));
        }

        public IWebElement RemoveElementIndex(int index)
        {
            throw new InvalidOperationException();
            //return By.CssSelector("#{0}_{1}_btnRemove".FormatWith(Prefix, index));
        }

        public void Remove(int index)
        {
            RemoveElementIndex(index).Click();
        }

        public EntityInfoProxy EntityInfo(int index)
        {
            return EntityInfoInternal(index);
        }

        public IWebElement AutoCompleteElement
        {
            get { return this.Selenium.NotImplemented(); /* By.CssSelector("#{0}_sfToStr".FormatWith(Prefix)); */ }
        }

        public void AutoComplete(Lite<IEntity> lite)
        {
            base.AutoCompleteAndSelect(this.AutoCompleteElement, lite);
        }

        public PopupControl<T> View<T>(int index) where T : ModifiableEntity
        {
            ViewElementIndex(index).Click(); 

            return this.ViewPopup<T>(index);
        }
    }

    public class EntityListCheckBoxProxy : EntityBaseProxy
    {
        public EntityListCheckBoxProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, element, route)
        {
        }

        public IWebElement CheckBoxElement(Lite<Entity> lite)
        {
            throw new InvalidOperationException();
            //return By.CssSelector("#" + this.Prefix + " input[value^='" + EntityInfoProxy.FromLite(lite) + "']");
        }

        public List<Lite<Entity>> GetDataElements()
        {
            throw new InvalidOperationException();
            //return Selenium.FindElements(By.CssSelector("#" + this.Prefix + " input[type=checkbox]")).Select(cb => EntityInfoProxy.FromFormValue(cb.GetAttribute("value")).ToLite(cb.GetParent().Text)).ToList();
        }

        public void SetChecked(Lite<Entity> lite, bool isChecked)
        {
            CheckBoxElement(lite).SetChecked(isChecked);
        }
    }


    public class FileLineProxy : BaseLineProxy
    {
        public FileLineProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, element, route)
        {
        }

        public void SetPath(string path)
        {
            throw new NotImplementedException();
            //Selenium.FindElement(By.CssSelector("#{0}_sfFile".FormatWith(Prefix))).SendKeys(path);
            ////Selenium.FireEvent("{0}_sfFile".FormatWith(Prefix), "change");
            //Selenium.Wait(() =>
            //    Selenium.IsElementVisible(By.CssSelector("#{0}_sfLink".FormatWith(Prefix))) ||
            //    Selenium.IsElementVisible(By.CssSelector("#{0}_sfToStr".FormatWith(Prefix))));
        }
    }
}
