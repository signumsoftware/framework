using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades
{
    class Upgrade_20210331_ReactWidgets5 : CodeUpgradeBase
    {
        public override string Description => "Upgrade to React Widgets 5 (final) and other minor upgrades";

        public override void Execute(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile(@"Southwind.React/App/MainPublic.tsx", file =>
            {
                file.Replace(
                    "../node_modules/react-widgets/lib/scss/react-widgets.scss",
                    "../node_modules/react-widgets/scss/styles.scss");
            });

            uctx.ForeachCodeFile("*.tsx", "Southwind.React", file =>
            {
                file.Replace(
                    "react-widgets/lib",
                    "react-widgets/cjs");
            });

            uctx.ChangeCodeFile(@"Southwind.React/package.json", file =>
            {
                file.UpdateNpmPackage("@fortawesome/fontawesome-svg-core", "1.2.35");
                file.UpdateNpmPackage("@fortawesome/free-regular-svg-icons", "5.15.3");
                file.UpdateNpmPackage("@fortawesome/free-solid-svg-icons", "5.15.3");
                file.UpdateNpmPackage("@fortawesome/free-brands-svg-icons", "5.15.3");
                file.UpdateNpmPackage("@fortawesome/react-fontawesome", "0.1.14");
                file.UpdateNpmPackage("@types/draft-js", "0.11.1");
                file.UpdateNpmPackage("@types/luxon", "1.26.2");

                file.UpdateNpmPackage("abortcontroller-polyfill", "1.7.1");
                file.UpdateNpmPackage("bootstrap", "4.6.0");
                file.UpdateNpmPackage("codemirror", "5.60.0");
                file.UpdateNpmPackage("core-js", "3.9.1");
                file.UpdateNpmPackage("d3", "6.6.2");

                file.UpdateNpmPackage("luxon", "1.26.0");
                file.UpdateNpmPackage("msal", "1.4.8");

                file.UpdateNpmPackage("react", "17.0.2");
                file.UpdateNpmPackage("react-bootstrap", "1.5.2");
                file.UpdateNpmPackage("react-dom", "17.0.2");

                file.UpdateNpmPackage("react-widgets", "5.0.0");
                file.UpdateNpmPackage("whatwg-fetch", "3.6.2");


                file.UpdateNpmPackage("css-loader", "5.2.0");

                file.ReplaceLine(a => a.Contains("node-sass"), @"""sass"": ""1.32.8"",");

                file.UpdateNpmPackage("sass-loader", "11.0.1");
                file.UpdateNpmPackage("ts-loader", "8.1.0");
                file.UpdateNpmPackage("typescript", "4.2.3");
                file.UpdateNpmPackage("webpack", "5.28.0");
                file.UpdateNpmPackage("webpack-cli", "4.6.0");
                file.UpdateNpmPackage("webpack-notifier", "1.13.0");
            });
        }
    }
}
