using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;

namespace Signum.Windows.Extensions
{
    public static class Converters
    {
        public static readonly IValueConverter Exponential =
            ConverterFactory.New((int b) => Math.Pow(1.1, b));

        public static readonly IValueConverter AutoScroll = ConverterFactory.New(
            (bool auto) => auto ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Visible);

        public static readonly IValueConverter Exponential100 = ConverterFactory.New(
            (double d) => Math.Pow(10, d * 2),
            (double d) => Math.Log10(d) / 2);

        public static readonly IValueConverter Logarithmic100 = ConverterFactory.New(
            (double d) => Math.Log10(d) / 2,
            (double d) => Math.Pow(10, d * 2));
    }
}
