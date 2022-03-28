using System.IO;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20211110_Bootstrap5 : CodeUpgradeBase
{
    public override string Description => "Upgrade to Bootstrap 5 and many other NPM packages";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind.React/package.json", file =>
        {
            if (!file.Content.Contains(@"""immutable"""))
                file.InsertAfterFirstLine(a => a.Contains("draft-js"), @"""immutable"": ""3.8.2"",");

            file.RemoveAllLines(a => a.Contains("jquery")); //Rome https://trends.google.com/trends/explore?date=all&geo=DE&q=jquery,react
                file.ReplaceLine(a => a.Contains("popper.js"), @"""@popperjs/core"": ""2.10.2"",");

            file.UpdateNpmPackages(@"
""@fortawesome/fontawesome-svg-core"": ""1.2.36"",
    ""@fortawesome/free-regular-svg-icons"": ""5.15.4"",
    ""@fortawesome/free-solid-svg-icons"": ""5.15.4"",
    ""@fortawesome/free-brands-svg-icons"": ""5.15.4"",
    ""@fortawesome/react-fontawesome"": ""0.1.16"",
    ""@types/draft-js"": ""0.11.6"",
    ""@types/luxon"": ""2.0.6"",
    ""@types/prop-types"": ""15.7.4"",
    ""abortcontroller-polyfill"": ""1.7.3"",
    ""bootstrap"": ""5.1.3"",
    ""codemirror"": ""5.63.3"",
    ""core-js"": ""3.19.1"",
    ""d3"": ""7.1.1"",
    ""d3-scale-chromatic"": ""3.0.0"",
    ""immutable"": ""3.8.2"",
    ""jquery"": ""3.5.1"",
    ""luxon"": ""2.1.0"",
    ""@azure/msal-browser"": ""2.19.0"",
    ""@popperjs/core"": ""2.10.2"",
    ""prop-types"": ""15.7.2"",
    ""react"": ""17.0.2"",
    ""react-bootstrap"": ""2.0.2"",
    ""react-dom"": ""17.0.2"",
    ""react-router"": ""5.2.1"",
    ""react-router-dom"": ""5.3.0"",
    ""react-widgets"": ""5.5.0"",
    ""whatwg-fetch"": ""3.6.2"",
    ""assets-webpack-plugin"": ""7.1.1"",
    ""css-loader"": ""6.5.1"",
    ""file-loader"": ""6.2.0"",
    ""sass"": ""1.43.4"",
    ""raw-loader"": ""4.0.2"",
    ""rimraf"": ""3.0.2"",
    ""sass-loader"": ""12.3.0"",
    ""style-loader"": ""3.3.1"",
    ""ts-loader"": ""9.2.6"",
    ""typescript"": ""4.4.4"",
    ""url-loader"": ""4.1.1"",
    ""webpack"": ""5.62.1"",
    ""webpack-bundle-analyzer"": ""4.5.0"",
    ""webpack-cli"": ""4.9.1"",
    ""webpack-notifier"": ""1.14.1""");
        });

        var allDirectories = new[] { uctx.RootFolder, Path.Combine(uctx.RootFolder, "Framework") };

        uctx.ForeachCodeFile(@"*.ts, *.tsx", allDirectories, file =>
        {
            file.Replace(new Regex(@"\bml-([1234]|auto)\b"), "ms-$1");
            file.Replace(new Regex(@"\bmr-([1234]|auto)\b"), "me-$1");
            file.Replace(new Regex(@"\bpl-([1234]|auto)\b"), "ps-$1");
            file.Replace(new Regex(@"\bpr-([1234]|auto)\b"), "pe-$1");
            file.Replace(new Regex(@"\bpr-([1234]|auto)\b"), "pe-$1");
            file.Replace(new Regex(@"\bfloat-left\b"), "float-start");
            file.Replace(new Regex(@"\bfloat-right\b"), "float-end");
            file.Replace(new Regex(@"\btext-left\b"), "text-start");
            file.Replace(new Regex(@"\btext-right\b"), "text-end");
            file.Replace(new Regex(@"\btext-right\b"), "text-end");

        });
    }
}
