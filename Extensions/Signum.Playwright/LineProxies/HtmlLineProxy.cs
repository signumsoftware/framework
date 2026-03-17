namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for HTML editor controls
/// Equivalent to Selenium's HtmlLineProxy
/// </summary>
public class HtmlLineProxy : BaseLineProxy
{
    public HtmlLineProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    // HTML editors typically use iframe or contenteditable
    protected ILocator InputLocator => Element.Locator(".html-editor, [contenteditable='true'], iframe");

    public override async Task SetValueUntypedAsync(object? value)
    {
        await SetHtmlAsync(value?.ToString());
    }

    public override async Task<object?> GetValueUntypedAsync()
    {
        return await GetHtmlAsync();
    }

    public override async Task<bool> IsReadonlyAsync()
    {
        var editor = InputLocator.First;
        
        // Check if it's an iframe (like TinyMCE)
        var tagName = await editor.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
        
        if (tagName == "iframe")
        {
            var iframe = Page.FrameLocator(await editor.GetAttributeAsync("name") ?? "");
            var body = iframe.Locator("body");
            var isEditable = await body.EvaluateAsync<bool>("el => el.contentEditable === 'true'");
            return !isEditable;
        }
        else
        {
            // Contenteditable element
            var isEditable = await editor.EvaluateAsync<bool>("el => el.contentEditable === 'true'");
            return !isEditable;
        }
    }

    /// <summary>
    /// Set HTML content
    /// </summary>
    public async Task SetHtmlAsync(string? html)
    {
        var editor = InputLocator.First;
        var tagName = await editor.EvaluateAsync<string>("el => el.tagName.toLowerCase()");

        if (tagName == "iframe")
        {
            // Handle iframe-based editor (like TinyMCE)
            var frameName = await editor.GetAttributeAsync("name");
            var iframe = Page.FrameLocator(frameName ?? "");
            var body = iframe.Locator("body");
            
            await body.EvaluateAsync($"el => el.innerHTML = {System.Text.Json.JsonSerializer.Serialize(html ?? "")}");
        }
        else
        {
            // Handle contenteditable element
            await editor.EvaluateAsync($"el => el.innerHTML = {System.Text.Json.JsonSerializer.Serialize(html ?? "")}");
        }
    }

    /// <summary>
    /// Get HTML content
    /// </summary>
    public async Task<string?> GetHtmlAsync()
    {
        var editor = InputLocator.First;
        var tagName = await editor.EvaluateAsync<string>("el => el.tagName.toLowerCase()");

        if (tagName == "iframe")
        {
            // Handle iframe-based editor
            var frameName = await editor.GetAttributeAsync("name");
            var iframe = Page.FrameLocator(frameName ?? "");
            var body = iframe.Locator("body");
            
            return await body.InnerHTMLAsync();
        }
        else
        {
            // Handle contenteditable element
            return await editor.InnerHTMLAsync();
        }
    }

    /// <summary>
    /// Get plain text content (HTML stripped)
    /// </summary>
    public async Task<string?> GetTextAsync()
    {
        var editor = InputLocator.First;
        var tagName = await editor.EvaluateAsync<string>("el => el.tagName.toLowerCase()");

        if (tagName == "iframe")
        {
            var frameName = await editor.GetAttributeAsync("name");
            var iframe = Page.FrameLocator(frameName ?? "");
            var body = iframe.Locator("body");
            
            return await body.TextContentAsync();
        }
        else
        {
            return await editor.TextContentAsync();
        }
    }

    /// <summary>
    /// Insert text at cursor position
    /// </summary>
    public async Task InsertTextAsync(string text)
    {
        var editor = InputLocator.First;
        await editor.PressSequentiallyAsync(text);
    }
}
