using Microsoft.Playwright;
using Signum.Basics;

namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for multi-line text input (TextArea)
/// Equivalent to Selenium's TextAreaLineProxy
/// </summary>
public class TextAreaLineProxy : TextBoxBaseLineProxy
{
    public TextAreaLineProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    protected override ILocator InputLocator => Element.Locator("textarea.form-control, textarea");
}
