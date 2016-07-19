using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Web.Selenium
{
    public class Popup : IDisposable
    {
        public RemoteWebDriver Selenium { get; private set; }

        public string Prefix { get; private set; }

        public Popup(RemoteWebDriver selenium, string prefix)
        {
            this.Selenium = selenium;
            this.Prefix = prefix;

            this.WaitVisible();
        }

        private By BackdropLocator
        {
            get
            {
                return By.XPath("//*[@id='" + PopupId(this.Prefix) + "']/following-sibling::div[@class='modal-backdrop fade in']");
            }
        }

        public By PopupLocator
        {
            get { return By.CssSelector("#{0}_panelPopup".FormatWith(Prefix)); }
        }

        public By CloseButtonLocator
        {
            get { return By.CssSelector("#{0}_panelPopup button.close, #{0}_panelPopup #{0}_btnCancel".FormatWith(Prefix)); }
        }

        public bool AvoidClose { get; set; }

        public virtual void Dispose()
        {
            if (!Selenium.IsAlertPresent())
                if (!AvoidClose)
                {
                    try
                    {


                        var button = Selenium.FindElements(CloseButtonLocator).SingleOrDefaultEx();
                        if (button != null && button.Displayed)
                            button.Click();

                    }
                    catch (ElementNotVisibleException)
                    {
                    }
                    catch (StaleElementReferenceException)
                    {
                    }

                    Selenium.WaitElementNotVisible(BackdropLocator);
                    Selenium.WaitElementNotVisible(PopupLocator);
                }

            if (Disposing != null)
                Disposing(OkPressed);
        }

        public void Close()
        {
            Selenium.FindElement(CloseButtonLocator).Click();
        }

        public Action<bool> Disposing;

        public By OkButtonLocator
        {
            get { return By.CssSelector("#{0}_btnOk".FormatWith(Prefix)); }
        }

        public void OkWaitSubmit()
        {
            Selenium.FindElement(OkButtonLocator).Click();
            Selenium.WaitElementNotPresent(OkButtonLocator);
            this.Disposing = null;
        }

        public NormalPage<T> OkWaitNormalPage<T>() where T : Entity
        {
            Selenium.FindElement(OkButtonLocator).Click();
            this.Disposing = null;
            return new NormalPage<T>(Selenium).WaitLoaded();
        }

        public PopupControl<T> OkWaitPopupControl<T>(string prefix = null) where T : Entity
        {
            if (prefix == null)
                this.AvoidClose = true;
            Selenium.FindElement(OkButtonLocator).Click();
            var disposing = this.Disposing;
            this.Disposing = null;
            return new PopupControl<T>(Selenium, prefix ?? Prefix) { Disposing = disposing };
        }

        public bool OkPressed;
        public void OkWaitClosed()
        {
            Selenium.FindElement(OkButtonLocator).Click();
            Selenium.WaitElementNotVisible(PopupLocator);
            this.OkPressed = true;
        }

        public static bool IsPopupVisible(RemoteWebDriver selenium, string prefix)
        {
            return selenium.IsElementVisible(By.CssSelector("#{0}.modal.fade.in".FormatWith(PopupId(prefix))));
        }

        public static string PopupId(string prefix)
        {
            return "_".CombineIfNotEmpty(prefix, "panelPopup");
        }


        public void WaitNotVisible()
        {
            Selenium.WaitElementNotVisible(PopupLocator);
        }
    }

    public class ChooserPopup : Popup
    {
        public ChooserPopup(RemoteWebDriver selenium, string prefix = "New") : base(selenium, prefix) { }

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

        public static bool IsChooser(RemoteWebDriver selenium, string prefix)
        {
            return selenium.IsElementVisible(By.CssSelector("#{0}".FormatWith(PopupId(prefix)) + " .sf-chooser-button"));
        }

        public static void ChooseButton(RemoteWebDriver Selenium, string prefix, Type type)
        {
            ChooseButton(Selenium, prefix, TypeLogic.GetCleanName(type));
        }

        public static void ChooseButton(RemoteWebDriver Selenium, string prefix, string value)
        {
            Selenium.FindElement(By.CssSelector("#" + Popup.PopupId(prefix) + " button[data-value='" + value + "']")).Click();
            Selenium.Wait(() => !IsChooser(Selenium, prefix));
        }
    }

    public class ValueLinePopup : Popup
    {
        public ValueLinePopup(RemoteWebDriver selenium, string prefix = "New") : base(selenium, prefix)
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

        public PopupControl(RemoteWebDriver selenium, string prefix, PropertyRoute route = null)
            : base(selenium, prefix)
        {
            this.Route = route == null || route.IsImplementation(typeof(T)) ? PropertyRoute.Root(typeof(T)) : route;
        }

        public By ContainerLocator()
        {
            return By.CssSelector("#{0}_panelPopup".FormatWith(Prefix));
        }

        public string GetTitle()
        {
            return Selenium.FindElement(ContainerLocator().CombineCss(" span.sf-entity-title")).Text;
        }

        public void CloseDiscardChanges()
        {
            Selenium.FindElement(CloseButtonLocator).Click();

            Selenium.ConsumeAlert();

            Selenium.WaitElementNotVisible(PopupLocator);
        }

        public override void Dispose()
        {
            if (!AvoidClose)
            {
                string confirmationMessage;
                Selenium.Wait(() =>
                {
                    if (!Selenium.IsElementVisible(PopupLocator))
                        return true;

                    if (Selenium.IsElementVisible(CloseButtonLocator))
                    {
                        try
                        {
                            Selenium.FindElement(CloseButtonLocator).Click();
                        }
                        catch (NoSuchElementException e)
                        {
                            if (!e.Message.Contains("not found"))
                                throw;
                        }
                    }

                    if (Selenium.IsAlertPresent())
                    {
                        var alert = Selenium.SwitchTo().Alert();
                        confirmationMessage = alert.Text;
                        alert.Accept();
                    }

                    return false;
                }, () => "popup {0} to disapear with or without confirmation".FormatWith(Prefix));
            }

            if (Disposing != null)
                Disposing(this.OkPressed);
        }

        public RuntimeInfoProxy RuntimeInfo()
        {
            return RuntimeInfoProxy.FromFormValue((string)Selenium.ExecuteScript("return $('#{0}_divMainControl').data('runtimeinfo')".FormatWith(Prefix)));
        }

        public string EntityState()
        {
            if ((long)Selenium.ExecuteScript("return $('#{0}_sfEntityState').length".FormatWith(Prefix)) == 0)
                return null;

            return (string)Selenium.ExecuteScript("return $('#{0}_sfEntityState')[0].value".FormatWith(Prefix));
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
            popup.Selenium.Wait(() => Popup.IsPopupVisible(popup.Selenium, popup.Prefix), () => "Popup {0} to be visible".FormatWith(popup.Prefix));

            return popup;
        }
    }
}
