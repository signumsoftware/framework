using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Text.RegularExpressions;

namespace Signum.Web.Extensions
{
    public static class WikiParserExtensions
    {
        public static string WikiParse(this HtmlHelper html, string content)
        {
            Match m;
            do
            {
                m = Regex.Match(content, "\\[(?<link>[^\\]]*)\\]");
                if (m.Success)
                {
                    content = content.Replace(m.Value, "<a href=\"Help/" + m.Groups["link"] + "\">" + m.Groups["link"] + "</a>");
                }
            }
            while (m.Success);
            return content;
        }
    }
}
