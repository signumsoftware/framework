using DocumentFormat.OpenXml.Packaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using System.IO;
#pragma warning disable CA1416 // Validate platform compatibility

namespace Signum.Word;

public class ImageSharpConverter : IImageConverter<Image>
{
    public static readonly ImageSharpConverter Instance = new ImageSharpConverter();

    public Image FromStream(Stream stream)
    {
        return Image.Load(stream);
    }

    public (int width, int height) GetSize(Image image)
    {
        var size = image.Size;
        return (size.Width, size.Height);
    }

    public Image Resize(Image image, int maxWidth, int maxHeight, ImageVerticalPosition verticalPosition = ImageVerticalPosition.Center, ImageHorizontalPosition horizontalPosition = ImageHorizontalPosition.Center)
    {
        var position = (verticalPosition, horizontalPosition) switch
        {
            (ImageVerticalPosition.Top, ImageHorizontalPosition.Left) => AnchorPositionMode.TopLeft,
            (ImageVerticalPosition.Top, ImageHorizontalPosition.Right) => AnchorPositionMode.TopRight,
            (ImageVerticalPosition.Top, ImageHorizontalPosition.Center) => AnchorPositionMode.Top,

            (ImageVerticalPosition.Center, ImageHorizontalPosition.Left) => AnchorPositionMode.Left,
            (ImageVerticalPosition.Center, ImageHorizontalPosition.Right) => AnchorPositionMode.Right,
            (ImageVerticalPosition.Center, ImageHorizontalPosition.Center) => AnchorPositionMode.Center,

            (ImageVerticalPosition.Bottom, ImageHorizontalPosition.Left) => AnchorPositionMode.BottomLeft,
            (ImageVerticalPosition.Bottom, ImageHorizontalPosition.Right) => AnchorPositionMode.BottomRight,
            (ImageVerticalPosition.Bottom, ImageHorizontalPosition.Center) => AnchorPositionMode.Bottom,

            _ => throw new UnexpectedValueException((verticalPosition, horizontalPosition))
        };

        return image.Clone(x =>
        {
            x
            .Resize(new ResizeOptions
            {
                PadColor = Color.White,
                Size = new Size(maxWidth, maxHeight),
                Mode = ResizeMode.Pad,
                Position = position
            });
        });
    }

    public void Save(Image image, Stream str, PartTypeInfo imagePartType)
    {
        image.Save(str, ToImageFormat(imagePartType));
    }

    public static Dictionary<PartTypeInfo, IImageEncoder> EncodersDictionary = new Dictionary<PartTypeInfo, IImageEncoder>
    {
        { ImagePartType.Bmp, new SixLabors.ImageSharp.Formats.Bmp.BmpEncoder()},
        { ImagePartType.Gif, new SixLabors.ImageSharp.Formats.Gif.GifEncoder()},
        { ImagePartType.Jpeg, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder()},
        { ImagePartType.Png, new SixLabors.ImageSharp.Formats.Png.PngEncoder()},
        { ImagePartType.Tiff, new SixLabors.ImageSharp.Formats.Tiff.TiffEncoder()},
    };

    private static IImageEncoder ToImageFormat(PartTypeInfo imagePartType)
    {
        var encoder = EncodersDictionary.TryGetC(imagePartType);

        if(encoder == null)
            throw new InvalidOperationException("Unexpected {0}".FormatWith(imagePartType));

        return encoder;
    }
}
