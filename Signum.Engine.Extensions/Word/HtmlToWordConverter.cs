using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Word;

public static class HtmlToWordConverter
{
    public static IEnumerable<OpenXmlElement> HtmlToWord(string html, WordTemplateParameters p)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);
        var currentParagraph = p.CurrentTokenNode!.Ancestors<Paragraph>().FirstEx();

        var elements = HtmlToWordPrivate(htmlDoc.DocumentNode, rp =>
        {
            p.CurrentTokenNode!.RunProperties.CopyTo(rp);

        }, pp =>
        {
            currentParagraph.ParagraphProperties.CopyTo(pp);

        }).ToList();

        if (elements.Any(a => a is Paragraph))
            elements.RemoveAll(a => a is Run r && string.IsNullOrWhiteSpace(r.InnerText));

        return elements;
    }

    private static IEnumerable<OpenXmlElement> HtmlToWordPrivate(HtmlNode htmlNode, Action<RunProperties>? addRunProperties, Action<ParagraphProperties>? addParagraphProperties)
    {
        switch (htmlNode.Name)
        {
            case "#document":
                return htmlNode.ChildNodes.SelectMany(c => HtmlToWordPrivate(c, addRunProperties, addParagraphProperties)).ToArray();

            case "p":

                var paragraphProperties = new ParagraphProperties();
                addParagraphProperties?.Invoke(paragraphProperties);
                var childrens = htmlNode.ChildNodes.SelectMany(c => HtmlToWordPrivate(c, addRunProperties, addParagraphProperties)).ToList();
                childrens.Insert(0, paragraphProperties);
                return new[] { new Paragraph(childrens) };

            case "em":
                return htmlNode.ChildNodes.SelectMany(c => HtmlToWordPrivate(c, addRunProperties + ((RunProperties rp) =>
                {
                    rp.Append(new Italic(), new ItalicComplexScript());
                }), addParagraphProperties));

            case "strong":
                return htmlNode.ChildNodes.SelectMany(c => HtmlToWordPrivate(c, addRunProperties + ((RunProperties rp) =>
                {
                    rp.Append(new Bold(), new BoldComplexScript());
                }), addParagraphProperties));

            case "ins":
            case "u":
                return htmlNode.ChildNodes.SelectMany(c => HtmlToWordPrivate(c, addRunProperties + ((RunProperties rp) =>
                {
                    rp.Append(new Underline() { Val = UnderlineValues.Single });
                }), addParagraphProperties));

            case "#text":
                var runProperties = new RunProperties();
                addRunProperties?.Invoke(runProperties);

                return new[]
                {
                     new Run(runProperties, new DocumentFormat.OpenXml.Wordprocessing.Text(htmlNode.InnerText)
                     {
                          Space =  SpaceProcessingModeValues.Preserve,
                     })
                };

            default:
                throw new UnexpectedValueException(htmlNode.Name);
        }

    }
}
