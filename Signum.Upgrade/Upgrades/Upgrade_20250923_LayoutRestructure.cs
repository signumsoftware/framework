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
            file.Replace("<div id=\"main\" key={refreshId}>", "<div id=\"site-content\" key={refreshId}>");
            file.Replace("<div id=\"main\">", "<div id=\"site-content\">");

            file.Replace("<div className=\"container-fluid overflow-auto pt-2\">\n              <VersionChangedAlert />\n              <Outlet context={{ sidebarMode }} />\n            </div>", "<main id=\"maincontent\" className=\"container-fluid overflow-auto pt-2\" tabIndex={-1}>\n              <VersionChangedAlert />\n              <Outlet context={{ sidebarMode }} />\n            </main>");
        });

        uctx.ChangeCodeFile(@"Southwind/SCSS/site.css", file =>
        {
            file.Replace("#main {", "#site-content {");
            file.InsertAfterLastLine(a => a.Contains('}'), ".skip-link {\n    position: absolute;\n    top: -40px;\n    left: 0;\n}");
        });
    }
}


