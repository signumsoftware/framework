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
            Strong = Em = Underlined = Strike = Lists = Titles = format; 
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

        public bool MaxTwoLineBreaks { get; set; }

    }

    public static class WikiParserExtensions
    {
        public static string HtmlSubstitute = "||HTML{0}||";

        public static string WikiParse(this string content, WikiSettings settings)
        {
            string result;

            //1: Replace token delimiters which are different from their encoded string so that they are not encoded
            result = Regex.Replace(content, "'{2,}", m => "####" + m.Length + "####");

            //2: Encode all text
            result = HttpUtility.HtmlEncode(result);

            //3: Replace encrypted tokens to original tokens 
            result = Regex.Replace(content, "####(?<count>\\d+)####", m => new string('\'', m.Groups["count"].Length));
            
            //4: Process tokens
            result = ProcessTokens(result, settings);

            //5: Process format
            result = ProcessFormat(result, settings);
           
            return result.Trim();
        }

        static string ProcessTokens(string content, WikiSettings settings)
        {
            return Regex.Replace(content, @"\[(?<content>([^\[\]]|\[\[|\]\])*)\]", m =>
            {
                string text = m.Groups["content"].Value.Replace("[[", "[").Replace("]]", "]");

                try
                {
                    return settings.TokenParser
                        .GetInvocationList()
                        .Cast<Func<string, string>>()
                        .Select(a => a(text))
                        .NotNull()
                        .First();
                }
                catch (Exception e)
                {
                    return "<span class=\"sf-wiki-error\">{0}</span>".Formato(m.Value);
                }
            });
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
            return "<h" + m.Groups["begin"].Length + ">" + m.Groups["content"].ToString().Trim() + "</h" + m.Groups["end"].Length + ">";
        }
    }

   
}
