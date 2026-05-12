using Signum.Entities.Reflection;
using Signum.Playwright.Frames;
using Signum.Playwright.Search;
using System.Diagnostics;
using System.Globalization;

namespace Signum.Playwright;

/// <summary>
/// Base class for the strongly-typed proxy for your application.
/// Can perform common actions like login/logout and contains method to navigate to different pages (SearchPage / FramePage)
/// You can inherit from this class to provide application-specific pages, and override Url with your base URL.
/// </summary>
public class BrowserProxy
{
    public IPage Page { get; }

    public BrowserProxy(IPage page)
    {
        Page = page;
    }

    
    /// <summary>
    /// When true, connects to an independent Chrome instance via CDP, keeps the browser open on test failure, and prevents modal auto-close.
    /// Checks for PLAYWRIGHT_DEBUG_MODE.txt file (relative path: ../../../PLAYWRIGHT_DEBUG_MODE.txt from bin folder).
    /// Debug mode is enabled only if the file exists AND contains "true".
    /// </summary>
    public static bool DebugMode { get; set; }

    /// <summary>
    /// Launches an independent Chrome instance with remote debugging on the given port and connects
    /// Playwright to it via CDP. The browser stays open after the test, making it suitable for debug mode.
    /// Chrome is started with a dedicated user-data-dir so it runs as a fresh, separate instance
    /// even if Chrome is already open.
    /// Chrome path is resolved from the CHROME_PATH env var or the standard Windows install locations.
    /// </summary>
    public static async Task<IBrowser> ConnectDebugChromeAsync(IPlaywright playwright, int port, string userDataDir)
    {
        var args = $"--remote-debugging-port={port} --user-data-dir=\"{userDataDir}\" " +
                   "--start-maximized --no-first-run --no-default-browser-check --disable-popup-blocking " +
                   "--enable-automation --disable-save-password-bubble about:blank";

        // Write Chrome profile preferences before launch so Password Manager and
        // leak detection are disabled from the very first run (equivalent to
        // Selenium's AddUserProfilePreference).
        var prefsDir = Path.Combine(userDataDir, "Default");
        Directory.CreateDirectory(prefsDir);
        File.WriteAllText(Path.Combine(prefsDir, "Preferences"), """
            {
              "profile": {
                "password_manager_enabled": false,
                "password_manager_leak_detection": false,
                "default_content_setting_values": {
                  "automatic_downloads": 1,
                  "notifications": 2
                }
              }
            }
            """);

        Console.WriteLine($"[PLAYWRIGHT DEBUG MODE] Starting Chrome on port {port}...");

        Process.Start(new ProcessStartInfo
        {
            FileName = GetChromePath(),
            Arguments = args,
            UseShellExecute = true,
        });

        // Give Chrome time to start and open the debugging port
        await Task.Delay(2000);

        Console.WriteLine($"[PLAYWRIGHT DEBUG MODE] Connecting Playwright to http://localhost:{port}");
        return await playwright.Chromium.ConnectOverCDPAsync($"http://localhost:{port}");
    }

    private static string GetChromePath()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("CHROME_PATH") ?? "",
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
        };

        foreach (var candidate in candidates)
            if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate))
                return candidate;

        throw new FileNotFoundException(
            "Chrome executable not found. Set the CHROME_PATH environment variable to point to chrome.exe.");
    }

    /// <summary>
    /// Override this method to provide base URL
    /// </summary>
    public virtual string Url(string url)
    {
        throw new InvalidOperationException("Implement this method returning something like: http://localhost:5000/ + url");
    }

    /// <summary>
    /// Navigate to search page for a query
    /// </summary>
    public async Task<SearchPageProxy> SearchPageAsync(object queryName, bool waitInitialSearch = true)
    {
        var url = Url(FindRoute(queryName));
        await Page.GotoAsync(url);

        var result = await SearchPageProxy.NewAsync(Page);

        if (waitInitialSearch)
        {
            await result.SearchControl.WaitInitialSearchCompletedAsync();
        }

        return result;
    }

    /// <summary>
    /// Get route for Find page
    /// </summary>
    public virtual string FindRoute(object queryName)
    {
        return "find/" + QueryUtils.GetKey(queryName);
    }

    /// <summary>
    /// Navigate to entity frame page
    /// </summary>
    public async Task<FramePageProxy<T>> FramePageAsync<T>(PrimaryKey id) where T : Entity
    {
        return await FramePageAsync<T>(Lite.Create<T>(id));
    }

    public async Task<FramePageProxy<T>> FramePageAsync<T>() where T : Entity
    {
        var url = Url(NavigateRoute(typeof(T), null));
        await Page.GotoAsync(url);
        return await FramePageProxy<T>.NewAsync(Page);
    }

    public async Task<FramePageProxy<T>> FramePageAsync<T>(Lite<T> lite) where T : Entity
    {
        if (lite.EntityType != typeof(T))
            throw new InvalidOperationException($"Use FramePage<{lite.EntityType.Name}> instead");

        var url = Url(NavigateRoute(lite));
        await Page.GotoAsync(url);
        return await FramePageProxy<T>.NewAsync(Page);
    }

    /// <summary>
    /// Get navigation route for entity
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
    /// </summary>
    public virtual async Task LoginAsync(string username, string password, int timeout = 30000)
    {
        await Page.GotoAsync(Url("Auth/Login"), new PageGotoOptions { Timeout = timeout });

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

    }

    public virtual async Task<CultureInfo> GetCultureFromDropdownAsync()
    {
        var cultureDropdown = Page.Locator(".sf-culture-dropdown");
        await cultureDropdown.WaitVisibleAsync();
        var culture = await cultureDropdown.GetAttributeAsync("data-culture");
        if (!string.IsNullOrEmpty(culture))
        {
            return new CultureInfo(culture);
        }

        throw new InvalidOperationException("Unable to find culture in .sf-culture-dropdown");
    }

    public async Task<CultureInfo> GetCultureFromLoginDropdownAsync()
    {
        var loginDropdown = Page.Locator(".sf-login-dropdown");
        await loginDropdown.WaitPresentAsync();
        await loginDropdown.ClickAsync();
        var cultureMenuItem = Page.Locator(".sf-login-dropdown .sf-culture-menu-item");
        await cultureMenuItem.WaitPresentAsync();
        var culture = await cultureMenuItem.GetAttributeAsync("data-culture");
        if (!string.IsNullOrEmpty(culture))
        {
            return new CultureInfo(culture);
        }

        throw new InvalidOperationException("Unable to find culture");
    }

    /// <summary>
    /// Wait helper
    /// </summary>
    public async Task<T> WaitAsync<T>(Func<Task<T?>> condition, string? description = null, TimeSpan? timeout = null)
    {
        return await Page.WaitAsync(condition, description, timeout);
    }

}
