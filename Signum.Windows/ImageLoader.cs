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
            var sri = Application.GetResourceStream(new Uri("pack://application:,,,/Signum.Windows;component/Images/" + name, UriKind.Absolute));
            return BitmapFrame.Create(sri.Stream); 
        }
    }
}
