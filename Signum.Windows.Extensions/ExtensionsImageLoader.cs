using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows;

namespace Signum.Windows
{
    public static class ExtensionsImageLoader
    {
        public static BitmapSource GetImageSortName(string name)
        {
            return Signum.Windows.ImageLoader.LoadIcon(PackUriHelper.Reference("Images/" + name, typeof(ExtensionsImageLoader)));
        }
    }
}
