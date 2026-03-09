# React UI Testing

UI tests use **xUnit.v3** + **Selenium WebDriver** with Signum's proxy layer. Tests drive a real Chrome browser against a running application.

## 1. Test Class Structure

Every test class relies on a static base class that initializes the environment once and provides a `Browse()` helper:

```cs
public class YourTestClass
{
    [Fact]
    public void SomeTest()
    {
        Browse("System", b =>
        {
            // use b (BrowserProxy) to interact with the app
        });
    }

    static void Browse(string username, Action<BrowserProxy> action) { ... }
}
```

- The static constructor calls `YourEnvironment.StartAndInitialize()` once per test run.
- `Browse()` creates a `ChromeDriver`, logs in as `username`, runs `action`, and always disposes the driver — even on failure.

---

## 2. Environment Setup

The test environment infrastructure (`YourEnvironment.cs`, `Common.cs`) is already provided by the project template — you do not need to create it.

What you need to configure before running tests for the first time:

- **`appsettings.json`** — set `BaseUrl` to the running application URL.
- **User Secrets** — set `ConnectionString` and any secrets (broadcast keys, storage). Never commit secrets to `appsettings.json`.
- **Database** — `StartAndInitialize()` calls `Administrator.RestoreSnapshotOrDatabase()` automatically. If no snapshot exists it creates the database from scratch.

After restoring the database, the infrastructure automatically calls `/api/clearAllBlocks` and `/api/cache/invalidateAll` to bring the running app in sync with the restored state.

---

## 3. Checking the App Is Running

**Before running any Selenium tests, the web application MUST be running.**

The test infrastructure expects the app to be already up and will fail immediately if it's not:
- The static constructor posts to `/api/clearAllBlocks` during test class initialization
- If the app is not running, this will throw an `HttpRequestException` with a connection refused error

### How to Check if IIS Express / Kestrel is Running

**Option 1 — Check via process list (CLI):**

```powershell
# For IIS Express (Visual Studio F5 default)
tasklist | Select-String "iisexpress"

# For Kestrel (dotnet run)
tasklist | Select-String "dotnet" | Select-String "exec"

# Combined check
$iis = Get-Process -Name "iisexpress" -ErrorAction SilentlyContinue
$kestrel = Get-Process -Name "dotnet" | Where-Object { $_.CommandLine -like "*exec*" } -ErrorAction SilentlyContinue
if ($iis -or $kestrel) { Write-Host "Web server is running" } else { Write-Host "Web server is NOT running" }
```

**Option 2 — Check via HTTP request:**

```powershell
# Test if the BaseUrl responds
$baseUrl = "http://localhost:5000"  # Replace with your actual BaseUrl
try {
    $response = Invoke-WebRequest -Uri $baseUrl -TimeoutSec 5 -UseBasicParsing
    Write-Host "App is running - Status: $($response.StatusCode)"
} catch {
    Write-Host "App is NOT running - Error: $_"
}
```

**Option 3 — Use Chrome DevTools MCP (for AI agents):**

If you have the Chrome DevTools MCP server set up, you can:
1. Use `chrome_devtools_navigate_page` to open `BaseUrl`
2. Use `chrome_devtools_take_snapshot` to confirm the login page is present

### What to Do if the App Is Not Running

**If you're an AI agent:** Stop immediately and tell the user the app must be running before tests can execute. **Do not attempt to start it automatically.**

**If you're a developer:** Start the application using one of these methods:

1. **Visual Studio:** Press F5 or Ctrl+F5 to run with IIS Express
2. **CLI:** Navigate to the project folder and run:
   ```bash
   dotnet run
   ```
3. **Visual Studio Test Explorer:** Some projects auto-start the app when running tests — check your `.runsettings` file

Once the app is running, the test infrastructure will automatically:
- Restore the database to a known state (snapshot or fresh creation)
- Call `/api/clearAllBlocks` to clear any in-memory state
- Call `/api/cache/invalidateAll` to sync the cache
- Proceed with the test execution

---

## 4. Proxies

A **proxy** is a class that represents a UI page or control. It encapsulates all Selenium interactions for that page so tests stay readable and free of low-level browser code.

| Proxy | Represents | Disposal |
|---|---|---|
| `BrowserProxy` (`b`) | The browser session | Disposed by `Browse()` |
| `FramePageProxy<T>` | An entity edit page | Navigates away |
| `SearchPageProxy` | A search / list page | Navigates away |
| `FrameModalProxy<T>` | An entity edit modal | Closes the modal |
| `MessageModalProxy` | A confirmation dialog | Closes the modal |
| Custom (e.g. `BoardPageProxy`) | Domain-specific page/widget | Custom logic |

### `using` / `EndUsing` — code structure mirrors page structure

Page and Modal Proxies implement `IDisposable`. The nesting of `EndUsing` blocks shows exactly which page or modal each line is operating on:

```cs
Browse("System", b =>
{
    b.SearchPage(typeof(ProjectEntity)).EndUsing(search =>      // on the search page
    {
        search.Create<ProjectEntity>().EndUsing(project =>      //   new project modal/page
        {
            project.AutoLineValue(p => p.Name, "Agile360");    //     filling the form
            project.ExecuteMainOperation(ProjectOperation.Save);
        });                                                      //   modal closed
        search.Results.WaitRows(1);
    });                                                          // search page left
});
```

### Built-in page navigation

```cs
b.FramePage(entity.ToLite()).EndUsing(page => { ... });        // entity edit page
b.SearchPage(typeof(ProjectEntity)).EndUsing(search => { ... }); // search page
```

### Custom proxies — proxy first, test second

When a page has domain-specific UI (drag-and-drop, custom widgets, etc.), **always build the proxy first, then write the test**. Raw Selenium calls must live inside the proxy, never in the test method.

**Step 1 — build the proxy:**
```cs
public class BoardPageProxy : IDisposable
{
    public IWebDriver Selenium { get; }
    public BoardPageProxy(IWebDriver selenium) => Selenium = selenium;

    public void MoveTaskToColumn(Lite<TaskEntity> task, BoardColumn column)
    {
        // Selenium details stay here, inside the proxy
        var card = Selenium.WaitElementVisible(By.CssSelector($"[data-task='{task.Id}']"));
        var col  = Selenium.WaitElementVisible(By.CssSelector($"[data-column='{column}']"));
        new Actions(Selenium).DragAndDrop(card, col).Perform();
    }

    public void WaitTaskInColumn(Lite<TaskEntity> task, BoardColumn column) { ... }

    public void Dispose() { }
}
```

**Step 2 — write the test using only proxy methods:**
```cs
[Fact]
public void MoveTaskToDone()
{
    var task = CreateTask("Fix bug");
    Browse("System", b =>
    {
        using var board = new BoardPageProxy(b.Selenium);
        b.Navigate(board.Url(sprintId));
        board.MoveTaskToColumn(task.ToLite(), BoardColumn.Done);
        board.WaitTaskInColumn(task.ToLite(), BoardColumn.Done);
    });
}
```

The test reads like a specification — not like a Selenium script.

---

## 5. Interacting with Form Lines

Inside a `FramePageProxy<T>`, access form fields via typed expression helpers:

```cs
page.EndUsing(p =>
{
    // Read / write any field generically
    p.AutoLineValue(m => m.Name, "New value");
    var name = p.AutoLineValue(m => m.Name);

    // Specific line types when you need more control
    p.TextBoxLine(m => m.Description).Element.SafeSendKeys("text");
    p.EntityLine(m => m.Project).LiteValue = project.ToLite();
    p.EnumLine(m => m.State).EnumValue = TaskState.Done;
    p.EntityStrip(m => m.Tags).AddElement().EndUsing(tag => { ... });

    // Navigate to an embedded entity
    p.EntityDetail(m => m.Address).EndUsing(addr =>
    {
        addr.AutoLineValue(a => a.City, "Barcelona");
    });

    // Save
    p.ExecuteMainOperation(TaskOperation.Save);
});
```

Prefer `AutoLineValue()` for simple reads/writes. Use the specific line proxy only when you need interactions beyond get/set (e.g. clicking the entity selector, adding items to a strip).

---

## 6. Search Pages

```cs
b.SearchPage(typeof(ProjectEntity)).EndUsing(search =>
{
    search.Search();
    search.Results.WaitRows(3);

    // Open entity from results
    search.Results.EntityClick(project.ToLite()).EndUsing(page => { ... });

    // Create new from search page
    search.Create<ProjectEntity>().EndUsing(page => { ... });

    // Filters
    search.Filters.AddFilter(...);
});
```

---

## 7. Modals

**Capture a modal opened by a button click:**

```cs
page.CaptureOnClick(page.OperationButton(MyOperation.DoSomething))
    .EndUsing(modal => { ... });
```

**Entity creation / edit in a modal:**

```cs
strip.AddElement().EndUsing((FrameModalProxy<TagEntity> tag) =>
{
    tag.AutoLineValue(t => t.Name, "Backend");
    tag.OkWaitClosed();
});
```

**Confirmation dialogs:**

```cs
page.CaptureOnClick(deleteButton).EndUsing((MessageModalProxy msg) =>
{
    msg.ClickWaitClose(MessageModalButton.Yes);
});
```

**Error modals** are detected automatically: if a modal with the `.error-modal` class appears while the test is waiting, it is thrown as a `WebDriverTimeoutException` with the title and body text included. You usually don't need to handle these explicitly — the test will fail with a descriptive message.

---

## 8. Waiting — Never Use `Thread.Sleep`

Always use Signum's `Wait` utilities. They poll every 200 ms up to a 20 s timeout and include a meaningful description in the timeout exception:

```cs
// Wait for an element
element.WithLocator(By.CssSelector(".my-class")).WaitVisible();
element.WithLocator(By.CssSelector(".spinner")).WaitNotVisible();

// Wait for a condition
selenium.Wait(() => someElement.GetDomAttribute("data-loaded") == "true",
    () => "data-loaded attribute to be true");

// Wait for a value
selenium.WaitEquals("Done", () => page.AutoLineValue(m => m.State).ToString());
```

If a debugger is attached and the wait exceeds 5 s, a breakpoint is triggered automatically — use this to inspect the live browser before the full 20 s timeout fires.

---

## 9. Debug Mode — Keep Browser Open on Failure

When debugging a failing test, you often want the browser to stay open so you can inspect the final state. Use **SELENIUM_DEBUG_MODE** to prevent the browser from closing.

### How to Enable Debug Mode

1. **Create the file** `SELENIUM_DEBUG_MODE.txt` in your test project root (same folder as your `.csproj` file)
2. **Set its content** to `true` (case-insensitive)
3. **Run your test** — the browser will stay open after failures

Example file structure:
```
YourProject.Test.React/
├── SELENIUM_DEBUG_MODE.txt    ← Add this file
├── YourProject.Test.React.csproj
├── Agile360TestClass.cs
└── ...
```

File content:
```text
true
```

### What Debug Mode Does

When `SELENIUM_DEBUG_MODE.txt` exists and contains `true`:

- ✅ **Browser stays open** after test failures (skips `selenium.Close()` in the `finally` block)
- ✅ **Modals don't auto-close** (lets you inspect modal state before disposal)
- ✅ **Console notification** printed at test startup: `[SELENIUM DEBUG MODE] Enabled via ...`

When the file is missing, empty, or contains anything other than `true`:
- ❌ Debug mode is disabled (normal cleanup behavior)

### Example Usage

**Test fails with a timeout:**
```cs
[Fact]
public void FailingTest()
{
    Browse("System", b =>
    {
        b.SearchPage(typeof(ProjectEntity)).EndUsing(search =>
        {
            // This will time out and fail
            search.Results.WaitRows(999);
        });
    });
}
```

**Without debug mode:**
- Test fails → screenshot saved → browser closes immediately → you can't inspect

**With debug mode (SELENIUM_DEBUG_MODE.txt = `true`):**
- Test fails → screenshot saved → **browser stays open** → you can:
  - Inspect the DOM in Chrome DevTools
  - Check the network tab for failed requests
  - Verify the console for JS errors
  - Manually interact with the page to understand the issue

### Resetting Debug Mode at Runtime

The `DebugMode` property is cached after the first read. If you change the file during a test run:

```cs
// Force re-read of SELENIUM_DEBUG_MODE.txt
BrowserProxy.ResetDebugMode();
```

This is rarely needed — usually you set it once before running tests and disable it when done.

### When to Use Debug Mode

✅ **Good use cases:**
- Debugging a failing test to see the final page state
- Inspecting an unexpected modal or error message
- Understanding why an element is not found
- Manually stepping through a complex UI interaction

❌ **Don't leave it enabled:**
- In CI/CD pipelines (will leak browser processes)
- When running large test suites (prevents cleanup)
- When committing to version control (add `SELENIUM_DEBUG_MODE.txt` to `.gitignore`)

---

## 10. Debugging Test Failures

Work through this checklist in order:

1. **Read the exception message.** Error modals are captured automatically and their title + body appear in the `WebDriverTimeoutException`. Usually this is enough.

2. **Check the screenshot.** On any `WebDriverException`, a `.jpg` is saved to `./Screenshots/` with the URL and timestamp in the filename. The full path is printed to the test output console.

3. **Use the Chrome DevTools MCP server** for live browser inspection (elements, network, console). Install the [Claude in Chrome](https://chromewebstore.google.com/detail/claude-in-chrome/...)) extension and use the `mcp__Claude_in_Chrome__*` tools to:
   - Inspect the DOM with `read_page`
   - Run JS with `javascript_tool`
   - Watch network requests with `read_network_requests`
   - Read console errors with `read_console_messages`

   If the extension is not installed, suggest the user install it and connect it before the test run.

4. **Add a temporary `Debugger.Break()`** just before the failing line and run with debugger attached — the 5 s wait threshold will pause at the right moment.
