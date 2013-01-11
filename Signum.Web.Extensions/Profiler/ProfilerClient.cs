using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using System.Reflection;
using Signum.Web.Operations;
using Signum.Entities;
using System.Web.Mvc;
using System.Diagnostics;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using Signum.Engine.Maps;
using System.Web.Routing;
using System.Web.Mvc.Html;
using System.Collections;
using System.Web.Script.Serialization;
using System.Drawing;
using Signum.Web.Omnibox;
using Signum.Entities.Profiler;
using Signum.Web.Extensions.Properties;

namespace Signum.Web.Profiler
{
    public static class ProfilerClient
    {
        public static string ViewPrefix = "~/Profiler/Views/{0}.cshtml";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(ProfilerClient));

                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("ProfilerHeavy",
                    () => ProfilerPermission.ViewHeavyProfiler.IsAuthorized(),
                    uh => uh.Action((ProfilerController pc) => pc.Heavy())));

                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("ProfilerTimeTable",
                    () => ProfilerPermission.ViewTimeTracker.IsAuthorized(),
                    uh => uh.Action((ProfilerController pc) => pc.TimeTable())));

                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("ProfilerTimes",
                    () => ProfilerPermission.ViewTimeTracker.IsAuthorized(),
                    uh => uh.Action((ProfilerController pc) => pc.Times())));

                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("OverrideSessionTimeout",
                    () => ProfilerPermission.OverrideSessionTimeout.IsAuthorized(),
                    uh => uh.Action((ProfilerController pc) => pc.OverrideSessionTimeout(60))));
            }
        }

        public static Dictionary<string, Color> RoleColors = new Dictionary<string, Color>
        {
            { "SQL", Color.Gold },
            { "DB", Color.MediumSlateBlue },
            { "LINQ", Color.Violet },
            { "MvcRequest", Color.LimeGreen },
            { "MvcResult", Color.SeaGreen }
        };

        public static string HeavyDetailsToJson(this IEnumerable<HeavyProfilerEntry> entries)
        {
            return new JavaScriptSerializer().Serialize(entries.Select(e => new 
            {
                e.BeforeStart,
                e.Start,
                e.End,
                Elapsed = e.Elapsed.NiceToString(),
                e.Role,
                Color = GetColor(e.Role),
                e.Depth,
                AditionalData = e.AditionalDataPreview(),
                FullIndex = e.FullIndex()
            }));
        }

        private static string GetColor(string role)
        {
            if (role == null)
                return Color.Gray.ToHtml();

            Color color;
            if (RoleColors.TryGetValue(role, out color))
                return color.ToHtml();

            return ColorExtensions.ToHtmlColor(StringHashEncoder.GetHashCode32(role));
        }

        public static MvcHtmlString ProfilerEntry(this HtmlHelper htmlHelper, string linkText,  string indices)
        {
            return htmlHelper.ActionLink(linkText, "HeavyRoute", new { indices }); 
        }
    }
}
