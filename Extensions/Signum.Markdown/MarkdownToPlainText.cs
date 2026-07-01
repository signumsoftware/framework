using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Linq;
using System.Text;

namespace Signum.Markdown;

public static class MarkdownToPlainText
{
    static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().Build();

    public static string? MarkdownToText(string? markdown)
    {
        if (markdown == null)
            return null;

        var document = Markdig.Markdown.Parse(markdown, Pipeline);
        var sb = new StringBuilder();
        ProcessBlock(document, sb, insideListItem: false);
        return sb.ToString().Trim();
    }

    static void ProcessBlock(Block block, StringBuilder sb, bool insideListItem)
    {
        switch (block)
        {
            case MarkdownDocument doc:
                foreach (var child in doc)
                    ProcessBlock(child, sb, insideListItem);
                break;

            case ParagraphBlock para:
                ProcessInlines(para.Inline, sb);
                if (!insideListItem)
                    sb.AppendLine();
                break;

            case HeadingBlock heading:
                ProcessInlines(heading.Inline, sb);
                sb.AppendLine();
                break;

            case ListBlock list:
                int index = 1;
                foreach (var item in list.Cast<ListItemBlock>())
                {
                    sb.Append(list.IsOrdered ? $"{index++}. " : "- ");
                    foreach (var child in item)
                        ProcessBlock(child, sb, insideListItem: true);
                    sb.AppendLine();
                }
                break;

            case ContainerBlock container:
                foreach (var child in container)
                    ProcessBlock(child, sb, insideListItem);
                break;
        }
    }

    static void ProcessInlines(ContainerInline? inlines, StringBuilder sb)
    {
        if (inlines == null)
            return;

        foreach (var inline in inlines)
            ProcessInline(inline, sb);
    }

    static void ProcessInline(Markdig.Syntax.Inlines.Inline inline, StringBuilder sb)
    {
        switch (inline)
        {
            case LiteralInline literal:
                sb.Append(literal.Content.ToString());
                break;

            case LineBreakInline:
                sb.AppendLine();
                break;

            case CodeInline code:
                sb.Append(code.Content);
                break;

            case ContainerInline container:
                foreach (var child in container)
                    ProcessInline(child, sb);
                break;
        }
    }
}
