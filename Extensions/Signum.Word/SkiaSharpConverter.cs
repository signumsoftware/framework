using DocumentFormat.OpenXml.Packaging;
using SkiaSharp;
using System.IO;

namespace Signum.Word;

public class SkiaSharpConverter : IImageConverter<SKBitmap>
{
    public static readonly SkiaSharpConverter Instance = new SkiaSharpConverter();

    public static int JpegQuality = 75;

    public SKBitmap FromStream(Stream stream)
    {
        return SKBitmap.Decode(stream) ?? throw new InvalidOperationException("Unable to decode image");
    }

    public (int width, int height) GetSize(SKBitmap image)
    {
        return (image.Width, image.Height);
    }

    public SKBitmap Resize(SKBitmap image, int maxWidth, int maxHeight, ImageVerticalPosition verticalPosition = ImageVerticalPosition.Center, ImageHorizontalPosition horizontalPosition = ImageHorizontalPosition.Center)
    {
        var ratio = Math.Min((float)maxWidth / image.Width, (float)maxHeight / image.Height);
        var width = (int)Math.Round(image.Width * ratio);
        var height = (int)Math.Round(image.Height * ratio);

        var x = horizontalPosition switch
        {
            ImageHorizontalPosition.Left => 0,
            ImageHorizontalPosition.Center => (maxWidth - width) / 2,
            ImageHorizontalPosition.Right => maxWidth - width,
            _ => throw new UnexpectedValueException(horizontalPosition)
        };

        var y = verticalPosition switch
        {
            ImageVerticalPosition.Top => 0,
            ImageVerticalPosition.Center => (maxHeight - height) / 2,
            ImageVerticalPosition.Bottom => maxHeight - height,
            _ => throw new UnexpectedValueException(verticalPosition)
        };

        var result = new SKBitmap(maxWidth, maxHeight);
        using (var canvas = new SKCanvas(result))
        using (var source = SKImage.FromBitmap(image))
        {
            canvas.Clear(SKColors.White);
            canvas.DrawImage(source, SKRect.Create(x, y, width, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
        }
        return result;
    }

    public void Save(SKBitmap image, Stream str, PartTypeInfo imagePartType)
    {
        using (var data = image.Encode(ToImageFormat(imagePartType), JpegQuality))
            data.SaveTo(str);
    }

    //SkiaSharp is only able to encode Png, Jpeg and Webp
    public static Dictionary<PartTypeInfo, SKEncodedImageFormat> FormatsDictionary = new Dictionary<PartTypeInfo, SKEncodedImageFormat>
    {
        { ImagePartType.Jpeg, SKEncodedImageFormat.Jpeg },
        { ImagePartType.Png, SKEncodedImageFormat.Png },
    };

    private static SKEncodedImageFormat ToImageFormat(PartTypeInfo imagePartType)
    {
        if (!FormatsDictionary.TryGetValue(imagePartType, out var format))
            throw new InvalidOperationException("Unexpected {0}, only Png and Jpeg are supported".FormatWith(imagePartType));

        return format;
    }
}
