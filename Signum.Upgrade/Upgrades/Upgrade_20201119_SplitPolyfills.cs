using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades
{
    class Upgrade_20201119_SplitPolyfills : CodeUpgradeBase
    {
        public override string Description => "Split polyfills and optional IE support";


        public override void Execute(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile("Southwind.React/Startup.cs", file =>
            {
                file.WarningLevel = WarningLevel.Warning;
                file.RemoveAllLines(a => a.Contains("builder.UseETagger();"));
            });

            uctx.ChangeCodeFile("Southwind.React/App/MainPublic.tsx", file =>
            {
                file.WarningLevel = WarningLevel.None;
                file.RemoveAllLines(a => a.Contains("WebAuthnClient.start({"));
            });

            uctx.ChangeCodeFile("Southwind.React/package.json", file =>
            {
                file.Replace(
                    searchFor: @"&& webpack --config webpack.config.dll.js",
                    replaceBy: @"&& webpack --config webpack.config.polyfills.js && webpack --config webpack.config.dll.js");
            });

            uctx.ChangeCodeFile("Southwind.React/webpack.config.js", file =>
            {
                file.Replace(
                    searchFor: @"""./App/polyfills.js"", ",
                    replaceBy:"");
            });

            uctx.CreateCodeFile("Southwind.React/webpack.config.polyfills.js", @"var path = require(""path"");
var webpack = require(""webpack"");
var AssetsPlugin = require('assets-webpack-plugin');

module.exports = {
  mode: ""development"",  //Now mandatory, alternatively ""production""
  devtool: false, //To remove source maps in ""development"", avoids problems with errors in Chrome
  entry: {
    polyfills: [path.join(__dirname, ""App"", ""polyfills.js"")]
  },
  output: {
    path: path.join(__dirname, ""wwwroot"", ""dist""),
    filename: ""[name].[hash].js"",
    library: ""[name]_[hash]""
  },
  plugins: [
    new AssetsPlugin({
      path: path.join(__dirname, ""wwwroot"", ""dist""),
      filename: ""webpack-assets.polyfills.json""
    }),
  ],
  resolve: {
    modules: [
      ""node_modules""
    ]
  }
};");

            uctx.ChangeCodeFile("Southwind.React/Views/Home/Index.cshtml", file =>
            {
                file.InsertAfterFirstLine(
                    a => a.Contains("var vendor = (string)JObject.Parse(jsonDll"),
@"
string jsonPolyfills = File.ReadAllText(System.IO.Path.Combine(hostingEnv.WebRootPath, ""dist/webpack-assets.polyfills.json""));
var polyfills = (string)JObject.Parse(jsonPolyfills).Property(""polyfills"")!.Value[""js""]!;");

                file.InsertAfterFirstLine(a => a.Contains(@" var __baseUrl = ""@Url.Content(""~/"")"";"),
@"var browser = (function (agent) {
    switch (true) {
        case agent.indexOf(""edge"") > -1: return ""old edge"";
        case agent.indexOf(""edg"") > -1: return ""chromium edge"";
        case agent.indexOf(""opr"") > -1 && !!window.opr: return ""opera"";
        case agent.indexOf(""chrome"") > -1 && !!window.chrome: return ""chrome"";
        case agent.indexOf(""trident"") > -1: return ""ie"";
        case agent.indexOf(""firefox"") > -1: return ""firefox"";
        case agent.indexOf(""safari"") > -1: return ""safari"";
        default: return ""other"";
    }
})(window.navigator.userAgent.toLowerCase());

var supportIE = true;
if (!supportIE && (browser == ""old edge"" || browser == ""ie"")) {
    var spinner = document.querySelector(""#reactDiv .loading-spinner"");
    spinner.parentElement.removeChild(spinner);
    document.querySelector(""#reactDiv h3"").innerHTML =
        ""Southwind is not compatible with your browser ("" + browser + ""). <br/>""
        + ""Please try with <a href='https://www.google.com/chrome/'>Google Chrome</a>, ""
        + ""<a href='https://www.mozilla.org/en-US/firefox/new/'>Firefox</a> or ""
        + ""the new <a href='https://www.microsoft.com/en-us/edge'>Microsoft Edge (chromium)</a>."";
} else {
    const scriptToLoad = [
        ""@Url.Content(""~/dist/"" + polyfills)"",
        ""@Url.Content(""~/dist/"" + vendor)"",
        ""@Url.Content(""~/dist/"" + main)"",
    ];

    if (!(browser == ""old edge"" || browser == ""ie"")) {
        scriptToLoad.splice(0, 1);
    }

    function loadNextScript() {
        // gets the first script in the list
        var script = scriptToLoad.shift();
        // all scripts were loaded
        if (!script) return;
        var js = document.createElement('script');
        js.type = 'text/javascript';
        js.src = script;
        js.onload = function () { loadNextScript(); };
        var s = document.getElementsByTagName('script')[0];
        s.parentNode.insertBefore(js, s);
    }

    loadNextScript();
}");

                file.RemoveAllLines(a => a.Contains(@" src=""@Url.Content(""~/dist/"""));
            });
        }
    }
}
