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
        int argb;
        public int Argb
        {
            get { return argb; }
            set { SetToStr(ref argb, value, () => Argb); }
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
