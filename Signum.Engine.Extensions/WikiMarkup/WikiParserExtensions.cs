using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Signum.Utilities;
using System.Web;

namespace Signum.Engine
{
    public class WikiSettings
    {
        public WikiSettings(bool format)
        {
            Strong = Em = Underlined = Strike = Lists = Titles = format; 
            AllowRawHtml = false;
            LineBreaks = MaxTwoLineBreaks = true;
        }

        public Func<string, string> TokenParser;
        public bool Strong { get; set; }
        public bool Em { get; set; }
        public bool Underlined { get; set; }
        public bool Strike { get; set; }
        public bool Lists { get; set; }
        public bool Titles { get; set; }
        public bool LineBreaks { get; set; }

        public bool AllowRawHtml { get; set; }
        public bool MaxTwoLineBreaks { get; set; }

    }

    public static class WikiParserExtensions
    {
        public static string HtmlSubstitute = "||HTML{0}||";

        public static string WikiParse(this string content, WikiSettings settings)
        {
            string result;

            Dictionary<string, string> htmlFragments = null;

            if (settings.AllowRawHtml)
            {
                htmlFragments = new Dictionary<string, string>();

                content = Regex.Replace(content, @"<!\[CDATA\[(?<html>.*?)\]\]>", m =>
                {
                    var key = HtmlSubstitute.Formato(htmlFragments.Count);
                    htmlFragments.Add(key, m.Groups["html"].Value);
                    return key;
                }, RegexOptions.Singleline);
            }

            //1: Process tokens
            result = ProcessTokens(content, settings);

            //2: Process format
            result = ProcessFormat(result, settings);

            //3: Encode
            result = HttpUtility.HtmlEncode(result);
           
            if (settings.AllowRawHtml)
                result = Regex.Replace(result, @"\|\|(?<key>HTML\d+)\|\|", m => htmlFragments[m.Value]);

            return result.Trim();
        }

        static string ProcessTokens(string content, WikiSettings settings)
        {
            StringBuilder sb = new StringBuilder();
            int firstIndex = 0;

            Match m = Regex.Match(content, @"\[(.+?)\]");
            while (m.Success)
            {
                string text = m.Value;
                try
                {
                    text = settings.TokenParser
                    .GetInvocationList()
                    .Cast<Func<string, string>>()
                    .Select(a => a(m.Value)).NotNull().First();
                }
                catch (Exception)
                {
                }

                sb.Append(content.Substring(firstIndex, m.Index - firstIndex) + ((text != null) ? text : ""));
                firstIndex = m.Index + m.Length;
                m = m.NextMatch();
            }
            sb.Append(content.Substring(firstIndex, content.Length - firstIndex));
            return sb.ToString();
        }

        static string ProcessFormat(string content, WikiSettings settings)
        {
            // Replacing both
                content = Regex.Replace(content,
                "(?<begin>'''''{1})(?<content>.+?)(?<end>'''''{1})",
                (settings.Strong && settings.Em) ? "<strong><em>${content}</em></strong>" : "${content}",
                RegexOptions.Compiled);

            // Replacing bolds
                content = Regex.Replace(content,
                "(?<begin>'''{1})(?<content>.+?)(?<end>'''{1})",
                settings.Strong ? "<strong>${content}</strong>" : "${content}",
                RegexOptions.Compiled);

            // Replacing italics
                content = Regex.Replace(content,
                "(?<begin>''{1})(?<content>.+?)(?<end>''{1})",
                settings.Em ? "<em>${content}</em>" : "${content}",
                RegexOptions.Compiled);

            // Replacing underlined
                content = Regex.Replace(content,
                "(?<begin>__{1})(?<content>.+?)(?<end>__{1})",
                settings.Underlined ? "<u>${content}</u>" : "${content}",
                RegexOptions.Compiled);

            // Replacing strike
            content = Regex.Replace(content,
            "(?<begin>\\-\\-{1})(?<content>.+?)(?<end>\\-\\-{1})",
            settings.Strike ? "<s>${content}</s>" : "${content}",
            RegexOptions.Compiled);

            // Replacing lists
            content = Regex.Replace(content,
     "^\\s*(?<begin>\\*{1}[ ]?)(?<content>.+)(?<end>[^*]?)[\\n]*",
     settings.Lists ? "<li>${content}</li>" : "${content} ",
     RegexOptions.Compiled | RegexOptions.Multiline);

            content = Regex.Replace(content,
     "^\\s*(?<begin>\\#{1}[ ]?)(?<content>.+)(?<end>[^#]?)[\\n]*",
     settings.Lists ? "<oli>${content}</oli>" : "${content} ",
     RegexOptions.Compiled | RegexOptions.Multiline);

            content = Regex.Replace(content,
            "(?<content>\\<li\\>{1}.+\\<\\/li\\>)",
            settings.Lists ? "<ul>${content}</ul>" : "${content} ",
            RegexOptions.Compiled);

            content = Regex.Replace(content,
       "(?<content>\\<oli\\>{1}.+\\<\\/oli\\>)",
        settings.Lists ? "<ol>${content}</ol>" : "${content} ",
       RegexOptions.Compiled);

            content = Regex.Replace(content,
       "(?<content>oli\\>{1})",
       "li>",
       RegexOptions.Compiled);

            // Assign the replace method to the MatchEvaluator delegate.
            MatchEvaluator meTitle = new MatchEvaluator(ReplaceTitle);

            // Replacing titles
            if (settings.Titles)
                content = Regex.Replace(content,
                "(?<begin>={2,})(?<content>[^\\n]+?)(?<end>={2,})[\\n]*",
                meTitle,
                RegexOptions.Compiled);
            else
                content = Regex.Replace(content,
                "(?<begin>={2,})(?<content>[^\\n]+?)(?<end>={2,})[\\n]*",
                "${content}. ",
                RegexOptions.Compiled);

            //Remove multiple breakline  
            if (settings.MaxTwoLineBreaks)
            {
                content = Regex.Replace(content,
                    "(?<content>\n{3,})","\n\n", 
                    RegexOptions.Compiled);
            }
      
            content = Regex.Replace(content,
                "(?<content>\n)", settings.LineBreaks ? "<br/>" : ". ",
                RegexOptions.Compiled);

            content = Regex.Replace(content,
                "(?<content>\r)", "",
                RegexOptions.Compiled);
            return content;
        }

        public static string ReplaceTitle(Match m)
        {
            return "<h" + m.Groups["begin"].Length + ">" + m.Groups["content"].ToString() + "</h" + m.Groups["end"].Length + ">";
        }
    }

    public static class FormatHtmlExtensions
    {
        public static string AddHtml(this string content, string firstTag, string lastTag, string regex)
        {
            //Do not add tags inside the content of other tags

            Regex rTags = new Regex("<[^>]*>[^<]*</[^>]>");

            var matchesTags = rTags.Matches(content);

            Regex r = new Regex(regex.RemoveDiacritics(), RegexOptions.IgnoreCase);

            int lastIndex = 0;
            StringBuilder sb = new StringBuilder(content.Length);

            foreach (Match m in r.Matches(content.RemoveDiacritics()))
            {
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
                    sb.Append(content.Substring(m.Index, m.Length));
                }
                sb.Append(lastTag);
                lastIndex = m.Index + m.Length;
            }
            sb.Append(content.Substring(lastIndex, content.Length - lastIndex));
            return sb.ToString();
        }
    }
}
