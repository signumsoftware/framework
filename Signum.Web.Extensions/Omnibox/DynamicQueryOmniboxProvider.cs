using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Omnibox;
using Signum.Entities.DynamicQuery;
using System.Web;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using System.Web.Mvc;

namespace Signum.Web.Omnibox
{
    public class DynamicQueryOmniboxProvider : OmniboxClient.OmniboxProvider<DynamicQueryOmniboxResult>
    {
        public override OmniboxResultGenerator<DynamicQueryOmniboxResult> CreateGenerator()
        {
            return new DynamicQueryOmniboxResultGenerator(DynamicQueryManager.Current.GetQueryNames());
        }

        public override MvcHtmlString RenderHtml(DynamicQueryOmniboxResult result)
        {
            MvcHtmlString html = result.QueryNameMatch.ToHtml();

            FindOptions findOptions = new FindOptions(result.QueryName);

            foreach (var item in result.Filters)
            {
                html = html.Concat(new MvcHtmlString(" "));

                QueryToken last = null;
                if (item.QueryTokenMatches != null)
                {
                    foreach (var tokenMatch in item.QueryTokenMatches)
                    {
                        html = html.Concat("{0}{1}".FormatHtml(
                            last != null ? "." : "",
                            tokenMatch.ToHtml()));

                        last = (QueryToken)tokenMatch.Value;
                    }
                }

                if (item.QueryToken != last)
                {
                    html = html.Concat("{0}{1}".FormatHtml(
                        last != null ? "." : "",
                        ColoredSpan(item.QueryToken.Key, "gray")));
                }

                if (item.CanFilter.HasText())
                {
                    html = html.Concat(ColoredSpan(item.CanFilter, "red"));
                }
                else if (item.Operation != null)
                {
                    html = html.Concat(new HtmlTag("b").InnerHtml(
                        new MvcHtmlString(DynamicQueryOmniboxResultGenerator.ToStringOperation(item.Operation.Value))).ToHtml());

                    if (item.Value == DynamicQueryOmniboxResultGenerator.UnknownValue)
                        html = html.Concat(ColoredSpan(Signum.Web.Extensions.Properties.Resources.Unknown, "red"));
                    else if (item.ValuePack != null)
                        html = html.Concat(item.ValuePack.ToHtml());
                    else if (item.Syntax != null && item.Syntax.Completion == FilterSyntaxCompletion.Complete)
                        html = html.Concat(new HtmlTag("b").InnerHtml(new MvcHtmlString(DynamicQueryOmniboxResultGenerator.ToStringValue(item.Value))).ToHtml());
                    else
                        html = html.Concat(ColoredSpan(DynamicQueryOmniboxResultGenerator.ToStringValue(item.Value), "gray"));
                }

                if (item.QueryToken != null && item.Operation != null && item.Value != DynamicQueryOmniboxResultGenerator.UnknownValue)
                {
                    if (findOptions.FilterOptions == null)
                        findOptions.FilterOptions = new List<FilterOption>();

                    var filter = new FilterOption(item.QueryToken.FullKey(), item.Value);
                    if (item.Operation != null)
                        filter.Operation = item.Operation.Value;

                    findOptions.FilterOptions.Add(filter);    
                }                
            } 

            html = html.Concat(Icon());

            html = new HtmlTag("a")
                    .Attr("href", findOptions.ToString())
                    .InnerHtml(html).ToHtml();

            return html;
        }
        
        public override MvcHtmlString Icon()
        {
            return ColoredSpan(" ({0})".Formato(typeof(QueryDN).NiceName()), "orange");
        }
    }
}
