using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250923_LayoutRestructure : CodeUpgradeBase
{
    public override string Description => "Restructure Index and Layout for WCAG";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind/Index.cshtml", file =>
        {
            file.Replace("filter: hue-rotate(3eg);", "filter: hue-rotate(3deg);");
            file.Replace("filter: hue-rotate(6eg);", "filter: hue-rotate(6deg);");
        });

        uctx.ChangeCodeFile(@"Southwind/Layout.tsx", file =>
        {
            file.Replace("<div id=\"main\"", "<div id=\"site-content\"");

            file.ProcessLines(lines =>
            {
                var index = lines.FindIndex(a => a.Contains("container-fluid"));

                lines[index] = lines[index].Replace("<div className=\"container-fluid", "<main tabIndex={-1} id=\"maincontent\" className=\"container-fluid");

                var divIndex = lines.FindIndex(index, a => a.Contains("</div>"));

                lines[divIndex] = lines[divIndex].Replace("</div>", "</main>");

                return true;
            });
        });

        uctx.ChangeCodeFile(@"Southwind/site.css", file =>
        {
            file.Replace("#main {", "#site-content {");
            file.InsertAfterLastLine(a => a.Contains('}'), """
                .skip-link {
                    position: absolute;
                    top: -40px;
                    left: 0;
                }
                """);
        });
    }
}


