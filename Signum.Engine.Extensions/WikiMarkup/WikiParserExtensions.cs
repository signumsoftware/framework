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
            using (var t = HeavyProfiler.LogNoStackTrace("SaveCodeRegions"))
            {
                string result = content;

                Dictionary<string, string> codeRegions = null;
                result = SaveCodeRegions(result, out codeRegions);

                if (settings.EncodeHtml)
                {
                    t.Switch("HtmlEncode");
                    //1: Replace token delimiters which are different from their encoded string so that they are not encoded
                    result = Regex.Replace(result, "'{2,}", m => "####" + m.Length + "####");

                   
                    //2: Encode all text
                    result = HttpUtility.HtmlEncode(result);

                    //3: Replace encrypted tokens to original tokens 
                    result = Regex.Replace(result, "####(?<count>\\d+)####", m => new string('\'', int.Parse(m.Groups["count"].Value)));
                }
                t.Switch("ProcessTokens");
                result = ProcessTokens(result, settings);
                t.Switch("ProcessFormat");

                result = ProcessFormat(result, settings);
                t.Switch("WriteCodeRegions");

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
                return "%%%CODE%{0}%%%".FormatWith(guid);
            }, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            codeRegions = regions;

            return result;
        }

        public static readonly Regex TokenRegex = new Regex(@"\[(?<content>([^\[\]]|\[\[|\]\])+)\]");

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
                    return "<span class=\"sf-wiki-error\">{0}</span>".FormatWith(m.Value);
                }
            });
        }


        static Regex RegexBoldItalics = new Regex(@"(?<begin>''''')(?<content>.+?)(?<end>''''')", RegexOptions.Singleline);
        static Regex RegexBold = new Regex(@"(?<begin>''')(?<content>.+?)(?<end>''')", RegexOptions.Singleline);
        static Regex RegexItalics = new Regex(@"(?<begin>'')(?<content>.+?)(?<end>'')", RegexOptions.Singleline);
        static Regex RegexUnderline = new Regex(@"(?<begin>__)(?<content>.+?)(?<end>__)", RegexOptions.Singleline);
        static Regex RegexStrike = new Regex(@"(?<begin>\-\-)(?<content>.+?)(?<end>\-\-)", RegexOptions.Singleline);
        
        static Regex RegexLI = new Regex(@"^\s*(?<begin>\*{1}[ ]?)(?<content>.+)(?<end>[^*]?)[\n]*", RegexOptions.Singleline);
        static Regex RegexOLI = new Regex(@"^\s*(?<begin>\#{1}[ ]?)(?<content>.+)(?<end>[^#]?)[\n]*", RegexOptions.Singleline);
        static Regex RegexUL = new Regex(@"(?<content>\<li\>{1}.+\<\/li\>)", RegexOptions.Singleline);
        static Regex RegexOL = new Regex(@"(?<content>\<oli\>{1}.+\<\/oli\>)", RegexOptions.Singleline);
        static Regex RegexLIFin = new Regex(@"(?<content>oli\>{1})", RegexOptions.Singleline);

        static Regex RegexTitles = new Regex(@"(?<begin>={2,})(?<content>[^\n]+?)(?<end>={2,})[\n]*", RegexOptions.Singleline);

        static Regex RegexMaxTwoLineBreaks = new Regex(@"(?<begin>={2,})(?<content>[^\n]+?)(?<end>={2,})[\n]*");

        static Regex RegexNewLine = new Regex(@"(?<content>\n)");
        static Regex RegexCarrageReturn = new Regex(@"(?<content>\r)");


        static string ProcessFormat(string content, WikiSettings settings)
        {
            content = RegexBoldItalics.Replace(content,(settings.Strong && settings.Em) ? "<strong><em>${content}</em></strong>" : "${content}");
            content = RegexBold.Replace(content, settings.Strong ? "<strong>${content}</strong>" : "${content}");
            content = RegexItalics.Replace(content, settings.Em ? "<em>${content}</em>" : "${content}");
            content = RegexUnderline.Replace(content, settings.Underlined ? "<u>${content}</u>" : "${content}");
            content = RegexStrike.Replace(content, settings.Strike ? "<s>${content}</s>" : "${content}");
            
            content = RegexLI.Replace(content, settings.Lists ? "<li>${content}</li>" : "${content} ");
            content = RegexOLI.Replace(content, settings.Lists ? "<oli>${content}</oli>" : "${content} ");
            content = RegexUL.Replace(content, settings.Lists ? "<ul>${content}</ul>" : "${content} ");
            content = RegexOL.Replace(content, settings.Lists ? "<ol>${content}</ol>" : "${content} ");
            content = RegexLIFin.Replace(content, "li>");

            content = RegexTitles.Replace(content, m =>  
                settings.Titles ? ("<h" + m.Groups["begin"].Length + ">" + m.Groups["content"].ToString().Trim() + "</h" + m.Groups["end"].Length + ">") :
                m.Groups["content"].Value);

            //Remove multiple breakline  
            if (settings.MaxTwoLineBreaks)
            {
                content = RegexMaxTwoLineBreaks.Replace(content, "\n\n");
            }

            if (settings.LineBreaks)
            {
                content = RegexNewLine.Replace(content, "<br/>");
                content = RegexCarrageReturn.Replace(content, "");
            }

            return content;
        }
    }
}
