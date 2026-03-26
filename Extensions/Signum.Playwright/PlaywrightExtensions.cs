using Microsoft.Playwright;

namespace Signum.Playwright;

/// <summary>
/// Core extension methods for Playwright in Signum Framework
/// Provides Selenium-like patterns with Playwright's advantages
/// </summary>
public static class PlaywrightExtensions
{
    public static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);
    public static TimeSpan DefaultPollingInterval = TimeSpan.FromMilliseconds(200);
    public static TimeSpan ScrollToTimeout = TimeSpan.FromMilliseconds(500);

    #region Existence Checks (No Wait)

    /// <summary>
    /// Check if locator exists without waiting (immediate)
    /// Equivalent to Selenium's IsElementPresent
    /// </summary>
    public static async Task<bool> IsPresentAsync(this ILocator locator)
    {
        try
        {
            return await locator.CountAsync() > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if locator is visible without waiting (immediate)
    /// Equivalent to Selenium's IsElementVisible
    /// </summary>
    public static async Task<bool> IsVisibleNowAsync(this ILocator locator)
    {
        try
        {
            return await locator.IsVisibleAsync(new LocatorIsVisibleOptions {});
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Try to find element, returns null if not found
    /// Equivalent to Selenium's TryFind
    /// </summary>
    public static async Task<IElementHandle?> TryFindAsync(this ILocator locator)
    {
        try
        {
            var count = await locator.CountAsync();
            if (count == 0)
                return null;

            return await locator.ElementHandleAsync(new LocatorElementHandleOptions { Timeout = 0 });
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Wait Methods

    /// <summary>
    /// Wait for locator to be present (attached to DOM)
    /// Equivalent to Selenium's WaitElementPresent
    /// </summary>
    public static async Task<ILocator> WaitPresentAsync(this ILocator locator, float? timeout = null)
    {
        await locator.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = timeout ?? (float)DefaultTimeout.TotalMilliseconds
        });
        return locator;
    }

    /// <summary>
    /// Wait for locator to be visible
    /// Equivalent to Selenium's WaitElementVisible
    /// </summary>
    public static async Task<ILocator> WaitVisibleAsync(this ILocator locator, float? timeout = null, bool scrollTo = false)
    {
        await locator.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = timeout ?? (float)DefaultTimeout.TotalMilliseconds
        });

        if (scrollTo)
        {
            await locator.ScrollIntoViewIfNeededAsync();
            await Task.Delay(ScrollToTimeout);
        }

        return locator;
    }

    /// <summary>
    /// Wait for locator to not be present (detached from DOM)
    /// Equivalent to Selenium's WaitElementNotPresent
    /// </summary>
    public static async Task WaitNotPresentAsync(this ILocator locator, float? timeout = null)
    {
        await locator.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Detached,
            Timeout = timeout ?? (float)DefaultTimeout.TotalMilliseconds
        });
    }

    /// <summary>
    /// Wait for locator to not be visible
    /// Equivalent to Selenium's WaitElementNotVisible
    /// </summary>
    public static async Task WaitNotVisibleAsync(this ILocator locator, float? timeout = null)
    {
        await locator.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden,
            Timeout = timeout ?? (float)DefaultTimeout.TotalMilliseconds
        });
    }

    #endregion

    #region Assert Methods

    /// <summary>
    /// Assert that locator is present
    /// Equivalent to Selenium's AssertElementPresent
    /// </summary>
    public static async Task AssertPresentAsync(this ILocator locator)
    {
        if (!await locator.IsPresentAsync())
            throw new PlaywrightException($"Locator '{locator}' is not present");
    }

    /// <summary>
    /// Assert that locator is not present
    /// Equivalent to Selenium's AssertElementNotPresent
    /// </summary>
    public static async Task AssertNotPresentAsync(this ILocator locator)
    {
        if (await locator.IsPresentAsync())
            throw new PlaywrightException($"Locator '{locator}' is present but should not be");
    }

    /// <summary>
    /// Assert that locator is visible
    /// Equivalent to Selenium's AssertElementVisible
    /// </summary>
    public static async Task AssertVisibleAsync(this ILocator locator)
    {
        if (!await locator.IsVisibleAsync())
            throw new PlaywrightException($"Locator '{locator}' is not visible");
    }

    /// <summary>
    /// Assert that locator is not visible
    /// Equivalent to Selenium's AssertElementNotVisible
    /// </summary>
    public static async Task AssertNotVisibleAsync(this ILocator locator)
    {
        if (await locator.IsVisibleAsync())
            throw new PlaywrightException($"Locator '{locator}' is visible but should not be");
    }

    #endregion

    #region Interaction Methods

    /// <summary>
    /// Click with auto-scroll and retry
    /// Equivalent to Selenium's SafeClick
    /// </summary>
    public static async Task SafeClickAsync(this ILocator locator)
    {
        await locator.ScrollIntoViewIfNeededAsync();
        await locator.ClickAsync();
    }

    /// <summary>
    /// Click button with checks
    /// Equivalent to Selenium's ButtonClick
    /// </summary>
    public static async Task ButtonClickAsync(this ILocator button)
    {
        // Check if enabled
        if (!await button.IsEnabledAsync())
            throw new InvalidOperationException("Button is not enabled");

        await button.ScrollIntoViewIfNeededAsync();
        await button.ClickAsync();
    }

    /// <summary>
    /// Fill input with clear and wait for value
    /// Equivalent to Selenium's SafeSendKeys
    /// </summary>
    public static async Task SafeFillAsync(this ILocator input, string text)
    {
        await input.ScrollIntoViewIfNeededAsync();
        await input.FillAsync(""); // Clear
        await input.FillAsync(text);

        // Wait for value to be set
        await Assertions.Expect(input).ToHaveValueAsync(text);
    }

    /// <summary>
    /// Set checkbox state
    /// Equivalent to Selenium's SetChecked
    /// </summary>
    public static async Task SetCheckedAsync(this ILocator checkbox, bool isChecked)
    {
        var currentState = await checkbox.IsCheckedAsync();
        if (currentState == isChecked)
            return;

        await checkbox.ClickAsync();
        await Assertions.Expect(checkbox).ToBeCheckedAsync(new LocatorAssertionsToBeCheckedOptions
        {
            Checked = isChecked
        });
    }

    /// <summary>
    /// Double click with offset
    /// Equivalent to Selenium's DoubleClick
    /// </summary>
    public static async Task DoubleClickAsync(this ILocator locator, float? offsetX = 2, float? offsetY = 2)
    {
        await locator.DblClickAsync(new LocatorDblClickOptions
        {
            Position = new Position { X = offsetX ?? 2, Y = offsetY ?? 2 }
        });
    }

    /// <summary>
    /// Context/right-click with offset
    /// Equivalent to Selenium's ContextClick
    /// </summary>
    public static async Task ContextClickAsync(this ILocator locator, float? offsetX = 2, float? offsetY = 2)
    {
        await locator.ClickAsync(new LocatorClickOptions
        {
            Button = MouseButton.Right,
            Position = new Position { X = offsetX ?? 2, Y = offsetY ?? 2 }
        });
    }

    #endregion

    #region Attribute/Property Methods

    /// <summary>
    /// Get attribute value or throw
    /// Equivalent to Selenium's GetDomAttributeOrThrow
    /// </summary>
    public static async Task<string> GetAttributeOrThrowAsync(this ILocator locator, string attributeName)
    {
        var value = await locator.GetAttributeAsync(attributeName);
        return value ?? throw new InvalidOperationException($"Attribute '{attributeName}' not found");
    }

    /// <summary>
    /// Get element ID
    /// Equivalent to Selenium's GetID
    /// </summary>
    public static async Task<string?> GetIdAsync(this ILocator locator)
    {
        return await locator.GetAttributeAsync("id");
    }

    /// <summary>
    /// Get CSS classes
    /// Equivalent to Selenium's GetClasses
    /// </summary>
    public static async Task<IEnumerable<string>> GetClassesAsync(this ILocator locator)
    {
        var classAttr = await locator.GetAttributeAsync("class");
        return (classAttr ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Check if locator has specific CSS class
    /// Equivalent to Selenium's HasClass
    /// </summary>
    public static async Task<bool> HasClassAsync(this ILocator locator, string className)
    {
        var classes = await locator.GetClassesAsync();
        return classes.Contains(className);
    }

    public static async Task WaitHasClassAsync(this ILocator locator, string className, bool shouldHave)
    {
        await locator.Page.WaitForFunctionAsync(
            @"([el, cls, shouldHave]) => {
                const hasClass = el.classList.contains(cls);
                return shouldHave == hasClass;
            }",
            new object[]{ await locator.ElementHandleAsync(), className, shouldHave }
        );
    }

    public static async Task WaitAttributeAsync(this ILocator locator, string attributeName, string? expectedValue, string op = "===")
    {
        var elementHandle = await locator.ElementHandleAsync();

        await locator.Page.WaitForFunctionAsync(
            $@"([el, attr, value]) => el.getAttribute(attr) {op} value",
            new object?[] { elementHandle, attributeName, expectedValue }
        );
    }

    public static async Task WaitDisabledAsync(this ILocator locator, bool shouldBeDisabled)
    {
        var elementHandle = await locator.ElementHandleAsync();

        await locator.Page.WaitForFunctionAsync(
            @"([el, disabled]) => el.disabled === disabled",
            new object[] { elementHandle, shouldBeDisabled }
        );
    }

    /// <summary>
    /// Check if locator has all specified CSS classes
    /// Equivalent to Selenium's HasClass (params version)
    /// </summary>
    public static async Task<bool> HasClassAsync(this ILocator locator, params string[] classNames)
    {
        var classes = (await locator.GetClassesAsync()).ToHashSet();
        return classNames.All(cn => classes.Contains(cn));
    }

    /// <summary>
    /// Check if input value contains text
    /// Equivalent to Selenium's ContainsText
    /// </summary>
    public static async Task<bool> ContainsTextAsync(this ILocator locator, string text)
    {
        var content = await locator.TextContentAsync();
        if (content?.Contains(text) == true)
            return true;

        // Check descendants
        var descendant = locator.Locator($":has-text('{text}')");
        return await descendant.CountAsync() > 0;
    }

    /// <summary>
    /// Get input value
    /// Equivalent to Selenium's Value()
    /// </summary>
    public static async Task<string> ValueAsync(this ILocator locator)
    {
        return await locator.InputValueAsync() ?? "";
    }

    #endregion

    #region Navigation Methods

    /// <summary>
    /// Get parent locator
    /// Equivalent to Selenium's GetParent
    /// </summary>
    public static ILocator GetParent(this ILocator locator)
    {
        return locator.Locator("..");
    }

    /// <summary>
    /// Scroll element into view
    /// Equivalent to Selenium's ScrollTo
    /// </summary>
    public static async Task<ILocator> ScrollToAsync(this ILocator locator)
    {
        await locator.ScrollIntoViewIfNeededAsync();
        await Task.Delay(ScrollToTimeout);
        return locator;
    }

    /// <summary>
    /// Lose focus on element
    /// Equivalent to Selenium's LoseFocus
    /// </summary>
    public static async Task LoseFocusAsync(this ILocator locator)
    {
        await locator.EvaluateAsync("el => { el.focus(); el.blur(); }");
    }

    #endregion

    #region Modal/Popup Methods

    /// <summary>
    /// Capture popup that opens on click
    /// Equivalent to Selenium's CaptureModal
    /// </summary>
    public static async Task<ILocator> CaptureModalAsync(this IPage page, Func<Task> clickAction)
    {
        var oldModals = await page.Locator(".modal.fade.show").AllAsync();
        var oldModalHandles = new HashSet<ILocator>(oldModals);

        await clickAction();

        // Wait for new modal
        await page.WaitForSelectorAsync(".modal.fade.show");

        var newModals = await page.Locator(".modal.fade.show").AllAsync();
        var newModal = newModals.FirstOrDefault(m => !oldModalHandles.Contains(m));

        return newModal ?? page.Locator(".modal.fade.show").Last;
    }

    /// <summary>
    /// Capture popup on locator click
    /// Equivalent to Selenium's CaptureOnClick
    /// </summary>
    public static async Task<ILocator> CaptureOnClickAsync(this ILocator button)
    {
        return await button.Page.CaptureModalAsync(async () => await button.ClickAsync());
    }

    public static async Task<ILocator> CaptureOnDoubleClickAsync(this ILocator button)
    {
        return await button.Page.CaptureModalAsync(async () => await button.DoubleClickAsync());
    }

    #endregion

    #region Disabled/Readonly Checks

    /// <summary>
    /// Check if element is disabled
    /// Equivalent to Selenium's IsDomDisabled
    /// </summary>
    public static async Task<bool> IsDomDisabledAsync(this ILocator locator)
    {
        return !await locator.IsEnabledAsync();
    }

    /// <summary>
    /// Check if element is readonly
    /// Equivalent to Selenium's IsDomReadonly
    /// </summary>
    public static async Task<bool> IsDomReadonlyAsync(this ILocator locator)
    {
        var readonlyAttr = await locator.GetAttributeAsync("readonly");
        return readonlyAttr != null && (readonlyAttr.ToLower() == "true" || readonlyAttr.ToLower() == "readonly");
    }

    #endregion

    #region Retry Helper

    /// <summary>
    /// Retry action on specific exception
    /// Equivalent to Selenium's Retry
    /// </summary>
    public static async Task RetryAsync<TException>(this IPage page, int times, Func<Task> action)
        where TException : Exception
    {
        for (int i = 0; i < times; i++)
        {
            try
            {
                await action();
                return;
            }
            catch (TException)
            {
                if (i >= times - 1)
                    throw;
            }
        }
    }

    #endregion

    #region Wait Helpers

    /// <summary>
    /// Generic wait helper
    /// Equivalent to Selenium's Wait
    /// </summary>
    public static async Task<T> WaitAsync<T>(this IPage page, Func<Task<T?>> condition, string? actionDescription = null, TimeSpan? timeout = null)
    {
        var maxTime = timeout ?? DefaultTimeout;
        var startTime = DateTime.Now;
        Exception? lastException = null;

        while (DateTime.Now - startTime < maxTime)
        {
            try
            {
                var result = await condition();
                if (result != null && !result.Equals(false))
                    return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            await Task.Delay(DefaultPollingInterval);
        }

        throw new TimeoutException(
            $"Timeout waiting for {actionDescription ?? "condition"} after {maxTime.TotalSeconds}s. " +
            (lastException != null ? $"Last exception: {lastException.Message}" : ""));
    }

    /// <summary>
    /// Wait for boolean condition
    /// </summary>
    public static async Task WaitAsync(this IPage page, Func<Task<bool>> condition, string? actionDescription = null, TimeSpan? timeout = null)
    {
        var maxTime = timeout ?? DefaultTimeout;
        var startTime = DateTime.Now;

        while (DateTime.Now - startTime < maxTime)
        {
            try
            {
                if (await condition())
                    return;
            }
            catch
            {
                // Continue waiting
            }

            await Task.Delay(DefaultPollingInterval);
        }

        throw new TimeoutException($"Timeout waiting for {actionDescription ?? "condition"} after {maxTime.TotalSeconds}s");
    }

    /// <summary>
    /// Wait for value to equal expected
    /// </summary>
    public static async Task WaitEqualsAsync<T>(this IPage page, T expectedValue, Func<Task<T>> getValue, TimeSpan? timeout = null)
    {
        await page.WaitAsync(async () =>
        {
            var value = await getValue();
            return EqualityComparer<T>.Default.Equals(value, expectedValue);
        }, $"value to equal {expectedValue}", timeout);
    }
    #endregion


    public static async Task RunAndConsumeAlertAsync(this IPage page, Func<Task> action)
    {
        var tcs = new TaskCompletionSource<IDialog>();

        void Handler(object? sender, IDialog dialog)
        {
            tcs.TrySetResult(dialog);
        }

        page.Dialog += Handler;

        await action();

        var dialog = await tcs.Task;
        await dialog.AcceptAsync();

        page.Dialog -= Handler;
    }

}
