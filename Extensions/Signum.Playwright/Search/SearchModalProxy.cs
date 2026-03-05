using Microsoft.Playwright;

namespace Signum.Playwright.Search;

public class SearchModalProxy : ModalProxy
{
    public SearchControlProxy SearchControl { get; }

    public SearchModalProxy(IPage page) : base(page)
    {
        SearchControl = new SearchControlProxy(Modal.Locator(".sf-search-control"), page);
    }

    public SearchModalProxy(IPage page, ILocator modalElement) : base(modalElement, page)
    {
        SearchControl = new SearchControlProxy(Modal.Locator(".sf-search-control"), page);
    }

    public async Task<ModalProxy> SelectAndViewAsync(int rowIndex)
    {
        var row = SearchControl.Results.GetRow(rowIndex);
        
        return await ModalProxy.CaptureAsync(Page, async () =>
        {
            await row.ClickAsync();
        });
    }

    public async Task SelectRowAsync(int rowIndex)
    {
        var row = SearchControl.Results.GetRow(rowIndex);
        await row.ClickAsync();
        await OkAsync();
    }

    public async Task<Lite<Entity>?> SelectLiteAsync(int rowIndex)
    {
        var row = SearchControl.Results.GetRow(rowIndex);
        var entityKey = await row.GetAttributeAsync("data-entity-key");
        
        await row.ClickAsync();
        await OkAsync();

        if (string.IsNullOrEmpty(entityKey))
            return null;

        var parts = entityKey.Split(';');
        if (parts.Length < 2)
            return null;

        var type = Type.GetType(parts[0]);
        var id = PrimaryKey.Parse(parts[1], type!);
        var text = await row.TextContentAsync();

        return Lite.Create(type!, id, text);
    }
}

public static class SearchModalExtensions
{
    public static SearchModalProxy AsSearchModal(this ModalProxy modal)
    {
        return new SearchModalProxy(modal.Page, modal.Modal);
    }
}
