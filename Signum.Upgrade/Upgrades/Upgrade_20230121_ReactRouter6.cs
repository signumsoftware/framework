using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230121_ReactRouter6 : CodeUpgradeBase
{
    public override string Description => "Update Bootstrap";

    public override void Execute(UpgradeContext uctx)
    {
        var interfaceRegex = new Regex(@"interface +(?<InterfaceName>\w+) +extends +RouteComponentProps<[^>]+> *{\s*}");
        var functionRegex = new Regex(@"function *(?<ComponentName>\w+)\( *(?<p>\w+) *: *(?<Props>\w+(<[^]+]+>)?) *\) *{");
        var importRegex = new Regex(@"(?<extra>import *{.*)\bRouteComponentProps\b");

        uctx.ForeachCodeFile("*.tsx", file =>
        {
            var content = file.Content;

            if (content.Contains("RouteComponentProps"))
            {
                var separator = content.Contains("\r\n") ? "\r\n" : "\n";
                var obj = content.TryBetween("RouteComponentProps<", ">");

                string? interfaceName = null;
                content = interfaceRegex.Replace(content, match =>
                {
                    var name = match.Groups["InterfaceName"].Value;

                    interfaceName = interfaceName == null ? name :
                       throw new InvalidOperationException("Two RouteComponentProps declarations found!");

                    return "";
                });

                string? p = null;
                content = functionRegex.Replace(content, match =>
                {
                    var propType = match.Groups["Props"].Value;
                    if (propType == interfaceName || propType.StartsWith("RouteComponentProps"))
                    {
                        if (p != null)
                            throw new InvalidOperationException("Two property arguments found!");

                        p = match.Groups["p"].Value;
                        var componentName = match.Groups["ComponentName"].Value;
                        return "function " + componentName + "() {"
                        + (content.Contains(p + ".match.params") ? separator + "  const params = useParams() as " + obj + ";" : null)
                        + (content.Contains(p + ".location") ? separator + "  const location = useLocation();" : null);
                    }

                    return match.Value;
                });

                if (p != null)
                {
                    content = content.Replace(p + ".match.params", "params");
                    content = content.Replace(p + ".location", "location");
                }

                content = importRegex.Replace(content, m => m.Groups["extra"] + "useLocation, useParams");

                file.Content = content;
            }
        });

        var regexImportRoute = new Regex("""<ImportRoute +(exact +)?path *= *"~(?<path>[^"]+)" +(exact +)?onImportModule={ *\(\) *=> *(?<import>[^}]+)} *\/>""");
        var regexAsyncImport = new Regex("""import *{ *ImportRoute *} *from +['"][^"']*/AsyncImport['"] *;? *""");
        var historyPush = new Regex("""AppContext\.history\.push\((?<exp>::EXPR::)\)""").WithMacros();
        var historyReplace = new Regex("""AppContext\.history\.replace\((?<exp>::EXPR::)\)""").WithMacros();
        var windoOpenFindOptionsPath = new Regex("""window\.open\(Finder\.findOptionsPath\((?<exp>::EXPR::)\)\)""").WithMacros();
        var windoOpenNavigateRoute = new Regex("""window\.open\(Navigator\.navigateRoute\((?<exp>::EXPR::)\)\)""").WithMacros();

        uctx.ForeachCodeFile("*.tsx", file =>
        {
            file.Replace(regexImportRoute, m => $$"""{ path: "{{m.Groups["path"]}}", element: <ImportComponent onImport={() => {{m.Groups["import"]}}} /> }""");
            file.Replace(regexAsyncImport, """import { ImportComponent } from '@framework/ImportComponent'""");

            file.Replace("~/", "/");
            file.Replace(historyPush, m => "AppContext.navigate(" + m.Groups["exp"].Value + ")");
            file.Replace(historyReplace, m => "AppContext.navigate(" + m.Groups["exp"].Value + ", { replace : true })");
            file.Replace("AppContext.history.location", "AppContext.location");
            file.Replace("AppContext.history", "AppContext.router");

            if (file.Content.Contains("routes: JSX.Element[]"))
            {
                file.Replace("routes: JSX.Element[]", "routes: RouteObject[]");
                file.InsertAfterFirstLine(a => a.Contains("from 'react'") || a.Contains("from \"react\""), "import { RouteObject } from 'react-router'");
            }

            file.RemoveAllLines(a => a.Contains("from 'history'"));
            file.RemoveAllLines(a => a.Contains("from \"history\""));

            file.Replace(windoOpenFindOptionsPath, m => "window.open(toAbsoluteUrl(Finder.findOptionsPath(" + m.Groups["exp"].Value + ")))");
            file.Replace(windoOpenFindOptionsPath, m => "window.open(toAbsoluteUrl(Navigator.navigateRoute(" + m.Groups["exp"].Value + ")))");
        });

        uctx.ChangeCodeFile("Southwind.React/Views/Home/Index.cshtml", file =>
        {
            file.Replace(
                """var __baseUrl = "@Url.Content("~/")";""",
                """var __baseName = "@Url.Content("~")";""");
        });

        uctx.ChangeCodeFile("Southwind.React/App/Layout.tsx", file =>
        {
            file.Replace(
                """import { Link } from 'react-router-dom'""",
                """import { Link, Outlet } from 'react-router-dom'""");

            file.Replace(
                """{Layout.switch}""",
                """<Outlet />""");

            file.RemoveAllLines(a => a.Contains("Layout.switch ="));
        });

        uctx.ChangeCodeFile("Southwind.React/App/NotFound.tsx", file =>
        {
            file.Replace("export default class NotFound extends React.Component {", "export default function NotFound() {");

            file.Replace("componentWillMount() {", "React.useEffect(() => {");

            file.Replace(new Regex(@"</div>\s*\n\s*\);\s*\n\s*}\s*\n\s*}"), "</div>\n);\n}");
            file.Replace(new Regex(@"}\s*\n\s*}"), "}\n},[]);");

            file.RemoveAllLines(a => a.Contains("render() {"));

            SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Please format the code in NotFound.tsx after the changes");

            file.ReplaceLine(a => a.Contains("""AppContext.navigate("/auth/login", { back: AppContext.location }, { replace : true });"""),
                """AppContext.navigate("/auth/login", { state: { back: AppContext.location() }, replace: true });""");
        });

        var regexIsFull = new Regex(@"return isFull;\s*\n\s*}\);\s*\n\s*}\);");

        uctx.ChangeCodeFile("Southwind.React/App/MainPublic.tsx", file =>
        {
            file.RemoveAllLines(a => a.Contains("import { Switch } from \"react-router\""));
            file.ReplaceLine(a => a.Contains("from \"react-router-dom\""), "import { createBrowserRouter, RouterProvider, Location } from \"react-router-dom\"");
            file.ReplaceLine(a => a.Contains("""__webpack_public_path__ = window.__baseUrl + "dist/";"""), """__webpack_public_path__ = window.__baseName + "/dist/";""");
            file.ReplaceBetweenIncluded(a => a.Contains("function reload() {"),
                a => a.Contains(".then(() => {"), """
  async function reload() {

      await AuthClient.autoLogin();
      await reloadTypes();
      await CultureClient.loadCurrentCulture();
  """);

            file.ReplaceBetweenIncluded(a => a.Contains("const promise"),
               a => a.Contains("return promise.then"), """
  if (isFull)
     (await import("./MainAdmin")).startFull(routes);
  """);

            file.RemoveAllLines(a => a.Contains("""routes.push(<Route exact path="/" component={Home} />);"""));
            file.RemoveAllLines(a => a.Contains("routes.push(<Route component={NotFound} />);"));
            file.RemoveAllLines(a => a.Contains("Layout.switch = React.createElement(Switch, undefined, ...routes);"));
            file.ReplaceLine(a => a.Contains("const h = AppContext.createAppRelativeHistory();"), """       
        const mainRoute: RouteObject = {
          path: "/",
          element: <Layout />,
          children: [
            {
              index: true,
              element: <Home />
            },
            ...routes,
            {
              path: "*",
              element: <NotFound />
            },
          ]
        };

        const router = createBrowserRouter([mainRoute], { basename: window.__baseName });

        AppContext.setRouter(router);

        const messages = ConfigureReactWidgets.getMessages();
        """);

            file.ReplaceBetween(
                new(a => a.Contains("<Router history={h}>"), 0),
                new(a => a.Contains("</Router>"), 0),
                "<RouterProvider router={router}/>");

            file.Replace(regexIsFull, """
return true;
""");

            file.ReplaceBetweenIncluded(a => a.Contains("const loc = AppContext.location;"), a => a.Contains("const back"), """    
            const back: Location = AppContext.location().state?.back; 
            """);

            SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Please format the code in MainPublic.tsx after the changes");
        });

        uctx.ChangeCodeFile("Southwind.React/App/MainAdmin.tsx", file =>
        {
            file.InsertBeforeFirstLine(a => a.StartsWith("import"), """
                    import { RouteObject } from "react-router"
                    """);
        });

        uctx.ChangeCodeFile("Southwind.React/package.json", file =>
        {
            file.UpdateNpmPackages("""
                "react-router": "6.7.0",
                "react-router-dom": "6.7.0",
                "luxon": "3.2.1",
                """);

            file.RemoveNpmPackage("history");

            SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Probably you need to kill yarn.lock and execute yarn install again");
        });
    }
}



