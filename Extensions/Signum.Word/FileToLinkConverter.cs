using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using Signum.Files;

namespace Signum.Word;

public static class FileToLinkConverter
{
    public static IEnumerable<OpenXmlElement> FileToLink(FilePathEmbedded? file, WordTemplateParameters p)
    {
        if(file == null)
            return Enumerable.Empty<OpenXmlElement>();

        var mainDocument = p.Document.GetPartsOfType<MainDocumentPart>().SingleEx();

        var fullWebPath = file.FullWebPath();

        if (fullWebPath == null)
            throw new InvalidOperationException($"File {file} of type {file.FileType} has not full web path");

        var hl = mainDocument.AddHyperlinkRelationship(new Uri(fullWebPath), true);

        var hyperlink = new Hyperlink(
            new Run(
                new RunProperties(),
                new RunStyle() { Val = "Hyperlink" },
                new RunFonts() { ComplexScriptTheme = ThemeFontValues.MinorHighAnsi },
                new Bold(),
                new BoldComplexScript(),
                new Color() { Val = "2F5496", ThemeColor = ThemeColorValues.Accent1, ThemeShade = "BF" },
                new FontSize() { Val = "28" },
                new FontSizeComplexScript() { Val = "28" },
                new Shading() { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "FFFFFF" },
                new Text(file.FileName)
            )
        )
        {
            Id = hl.Id,
        };

        Style hyperlinkStyle;
        {
            var styles = mainDocument.GetPartsOfType<StyleDefinitionsPart>().FirstOrDefault();
            if (styles == null)
            {
                styles = mainDocument.AddNewPart<StyleDefinitionsPart>();
                styles.Styles = new Styles();
            }

            hyperlinkStyle = styles.Styles!.ChildElements.OfType<Style>().FirstOrDefault(a => a.StyleName?.Val == "Hyperlink")!;

            if (hyperlinkStyle == null)
            {
                hyperlinkStyle = new Style(

                new StyleName() { Val = "Hyperlink" },
                new BasedOn() { Val = "Absatz-Standardschriftart" },
                new UIPriority() { Val = 99 },
                new UnhideWhenUsed(),
                new StyleRunProperties(
                    new Color() { Val = "0563C1", ThemeColor = ThemeColorValues.Hyperlink },
                    new Underline() { Val = UnderlineValues.Single }
                )

               )
                { Type = StyleValues.Character, StyleId = "Hyperlink" };

                styles.Styles.Append(hyperlinkStyle);
                styles.Styles.Save(styles);
            }
        }

        return new[] { hyperlink };
     
    }
}
