using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Drawing;
using Signum.Utilities;

namespace Signum.Entities.Basics
{
    [Serializable]
    public class ColorEmbedded : EmbeddedEntity
    {
        public ColorEmbedded()
        {
        }

        public override bool Equals(object color)
        {
           return this.Argb == ((ColorEmbedded)color).Argb;
        }

        public override int GetHashCode()
        {
            return this.Argb;  
        }
        public static ColorEmbedded FromARGB(byte a, byte r, byte g, byte b)
        {
            return new ColorEmbedded { Argb = a << 0x18 | r << 0x10 | g << 0x8 | b };
        }

        public static ColorEmbedded FromARGB(byte a, int rgb)
        {
            return new ColorEmbedded { Argb = a << 0x18 | rgb };
        }

        public static ColorEmbedded FromARGB(int argb)
        {
            return new ColorEmbedded { Argb = argb };
        }

        public static ColorEmbedded FromRGBHex(string htmlColor)
        {
            return ColorEmbedded.FromARGB(ColorTranslator.FromHtml(htmlColor).ToArgb());
        }

        public int Argb { get; set; }

        [HiddenProperty]
        public byte A
        {
            get { return (byte)((Argb >> 0x18) & 0xff); }
        }

        [HiddenProperty]
        public byte R
        {
            get { return (byte)((Argb >> 0x10) & 0xff); }
        }

        [HiddenProperty]
        public byte G
        {
            get { return (byte)((Argb >> 0x8) & 0xff); }
        }

        [HiddenProperty]
        public byte B
        {
            get { return (byte)(Argb & 0xff); }
        }

        public Color ToColor()
        {
            return Color.FromArgb(Argb);
        }

        public string RGBHex()
        {
            return "#" + R.ToString("X2") + G.ToString("X2") + B.ToString("X2");
        }

        public string ARGBHex()
        {
            return "#" + A.ToString("X2") + R.ToString("X2") + G.ToString("X2") + B.ToString("X2");
        }

        public string RGBAExpression()
        {
            return "rgb({0:X2}, {1:X2}, {2:X2}, {3})".FormatWith(R, G, B, (A / 255.0));
        }

        public override string ToString()
        {
            return "#" + Argb.ToString("X8");
        }
    }
}
