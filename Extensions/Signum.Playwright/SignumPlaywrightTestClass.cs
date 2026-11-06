using System.Diagnostics;
using Signum.Utilities.Synchronization;

namespace Signum.Playwright;

public class SignumPlaywrightTestClass
{
    public static string BaseUrl { get; protected set; } = null!;

    protected bool Cleaned = false;

    protected const int DebugChromePort = 9222;

    protected static void AssertClean200(HttpResponseMessage response)
    {
        var content = response.Content.ReadAsStringAsync().ResultSafe();
        if (!response.IsSuccessStatusCode || content != "")
            throw new InvalidOperationException($"Error {response.StatusCode}\n"
                + "Content:\n"
                + content
                );
    }

    protected static string? GetPlaywrightMode()
    {
        string? mode = System.Environment.GetEnvironmentVariable("PLAYWRIGHT_MODE") ??
                   ReadModeFromFile("../../../PLAYWRIGHT_MODE.txt") ??
                  (Debugger.IsAttached ? "debug" : null);

        return mode;
    }

    /// <summary>
    /// "debug" connects to an independent Chrome over CDP and leaves it open, "headed" opens a maximized window,
    /// anything else (the default) runs headless.
    /// Headless is the default because not every test host can open a window: in the Visual Studio one the headed
    /// LaunchAsync fails with "Target page, context or browser has been closed" right after the browser launches,
    /// while the very same code works from a console. Ask for "headed" when you want to watch the test.
    /// </summary>
    protected static async Task<IBrowser> GetBrowser(IPlaywright playwright, string? mode)
    {
        if (mode != null && mode.ToLower() == "debug")
        {
            BrowserProxy.DebugMode = true;
            var userDataDir = Path.Combine(Path.GetTempPath(), "playwright-debug-chrome");
            return await BrowserProxy.ConnectDebugChromeAsync(playwright, DebugChromePort, userDataDir);
        }

        if (mode != null && mode.ToLower() == "headed")
            return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                Args = new[]
                {
                    "--start-maximized",
                    "--no-first-run",
                    "--no-default-browser-check",
                    "--disable-popup-blocking",
                },
            });

        HeadlessMode = true;
        return await playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    /// <summary>
    /// True once GetBrowser has launched the headless browser, which has no window to take the size from.
    /// </summary>
    protected static bool HeadlessMode { get; private set; }

    /// <summary>
    /// Viewport of the headless browser. Without it the pages would get Chromium's default window size,
    /// so a responsive layout could render differently than in headed mode. Change it if the application
    /// needs more room.
    /// </summary>
    public static ViewportSize HeadlessViewport { get; set; } = new ViewportSize { Width = 1920, Height = 1080 };

    protected virtual async Task<IPage> GetPageAsync(IBrowser browser, IEnumerable<string> permissions)
    {
        if (BrowserProxy.DebugMode)
        {
            await CleanDebugTabs(browser);
            // Reuse the default context of the CDP-launched Chrome window so the
            // new page opens as a tab there instead of a separate incognito window.
            var context = browser.Contexts.SingleEx();
            await context.GrantPermissionsAsync(permissions);
            return context.Pages.Last(a => !IsChromePage(a));
        }
        else
        {
            var context = browser.Contexts.SingleOrDefaultEx() /*Nested BrowseAsync*/ ??  await browser.NewContextAsync(new BrowserNewContextOptions
            {
                //Headed mode takes the size from the window, so --start-maximized works
                ViewportSize = HeadlessMode ? HeadlessViewport : ViewportSize.NoViewport,
                Permissions = permissions,
            });
            return await context.NewPageAsync();
        }
    }

    protected virtual async Task CleanDebugTabs(IBrowser browser)
    {
        if (!Cleaned)
        {
            foreach (var item in browser.Contexts.Skip(1).ToList())
                await item.CloseAsync();

            var ctx = browser.Contexts.SingleEx();
            
            foreach (var pg in ctx.Pages.Where(a => !IsChromePage(a)).Skip(1).ToList())
                await pg.CloseAsync();

            Cleaned = true;
        }
        else
        {
            var context = browser.Contexts.SingleEx(); 

            await context.NewPageAsync();
        }
    }

    private static bool IsChromePage(IPage a)
    {
        return a.Url.Contains("omnibox-popup.top-chrome") || a.Url.Contains("chrome://");
    }

    public static string? ReadModeFromFile(string fileName)
    {
        if (!File.Exists(fileName))
            return null;

        var firstLine = File.ReadAllLines(fileName).FirstOrDefault();

        if (firstLine != null && firstLine.ToLower() is "debug" or "headed" or "headless")
            return firstLine;

        return null;
    }
}

