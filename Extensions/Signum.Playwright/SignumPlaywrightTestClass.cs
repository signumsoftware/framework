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

    protected static async Task<IBrowser> GetBrowser(IPlaywright playwright, string? mode)
    {
        if (mode != null && mode.ToLower() == "headless")
            return await playwright.Chromium.LaunchAsync(new() { Headless = true });

        if (mode != null && mode.ToLower() == "debug")
        {
            BrowserProxy.DebugMode = true;
            var userDataDir = Path.Combine(Path.GetTempPath(), "playwright-debug-chrome");
            return await BrowserProxy.ConnectDebugChromeAsync(playwright, DebugChromePort, userDataDir);
        }

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
    }

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
                ViewportSize = ViewportSize.NoViewport, // Allow start-maximized to work
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

        if (firstLine != null && firstLine.ToLower() is "debug" or "headless")
            return firstLine;

        return null;
    }
}

