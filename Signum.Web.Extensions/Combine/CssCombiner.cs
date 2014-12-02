using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Signum.Utilities;
using System.Web;
using Signum.Web.PortableAreas;

namespace Signum.Web.Combine
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

        internal static StaticContentResult Combine(string[] virtualFiles)
        {
            StringBuilder sb = new StringBuilder();
          
            foreach (var vf in virtualFiles)
            {
                var content = CombineClient.ReadStaticFile(vf);
                content = ReplaceRelativeImg(content, VirtualPathUtility.ToAbsolute(vf));
                sb.AppendLine("/* {0} */".FormatWith(vf));
                content = Minify(content); 
                sb.AppendLine(content);
            }

            return new StaticContentResult(Encoding.UTF8.GetBytes(sb.ToString()), "bla.css");
        }

        static string ReplaceRelativeImg(string content, string virtualFile)
        {
            string result = Regex.Replace(content, @"url\([""']?(?<route>.+?)[""']?\)", m =>
            {
                string route = m.Groups["route"].Value;

                if (route.StartsWith("data"))
                    return m.Value;

                string absolute = VirtualPathUtility.Combine(virtualFile, route); 

                return "url(\"{0}\")".FormatWith(absolute);
            }); 

            return result;
        }
    }
}
