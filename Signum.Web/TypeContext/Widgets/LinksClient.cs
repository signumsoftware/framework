using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Engine;
using System.Reflection;

namespace Signum.Web
{
   

    static class QuickLinkWidgetHelper
    {  
        internal static WidgetItem CreateWidget(IdentifiableEntity identifiable, string partialViewName, string prefix)
        {
            if (identifiable.IsNew)
                return null;

            List<QuickLink> quicklinks = LinksClient.GetForEntity(identifiable.ToLiteFat(), partialViewName, prefix);
            if (quicklinks == null || quicklinks.Count == 0)
                return null;

            HtmlStringBuilder content = new HtmlStringBuilder();
            using (content.Surround(new HtmlTag("ul").Class("sf-menu-button sf-widget-content sf-quicklinks")))
            {
                foreach (var q in quicklinks)
                {
                    using (content.Surround(new HtmlTag("li").Class("sf-quicklink")))
                    {
                        content.Add(q.Execute());
                    }
                }
            }

            HtmlStringBuilder label = new HtmlStringBuilder();
            using (label.Surround(new HtmlTag("a").Class("sf-widget-toggler sf-quicklink-toggler").Attr("title", QuickLinkMessage.Quicklinks.NiceToString())))
            {
                label.Add(new HtmlTag("span")
                    .Class("ui-icon ui-icon-star")
                    .InnerHtml(QuickLinkMessage.Quicklinks.NiceToString().EncodeHtml())
                    .ToHtml());

                label.Add(new HtmlTag("span")
                    .Class("sf-widget-count")
                    .SetInnerText(quicklinks.Count.ToString())
                    .ToHtml());
            }

            return new WidgetItem
            {
                Id = TypeContextUtilities.Compose(prefix, "quicklinksWidget"),
                Label = label.ToHtml(),
                Content = content.ToHtml()
            };
        }
    }

    static class QuickLinkContextualMenu
    {
        internal static ContextualItem ContextualItemsHelper_GetContextualItemsForLite(SelectedItemsMenuContext ctx)
        {
            if (ctx.Lites.IsNullOrEmpty() || ctx.Lites.Count > 1)
                return null;

            List<QuickLink> quicklinks = LinksClient.GetForEntity(ctx.Lites[0], null, ctx.Prefix);
            if (quicklinks == null || quicklinks.Count == 0)
                return null;

            HtmlStringBuilder content = new HtmlStringBuilder();
            using (content.Surround(new HtmlTag("ul").Class("sf-search-ctxmenu-quicklinks")))
            {
                string ctxItemClass = "sf-search-ctxitem";

                content.AddLine(new HtmlTag("li")
                    .Class(ctxItemClass + " sf-search-ctxitem-header")
                    .InnerHtml(
                        new HtmlTag("span").InnerHtml(QuickLinkMessage.Quicklinks.NiceToString().EncodeHtml()))
                    );

                foreach (var q in quicklinks)
                {
                    using (content.Surround(new HtmlTag("li").Class(ctxItemClass)))
                    {
                        content.Add(q.Execute());
                    }
                }
            }

            return new ContextualItem
            {
                Id = TypeContextUtilities.Compose(ctx.Prefix, "ctxItemQuickLinks"),
                Content = content.ToHtml().ToString()
            };
        }
    }

    public class QuickLinkContext
    {
        public string PartialViewName { get; internal set; }
        public string Prefix { get; internal set; }
    }


    public static class LinksClient
    {
        static Polymorphic<Func<Lite<IdentifiableEntity>, QuickLinkContext, QuickLink[]>> entityLinks =
            new Polymorphic<Func<Lite<IdentifiableEntity>, QuickLinkContext, QuickLink[]>>(
                merger: (currentVal, baseVal, interfaces) => currentVal.Value + baseVal.Value,
                minimumType: typeof(IdentifiableEntity));


        public static void RegisterEntityLinks<T>(Func<Lite<T>, QuickLinkContext, QuickLink[]> getQuickLinks)
            where T : IdentifiableEntity
        {
            var current = entityLinks.GetDefinition(typeof(T));

            current += (t, p0) => getQuickLinks((Lite<T>)t, p0);

            entityLinks.SetDefinition(typeof(T), current);
        }


        public static List<QuickLink> GetForEntity(Lite<IdentifiableEntity> ident, string partialViewName, string prefix)
        {
            List<QuickLink> links = new List<QuickLink>();

            QuickLinkContext ctx = new QuickLinkContext { PartialViewName = partialViewName, Prefix = prefix }; 

            var func  =  entityLinks.TryGetValue(ident.EntityType);
            if (func != null)
            {
                foreach (var item in func.GetInvocationList().Cast<Func<Lite<IdentifiableEntity>, QuickLinkContext, QuickLink[]>>())
                {
                    var array = item(ident, ctx);
                    if (array != null)
                        links.AddRange(array.NotNull().Where(l => l.IsVisible));
                }
            }

            return links;
        }

        static QuickLink[] Empty = new QuickLink[0];

        public static void Start(bool widget, bool contextualItems)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (widget)
                    WidgetsHelper.GetWidgetsForView += (entity, partialViewName, prefix) => entity is IdentifiableEntity ? QuickLinkWidgetHelper.CreateWidget((IdentifiableEntity)entity, partialViewName, prefix) : null;

                if (contextualItems)
                    ContextualItemsHelper.GetContextualItemsForLites += QuickLinkContextualMenu.ContextualItemsHelper_GetContextualItemsForLite;
            }
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

        public abstract MvcHtmlString Execute();
    }

    public class QuickLinkAction : QuickLink
    {
        public string Url { get; set; }
        
        public QuickLinkAction(string text, string url)
        {
            Text = text;
            Url = url;
            IsVisible = true;
        }

        public override MvcHtmlString Execute()
        {
            return new HtmlTag("a").Attr("href", Url).SetInnerText(Text);
        }
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

        public override MvcHtmlString Execute()
        {
            string onclick = JsFindNavigator.openFinder(new JsFindOptions
            {
                FindOptions = FindOptions,
                Prefix = Js.NewPrefix(Prefix ?? "")
            }).ToJS();

            return new HtmlTag("a").Attr("onclick", onclick).SetInnerText(Text);
        }
    }

    public class QuickLinkView : QuickLink
    {
        public Lite<IdentifiableEntity> lite;

        public QuickLinkView(Lite<IdentifiableEntity> liteEntity)
        {
            lite = liteEntity;
            IsVisible = Navigator.IsNavigable(lite.EntityType, isSearchEntity: false);
            Text = lite.EntityType.NiceName();
        }

        public override MvcHtmlString Execute()
        {
            return new HtmlTag("a").Attr("href", Navigator.NavigateRoute(lite)).SetInnerText(Text);
        }
    }
}