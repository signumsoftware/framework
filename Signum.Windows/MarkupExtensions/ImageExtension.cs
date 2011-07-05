using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Media;

namespace Signum.Windows
{
    public class ImageExtension : MarkupExtension
    {
        Uri source;
        public ImageExtension(Uri source)
        {
            this.source = source;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new Image
            {
                //Height = 16,
                //Width = 16,
                Source =  BitmapFrame.Create(GetUriFromUriContext((IUriContext)serviceProvider.GetService(typeof(IUriContext)), source)),
                Stretch = Stretch.None
            };
        }

        internal static Uri GetUriFromUriContext(IUriContext context, Uri original)
        {
            if (!original.IsAbsoluteUri && (context != null) && context.BaseUri != null)
            {
                return new Uri(context.BaseUri, original);
            }
            return original;
        }
    }
}
