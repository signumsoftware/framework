using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlAgilityPack;
using System.Web;

namespace Signum.Word;

public static class HtmlToWordConverter
{
    public static IEnumerable<OpenXmlElement> HtmlToWord(string html, WordTemplateParameters p)
    {
        var htmlDoc = new HtmlDocument();

        if (html == null)
            return new List<OpenXmlElement>() { new Paragraph() };

        htmlDoc.LoadHtml(html);
        var currentParagraph = p.CurrentTokenNode!.Ancestors<Paragraph>().FirstEx();


        var elements = HtmlToWordPrivate(htmlDoc.DocumentNode, rp =>
        {
            p.CurrentTokenNode!.RunProperties.CopyTo(rp);

        }, pp =>
        {
            currentParagraph.ParagraphProperties.CopyTo(pp);

        }, p.Document).ToList();

        if (elements.Any(a => a is Paragraph))
            elements.RemoveAll(a => a is Run r && string.IsNullOrWhiteSpace(r.InnerText));

        return elements;
    }

    private static IEnumerable<OpenXmlElement> HtmlToWordPrivate(HtmlNode htmlNode, Action<RunProperties>? addRunProperties, Action<ParagraphProperties>? addParagraphProperties, OpenXmlPackage document)
    {
        switch (htmlNode.Name)
        {
            case "#document":
                return htmlNode.ChildNodes.SelectMany(c => HtmlToWordPrivate(c, addRunProperties, addParagraphProperties, document)).ToArray();

            case "br":
                return new[] { new Break() };

            case "li":
            case "p":
                {
                    var pp = new ParagraphProperties();
                    addParagraphProperties?.Invoke(pp);
                    var childrens = htmlNode.ChildNodes.SelectMany(c => HtmlToWordPrivate(c, addRunProperties, addParagraphProperties, document)).ToList();
                    childrens.Insert(0, pp);
                    return new[] { new Paragraph(childrens) };
                }
            case "em":
                return htmlNode.ChildNodes.SelectMany(c => HtmlToWordPrivate(c, addRunProperties + ((RunProperties rp) =>
                {
                    rp.Append(new Italic(), new ItalicComplexScript());
                }), addParagraphProperties, document));

            case "strong":
                return htmlNode.ChildNodes.SelectMany(c => HtmlToWordPrivate(c, addRunProperties + ((RunProperties rp) =>
                {
                    rp.Append(new Bold(), new BoldComplexScript());
                }), addParagraphProperties, document));

            case "ins":
            case "u":
                return htmlNode.ChildNodes.SelectMany(c => HtmlToWordPrivate(c, addRunProperties + ((RunProperties rp) =>
                {
                    rp.Append(new Underline() { Val = UnderlineValues.Single });
                }), addParagraphProperties, document));

            case "del":
                return htmlNode.ChildNodes.SelectMany(c => HtmlToWordPrivate(c, addRunProperties + ((RunProperties rp) =>
                {
                    rp.Append(new Strike() { Val = true });
                }), addParagraphProperties, document));

            case "h1":
            case "h2":
            case "h3":
            case "h4":
                {
                    var mainDocument = document.GetPartsOfType<MainDocumentPart>().SingleEx();

                    var hi = htmlNode.Name.After("h");


                    Style headingStyle;
                    {
                        var styles = mainDocument.GetPartsOfType<StyleDefinitionsPart>().FirstOrDefault();
                        if (styles == null)
                        {
                            styles = mainDocument.AddNewPart<StyleDefinitionsPart>();
                            styles.Styles = new Styles();
                        }

                        headingStyle = styles.Styles!.Elements<Style>().FirstOrDefault(a => a.StyleName?.Val == "heading " + hi)!;

                        if (headingStyle == null)
                        {
                            var size = hi == "1" ? "32" :
                                hi == "2" ? "26" :
                                hi == "3" ? "24" :
                                hi == "4" ? "22" : 
                                throw new UnexpectedValueException(hi);

                            var color = hi == "1" ? "2F5496" :
                                hi == "2" ? "2F5496" :
                                hi == "3" ? "1F3763" :
                                hi == "4" ? "1F3763" :
                                throw new UnexpectedValueException(hi);

                            var themeShade = hi == "1" ? "BF" :
                                hi == "2" ? "BF" :
                                hi == "3" ? "7F" :
                                hi == "4" ? "7F" :
                                throw new UnexpectedValueException(hi);

                            headingStyle = new Style(
                             new StyleName() { Val = "heading " + hi },
                             new BasedOn() { Val = "Standard" },
                             new NextParagraphStyle() { Val = "Standard" },
                             //new LinkedStyle() { Val = "berschrift1Zchn" },
                             new UIPriority() { Val = 9 },
                             new PrimaryStyle(),
                             new StyleParagraphProperties(
                                 new KeepNext(),
                                 new KeepLines(),
                                 new SpacingBetweenLines() { Before = hi == "1" ? "240" : "40", After = "0" }
                                 //new OutlineLevel() { Val = int.Parse(hi) -1 }),
                             ),
                             new StyleRunProperties(
                                 new RunFonts() { AsciiTheme = ThemeFontValues.MajorHighAnsi, HighAnsiTheme = ThemeFontValues.MajorHighAnsi, EastAsiaTheme = ThemeFontValues.MajorEastAsia, ComplexScriptTheme = ThemeFontValues.MajorBidi },
                                 new Color() { Val = color, ThemeColor = ThemeColorValues.Accent1, ThemeShade = themeShade },
                                 new FontSize() { Val = size },
                                 new FontSizeComplexScript() { Val = size }
                             )
                             )
                            { Type = StyleValues.Paragraph, StyleId = "berschrift" + hi };

                            styles.Styles.InsertAfter(headingStyle,
                                styles.Styles!.Elements<Style>().LastOrDefault(a => a.StyleName?.Val?.Value?.StartsWith("heading ") == true) ?? 
                                styles.Styles.Elements<Style>().LastOrDefault()
                                );
                            styles.Styles.Save(styles);
                        }
                    }


                    var pp = new ParagraphProperties
                    {
                        ParagraphStyleId = new ParagraphStyleId { Val = headingStyle.StyleId }
                    };
                    var childrens = htmlNode.ChildNodes.SelectMany(c => HtmlToWordPrivate(c, null, null, document)).ToList();
                    childrens.Insert(0, pp);
                    return new[] { new Paragraph(childrens) };

                }

            case "ul":
            case "ol":
                {
                    var mainDocument = document.GetPartsOfType<MainDocumentPart>().SingleEx();
                    var listLevel = 0;

                    int numberId; 
                    {
                        var numberings = mainDocument.GetPartsOfType<NumberingDefinitionsPart>().FirstOrDefault();

                        if (numberings == null)
                        {
                            numberings = mainDocument.AddNewPart<NumberingDefinitionsPart>();
                            numberings.Numbering = new Numbering();
                        }

                        int abstractId = (numberings.Numbering.Elements<AbstractNum>().Max(a => a.AbstractNumberId?.Value) ?? 0) + 1;
                        numberId = (numberings.Numbering.Elements<NumberingInstance>().Max(a => a.NumberID?.Value) ?? 0) + 1;

                        var node = htmlNode;
                        while(node.ParentNode != null)
                        {
                            if(node.ParentNode.Name == "ul" || node.ParentNode.Name == "li")
                                listLevel++;
                            node = node.ParentNode;
                        }
                        numberings.Numbering.InsertAfter(new AbstractNum(
                            new Level(
                                new StartNumberingValue { Val = 1 },
                                new NumberingFormat() { Val = htmlNode.Name == "ul" ? NumberFormatValues.Bullet : NumberFormatValues.Decimal },
                                new LevelText() { Val = htmlNode.Name == "ul" ? "\x2022" : "%1." },
                                new PreviousParagraphProperties(new Indentation() { Left = (360 * (listLevel + 1)).ToString(), Hanging = "180" }),
                                null!
                                )
                            { LevelIndex = 0 })
                        { AbstractNumberId = abstractId },
                          numberings.Numbering.OfType<AbstractNum>().LastOrDefault()
                        );

                        numberings.Numbering.InsertAfter(
                            new NumberingInstance(new AbstractNumId() { Val = abstractId }) { NumberID = numberId },
                            (OpenXmlElement?)numberings.Numbering.Elements<NumberingInstance>().LastOrDefault() ??
                            numberings.Numbering.Elements<AbstractNum>().LastOrDefault()
                            );

                        numberings.Numbering.Save(numberings);
                    }

                    Style listParagraph; 
                    {
                        var styles = mainDocument.GetPartsOfType<StyleDefinitionsPart>().FirstOrDefault();
                        if (styles == null)
                        {
                            styles = mainDocument.AddNewPart<StyleDefinitionsPart>();
                            styles.Styles = new Styles();
                        }

                        listParagraph = styles.Styles!.ChildElements.OfType<Style>().FirstOrDefault(a => a.StyleName?.Val == "List Paragraph")!;

                        if(listParagraph == null)
                        {
                            listParagraph = new Style(
                               new StyleName() { Val = "List Paragraph" },
                               new BasedOn() { Val = "Standard" },
                               new UIPriority() { Val = 34 },
                               new PrimaryStyle(),
                               new StyleParagraphProperties(
                                   new Indentation() { Left = (360 * (listLevel + 1)).ToString() },
                                   new ContextualSpacing()
                               )
                           )
                            { Type = StyleValues.Paragraph, StyleId = "Listenabsatz" };

                            styles.Styles.Append(listParagraph);
                            styles.Styles.Save(styles);
                        }
                    }

                    var childrens = htmlNode.ChildNodes.SelectMany(c => HtmlToWordPrivate(c, addRunProperties, addParagraphProperties + ((ParagraphProperties pp) =>
                    {
                        pp.NumberingProperties = new NumberingProperties
                        {
                            NumberingLevelReference = new NumberingLevelReference { Val = 0 },
                            NumberingId = new NumberingId { Val = numberId }
                        };
                        pp.ParagraphStyleId = new ParagraphStyleId { Val = listParagraph.StyleId };
                    }), document));

                    return childrens;
                }
            case "#text":
                var runProperties = new RunProperties();
                addRunProperties?.Invoke(runProperties);

                var text = HttpUtility.HtmlDecode(htmlNode.InnerText);

                return new[]
                {
                     new Run(runProperties, new DocumentFormat.OpenXml.Wordprocessing.Text(text)
                     {
                          Space =  SpaceProcessingModeValues.Preserve,
                     })
                };

            default:
                //throw new UnexpectedValueException(htmlNode.Name);
                return htmlNode.ChildNodes.SelectMany(c => HtmlToWordPrivate(c, addRunProperties + ((RunProperties rp) =>
                {
                }), addParagraphProperties, document));
        }


        
    }
}
