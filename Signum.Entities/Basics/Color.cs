using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Drawing;

namespace Signum.Entities.Basics
{
    [Serializable]
    public class ColorDN : EmbeddedEntity
    {
        public ColorDN()
        {
        }

        public static ColorDN FromARGB(byte a, byte r, byte g, byte b)
        {
            return new ColorDN { Argb = a << 0x18 | r << 0x10 | g << 0x8 | b };
        }

        public static ColorDN FromARGB(byte a, int rgb)
        {
            return new ColorDN { Argb = a << 0x18 | rgb };
        }

        public static ColorDN FromARGB(int argb)
        {
            return new ColorDN { Argb = argb };
        }

        int argb;
        public int Argb
        {
            get { return argb; }
            set { SetToStr(ref argb, value, () => Argb); }
        }

        [HiddenProperty]
        public byte A
        {
            get { return (byte)((argb >> 0x18) & 0xff); }
        }

        [HiddenProperty]
        public byte R
        {
            get { return (byte)((argb >> 0x10) & 0xff); }
        }

        [HiddenProperty]
        public byte G
        {
            get { return (byte)((argb >> 0x8) & 0xff); }
        }

        [HiddenProperty]
        public byte B
        {
            get { return (byte)(argb & 0xff); }
        }

        public Color ToColor()
        {
            return Color.FromArgb(argb); 
        }

        public override string ToString()
        {
            return ToColor().ToString();
        }
    }
}
