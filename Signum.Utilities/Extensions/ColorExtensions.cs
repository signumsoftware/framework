using System.Drawing;
using System.Globalization;

namespace Signum.Utilities;

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

    public static string? TryToHtml(this Color? color)
    {
        if (color == null)
            return null;

        return ToHtmlColor(color.Value.ToArgb());
    }

    public static string ToHtmlColor(int value)
    {
        return "#" + (value & 0xffffff).ToString("X6");
    }


    public static Color FromHsv(double h, double S, double V)
    {
        double H = h;
        while (H < 0) { H += 360; };
        while (H >= 360) { H -= 360; };
        double R, G, B;
        if (V <= 0)
        { R = G = B = 0; }
        else if (S <= 0)
        {
            R = G = B = V;
        }
        else
        {
            double hf = H / 60.0;
            int i = (int)Math.Floor(hf);
            double f = hf - i;
            double pv = V * (1 - S);
            double qv = V * (1 - S * f);
            double tv = V * (1 - S * (1 - f));
            switch (i)
            {

                // Red is the dominant color

                case 0:
                    R = V;
                    G = tv;
                    B = pv;
                    break;

                // Green is the dominant color

                case 1:
                    R = qv;
                    G = V;
                    B = pv;
                    break;
                case 2:
                    R = pv;
                    G = V;
                    B = tv;
                    break;

                // Blue is the dominant color

                case 3:
                    R = pv;
                    G = qv;
                    B = V;
                    break;
                case 4:
                    R = tv;
                    G = pv;
                    B = V;
                    break;

                // Red is the dominant color

                case 5:
                    R = V;
                    G = pv;
                    B = qv;
                    break;

                // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                case 6:
                    R = V;
                    G = tv;
                    B = pv;
                    break;
                case -1:
                    R = V;
                    G = pv;
                    B = qv;
                    break;

                // The color is not defined, we should throw an error.

                default:
                    //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                    R = G = B = V; // Just pretend its black/white
                    break;
            }
        }

        return Color.FromArgb(
            Clamp((int)(R * 255.0)),
            Clamp((int)(G * 255.0)),
            Clamp((int)(B * 255.0)));
    }

    static int Clamp(int i)
    {
        if (i < 0) return 0;
        if (i > 255) return 255;
        return i;
    }

    public static Color FromHex(string hex)
    {
        FromHex(hex, out var a, out var r, out var g, out var b);

        return Color.FromArgb(a, r, g, b);
    }

    public static void FromHex(string hex, out byte a, out byte r, out byte g, out byte b)
    {
        var nhex = ToRgbaHex(hex);
        if (nhex == null || !uint.TryParse(nhex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var packedValue))
        {
            throw new ArgumentException("Hexadecimal string is not in the correct format.", nameof(hex));
        }

        a = (byte)(packedValue >> 0);
        r = (byte)(packedValue >> 24);
        g = (byte)(packedValue >> 16);
        b = (byte)(packedValue >> 8);
    }

    private static string? ToRgbaHex(string hex)
    {
        hex = hex.StartsWith("#") ? hex.Substring(1) : hex;

        if (hex.Length == 8)
        {
            return hex;
        }

        if (hex.Length == 6)
        {
            return hex + "FF";
        }

        if (hex.Length < 3 || hex.Length > 4)
        {
            return null;
        }

        //Handle values like #3B2
        string red = char.ToString(hex[0]);
        string green = char.ToString(hex[1]);
        string blue = char.ToString(hex[2]);
        string alpha = hex.Length == 3 ? "F" : char.ToString(hex[3]);


        return string.Concat(red, red, green, green, blue, blue, alpha, alpha);
    }


}
