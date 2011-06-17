using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Web.Properties;

namespace Signum.Web
{
    public delegate QuickLink[] GetQuickLinkItemDelegate<T>(HtmlHelper helper, T entity, string partialViewName, string prefix);  

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

        public static List<QuickLink> GetForEntity(HtmlHelper helper, IdentifiableEntity ident, string partialViewName, string prefix)
        {
            List<QuickLink> links = new List<QuickLink>();

            links.AddRange(globalLinks.SelectMany(a => (a(helper, ident, partialViewName, prefix) ?? Empty)).NotNull());

            List<Delegate> list = entityLinks.TryGetC(ident.GetType());
            if (list != null)
                links.AddRange(list.SelectMany(a => (QuickLink[])a.DynamicInvoke(helper, ident, partialViewName, prefix) ?? Empty).NotNull());

            return links;
        }

        static QuickLink[] Empty = new QuickLink[0];

        public static void Start()
        {
            WidgetsHelper.GetWidgetsForView += (helper, entity, partialViewName, prefix) => CreateWidget(helper, (IdentifiableEntity)entity, partialViewName, prefix);
        }

        public static WidgetItem CreateWidget(HtmlHelper helper, IdentifiableEntity identifiable, string partialViewName, string prefix)
        {
            List<QuickLink> quicklinks = GetForEntity(helper, identifiable, partialViewName, prefix);
            if (quicklinks == null || quicklinks.Count == 0) 
                return null;

            HtmlStringBuilder content = new HtmlStringBuilder();
            using (content.Surround(new HtmlTag("div").Class("sf-widget sf-quicklinks")))
            {
                using (content.Surround("ul"))
                {
                    foreach (var q in quicklinks)
                    {
                        using (content.Surround(new HtmlTag("li").Class("sf-quicklink")))
                        {
                            content.Add(
                                new HtmlTag("a").Attr("onclick", q.Execute()).SetInnerText(q.Text));
                        }
                    }
                }
            }

            return new WidgetItem
            {
                Id = TypeContextUtilities.Compose(prefix, "widgetQuicklinks"),
                Label = new HtmlTag("a", "quicklinks").InnerHtml(
                     Resources.Quicklinks.EncodeHtml(),
                     new HtmlTag("span").Class("sf-count").SetInnerText(quicklinks.Count.ToString()).ToHtml()
                     ),
                Content = content.ToHtml()
            };
        }     
    }

    public abstract class QuickLink : ToolBarButton
    {
        public QuickLink()
        {
            DivCssClass = "sf-quicklink";
        }

        public string Prefix { get; set; }

        public bool IsVisible { get; set; }

        public abstract string Execute();
    }

    public class QuickLinkFind : QuickLink
    {
        public FindOptions FindOptions { get; set; }
        
        public QuickLinkFind(FindOptions findOptions)
        {
            FindOptions = findOptions;
            IsVisible = Navigator.IsFindable(findOptions.QueryName);
            Text = QueryUtils.GetNiceName(findOptions.QueryName);
        }

        public QuickLinkFind(object queryName, string columnName, object value, bool hideColumn) :
            this(new FindOptions
            {
                QueryName = queryName,
                SearchOnLoad = true,
                ColumnOptionsMode = hideColumn ? ColumnOptionsMode.Remove: ColumnOptionsMode.Add,
                ColumnOptions = hideColumn ? new List<ColumnOption>{new ColumnOption(columnName)}: new List<ColumnOption>(),
                FilterMode = FilterMode.Hidden,
                FilterOptions = new List<FilterOption>
                {
                    new FilterOption(columnName, value),
                },
                Create = false
            })
        {
        }

        public override string Execute()
        {
            return new JsFindNavigator(new JsFindOptions
            {
                FindOptions = FindOptions,
                Prefix = Js.NewPrefix(Prefix ?? "")
            }).openFinder().ToJS();
        }
    }
}