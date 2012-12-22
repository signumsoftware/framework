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
        static BitmapFrame png = ExtensionsImageLoader.GetImageSortName("pdf.png");
        static BitmapFrame wordx = ExtensionsImageLoader.GetImageSortName("word.png");
        static BitmapFrame word = ExtensionsImageLoader.GetImageSortName("word9703.png");
        static BitmapFrame excel = ExtensionsImageLoader.GetImageSortName("excel.png");
        static BitmapFrame email = ExtensionsImageLoader.GetImageSortName("email2.png");
        static BitmapFrame html = ExtensionsImageLoader.GetImageSortName("html.png");
        static BitmapFrame txt = ExtensionsImageLoader.GetImageSortName("text.png");
        static BitmapFrame rtf = ExtensionsImageLoader.GetImageSortName("rtf.png");
        static BitmapFrame image = ExtensionsImageLoader.GetImageSortName("pics2.png");
        static BitmapFrame psd = ExtensionsImageLoader.GetImageSortName("photoshop.png");
        static BitmapFrame zip = ExtensionsImageLoader.GetImageSortName("zip.png");
        static BitmapFrame rar = ExtensionsImageLoader.GetImageSortName("rar.png");
        static BitmapFrame film = ExtensionsImageLoader.GetImageSortName("film.png");
        static BitmapFrame music = ExtensionsImageLoader.GetImageSortName("documentMusic.png");
        static BitmapFrame cs = ExtensionsImageLoader.GetImageSortName("textCodeCsharp.png");
        static BitmapFrame ppt = ExtensionsImageLoader.GetImageSortName("ppt.png");
        static BitmapFrame pptx = ExtensionsImageLoader.GetImageSortName("pptx.png");
        static BitmapFrame unknown = ExtensionsImageLoader.GetImageSortName("unknown.png");

        static Dictionary<string, BitmapFrame> imagenes = new Dictionary<string, BitmapFrame>() 
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
