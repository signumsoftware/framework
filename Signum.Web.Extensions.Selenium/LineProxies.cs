using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Selenium;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.Reflection;

namespace Signum.Web.Selenium
{

    public class BaseLineProxy
    {
        public ISelenium Selenium { get; private set; }

        public string Prefix { get; private set; }

        public PropertyRoute Route { get; private set; }

        public BaseLineProxy(ISelenium selenium, string prefix, PropertyRoute route)
        {
            this.Selenium = selenium;
            this.Prefix = prefix;
            this.Route = route;
        }

        protected static string ToVisible(bool visible)
        {
            return visible ? "visible" : "not visible";
        }
    }

    public class ValueLineProxy : BaseLineProxy
    {
        public ValueLineProxy(ISelenium selenium, string prefix, PropertyRoute route)
            : base(selenium, prefix, route)
        {
        }

        public string StringValue
        {
            get
            {
                if (Selenium.IsElementPresent("jq=input[type=checkbox]{0}".Formato(Prefix)))
                    return Selenium.IsChecked(Prefix).ToString();

                return Selenium.GetValue(Prefix);
            }

            set
            {
                if (Selenium.IsElementPresent("jq=input[type=checkbox]{0}".Formato(Prefix)))
                    Selenium.SetChecked(Prefix, bool.Parse(value));
                else
                    Selenium.Type(Prefix, value);
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

        public void SetValue(object value, string format)
        {
            StringValue = value == null ? null :
                    value is IFormattable ? ((IFormattable)value).ToString(format, null) :
                    value.ToString();
        }
    }

    public abstract class EntityBaseProxy : BaseLineProxy
    {
        public EntityBaseProxy(ISelenium selenium, string prefix, PropertyRoute route)
            : base(selenium, prefix, route)
        {
        }

        public string CreateLocator
        {
            get { return "jq=#{0}_btnCreate".Formato(Prefix); }
        }

        public void Create()
        {
            Selenium.Click(CreateLocator);
        }

        public void CreateImplementations(Type selectType)
        {
            Create();

            string newPrefix = ChooseType(selectType, NewIndex());
        }

        public virtual int? NewIndex()
        {
            return null;
        }

        public PopupControl<T> CreatePopup<T>() where T : ModifiableEntity
        {
            var index = NewIndex();

            Create();

            string newPrefix = ChooseType(typeof(T), index);

            PropertyRoute route = index == null ? this.Route : this.Route.Add("Item");

            return new PopupControl<T>(this.Selenium, newPrefix, route);
        }

        public string ViewLocator
        {
            get { return "jq=#{0}_btnView".Formato(Prefix); }
        }

        protected PopupControl<T> ViewPopup<T>(int? index) where T : ModifiableEntity
        {
            string newPrefix = Prefix + (index == null ? "" : ("_" + index));

            PropertyRoute route = index == null ? this.Route : this.Route.Add("Item");

            return new PopupControl<T>(this.Selenium, newPrefix, route);
        }

        public string FindLocator
        {
            get { return "jq=#{0}_btnFind".Formato(Prefix); }
        }

        public string RemoveLocator
        {
            get { return "jq=#{0}_btnRemove".Formato(Prefix); }
        }

        public void Remove()
        {
            Selenium.Click(RemoveLocator);
        }

        public SearchPopupProxy Find(Type selectType = null)
        {
            int? index = NewIndex();

            Selenium.Click(FindLocator);

            string newPrefix = ChooseType(selectType, index);

            return new SearchPopupProxy(Selenium, newPrefix);
        }

        private string ChooseType(Type selectType, int? index)
        {
            string newPrefix = Prefix + (index == null ? "" : ("_" + index));

            Selenium.Wait(() => Popup.IsPopupVisible(Selenium, newPrefix) || Popup.IsPopupVisible(Selenium, Prefix), () => "Popup {0} or {1} to be visible".Formato(newPrefix, Prefix));

            if (selectType == null)
            {
                if (Popup.IsChooser(Selenium, Prefix))
                    throw new InvalidOperationException("TypeChooser found but arugment selectType is not specified");
            }
            else
            {
                if (Popup.IsChooser(Selenium, Prefix))
                {
                    Selenium.Click("jq=[data-value=" + TypeLogic.GetCleanName(selectType) + "]");

                    Selenium.Wait(() => !Popup.IsChooser(Selenium, Prefix));
                }
            }

            return newPrefix;
        }

        protected string RuntimeInfoLocatorInternal(int? index = null)
        {
            return "jq=#" + Prefix + (index == null ? "" : ("_" + index)) + "_sfRuntimeInfo";
        }

        protected RuntimeInfoProxy RuntimeInfoInternal(int? index = null)
        {
            return RuntimeInfoProxy.FromFormValue(Selenium.GetValue(RuntimeInfoLocatorInternal(index)));
        }

        internal void AutoCompleteAndSelect(string autoCompleteLocator, Lite<IIdentifiable> lite)
        {
            Selenium.Type(autoCompleteLocator, lite.Id.ToString());
            Selenium.FireEvent(autoCompleteLocator, "keydown");

            Selenium.WaitElementPresent("jq=.ui-autocomplete:visible");
            string locator = "jq=.ui-autocomplete:visible > li[data-type='{0}'][data-id={1}] > a".Formato(TypeLogic.GetCleanName(lite.EntityType), lite.Id);
            Selenium.Click(locator);
        }
    }


    public class EntityLineProxy : EntityBaseProxy
    {
        public EntityLineProxy(ISelenium selenium, string prefix, PropertyRoute route)
            : base(selenium, prefix, route)
        {
        }

        public string ToStrLocator
        {
            get { return "jq=#{0}_sfToStr".Formato(Prefix); }
        }

        public string LinkLocator
        {
            get { return "jq=#{0}_sfLink".Formato(Prefix); }
        }

        public bool HasEntity()
        {
            bool toStrVisible = Selenium.IsElementPresent(ToStrLocator + ":visible");
            bool linkVisible = Selenium.IsElementPresent(LinkLocator + ":visible");

            if (toStrVisible != !linkVisible)
                throw new InvalidOperationException("{0}_sfToStr is {1} but {0}_sfLink is {2}".Formato(Prefix,
                    toStrVisible ? "visible" : "not visible",
                    linkVisible ? "visible" : "not visible"));

            return linkVisible;
        }

        public Lite<IIdentifiable> LiteValue
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

        public string AutoCompleteLocator
        {
            get { return "jq=#{0}_sfToStr".Formato(Prefix); }
        }

        public void AutoComplete(Lite<IIdentifiable> lite)
        {
            base.AutoCompleteAndSelect(AutoCompleteLocator, lite);
        }

        public PopupControl<T> View<T>() where T : ModifiableEntity
        {
            Selenium.Click(ViewLocator);

            return base.ViewPopup<T>(null);
        }

        public string RuntimeInfoLocator()
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
        public EntityComboProxy(ISelenium selenium, string prefix, PropertyRoute route)
            : base(selenium, prefix, route)
        {
        }

        public string ComboLocator
        {
            get { return "jq=#{0}_sfCombo".Formato(Prefix); }
        }

        public Lite<IIdentifiable> LiteValue
        {
            get { return RuntimeInfo().ToLite(); }
            set
            {
                Selenium.Select(ComboLocator, "value=" + (value == null ? null : value.Id.ToString()));
            }
        }

        public PopupControl<T> View<T>() where T : ModifiableEntity
        {
            Selenium.Click(ViewLocator);

            return base.ViewPopup<T>(null);
        }

        public void SelectLabel(string label)
        {
            Selenium.Select(ComboLocator, "label=" + label);
        }

        public void SelectIndex(int index)
        {
            Selenium.Select(ComboLocator, "index=" + (index + 1));
        }

        public string RuntimeInfoLocator()
        {
            return RuntimeInfoLocatorInternal(null);
        }

        public RuntimeInfoProxy RuntimeInfo()
        {
            return RuntimeInfoInternal(null);
        }
    }

    public class EntityLineDetailProxy : EntityBaseProxy
    {
        public EntityLineDetailProxy(ISelenium selenium, string prefix, PropertyRoute route)
            : base(selenium, prefix, route)
        {
        }

        public string DivSelector
        {
            get { return "jq=#{0}_sfDetail".Formato(Prefix); }
        }

        public bool HasEntity()
        {
            bool parentVisible = Selenium.IsElementPresent(DivSelector + ":parent");
            bool emptyVisible = Selenium.IsElementPresent(DivSelector + ":empty");

            if (parentVisible != !emptyVisible)
                throw new InvalidOperationException("{0}sfDetail is {1} but has {1}".Formato(Prefix,
                    parentVisible ? "has parent" : "has no parent",
                    emptyVisible ? "empty" : "not empty"));


            return parentVisible;
        }

        public Lite<IIdentifiable> Lite
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

        public string RuntimeInfoLocator()
        {
            return RuntimeInfoLocatorInternal(null);
        }

        public RuntimeInfoProxy RuntimeInfo()
        {
            return RuntimeInfoInternal(null);
        }
    }

    public class EntityListProxy : EntityBaseProxy
    {
        public EntityListProxy(ISelenium selenium, string prefix, PropertyRoute route)
            : base(selenium, prefix, route)
        {
        }

        public string OptionIdLocator(int index)
        {
            return "jq=#{0}_{1}_sfToStr".Formato(Prefix, index);
        }

        public string ListLocator
        {
            get { return "jq=#{0}_sfList".Formato(Prefix); }
        }

        public void Select(int index)
        {
            Selenium.Select(ListLocator, "id={0}_{1}_sfToStr".Formato(Prefix, index));
        }

        public void AddSelection(int index)
        {
            Selenium.AddSelection(ListLocator, "id={0}_{1}_sfToStr".Formato(Prefix, index));
        }

        public PopupControl<T> View<T>(int index) where T : ModifiableEntity
        {
            Select(index);

            Selenium.Click(ViewLocator);

            return base.ViewPopup<T>(index);
        }

        public bool HasEntity(int index)
        {
            bool optionVisible = Selenium.IsElementPresent(OptionIdLocator(index));
            bool runtimeInfoVisible = Selenium.IsElementPresent(RuntimeInfoLocator(index));

            if (optionVisible != runtimeInfoVisible)
                throw new InvalidOperationException("{0} is {1} but {2} is {3}".Formato(OptionIdLocator(index), ToVisible(optionVisible), RuntimeInfoLocator(index), ToVisible(runtimeInfoVisible)));

            return optionVisible;
        }


        public int ItemsCount()
        {
            string result = Selenium.GetEval("window.$('#{0}_sfList option').length".Formato(Prefix));

            return int.Parse(result);
        }

        public override int? NewIndex()
        {
            string result = Selenium.GetEval("window.$('#{0}_sfList option').get().map(function(a){{return parseInt(a.id.substr('{0}'.length + 1));}}).join()".Formato(Prefix));

            return string.IsNullOrEmpty(result) ? 0 : result.Split(',').Select(int.Parse).Max() + 1;
        }



        public string RuntimeInfoLocator(int index)
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
            Selenium.DoubleClick(OptionIdLocator(index));
        }
    }

    public class EntityListDetailProxy : EntityListProxy
    {
        public EntityListDetailProxy(ISelenium selenium, string prefix, PropertyRoute route)
            : base(selenium, prefix, route)
        {
        }

        public string DetailsDivSelector
        {
            get { return "jq=#{0}_sfDetail".Formato(Prefix); }
        }

        public bool HasDetailEntity()
        {
            bool parentVisible = Selenium.IsElementPresent(DetailsDivSelector + ":parent");
            bool emptyVisible = Selenium.IsElementPresent(DetailsDivSelector + ":empty");

            if (parentVisible != !emptyVisible)
                throw new InvalidOperationException("{0}_sfDetail is {1} but has {1}".Formato(Prefix,
                    parentVisible ? "has parent" : "has no parent",
                    emptyVisible ? "empty" : "not empty"));


            return parentVisible;
        }

        public LineContainer<T> Details<T>(int index) where T : ModifiableEntity
        {
            return new LineContainer<T>(Selenium, Prefix + "_" + index, Route.Add("Item"));
        }
    }


    public class EntityRepeaterProxy : EntityBaseProxy
    {
        public EntityRepeaterProxy(ISelenium selenium, string prefix, PropertyRoute route)
            : base(selenium, prefix, route)
        {
        }

        public string ItemsContainerLocator
        {
            get { return "jq=#{0}_sfItemsContainer".Formato(Prefix); }
        }

        public string RepeaterItemSelector(int index)
        {
            return "{0} > #{1}_{2}_sfRepeaterItem".Formato(ItemsContainerLocator, Prefix, index);
        }

        public void WaitItemLoaded(int index)
        {
            Selenium.WaitElementPresent(RepeaterItemSelector(index));
        }

        public void MoveUp(int index)
        {
            Selenium.Click("{0} > legend .sf-move-up".Formato(RepeaterItemSelector(index)));
        }

        public void MoveDown(int index)
        {
            Selenium.Click("{0} > legend .sf-move-down".Formato(RepeaterItemSelector(index)));
        }

        public bool HasEntity(int index)
        {
            bool divPresent = Selenium.IsElementPresent(RepeaterItemSelector(index));
            bool runtimeInfoPresent = Selenium.IsElementPresent(RuntimeInfoLocator(index));

            if (divPresent != runtimeInfoPresent)
                throw new InvalidOperationException("{0} is {2} but {1} is {3}".Formato(
                    RepeaterItemSelector(index), RuntimeInfoLocator(index),
                    ToVisible(divPresent), ToVisible(runtimeInfoPresent)));

            return divPresent;
        }

        public int ItemsCount()
        {
            string result = Selenium.GetEval("window.$('#{0}_sfItemsContainer fieldset').length".Formato(ItemsContainerLocator));

            return int.Parse(result);
        }

        public override int? NewIndex()
        {
            string result = Selenium.GetEval("window.$('#{0}_sfItemsContainer fieldset').get().map(function(a){{return parseInt(a.id.substr('{0}'.length + 1));}}).join()".Formato(Prefix));

            return string.IsNullOrEmpty(result) ? 0 : result.Split(',').Select(int.Parse).Max() + 1;
        }

        public LineContainer<T> Details<T>(int index) where T : ModifiableEntity
        {
            return new LineContainer<T>(Selenium, Prefix + "_" + index, Route.Add("Item"));
        }

        public string RemoveLocatorIndex(int index)
        {
            return "jq=#{0}_{1}_btnRemove".Formato(Prefix, index);
        }

        public void Remove(int index)
        {
            Selenium.Click(RemoveLocatorIndex(index));
        }

        public string RuntimeInfoLocator(int index)
        {
            return RuntimeInfoLocatorInternal(index);
        }

        public RuntimeInfoProxy RuntimeInfo(int index)
        {
            return RuntimeInfoInternal(index);
        }
    }

    public class EntityStripProxy : EntityBaseProxy
    {
        public EntityStripProxy(ISelenium selenium, string prefix, PropertyRoute route)
            : base(selenium, prefix, route)
        {
        }

        public string ItemsContainerLocator
        {
            get { return "jq=#{0}_sfItemsContainer".Formato(Prefix); }
        }

        public string StripItemSelector(int index)
        {
            return "{0} > #{1}_{2}_sfStripItem".Formato(ItemsContainerLocator, Prefix, index);
        }

        public void WaitItemLoaded(int index)
        {
            Selenium.WaitElementPresent(StripItemSelector(index));
        }

        public void MoveUp(int index)
        {
            Selenium.Click("jq=#{0}_{1}_btnUp".Formato(Prefix, index));
        }

        public void MoveDown(int index)
        {
            Selenium.Click("jq=#{0}_{1}_btnDown".Formato(Prefix, index));
        }

        public bool HasEntity(int index)
        {
            bool divPresent = Selenium.IsElementPresent(StripItemSelector(index));
            bool runtimeInfoPresent = Selenium.IsElementPresent(RuntimeInfoLocator(index));

            if (divPresent != runtimeInfoPresent)
                throw new InvalidOperationException("{0} is {2} but {1} is {3}".Formato(
                    StripItemSelector(index), RuntimeInfoLocator(index),
                    ToVisible(divPresent), ToVisible(runtimeInfoPresent)));

            return divPresent;
        }

        public int ItemsCount()
        {
            string result = Selenium.GetEval("window.$('#{0}_sfItemsContainer li.sf-strip-element').length".Formato(ItemsContainerLocator));

            return int.Parse(result);
        }

        public override int? NewIndex()
        {
            string result = Selenium.GetEval("window.$('#{0}_sfItemsContainer li.sf-strip-element').get().map(function(a){{return parseInt(a.id.substr('{0}'.length + 1));}}).join()".Formato(Prefix));

            return string.IsNullOrEmpty(result) ? 0 : result.Split(',').Select(int.Parse).Max() + 1;
        }


        public string ViewLocatorIndex(int index)
        {
            return "jq=#{0}_{1}_btnView".Formato(Prefix, index);
        }

        public string RemoveLocatorIndex(int index)
        {
            return "jq=#{0}_{1}_btnRemove".Formato(Prefix, index);
        }

        public void Remove(int index)
        {
            Selenium.Click(RemoveLocatorIndex(index));
        }

        public string RuntimeInfoLocator(int index)
        {
            return RuntimeInfoLocatorInternal(index);
        }

        public RuntimeInfoProxy RuntimeInfo(int index)
        {
            return RuntimeInfoInternal(index);
        }

        public string AutoCompleteLocator
        {
            get { return "jq=#{0}_sfToStr".Formato(Prefix); }
        }

        public void AutoComplete(Lite<IIdentifiable> lite)
        {
            base.AutoCompleteAndSelect(AutoCompleteLocator, lite);
        }

        public PopupControl<T> View<T>(int index) where T : ModifiableEntity
        {
            Selenium.Click(ViewLocatorIndex(index)); 

            return this.ViewPopup<T>(index);
        }
    }


    public class FileLineProxy : BaseLineProxy
    {
        public FileLineProxy(ISelenium selenium, string prefix, PropertyRoute route)
            : base(selenium, prefix, route)
        {
        }

        public void SetPath(string path)
        {
            Selenium.WaitElementPresent("jq=#{0}_DivNew .sf-file-drop:visible".Formato(Prefix));
            Selenium.Type("{0}_sfFile".Formato(Prefix), path);
            Selenium.FireEvent("{0}_sfFile".Formato(Prefix), "change");
            Selenium.WaitElementPresent("jq=#{0}_sfToStr:visible".Formato(Prefix));
        }
    }
}
