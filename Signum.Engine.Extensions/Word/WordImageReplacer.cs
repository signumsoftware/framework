using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Signum.Engine.Word
{
    public static class WordImageReplacer
    {
        public static bool AvoidAdaptSize = false; //Jpeg compression creates different images in TeamCity

        /// <param name="titleOrDescription">Word Image -> Right Click -> Format Picture -> Alt Text -> Title </param>
        public static void ReplaceImage(this WordprocessingDocument doc, string titleOrDescription, Bitmap bitmap, string newImagePartId, bool adaptSize = false, ImagePartType imagePartType = ImagePartType.Png)
        {
            var blip = doc.FindBlip(titleOrDescription);

            if (adaptSize && AvoidAdaptSize == false)
            {
                var size = doc.GetBlipBitmapSize(blip);
                bitmap = ImageResizer.Resize(bitmap, size.Width, size.Height);
            }

            doc.ReplaceBlipContent(blip, bitmap, newImagePartId, imagePartType);
        }
        
        public static Size GetBlipBitmapSize(this WordprocessingDocument doc, Blip blip)
        {
            var part = doc.MainDocumentPart.GetPartById(blip.Embed);

            using (var str = part.GetStream())
                return Bitmap.FromStream(str).Size;
        }

        public static void ReplaceBlipContent(this WordprocessingDocument doc, Blip blip, Bitmap bitmap, string newImagePartId, ImagePartType imagePartType = ImagePartType.Png)
        {
            if (doc.MainDocumentPart.Parts.Any(p => p.RelationshipId == blip.Embed))
                doc.MainDocumentPart.DeletePart(blip.Embed);
            ImagePart img = CreateImagePart(doc, bitmap, newImagePartId, imagePartType);
            blip.Embed = doc.MainDocumentPart.GetIdOfPart(img);
        }

        public static void RemoveImage(this WordprocessingDocument doc, string title, bool removeFullDrawing)
        {
            Blip blip = FindBlip(doc, title);
            doc.MainDocumentPart.DeletePart(blip.Embed);

            if (removeFullDrawing)
                ((OpenXmlElement)blip).Follow(a => a.Parent).OfType<Drawing>().FirstEx().Remove();
            else
                blip.Remove();
        }

        static ImagePart CreateImagePart(this WordprocessingDocument doc, Bitmap bitmap, string id, ImagePartType imagePartType = ImagePartType.Png)
        {
            ImagePart img = doc.MainDocumentPart.AddImagePart(imagePartType, id);

            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ToImageFormat(imagePartType));
                ms.Seek(0, SeekOrigin.Begin);
                img.FeedData(ms);
            }
            return img;
        }

        private static ImageFormat ToImageFormat(ImagePartType imagePartType)
        {
            switch (imagePartType)
            {
                case ImagePartType.Bmp: return ImageFormat.Bmp;
                case ImagePartType.Emf: return ImageFormat.Emf;
                case ImagePartType.Gif: return ImageFormat.Gif;
                case ImagePartType.Icon: return ImageFormat.Icon;
                case ImagePartType.Jpeg: return ImageFormat.Jpeg;
                case ImagePartType.Png: return ImageFormat.Png;
                case ImagePartType.Tiff: return ImageFormat.Tiff;
                case ImagePartType.Wmf: return ImageFormat.Wmf;
            }

            throw new InvalidOperationException("Unexpected {0}".FormatWith(imagePartType));
        }

        public static Blip FindBlip(this WordprocessingDocument doc, string titleOrDescription)
        {
            var drawing = doc.MainDocumentPart.Document.Descendants().OfType<Drawing>().Single(r =>
            {
                var prop = r.Descendants<DocProperties>().SingleOrDefault();
                var match = prop != null && (prop.Title == titleOrDescription || prop.Description == titleOrDescription);

                return match;
            });

            return drawing.Descendants<Blip>().SingleEx();
        }

        public static Blip[] FindAllBlips(this WordprocessingDocument doc, Func<DocProperties, bool> predicate)
        {
            var drawing = doc.MainDocumentPart.Document.Descendants().OfType<Drawing>().Where(r =>
            {
                var prop = r.Descendants<DocProperties>().SingleOrDefault();
                var match = prop != null && predicate(prop);

                return match;
            });

            return drawing.Select(d => d.Descendants<Blip>().SingleEx()).ToArray();
        }
    }

    public static class ImageResizer
    {

        //http://stackoverflow.com/a/10445101/38670
        public static Bitmap Resize(Bitmap image, int? maxWidth, int? maxHeight)
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
}
