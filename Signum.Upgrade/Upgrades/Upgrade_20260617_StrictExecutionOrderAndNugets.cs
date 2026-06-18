using Signum.Utilities;
using System;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20260617_StrictExecutionOrderAndNugets : CodeUpgradeBase
{
    public override string Description => "Set Vite strictExecutionOrder (fixes 'Prism is not defined' with lexical/@lexical/code), update Nugets and register HtmlEditor/Markdown clients";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReferences("""
                <PackageReference Include="Swashbuckle.AspNetCore" Version="10.2.1" />
                <PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.9" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.9" />
                <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.9" />
                <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="10.0.9" />
                <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.9" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.6.0" />
                <PackageReference Include="System.DirectoryServices" Version="10.0.9" />
                <PackageReference Include="System.DirectoryServices.AccountManagement" Version="10.0.9" />
                <PackageReference Include="System.Drawing.Common" Version="10.0.9" />
                <PackageReference Include="Microsoft.Graph" Version="6.2.0" />
                <PackageReference Include="Markdig" Version="1.3.1" />
                <PackageReference Include="Selenium.Support" Version="4.45.0" />
                <PackageReference Include="Selenium.WebDriver" Version="4.45.0" />
                """);
        });

        uctx.ChangeCodeFile(@"Southwind.Server/vite.config.js", file =>
        {
            file.InsertBeforeFirstLine(a => a.Contains("codeSplitting: {"),
                """
                // Rolldown (Vite 8) may reorder side-effect imports. @lexical/code needs prismjs core
                // (which sets the global Prism) to run before its prismjs/components/prism-* imports,
                // otherwise the build throws "ReferenceError: Prism is not defined".
                strictExecutionOrder: true,
                """);
        });

        uctx.ChangeCodeFile(@"Southwind/MainAdmin.tsx", file =>
        {
            if (!file.Content.Contains("Signum.HtmlEditor/HtmlEditorClient"))
                file.InsertAfterFirstLine(a => a.Contains("Signum.Eval/EvalClient"),
                    """
                    import { HtmlEditorClient } from "@extensions/Signum.HtmlEditor/HtmlEditorClient"
                    import { MarkdownClient } from "@extensions/Signum.Markdown/MarkdownClient"
                    """);

            if (!file.Content.Contains("HtmlEditorClient.start("))
                file.InsertAfterFirstLine(a => a.Contains("EvalClient.start("),
                    """
                    HtmlEditorClient.start();
                    MarkdownClient.start();
                    """);

            // Move ToolbarClient.start to just before DashboardClient.start (Dashboard registers toolbar parts)
            file.ProcessLines(lines =>
            {
                var toolbarIdx = lines.FindIndex(a => a.Contains("ToolbarClient.start("));
                var dashIdx = lines.FindIndex(a => a.Contains("DashboardClient.start("));
                if (toolbarIdx == -1 || dashIdx == -1 || toolbarIdx == dashIdx - 1)
                    return false;

                var toolbarLine = lines[toolbarIdx];
                lines.RemoveAt(toolbarIdx);
                dashIdx = lines.FindIndex(a => a.Contains("DashboardClient.start("));
                lines.Insert(dashIdx, toolbarLine);
                return true;
            });
        });

        SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Remember to:");
        SafeConsole.WriteLineColor(ConsoleColor.DarkMagenta, "Run yarn install after this upgrade (lexical and @lexical/* updated to 0.45.0)");
    }
}
