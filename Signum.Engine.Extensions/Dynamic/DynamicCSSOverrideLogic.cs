using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Signum.Engine.Dynamic
{

    public static class DynamicCSSOverrideLogic
    {
        public static ResetLazy<List<DynamicCSSOverrideEntity>> Cached = null!;

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodBase.GetCurrentMethod()))
            {
                sb.Include<DynamicCSSOverrideEntity>()
                   .WithSave(DynamicCSSOverrideOperation.Save)
                   .WithDelete(DynamicCSSOverrideOperation.Delete)
                   .WithQuery(() => e => new
                   {
                       Entity = e,
                       e.Id,
                       e.Name,
                       Script = e.Script.Etc(100),
                   });

                Cached = sb.GlobalLazy(() =>
                 Database.Query<DynamicCSSOverrideEntity>().Where(a => !a.Mixin<DisabledMixin>().IsDisabled).ToList(),
                 new InvalidateWith(typeof(DynamicCSSOverrideEntity)));
            }
        }
    }
}

// In order to work this module, you should apply below mentioned changes to your index.cshtml file
/*
@using Signum.Utilities;
@using Signum.Engine.Dynamic; <====*
@using Newtonsoft.Json.Linq;

@{
    string json = File.ReadAllText(Path.Combine(Server.MapPath("~/dist/"), "webpack-assets.json"));
    var main = (string)JObject.Parse(json).Property("main").Value["js"];

    string jsonDll = File.ReadAllText(Path.Combine(Server.MapPath("~/dist/"), "webpack-assets.dll.json"));
    var vendor = (string)JObject.Parse(jsonDll).Property("vendor").Value["js"];

    var cssOverride = String.Join("\r\n", DynamicCSSOverrideLogic.Cached.Value.Select(a => a.Script)); <====*
}
<!doctype html>
<html>
<head>
    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>@ViewBag.Title</title>
</head>
<body>
    <style type="text/css">@cssOverride</style> <====*
    <div id="reactDiv"></div>
    <script>
        var __baseUrl = "@Url.Content("~/")";
    </script>
    <script language="javascript" src="@Url.Content("~/dist/es6-promise.auto.min.js")"></script>
    <script language="javascript" src="@Url.Content("~/dist/fetch.js")"></script>
    <script language="javascript" src="@Url.Content("~/dist/" + vendor)"></script>
    <script language="javascript" src="@Url.Content("~/dist/" + main)"></script>
</body>
</html>
 
*/
