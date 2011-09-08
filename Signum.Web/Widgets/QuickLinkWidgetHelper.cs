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
    public delegate QuickLinkItem[] GetQuickLinkItemDelegate<T>(T entity, HtmlHelper helper, string partialViewName);  

    public static class QuickLinkWidgetHelper
    {
        static Dictionary<Type, List<Delegate>> entityLinks = new Dictionary<Type, List<Delegate>>();
        static List<GetQuickLinkItemDelegate<IdentifiableEntity>> globalLinks = new List<GetQuickLinkItemDelegate<IdentifiableEntity>>();

        public static void RegisterEntityLinks<T>(GetQuickLinkItemDelegate<T> getQuickLinks)
            where T : IdentifiableEntity
        {
            entityLinks.GetOrCreate(typeof(T)).Add(getQuickLinks);
        }

        public static void RegisterGlobalLinks(GetQuickLinkItemDelegate<IdentifiableEntity> getQuickLinks)
        {
            globalLinks.Add(getQuickLinks);
        }

        public static List<QuickLinkItem> GetForEntity(IdentifiableEntity ident, HtmlHelper helper, string partialViewName)
        {
            List<QuickLinkItem> links = new List<QuickLinkItem>();

            links.AddRange(globalLinks.SelectMany(a => (a(ident, helper, partialViewName)??Empty)).NotNull());

            List<Delegate> list = entityLinks.TryGetC(ident.GetType());
            if (list != null)
                links.AddRange(list.SelectMany(a => (QuickLinkItem[])a.DynamicInvoke(ident, helper, partialViewName) ?? Empty).NotNull());

            return links;
        }

        static QuickLinkItem[] Empty = new QuickLinkItem[0];

        public static void Start()
        {
            WidgetsHelper.GetWidgetsForView += (helper, entity, partialViewName) => CreateWidget(helper, (IdentifiableEntity)entity, partialViewName);
        }

        public static WidgetItem CreateWidget(HtmlHelper helper, IdentifiableEntity identifiable, string partialViewName)
        {
            List<QuickLinkItem> quicklinks = GetForEntity(identifiable, helper, partialViewName);
            if (quicklinks == null || quicklinks.Count == 0) return null;

            HtmlStringBuilder content = new HtmlStringBuilder();
            using (content.Surround(new HtmlTag("div").Class("widget quicklinks")))
            {
                using (content.Surround("ul"))
                    foreach (var q in quicklinks)
                    {
                        using (content.Surround("li"))
                            content.Add(new HtmlTag("a")
                                .Attr("onclick", "javascript:new SF.FindNavigator({0}).openFinder();".Formato(JsFindOptions(q).ToJS()))
                                .SetInnerText(QueryUtils.GetNiceName(q.FindOptions.QueryName)));
                    }
            }


            return new WidgetItem
            {
                Content = content.ToHtml(),
                Label = new HtmlTag("a","Quicklinks").InnerHtml(
                    "Quicklinks".EncodeHtml(),
                     new HtmlTag("span")
                        .Class("count")
                        .Class(quicklinks.Count == 0 ? "disabled" : null)
                        .SetInnerText(quicklinks.Count.ToString())
                     ),
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
                },
                Prefix = Js.NewPrefix("")
            };
            return foptions;
        }

    }
    public class QuickLinkItem : ToolBarButton
    {
        public QuickLinkItem(object queryName, List<FilterOption> filterOptions)
        {
            DivCssClass = "sf-quick-link";
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