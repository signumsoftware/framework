using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;
using System.Web;
using Signum.Entities;
 
namespace Signum.Web
{
    public class EntityButtonContext
    {
        public UrlHelper Url { get; set; }
        public ControllerContext ControllerContext { get; set; }
        public string PartialViewName { get; set; }
        public string Prefix{ get; set; }
        public ViewMode ViewMode { get; set; }
        public bool ShowOperations { get; set; }
    }
    
    public static class ButtonBarEntityHelper
    {
        static Dictionary<Type, List<Delegate>> entityButtons = new Dictionary<Type, List<Delegate>>();
        static List<Func<EntityButtonContext, ModifiableEntity, ToolBarButton[]>> globalButtons = new List<Func<EntityButtonContext, ModifiableEntity, ToolBarButton[]>>();

        public static void RegisterEntityButtons<T>(Func<EntityButtonContext, T, ToolBarButton[]> getToolBarButtons)
            where T : ModifiableEntity
        {
            entityButtons.GetOrCreate(typeof(T)).Add(getToolBarButtons);
        }

        public static void RegisterGlobalButtons(Func<EntityButtonContext, ModifiableEntity, ToolBarButton[]> getToolBarButtons)
        {
            globalButtons.Add(getToolBarButtons);
        }

        public static List<ToolBarButton> GetForEntity(EntityButtonContext ctx, ModifiableEntity entity)
        {
            List<ToolBarButton> links = new List<ToolBarButton>();

            links.AddRange(globalButtons.SelectMany(a => a(ctx, entity) ?? Enumerable.Empty<ToolBarButton>()).NotNull());

            List<Delegate> list = entityButtons.TryGetC(entity.GetType());
            if (list != null)
                links.AddRange(list.SelectMany(a => ((ToolBarButton[])a.DynamicInvoke(ctx, entity)) ?? Enumerable.Empty<ToolBarButton>()).NotNull());

            return links.OrderBy(a => a.Order).ToList();
        }
    }

    public class QueryButtonContext
    {
        public UrlHelper Url { get; internal set; }
        public object QueryName { get; internal set; }
        public ToolBarButton[] ManualQueryButtons { get; internal set; }
        public Type EntityType { get; internal set; }
        public string Prefix { get; internal set; }
        public ControllerContext ControllerContext { get; internal set; }
    }

    public static class ButtonBarQueryHelper
    {
        static List<Func<QueryButtonContext, ToolBarButton[]>> globalButtons = new List<Func<QueryButtonContext, ToolBarButton[]>>();

        static Dictionary<object, List<Func<QueryButtonContext, ToolBarButton[]>>> queryButtons = new Dictionary<object, List<Func<QueryButtonContext, ToolBarButton[]>>>();

        public static List<ToolBarButton> GetButtonBarElementsForQuery(QueryButtonContext ctx)
        {
            List<ToolBarButton> elements = new List<ToolBarButton>();

            if (ctx.ManualQueryButtons != null)
                elements.AddRange(ctx.ManualQueryButtons.NotNull());

            var querySpecific = queryButtons.TryGetC(ctx.QueryName);
            if (querySpecific != null)
                elements.AddRange(querySpecific.SelectMany(d => d(ctx) ?? Enumerable.Empty<ToolBarButton>()).NotNull());

            if (globalButtons != null)
                elements.AddRange(globalButtons.SelectMany(d => d(ctx) ?? Enumerable.Empty<ToolBarButton>()).NotNull().ToList());

            return elements;
        }

        public static void RegisterQueryButton(object queryName, Func<QueryButtonContext, ToolBarButton[]> buttonFactory)
        {
            queryButtons.GetOrCreate(queryName).Add(buttonFactory);
        }

        public static void RegisterGlobalButtons(Func<QueryButtonContext, ToolBarButton[]> buttonsFactory)
        {
            globalButtons.Add(buttonsFactory);
        }

        public static MvcHtmlString ToStringButton(this List<ToolBarButton> elements, HtmlHelper helper)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder(elements.Select(tb=> tb.ToHtml(helper)));

            sb.Add(new MvcHtmlString(@"<script>$(function(){ $('[data-toggle=""tooltip""]').tooltip({}); });</script>"));

            return sb.ToHtml();
        }
    }
}
