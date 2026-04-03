using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20211109_SimplifyNamespaces : CodeUpgradeBase
{
    public override string Description => "Uses file-scoped namespaces declarations to recover ident space";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.cs", file =>
        {
            file.WarningLevel = WarningLevel.Warning;
            file.ProcessLines(lines =>
            {
                var index = lines.FindIndex(a => a.StartsWith("namespace "));
                if (index == -1)
                {
                    file.Warning($"File {file.FilePath} has no namespace ?!?");
                    return false;
                }

                if (lines[index].EndsWith(";"))
                {
                    file.Warning($"File {file.FilePath} already simplified");
                    return false;
                }

                var ns = lines[index].After("namespace ");

                var startIndex = lines.FindIndex(index, a => a.Trim() == "{");
                var endIndex = lines.FindLastIndex(a => a.Trim() == "}");

                if (startIndex == -1 || endIndex == -1)
                {
                    file.Warning($"File {file.FilePath} has no namespace ?!?");
                    return false;
                }

                for (int i = startIndex +1; i < endIndex; i++)
                {
                    var line = lines[i];

                    lines[i] = line.StartsWith("\t") ? line.Substring(1) :
                    line.StartsWith("    ") ? line.Substring(4) :
                    line;
                }

                lines[index] = "namespace " + ns + ";";
                lines[startIndex] = "";
                lines.RemoveAt(endIndex);

                return true;
            });
        });
    }
}
