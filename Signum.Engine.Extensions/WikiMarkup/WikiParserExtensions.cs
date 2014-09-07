using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Signum.Utilities;
using System.Web;

namespace Signum.Engine.WikiMarkup
{
    public class WikiSettings
    {
        public WikiSettings(bool format)
        {
            EncodeHtml = CodeRegion = LineBreaks = Strong = Em = Underlined = Strike = Lists = Titles = format;
            MaxTwoLineBreaks = true;
        }

        public Func<string, string> TokenParser;
        public bool Strong { get; set; }
        public bool Em { get; set; }
        public bool Underlined { get; set; }
        public bool Strike { get; set; }
        public bool Lists { get; set; }
        public bool Titles { get; set; }
        public bool LineBreaks { get; set; }

        public bool EncodeHtml { get; set; }

        public bool CodeRegion { get; set; }

        public bool MaxTwoLineBreaks { get; set; }

    }

    public static class WikiParserExtensions
    {
        public static string HtmlSubstitute = "||HTML{0}||";

        public static string WikiParse(this WikiSettings settings, string content)
        {
            using (HeavyProfiler.Log("Wiki"))
            {
                string result = content;

                Dictionary<string, string> codeRegions = null;
                result = SaveCodeRegions(result, out codeRegions);

                if (settings.EncodeHtml)
                {
                    //1: Replace token delimiters which are different from their encoded string so that they are not encoded
                    result = Regex.Replace(result, "'{2,}", m => "####" + m.Length + "####");

                    //2: Encode all text
                    result = HttpUtility.HtmlEncode(result);

                    //3: Replace encrypted tokens to original tokens 
                    result = Regex.Replace(result, "####(?<count>\\d+)####", m => new string('\'', int.Parse(m.Groups["count"].Value)));
                }

                result = ProcessTokens(result, settings);

                result = ProcessFormat(result, settings);

                result = WriteCodeRegions(result, codeRegions, settings);

                return result.Trim();
            }
        }

        private static string WriteCodeRegions(string result, Dictionary<string, string> codeRegions, WikiSettings settings)
        {
            result = Regex.Replace(result, @"%%%CODE%(?<guid>.+?)%%%", m =>
            {
                var value = codeRegions[m.Groups["guid"].Value];
                return settings.CodeRegion ?
                    "<pre><code>" + (settings.EncodeHtml ? HttpUtility.HtmlEncode(value) : value) + "</code></pre>" :
                    codeRegions[m.Groups["guid"].Value];
            });
            return result;
        }

        private static string SaveCodeRegions(string content, out Dictionary<string, string> codeRegions)
        {
            var regions =  new Dictionary<string, string>();

            var result = Regex.Replace(content, @"< *code( lang *= *""(?<lang>.*?)"")? *\>(?<code>.*?)</ *code *>", m =>
            {
                var guid = Guid.NewGuid();
                regions.Add(guid.ToString(), m.Groups["code"].Value);
                return "%%%CODE%{0}%%%".Formato(guid);
            }, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            codeRegions = regions;

            return result;
        }

        public static readonly Regex TokenRegex = new Regex(@"\[(?<content>([^\[\]]|\[\[|\]\])*)\]");

        static string ProcessTokens(string content, WikiSettings settings)
        {
            return TokenRegex.Replace(content, m =>
            {
                string text = m.Groups["content"].Value.Replace("[[", "[").Replace("]]", "]");

                try
                {
                    return settings.TokenParser
                        .GetInvocationListTyped()
                        .Select(a => a(text))
                        .NotNull()
                        .FirstEx();
                }
                catch
                {
                    return "<span class=\"sf-wiki-error\">{0}</span>".Formato(m.Value);
                }
            });
        }

        static string ProcessFormat(string content, WikiSettings settings)
        {
            // Replacing both
            content = Regex.Replace(content,
            @"(?<begin>''''')(?<content>.+?)(?<end>''''')",
            (settings.Strong && settings.Em) ? "<strong><em>${content}</em></strong>" : "${content}",
            RegexOptions.Compiled | RegexOptions.Singleline);

            // Replacing bolds
            content = Regex.Replace(content,
            @"(?<begin>''')(?<content>.+?)(?<end>''')",
            settings.Strong ? "<strong>${content}</strong>" : "${content}",
            RegexOptions.Compiled | RegexOptions.Singleline);

            // Replacing italics
            content = Regex.Replace(content,
            @"(?<begin>'')(?<content>.+?)(?<end>'')",
            settings.Em ? "<em>${content}</em>" : "${content}",
            RegexOptions.Compiled | RegexOptions.Singleline);

            // Replacing underlined
            content = Regex.Replace(content,
            @"(?<begin>__)(?<content>.+?)(?<end>__)",
            settings.Underlined ? "<u>${content}</u>" : "${content}",
            RegexOptions.Compiled | RegexOptions.Singleline);

            // Replacing strike
            content = Regex.Replace(content,
            @"(?<begin>\-\-)(?<content>.+?)(?<end>\-\-)",
            settings.Strike ? "<s>${content}</s>" : "${content}",
            RegexOptions.Compiled | RegexOptions.Singleline);

            // Replacing lists
            content = Regex.Replace(content,
             @"^\s*(?<begin>\*{1}[ ]?)(?<content>.+)(?<end>[^*]?)[\n]*",
             settings.Lists ? "<li>${content}</li>" : "${content} ",
             RegexOptions.Compiled | RegexOptions.Multiline);

            content = Regex.Replace(content,
             @"^\s*(?<begin>\#{1}[ ]?)(?<content>.+)(?<end>[^#]?)[\n]*",
             settings.Lists ? "<oli>${content}</oli>" : "${content} ",
             RegexOptions.Compiled | RegexOptions.Multiline);

            content = Regex.Replace(content,
            @"(?<content>\<li\>{1}.+\<\/li\>)",
            settings.Lists ? "<ul>${content}</ul>" : "${content} ",
            RegexOptions.Compiled);

            content = Regex.Replace(content,
            @"(?<content>\<oli\>{1}.+\<\/oli\>)",
            settings.Lists ? "<ol>${content}</ol>" : "${content} ",
            RegexOptions.Compiled);

            content = Regex.Replace(content,
            @"(?<content>oli\>{1})", "li>",
            RegexOptions.Compiled);


            // Replacing titles
            if (settings.Titles)
                content = Regex.Replace(content,
                @"(?<begin>={2,})(?<content>[^\n]+?)(?<end>={2,})[\n]*",
                m => "<h" + m.Groups["begin"].Length + ">" + m.Groups["content"].ToString().Trim() + "</h" + m.Groups["end"].Length + ">",
                RegexOptions.Compiled);
            else
                content = Regex.Replace(content,
                @"(?<begin>={2,})(?<content>[^\n]+?)(?<end>={2,})[\n]*",
                "${content}. ",
                RegexOptions.Compiled);

            //Remove multiple breakline  
            if (settings.MaxTwoLineBreaks)
            {
                content = Regex.Replace(content,
                    @"(?<content>(\r?\n){3,})", "\n\n",
                    RegexOptions.Compiled);
            }

            if (settings.LineBreaks)
            {
                content = Regex.Replace(content,
                    @"(?<content>\n)", settings.LineBreaks ? "<br/>" : ". ",
                    RegexOptions.Compiled);

                content = Regex.Replace(content,
                    @"(?<content>\r)", "",
                    RegexOptions.Compiled);
            }

            return content;
        }
    }
}
