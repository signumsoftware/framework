using Signum.Utilities;
using System;
using System.Collections.Generic;

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

                if(p != null)
                {
                    content = content.Replace(p + ".match.params", "params");
                    content = content.Replace(p + ".location", "location");
                }

                content = importRegex.Replace(content, m => m.Groups["extra"] + "useLocation, useParams");

                file.Content = content;
            }
        });
    }
}



