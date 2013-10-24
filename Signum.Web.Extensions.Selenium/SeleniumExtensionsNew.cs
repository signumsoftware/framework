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
    }
}
