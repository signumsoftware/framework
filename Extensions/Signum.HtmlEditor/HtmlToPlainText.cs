using HtmlAgilityPack;
using System.Linq;
using System.Text;

namespace Signum.HtmlEditor;

public static class HtmlToPlainText
{
    public static string? HtmlToText(string? html)
    {
        if (html == null)
            return null;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var sb = new StringBuilder();
        ProcessNode(doc.DocumentNode, sb);
        return sb.ToString().Trim();
    }

    static void ProcessNode(HtmlNode node, StringBuilder sb)
    {
        switch (node.Name.ToLowerInvariant())
        {
            case "#text":
                sb.Append(HtmlEntity.DeEntitize(node.InnerText));
                break;

            case "br":
                sb.AppendLine();
                break;

            case "p":
            case "div":
            case "h1":
            case "h2":
            case "h3":
            case "h4":
            case "h5":
            case "h6":
                foreach (var child in node.ChildNodes)
                    ProcessNode(child, sb);
                sb.AppendLine();
                break;

            case "ul":
                foreach (var li in node.ChildNodes.Where(n => n.Name == "li"))
                {
                    sb.Append("- ");
                    foreach (var child in li.ChildNodes)
                        ProcessNode(child, sb);
                    sb.AppendLine();
                }
                break;

            case "ol":
                int i = 1;
                foreach (var li in node.ChildNodes.Where(n => n.Name == "li"))
                {
                    sb.Append($"{i++}. ");
                    foreach (var child in li.ChildNodes)
                        ProcessNode(child, sb);
                    sb.AppendLine();
                }
                break;

            default:
                foreach (var child in node.ChildNodes)
                    ProcessNode(child, sb);
                break;
        }
    }
}
