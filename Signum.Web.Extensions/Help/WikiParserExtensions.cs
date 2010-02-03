using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Text.RegularExpressions;
using Signum.Engine.Help;
using Signum.Utilities;
using Signum.Entities.Operations;
using Signum.Engine.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Basics;
using Signum.Engine.Operations;
using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;

namespace Signum.Web.Extensions
{
    public static class WikiParserExtensions
    {
        public static string WikiUrl = "http://192.168.0.5:8085/";
        public static string ImagesFolder = "HelpImagenes/";
        public class WikiLink
        {
            public string Text { get; set; }
            public string Url { get; set; }
            public string Class { get; set; }

            public WikiLink(string url)
            {
                this.Url = url;
            }

            public WikiLink(string url, string text)
            {
                this.Url = url;
                this.Text = text;
            }

            public WikiLink(string url, string text, string @class)
            {
                this.Url = url;
                this.Text = text;
                this.Class = @class;
            }

            public string ToHtmlString()
            {
                return "<a {0} href=\"{1}\">{2}</a>".Formato(
                    Class.HasText() ? "class=\"" + Class + "\"" : "",
                    Url,
                    Text);
            }
        }
        public static string ProcessImages(string content)
        {
            StringBuilder sb = new StringBuilder();
            int firstIndex = 0;
            string result = string.Empty;

            Match m = Regex.Match(content, @"\[image(?<position>[^\|]+)\|(?<part1>[^\|\]]*)(\|(?<part2>.*))?\]");

            while (m.Success)
            {
                string position = m.Groups["position"].ToString();
                string part1 = m.Groups["part1"].ToString();
                string part2 = m.Groups["part2"].ToString();

                if (part2.HasText())
                {
                    //Has footer
                    result = "<div class=\"image{0}\"><img alt=\"{1}\" src=\"{2}{3}\"/><p class=\"imagedescription\">{1}</p></div>".Formato(position, part1, ImagesFolder, part2); 
                }
                else
                {
                    result = "<div class=\"image{0}\"><img src=\"{1}{2}\"/></div>".Formato(position, ImagesFolder, part1); 
                }                

                sb.Append(content.Substring(firstIndex, m.Index - firstIndex));
                sb.Append(result);
                firstIndex = m.Index + m.Length;
                m = m.NextMatch();
            }
            sb.Append(content.Substring(firstIndex, content.Length - firstIndex));
            return sb.ToString();
        }

        public static string WikiParse(this HtmlHelper html, string content)
        {
            StringBuilder sb = new StringBuilder();
            int firstIndex = 0;

            Match m = Regex.Match(content, @"\[(?<letter>.):(?<link>[^\|\]]*)(\|(?<text>.*?))?\]");
            
                while (m.Success)
                {
                    string letter = m.Groups["letter"].ToString();
                    string link = m.Groups["link"].ToString();
                    string text = m.Groups["text"].ToString();

                    string result = string.Empty;

                    switch (letter)
                    {
                        case WikiFormat.EntityLink:
                            Type t = HelpLogic.GetNameToType(link, false);
                            result = new WikiLink(t != null ? HelpLogic.EntityUrl(t) : link,
                                t != null ? t.NiceName() : link,
                                t != null ? "" : "unavailable").ToHtmlString();
                            break;

                        case WikiFormat.HyperLink:
                            result = new WikiLink(link, text).ToHtmlString();
                            break;

                        case WikiFormat.OperationLink:
                            Enum operation = EnumLogic<OperationDN>.ToEnum(link, false);
                            result = new WikiLink(operation != null ?
                                HelpLogic.EntityUrl(OperationLogic.FindType(operation)) + "#" + "o-" + OperationDN.UniqueKey(operation).Replace('.','_') : link,
                                text.HasText() ? text : (operation != null ? operation.NiceToString() : link),
                                operation != null ? "" : "unavailable").ToHtmlString();
                            break;

                        case WikiFormat.PropertyLink:
                            string[] parts = link.Split('.');
                            Type type = HelpLogic.GetNameToType(parts[0], false);
                            //TODO: NiceToString de la propiedad
                            result = new WikiLink(type != null ? HelpLogic.EntityUrl(type) + "#" + "p-" + parts[1] : link,
                                text.HasText() ? text : parts[1],
                                type != null ? "" : "unavailable").ToHtmlString();
                            break;

                        case WikiFormat.QueryLink:
                            object o = QueryLogic.ToQueryName(link, false);
                            if (o as Enum != null)
                            {
                                Enum query = (Enum)o;
                                result = new WikiLink(
                                    query != null ? (HelpLogic.EntityUrl(DynamicQueryManager.Current[query].EntityCleanType()) + "#" + "q-" + QueryUtils.GetQueryName(query).ToString().Replace(".", "_")) : link,
                                    text.HasText() ? text : (query != null ? QueryUtils.GetNiceQueryName(query) : link),
                                    query != null ? "" : "unavailable").ToHtmlString();
                            }
                            else
                            {
                                Type query = (Type)o;
                                result = new WikiLink(
                                    query != null ? (HelpLogic.EntityUrl(query) + "#" + "q-" + query.FullName.Replace(".", "_")) : link,
                                    text.HasText() ? text : (query != null ? QueryUtils.GetNiceQueryName(query) : link),
                                    query != null ? "" : "unavailable").ToHtmlString();

                                //Treat as entity
                            }
                            break;

                        case WikiFormat.WikiLink:
                            result = new WikiLink(WikiUrl + link, text.HasText() ? text : "Enlace a wiki").ToHtmlString();
                            break;
                    }

                    sb.Append(content.Substring(firstIndex, m.Index - firstIndex));
                    sb.Append(result);
                    firstIndex = m.Index + m.Length;
                    m = m.NextMatch();
                }
            sb.Append(content.Substring(firstIndex, content.Length - firstIndex));

            string postLinks = sb.ToString();
            postLinks = ProcessImages(postLinks);

            // Replacing both
            postLinks = Regex.Replace(postLinks,
            "(?<begin>'''''{1})(?<content>.+?)(?<end>'''''{1})",
            "<b><i>${content}</i></b>",
            RegexOptions.Compiled);

            // Replacing bolds
            postLinks = Regex.Replace(postLinks,
            "(?<begin>'''{1})(?<content>.+?)(?<end>'''{1})",
            "<b>${content}</b>",
            RegexOptions.Compiled);

            // Replacing italics
            postLinks = Regex.Replace(postLinks,
            "(?<begin>''{1})(?<content>.+?)(?<end>''{1})",
            "<i>${content}</i>",
            RegexOptions.Compiled);

            // Replacing underlined
            postLinks = Regex.Replace(postLinks,
            "(?<begin>__{1})(?<content>.+?)(?<end>__{1})",
            "<u>${content}</u>",
            RegexOptions.Compiled);

            // Replacing strike
            postLinks = Regex.Replace(postLinks,
            "(?<begin>\\-\\-{1})(?<content>.+?)(?<end>\\-\\-{1})",
            "<s>${content}</s>",
            RegexOptions.Compiled);

            // Replacing lists
            postLinks = Regex.Replace(postLinks,
     "(?<begin>\\*{1}[ ]{1})(?<content>.+)(?<end>[^*]?)",
     "<li>${content}</li>",
     RegexOptions.Compiled);

            postLinks = Regex.Replace(postLinks,
     "(?<begin>\\#{1}[ ]{1})(?<content>.+)(?<end>[^#]?)",
     "<oli>${content}</oli>",
     RegexOptions.Compiled);

     postLinks = Regex.Replace(postLinks,
     "(?<content>\\<li\\>{1}.+\\<\\/li\\>)",
     "<ul>${content}</ul>",
     RegexOptions.Compiled);

     postLinks = Regex.Replace(postLinks,
"(?<content>\\<oli\\>{1}.+\\<\\/oli\\>)",
"<ol>${content}</ol>",
RegexOptions.Compiled);

     postLinks = Regex.Replace(postLinks,
"(?<content>oli\\>{1})",
"li>",
RegexOptions.Compiled);

    // Assign the replace method to the MatchEvaluator delegate.
    MatchEvaluator meTitle = new MatchEvaluator(ReplaceTitle);


     // Replacing titles
     postLinks = Regex.Replace(postLinks,
     "(?<begin>={2,})(?<content>[^\\n]+?)(?<end>={2,})",
     meTitle,
     RegexOptions.Compiled);

            //Remove multiple breakline
     postLinks = Regex.Replace(postLinks,
    "(?<content>\n{2,})","\n",
    RegexOptions.Compiled);

/*     postLinks = Regex.Replace(postLinks,
     "(?<begin>\\*{1}[ ]{1})(?<content>.+)(?<end>[^*])",
     "<li1>${content}</li1>",
     RegexOptions.Compiled);
     postLinks = Regex.Replace(postLinks,
     "(?<content>\\<li1\\>{1}.+\\<\\/li1\\>)",
     "<ul>${content}</ul>",
     RegexOptions.Compiled);

     // Replacing lists
     postLinks = Regex.Replace(postLinks,
     "(?<begin>\\*{1})(?<content>.+)(?<end>[^*])",
     "<li2>${content}</li2>",
     RegexOptions.Compiled);
     postLinks = Regex.Replace(postLinks,
     "(?<content>\\<li2\\>{1}.+\\<\\/li2\\>)",
     "<ul>${content}</ul>",
     RegexOptions.Compiled);

     postLinks = Regex.Replace(postLinks,
     "(?<content>\\<li[0-9]+\\>{1})",
     "<li>",
     RegexOptions.Compiled);

     postLinks = Regex.Replace(postLinks,
     "(?<content>\\</li[0-9]+\\>{1})",
     "</li>",
     RegexOptions.Compiled);*/

          //  postLinks = Parse(postLinks, "'''", "<b>", "</b>");
          //  postLinks = Parse(postLinks, "''", "<i>", "</i>"); 
           // postLinks = WikiMarkupToHtml(postLinks);
            return postLinks.Trim();
        }

        public static string ReplaceTitle(Match m)
        {
            return "<h" + m.Groups["begin"].Length + ">" + m.Groups["content"].ToString() + "</h" + m.Groups["end"].Length + ">";
        }

        public static string Parse(string text, string token, string first, string last)
        {
            int firstIndex = 0;
            StringBuilder sb = new StringBuilder();
            Match regex = Regex.Match(text, token + "(?<content>.*?)" + token);
            while (regex.Success)
            {
                sb.Append(text.Substring(firstIndex, regex.Index - firstIndex));
                sb.Append(first + regex.Groups["content"].ToString() + last);
                firstIndex = regex.Index + regex.Length;
                regex = regex.NextMatch();
            }
            sb.Append(text.Substring(firstIndex, text.Length - firstIndex));
            return sb.ToString();
        }

     /*  public static string HandleList(MatchCollection mathes) {
           
         Dictionary<string, string> listtypes = new Dictionary<string,string> { "*" = "ul", "#" = "ol"};

		 StringBuilder output = new StringBuilder();
		 
           int newlevel = 0;
		    int listLevel = 0;
		while ($this->list_level!=$newlevel) {
			$listchar = substr($matches[1],-1);
			$listtype = $listtypes[$listchar];
			
			//$output .= "[".$this->list_level."->".$newlevel."]";
			
			if ($this->list_level>$newlevel) {
				$listtype = '/'.array_pop($this->list_level_types);
				$this->list_level--;
			} else {
				$this->list_level++;
				array_push($this->list_level_types,$listtype);
			}
			$output .= "\n<{$listtype}>\n";
		}
		
		if ($close) return $output;
		
		$output .= "<li>".$matches[2]."</li>\n";
		
		return $output;
	}*/
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
