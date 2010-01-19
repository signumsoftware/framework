using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Text.RegularExpressions;
using Signum.Engine.Help;
using Signum.Utilities;

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
                    string s = m.Groups["link"].ToString();
                    Type t = HelpLogic.ToType(s, false);
                    content = content.Replace(m.Value, "<a {0} href=\"{1}\">{2}</a>".Formato(
                        t != null ? "" : "class=\"unavailable\"",
                        t != null ? HelpLogic.EntityUrl(t) : s,
                        t != null ? t.NiceName() : s));
                }
            }
            while (m.Success);
            return content;
        }
    }

    public static class FormatHtmlExtensions
    {
        public static string AddHtml(this HtmlHelper html, string content, string firstTag, string lastTag, string regex)
        {
            //Do not add tags inside the content of other tags

            Regex rTags = new Regex("<[^>]*>[^<]*</[^>]>");

            var matchesTags = rTags.Matches(content);


            Regex r = new Regex(regex, RegexOptions.IgnoreCase);
            
            int lastIndex = 0;
            StringBuilder sb = new StringBuilder(content.Length);

            foreach (Match m in r.Matches(content)){
                bool skip = false;
                foreach (Match mt in matchesTags)
                {
                    if (mt.Index <= m.Index && mt.Index + mt.Length > m.Index + m.Length)
                    {
                        skip = true;
                        break;
                    }
                }
                if (skip)
                    sb.Append(m.Value);
                else
                {
                    sb.Append(content.Substring(lastIndex, m.Index - lastIndex));
                    sb.Append(firstTag);
                    sb.Append(m.Value);
                }
                sb.Append(lastTag);
                lastIndex = m.Index+m.Length;
            }
            sb.Append(content.Substring(lastIndex, content.Length-lastIndex));
            return sb.ToString();
        }
    }
}
