using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Signum.Utilities;
using System.Web;
using Signum.Web.PortableAreas;

namespace Signum.Web.ScriptCombiner
{
    public static class CssCombiner
    {
        static string Minify(string content)
        {
            content = Regex.Replace(content, @"/\*.*?\*/", "");
            content = Regex.Replace(content, "(\\s{2,}|\\t+|\\r+|\\n+)", " ");
            content = content.Replace(" {", "{");
            content = content.Replace("{ ", "{");
            content = content.Replace(" :", ":");
            content = content.Replace(": ", ":");
            content = content.Replace(", ", ",");
            content = content.Replace("; ", ";");
            content = content.Replace(";}", "}");
            content = Regex.Replace(content, "/\\*[^\\*]*\\*+([^/\\*]*\\*+)*/", "$1");
            content = Regex.Replace(content, "(?<=[>])\\s{2,}(?=[<])|(?<=[>])\\s{2,}(?=&nbsp;)|(?<=&ndsp;)\\s{2,}(?=[<])", string.Empty);

            content = Regex.Replace(content, "[^\\}]+\\{\\}", string.Empty);  //Eliminamos reglas vacías

            return content;
        }

        internal static ScriptContentResult Combine(string[] virtualFiles)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("/* {0} */".Formato(virtualFiles.ToString(",")));
            foreach (var vf in virtualFiles)
            {
                var content = Combiner.ReadVirtualFile(vf);
                content = ReplaceRelativeImg(content, vf);
                sb.AppendLine(Minify(content));
            }

            return new ScriptContentResult(sb.ToString(), "text/css");
        }

        static string ReplaceRelativeImg(string content, string virtualFile)
        {
            string result = Regex.Replace(content, @"url\([""']?(?<route>.+?)[""']?\)", m =>
            {
                string route = m.Groups["route"].Value;

                string absolute = VirtualPathUtility.Combine(virtualFile, route); 

                return "url(\"{0}\")".Formato(absolute);
            }); 

            return result;
        }
    }
}
