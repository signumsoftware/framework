using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

namespace Signum.Web.ScriptCombiner
{
    public static class JavascriptCombiner
    {
        public static bool UseGoogleClosure; 

        static string Minify(string content)
        {
            string minified = new JavaScriptMinifier().Minify(content);

            if (UseGoogleClosure) return GoogleClosure.CompressSourceCode(minified);

            return minified;
        }

        internal static ScriptContentResult Combine(string[] virtualFiles)
        {
            string combineComment = "/* {0} */".Formato(virtualFiles.ToString(",")) + "\r\n";

            StringBuilder sb = new StringBuilder();
            foreach (var vf in virtualFiles)
            {
                sb.AppendLine(Combiner.ReadVirtualFile(vf));
            }

            return new ScriptContentResult
            {
                Content = combineComment + Minify(sb.ToString()),
                CacheDuration = TimeSpan.FromDays(10),
                ContentType = "application/x-javascript"
            };
        }
    }
}
