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
        var size = image.Size();
        return (size.Width, size.Height);
    }

    public Image Resize(Image image, int maxWidth, int maxHeight, ImageVerticalPosition verticalPosition, ImageHorizontalPosition horizontalPosition)
    {
        AnchorPositionMode setPosition ()
        {
            switch (verticalPosition)
            {
                case ImageVerticalPosition.Top:
                    return horizontalPosition == ImageHorizontalPosition.Left ? AnchorPositionMode.TopLeft :
                        horizontalPosition == ImageHorizontalPosition.Right ? AnchorPositionMode.TopRight :
                        AnchorPositionMode.Top;

                case ImageVerticalPosition.Center:
                    return horizontalPosition == ImageHorizontalPosition.Left ? AnchorPositionMode.Left :
                        horizontalPosition == ImageHorizontalPosition.Right ? AnchorPositionMode.Right :
                        AnchorPositionMode.Center;

                case ImageVerticalPosition.Bottom:
                    return horizontalPosition == ImageHorizontalPosition.Left ? AnchorPositionMode.BottomLeft :
                        horizontalPosition == ImageHorizontalPosition.Right ? AnchorPositionMode.BottomRight :
                        AnchorPositionMode.Bottom;
                default: throw new NotImplementedException("unknown image position values");                        
            }
        }
        return image.Clone(x =>
        {
            x.Resize(new ResizeOptions
            {
                Size = new Size(maxWidth, maxHeight),
                Mode = ResizeMode.Pad,
                Position = setPosition()
            })
            .BackgroundColor(Color.White);
        });
    }

    public void Save(Image image, Stream str, ImagePartType imagePartType)
    {
        image.Save(str, ToImageFormat(imagePartType));
    }

    private static IImageEncoder ToImageFormat(ImagePartType imagePartType)
    {
        switch (imagePartType)
        {
            case ImagePartType.Bmp: return new SixLabors.ImageSharp.Formats.Bmp.BmpEncoder();
            case ImagePartType.Emf: throw new NotSupportedException(imagePartType.ToString());
            case ImagePartType.Gif: return new SixLabors.ImageSharp.Formats.Gif.GifEncoder();
            case ImagePartType.Icon: throw new NotSupportedException(imagePartType.ToString());
            case ImagePartType.Jpeg: return new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder();
            case ImagePartType.Png: return new SixLabors.ImageSharp.Formats.Png.PngEncoder();
            case ImagePartType.Tiff: return new SixLabors.ImageSharp.Formats.Tiff.TiffEncoder();
            case ImagePartType.Wmf: throw new NotSupportedException(imagePartType.ToString());
        }

        throw new InvalidOperationException("Unexpected {0}".FormatWith(imagePartType));
    }
}
