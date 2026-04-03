using DocumentFormat.OpenXml.Packaging;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
#pragma warning disable CA1416 // Validate platform compatibility

namespace Signum.Word;

public class GdiBitmapConverter : IImageConverter<Bitmap>
{
    public static readonly GdiBitmapConverter Instance = new GdiBitmapConverter();

    public Bitmap FromStream(Stream stream)
    {
        return (Bitmap)Bitmap.FromStream(stream);
    }

    public (int width, int height) GetSize(Bitmap image)
    {
        var size = image.Size;
        return (size.Width, size.Height);
    }

    public Bitmap Resize(Bitmap image, int maxWidth, int maxHeight, ImageVerticalPosition verticalPosition, ImageHorizontalPosition horizontalPosition)
    {
        return ResizeBitmap(image, maxWidth, maxHeight);
    }

    public void Save(Bitmap image, Stream str, PartTypeInfo imagePartType)
    {
        image.Save(str, ToImageFormat(imagePartType));
    }

    public static Dictionary<PartTypeInfo, ImageFormat> ToImageFormatMapping = new Dictionary<PartTypeInfo, ImageFormat>()
    {
        { ImagePartType.Bmp, ImageFormat.Bmp}, 
        { ImagePartType.Emf, ImageFormat.Emf},
        { ImagePartType.Gif, ImageFormat.Gif},
        { ImagePartType.Icon, ImageFormat.Icon},
        { ImagePartType.Jpeg, ImageFormat.Jpeg},
        { ImagePartType.Png, ImageFormat.Png},
        { ImagePartType.Tiff, ImageFormat.Tiff},
        { ImagePartType.Wmf, ImageFormat.Wmf},
    };

    private static ImageFormat ToImageFormat(PartTypeInfo imagePartType)
    {
        var imageFormat = ToImageFormatMapping.TryGetC(imagePartType);

        if (imageFormat == null)
            throw new InvalidOperationException("Unexpected {0}".FormatWith(imagePartType));

        return imageFormat;
    }

    //http://stackoverflow.com/a/10445101/38670
    public static Bitmap ResizeBitmap(Bitmap image, int? maxWidth, int? maxHeight)
    {
        var brush = new SolidBrush(System.Drawing.Color.White);

        float scale = maxWidth.HasValue && maxHeight.HasValue ? Math.Min(maxWidth.Value / (float)image.Width, maxHeight.Value / (float)image.Height) :
            maxHeight.HasValue ? maxHeight.Value / (float)image.Height :
            maxWidth.HasValue ? maxWidth.Value / (float)image.Width :
            throw new ArgumentNullException("maxWidth and maxHeight");

        int scaleWidth = (int)(image.Width * scale);
        int scaleHeight = (int)(image.Height * scale);

        int width = maxWidth ?? scaleWidth;
        int height = maxHeight ?? scaleHeight;

        var bmp = new Bitmap(width, height);
        var graph = Graphics.FromImage(bmp);

        // uncomment for higher quality output
        graph.InterpolationMode = InterpolationMode.High;
        graph.CompositingQuality = CompositingQuality.HighQuality;
        graph.SmoothingMode = SmoothingMode.AntiAlias;

        graph.FillRectangle(brush, new RectangleF(0, 0, width, height));
        graph.DrawImage(image, new System.Drawing.Rectangle(((int)width - scaleWidth) / 2, ((int)height - scaleHeight) / 2, scaleWidth, scaleHeight));

        return bmp;
    }

}
