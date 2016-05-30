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

        public IWebElement CreateElement
        {
            get { return this.Element.TryFindElement(By.CssSelector("#{0}_btnCreate".FormatWith()); }
        }

        protected void CreateEmbedded<T>(bool mlist)
        {
            WaitChanges(() =>
            {
                this.CreateElement.Click();

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

            this.CreateElement.Click();

            string newPrefix = ChooseType(typeof(T), index);

            PropertyRoute route = index == null ? this.Route : this.Route.Add("Item");

            return new PopupControl<T>(this.Selenium, newPrefix, route)
            {
                Disposing = okPressed => { WaitNewChanges(changes, "create dialog closed"); }
            };
        }

        public IWebElement ViewElement
        {
            get { return By.CssSelector("#{0}_btnView".FormatWith(this.Route.PropertyString())); }
        }

        protected PopupControl<T> ViewPopup<T>(int? index) where T : ModifiableEntity
        {
            string newPrefix = Prefix + (index == null ? "" : ("_" + index));
            string changes = GetChanges();

            PropertyRoute route = index == null ? this.Route : this.Route.Add("Item");

            return new PopupControl<T>(this.Selenium, newPrefix, route)
            {
                Disposing = okPressed => WaitNewChanges(changes, "create dialog closed")
            };
        }

        public IWebElement FindElement
        {
            get { return By.CssSelector("#{0}_btnFind".FormatWith(Prefix)); }
        }

        public IWebElement RemoveElement
        {
            get { return By.CssSelector("#{0}_btnRemove".FormatWith(Prefix)); }
        }

        public void Remove()
        {
            WaitChanges(() => Selenium.FindElement(RemoveLocator).Click(), "removing");
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

        private string ChooseType(Type selectType, int? index)
        {
            string newPrefix = Prefix + (index == null ? "" : ("_" + index));

            Selenium.Wait(() => Popup.IsPopupVisible(Selenium, newPrefix) || Popup.IsPopupVisible(Selenium, Prefix), 
                () => "Popup {0} or {1} to be visible".FormatWith(newPrefix, Prefix));

            if (selectType == null)
            {
                if (ChooserPopup.IsChooser(Selenium, Prefix))
                    throw new InvalidOperationException("TypeChooser found but arugment selectType is not specified");
            }
            else
            {
                if (ChooserPopup.IsChooser(Selenium, Prefix))
                {
                    ChooserPopup.ChooseButton(Selenium, Prefix, TypeLogic.GetCleanName(selectType));

                    Selenium.Wait(() => !ChooserPopup.IsChooser(Selenium, Prefix));
                }
            }

            return newPrefix;
        }

        public void WaitChanges(Action action, string actionDescription)
        {
            var changes = GetChanges();

            action();

            WaitNewChanges(changes, actionDescription);
        }

        public void WaitNewChanges(string changes, string actionDescription)
        {
            Selenium.Wait(() => GetChanges() != changes, () => "Waiting for changes after {0} in {1}".FormatWith(actionDescription, Prefix));
        }

        public string GetChanges()
        {
            return (string)Selenium.ExecuteScript("return $('#{0}').attr('changes')".FormatWith(Prefix));
        }


        protected IWebElement RuntimeInfoElementInternal(int? index = null)
        {
            return By.CssSelector("#" + Prefix + (index == null ? "" : ("_" + index)) + "_sfRuntimeInfo");
        }

        protected RuntimeInfoProxy RuntimeInfoInternal(int? index = null)
        {
            return RuntimeInfoProxy.FromFormValue(Selenium.FindElement(RuntimeInfoLocatorInternal(index)).GetAttribute("value"));
        }

        internal void AutoCompleteAndSelect(IWebElement autoCompleteElement, Lite<IEntity> lite)
        {
            WaitChanges(() =>
            {
                Selenium.FindElement(autoCompleteLocator).SafeSendKeys(lite.Id.ToString());
                //Selenium.FireEvent(autoCompleteLocator, "keyup");

                var listLocator = By.CssSelector("ul.typeahead.dropdown-menu");

                Selenium.WaitElementVisible(listLocator);
                IWebElement itemElement = listLocator.CombineCss(" span[data-type='{0}'][data-id='{1}']".FormatWith(TypeLogic.GetCleanName(lite.EntityType), lite.Id));

                Selenium.FindElement(itemLocator).Click();

            }, "autocomplete selection");
        }
    }


    public class EntityLineProxy : EntityBaseProxy
    {
        public EntityLineProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, prefix, route)
        {
        }

        public IWebElement ToStrElement
        {
            get { return By.CssSelector("#{0}_sfToStr".FormatWith(Prefix)); }
        }

        public IWebElement LinkElement
        {
            get { return By.CssSelector("#{0}_sfLink".FormatWith(Prefix)); }
        }

        public bool HasEntity()
        {
            return Selenium.IsElementVisible(LinkLocator);
        }

        public Lite<IEntity> LiteValue
        {
            get { return RuntimeInfo().ToLite(); }
            set
            {
                if (HasEntity())
                    this.Remove();

                if (value != null)
                {
                    if (Selenium.IsElementPresent(AutoCompleteLocator))
                        AutoComplete(value);
                    else if (Selenium.IsElementPresent(FindLocator))
                        this.Find().SelectLite(value);
                    else
                        throw new NotImplementedException("AutoComplete");
                }
            }
        }

        public IWebElement AutoCompleteElement
        {
            get { return By.CssSelector("#{0}_sfToStr".FormatWith(Prefix)); }
        }

        public void AutoComplete(Lite<IEntity> lite)
        {
            base.AutoCompleteAndSelect(AutoCompleteLocator, lite);
        }

        public PopupControl<T> View<T>() where T : ModifiableEntity
        {
            Selenium.FindElement(ViewLocator).Click();

            return base.ViewPopup<T>(null);
        }

        public IWebElement RuntimeInfoElement()
        {
            return RuntimeInfoLocatorInternal(null);
        }

        public RuntimeInfoProxy RuntimeInfo()
        {
            return RuntimeInfoInternal(null);
        }
    }

    public class EntityComboProxy : EntityBaseProxy
    {
        public EntityComboProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, prefix, route)
        {
        }

        public IWebElement ComboElement
        {
            get { return By.CssSelector("#{0}_sfCombo".FormatWith(Prefix)); }
        }

        public Lite<IEntity> LiteValue
        {
            get
            {
                var text = Selenium.FindElement(ComboLocator).SelectElement().AllSelectedOptions.SingleOrDefaultEx()?.Text;

                return RuntimeInfo().ToLite(text);
            }
            set
            {
                Selenium.FindElement(ComboLocator).SelectElement().SelectByValue(value == null ? "" : value.Key());
            }
        }

        public List<Lite<Entity>> Options()
        {
           return Selenium.FindElement(ComboLocator)
                .SelectElement().Options
                .Select(o => Lite.Parse(o.GetAttribute("value"))?.Do(l => l.SetToString(o.Text)))
                .ToList();
        }

        public PopupControl<T> View<T>() where T : ModifiableEntity
        {
            Selenium.FindElement(ViewLocator).Click();

            return base.ViewPopup<T>(null);
        }

        public void SelectLabel(string label)
        {
            WaitChanges(() =>
                Selenium.FindElement(ComboLocator).SelectElement().SelectByText(label),
                "ComboBox selected");
        }

        public void SelectIndex(int index)
        {
            WaitChanges(() =>
                Selenium.FindElement(ComboLocator).SelectElement().SelectByIndex(index + 1),
                "ComboBox selected");
        }

        public IWebElement RuntimeInfoElement()
        {
            return RuntimeInfoLocatorInternal(null);
        }

        public RuntimeInfoProxy RuntimeInfo()
        {
            return RuntimeInfoInternal(null);
        }
    }

    public class EntityDetailProxy : EntityBaseProxy
    {
        public EntityDetailProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, prefix, route)
        {
        }

        public By DivSelector
        {
            get { return By.CssSelector("#{0}_sfDetail".FormatWith(Prefix)); }
        }

        public bool HasEntity()
        {
            return Selenium.IsElementPresent(DivSelector.CombineCss(" *:first-child"));
        }

        public Lite<IEntity> Lite
        {
            get { return RuntimeInfo().ToLite(); }
            set
            {
                if (HasEntity())
                    this.Remove();

                if (Selenium.IsElementPresent(FindLocator))
                    this.Find().SelectLite(value);

                throw new NotImplementedException("AutoComplete");
            }
        }

        public LineContainer<T> Details<T>() where T : ModifiableEntity
        {
            return new LineContainer<T>(Selenium, Prefix, Route);
        }

        public IWebElement RuntimeInfoElement()
        {
            return RuntimeInfoLocatorInternal(null);
        }

        public RuntimeInfoProxy RuntimeInfo()
        {
            return RuntimeInfoInternal(null);
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
            : base(selenium, prefix, route)
        {
        }

        public IWebElement OptionIdElement(int index)
        {
            return By.CssSelector("#{0}_{1}_sfToStr".FormatWith(Prefix, index));
        }

        public IWebElement ListElement
        {
            get { return By.CssSelector("#{0}_sfList".FormatWith(Prefix)); }
        }

        public void Select(int index)
        {
            var selectElement = Selenium.FindElement(ListLocator).SelectElement();
            if (selectElement.IsMultiple)
                selectElement.DeselectAll();

            var id = "{0}_{1}_sfToStr".FormatWith(Prefix, index);
            selectElement.SelectByPredicate(a => a.GetAttribute("id") == id);
        }

        public PopupControl<T> View<T>(int index) where T : ModifiableEntity
        {
            Select(index);

            Selenium.FindElement(ViewLocator).Click();

            return base.ViewPopup<T>(index);
        }

        public bool HasEntity(int index)
        {
            return Selenium.IsElementPresent(OptionIdLocator(index));
        }


        public int ItemsCount()
        {
            return (int)(long)Selenium.ExecuteScript("return $('#{0}_sfList option').length".FormatWith(Prefix));
        }

        public override int? NewIndex()
        {
            string result = (string)Selenium.ExecuteScript("return $('#{0}_sfList option').get().map(function(a){{return parseInt(a.id.substr('{0}'.length + 1));}}).join()".FormatWith(Prefix));

            return string.IsNullOrEmpty(result) ? 0 : result.Split(',').Select(int.Parse).Max() + 1;
        }



        public IWebElement RuntimeInfoElement(int index)
        {
            return RuntimeInfoLocatorInternal(index);
        }

        public RuntimeInfoProxy RuntimeInfo(int index)
        {
            return RuntimeInfoInternal(index);
        }


        public void DoubleClick(int index)
        {
            Select(index);
            Selenium.FindElement(OptionIdLocator(index)).DoubleClick();
        }
    }

    public class EntityListDetailProxy : EntityListProxy
    {
        public EntityListDetailProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, prefix, route)
        {
            this.DetailsDivSelector = By.CssSelector("#{0}_sfDetail".FormatWith(Prefix));
        }

        public By DetailsDivSelector { get; set; }

        public bool HasDetailEntity()
        {
            return Selenium.FindElements(DetailsDivSelector.CombineCss(" *")).Any();
        }

        public LineContainer<T> CreateElement<T>() where T : ModifiableEntity
        {
            var index = NewIndex();

            CreateEmbedded<T>(mlist: true);

            return this.Details<T>(index.Value);
        }

        public LineContainer<T> Details<T>(int index) where T : ModifiableEntity
        {
            return new LineContainer<T>(Selenium, Prefix + "_" + index, Route.Add("Item"));
        }
    }


    public class EntityRepeaterProxy : EntityBaseProxy
    {
        public EntityRepeaterProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, prefix, route)
        {
        }

        public IWebElement ItemsContainerElement
        {
            get { return By.CssSelector("#{0}_sfItemsContainer".FormatWith(Prefix)); }
        }

        public virtual By RepeaterItemSelector(int index)
        {
            return ItemsContainerLocator.CombineCss(" > #{0}_{1}_sfRepeaterItem".FormatWith(Prefix, index));
        }

        public void WaitItemLoaded(int index)
        {
            Selenium.WaitElementPresent(RepeaterItemSelector(index));
        }

        public virtual void MoveUp(int index)
        {
            Selenium.FindElement(By.CssSelector("#{0}_{1}_btnUp".FormatWith(Prefix, index))).Click();
        }

        public virtual void MoveDown(int index)
        {
            Selenium.FindElement(By.CssSelector("#{0}_{1}_btnDown".FormatWith(Prefix, index))).Click();
        }

        public bool HasEntity(int index)
        {
            return Selenium.IsElementPresent(RepeaterItemSelector(index)); ;
        }

        public virtual int ItemsCount()
        {
            return (int)(long)Selenium.ExecuteScript("return $('{0} fieldset:not(.hidden)').length".FormatWith(ItemsContainerLocator.CssSelector()));
        }

        public virtual int HiddenItemsCount()
        {
            return (int)(long)Selenium.ExecuteScript("return $('{0} fieldset.hidden').length".FormatWith(ItemsContainerLocator.CssSelector()));
        }

        public override int? NewIndex()
        {
            string result = (string)Selenium.ExecuteScript("return $('#{0}_sfItemsContainer fieldset').get().map(function(a){{return parseInt(a.id.substr('{0}'.length + 1));}}).join()".FormatWith(Prefix));

            return string.IsNullOrEmpty(result) ? 0 : result.Split(',').Select(int.Parse).Max() + 1;
        }

        public LineContainer<T> Details<T>(int index) where T : ModifiableEntity
        {
            return new LineContainer<T>(Selenium, Prefix + "_" + index, Route.Add("Item"));
        }

        public IWebElement RemoveElementIndex(int index)
        {
            return By.CssSelector("#{0}_{1}_btnRemove".FormatWith(Prefix, index));
        }

        public void Remove(int index)
        {
            Selenium.FindElement(RemoveLocatorIndex(index)).Click();
        }

        public IWebElement RuntimeInfoElement(int index)
        {
            return RuntimeInfoLocatorInternal(index);
        }

        public RuntimeInfoProxy RuntimeInfo(int index)
        {
            return RuntimeInfoInternal(index);
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
            : base(selenium, prefix, route)
        {
        }

        public override void MoveUp(int index)
        {
            Selenium.FindElement(By.CssSelector("#{0}_{1}_btnUp".FormatWith(Prefix, index))).Click();
        }

        public override void MoveDown(int index)
        {
            Selenium.FindElement(By.CssSelector("#{0}_{1}_btnDown".FormatWith(Prefix, index))).Click();
        }

        public override int ItemsCount()
        {
            return (int)(long)Selenium.ExecuteScript("return $('#{0}_sfItemsContainer li').length".FormatWith(ItemsContainerLocator));
        }

        public override int? NewIndex()
        {
            string result = (string)Selenium.ExecuteScript("return $('#{0}_sfItemsContainer li').get().map(function(a){{return parseInt(a.id.substr('{0}'.length + 1));}}).join()".FormatWith(Prefix));

            return string.IsNullOrEmpty(result) ? 0 : result.Split(',').Select(int.Parse).Max() + 1;
        }
    }

    public class EntityStripProxy : EntityBaseProxy
    {
        public EntityStripProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, prefix, route)
        {
        }

        public IWebElement ItemsContainerElement
        {
            get { return By.CssSelector("#{0}_sfItemsContainer".FormatWith(Prefix)); }
        }

        public By StripItemSelector(int index)
        {
            return ItemsContainerLocator.CombineCss(" > #{0}_{1}_sfStripItem".FormatWith(Prefix, index));
        }

        public void WaitItemLoaded(int index)
        {
            Selenium.WaitElementPresent(StripItemSelector(index));
        }

        public void MoveUp(int index)
        {
            Selenium.FindElement(By.CssSelector("#{0}_{1}_btnUp".FormatWith(Prefix, index))).Click();
        }

        public void MoveDown(int index)
        {
            Selenium.FindElement(By.CssSelector("#{0}_{1}_btnDown".FormatWith(Prefix, index))).Click();
        }

        public bool HasEntity(int index)
        {
            return Selenium.IsElementPresent(StripItemSelector(index));
        }

        public int ItemsCount()
        {
            return (int)(long)Selenium.ExecuteScript("return $('#{0}_sfItemsContainer li.sf-strip-element').length".FormatWith(ItemsContainerLocator));
        }

        public override int? NewIndex()
        {
            var result = (string)Selenium.ExecuteScript("return $('#{0}_sfItemsContainer li.sf-strip-element').get().map(function(a){{return parseInt(a.id.substr('{0}'.length + 1));}}).join()".FormatWith(Prefix));

            return string.IsNullOrEmpty(result) ? 0 : result.Split(',').Select(int.Parse).Max() + 1;
        }


        public IWebElement ViewElementIndex(int index)
        {
            return By.CssSelector("#{0}_{1}_btnView".FormatWith(Prefix, index));
        }

        public IWebElement RemoveElementIndex(int index)
        {
            return By.CssSelector("#{0}_{1}_btnRemove".FormatWith(Prefix, index));
        }

        public void Remove(int index)
        {
            Selenium.FindElement(RemoveLocatorIndex(index)).Click();
        }

        public IWebElement RuntimeInfoElement(int index)
        {
            return RuntimeInfoLocatorInternal(index);
        }

        public RuntimeInfoProxy RuntimeInfo(int index)
        {
            return RuntimeInfoInternal(index);
        }

        public IWebElement AutoCompleteElement
        {
            get { return By.CssSelector("#{0}_sfToStr".FormatWith(Prefix)); }
        }

        public void AutoComplete(Lite<IEntity> lite)
        {
            base.AutoCompleteAndSelect(AutoCompleteLocator, lite);
        }

        public PopupControl<T> View<T>(int index) where T : ModifiableEntity
        {
            Selenium.FindElement(ViewLocatorIndex(index)).Click(); 

            return this.ViewPopup<T>(index);
        }
    }

    public class EntityListCheckBoxProxy : EntityBaseProxy
    {
        public EntityListCheckBoxProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, prefix, route)
        {
        }

        public IWebElement CheckBoxElement(Lite<Entity> lite)
        {
            return By.CssSelector("#" + this.Prefix + " input[value^='" + RuntimeInfoProxy.FromLite(lite) + "']");
        }

        public List<Lite<Entity>> GetDataElements()
        {
            return Selenium.FindElements(By.CssSelector("#" + this.Prefix + " input[type=checkbox]")).Select(cb => RuntimeInfoProxy.FromFormValue(cb.GetAttribute("value")).ToLite(cb.GetParent().Text)).ToList();
        }

        public void SetChecked(Lite<Entity> lite, bool isChecked)
        {
            this.Selenium.FindElement(CheckBoxLocator(lite)).SetChecked(isChecked);
        }
    }


    public class FileLineProxy : BaseLineProxy
    {
        public FileLineProxy(RemoteWebDriver selenium, IWebElement element, PropertyRoute route)
            : base(selenium, prefix, route)
        {
        }

        public void SetPath(string path)
        {
            Selenium.FindElement(By.CssSelector("#{0}_sfFile".FormatWith(Prefix))).SendKeys(path);
            //Selenium.FireEvent("{0}_sfFile".FormatWith(Prefix), "change");
            Selenium.Wait(() =>
                Selenium.IsElementVisible(By.CssSelector("#{0}_sfLink".FormatWith(Prefix))) ||
                Selenium.IsElementVisible(By.CssSelector("#{0}_sfToStr".FormatWith(Prefix))));
        }
    }
}
