using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using Signum.Windows.Operations;
using Signum.Utilities;
using Signum.Entities.Authorization;
using System.Windows.Media;
using System.Windows.Ink;
using Signum.Entities.Chart;
using Signum.Entities.Basics;
using Signum.Entities.UserQueries;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserAssets;
using System.Windows;

namespace Signum.Windows.Extensions
{
    public static class Converters
    {
        public static readonly IValueConverter Exponential =
            ConverterFactory.New((int b) => Math.Pow(1.1, b));

        public static readonly IValueConverter AutoScroll = ConverterFactory.New(
            (bool auto) => auto ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Visible);

        public static readonly IValueConverter NotNull = ConverterFactory.New(
            (object obj) => obj != null);

        public static readonly IValueConverter Exponential100 = ConverterFactory.New(
            (double d) => Math.Pow(10, d * 2),
            (double d) => Math.Log10(d) / 2);

        public static readonly IValueConverter Logarithmic100 = ConverterFactory.New(
            (double d) => Math.Log10(d) / 2,
            (double d) => Math.Pow(10, d * 2));

        public static readonly IValueConverter DecimalOrZero =
            ConverterFactory.New((decimal? val) => val ?? 0);

        public static readonly IValueConverter ThumbnailBrush = ConverterFactory.New(
            (AuthThumbnail? d) =>
                !d.HasValue ? null :
                d.Value == AuthThumbnail.All ? Brushes.GreenYellow :
                d.Value == AuthThumbnail.Mix ? Brushes.Yellow : Brushes.Red);

        public static readonly IValueConverter ThumbnailStroke = ConverterFactory.New(
            (AuthThumbnail? d) =>
                !d.HasValue ? null :
                d.Value == AuthThumbnail.All ? Brushes.Green :
                d.Value == AuthThumbnail.Mix ? Brushes.Gold : Brushes.DarkRed);


        public static readonly IValueConverter NotNullToRedBrush = ConverterFactory.New(
            (object d) => d == null ? null : Brushes.Pink);

        public static readonly IValueConverter TokenToEntity = ConverterFactory.New(
            (QueryTokenEmbedded qt) => qt?.TryToken,
            (QueryToken t) => new QueryTokenEmbedded(t));


        public static readonly IValueConverter ThicknessToBool = ConverterFactory.New(
            (Thickness tk) => Math.Max(Math.Max(tk.Left, tk.Right), Math.Max(tk.Bottom, tk.Top)));
    }
}
