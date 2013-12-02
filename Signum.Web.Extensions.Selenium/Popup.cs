using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Selenium;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Web.Selenium
{
    public class Popup : IDisposable
    {
        public ISelenium Selenium { get; private set; }

        public string Prefix { get; private set; }

        public Popup(ISelenium selenium, string prefix)
        {
            this.Selenium = selenium;
            this.Prefix = prefix;
        }

        public string PopupVisibleLocator
        {
            get { return "jq=#{0}_Temp:visible".Formato(Prefix); }
        }

        public string CloseButtonLocator
        {
            get { return "jq=[aria-describedby='{0}_Temp'] .ui-dialog-titlebar-close:visible".Formato(Prefix); }
        }

        public void Close()
        {
            Selenium.Click(CloseButtonLocator);
        }

        public virtual void Dispose()
        {
            if (Selenium.IsElementPresent(CloseButtonLocator))
                Selenium.Click(CloseButtonLocator);

            Selenium.WaitElementDisapear(PopupVisibleLocator);
        }

        public string OkButtonLocator
        {
            get { return "jq=#{0}_btnOk".Formato(Prefix); }
        }

        public void OkWaitSubmit()
        {
            Selenium.Click(OkButtonLocator);
            Selenium.WaitForPageToLoad();
        }

        public NormalPage<T> OkWaitNormalPage<T>() where T : IdentifiableEntity
        {
            Selenium.Click(OkButtonLocator);
            Selenium.WaitForPageToLoad();
            return new NormalPage<T>(Selenium);
        }

        public void OkWaitClosed()
        {
            Selenium.Click(OkButtonLocator);
            Selenium.WaitElementDisapear(PopupVisibleLocator);
        }

        public static bool IsPopupVisible(ISelenium selenium, string prefix)
        {
            return selenium.IsElementPresent("jq=#{0}:visible".Formato("_".CombineIfNotEmpty(prefix, "Temp")));
        }

        public static bool IsChooser(ISelenium selenium, string prefix)
        {
            return selenium.IsElementPresent("jq=#{0}:visible".Formato("_".CombineIfNotEmpty(prefix, "Temp")) + " .sf-chooser-button");
        }
    }

    public class SelectorPopup : Popup
    {
        public SelectorPopup(ISelenium selenium, string prefix = "New") : base(selenium, prefix) { }

        public void Select(string locator)
        {
            Selenium.Click(locator);
        }

        public void Select(Enum enumValue)
        {
            Selenium.Click(enumValue.ToString());
        }

        public void Select<T>()
        {
            Selenium.Click(TypeLogic.GetCleanName(typeof(T)));
        }
    }

    public class ValueLinePopup : Popup
    {
        public ValueLinePopup(ISelenium selenium, string prefix = "New") : base(selenium, prefix)
        {
        }

        public ValueLineProxy StringValueLine
        {
            get { return new ValueLineProxy(Selenium, Prefix + "_StringValue", null); }
        }
    }

   
    public class PopupControl<T> : Popup, ILineContainer<T>, IEntityButtonContainer where T : ModifiableEntity
    {
        public PropertyRoute Route { get; private set; }

        public PopupControl(ISelenium selenium, string prefix, PropertyRoute route = null)
            : base(selenium, prefix)
        {
            this.Route = route == null || route.IsImplementation(typeof(T)) ? PropertyRoute.Root(typeof(T)) : route;
        }


        public string ButtonLocator(string buttonId)
        {
            return "jq=#{0}_panelPopup #{1}.sf-entity-button".Formato(Prefix, buttonId);
        }

        public bool HasChanges()
        {
            return Selenium.IsElementPresent("jq=#{0}_divMainControl.sf-changed".Formato(Prefix));
        }

        public void CloseDiscardChanges()
        {
            Selenium.Click(CloseButtonLocator);

            Selenium.ConsumeConfirmation();

            Selenium.WaitElementDisapear(PopupVisibleLocator + ":visible");
        }

        public override void Dispose()
        {
            if (Selenium.IsElementPresent(CloseButtonLocator))
            {
                Selenium.Click(CloseButtonLocator);

                string confirmation;
                Selenium.Wait(() =>
                {
                    if (!Selenium.IsElementPresent(PopupVisibleLocator))
                        return true;

                    if (Selenium.IsConfirmationPresent())
                        confirmation = Selenium.GetConfirmation();

                    return false;
                }, ()=>"popup {0} to disapear with or without confirmation");
            }

            if (Selenium.IsConfirmationPresent())
                Selenium.ConsumeConfirmation();
        }

        public RuntimeInfoProxy RuntimeInfo()
        {
            return RuntimeInfoProxy.FromFormValue(Selenium.GetEval("window.$('#{0}_divMainControl').data('runtimeinfo')".Formato(Prefix)));
        }

        public string EntityState()
        {
            if (int.Parse(Selenium.GetEval("window.$('#{0}_sfEntityState').length".Formato(Prefix))) == 0)
                return null;

            return Selenium.GetEval("window.$('#{0}_sfEntityState')[0].value".Formato(Prefix));
        }
    }

    public static class PopupExtensions
    {
        public static P WaitVisible<P>(this P popup) where P : Popup
        {
            popup.Selenium.Wait(() => Popup.IsPopupVisible(popup.Selenium, popup.Prefix), () => "Popup {0} to be visible".Formato(popup.Prefix));

            return popup;
        }
    }
}
