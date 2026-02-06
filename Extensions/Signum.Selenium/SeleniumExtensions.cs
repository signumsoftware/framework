using System.Diagnostics;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace Signum.Selenium;

public static class SeleniumExtensions
{
    public static TimeSpan ScrollToTimeout = TimeSpan.FromMilliseconds(500);
    public static TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(20 * 1000);
    public static TimeSpan ThrowExceptionForDeveloperAfter = TimeSpan.FromMilliseconds(5 * 1000);
    public static TimeSpan DefaultPoolingInterval = TimeSpan.FromMilliseconds(200);

    public static T Wait<T>(this WebDriver selenium, Func<T> condition, Func<string>? actionDescription = null, TimeSpan? timeout = null)
    {
        try
        {
            var wait = new DefaultWait<string>("")
            {
                Timeout = timeout ?? DefaultTimeout,
                PollingInterval = DefaultPoolingInterval
            };

            wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(NoAlertPresentException), typeof(StaleElementReferenceException));
            var throwExceptionAfter = Debugger.IsAttached ? DateTime.Now.Add(ThrowExceptionForDeveloperAfter) : (DateTime?)null;
            return wait.Until(str =>
            {
                var result = condition();

                if ((result == null || result.Equals(false)) && throwExceptionAfter < DateTime.Now)
                {
                    try
                    {
                        throw new WaitTakingTooLongException("Hey Developer! looks like this condition is taking too long");
                    }
                    catch (WaitTakingTooLongException)
                    {
                        throwExceptionAfter = null;
                        return result;
                    }
                }

                return result;
            });
        }
        catch (WebDriverTimeoutException ex)
        {
            var errorModal = selenium.GetErrorModal();

            if (errorModal != null)
            {
                throw new WebDriverTimeoutException(
                    $"ErrorModal '{errorModal.TitleText}' ..waiting for {(actionDescription?.Invoke() ?? "visual condition")}\n" +
                    errorModal.BodyText
                );
            }

            throw new WebDriverTimeoutException(ex.Message + ": waiting for {0} in page {1}({2})".FormatWith(
                actionDescription == null ? "visual condition" : actionDescription(),
                selenium.Title,
                selenium.Url));
        }
    }


    public class WaitTakingTooLongException : Exception
    {
        public WaitTakingTooLongException() { }
        public WaitTakingTooLongException(string message) : base(message) { }
        public WaitTakingTooLongException(string message, Exception inner) : base(message, inner) { }
    }

    public static string WaitNewWindow(this WebDriver selenium, Action action)
    {
        var old = selenium.WindowHandles.ToHashSet();
        action();
        return selenium.Wait(() => selenium.WindowHandles.SingleOrDefaultEx(a => !old.Contains(a)))!;
    }

    public static void WaitEquals<T>(this WebDriver selenium, T expectedValue, Func<T> value, TimeSpan? timeout = null)
    {
        T lastValue = default(T)!;
        selenium.Wait(() => EqualityComparer<T>.Default.Equals(lastValue = value(), expectedValue), () => "expression to be " + expectedValue + " but is " + lastValue, timeout);
    }

    public static IWebElement? TryFindElement(this ISearchContext searchContext, By locator)
    {
        return searchContext.FindElements(locator).FirstOrDefault();
    }

    public static IWebElement WaitElementPresent(this ISearchContext searchContext, By locator, Func<string>? actionDescription = null, TimeSpan? timeout = null)
    {
        var driver = searchContext.GetDriver();
        return driver.Wait(() => searchContext.FindElements(locator).FirstOrDefault(),
            actionDescription ?? (Func<string>)(() => "{0} to be present".FormatWith(locator)), timeout)!;
    }

    public static void AssertElementPresent(this ISearchContext searchContext, By locator)
    {
        if (!searchContext.IsElementPresent(locator))
            throw new InvalidOperationException("{0} not found".FormatWith(locator));
    }

    public static bool IsElementPresent(this ISearchContext searchContext, By locator)
    {
        return searchContext.FindElements(locator).Any();
    }

    public static void WaitElementNotPresent(this ISearchContext searchContext, By locator, Func<string>? actionDescription = null, TimeSpan? timeout = null)
    {
        var driver = searchContext.GetDriver();
        driver.Wait(() => !searchContext.IsElementPresent(locator),
            actionDescription ?? (Func<string>)(() => "{0} to be not present".FormatWith(locator)), timeout);
    }

    public static void AssertElementNotPresent(this ISearchContext searchContext, By locator)
    {
        if (searchContext.IsElementPresent(locator))
            throw new InvalidOperationException("{0} is found".FormatWith(locator));
    }

    //[DebuggerHidden]
    public static bool IsStale(this IWebElement element)
    {
        try
        {
            // Calling any method forces a staleness check
            return element == null || !element.Enabled;
        }
        catch (StaleElementReferenceException)
        {
            return true;
        }
    }

    public static IWebElement WaitElementVisible(this ISearchContext searchContext, By locator, Func<string>? actionDescription = null, TimeSpan? timeout = null)
    {
        var driver = searchContext.GetDriver();
        return driver.Wait(() => searchContext.FindElements(locator).FirstOrDefault(a => a.Displayed),
            actionDescription ?? (Func<string>)(() => "{0} to be visible".FormatWith(locator)), timeout)!;
    }

    public static void AssertElementVisible(this ISearchContext searchContext, By locator)
    {
        var elements = searchContext.FindElements(locator);

        if (!elements.Any())
            throw new InvalidOperationException("{0} not found".FormatWith(locator));

        if (!elements.First().Displayed)
            throw new InvalidOperationException("{0} found but not visible".FormatWith(locator));
    }

    public static bool IsElementVisible(this ISearchContext searchContext, By locator)
    {
        try
        {
            var elements = searchContext.FindElements(locator);
            return elements.Count != 0 && elements.First().Displayed;
        }
        catch (StaleElementReferenceException)
        {
            return false;
        }
    }

    public static void WaitElementNotVisible(this ISearchContext searchContext, By locator, Func<string>? actionDescription = null, TimeSpan? timeout = null)
    {
        var driver = searchContext.GetDriver();
        driver.Wait(() => !searchContext.IsElementVisible(locator),
            actionDescription ?? (Func<string>)(() => "{0} to be not visible".FormatWith(locator)), timeout);
    }

    public static void AssertElementNotVisible(this ISearchContext searchContext, By locator)
    {
        if (searchContext.IsElementVisible(locator))
            throw new InvalidOperationException("{0} is visible".FormatWith(locator));
    }

    public static WebDriver GetDriver(this ISearchContext searchContext)
    {
        if (searchContext is WebDriver driver)
            return driver;

        if (searchContext is IWrapsDriver wrapsDriver)
            return (WebDriver)wrapsDriver.WrappedDriver;

        throw new InvalidOperationException($"Cannot get WebDriver from {searchContext.GetType().Name}");
    }

    public static void SetChecked(this IWebElement element, bool isChecked)
    {
        if (element.Selected == isChecked)
            return;

        element.SafeClick();

        element.GetDriver().Wait(() => element.Selected == isChecked, () => "Set Checkbox to " + isChecked);
    }

    //[DebuggerStepThrough]
    //public static bool IsAlertPresent(this WebDriver selenium)
    //{
    //    try
    //    {
    //        selenium.SwitchTo().Alert();
    //        return true;
    //    }
    //    catch (NoAlertPresentException)
    //    {
    //        return false;
    //    }
    //}

    public static void ConsumeAlert(this WebDriver selenium)
    {
        var alertPresent = selenium.Wait(() => selenium.GetMessageModal());

        var alert = selenium.Wait(() => selenium.SwitchTo().Alert());

        alert.Accept();
    }

    public static string CssSelector(this By by)
    {
        string str = by.ToString();

        var after = str.After(": ");
        switch (str.Before(":"))
        {
            case "By.CssSelector": return after;
            case "By.Id": return "#" + after;
            case "By.Name": return "[name=" + after + "]";
            case "By.ClassName[Contains]": return "." + after;
            default: throw new InvalidOperationException("Impossible to combine: " + str);
        }
    }

    public static By CombineCss(this By by, string cssSelectorSuffix)
    {
        return By.CssSelector(by.CssSelector() + cssSelectorSuffix);
    }

    public static bool ContainsText(this IWebElement element, string text)
    {
        return element.Text.Contains(text) || element.FindElements(By.XPath("descendant::*[contains(text(), '" + text + "')]")).Any();
    }

    public static SelectElement SelectElement(this IWebElement element)
    {
        return new SelectElement(element);
    }

    //  blogs.rahulrpandya.in/understanding-the-deprecation-of-getattribute-in-selenium-26f490598d20
    public static string GetDomAttributeOrThrow(this IWebElement element, string attributeName)
    {
        return element.GetDomAttribute(attributeName) ??  
            throw new InvalidOperationException($"Attribute '{attributeName}' was not found on element: {element.TagName}.");
    }

    public static string GetDomPropertyOrThrow(this IWebElement element, string propertyName)
    {
        return element.GetDomProperty(propertyName) ??  
            throw new InvalidOperationException($"Property '{propertyName}' was not found on element: {element.TagName}.");
    }

    public static string? GetID(this IWebElement element)
    {
        return element.GetDomProperty("id");
    }

    public static IEnumerable<string> GetClasses(this IWebElement element)
    {
        return (element.GetDomAttribute("class") ?? "").Split(' ');
    }

    public static bool HasClass(this IWebElement element, string className)
    {
        return element.GetClasses().Contains(className);
    }

    public static bool HasClass(this IWebElement element, params string[] classNames)
    {
        var classes = element.GetClasses();
        return classNames.All(cn => classes.Contains(cn));
    }

    public static IWebElement GetParent(this IWebElement e)
    {
        return e.FindElement(By.XPath("./.."));
    }

    public static IWebElement GetAscendant(this IWebElement e, Func<IWebElement, bool> predicate)
    {
        return e.Follow(a => a.GetParent()).FirstEx(predicate);
    }

    public static IWebElement? TryGetAscendant(this IWebElement e, Func<IWebElement, bool> predicate)
    {
        return e.Follow(a => a.GetParent()).FirstOrDefault(predicate);
    }

    public static void SelectByPredicate(this SelectElement element, Func<IWebElement, bool> predicate)
    {
        element.Options.SingleEx(predicate).Click();
    }

    public static IWebElement CaptureOnClick(this IWebElement button)
    {
        return button.GetDriver().CapturePopup(() => button.SafeClick());
    }

    public static List<IWebElement> CaptureManyOnClick(this IWebElement button)
    {
        return button.GetDriver().CaptureManyPopup(() => button.SafeClick());
    }

    public static IWebElement CaptureOnDoubleClick(this IWebElement button)
    {
        return button.GetDriver().CapturePopup(() => button.DoubleClick());
    }

    public static IWebElement CapturePopup(this WebDriver selenium, Action clickToOpen)
    {
        var body = selenium.FindElement(By.TagName("body"));
        var oldDialogs = body.FindElements(By.CssSelector("div.modal.fade.show"));
        clickToOpen();
        var result = selenium.Wait(() =>
        {
            var newDialogs = body.FindElements(By.CssSelector("div.modal.fade.show"));

            var newTop = newDialogs.SingleOrDefaultEx(a => !oldDialogs.Contains(a));

            if (newTop == null)
                return null;

            return newTop;
        })!;

        return result;
    }

    public static List<IWebElement> CaptureManyPopup(this WebDriver selenium, Action clickToOpen)
    {
        var body = selenium.FindElement(By.TagName("body"));
        var oldDialogs = body.FindElements(By.CssSelector("div.modal.fade.show"));
        clickToOpen();
        var result = selenium.Wait(() =>
        {
            Thread.Sleep(300);
            var newDialogs = body.FindElements(By.CssSelector("div.modal.fade.show"));

            var newTop = newDialogs.Where(a => !oldDialogs.Contains(a)).ToList();

            if (newTop == null)
                return null;

            return newTop;
        })!;

        return result;
    }
    public static void ContextClick(this IWebElement element, int offsetX = 2, int offsetY = 2)
    {
        Actions builder = new Actions(element.GetDriver());
        builder.MoveToElement(element, offsetX, offsetY).ContextClick().Build().Perform();
    }

    public static void DoubleClick(this IWebElement element, int offsetX = 2, int offsetY = 2)
    {
        Actions builder = new Actions(element.GetDriver());
        builder.MoveToElement(element, offsetX, offsetY).DoubleClick().Build().Perform();
    }

    public static void SafeSendKeys(this IWebElement element, string? text)
    {
        element.ScrollTo();
        new Actions(element.GetDriver()).MoveToElement(element).Perform();
        var length = 0;
        while((length = element.GetDomPropertyOrThrow("value").Length) > 0)
        {
            for (int i = 0; i < length; i++)
                element.SendKeys(Keys.Backspace);
        }
           

        if (text.HasText())
            element.SendKeys(text);

        Thread.Sleep(0);
        element.GetDriver().Wait(() => element.GetDomProperty("value") == (text ?? ""));
    }

    public static string Value(this IWebElement e) => e.GetDomPropertyOrThrow("value");

    public static void ButtonClick(this IWebElement button)
    {
        if (!button.Enabled)
            throw new InvalidOperationException("Button is not enabled");

        if (!button.Displayed)
        {
            var menu = button.FindElement(By.XPath("ancestor::*[contains(@class,'dropdown-menu')]"));
            var superButton = menu.GetParent().FindElement(By.CssSelector("a[data-toggle='dropdown']"));
            superButton.Click();
        }

        try
        {
            button.ScrollTo();
            button.Click();
        }
        catch (InvalidOperationException e)
        {
            if (e.Message.Contains("Element is not clickable")) //Scrolling problems
                button.Click();

        }
    }

    public static void SafeClick(this IWebElement element)
    {
        element.ScrollTo();
        element.Click();
    }

    public static IWebElement ScrollTo(this IWebElement element)
    {
        IJavaScriptExecutor js = (IJavaScriptExecutor)element.GetDriver();
        js.ExecuteScript("arguments[0].scrollIntoView({behavior: 'auto', block: 'center', inline: 'center'});", element);
        Thread.Sleep(ScrollToTimeout);

        return element;
    }

    public static void LoseFocus(this IWebElement element)
    {
        IJavaScriptExecutor js = (IJavaScriptExecutor)element.GetDriver();
        js.ExecuteScript("arguments[0].focus(); arguments[0].blur(); return true", element);
    }

    public static void Retry<T>(this WebDriver driver, int times, Action action) where T : Exception
    {
        for (int i = 0; i < times; i++)
        {
            try
            {
                action();
                return;
            }
            catch (T)
            {
                if (i >= times - 1)
                    throw;
            }
        }
    }

    public static WebElementLocator WithLocator(this ISearchContext searchContext, By locator)
    {
        return new WebElementLocator(searchContext, locator);
    }

    public static bool IsDomDisabled(this IWebElement element)
    {
        var disabled = element.GetDomProperty("disabled")?.ToLower() ?? element.GetDomAttribute("disabled")?.ToLower();
        return disabled != null && disabled == "true";
    }

    public static bool IsDomReadonly(this IWebElement element)
    {
        var _readonly = element.GetDomProperty("readonly")?.ToLower() ?? element.GetDomAttribute("readonly")?.ToLower();
        return _readonly != null && _readonly == "true";
    }
}
