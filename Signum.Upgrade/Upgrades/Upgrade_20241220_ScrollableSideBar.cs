using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20241220_ScrollableSideBar : CodeUpgradeBase
{
    public override string Description => "Scrollable Sidebar and content";

    public override void Execute(UpgradeContext uctx)
    {
        var regex = new Regex("""\.GetAttribute\("(?<attr>[\w-]+)"\)""");
        uctx.ChangeCodeFile("Southwind/Layout.tsx", a =>
        {
            a.InsertBeforeFirstLine(a => a.Contains("<VersionChangedAlert"), "<div className=\"container-fluid overflow-auto pt-2\">");
            a.InsertAfterFirstLine(a => a.Contains("<Outlet"), "</div>");
            a.Replace("<Outlet", "<Outlet context={{ sidebarMode }}");
            a.Replace("appTitle={renderTitle()} ", "");
            a.InsertBeforeFirstLine(a => a.Contains("<ToolbarRenderer"), "{renderTitle()}");
            SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Check the changes in Layout.tsx, maybe you need to make fixes in renderTitle() function or site.css");
        });
    }
}



