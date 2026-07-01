using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20260602_Vite8AndOptions : CodeUpgradeBase
{
    public override string Description => "Update to Vite 8 and replace direct properties with .Options pattern";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind.Server/package.json", file =>
        {
            file.UpdateNpmPackage("sass", "1.100.0");
            file.UpdateNpmPackage("vite", "8.0.15");
            file.UpdateNpmPackage("@vitejs/plugin-react", "6.0.2");
        });

        uctx.ChangeCodeFile(@"Southwind.Server/vite.config.js", file =>
        {
            file.ProcessLines(lines =>
            {
                var fromIdx = lines.FindIndex(a => a.Contains("manualChunks"));
                if (fromIdx == -1)
                    return false;

                // Find the opening { of the manualChunks object
                var objStartIdx = lines.FindIndex(fromIdx, a => a.Contains("{"));
                if (objStartIdx == -1)
                    return false;

                // Find matching closing } by tracking brace depth
                int depth = 0;
                int objEndIdx = -1;
                for (int i = objStartIdx; i < lines.Count; i++)
                {
                    foreach (char c in lines[i])
                    {
                        if (c == '{') depth++;
                        else if (c == '}') { depth--; if (depth == 0) { objEndIdx = i; break; } }
                    }
                    if (objEndIdx != -1) break;
                }
                if (objEndIdx == -1)
                    return false;

                // Extract lines inside the object (between { and })
                var innerLines = lines.GetRange(objStartIdx + 1, objEndIdx - objStartIdx - 1);

                // Normalize JS object to valid JSON
                var jsonContent = string.Join("\n", innerLines
                    .Select(l => l.Trim())
                    .Where(l => !l.StartsWith("//") && l.Length > 0));
                jsonContent = jsonContent.Replace("'", "\"");
                jsonContent = Regex.Replace(jsonContent, @"(\w+)\s*:", "\"$1\":");
                jsonContent = Regex.Replace(jsonContent, @",(\s*[\]\}])", "$1");
                jsonContent = "{" + jsonContent + "}";

                var docOptions = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip };
                using var doc = JsonDocument.Parse(jsonContent, docOptions);

                var indent = CodeFile.GetIndent(lines[fromIdx]);

                lines.RemoveRange(fromIdx, objEndIdx - fromIdx + 1);

                var newLines = new List<string> { "codeSplitting: {".Indent(indent), "  groups: [".Indent(indent) };

                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    var packages = prop.Value.EnumerateArray().Select(e => e.GetString()!).ToArray();
                    var normalized = packages
                        .Select(p => Regex.Match(p, @"^(@[^/]+)/").Success ? Regex.Match(p, @"^(@[^/]+)/").Groups[1].Value : p)
                        .Distinct()
                        .ToArray();
                    var regexParts = string.Join("|", normalized.Select(p => Regex.Escape(p).Replace("/", "[\\\\/]")));
                    newLines.AddRange(new[]
                    {
                        "    {",
                        $"      name: '{prop.Name}',",
                        $"      test: /node_modules[\\\\/]({regexParts})/,",
                        "      priority: 10,",
                        "    },",
                    }.Select(l => l.Indent(indent)));
                }

                newLines.Add("  ],".Indent(indent));
                newLines.Add("},".Indent(indent));

                lines.InsertRange(fromIdx, newLines);
                return true;
            });
        });

        uctx.ForeachCodeFile(@"*.tsx", file =>
        {
            file.Replace("AppContext.Expander.onGetExpanded", "AppContext.Expander.Options.onGetExpanded");
            file.Replace("AppContext.Expander.onSetExpanded", "AppContext.Expander.Options.onSetExpanded");
            file.Replace("Services.NotifyPendingFilter.notifyPendingRequests", "Services.NotifyPendingFilter.Options.notifyPendingRequests");
            file.Replace("Notify.singleton && Notify.singleton.", "Notify.getSingleton()?.");
            file.Replace("Notify.singleton", "Notify.getSingleton()");
            file.Replace("NumberFormatSettings.defaultNumberFormatLocale", "NumberFormatSettings.Options.defaultNumberFormatLocale");
            file.Replace("Services.VersionFilter.versionHasChanged", "Services.VersionFilter.Options.versionHasChanged");
            file.Replace("AuthClient.validatePassword", "AuthClient.Options.validatePassword");
        });

        SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Remember to:");
        SafeConsole.WriteLineColor(ConsoleColor.DarkMagenta, "Run yarn install after this upgrade");
    }
}
