using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.DynamicQuery;

namespace Signum.Web
{
    public delegate List<QuickLinkItem> GetQuickLinksDelegate(HtmlHelper helper, IdentifiableEntity entity, string partialViewName);

    public static class QuickLinkWidgetHelper
    {
        public static event GetQuickLinksDelegate GetQuickLinks;


        public static void Start() {
            WidgetsHelper.GetWidgetsForView += (helper, entity, partialViewName) => CreateWidget(helper, (IdentifiableEntity)entity, partialViewName);
        }

        public static WidgetItem CreateWidget(HtmlHelper helper, IdentifiableEntity identifiable, string partialViewName)
        {
            List<QuickLinkItem> quicklinks = new List<QuickLinkItem>();
            if (GetQuickLinks != null) quicklinks = GetQuickLinks(helper, identifiable, partialViewName);
            if (quicklinks == null || quicklinks.Count == 0) return null;
            return new WidgetItem
            {
                Content =
                    @"<div class='widget quicklinks'><ul>{0}</ul>
                    </div>".Formato(quicklinks.ToString(q => "<li><a onclick=\"javascript:OpenFinder({0});\">{1}</li>".Formato(JsFindOptions(q).ToJS(), QueryUtils.GetNiceQueryName(q.FindOptions.QueryName)), "")),
                Label = "<a id='{0}'>{0}<span class='count {1}'>{2}</span></a>".Formato("Quicklinks", quicklinks.Count == 0 ? "disabled" : "", quicklinks.Count),
                Id = "Notes",
                Show = true,
            };
        }

        private static JsFindOptions JsFindOptions(QuickLinkItem quickLinkItem)
        {
            JsFindOptions foptions = new JsFindOptions
            {
                FindOptions = new FindOptions
                {
                    QueryName = quickLinkItem.FindOptions.QueryName,
                    Create = false,
                    SearchOnLoad = true,
                    FilterMode = FilterMode.Hidden,
                    FilterOptions = quickLinkItem.FindOptions.FilterOptions
                }
            };
            return foptions;
        }

    }
    public class QuickLinkItem : WebMenuItem
    {
        public QuickLinkItem(object queryName, List<FilterOption> filterOptions)
        {
            DivCssClass = "QuickLink";
            FindOptions = new FindOptions
            {
                QueryName = queryName,
                FilterOptions = filterOptions,
                AllowMultiple = false,
                FilterMode = FilterMode.Hidden,
                SearchOnLoad = true,
                Create = false,
            };
        }

        public FindOptions FindOptions { get; set; }
    }
}