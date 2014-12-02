using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Web.PortableAreas;

namespace Signum.Web.Combine
{
    public static class JavascriptCombiner
    {
        public static bool UseGoogleClosure;

        static string Minify(string content)
        {
            if (UseGoogleClosure)
                return GoogleClosure.CompressSourceCode(content);

            return content;
        }

        internal static StaticContentResult Combine(string[] virtualFiles)
        {
            string combineComment = "/* {0} */".FormatWith(virtualFiles.ToString(",")) + "\r\n";

            StringBuilder sb = new StringBuilder();
            foreach (var vf in virtualFiles)
            {
                sb.AppendLine(CombineClient.ReadStaticFile(vf));
            }

            return new StaticContentResult(Encoding.UTF8.GetBytes(combineComment + Minify(sb.ToString())), "bla.js");
        }
    }
}
