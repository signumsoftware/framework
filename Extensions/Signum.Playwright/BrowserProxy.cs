using Signum.Entities.Reflection;
using Signum.Playwright.Frames;
using Signum.Playwright.Search;

namespace Signum.Playwright;

/// <summary>
/// Playwright equivalent of Selenium's BrowserProxy
/// Main entry point for Signum Framework testing with Playwright
/// </summary>
public class BrowserProxy
{
    public IPage Page { get; }
    public IPlaywright PlaywrightInstance { get; }
    public IBrowser Browser { get; }

    public BrowserProxy(IPage page, IPlaywright playwrightInstance, IBrowser browser)
    {
        Page = page;
        PlaywrightInstance = playwrightInstance;
        Browser = browser;
    }

    /// <summary>
    /// Override this method to provide base URL
    /// Equivalent to Selenium's Url method
    /// </summary>
    public virtual string Url(string url)
    {
        throw new InvalidOperationException("Implement this method returning something like: http://localhost:5000/ + url");
    }

    /// <summary>
    /// Navigate to search page for a query
    /// Equivalent to Selenium's SearchPage
    /// </summary>
    public async Task<SearchPageProxy> SearchPageAsync(object queryName, bool waitInitialSearch = true)
    {
        var url = Url(FindRoute(queryName));
        await Page.GotoAsync(url);

        var result = new SearchPageProxy(Page);

        if (waitInitialSearch)
        {
            await result.SearchControl.WaitInitialSearchCompletedAsync();
        }

        return result;
    }

    /// <summary>
    /// Get route for Find page
    /// Equivalent to Selenium's FindRoute
    /// </summary>
    public virtual string FindRoute(object queryName)
    {
        return "Find/" + GetWebQueryName(queryName);
    }

    /// <summary>
    /// Get web query name from type or object
    /// </summary>
    public string GetWebQueryName(object queryName)
    {
        if (queryName is Type t)
            return Reflector.CleanTypeName(t);

        return queryName.ToString()!;
    }

    /// <summary>
    /// Navigate to entity frame page
    /// Equivalent to Selenium's FramePage
    /// </summary>
    public async Task<FramePageProxy<T>> FramePageAsync<T>(PrimaryKey id) where T : Entity
    {
        return await FramePageAsync<T>(Lite.Create<T>(id));
    }

    public async Task<FramePageProxy<T>> FramePageAsync<T>() where T : Entity
    {
        var url = Url(NavigateRoute(typeof(T), null));
        await Page.GotoAsync(url);
        return new FramePageProxy<T>(Page);
    }

    public async Task<FramePageProxy<T>> FramePageAsync<T>(Lite<T> lite) where T : Entity
    {
        if (lite.EntityType != typeof(T))
            throw new InvalidOperationException($"Use FramePage<{lite.EntityType.Name}> instead");

        var url = Url(NavigateRoute(lite));
        await Page.GotoAsync(url);
        return new FramePageProxy<T>(Page);
    }

    /// <summary>
    /// Get navigation route for entity
    /// Equivalent to Selenium's NavigateRoute
    /// </summary>
    public virtual string NavigateRoute(Type type, PrimaryKey? id)
    {
        var typeName = Reflector.CleanTypeName(type);

        if (id.HasValue)
            return $"view/{typeName}/{id}";
        else
            return $"create/{typeName}";
    }

    public virtual string NavigateRoute(Lite<IEntity> lite)
    {
        return NavigateRoute(lite.EntityType, lite.IdOrNull);
    }

    /// <summary>
    /// Get current logged-in user
    /// Equivalent to Selenium's GetCurrentUser
    /// </summary>
    public virtual async Task<string?> GetCurrentUserAsync()
    {
        var loginDropdown = Page.Locator(".sf-login-dropdown, .sf-login");
        
        if (!await loginDropdown.IsPresentAsync())
            return null;

        if (await loginDropdown.HasClassAsync("sf-login"))
            return null;

        return await loginDropdown.TextContentAsync();
    }

    /// <summary>
    /// Logout current user
    /// Equivalent to Selenium's Logout
    /// </summary>
    public virtual async Task LogoutAsync()
    {
        await Page.Locator("#sfLoginDropdown").ClickAsync();
        await Page.Locator("#sf-auth-logout").ClickAsync();
        
        await Page.WaitAsync(async () => await GetCurrentUserAsync() == null);
        await Page.GotoAsync(Url("Auth/Login"));
        await Page.Locator(".sf-login").WaitVisibleAsync();
    }

    /// <summary>
    /// Login to application
    /// Equivalent to Selenium's Login
    /// </summary>
    public virtual async Task LoginAsync(string username, string password)
    {
        await Page.GotoAsync(Url("Auth/Login"));

        await Page.Locator(".sf-login-page").WaitPresentAsync();
        // Check if login form button exists
        var showLoginButton = Page.Locator("#sf-show-login-form");
        if (await showLoginButton.IsPresentAsync())
        {
            await showLoginButton.ClickAsync();
            await Page.Locator("#login").WaitVisibleAsync();
        }

        var currentUser = await GetCurrentUserAsync();
        if (currentUser == username)
            return;

        await Page.Locator("#userName").FillAsync(username);
        await Page.Locator("#password").FillAsync(password);
        await Page.Locator("#login").ClickAsync();

        await Page.Locator("#login").WaitNotPresentAsync();
        await Page.Locator(".sf-login-dropdown").WaitVisibleAsync();

        await SetCurrentCultureAsync();
    }

    /// <summary>
    /// Set current culture from page
    /// </summary>
    public virtual async Task SetCurrentCultureAsync()
    {
        var cultureDropdown = Page.Locator(".sf-culture-dropdown");
        var culture = await cultureDropdown.GetAttributeAsync("data-culture");

        if (!string.IsNullOrEmpty(culture))
        {
            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = 
                new System.Globalization.CultureInfo(culture);
        }
    }

    /// <summary>
    /// Wait helper
    /// Equivalent to Selenium's Wait
    /// </summary>
    public async Task<T> WaitAsync<T>(Func<Task<T?>> condition, string? description = null, TimeSpan? timeout = null)
    {
        return await Page.WaitAsync(condition, description, timeout);
    }
}
