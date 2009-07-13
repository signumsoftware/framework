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
        public static BitmapFrame GetImageSortName(string name)
        {
            StreamResourceInfo sri = Application.GetResourceStream(PackUriHelper.Reference("Images/" + name, typeof(Navigator)));
            return BitmapFrame.Create(sri.Stream); 
        }
    }
}
