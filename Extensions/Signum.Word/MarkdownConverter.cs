using Markdig;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Signum.Word;

public static class MarkdownConverter
{
    private static readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions().Build();

    public static string MarkdownToHtml(string markdown)
    {
        if(string.IsNullOrEmpty(markdown))
            return string.Empty;

        string html = Markdown.ToHtml(markdown, pipeline);

        html = FormatHtmlForWord(html);

        return html;
    }

    public static IEnumerable<OpenXmlElement> MarkdownToWord(string markdown, WordTemplateParameters parameters)
    {
        if(string.IsNullOrEmpty(markdown))
            return new List<OpenXmlElement>() { new Paragraph() };
        
        string html = MarkdownToHtml(markdown);

        return HtmlToWordConverter.HtmlToWord(html, parameters);
    }

    private static string FormatHtmlForWord(string html)
    {
        // Bereinige und formatiere HTML für optimale Word-Konvertierung
        return html
            .Replace("<p></p>", "")
            .Replace("<ul>", "\n<ul>")
            .Replace("</ul>", "</ul>\n")
            .Replace("<ol>", "\n<ol>")
            .Replace("</ol>", "</ol>\n")
            .Replace("<pre><code>", "<p>")
            .Replace("</code></pre>", "</p>")
            .Replace("<code>", "<strong>")
            .Replace("</code>", "</strong>")
            .Replace("<h1>", "\n<h1>")
            .Replace("<h2>", "\n<h2>")
            .Replace("<h3>", "\n<h3>")
            .Replace("<h4>", "\n<h4>");
    }
}
