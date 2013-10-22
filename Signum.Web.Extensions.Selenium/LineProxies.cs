using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Selenium;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Utilities;

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

    public abstract class EntityBaseProxy : BaseLineProxy
    {
        public EntityBaseProxy(ISelenium selenium, string prefix, PropertyRoute route)
            : base(selenium, prefix, route)
        {
        }

        public string CreateLocator
        {
            get { return "jq=#{0}btnCreate".Formato(Prefix); }
        }

        public void Create()
        {
            Selenium.Click(CreateLocator);
        }

        public void CreateImplementations(Type selectType, int? index = null)
        {
            Create();

            string newPrefix = Prefix + (index == null ? "" : ("_" + index));

            Selenium.WaitAjaxFinished(() => Popup.IsPopupVisible(Selenium, newPrefix));

            if (!Popup.IsChooser(Selenium, newPrefix))
                throw new InvalidOperationException("No chooser found");

            Selenium.Click(TypeLogic.GetCleanName(selectType));
        }

        public PopupControl<T> CreateWaitPopup<T>(int? index = null) where T : ModifiableEntity
        {
            Create();

            string newPrefix = Prefix + (index == null ? "" : ("_" + index));

            Selenium.WaitAjaxFinished(() => Popup.IsPopupVisible(Selenium, newPrefix));

            if (Popup.IsChooser(Selenium, newPrefix))
            {
                Selenium.Click(TypeLogic.GetCleanName(typeof(T)));

                Selenium.WaitAjaxFinished(() => !Popup.IsChooser(Selenium, newPrefix));
            }

            PropertyRoute route = index == 0 ? this.Route : this.Route.Add("Item");

            return new PopupControl<T>(this.Selenium, newPrefix, route);
        }

        public string ViewLocator
        {
            get { return "jq=#{0}btnView".Formato(Prefix); }
        }

        public PopupControl<T> View<T>(int? index = null) where T : ModifiableEntity
        {
            Selenium.Click(ViewLocator);

            string newPrefix = Prefix + (index == null ? "" : ("_" + index));

            Selenium.WaitAjaxFinished(() => Popup.IsPopupVisible(Selenium, newPrefix));

            PropertyRoute route = index == 0 ? this.Route : this.Route.Add("Item");

            return new PopupControl<T>(this.Selenium, newPrefix, route);
        }

        public string FindLocator
        {
            get { return "jq=#{0}btnFind".Formato(Prefix); }
        }

        public SearchPopupProxy Find(Type selectType = null, int? index = null)
        {
            Find();

            string newPrefix = Prefix + (index == null ? "" : ("_" + index));

            Selenium.WaitAjaxFinished(() => Popup.IsPopupVisible(Selenium, newPrefix));

            if (selectType == null)
            {
                if (Popup.IsChooser(Selenium, newPrefix))
                    throw new InvalidOperationException("TypeChooser found but argment selectType is not specified");
            }
            else
            {
                if (Popup.IsChooser(Selenium, newPrefix))
                    throw new InvalidOperationException("No TypeChooser found but argment selectType is set to {0}".Formato(selectType.Name));

                Selenium.Click(TypeLogic.GetCleanName(selectType));

                Selenium.WaitAjaxFinished(() => !Popup.IsChooser(Selenium, newPrefix));
            }

            Selenium.Click(TypeLogic.GetCleanName(selectType));

            return new SearchPopupProxy(Selenium, newPrefix);
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
            return "jq=#{0}{1}_sfToStr".Formato(Prefix, index);
        }

        public string RuntimeInfoLocator(int index)
        {
            return "jq=#{0}{1}_sfRuntimeInfo".Formato(Prefix, index);
        }

        public string ListLocator
        {
            get { return "jq=#{0}sfList".Formato(Prefix); }
        }

        public void Select(int index)
        {
            Selenium.Select(ListLocator, "id=" + OptionIdLocator(index));
        }

        public void AddSelection(int index)
        {
            Selenium.AddSelection(ListLocator, "id=" + OptionIdLocator(index));
        }

        public PopupControl<T> SelectView<T>(int index) where T : ModifiableEntity
        {
            Select(index);

            Selenium.DoubleClick(OptionIdLocator(index));

            string newPrefix = Prefix + index;

            Selenium.WaitAjaxFinished(() => Popup.IsPopupVisible(Selenium, newPrefix));

            return new PopupControl<T>(this.Selenium, newPrefix, this.Route.Add("Item"));
        }

        public bool HasEntity(int index)
        {
            bool optionVisible = Selenium.IsElementPresent(OptionIdLocator(index));
            bool runtimeInfoVisible = Selenium.IsElementPresent(RuntimeInfoLocator(index));

            if (optionVisible != runtimeInfoVisible)
                throw new InvalidOperationException("{0}{1}_sfToStr is {2} but {0}{1}_sfRuntimeInfo is {3}".Formato(Prefix, index, ToVisible(optionVisible), ToVisible(runtimeInfoVisible)));

            return optionVisible;
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
            get { return "jq=#{0}sfItemsContainer".Formato(Prefix); }
        }

        public string RepeaterItemSelector(int index)
        {
            return "{0} > #{1}{2}_sfRepeaterItem".Formato(ItemsContainerLocator, Prefix, index);
        }

        public string RuntimeInfoLocator(int index)
        {
            return "jq=#{0}{1}_sfRuntimeInfo".Formato(Prefix, index);
        }

        public void WaitItemLoaded(int index)
        {
            Selenium.WaitAjaxFinished(() => Selenium.IsElementPresent(RepeaterItemSelector(index)));
        }

        public void ItemMove(int index, bool up)
        {
            Selenium.Click("{0} > legend .sf-move-{1}".Formato(RepeaterItemSelector(index), up ? "up" : "down"));
        }

        public bool HasEntity(int index)
        {
            bool divPresent = Selenium.IsElementPresent(RepeaterItemSelector(index));
            bool runtimeInfoPresent = Selenium.IsElementPresent(RuntimeInfoLocator(index));

            if (divPresent != runtimeInfoPresent)
                throw new InvalidOperationException("{0}{1}_sfToStr is {2} but {0}{1}_sfRuntimeInfo is {3}".Formato(Prefix, index, ToVisible(divPresent), ToVisible(runtimeInfoPresent)));

            return divPresent;
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
            get { return "jq=#{0}sfToStr".Formato(Prefix); }
        }

        public string LinkLocator
        {
            get { return "jq=#{0}sfLink".Formato(Prefix); }
        }

        public bool HasEntity()
        {
            bool toStrVisible = Selenium.IsElementPresent(ToStrLocator + ":visible");
            bool linkVisible = Selenium.IsElementPresent(LinkLocator + ":visible");

            if (toStrVisible != !linkVisible)
                throw new InvalidOperationException("{0}sfToStr is {1} but {0}sfLink is {2}".Formato(Prefix,
                    toStrVisible ? "visible" : "not visible",
                    linkVisible ? "visible" : "not visible"));

            return linkVisible;
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
            get { return "jq=#{0}sfDetail".Formato(Prefix); }
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
    }

}
