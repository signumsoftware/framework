using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;
using System.Web;
using Signum.Web.Properties;
using Signum.Entities;
 
namespace Signum.Web
{
    public delegate ToolBarButton[] GetToolBarButtonEntityDelegate<T>(ControllerContext controllerContext, T entity, string partialViewName, string prefix);


    public static class ButtonBarEntityHelper
    {
        static Dictionary<Type, List<Delegate>> entityButtons = new Dictionary<Type, List<Delegate>>();
        static List<GetToolBarButtonEntityDelegate<ModifiableEntity>> globalButtons = new List<GetToolBarButtonEntityDelegate<ModifiableEntity>>();

        public static void RegisterEntityButtons<T>(GetToolBarButtonEntityDelegate<T> getToolBarButtons)
            where T : ModifiableEntity
        {
            entityButtons.GetOrCreate(typeof(T)).Add(getToolBarButtons);
        }

        public static void RegisterGlobalButtons(GetToolBarButtonEntityDelegate<ModifiableEntity> getToolBarButtons)
        {
            globalButtons.Add(getToolBarButtons);
        }

        public static List<ToolBarButton> GetForEntity(ControllerContext controllerContext, ModifiableEntity entity, string partialViewName, string prefix)
        {
            List<ToolBarButton> links = new List<ToolBarButton>();

            links.AddRange(globalButtons.SelectMany(a => a(controllerContext, entity, partialViewName, prefix) ?? Enumerable.Empty<ToolBarButton>()).NotNull());

            List<Delegate> list = entityButtons.TryGetC(entity.GetType());
            if (list != null)
                links.AddRange(list.SelectMany(a => ((ToolBarButton[])a.DynamicInvoke(controllerContext, entity, partialViewName, prefix)) ?? Enumerable.Empty<ToolBarButton>()).NotNull());

            foreach (var l in links)
            {
                if (l.DivCssClass == "not-set")
                    l.DivCssClass = ToolBarButton.DefaultEntityDivCssClass;
            }

            return links;
        }
    }


    public delegate ToolBarButton[] GetToolBarButtonQueryDelegate(ControllerContext controllerContext, object queryName, Type entityType, string prefix);

    public static class ButtonBarQueryHelper
    {
        public static event GetToolBarButtonQueryDelegate GetButtonBarForQueryName;

        public static List<ToolBarButton> GetButtonBarElementsForQuery(ControllerContext context, object queryName, Type entityType, string prefix)
        {
            List<ToolBarButton> elements = new List<ToolBarButton>();
            if (GetButtonBarForQueryName != null)
                elements.AddRange(GetButtonBarForQueryName.GetInvocationList()
                    .Cast<GetToolBarButtonQueryDelegate>()
                    .Select(d => d(context, queryName, entityType, prefix) ?? Enumerable.Empty<ToolBarButton>())
                    .NotNull().SelectMany(d => d).ToList());

            foreach (var el in elements)
            {
                if (el.DivCssClass == "not-set")
                    el.DivCssClass = ToolBarButton.DefaultQueryCssClass;
            }

            return elements;
        }

        public static MvcHtmlString ToString(this List<ToolBarButton> elements, HtmlHelper helper)
        {
            return MvcHtmlString.Create(elements.ToString(tb => "<li>" + tb.ToHtml(helper).ToHtmlString() + "</li>", "\r\n"));
        }
    }
}
