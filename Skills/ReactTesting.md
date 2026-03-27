# React UI Testing

UI tests use **xUnit.v3** + **Playwright** with Signum's async proxy layer. 

## 1. Test Class Structure

Every test class relies on a base class that initializes the environment once and provides a `BrowseAsync()` helper:

```cs
    static async Task BrowseAsync(string username, Func<SouthwindProxy, Task> action) { ... }
```

A test method looks like this:

```cs
public class YourTestClass : SpitzleiTestClass
{
    [Fact]
    public async Task SomeTestAsync()
    {
        //Prepare test data here (using Database.Save Database.Query<T>, Operations, etc..) 

        await BrowseAsync("System", async b =>
        {
            // use b (BrowserProxy) to interact with the app
        });
    }
}
```

---

## 2. Environment Setup

The test environment infrastructure (`YourEnvironment.cs`, `Common.cs`) is already provided by the project template — you do not need to create it.

What you need to configure before running tests for the first time:

- **`appsettings.json`** — set `Url` to the running application URL (e.g., `http://localhost:5000`).
- **User Secrets** — set `ConnectionString` and any secrets (broadcast keys, storage). Never commit secrets to `appsettings.json`.
- **Database** — `StartAndInitialize()` calls `Administrator.RestoreSnapshot()` or `RestoreSnapshotOrDatabase()` automatically. If no snapshot exists it creates the database from scratch.

After restoring the database, the infrastructure automatically calls `/api/cache/invalidateAll` to bring the running app in sync with the restored state.

---

## 3. Checking the App Is Running

**Before running any UI tests, the web application MUST be running.**

The test infrastructure expects the app to be already up and will fail immediately if it's not:
- The static constructor posts to `/api/cache/invalidateAll` during test class initialization
- If the app is not running, this will throw an `HttpRequestException` with a connection refused error

### How to Check if the App is Running

**Option 1 — Check via HTTP request:**

```powershell
# Test if the BaseUrl responds
$baseUrl = "http://localhost:5000"  # Replace with your actual URL
try {
    $response = Invoke-WebRequest -Uri $baseUrl -TimeoutSec 5 -UseBasicParsing
    Write-Host "App is running - Status: $($response.StatusCode)"
} catch {
    Write-Host "App is NOT running - Error: $_"
}
```

**Option 2 — Check via process list:**

```powershell
# For Kestrel (dotnet run)
tasklist | Select-String "dotnet" | Select-String "exec"
```

### What to Do if the App Is Not Running

**If you're an AI agent:** Stop immediately and tell the user the app must be running before tests can execute. **Do not attempt to start it automatically.**

**If you're a developer:** Start the application using one of these methods:

1. **Visual Studio:** Press F5 or Ctrl+F5 to run the server project
2. **CLI:** Navigate to the server project folder and run:
   ```bash
   dotnet run
   ```

Once the app is running, the test infrastructure will automatically:
- Restore the database to a known state (snapshot or fresh creation)
- Call `/api/cache/invalidateAll` to sync the cache
- Proceed with the test execution

---

## 4. Proxies

A **proxy** is a class that represents a UI page or control. It encapsulates all Playwright interactions for that page so tests stay readable and free of low-level browser code.

| Proxy | Represents | Disposal |
|---|---|---|
| `BrowserProxy` (`b`) | The browser session | Disposed by `BrowseAsync()` |
| `FramePageProxy<T>` | An entity edit page | Async disposal via `IAsyncDisposable` |
| `SearchPageProxy` | A search / list page | Async disposal via `IAsyncDisposable` |
| `FrameModalProxy<T>` | An entity edit modal | Closes the modal on disposal |
| `MessageModalProxy` | A confirmation dialog | Closes the modal on disposal |
| Custom (e.g. `BoardPageProxy`) | Domain-specific page/widget | Custom async disposal logic |

### Async/Await Patterns — Understanding the Differences

With Playwright, all UI interactions are asynchronous. Signum provides extension methods that combine awaiting a `Task<T>` with resource disposal, making test code clean and maintainable.

#### Pattern 1: `Await_UsingAsync` / `Await_EndUsingAsync` (Most Common)

Use these when a method **returns a `Task<T>`** where `T` is `IDisposable`:

```cs
await BrowseAsync("System", async b =>
{
    // SearchPageAsync returns Task<SearchPageProxy>
    // SearchPageProxy implements IDisposable
    await b.SearchPageAsync(typeof(ProjectEntity))
        .Await_EndUsingAsync(async search =>
        {
            // Create returns Task<FrameModalProxy<ProjectEntity>>
            await search.CreateModalAsync<ProjectEntity>()
                .Await_EndUsingAsync(async project =>
                {
                    await project.AutoLineValueAsync(p => p.Name, "Agile360");
                    await project.ExecuteAsync(ProjectOperation.Save);
                    await project.OkWaitClosedAsync();
                });

            await search.Results.WaitRowsAsync(1);
        });
});
```

**What it does:**
1. Awaits the `Task<T>` to get the disposable resource
2. Executes your async lambda
3. Calls `DisposeAsync()` if `T` implements `IAsyncDisposable`, otherwise calls `Dispose()`
4. Ensures disposal even if exceptions occur

**When to use:**
- The method returns `Task<SearchPageProxy>`, `Task<FramePageProxy<T>>`, or any `Task<T>` where `T : IDisposable`
- You need to work with the proxy and ensure it's disposed when done

**Variants:**
- `Await_EndUsingAsync` — returns no value, just runs the action
- `Await_UsingAsync` — returns a value from the lambda for further chaining

#### Pattern 2: `EndUsingAsync` / `UsingAsync` (Direct Disposal)

Use these when you **already have the disposable object** (not a Task):

```cs
// When you have the proxy directly (synchronous creation)
var page = await b.FramePageAsync<ProjectEntity>();
await page.EndUsingAsync(async p =>
{
    await p.AutoLineValueAsync(p => p.Name, "Test");
    await p.ExecuteAsync(ProjectOperation.Save);
});
```

**What it does:**
1. Takes the disposable directly (no awaiting)
2. Executes your async lambda
3. Calls `DisposeAsync()` or `Dispose()` after completion

**When to use:**
- You've already awaited the Task and have the proxy
- Less common in tests — prefer `Await_EndUsingAsync` for cleaner chaining

#### Pattern 3: `await using` (C# Built-in)

Standard C# async disposal pattern:

```cs
await BrowseAsync("System", async b =>
{
    await using var search = await b.SearchPageAsync(typeof(ProjectEntity));
    await search.Results.WaitRowsAsync(1);

    // search.DisposeAsync() called automatically at end of scope
});
```

**When to use:**
- When you need the proxy for multiple statements in a scope
- Equivalent to `using` but calls `DisposeAsync()` instead of `Dispose()`

#### Pattern 4: `Await_DoAsync` (No Disposal)

Use when a method returns `Task<T>` but `T` is **NOT disposable**, and you want to perform actions on `T`:

```cs
await fall.EntityTable(f => f.Fahrten).CreateRowAsync<FahrtEntity>()
    .Await_DoAsync(async row =>
    {
        await row.AutoLineValueAsync(a => a.Datum, Clock.Today);
        await row.AutoLineValueAsync(a => a.Strecke, 10);
    });
```

**What it does:**
1. Awaits the `Task<T>`
2. Executes your lambda with the result
3. Returns the result (no disposal)

**When to use:**
- Working with row proxies, line proxies, or other non-disposable objects
- You need to perform async operations on the awaited result

### Code Structure Mirrors Page Structure

The nesting of async disposal blocks shows exactly which page or modal each line is operating on:

```cs
await BrowseAsync("System", async b =>
{
    await b.SearchPageAsync(typeof(ProjectEntity))          // Task<SearchPageProxy>
        .Await_EndUsingAsync(async search =>                 // on the search page
        {
            await search.CreateModalAsync<ProjectEntity>()   // Task<FrameModalProxy<T>>
                .Await_EndUsingAsync(async project =>        //   modal opened
                {
                    await project.AutoLineValueAsync(p => p.Name, "Agile360");
                    await project.ExecuteAsync(ProjectOperation.Save);
                    await project.OkWaitClosedAsync();
                });                                           //   modal closed

            await search.Results.WaitRowsAsync(1);
        });                                                   // search page left
});
```

### Built-in Page Navigation

```cs
// Navigate to entity page
await b.FramePageAsync<ProjectEntity>()
    .Await_EndUsingAsync(async page => { /* ... */ });

// Navigate to search page
await b.SearchPageAsync(typeof(ProjectEntity))
    .Await_EndUsingAsync(async search => { /* ... */ });

// Navigate to specific entity
await b.FramePageAsync(entity.ToLite())
    .Await_EndUsingAsync(async page => { /* ... */ });
```

### Custom Proxies — Proxy First, Test Second

When a page has domain-specific UI (drag-and-drop, custom widgets, etc.), **always build the proxy first, then write the test**. Raw Playwright calls must live inside the proxy, never in the test method.

**Step 1 — Build the proxy:**
```cs
public class BoardPageProxy : IAsyncDisposable
{
    public IPage Page { get; }
    public BoardPageProxy(IPage page) => Page = page;

    public async Task MoveTaskToColumnAsync(Lite<TaskEntity> task, BoardColumn column)
    {
        // Playwright details stay here, inside the proxy
        var card = Page.Locator($"[data-task='{task.Id}']");
        var col = Page.Locator($"[data-column='{column}']");
        await card.DragToAsync(col);
    }

    public async Task WaitTaskInColumnAsync(Lite<TaskEntity> task, BoardColumn column)
    {
        var card = Page.Locator($"[data-task='{task.Id}']");
        var col = Page.Locator($"[data-column='{column}']");
        await Page.WaitAsync(async () => 
            await card.IsVisibleAsync() && 
            await col.Locator($"[data-task='{task.Id}']").IsVisibleAsync());
    }

    public async ValueTask DisposeAsync() { }
}
```

**Step 2 — Write the test using only proxy methods:**
```cs
[Fact]
public async Task MoveTaskToDoneAsync()
{
    var task = CreateTask("Fix bug");
    await BrowseAsync("System", async b =>
    {
        await using var board = new BoardPageProxy(b.Page);
        await b.Page.GotoAsync(board.Url(sprintId));
        await board.MoveTaskToColumnAsync(task.ToLite(), BoardColumn.Done);
        await board.WaitTaskInColumnAsync(task.ToLite(), BoardColumn.Done);
    });
}
```

The test reads like a specification — not like a Playwright script.

---

## 5. Interacting with Form Lines

Inside a `FramePageProxy<T>`, access form fields via typed expression helpers. **All methods are async:**

```cs
await page.Await_EndUsingAsync(async p =>
{
    // Read / write any field generically
    await p.AutoLineValueAsync(m => m.Name, "New value");
    var name = await p.AutoLineValueAsync(m => m.Name);

    // Specific line types when you need more control
    await p.TextBoxLine(m => m.Description).Element.FillAsync("text");
    await p.EntityLine(m => m.Project).SetLiteValueAsync(project.ToLite());
    await p.EnumLine(m => m.State).SetEnumValueAsync(TaskState.Done);

    // Work with entity collections
    await p.EntityTable(m => m.Tags).CreateRowAsync<TagEntity>()
        .Await_DoAsync(async tag =>
        {
            await tag.AutoLineValueAsync(t => t.Name, "Backend");
        });

    // Navigate to an embedded entity
    await p.EntityDetail(m => m.Address).Details<AddressEntity>()
        .EndUsingAsync(async addr =>
        {
            await addr.AutoLineValueAsync(a => a.City, "Barcelona");
        });

    // Save
    await p.ExecuteAsync(TaskOperation.Save);
});
```

**Key points:**
- Prefer `AutoLineValueAsync()` for simple reads/writes
- All interactions are awaited
- Use `.Await_DoAsync()` for non-disposable line operations
- Use `.EndUsingAsync()` for disposable nested entities

---

## 6. Search Pages

```cs
await b.SearchPageAsync(typeof(ProjectEntity))
    .Await_EndUsingAsync(async search =>
    {
        await search.SearchControl.SearchAsync();
        await search.Results.WaitRowsAsync(3);

        // Open entity from results
        await search.Results.EntityClickInPlaceAsync(project.ToLite())
            .Await_EndUsingAsync(async page => { /* ... */ });

        // Create new from search page
        await search.CreateModalAsync<ProjectEntity>()
            .Await_EndUsingAsync(async modal => { /* ... */ });

        // Add filters
        await search.Filters.AddFilterAsync(...);
    });
```

---

## 7. Modals

**Capture a modal opened by a button click:**

```cs
await page.OperationClickCaptureAsync(MyOperation.DoSomething)
    .Await_EndUsingAsync(async modal => { /* ... */ });
```

**Entity creation / edit in a modal:**

```cs
await combo.CreateModalAsync<TagEntity>()
    .Await_EndUsingAsync(async tag =>
    {
        await tag.AutoLineValueAsync(t => t.Name, "Backend");
        await tag.ExecuteAsync(TagOperation.Save);
        await tag.OkWaitClosedAsync();
    });
```

**Confirmation dialogs:**

```cs
await deleteButton.CaptureOnClickAsync()
    .Await_DoAsync(modal => 
        new MessageModalProxy(modal).ClickWaitCloseAsync(MessageModalButton.Yes));
```

**Selector modals:**

```cs
await createButton.CaptureOnClickAsync()
    .Await_EndUsingAsync(modal => 
        modal.AsSelectorModal().SelectAsync(SomeType.Value));
```

**Error modals** are detected automatically: if a modal with error content appears while the test is waiting, it is thrown as a `TimeoutException` with the title and body text included. You usually don't need to handle these explicitly — the test will fail with a descriptive message.

---

## 8. Waiting — Never Use `Task.Delay` or `Thread.Sleep`

Always use Signum's async `Wait` utilities. They poll every 200 ms up to a 20 s timeout and include a meaningful description in the timeout exception:

```cs
// Wait for an element
await page.Locator(".my-class").WaitVisibleAsync();
await page.Locator(".spinner").WaitNotVisibleAsync();

// Wait for a condition
await page.WaitAsync(async () => 
    await someElement.GetAttributeAsync("data-loaded") == "true",
    description: "data-loaded attribute to be true");

// Wait for an element to be present
await page.Locator(".content").WaitPresentAsync();
```

**Playwright advantages:**
- Built-in auto-waiting for most actions (click, fill, etc.)
- More reliable element detection
- Better error messages with selector chains

---

## 9. Debugging Test Failures

Work through this checklist in order:

1. **Read the exception message.** Error modals are captured automatically and their content appears in the exception. Usually this is enough.

2. **Check Playwright traces.** Playwright can record traces with screenshots, network activity, and DOM snapshots. Enable tracing in your test setup:
   ```cs
   await context.Tracing.StartAsync(new() 
   {
       Screenshots = true,
       Snapshots = true
   });
   // Run test
   await context.Tracing.StopAsync(new() 
   {
       Path = "trace.zip"
   });
   ```
   Then view with: `playwright show-trace trace.zip`

3. **Use headed mode.** Set the `PLAYWRIGHT_HEADLESS` environment variable to `false` to see the browser:
   ```cs
   Environment.SetEnvironmentVariable("PLAYWRIGHT_HEADLESS", "false");
   ```

4. **Pause execution.** Add `await page.PauseAsync()` to pause the test and inspect the browser interactively.

5. **Add a breakpoint.** Set a breakpoint just before the failing line and inspect the page state in the debugger.

---

## 10. Async/Await Best Practices

### ✅ Do:
- Always use `async Task` for test methods (never `async void`)
- Always `await` Playwright operations — never `.Wait()` or `.Result`
- Use `Await_EndUsingAsync` when disposing proxies returned from async methods
- Use `Await_DoAsync` for non-disposable async operations
- Keep async lambdas (`async () => { }` or `async x => { }`) 

### ❌ Don't:
- Don't use synchronous disposal (`using` or `EndUsing`) with async methods
- Don't mix `.Wait()` or `.Result` — causes deadlocks
- Don't use `Thread.Sleep` — use `await Task.Delay()` if absolutely necessary (prefer `WaitAsync`)
- Don't forget to `await` — unawaited tasks may not execute

### Common Patterns

**Opening and closing modals:**
```cs
// Modal that you interact with
await button.CaptureOnClickAsync()
    .Await_EndUsingAsync(async modal =>
    {
        await modal.DoSomethingAsync();
        await modal.OkWaitClosedAsync();
    });

// Confirmation dialog (no interaction needed)
await button.CaptureOnClickAsync()
    .Await_DoAsync(modal =>
        new MessageModalProxy(modal).ClickWaitCloseAsync(MessageModalButton.Yes));
```

**Working with entity tables:**
```cs
await page.EntityTable(f => f.Items).CreateRowAsync<ItemEntity>()
    .Await_DoAsync(async row =>
    {
        await row.AutoLineValueAsync(i => i.Name, "Item 1");
        await row.AutoLineValueAsync(i => i.Quantity, 5);
    });

await page.EntityTable(f => f.Items).LastRowAsync<ItemEntity>()
    .Await_DoAsync(async row =>
    {
        await row.AutoLineValueAsync(i => i.Price, 100);
    });
```

**Nested proxies:**
```cs
await b.SearchPageAsync(typeof(ProjectEntity))
    .Await_EndUsingAsync(async search =>
    {
        await search.Results.EntityClickInPlaceAsync(project.ToLite())
            .Await_EndUsingAsync(async page =>
            {
                await page.EntityDetail(p => p.Address).Details<AddressEntity>()
                    .EndUsingAsync(async addr =>
                    {
                        await addr.AutoLineValueAsync(a => a.City, "Madrid");
                    });

                await page.ExecuteAsync(ProjectOperation.Save);
            });
    });
```
