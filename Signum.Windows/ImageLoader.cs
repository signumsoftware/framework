using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Resources;

namespace Signum.Windows
{
    public static class ImageLoader
    {
        internal static BitmapFrame GetImageSortName(string name)
        {
            return LoadIcon(PackUriHelper.Reference("Images/" + name, typeof(Navigator))); 
        }

        public static BitmapFrame LoadIcon(Uri uri)
        {
            return BitmapFrame.Create(Application.GetResourceStream(uri).Stream);
        }
    }
}
