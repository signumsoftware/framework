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
        var functionRegex = new Regex(@"export +default +function +(?<ComponentName>\w+)\((?<prop>\w+)\: +(?<Props>\w+(<[^]+]+>)?)\) *{");

        uctx.ChangeCodeFile("*.tsx", file =>
        {
            var content = file.Content;

            if (content.Contains("RouteComponentProps"))
            {
                var separator = content.Contains("\r\n") ? "\r\n" : "\n";
                var obj = content.Between("RouteComponentProps<", ">");

                string? interfaceName = null;
                content = interfaceRegex.Replace(content, match =>
                {
                    var name = match.Groups["InterfaceName"].Value;

                    interfaceName = interfaceName == null ? name :
                       throw new InvalidOperationException("Two RouteComponentProps declarations found!");

                    return "";
                });

                string? propName = null;
                content = functionRegex.Replace(content, match =>
                {
                    propName = match.Groups["Props"].Value;
                    var componentName = match.Groups["ComponentName"].Value;
                    if (propName == interfaceName || propName.StartsWith("RouteComponentProps"))
                    {
                        return "export default function " + componentName + "() {"
                        + (content.Contains(propName + ".match.params") ? separator + "  const params = useParams<{ queryName: string }>();" : null)
                        + (content.Contains(propName + ".location") ? separator + "  const location = useLocation();" : null);
                    }

                    return match.Value;
                });

                if(propName != null)
                {
                    content = content.Replace(propName + ".match.params", "params");
                    content = content.Replace(propName + ".location", "location");
                }

                content.Replace("RouteComponentProps", "useLocation, useParams"); //From import
            }
        });
    }
}



