using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Resources;
using System.Windows.Controls;
using System.Windows.Media;

namespace Signum.Windows
{
    public static class ImageLoader
    {
        public static BitmapSource GetImageSortName(string name)
        {
            return LoadIcon(PackUriHelper.Reference("Images/" + name, typeof(Navigator)));
        }

        public static BitmapSource LoadIcon(Uri uri)
        {
            var result = BitmapFrame.Create(Application.GetResourceStream(uri).Stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            result.Freeze();
            var writable = new WriteableBitmap(result);
            writable.Freeze();
            return writable;
        }

        public static Image ToSmallImage(this ImageSource source)
        {
            var result = new Image
            {
                Width = 16,
                Height = 16,
                SnapsToDevicePixels = true,
                Source = source
            };

            RenderOptions.SetBitmapScalingMode(result, BitmapScalingMode.NearestNeighbor);

            source.Freeze();

            return result;
        }

    }
}
