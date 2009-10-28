using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Basics
{
    [Serializable]
    public class ColorDN : EmbeddedEntity
    {
        string hex;
        public string Hex
        {
            get { return hex; }
            set { SetToStr(ref hex, value, () => Hex); }
        }

        public override string ToString()
        {
            return hex;
        }
    }
}
