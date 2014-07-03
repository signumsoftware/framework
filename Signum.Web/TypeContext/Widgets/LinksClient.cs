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
using Newtonsoft.Json.Linq;

namespace Signum.Web
{
    static class QuickLinkWidgetHelper
    {  
        internal static IWidget CreateWidget(WidgetContext ctx)
        {
            var ident = ctx.Entity as IdentifiableEntity;

            if(ident == null || ident.IsNew)
                return null;

            List<QuickLink> quicklinks = LinksClient.GetForEntity(ident.ToLiteFat(), ctx.PartialViewName, ctx.Prefix);
            if (quicklinks == null || quicklinks.Count == 0)
                return null;

            return new Widget 
            {
                Id = TypeContextUtilities.Compose(ctx.Prefix, "quicklinksWidget"),
                Class = "sf-quicklinks",
                Title = QuickLinkMessage.Quicklinks.NiceToString(),
                IconClass = "glyphicon glyphicon-star",
                Text = quicklinks.Count.ToString(),
                Items = quicklinks.OrderBy(a=>a.Order).Cast<IMenuItem>().ToList(),
                Active = true,
            };
        }
    }

    static class QuickLinkContextualMenu
    {
        internal static List<IMenuItem> ContextualItemsHelper_GetContextualItemsForLite(SelectedItemsMenuContext ctx)
        {
            if (ctx.Lites.IsNullOrEmpty() || ctx.Lites.Count > 1)
                return null;

            List<QuickLink> quickLinks = LinksClient.GetForEntity(ctx.Lites[0], null, ctx.Prefix);
            if (quickLinks.IsNullOrEmpty())
                return null;

            List<IMenuItem> menuItems = new List<IMenuItem>();
            menuItems.Add(new MenuItemHeader(QuickLinkMessage.Quicklinks.NiceToString()));
            menuItems.AddRange(quickLinks);

            return menuItems;
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
                    WidgetsHelper.GetWidget += QuickLinkWidgetHelper.CreateWidget;

                if (contextualItems)
                    ContextualItemsHelper.GetContextualItemsForLites += QuickLinkContextualMenu.ContextualItemsHelper_GetContextualItemsForLite;
            }
        }
    }

    public abstract class QuickLink : IMenuItem
    {
        public string Prefix { get; set; }
        public bool IsVisible { get; set; }
        public string Text { get; set; }
        public double Order { get; set; }
        public string Name { get; set; }

        public QuickLink()
        {
        }

        public MvcHtmlString ToHtml()
        {
            return new HtmlTag("li").Class("sf-quick-link").Attr("data-name", Name).InnerHtml(Execute());
        }

        public abstract MvcHtmlString Execute();
    }

    public class QuickLinkAction : QuickLink
    {
        public string Url { get; set; }


        public QuickLinkAction(Enum nameAndText, string url): this
            (nameAndText.ToString(), nameAndText.NiceToString(), url)
        {
        }

        public QuickLinkAction(string name, string text, string url)
        {
            Text = text;
            Url = url;
            Name = name; 
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
            Name = Navigator.ResolveWebQueryName(findOptions.QueryName);
        }

        public QuickLinkFind(object queryName, string columnName, object value, bool hideColumn) :
            this(new FindOptions
            {
                QueryName = queryName,
                SearchOnLoad = true,
                ColumnOptionsMode = hideColumn ? ColumnOptionsMode.Remove: ColumnOptionsMode.Add,
                ColumnOptions = hideColumn ? new List<ColumnOption>{new ColumnOption(columnName)}: new List<ColumnOption>(),
                ShowFilters = false,
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
            JObject jsFindOptions = FindOptions.ToJS(TypeContextUtilities.Compose("New", Prefix));

            return new HtmlTag("a")
                .Attr("onclick", JsModule.Finder["explore"](jsFindOptions).ToString())
                .SetInnerText(Text);
        }
    }

    public class QuickLinkView : QuickLink
    {
        public Lite<IdentifiableEntity> lite;

        public QuickLinkView(Lite<IdentifiableEntity> liteEntity)
        {
            lite = liteEntity;
            IsVisible = Navigator.IsNavigable(lite.EntityType, null, isSearch: false);
            Text = lite.EntityType.NiceName();
            Name = Navigator.ResolveWebTypeName(liteEntity.EntityType);
        }

        public override MvcHtmlString Execute()
        {
            return new HtmlTag("a").Attr("href", Navigator.NavigateRoute(lite)).SetInnerText(Text);
        }
    }
}