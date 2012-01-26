using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Signum.Utilities
{
    public static class ColorExtensions
    {
        public static Color Interpolate(this Color from, float ratio, Color to)
        {
            var ratioNeg = 1 - ratio;

            return Color.FromArgb(
                (int)(from.A * ratioNeg + to.A * ratio),
                (int)(from.R * ratioNeg + to.R * ratio),
                (int)(from.G * ratioNeg + to.G * ratio),
                (int)(from.B * ratioNeg + to.B * ratio));
        }

        public static string ToHtml(this Color color)
        {
            return ToHtmlColor(color.ToArgb());
        }

        public static string ToHtmlColor(int value)
        {
            return "#" + (value & 0xffffff).ToString("X6");
        }
    }
}
