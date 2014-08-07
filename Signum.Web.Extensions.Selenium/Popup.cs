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

            this.WaitVisible();
        }

        public string PopupVisibleLocator
        {
            get { return "jq=#{0}_panelPopup:visible".Formato(Prefix); }
        }

        public string CloseButtonLocator
        {
            get { return "jq=#{0}_panelPopup:visible button.close, #{0}_panelPopup:visible #{0}_btnCancel".Formato(Prefix); }
        }

        public bool AvoidClose { get; set; }

        public virtual void Dispose()
        {
            if (!AvoidClose)
            {
                if (Selenium.IsElementPresent(CloseButtonLocator))
                    Selenium.Click(CloseButtonLocator);

                Selenium.WaitElementDisapear(PopupVisibleLocator);
            }

            if (Disposing != null)
                Disposing(OkPressed);
        }

        public void Close()
        {
            Selenium.Click(CloseButtonLocator);
        }

        public Action<bool> Disposing;

        public string OkButtonLocator
        {
            get { return "jq=#{0}_btnOk".Formato(Prefix); }
        }

        public void OkWaitSubmit()
        {
            Selenium.Click(OkButtonLocator);
            Selenium.WaitForPageToLoad();
            this.Disposing = null;
        }

        public NormalPage<T> OkWaitNormalPage<T>() where T : IdentifiableEntity
        {
            Selenium.Click(OkButtonLocator);
            Selenium.WaitForPageToLoad();
            this.Disposing = null;
            return new NormalPage<T>(Selenium);
        }

        public PopupControl<T> OkWaitPopupControl<T>(string prefix = null) where T : IdentifiableEntity
        {
            if (prefix == null)
                this.AvoidClose = true;
            Selenium.Click(OkButtonLocator);
            var disposing = this.Disposing;
            this.Disposing = null;
            return new PopupControl<T>(Selenium, prefix ?? Prefix) { Disposing = disposing };
        }

        public bool OkPressed; 
        public void OkWaitClosed()
        {
            Selenium.Click(OkButtonLocator);
            Selenium.WaitElementDisapear(PopupVisibleLocator);
            this.OkPressed = true;
        }

        public static bool IsPopupVisible(ISelenium selenium, string prefix)
        {
            return selenium.IsElementPresent("jq=#{0}.modal.fade.in:visible".Formato(PopupId(prefix)));
        }

        public static string PopupId(string prefix)
        {
            return "_".CombineIfNotEmpty(prefix, "panelPopup");
        }

    
    }

    public class ChooserPopup : Popup
    {
        public ChooserPopup(ISelenium selenium, string prefix = "New") : base(selenium, prefix) { }

        public void Choose(string value)
        {
            ChooseButton(Selenium, Prefix, value);
        }

        public void Choose(Enum enumValue)
        {
            ChooseButton(Selenium, Prefix, enumValue.ToString());
        }

        public void Choose<T>()
        {
            ChooseButton(Selenium, Prefix, TypeLogic.GetCleanName(typeof(T)));
        }

        public static bool IsChooser(ISelenium selenium, string prefix)
        {
            return selenium.IsElementPresent("jq=#{0}:visible".Formato(PopupId(prefix)) + " .sf-chooser-button");
        }

        public static void ChooseButton(ISelenium Selenium, string prefix, Type type)
        {
            ChooseButton(Selenium, prefix, TypeLogic.GetCleanName(type));
        }

        public static void ChooseButton(ISelenium Selenium, string prefix, string value)
        {
            Selenium.Click("jq=#" + Popup.PopupId(prefix) + " button[data-value='" + value + "']");
            Selenium.Wait(() => !IsChooser(Selenium, prefix));
        }
    }

    public class ValueLinePopup : Popup
    {
        public ValueLinePopup(ISelenium selenium, string prefix = "New") : base(selenium, prefix)
        {
        }

        public ValueLineProxy ValueLine
        {
            get { return new ValueLineProxy(Selenium, Prefix + "_value", null); }
        }

        public string StringValue
        {
            get { return ValueLine.StringValue; }
            set { ValueLine.StringValue = value; }
        }

        public T GetValue<T>()
        {
            return ValueLine.GetValue<T>(); 
        }

        public void SetValue(object value, string format = null)
        {
            ValueLine.SetValue(value, format);
        }
    }


    public class PopupControl<T> : Popup, ILineContainer<T>, IEntityButtonContainer<T> where T : ModifiableEntity
    {
        public PropertyRoute Route { get; private set; }

        public PopupControl(ISelenium selenium, string prefix, PropertyRoute route = null)
            : base(selenium, prefix)
        {
            this.Route = route == null || route.IsImplementation(typeof(T)) ? PropertyRoute.Root(typeof(T)) : route;
        }

        public string ContainerLocator()
        {
            return "jq=#{0}_panelPopup".Formato(Prefix);
        }

        public void CloseDiscardChanges()
        {
            Selenium.Click(CloseButtonLocator);

            Selenium.ConsumeConfirmation();

            Selenium.WaitElementDisapear(PopupVisibleLocator);
        }

        public override void Dispose()
        {
            if (!AvoidClose)
            {
                string confirmation;
                Selenium.Wait(() =>
                {
                    if (!Selenium.IsElementPresent(PopupVisibleLocator))
                        return true;

                    if (Selenium.IsElementPresent(CloseButtonLocator))
                    {
                        try
                        {
                            Selenium.Click(CloseButtonLocator);
                        }
                        catch (SeleniumException e)
                        {
                            if (!e.Message.Contains("not found"))
                                throw;
                        }
                    }

                    if (Selenium.IsConfirmationPresent())
                        confirmation = Selenium.GetConfirmation();

                    return false;
                }, () => "popup {0} to disapear with or without confirmation".Formato(Prefix));
            }

            if (Disposing != null)
                Disposing(this.OkPressed);
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

        public bool HasId()
        {
            return this.RuntimeInfo().IdOrNull.HasValue;
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
