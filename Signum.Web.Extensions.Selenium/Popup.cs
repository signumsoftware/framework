using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Selenium;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Web.Selenium
{
    public abstract class Popup : IDisposable
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
            get { return "jq=#{0}Temp:visible".Formato(Prefix); }
        }

        public string CloseButtonLocator
        {
            get { return "jq=.ui-dialog-titlebar-close:visible"; }
        }

        public void Close()
        {
            Selenium.Click(CloseButtonLocator);

            Selenium.WaitAjaxFinished(() => Selenium.IsConfirmationPresent());
        }

        public void Dispose()
        {
            if (Selenium.IsElementPresent(PopupVisibleLocator))
                throw new InvalidOperationException("Close popup {0} before dispossing".Formato(Prefix));
        }


        public static bool IsPopupVisible(ISelenium selenium, string prefix)
        {
            return selenium.IsElementPresent("jq=#{0}Temp:visible".Formato(prefix));
        }

        public static bool IsChooser(ISelenium selenium, string prefix)
        {
            return selenium.IsElementPresent("jq=#{0}Temp:visible".Formato(prefix) + " .sf-chooser-button");
        }
    }

   
    public class PopupControl<T> : Popup, ILineContainer<T>, IEntityButtonContainer where T : ModifiableEntity
    {
        public PropertyRoute Route { get; private set; }

        public PopupControl(ISelenium selenium, string prefix, PropertyRoute route = null)
            : base(selenium, prefix)
        {
            this.Route = route ?? PropertyRoute.Root(typeof(T));
        }

        public string OkButtonLocator
        {
            get { return "jq=#{0}btnOk".Formato(Prefix); }
        }

        public void OkWaitSubmit()
        {
            Selenium.Click(OkButtonLocator);
            Selenium.WaitForPageToLoad(SeleniumExtensions.DefaultPageLoadTimeout);
        }

        public void OkWaitClosed()
        {
            Selenium.Click(OkButtonLocator);
            Selenium.WaitAjaxFinished(() => !Selenium.IsElementPresent(PopupVisibleLocator));
        }

       
    }
}
