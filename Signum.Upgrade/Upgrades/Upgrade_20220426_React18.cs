namespace Signum.Upgrade.Upgrades;

class Upgrade_20220426_React18 : CodeUpgradeBase
{
    public override string Description => "Update to React 18 and other NPM upgrades";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile("Southwind.React/App/MainPublic.tsx", file =>
        {
            file.ReplaceLine(a => a.Contains(@"import { render, unmountComponentAtNode } from ""react-dom"""), @"import { createRoot, Root } from ""react-dom/client""");
            file.InsertBeforeFirstLine(a=>a.Contains("function reload() {"), "let root: Root | undefined = undefined;");
            file.ReplaceLine(a=> a.Contains(@"unmountComponentAtNode(reactDiv);"), @"if (root)
  root.unmount();

root = createRoot(reactDiv);");
            file.ReplaceLine(a => a.Contains("render("), "root.render(");
            file.ReplaceLine(a => a.Contains("</Localization>, reactDiv);"), "</Localization>);");
        });

        uctx.ChangeCodeFile("Southwind.React/package.json", file =>
        {
            file.UpdateNpmPackages(
              @" ""@azure/msal-browser"": ""2.23.0"",
    ""@fortawesome/fontawesome-svg-core"": ""6.1.1"",
    ""@fortawesome/free-brands-svg-icons"": ""6.1.1"",
    ""@fortawesome/free-regular-svg-icons"": ""6.1.1"",
    ""@fortawesome/free-solid-svg-icons"": ""6.1.1"",
    ""@fortawesome/react-fontawesome"": ""0.1.18"",
    ""@microsoft/microsoft-graph-client"": ""3.0.2"",
    ""@microsoft/signalr"": ""6.0.4"",
    ""@popperjs/core"": ""2.11.5"",
    ""@types/draft-js"": ""0.11.9"",
    ""@types/luxon"": ""2.3.1"",
    ""@types/prop-types"": ""15.7.5"",
    ""@types/react"": ""file:../Framework/Signum.React/node_modules/@types/react"",
    ""abortcontroller-polyfill"": ""1.7.3"",
    ""bootstrap"": ""5.1.3"",
    ""codemirror"": ""5.65.3"",
    ""core-js"": ""3.22.2"",
    ""d3"": ""7.4.4"",
    ""d3-scale-chromatic"": ""3.0.0"",
    ""draft-js"": ""0.11.7"",
    ""draftjs-to-html"": ""0.9.1"",
    ""history"": ""4.10.1"",
    ""html-to-draftjs"": ""1.5.0"",
    ""immutable"": ""3.8.2"",
    ""luxon"": ""2.3.2"",
    ""prop-types"": ""15.8.1"",
    ""react"": ""18.0.0"",
    ""react-bootstrap"": ""2.3.0"",
    ""react-dom"": ""18.0.0"",
    ""react-router"": ""5.3.1"",
    ""react-router-dom"": ""5.3.1"",
    ""react-widgets"": ""5.8.4"",
    ""whatwg-fetch"": ""3.6.2""");

            file.UpdateNpmPackages(
                @" ""assets-webpack-plugin"": ""7.1.1"",
    ""css-loader"": ""6.7.1"",
    ""file-loader"": ""6.2.0"",
    ""raw-loader"": ""4.0.2"",
    ""rimraf"": ""3.0.2"",
    ""sass"": ""1.50.1"",
    ""sass-loader"": ""12.6.0"",
    ""style-loader"": ""3.3.1"",
    ""ts-loader"": ""9.2.8"",
    ""typescript"": ""4.6.3"",
    ""url-loader"": ""4.1.1"",
    ""webpack"": ""5.72.0"",
    ""webpack-bundle-analyzer"": ""4.5.0"",
    ""webpack-cli"": ""4.9.2"",
    ""webpack-notifier"": ""1.15.0"""
                );
        });
    }
}
