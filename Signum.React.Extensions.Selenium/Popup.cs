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

namespace Signum.React.Selenium
{
    public class Popup : IDisposable
    {
        public RemoteWebDriver Selenium { get; private set; }

        public IWebElement Element { get; private set; }

        public Popup(RemoteWebDriver selenium, IWebElement element)
        {
            this.Selenium = selenium;
            this.Element = element;

            this.WaitVisible();
        }

        private By BackdropLocator
        {
            get
            {
                return By.XPath("//*[@id='" + PopupId(null) + "']/following-sibling::div[@class='modal-backdrop fade in']");
            }
        }

        public By PopupLocator
        {
            get { return By.CssSelector("#{0}_panelPopup");  }
        }

        public IWebElement CloseButtonElement
        {
            get { return this.Selenium.NotImplemented(); /* By.CssSelector("#{0}_panelPopup button.close, #{0}_panelPopup #{0}_btnCancel".FormatWith(Prefix)); */ }
        }

        public bool AvoidClose { get; set; }

        public virtual void Dispose()
        {
            if (!Selenium.IsAlertPresent())
                if (!AvoidClose)
                {
                    try
                    {


                        var button = this.CloseButtonElement;
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
            this.CloseButtonElement.Click();
        }

        public Action<bool> Disposing;

        public IWebElement OkButtonElement
        {
            get { return this.Selenium.FindElement(this.OkButtonLocator); }
        }

        public By OkButtonLocator
        {
            get { throw new NotImplementedException(); /* By.CssSelector("#{0}_btnOk".FormatWith(Prefix));*/ }
        }

        public void OkWaitSubmit()
        {
            this.OkButtonElement.Click();

            Selenium.WaitElementNotPresent(OkButtonLocator);
            this.Disposing = null;
        }

        public NormalPage<T> OkWaitNormalPage<T>() where T : Entity
        {
            this.OkButtonElement.Click();
            this.Disposing = null;
            return new NormalPage<T>(Selenium).WaitLoaded();
        }

        public PopupControl<T> OkWaitPopupControl<T>(IWebElement element = null) where T : Entity
        {
            if (element == null)
                this.AvoidClose = true;
            this.OkButtonElement.Click();
            var disposing = this.Disposing;
            this.Disposing = null;
            return new PopupControl<T>(Selenium, element) { Disposing = disposing };
        }

        public bool OkPressed;
        public void OkWaitClosed()
        {
            this.Selenium.FindElement(OkButtonLocator).Click();
            Selenium.WaitElementNotVisible(PopupLocator);
            this.OkPressed = true;
        }

        public static bool IsPopupVisible(RemoteWebDriver selenium, IWebElement element)
        {
            throw new NotImplementedException();
            //return selenium.IsElementVisible(By.CssSelector("#{0}.modal.fade.in".FormatWith(PopupId(prefix))));
        }

        public static string PopupId(IWebElement element)
        {
            throw new NotImplementedException();
            //return "_".CombineIfNotEmpty(prefix, "panelPopup");
        }


        public void WaitNotVisible()
        {
            Selenium.WaitElementNotVisible(PopupLocator);
        }
    }

    public class ChooserPopup : Popup
    {
        public ChooserPopup(RemoteWebDriver selenium, IWebElement element = null) : base(selenium, element) { }

        public void Choose(string value)
        {
            ChooseButton(Selenium, this.Element, value);
        }

        public void Choose(Enum enumValue)
        {
            ChooseButton(Selenium, this.Element, enumValue.ToString());
        }

        public void Choose<T>()
        {
            ChooseButton(Selenium, this.Element, TypeLogic.GetCleanName(typeof(T)));
        }

        public static bool IsChooser(RemoteWebDriver selenium, IWebElement element)
        {
            throw new NotImplementedException();
            //return selenium.IsElementVisible(By.CssSelector("#{0}".FormatWith(PopupId(prefix)) + " .sf-chooser-button"));
        }

        public static void ChooseButton(RemoteWebDriver selenium, IWebElement element, Type type)
        {
            ChooseButton(selenium, element, TypeLogic.GetCleanName(type));
        }

        public static void ChooseButton(RemoteWebDriver selenium, IWebElement element, string value)
        {

            throw new NotImplementedException();
            //selenium.FindElement(By.CssSelector("#" + Popup.PopupId(prefix) + " button[data-value='" + value + "']")).Click();
            //selenium.Wait(() => !IsChooser(selenium, prefix));
        }
    }

    public class ValueLinePopup : Popup
    {
        public ValueLinePopup(RemoteWebDriver selenium, IWebElement element) : base(selenium, element)
        {
        }

        public ValueLineProxy ValueLine
        {
            get { return new ValueLineProxy(Selenium, this.Element.GetDriver().NotImplemented(), null); }
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

        public PopupControl(RemoteWebDriver selenium, IWebElement element, PropertyRoute route = null)
            : base(selenium, element)
        {
            this.Route = route == null || route.IsImplementation(typeof(T)) ? PropertyRoute.Root(typeof(T)) : route;
        }

        public IWebElement ContainerElement()
        {
            throw new NotImplementedException();
            //return By.CssSelector("#{0}_panelPopup".FormatWith(Prefix));
        }

        public string GetTitle()
        {
            throw new InvalidOperationException();
            //return Selenium.FindElement(ContainerLocator().CombineCss(" span.sf-entity-title")).Text;
        }

        public void CloseDiscardChanges()
        {
            this.CloseButtonElement.Click();

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

                    if (this.CloseButtonElement.Displayed)
                    {
                        try
                        {
                            this.CloseButtonElement.Click();
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
                }, () => "popup {0} to disapear with or without confirmation".FormatWith());
            }

            if (Disposing != null)
                Disposing(this.OkPressed);
        }

        public EntityInfoProxy EntityInfo()
        {
            throw new InvalidOperationException();
            //return EntityInfoProxy.FromFormValue((string)Selenium.ExecuteScript("return $('#{0}_divMainControl').data('EntityInfo')".FormatWith(Prefix)));
        }

        public bool HasId()
        {
            return this.EntityInfo().IdOrNull.HasValue;
        }
    }


    public static class PopupExtensions
    {
        public static P WaitVisible<P>(this P popup) where P : Popup
        {
            popup.Selenium.Wait(() => Popup.IsPopupVisible(popup.Selenium, popup.Element), () => "Popup {0} to be visible".FormatWith(popup.Element));

            return popup;
        }
    }
}
