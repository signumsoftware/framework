namespace Signum.Playwright.LineProxies;

public class FileLineProxy : BaseLineProxy
{
    public ILocator Element { get; }
    public IPage Page { get; }

    public FileLineProxy(ILocator element, IPage page, PropertyRoute route)
        : base(element, route, page)
    {
        this.Element = element;
        this.Page = page;
    }

    private ILocator FileElement => Element.Locator("input[type=file]");

    public override Task<object?> GetValueUntypedAsync()
        => throw new NotImplementedException("File inputs do not support reading value for security reasons.");

    public override async Task SetValueUntypedAsync(object? value)
    {
        if (value is string path)
        {
            await SetPathAsync(path);
            return;
        }

        throw new InvalidOperationException("FileLineProxy expects a file path string.");
    }

    public override async Task<bool> IsReadonlyAsync()
    {
        await FileElement.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached
        });

        return await FileElement.IsDisabledAsync();
    }

    public async Task SetPathAsync(string path)
    {
        await FileElement.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });

        await FileElement.SetInputFilesAsync(path);

        // Optional: warten bis Upload abgeschlossen oder Input verschwindet
        await FileElement.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Detached
        });
    }
}
