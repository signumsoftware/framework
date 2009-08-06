using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Signum.Web
{
    public static class Utils
    {
        public static string Specify(string functionText)
        {
            if (string.IsNullOrEmpty(functionText))
                return functionText;

            Match m = Regex.Match(functionText, @"^\s*function\s*\(\s*\)\s*{\s*(?<codigo>.*)\s*}\s*$");
            if (m != null)
                return m.Groups["codigo"].Value;

            return functionText; 
        }
    }
}
