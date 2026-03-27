using Microsoft.Playwright;

namespace Signum.Playwright;

public class ElementLocator
{
    public ILocator Locator { get; private set; }
    public IPage Page { get; private set; }

    public ElementLocator(ILocator parentLocator, string selector, IPage page)
    {
        Locator = parentLocator.Locator(selector);
        Page = page;
    }

    public ElementLocator(IPage page, string selector)
    {
        Locator = page.Locator(selector);
        Page = page;
    }

    public async Task<ILocator> WaitVisibleAsync(bool scrollTo = false)
    {
        await Locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        if (scrollTo)
            await Locator.ScrollIntoViewIfNeededAsync();
        return Locator;
    }

    public async Task<ILocator> WaitPresentAsync()
    {
        await Locator.WaitForAsync();
        return Locator;
    }

    public async Task WaitNoVisibleAsync()
    {
        await Locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
    }

    public async Task WaitNoPresentAsync()
    {
        await Locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
    }

    public async Task<bool> IsVisibleAsync()
    {
        try
        {
            return await Locator.IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsPresentAsync()
    {
        try
        {
            return await Locator.CountAsync() > 0;
        }
        catch
        {
            return false;
        }
    }

    public ElementLocator CombineCss(string suffix)
    {
        return new ElementLocator(Locator, suffix, Page);
    }

    public async Task ClickAsync()
    {
        await Locator.ClickAsync();
    }

    public async Task<string?> GetTextAsync()
    {
        return await Locator.InnerTextAsync();
    }

    public async Task<string?> GetValueAsync()
    {
        return await Locator.InputValueAsync();
    }

    public async Task FillAsync(string value)
    {
        await Locator.FillAsync(value);
    }

    public async Task SelectOptionAsync(string value)
    {
        await Locator.SelectOptionAsync(value);
    }

    public async Task ScrollIntoViewIfNeededAsync()
    {
        await Locator.ScrollIntoViewIfNeededAsync();
    }
}
