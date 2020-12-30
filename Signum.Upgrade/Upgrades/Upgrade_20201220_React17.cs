using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades
{
    class Upgrade_20201220_React17 : CodeUpgradeBase
    {
        public override string Description => "upgrade NPM modules, react 16, react-widgets";


        public override void Execute(UpgradeContext uctx)
        {
            uctx.ForeachCodeFile("*.csproj", file =>
            {  
                file.UpdateNugetReference(@"Microsoft.NET.Test.Sdk", @"16.8.3");
            });

            uctx.ChangeCodeFile("Southwind.React/App/MainPublic.tsx", file =>
            {
                file.InsertBeforeFirstLine(a => a.Contains(@"""./site.css"""),
                    @"import ""../node_modules/react-widgets/lib/scss/react-widgets.scss"""
                    );

                file.InsertAfterFirstLine(a => a.Contains(@"import * as React from ""react"""),
                    @"import { Localization } from ""react-widgets"""
                    );

                file.ReplaceLine(a => a.Contains(@"ConfigureReactWidgets.configure();"),
@"const dateLocalizer = ConfigureReactWidgets.getDateLocalizer();
const numberLocalizer = ConfigureReactWidgets.getNumberLocalizer();"
         );

                file.Replace("var ", "const ");

                file.InsertBeforeFirstLine(a => a.Contains(@"return promise.then(() => {"),
                 @"const messages = ConfigureReactWidgets.getMessages();
"
                  );

                file.ReplaceLine(
                    a => a.Contains(@"<Router history={h}>"),
                    @"<Localization date={dateLocalizer} number={numberLocalizer} messages={messages} >
            <Router history={h}>");


                file.ReplaceLine(
                a => a.Contains(@" </Router>, reactDiv);"),
                @"</Router>
          </Localization>, reactDiv);");

            });

            uctx.ChangeCodeFile("Southwind.React/package.json", file =>
            {
                file.RemoveAllLines(a => a.Contains("min-dash"));

                file.UpdateNpmPackage("@fortawesome/fontawesome-svg-core", "1.2.32");
                file.UpdateNpmPackage("@fortawesome/fontawesome-svg-core", "1.2.32");
                file.UpdateNpmPackage("@fortawesome/free-brands-svg-icons", "5.15.1");
                file.UpdateNpmPackage("@fortawesome/free-regular-svg-icons", "5.15.1");
                file.UpdateNpmPackage("@fortawesome/free-solid-svg-icons", "5.15.1");
                file.UpdateNpmPackage("@fortawesome/react-fontawesome", "0.1.13");
                file.UpdateNpmPackage("@types/luxon", "1.25.0");
                file.UpdateNpmPackage("abortcontroller-polyfill", "1.5.0");
                file.UpdateNpmPackage("bootstrap", "4.5.3");
                file.UpdateNpmPackage("d3", "6.3.1");
                file.UpdateNpmPackage("d3-scale-chromatic", "2.0.0");
                file.UpdateNpmPackage("core-js", "3.8.1");
                file.UpdateNpmPackage("react", "17.0.1");
                file.UpdateNpmPackage("react-dom", "17.0.1");
                file.UpdateNpmPackage("react-widgets", "5.0.0-beta.22");
                file.UpdateNpmPackage("whatwg-fetch", "3.5.0");

                file.UpdateNpmPackage("assets-webpack-plugin", "7.0.0");
                file.UpdateNpmPackage("css-loader", "5.0.1");
                file.UpdateNpmPackage("file-loader", "6.2.0");
                file.UpdateNpmPackage("raw-loader", "4.0.2");
                file.UpdateNpmPackage("style-loader", "2.0.0");
                file.UpdateNpmPackage("ts-loader", "8.0.12");
                file.UpdateNpmPackage("typescript", "4.1.3");
                file.UpdateNpmPackage("url-loader", "4.1.1");
                file.UpdateNpmPackage("webpack", "5.11.0");
                file.UpdateNpmPackage("webpack-cli", "4.2.0");

                file.WarningLevel = WarningLevel.None;

                file.RemoveAllLines(a => a.Contains("dom-helpers"));

                file.UpdateNpmPackage("@types/draft-js", "0.10.44");
                file.UpdateNpmPackage("bpmn-js", "7.5.0");
                file.UpdateNpmPackage("codemirror", "5.58.3");
                file.UpdateNpmPackage("diagram-js-minimap", "2.0.4");
                file.UpdateNpmPackage("draft-js", "0.11.7");
            });

            uctx.ChangeCodeFile(@"Southwind.React\webpack.config.dll.js", file =>
            {
                file.Replace("[hash]", "[fullhash]");
                file.InsertAfterFirstLine(a => a.Contains(@"new AssetsPlugin({"), "  fullPath: false,");
            });

            uctx.ChangeCodeFile(@"Southwind.React\webpack.config.js", file =>
            {
                file.InsertAfterFirstLine(a => a.Contains(@"new AssetsPlugin({"), "  fullPath: false,");

                file.WarningLevel = WarningLevel.None;
                file.Replace("[hash]", "[chunkhash]");
            });

            uctx.ChangeCodeFile(@"Southwind.React\webpack.config.polyfills.js", file =>
            {
                file.Replace("[hash]", "[fullhash]");
                file.InsertAfterFirstLine(a => a.Contains(@"new AssetsPlugin({"), "  fullPath: false,");
            });
        }
    }
}
