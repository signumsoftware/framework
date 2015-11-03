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
        public List<Lite<Entity>> Lites { get; set; }
        public object QueryName { get; set; }
        public Implementations Implementations { get; set; }
        public string Prefix { get; set; }
    }

    public static class ContextualItemsHelper
    {
        public static bool SelectedItemsMenuInSearchPage = true;
        public static event Func<SelectedItemsMenuContext, MenuItemBlock> GetContextualItemsForLites;

        public static List<IMenuItem> GetContextualItemListForLites(SelectedItemsMenuContext ctx)
        {
            List<IMenuItem> items = new List<IMenuItem>();
            if (!ctx.Lites.IsNullOrEmpty())
            {
                if (GetContextualItemsForLites != null)
                {
                    foreach (var block in GetContextualItemsForLites.GetInvocationListTyped().Select(d=>d(ctx)).NotNull().OrderBy(a=>a.Order))
                    {
                        if (items.Any() && block.Items.NotNull().Any())
                            items.Add(new MenuItemSeparator());

                        if (block.Header.HasText())
                            items.Add(new MenuItemHeader(block.Header));

                        items.AddRange(block.Items.NotNull());
                    }
                }
            }
            return items;
        }
    }

    public class MenuItemBlock
    {
        public int Order;
        public string Header; 
        public IEnumerable<IMenuItem> Items;
             
    }

}