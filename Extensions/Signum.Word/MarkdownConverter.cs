using Markdig;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Renderers.Html;

namespace Signum.Word;

public static class MarkdownConverter
{
    private static readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    public static string MarkdownToHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;

        string html = Markdown.ToHtml(markdown, pipeline);

        html = FormatHtmlForWord(html);

        return html;
    }

    public static IEnumerable<OpenXmlElement> MarkdownToWord(string markdown, WordTemplateParameters parameters)
    {
        if (string.IsNullOrEmpty(markdown))
            return new List<OpenXmlElement>() { new Paragraph() };

        string html = MarkdownToHtml(markdown);

        return HtmlToWordConverter.HtmlToWord(html, parameters);
    }

    private static string FormatHtmlForWord(string html)
    {
        // Bereinige und formatiere HTML für optimale Word-Konvertierung
        return html
            .Replace("<p></p>", "")
            .Replace("<ul>", "\r\n<ul>")
            .Replace("</ul>", "</ul>\r\n")
            .Replace("<ol>", "\r\n<ol>")
            .Replace("</ol>", "</ol>\r\n")
            .Replace("<pre><code>", "<p>")
            .Replace("</code></pre>", "</p>")
            .Replace("<code>", "<strong>")
            .Replace("</code>", "</strong>")
            .Replace("<h1>", "\r\n<h1>")
            .Replace("<h2>", "\r\n<h2>")
            .Replace("<h3>", "\r\n<h3>")
            .Replace("<h4>", "\r\n<h4>")
            .Replace("\n", "");
    }
}

public class MyParagraphExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        renderer.ObjectRenderers.RemoveAll(x => x is ParagraphRenderer);
        renderer.ObjectRenderers.Add(new MyParagraphRenderer());
    }
}

public class MyParagraphRenderer : ParagraphRenderer
{
    protected override void Write(HtmlRenderer renderer, ParagraphBlock obj)
    {
        if (obj.Parent is MarkdownDocument)
        {
            if (!renderer.IsFirstInContainer)
            {
                renderer.EnsureLine();
            }
            renderer.WriteLeafInline(obj);
            if (!renderer.IsLastInContainer)
            {
                renderer.WriteLine("<br />");
                renderer.WriteLine("<br />");
            }
            else
            {
                renderer.EnsureLine();
            }
        }
        else
        {
            base.Write(renderer, obj);
        }
    }
}
