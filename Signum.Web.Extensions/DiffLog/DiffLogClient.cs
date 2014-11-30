using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Utilities;
using Signum.Web.Omnibox;
using Signum.Engine.DiffLog;
using Signum.Entities.Basics;
using System.IO;
using System.Web.Mvc;
using System.Web.WebPages;

namespace Signum.Web.DiffLog
{
    public class DiffLogClient
    {
        public static string ViewPrefix = "~/DiffLog/Views/{0}.cshtml";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(DiffLogClient));

                Navigator.AddSetting(new EntitySettings<OperationLogEntity> { PartialViewName = e => ViewPrefix.FormatWith("OperationLog") });

                LinksClient.RegisterEntityLinks<Entity>((ident, ctx) => new[] { new QuickLinkExplore(typeof(OperationLogEntity), "Target", ident) });
            }
        }
    }

    public class LinkTab : Tab
    {
        public string Url;
        public LinkTab(Func<object, HelperResult> title, string url)
            : base(null, title, MvcHtmlString.Empty)
        {
            this.Url = url;
        }

        public override void WriteHeader(TextWriter writer, Tab first, TypeContext context)
        {
            using (TabContainer.Surround(writer, new HtmlTag("li").Class("linkTab")))
            using (TabContainer.Surround(writer, new HtmlTag("a").Attr("href", Url).Attr("title", this.ToolTip)))
                this.Title.WriteTo(writer);
        }
    }
}