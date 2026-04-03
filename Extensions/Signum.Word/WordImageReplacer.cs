using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;
#pragma warning disable CA1416 // Validate platform compatibility

namespace Signum.Word;

public static class WordImageReplacer
{
    public static bool AvoidAdaptSize = false; //Jpeg compression creates different images in TeamCity

    /// <param name="titleOrDescription">
    /// Replaces a placeholder-image with the provided image by comparing title/description
    /// 
    /// Word Image -> Right Click -> Format Picture -> Alt Text -> Title 
    /// </param>
    public static void ReplaceImage<TImage>(this WordprocessingDocument doc, string titleOrDescription, TImage image, IImageConverter<TImage> converter, string newImagePartId, bool adaptSize = false, PartTypeInfo? imagePartType = null,
        ImageVerticalPosition verticalPosition = ImageVerticalPosition.Center,
        ImageHorizontalPosition horizontalPosition = ImageHorizontalPosition.Center)
    {
        var blip = doc.FindBlip(titleOrDescription);

        if (adaptSize && AvoidAdaptSize == false)
        {
            var size = doc.GetBlipBitmapSize(blip, converter);
            image = converter.Resize(image, size.width, size.height, verticalPosition, horizontalPosition);
        }

        doc.ReplaceBlipContent(blip, image, converter, newImagePartId, imagePartType ?? ImagePartType.Png);
    }

    /// <param name="titleOrDescription">
    /// Replaces a placeholder-image with multiple images by comparing title/description
    /// 
    /// Word Image -> Right Click -> Format Picture -> Alt Text -> Title 
    /// </param>
    public static void ReplaceMultipleImages<TImage>(this WordprocessingDocument doc, string titleOrDescription, TImage[] images, IImageConverter<TImage> converter, string newImagePartId, bool adaptSize = false, PartTypeInfo? imagePartType = null,
        ImageVerticalPosition verticalPosition = ImageVerticalPosition.Center,
        ImageHorizontalPosition horizontalPosition = ImageHorizontalPosition.Center)
    {
        Blip[] blips = FindAllBlips(doc, d => d.Title == titleOrDescription || d.Description == titleOrDescription);

        if (blips.Count() != images.Length)
            throw new ApplicationException("Images count does not match the images count in word");

        if (adaptSize && !AvoidAdaptSize)
        {
            images = images.Select(bitmap =>
            {
                var part = doc.MainDocumentPart!.GetPartById(blips.First().Embed!);

                using (var stream = part.GetStream())
                {
                    TImage oldImage = converter.FromStream(stream);
                    var size = converter.GetSize(oldImage);
                    return converter.Resize(bitmap, size.width, size.height, verticalPosition, horizontalPosition);
                }
            }).ToArray();
        }

        doc.MainDocumentPart!.DeletePart(blips.First().Embed!);

        var i = 0;
        var bitmapStack = new Stack<TImage>(images.Reverse());
        foreach (var blip in blips)
        {
            ImagePart img = CreateImagePart(doc, bitmapStack.Pop(), converter, newImagePartId + i, imagePartType ?? ImagePartType.Png);
            blip.Embed = doc.MainDocumentPart.GetIdOfPart(img);
            i++;
        }
    }

    public static (int width, int height) GetBlipBitmapSize<TImage>(this WordprocessingDocument doc, Blip blip, IImageConverter<TImage> converter)
    {
        var part = doc.MainDocumentPart!.GetPartById(blip.Embed!);

        using (var str = part.GetStream())
        {
            var image = converter.FromStream(str);
            return converter.GetSize(image);
        }
    }

    public static void ReplaceBlipContent<TImage>(this WordprocessingDocument doc, Blip blip, TImage image, IImageConverter<TImage> converter, string newImagePartId, PartTypeInfo imagePartType)
    {
        if (doc.MainDocumentPart!.Parts.Any(p => p.RelationshipId == blip.Embed))
            doc.MainDocumentPart.DeletePart(blip.Embed!);
        ImagePart img = CreateImagePart(doc, image, converter, newImagePartId, imagePartType);
        blip.Embed = doc.MainDocumentPart.GetIdOfPart(img);
    }

    public static void RemoveImage(this WordprocessingDocument doc, string title, bool removeFullDrawing)
    {
        Blip blip = FindBlip(doc, title);
        doc.MainDocumentPart!.DeletePart(blip.Embed!);

        if (removeFullDrawing)
            ((OpenXmlElement)blip).Follow(a => a.Parent).OfType<Drawing>().FirstEx().Remove();
        else
            blip.Remove();
    }

    public static void RemoveMultipleImage(this WordprocessingDocument doc, string title, bool removeFullDrawing)
    {
        Blip[] blips = FindAllBlips(doc, d => d.Title == title || d.Description == title);
        foreach (var blip in blips)
        {
            doc.MainDocumentPart!.DeletePart(blip.Embed!);

            if (removeFullDrawing)
                ((OpenXmlElement)blip).Follow(a => a.Parent).OfType<Drawing>().FirstEx().Remove();
            else
                blip.Remove();
        }
    }

    static ImagePart CreateImagePart<TImage>(this WordprocessingDocument doc, TImage image, IImageConverter<TImage> converter, string id, PartTypeInfo imagePartType)
    {
        ImagePart img = doc.MainDocumentPart!.AddImagePart(imagePartType, id);

        using (var ms = new MemoryStream())
        {
            converter.Save(image, ms, imagePartType);
            ms.Seek(0, SeekOrigin.Begin);
            img.FeedData(ms);
        }
        return img;
    }

    public static bool HasBlip(this WordprocessingDocument doc, string titleOrDescription)
    {
        var query = GetDrawings(doc);

        return query.Any(r => r.GetTitle() == titleOrDescription);
    }

    public static Blip FindBlip(this WordprocessingDocument doc, string titleOrDescription)
    {
        var query = GetDrawings(doc);

        var drawing = query.Single(r => r.GetTitle() == titleOrDescription);

        return drawing.Descendants<Blip>().SingleEx();
    }

    public static Blip[] FindAllBlips(this WordprocessingDocument doc, Func<DocProperties, bool> predicate)
    {
        var query = GetDrawings(doc);

        var drawing = query.Where(r =>
        {
            var prop = r.Descendants<DocProperties>().FirstOrDefault();
            var match = prop != null && predicate(prop);

            return match;
        });

        return drawing.Select(d => d.Descendants<Blip>().SingleEx()).ToArray();
    }

    static IEnumerable<Drawing> GetDrawings(WordprocessingDocument doc)
    {
        return doc.MainDocumentPart!.Document!.Descendants<Drawing>()
                    .Concat(doc.MainDocumentPart!.HeaderParts.SelectMany(hp => hp.Header!.Descendants<Drawing>()))
                    .Concat(doc.MainDocumentPart!.FooterParts.SelectMany(hp => hp.Footer!.Descendants<Drawing>()));
    }
}

//https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/system-drawing-common-windows-only
/// <summary>
/// System.Drawing is being deprecated outside windows
/// </summary>
/// <typeparam name="TImage"></typeparam>
public interface IImageConverter<TImage>
{
    (int width, int height) GetSize(TImage image);
    TImage FromStream(Stream str);
    void Save(TImage image, Stream str, PartTypeInfo imagePartType);
    TImage Resize(TImage image, int maxWidth, int maxHeight, ImageVerticalPosition verticalPosition, ImageHorizontalPosition horizontalPosition);
}


public enum ImageVerticalPosition
{
    Top,
    Center,
    Bottom,
}

public enum ImageHorizontalPosition
{
    Left,
    Center,
    Right,
}
