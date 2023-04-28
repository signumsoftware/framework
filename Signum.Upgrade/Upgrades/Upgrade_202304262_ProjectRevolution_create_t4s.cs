using LibGit2Sharp;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Signum.Upgrade.Upgrades;

class Upgrade_202304262_ProjectRevolution_create_t4s : CodeUpgradeBase
{
    public override string Description => "Project REVOLUTION - create t4s files";

    public override void Execute(UpgradeContext uctx)
    {
        var createdFiles = new List<string>();

        uctx.ForeachCodeFile(@"*.cs", file =>
        {
            if(file.Content.Contains("[EntityKind(") )
            {
                var dir = Path.GetDirectoryName(file.FilePath);
                var typeFile = dir + "\\" + $"{dir!.Replace("\\", ".")}.t4s";
                if(!createdFiles.Contains(typeFile))
                {
                    uctx.CreateCodeFile(typeFile, "");
                    createdFiles.Add(typeFile);
                }
            }
        });
    }
}
