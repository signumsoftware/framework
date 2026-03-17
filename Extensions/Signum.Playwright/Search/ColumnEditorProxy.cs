namespace Signum.Playwright.Search;

public class ColumnEditorProxy
{
    public ILocator Element { get; }

    public ColumnEditorProxy(ILocator element)
    {
        Element = element;
    }

    public async Task CloseAsync()
    {
        await Element.Locator(".button.close").ClickAsync();
    }

    public QueryTokenBuilderProxy QueryToken => new QueryTokenBuilderProxy(Element.Locator(".sf-query-token-builder"));
    public ILocator Name => Element.Locator("input.form-control");
}
