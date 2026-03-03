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

Before running tests, verify the application process is up. As an AI agent, check this with CLI or MCP tools:

**CLI — check the process:**
```bash
tasklist | findstr iisexpress   # IIS Express
tasklist | findstr dotnet        # Kestrel
```

**MCP (Chrome DevTools) — check the login page loads:**
Use `mcp__Claude_in_Chrome__navigate` to open `BaseUrl`, then `mcp__Claude_in_Chrome__read_page` to confirm the login page is present.

If the app is not running, **stop and tell the user** — do not try to start it. Ask them to start the project in Visual Studio (F5 / IIS Express) or run `dotnet run`, then retry.

The test startup itself also verifies availability: it posts to `/api/clearAllBlocks` and will throw an `HttpRequestException` immediately if the app is down.

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

## 9. Debugging Test Failures

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
