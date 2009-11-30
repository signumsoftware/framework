using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Signum.Excel
{
    public static class Util
    {
        public static string ToStringExcel(this DateTime datetime)
        {
            return datetime.ToString("s"); 
        }

        public static string ToStringExcel(this decimal datetime)
        {
            return datetime.ToString(CultureInfo.InvariantCulture);
        }
    }
}
