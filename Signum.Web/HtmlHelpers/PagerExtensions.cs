using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using Signum.Utilities;
using System.Threading;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using System.Globalization;
using Signum.Web;

namespace Signum.Web
{    
    public static class PagerExtensions
    {
        public static MvcHtmlString NumericPagerEllipsis<T>(this HtmlHelper helper, Page<T> page, string urlFormat, string footer = null)
        {
            if (footer != null && footer.Contains("{0}"))
                footer = footer.FormatWith(page.TotalElements); 

            return NumericPagerEllipsis(helper, page.TotalPages, page.CurrentPage, urlFormat, footer);
        }

        public static MvcHtmlString NumericPagerEllipsis(this HtmlHelper helper, int totalPages, int currentPage, string urlFormat, string footer = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<div id=\"paging-container\"><table id=\"paging\"><tr>");

            int prevCurrentPage = Math.Max(1, currentPage - 4);
            int nextCurrentPage = Math.Min(totalPages, currentPage + 4);
            string linkFormat = "<td><a href=\"{0}\">{1}</a></td>";

            if (prevCurrentPage != 1)
            {
                //print first page and ellipsis                
                sb.AppendFormat(linkFormat, string.Format(urlFormat, 1), 1);
                if (prevCurrentPage > 1)
                    sb.Append("<td>...</td>");
            }

            for (int i = prevCurrentPage; i <= nextCurrentPage; i++)
            {
                if (i == currentPage)
                    sb.AppendFormat("<td><span>{0}</span></td>", i);
                else
                    sb.AppendFormat(linkFormat, string.Format(urlFormat, i), i);
            }

            if (nextCurrentPage != totalPages)
            {
                //print ellipsis and last page
                if (totalPages - 1 > nextCurrentPage)
                {
                    sb.Append("<td>...</td>");
                }

                sb.AppendFormat(linkFormat, string.Format(urlFormat, totalPages), totalPages);
            }

            sb.Append("</tr></table>");

            if (footer.HasText())
            {
                sb.AppendFormat("<div class=\"pager-footer\">{0}</div>", footer);
            }
            sb.Append("</div>");

            return MvcHtmlString.Create(sb.ToString());
        }
    }
}
