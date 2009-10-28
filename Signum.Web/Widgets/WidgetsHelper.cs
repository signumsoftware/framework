using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web;
using Signum.Utilities;

namespace Signum.Web
{
    public delegate List<IWidget> GetWidgetDelegate(HtmlHelper helper, object entity, string partialViewName);

    public interface IWidget
    {
        event Action ForceShow;
    }

    public static class WidgetsHelper
    {
        public static event GetWidgetDelegate GetWidgetsForView;

        public static string GetWidgetsForViewName(this HtmlHelper helper, object entity, string partialViewName)
        {
            List<IWidget> widgets = new List<IWidget>();
            if (GetWidgetsForView != null)
                widgets.AddRange(GetWidgetsForView.GetInvocationList()
                    .Cast<GetWidgetDelegate>()
                    .Select(d => d(helper, entity, partialViewName))
                    .NotNull().SelectMany(d => d).ToList());

            return WidgetsToString(helper, widgets);
        }

        private static string WidgetsToString(HtmlHelper helper, List<IWidget> widgets)
        {
            if (widgets == null || widgets.Count == 0)
                return "";

            StringBuilder sb = new StringBuilder();

            foreach (IWidget widget in widgets)
            { 
                
            }

            return sb.ToString();
        }
    }
}
