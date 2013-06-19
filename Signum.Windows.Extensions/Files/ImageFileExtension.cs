using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using Signum.Utilities;
using System.IO;
using System.Windows.Data;
using Signum.Windows.Extensions;

namespace Signum.Windows.Files
{
    public static class ImageFileExtension
    {
        static BitmapSource png = ExtensionsImageLoader.GetImageSortName("pdf.png");
        static BitmapSource wordx = ExtensionsImageLoader.GetImageSortName("word.png");
        static BitmapSource word = ExtensionsImageLoader.GetImageSortName("word9703.png");
        static BitmapSource excel = ExtensionsImageLoader.GetImageSortName("excel.png");
        static BitmapSource email = ExtensionsImageLoader.GetImageSortName("email2.png");
        static BitmapSource html = ExtensionsImageLoader.GetImageSortName("html.png");
        static BitmapSource txt = ExtensionsImageLoader.GetImageSortName("text.png");
        static BitmapSource rtf = ExtensionsImageLoader.GetImageSortName("rtf.png");
        static BitmapSource image = ExtensionsImageLoader.GetImageSortName("pics2.png");
        static BitmapSource psd = ExtensionsImageLoader.GetImageSortName("photoshop.png");
        static BitmapSource zip = ExtensionsImageLoader.GetImageSortName("zip.png");
        static BitmapSource rar = ExtensionsImageLoader.GetImageSortName("rar.png");
        static BitmapSource film = ExtensionsImageLoader.GetImageSortName("film.png");
        static BitmapSource music = ExtensionsImageLoader.GetImageSortName("documentMusic.png");
        static BitmapSource cs = ExtensionsImageLoader.GetImageSortName("textCodeCsharp.png");
        static BitmapSource ppt = ExtensionsImageLoader.GetImageSortName("ppt.png");
        static BitmapSource pptx = ExtensionsImageLoader.GetImageSortName("pptx.png");
        static BitmapSource unknown = ExtensionsImageLoader.GetImageSortName("unknown.png");

        static Dictionary<string, BitmapSource> imagenes = new Dictionary<string, BitmapSource>() 
        {
            {".pdf", png},
            {".doc", word},
            {".docx", wordx},
            {".xls", excel},
            {".xlsx", excel},
            {".csv", excel},
            {".eml", email},
            {".msg", email},
            {".htm", html},
            {".html", html},
            {".mht", html},
            {".txt", txt},
            {".rtf", rtf},
            {".tiff", image},
            {".tif", image},
            {".gif", image},
            {".jpg", image},
            {".jpeg", image},
            {".png", image},
            {".psd", psd},
            {".zip", zip},
            {".rar", rar},
            {".avi", film},
            {".mpg", film},
            {".mpeg", film},
            {".wmv", film},
            {".mp3", music},
            {".mp4", music},
            {".wma", music},
            {".wav", music},
            {".cs", cs},
            {".ppt", ppt},
            {".pptx", ppt}
        };

        public static IValueConverter Converter = ConverterFactory.New((string fileName) =>
            imagenes.TryGetC(Path.GetExtension(fileName).ToLower()) ?? unknown);
    }
}
