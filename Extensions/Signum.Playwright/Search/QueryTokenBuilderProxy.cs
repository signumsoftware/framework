namespace Signum.Playwright.Search;

/// <summary>
/// Proxy for QueryTokenBuilder.tsx
/// </summary>
public class QueryTokenBuilderProxy
{
    public ILocator Element { get; }

    public QueryTokenBuilderProxy(ILocator element)
    {
        Element = element;
    }

    public async Task<string?> FullKeyAsync()
    {
        return await Element.GetAttributeAsync("data-token");
    }

    public ILocator TokenElement(int tokenIndex)
    {
        return Element.Locator($".sf-query-token-part:nth-child({tokenIndex + 1})");
    }

    public async Task SelectTokenAsync(string token)
    {
        string[] parts = token.Split('.');

        for (int i = 0; i < parts.Length; i++)
        {
            var prev = string.Join(".", parts.Take(i));

            var qt = new QueryTokenPartProxy(TokenElement(i));
            await qt.SelectAsync(string.Join(".", parts.Take(i + 1)));
        }
    }
}
