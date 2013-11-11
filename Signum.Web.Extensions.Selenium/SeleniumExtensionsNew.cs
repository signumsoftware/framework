using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Selenium;
using Signum.Utilities;

namespace Signum.Web.Selenium
{
    public static class SeleniumExtensionsNew
    {
        public static void AssertElementPresent(this ISelenium selenium, string locator)
        {
            if (!selenium.IsElementPresent(locator))
                throw new InvalidOperationException("{0} not found".Formato(locator));
        }

        public static void AssertElementNotPresent(this ISelenium selenium, string locator)
        {
            if (selenium.IsElementPresent(locator))
                throw new InvalidOperationException("{0} is found".Formato(locator));
        }

        public static void SetChecked(this ISelenium selenium, string locator, bool isChecked)
        {
            if (selenium.IsChecked(locator) == isChecked)
                return;

            if (isChecked)
                selenium.Check(locator);
            else
                selenium.Uncheck(locator); 
        }

        public static void ConsumeConfirmation(this ISelenium selenium)
        {
            selenium.Wait(() => selenium.IsConfirmationPresent(), () => "confirmation present");
            selenium.GetConfirmation();
        }


        public static void ConsumeAlert(this ISelenium selenium)
        {
            selenium.Wait(() => selenium.IsAlertPresent(), () => "alert present");
            selenium.GetAlert();
        }
    }
}
