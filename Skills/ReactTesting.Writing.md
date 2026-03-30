# React UI Testing

UI tests use **xUnit.v3** + **Playwright** with Signum proxy abstractions.

## Test Class Structure

Every test class should use a shared base (like `SouthwindTestClass`) and call `BrowseAsync`:

```cs
public class YourTestClass : SouthwindTestClass
{
    [Fact]
    public async Task SomeTestAsync()
    {
        // Arrange data
        await BrowseAsync("System", async b =>
        {
            // b is SouthwindBrowser
            // Act + Assert through proxies
        });
    }
}
```

Keep test methods as `async Task`.

## Proxy classes

A **proxy** wraps Playwright interactions for a page/control and exposes domain actions.

- Keep raw selectors and low-level interactions inside proxies.
- For complex UIs, create a **custom proxy first**, then write the test.
- Tests should read as business flows, not Playwright scripts.

## BrowserProxy

`BrowserProxy` is the root proxy for browser/session navigation.

Typical responsibilities:
- `LoginAsync` / `LogoutAsync`
- Navigate to search pages: `SearchPageAsync(...)`
- Navigate to entity pages: `FramePageAsync<T>(...)`
- Route helpers: `FindRoute(...)`, `NavigateRoute(...)`

Inherit from `BrowserProxy` in your app (`SouthwindBrowser`) and add custom app navigation methods.

## Modal Proxies

`ModalProxy` is the base modal proxy for Bootstrap modals.

It implements `IAsyncDisposable` (and `IDisposable`) so modal lifecycle/close behavior is handled automatically.

### Standard Modal Proxies

Common modal proxies:
- `FrameModalProxy<T>`: entity frame modal (lines + operations)
- `SearchModalProxy`: search control modal
- `SelectorModalProxy`: type/value selection modal
- `AutoLineModalProxy`: dynamic single-line modal
- `MessageModalProxy`: confirmation/info modal
- `ErrorModalProxy`: error modal

You can create strongly-typed modal proxies from an `ILocator` using `NewAsync(...)`.
`NewAsync` waits for the modal main content to load before returning.

Example:

```cs
ILocator locator = await page.CaptureModalAsync(() => someButton.ClickAsync());
var modal = await FrameModalProxy<OrderEntity>.NewAsync(locator);
```

### Capturing Proxies

Start with the generic capture primitive:

- `CaptureModalAsync(this IPage, Func<Task>)`: executes an action and captures the newly opened modal (`ILocator`).

Convenience capture methods:
- `CaptureOnClickAsync()`
- `CaptureOnDoubleClickAsync()`
- `OperationClickCaptureAsync(...)`

Methods that already return strongly-typed proxies (typical flows):
- `EntityBaseProxy.ViewAsync(...)`
- `EntityBaseProxy.CreateModalAsync<T>()`
- `SearchControlProxy.CreateAsync<T>()`
- `ResultTableProxy.EntityClickAsync<T>(...)`
- `EntityButtonContainerExtensions.ConstructFromAsync<F, T>(...)`

### Async Workflows (UsingAsync, EndUsingAsync, Await_UsingAsync, Await_EndUsingAsync)

Use these helpers to keep disposal safe and express modal nesting clearly.

1. `EndUsingAsync`: takes an existing disposable proxy and returns nothing.
2. `UsingAsync`: takes an existing disposable proxy and returns a value/proxy.
3. `Await_EndUsingAsync`: same as `EndUsingAsync` but starts from `Task<TDisposable>`.
4. `Await_UsingAsync`: same as `UsingAsync` but starts from `Task<TDisposable>`.

Also use modal casting helpers when you capture raw `ILocator`:
- `Await_AsSearchModal(...)`
- `Await_AsFrameModal<T>(...)`

Why preferred:
- block/lambda nesting mirrors modal levels
- easy to jump modal-to-modal in wizard-like flows
- disposal is guaranteed

Guideline: prefer these helpers over `using` in modal-heavy tests. 
AVOID `using var ...` without an explicit block (Dispose iscalled too late).

Example:

```cs
await page.OperationClickCaptureAsync(OrderOperation.Ship)
    .Await_AsSearchModal()
    .Await_EndUsingAsync(async searchModal =>
    {
        await searchModal.SearchAsync();

        await searchModal.Results.EntityClickAsync<OrderEntity>(0)
            .Await_EndUsingAsync(async orderModal =>
            {
                await orderModal.OperationClickCaptureAsync(OrderOperation.Save)
                    .Await_AsFrameModal<OrderEntity>()
                    .Await_EndUsingAsync(async nextModal =>
                    {
                        await nextModal.OkWaitClosedAsync();
                    });
            });
    });
```

## Writing and reading entity data

`ILineContainer` / `ILineContainer<T>` represent a UI container with typed lines.

Core members:
- `Element`: root locator for line resolution
- `Route`: `PropertyRoute` used for strongly-typed navigation

Common implementations:
- `FramePageProxy<T>`
- `FrameModalProxy<T>`
- `LineContainer<T>`
- `EntityTableRow<T>`

`LineContainerExtensions` adds strongly-typed line access:
- `TextBoxLine(...)`, `EntityLine(...)`, `EntityTable(...)`, etc.
- `AutoLine(...)` for automatic proxy selection
- `AutoLineValueAsync(...)` strongly-typed get/set

`BaseLineProxy` is the root abstraction for all line proxies.

Main concrete line proxies include:
- `CheckboxLineProxy`, `DateTimeLineProxy`, `EnumLineProxy`, `FileLineProxy`, `GuidBoxLineProxy`, `HtmlEditorLineProxy`, `NumberLineProxy`, `TextAreaLineProxy`, `TimeLineProxy`
- plus entity-related and text-related hierarchies (`EntityBaseProxy`, `TextBaseLineProxy`)

Example:

```cs
await b.FramePageAsync<OrderEntity>()
    .Await_EndUsingAsync(async order =>
    {
        await order.AutoLineValueAsync(o => o.Reference, "SO-1001");
        await order.AutoLineValueAsync(o => o.OrderDate, Clock.Now);

        var reference = await order.AutoLineValueAsync(o => o.Reference);

        await order.EntityTable(o => o.Details).CreateRowAsync<OrderDetailEmbedded>()
            .Await_DoAsync(async row =>
            {
                await row.AutoLineValueAsync(d => d.Quantity, 5);
            });
    });
```

## Executing operations

`IEntityButtonContainer` / `IEntityButtonContainer<T>` represent proxies with operation buttons.

`EntityButtonContainerExtensions` provides helpers like:
- `OperationButtonAsync(...)`, `OperationEnabledAsync(...)`
- `OperationClickAsync(...)`, `OperationClickCaptureAsync(...)`
- `ExecuteAsync(...)`, `DeleteAsync(...)`
- `ConstructFromAsync<F, T>(...)`

Example:

```cs
await b.FramePageAsync<OrderEntity>(order.ToLite())
    .Await_EndUsingAsync(async page =>
    {
        await page.ExecuteAsync(OrderOperation.Save);

        await page.ConstructFromAsync<OrderEntity, InvoiceEntity>(OrderOperation.InvoiceFrom)
            .Await_EndUsingAsync(async invoiceModal =>
            {
                await invoiceModal.OkWaitClosedAsync();
            });
    });
```

## Writing Custom Proxies

Proxy classes should have a comment pointing to the equivalent `.tsx` file.

There are typically three ways of writing custome proxies and making them discoverable and strongly-typed: 

### 1. Custom Page Proxies

Add methods to `SouthwindBrowser` for page-level proxies. Example:

```cs
public class SouthwindBrowser : BrowserProxy
{
    public SouthwindBrowser(IPage page) : base(page) { }

    public async Task<SalesDashboardPageProxy> SalesDashboardAsync()
    {
        await Page.GotoAsync(Url("sales/dashboard"));
        return await SalesDashboardPageProxy.NewAsync(Page);
    }
}

// Proxy for SalesDashboardPage.tsx
public class SalesDashboardPageProxy : IAsyncDisposable
{
    public IPage Page { get; }
    SalesDashboardPageProxy(IPage page) { Page = page; }

    public static async Task<SalesDashboardPageProxy> NewAsync(IPage page)
    {
        await page.Locator(".sales-dashboard").WaitVisibleAsync();
        return new SalesDashboardPageProxy(page);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

### 2. Custom Component Proxies

Often add extension methods on `ILineContainer<SomeEntity>` (or `SearchControlProxy`).

```cs
// Proxy for InvoiceCalculations.tsx
public class CalculationsProxy
{
    public ILocator Element { get; }
    public CalculationsProxy(ILocator element) { Element = element; }

    public Task<string?> GetTotalAsync()
        => Element.Locator(".calc-total").TextContentAsync();

    public async Task ApplyDiscountAsync(string percent)
    {
        await Element.Locator(".calc-discount").FillAsync(percent);
        await Element.Locator(".calc-apply").ClickAsync();
    }
}

public static class InvoiceLineContainerExtensions
{
    public static CalculationsProxy Calculations(this ILineContainer<InvoiceEntity> c)
        => new CalculationsProxy(c.Element.Locator(".invoice-calculations"));
}
```

### 3. Simple Extension Methods

Example (simple case: extension only, no new proxy class):

```cs
public static class InvoiceSimpleExtensions
{
    public static async Task RecalculateAsync(this ILineContainer<InvoiceEntity> c)
    {
        await c.Element.Locator("button.sf-recalculate").ClickAsync();
    }
}
```
