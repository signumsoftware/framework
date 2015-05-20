using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using System.Reflection;
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
using Signum.Entities.Disconnected;
using Signum.Web.Maps;
using Signum.Engine.Disconnected;

namespace Signum.Web.Disconnected
{
    public static class DisconnectedClient
    {
        public static string ViewPrefix = "~/Disconnected/Views/{0}.cshtml";
        public static JsModule ColorModule = new JsModule("Extensions/Signum.Web.Extensions/Disconnected/Scripts/DisconnectedColors");

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(DisconnectedClient));

                Navigator.AddSettings(new List<EntitySettings>()
                {
                    new EntitySettings<DisconnectedMachineEntity> { PartialViewName = e => ViewPrefix.FormatWith("DisconnectedMachine") },
                    new EntitySettings<DisconnectedExportEntity> { PartialViewName = e => ViewPrefix.FormatWith("DisconnectedExport") },
                    new EntitySettings<DisconnectedImportEntity> { PartialViewName = e => ViewPrefix.FormatWith("DisconnectedImport") },
                });

                MapClient.GetColorProviders += GetMapColors;
            }
        }

        static MapColorProvider[] GetMapColors()
        {
            var strategies = DisconnectedLogic.GetStrategyPairs().SelectDictionary(t => Navigator.ResolveWebTypeName(t), p => p);

            return new[]
            {
                new MapColorProvider
                { 
                    Name = "disconnected", 
                    NiceName = "Disconnected", 
                    GetJsProvider =  ColorModule["disconnectedColors"](MapClient.NodesConstant),
                    AddExtra = t => 
                    {
                        var s = strategies.TryGetC(t.webTypeName);

                        if (s == null)
                            return;
                        
                        t.extra["disc-upload"] = s.Upload.ToString();
                        foreach (var mt in t.mlistTables)
                            mt.extra["disc-upload"] = s.Upload.ToString();

                        t.extra["disc-download"] = s.Download.ToString();
                        foreach (var mt in t.mlistTables)
                            mt.extra["disc-download"] = s.Download.ToString();
                        
                    },
                    Defs =  new HtmlStringBuilder(
                        from u in EnumExtensions.GetValues<Upload>()
                        from d in EnumExtensions.GetValues<Download>()
                        select GradientDef(u, d)).ToHtml(),
                    Order = 4,
                },
            };
        }

        static string GradientName(Upload upload, Download download)
        {
            return "disconnected-" + upload + "-" + download;
        }

        static MvcHtmlString GradientDef(Upload upload, Download download)
        {
            return MvcHtmlString.Create(@"
<linearGradient id=""" + GradientName(upload, download) + @""" x1=""0%"" y1=""0%"" x2=""0%"" y2=""100%"">
    <stop offset=""0%"" style=""stop-color:"  + UploadColor(upload)  +  @""" />
    <stop offset=""100%"" style=""stop-color:" + DownloadColor(download) + @""" />
</linearGradient>");
        }

        private static string DownloadColor(Download download)
        {
            switch (download)
            {
                case Download.None: return "#ccc";
                case Download.All: return "red";
                case Download.Subset: return "gold";
                case Download.Replace: return "#CC0099";
                default: throw new InvalidOperationException();
            }
        }

        private static string UploadColor(Upload upload)
        {
            switch (upload)
            {
                case Upload.None: return "#ccc";
                case Upload.New: return "green";
                case Upload.Subset: return "gold";
                default: throw new InvalidOperationException();
            }
        }
    }
}
