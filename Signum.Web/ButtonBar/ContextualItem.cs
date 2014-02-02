using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using System.Text;
using Signum.Entities;

namespace Signum.Web
{
    public class SelectedItemsMenuContext
    {
        public UrlHelper Url { get; set; }
        public ControllerContext ControllerContext { get; set; }
        public List<Lite<IdentifiableEntity>> Lites { get; set; }
        public object QueryName { get; set; }
        public Implementations Implementations { get; set; }
        public string Prefix { get; set; }
    }

    public delegate ContextualItem GetContextualItemDelegate(SelectedItemsMenuContext context);

    public class ContextualItem : ToolBarMenu
    {
        public string Content { get; set; }

        public override string ToString()
        {
            return Content;
        }
    }

    public static class ContextualItemsHelper
    {
        public static bool SelectedItemsMenuInSearchPage = false;
        public static event GetContextualItemDelegate GetContextualItemsForLites;

        public static void Start()
        {
            SelectedItemsMenuInSearchPage = true;

            ButtonBarQueryHelper.RegisterGlobalButtons(ctx =>
            {
                var selectedText = JavascriptMessage.searchControlMenuSelected.NiceToString();
                return new ToolBarButton[]
                {
                    new ToolBarMenu
                    {
                        Id = TypeContextUtilities.Compose(ctx.Prefix, "sfTmSelected"),
                        DivCssClass = ToolBarMenu.DefaultQueryCssClass + " sf-tm-selected",
                        Text = selectedText + " (0)",
                        AltText = selectedText,
                    }
                };
            });
        }

        public static List<ContextualItem> GetContextualItemListForLites(SelectedItemsMenuContext ctx)
        {
            List<ContextualItem> items = new List<ContextualItem>();
            if (!ctx.Lites.IsNullOrEmpty())
            {
                if (GetContextualItemsForLites != null)
                    items.AddRange(GetContextualItemsForLites.GetInvocationList()
                        .Cast<GetContextualItemDelegate>()
                        .Select(d => d(ctx))
                        .NotNull().ToList());
            }
            return items;
        }

        public static string ContextualItemsToString(this List<ContextualItem> items)
        {
            if (items == null || items.Count == 0)
                return "";

            StringBuilder sb = new StringBuilder();

            foreach (var item in items)
            {
                if (item != null)
                    sb.AppendLine(item.ToString());
            }

            return sb.ToString();
        }
    }
}