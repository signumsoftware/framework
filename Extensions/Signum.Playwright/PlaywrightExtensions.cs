using Microsoft.Playwright;
using System.Diagnostics;

namespace Signum.Playwright;

/// <summary>
/// Core extension methods for Playwright in Signum Framework
/// </summary>
public static class PlaywrightExtensions
{
    #region Existence Checks (No Wait)

    /// <summary>
    /// Check if locator exists without waiting (immediate)
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
    /// Try to find element, returns null if not found
    /// </summary>
    public static async Task<IElementHandle?> TryElementHandleAsync(this ILocator locator)
    {
        try
        {
            var count = await locator.CountAsync();
            if (count == 0)
                return null;

            return await locator.ElementHandleAsync();
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
    /// </summary>
    public static async Task<ILocator> WaitPresentAsync(this ILocator locator, float? timeoutMs = null)
    {
        await locator.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = timeoutMs,
        });
        return locator;
    }

    /// <summary>
    /// Wait for locator to be visible
    /// </summary>
    public static async Task<ILocator> WaitVisibleAsync(this ILocator locator, float? timeoutMs = null, bool scrollTo = false)
    {
        await locator.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = timeoutMs,
        });

        if (scrollTo)
        {
            await locator.ScrollIntoViewIfNeededAsync();
        }

        return locator;
    }

    /// <summary>
    /// Wait for locator to not be present (detached from DOM)
    /// </summary>
    public static async Task WaitNotPresentAsync(this ILocator locator, float? timeoutMs = null)
    {
        await locator.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Detached,
            Timeout = timeoutMs,
        });
    }

    /// <summary>
    /// Wait for locator to not be visible
    /// </summary>
    public static async Task WaitNotVisibleAsync(this ILocator locator, float? timeoutMs = null)
    {
        await locator.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden,
            Timeout = timeoutMs,
        });
    }

    #endregion

    #region Assert Methods

    /// <summary>
    /// Assert that locator is present
    /// </summary>
    public static async Task AssertPresentAsync(this ILocator locator)
    {
        if (!await locator.IsPresentAsync())
            throw new PlaywrightException($"Locator '{locator}' is not present");
    }

    /// <summary>
    /// Assert that locator is not present
    /// </summary>
    public static async Task AssertNotPresentAsync(this ILocator locator)
    {
        if (await locator.IsPresentAsync())
            throw new PlaywrightException($"Locator '{locator}' is present but should not be");
    }

    /// <summary>
    /// Assert that locator is visible
    /// </summary>
    public static async Task AssertVisibleAsync(this ILocator locator)
    {
        if (!await locator.IsVisibleAsync())
            throw new PlaywrightException($"Locator '{locator}' is not visible");
    }

    /// <summary>
    /// Assert that locator is not visible
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
    /// </summary>
    public static async Task SafeClickAsync(this ILocator locator)
    {
        await locator.ScrollIntoViewIfNeededAsync();
        await locator.ClickAsync();
    }

    /// <summary>
    /// Fill input with clear and wait for value
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
    /// </summary>
    public static async Task<string> GetAttributeOrThrowAsync(this ILocator locator, string attributeName)
    {
        var value = await locator.GetAttributeAsync(attributeName);
        return value ?? throw new InvalidOperationException($"Attribute '{attributeName}' not found");
    }

    /// <summary>
    /// Get element ID
    /// </summary>
    public static async Task<string?> GetIdAsync(this ILocator locator)
    {
        return await locator.GetAttributeAsync("id");
    }

    /// <summary>
    /// Get CSS classes
    /// </summary>
    public static async Task<IEnumerable<string>> GetClassesAsync(this ILocator locator)
    {
        var classAttr = await locator.GetAttributeAsync("class");
        return (classAttr ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Check if locator has specific CSS class
    /// </summary>
    public static async Task<bool> HasClassAsync(this ILocator locator, string className)
    {
        var classes = await locator.GetClassesAsync();
        return classes.Contains(className);
    }

    public static async Task WaitHasClassAsync(this ILocator locator, string className, bool shouldHave, float? timeoutMs = null)
    {
        await locator.Page.WaitForFunctionAsync(
            @"([el, cls, shouldHave]) => {
                debugger;
                const hasClass = el.classList.contains(cls);
                return shouldHave == hasClass;
            }",
            new object[]{ await locator.ElementHandleAsync(), className, shouldHave },
            new PageWaitForFunctionOptions { Timeout = timeoutMs }
        );
    }

    public static async Task WaitAttributeAsync(this ILocator locator, string attributeName, string? expectedValue, string op = "===", float? timeoutMs = null)
    {
        var elementHandle = await locator.ElementHandleAsync();

        await locator.Page.WaitForFunctionAsync(
            $@"([el, attr, value]) => el.getAttribute(attr) {op} value",
            new object?[] { elementHandle, attributeName, expectedValue },
            new PageWaitForFunctionOptions() { Timeout = timeoutMs}
        );
    }

    public static async Task WaitContentAsync(this ILocator locator, string? expectedContent, string op = "===", float? timeoutMs = null)
    {
        var elementHandle = await locator.ElementHandleAsync();

        await locator.Page.WaitForFunctionAsync(
            $@"([el, content]) => el.innerText {op} content",
            new object?[] { elementHandle, expectedContent },
            new PageWaitForFunctionOptions() { Timeout = timeoutMs}
        );
    }

    public static async Task WaitDisabledAsync(this ILocator locator, bool shouldBeDisabled, float? timeoutMs = null)
    {
        if (shouldBeDisabled)
            await Assertions.Expect(locator).ToBeDisabledAsync(timeoutMs == null ? null : new LocatorAssertionsToBeDisabledOptions { Timeout = timeoutMs });
        else
            await Assertions.Expect(locator).ToBeEnabledAsync(timeoutMs == null ? null : new LocatorAssertionsToBeEnabledOptions { Timeout = timeoutMs });
    }

    /// <summary>
    /// Check if locator has all specified CSS classes
    /// </summary>
    public static async Task<bool> HasClassAsync(this ILocator locator, params string[] classNames)
    {
        var classes = (await locator.GetClassesAsync()).ToHashSet();
        return classNames.All(cn => classes.Contains(cn));
    }

    /// <summary>
    /// Check if input value contains text
    /// </summary>
    public static async Task<bool> ContainsTextAsync(this ILocator locator, string text)
    {
        var content = await locator.TextContentAsync();
        return content?.Contains(text) == true;
    }

    #endregion

    #region Navigation Methods

    /// <summary>
    /// Get parent locator
    /// </summary>
    public static ILocator GetParent(this ILocator locator)
    {
        return locator.Locator("..");
    }

    /// <summary>
    /// Returns a valid CSS selector derived from the locator's ToString() representation.
    /// Strips the "Locator@" prefix, removes Playwright-specific "nth=N" parts,
    /// and replaces " >> " descendant separators with a space.
    /// </summary>
    public static string ToCssSelector_QueryAll(this ILocator locator) => $"document.querySelectorAll(\"{locator.ToCssSelector()}\")"; 
    public static string ToCssSelector(this ILocator locator)
    {
        var selector = locator.ToString()!.After('@');

        var parts = selector.Split(" >> ");
        var result = new List<string>();
        foreach (var part in parts)
        {
            if (part.StartsWith("nth=") && part[4..].All(char.IsDigit))
            {
                //var n = int.Parse(part[4..]);
                //if (result.Count > 0)
                //    result[^1] += $":nth-child({n + 1})";
            }
            else if (part.Contains(','))
            {
                result.Add($":is({part})");
            }
            else
            {
                result.Add(part);
            }
        }
        return string.Join(" ", result);
    }

    #endregion

    #region Modal/Popup Methods


    /// <summary>
    /// Capture the modal that opens as a result of clickAction.
    /// Uses data-capture-index to identify old modals and assign a unique index to the new one.
    /// Only captures the first modal that appears. Call this method multiple times to capture multiple modals.
    /// </summary>
    public static async Task<ILocator> CaptureModalAsync(this IPage page, Func<Task> clickButton)
    {
        var maxBefore = await page.EvaluateAsync("""
            () => {
                var max = 0;
                document.querySelectorAll('.modal.fade.show[data-capture-index]').forEach(el => {
                    max = Math.max(max, parseInt(el.getAttribute('data-capture-index')));
                });
                return max;
            }
            """);

        await clickButton();

        // Wait for the new modal (the first visible modal without capture-index)
        await page.WaitForSelectorAsync(".modal.fade.show:not([data-capture-index])");

        var maxAfter = await page.EvaluateAsync("""
            () => {
                var max = 0;
                document.querySelectorAll('.modal.fade.show[data-capture-index]').forEach(el => {
                    max = Math.max(max, parseInt(el.getAttribute('data-capture-index')));
                });
                return max;
            }
            """);

        var nextIndex = Math.Max(
            maxBefore?.ToObject<int>() ?? 0,
            maxAfter?.ToObject<int>() ?? 0
            ) + 1;

        // Tag only the first captured modal with a unique index
        await page.EvaluateAsync("""
            (nextIndex) => {
                const modal = document.querySelector('.modal.fade.show:not([data-capture-index])');
                if (modal) {
                    modal.setAttribute('data-capture-index', nextIndex.toString());
                }
            }
            """, nextIndex);

        return page.Locator($".modal.fade.show[data-capture-index='{nextIndex}']");
    }

    /// <summary>
    /// Capture popup on locator click
    /// </summary>
    public static async Task<ILocator> CaptureOnClickAsync(this ILocator button)
    {
        return await button.Page.CaptureModalAsync(()=> button.ClickAsync());
    }

    public static async Task MoveMouseAsync(this ILocator button, float xRatio = .5f, float yRatio = .5f)
    {
        var bb = await button.BoundingBoxAsync();
        await button.Page.Mouse.MoveAsync(bb!.X + bb.Width * xRatio, bb.Y + bb.Height * yRatio);
    }



    public static async Task<ILocator> CaptureOnDoubleClickAsync(this ILocator button)
    {
        return await button.Page.CaptureModalAsync(()=> button.DoubleClickAsync());
    }

    #endregion

    #region Disabled/Readonly Checks
    /// <summary>
    /// Check if element is readonly
    /// </summary>
    public static async Task<bool> IsDomReadonlyAsync(this ILocator locator)
    {
        var readonlyAttr = await locator.GetAttributeAsync("readonly");
        return readonlyAttr != null && (readonlyAttr.ToLower() == "true" || readonlyAttr.ToLower() == "readonly");
    }

    #endregion

    #region Wait Helpers

    public static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);
    public static TimeSpan DefaultPollingInterval = TimeSpan.FromMilliseconds(200);
    /// <summary>
    /// Generic wait helper, waits in C# (slower) not in javascript (faster) but allows complex conditions and better error messages
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

    #endregion

}
