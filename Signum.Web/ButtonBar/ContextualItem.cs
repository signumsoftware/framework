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

    public static class ContextualItemsHelper
    {
        public static bool SelectedItemsMenuInSearchPage = false;
        public static event Func<SelectedItemsMenuContext, List<IMenuItem>> GetContextualItemsForLites;

        public static void Start()
        {
            SelectedItemsMenuInSearchPage = true;
        }

        public static List<IMenuItem> GetContextualItemListForLites(SelectedItemsMenuContext ctx)
        {
            List<IMenuItem> items = new List<IMenuItem>();
            if (!ctx.Lites.IsNullOrEmpty())
            {
                if (GetContextualItemsForLites != null)
                {
                    foreach (Func<SelectedItemsMenuContext, List<IMenuItem>> d in GetContextualItemsForLites.GetInvocationList())
                    {
                        var newItems = d(ctx);

                        if (newItems != null)
                        {
                            if (items.Any() && newItems.NotNull().Any())
                                items.Add(new MenuItemSeparator());

                            items.AddRange(newItems.NotNull());
                        }
                    }
                }
            }
            return items;
        }
    }
}