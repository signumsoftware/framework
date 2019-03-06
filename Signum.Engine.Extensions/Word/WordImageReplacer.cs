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
    public class WordImageReplacer
    {
        public static bool AvoidAdaptSize = false; //Jpeg compression creates different images in TeamCity

        /// <param name="titleOrDescription">Word Image -> Right Click -> Format Picture -> Alt Text -> Title </param>
        public static void ReplaceImage(WordprocessingDocument doc, string titleOrDescription, Bitmap bitmap, string newImagePartId, bool adaptSize = false, bool allowMultiple = false, ImagePartType imagePartType = ImagePartType.Png)
        {
            Blip blip = FindBlip(doc, titleOrDescription);

            if (adaptSize && !AvoidAdaptSize)
            {
                var part = doc.MainDocumentPart.GetPartById(blip.Embed);

                using (var stream = part.GetStream())
                {
                    Bitmap oldBmp = (Bitmap)Bitmap.FromStream(stream);

                    bitmap = ImageResizer.Resize(bitmap, oldBmp.Width, oldBmp.Height);
                }
            }

            doc.MainDocumentPart.DeletePart(blip.Embed);

            ImagePart img = CreateImagePart(doc, bitmap, newImagePartId, imagePartType);

            blip.Embed = doc.MainDocumentPart.GetIdOfPart(img);
        }

        /// <param name="titleOrDescription">Word Image -> Right Click -> Format Picture -> Alt Text -> Title </param>
        public static void ReplaceMultipleImages(WordprocessingDocument doc, string titleOrDescription, Bitmap[] bitmaps, string newImagePartId, bool adaptSize = false, ImagePartType imagePartType = ImagePartType.Png)
        {
            Blip[] blips = FindAllBlips(doc, titleOrDescription);

            if (blips.Count() != bitmaps.Length)
                throw new ApplicationException("Images count does not match the images count in word");

            if (adaptSize && !AvoidAdaptSize)
            {
                Array.ForEach(bitmaps, bitmap =>
                {
                    var part = doc.MainDocumentPart.GetPartById(blips.First().Embed);

                    using (var stream = part.GetStream())
                    {
                        Bitmap oldBmp = (Bitmap)Bitmap.FromStream(stream);
                        bitmap = ImageResizer.Resize(bitmap, oldBmp.Width, oldBmp.Height);
                    }
                });
            }

            doc.MainDocumentPart.DeletePart(blips.First().Embed);

            var i = 0;
            var bitmapStack = new Stack<Bitmap>(bitmaps.Reverse());
            foreach (var blip in blips)
            {
                ImagePart img = CreateImagePart(doc, bitmapStack.Pop(), newImagePartId + i, imagePartType);
                blip.Embed = doc.MainDocumentPart.GetIdOfPart(img);
                i++;
            }
        }

        public static void RemoveImage(WordprocessingDocument doc, string title, bool removeFullDrawing)
        {
            Blip blip = FindBlip(doc, title);
            doc.MainDocumentPart.DeletePart(blip.Embed);

            if (removeFullDrawing)
                ((OpenXmlElement)blip).Follow(a => a.Parent).OfType<Drawing>().FirstEx().Remove();
            else
                blip.Remove();
        }

        static ImagePart CreateImagePart(WordprocessingDocument doc, Bitmap bitmap, string id, ImagePartType imagePartType = ImagePartType.Png)
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

        static Blip FindBlip(WordprocessingDocument doc, string titleOrDescription)
        {
            var drawing = doc.MainDocumentPart.Document.Descendants().OfType<Drawing>().Single(r =>
            {
                var prop = r.Descendants<DocProperties>().SingleOrDefault();
                var match = prop != null && (prop.Title == titleOrDescription || prop.Description == titleOrDescription);

                return match;
            });

            return drawing.Descendants<Blip>().SingleEx();
        }

        static Blip[] FindAllBlips(WordprocessingDocument doc, string titleOrDescription)
        {
            var i = 0;
            var drawing = doc.MainDocumentPart.Document.Descendants().OfType<Drawing>().Where(r =>
            {
                var prop = r.Descendants<DocProperties>().SingleOrDefault();
                var match = prop != null && (prop.Title == titleOrDescription || prop.Description == titleOrDescription);
                prop.Title += i;
                i++;

                return match;
            });

            return drawing.Select(d => d.Descendants<Blip>().SingleEx()).ToArray();
        }
    }

    public static class ImageResizer
    {
        //http://stackoverflow.com/a/10445101/38670
        public static Bitmap Resize(Bitmap image, int width, int height)
        {
            var brush = new SolidBrush(System.Drawing.Color.White);

            float scale = Math.Min(width / (float)image.Width, height / (float)image.Height);

            var bmp = new Bitmap((int)width, (int)height);
            var graph = Graphics.FromImage(bmp);

            // uncomment for higher quality output
            graph.InterpolationMode = InterpolationMode.High;
            graph.CompositingQuality = CompositingQuality.HighQuality;
            graph.SmoothingMode = SmoothingMode.AntiAlias;

            var scaleWidth = (int)(image.Width * scale);
            var scaleHeight = (int)(image.Height * scale);

            graph.FillRectangle(brush, new RectangleF(0, 0, width, height));
            graph.DrawImage(image, new System.Drawing.Rectangle(((int)width - scaleWidth) / 2, ((int)height - scaleHeight) / 2, scaleWidth, scaleHeight));

            return bmp;
        }
    }
}
