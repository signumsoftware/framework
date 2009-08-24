using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Extensions.Basics
{
    [Serializable]
    public class DateSpanDN : EmbeddedEntity
    {
        int year;
        public int Year
        {
            get { return year; }
            set { Set(ref year, value, "Year"); }
        }

    }
}
