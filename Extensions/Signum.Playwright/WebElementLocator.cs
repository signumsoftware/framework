using Microsoft.Playwright;

namespace Signum.Playwright;

/// <summary>
/// Playwright equivalent of Selenium's WebElementLocator
/// Provides a consistent API while using Playwright's lazy evaluation
/// </summary>
public class WebElementLocator
{
    public ILocator Locator { get; }

    public WebElementLocator(ILocator locator)
    {
        Locator = locator;
    }

    public WebElementLocator(IPage page, string selector)
    {
        Locator = page.Locator(selector);
    }

    #region Find Methods

    /// <summary>
    /// Get first element handle (use sparingly - prefer Locator)
    /// Equivalent to Selenium's Find()
    /// </summary>
    public async Task<IElementHandle> FindAsync()
    {
        return await Locator.ElementHandleAsync() 
            ?? throw new PlaywrightException($"Element not found: {Locator}");
    }

    /// <summary>
    /// Get all element handles (use sparingly - prefer Locator.All())
    /// Equivalent to Selenium's FindElements()
    /// </summary>
    public async Task<IReadOnlyList<IElementHandle>> FindElementsAsync()
    {
        return await Locator.ElementHandlesAsync();
    }

    /// <summary>
    /// Try to find element, returns null if not found
    /// Equivalent to Selenium's TryFind()
    /// </summary>
    public async Task<IElementHandle?> TryFindAsync()
    {
        return await Locator.TryFindAsync();
    }

    #endregion

    #region Presence/Visibility Checks

    /// <summary>
    /// Check if element is present (no wait)
    /// Equivalent to Selenium's IsPresent()
    /// </summary>
    public async Task<bool> IsPresentAsync()
    {
        return await Locator.IsPresentAsync();
    }

    /// <summary>
    /// Check if element is visible (no wait)
    /// Equivalent to Selenium's IsVisible()
    /// </summary>
    public async Task<bool> IsVisibleAsync()
    {
        return await Locator.IsVisibleNowAsync();
    }

    #endregion

    #region Wait Methods

    /// <summary>
    /// Wait for element to be present
    /// Equivalent to Selenium's WaitPresent()
    /// </summary>
    public async Task<WebElementLocator> WaitPresentAsync()
    {
        await Locator.WaitPresentAsync();
        return this;
    }

    /// <summary>
    /// Wait for element to not be present
    /// Equivalent to Selenium's WaitNoPresent()
    /// </summary>
    public async Task WaitNoPresentAsync()
    {
        await Locator.WaitNotPresentAsync();
    }

    /// <summary>
    /// Wait for element to be visible
    /// Equivalent to Selenium's WaitVisible()
    /// </summary>
    public async Task<WebElementLocator> WaitVisibleAsync(bool scrollTo = false)
    {
        await Locator.WaitVisibleAsync(scrollTo: scrollTo);
        return this;
    }

    /// <summary>
    /// Wait for element to not be visible
    /// Equivalent to Selenium's WaitNoVisible()
    /// </summary>
    public async Task WaitNoVisibleAsync()
    {
        await Locator.WaitNotVisibleAsync();
    }

    #endregion

    #region Assert Methods

    /// <summary>
    /// Assert element is present
    /// Equivalent to Selenium's AssertPresent()
    /// </summary>
    public async Task AssertPresentAsync()
    {
        await Locator.AssertPresentAsync();
    }

    /// <summary>
    /// Assert element is not present
    /// Equivalent to Selenium's AssertNotPresent()
    /// </summary>
    public async Task AssertNotPresentAsync()
    {
        await Locator.AssertNotPresentAsync();
    }

    /// <summary>
    /// Assert element is visible
    /// Equivalent to Selenium's AssertVisible()
    /// </summary>
    public async Task AssertVisibleAsync()
    {
        await Locator.AssertVisibleAsync();
    }

    /// <summary>
    /// Assert element is not visible
    /// Equivalent to Selenium's AssertNotVisible()
    /// </summary>
    public async Task AssertNotVisibleAsync()
    {
        await Locator.AssertNotVisibleAsync();
    }

    #endregion

    #region Combine/Chain Methods

    /// <summary>
    /// Combine with CSS selector suffix (chain locators)
    /// Equivalent to Selenium's CombineCss()
    /// </summary>
    public WebElementLocator CombineCss(string cssSelectorSuffix)
    {
        return new WebElementLocator(Locator.Locator(cssSelectorSuffix));
    }

    /// <summary>
    /// Create nested locator
    /// </summary>
    public WebElementLocator NestedLocator(string selector)
    {
        return new WebElementLocator(this.Locator.Locator(selector));
    }

    #endregion

    #region Interaction Methods

    /// <summary>
    /// Click element safely
    /// </summary>
    public async Task ClickAsync()
    {
        await Locator.SafeClickAsync();
    }

    /// <summary>
    /// Fill input
    /// </summary>
    public async Task FillAsync(string text)
    {
        await Locator.SafeFillAsync(text);
    }

    /// <summary>
    /// Get text content
    /// </summary>
    public async Task<string?> TextContentAsync()
    {
        return await Locator.TextContentAsync();
    }

    /// <summary>
    /// Get input value
    /// </summary>
    public async Task<string> ValueAsync()
    {
        return await Locator.ValueAsync();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get the underlying ILocator
    /// </summary>
    public ILocator GetLocator()
    {
        return Locator;
    }

    #endregion
}

/// <summary>
/// Extension methods to create WebElementLocator
/// </summary>
public static class WebElementLocatorExtensions
{
    /// <summary>
    /// Create wrapper from page and selector
    /// Equivalent to Selenium's WithLocator
    /// </summary>
    public static WebElementLocator WithLocator(this IPage page, string selector)
    {
        return new WebElementLocator(page, selector);
    }

    /// <summary>
    /// Wrap existing locator
    /// </summary>
    public static WebElementLocator Wrap(this ILocator locator)
    {
        return new WebElementLocator(locator);
    }
}
