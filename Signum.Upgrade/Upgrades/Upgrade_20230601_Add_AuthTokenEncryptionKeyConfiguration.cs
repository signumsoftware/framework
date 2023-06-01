using LibGit2Sharp;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230601_Add_AuthTokenEncryptionKeyConfiguration : CodeUpgradeBase
{
    public override string Description => "Add AuthTokenEncryptionKey Configuration";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile($"{uctx.ApplicationName}/appsettings.json", file =>
        {
            file.InsertAfterFirstLine(l => l.Contains(@"""ServerName"""),
                $"\"AuthTokenEncryptionKey\": \"<Default Encryption Key for {uctx.ApplicationName} >\","
            );

            SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Remember to add configuration Key 'AuthTokenEncryptionKey' in production before deployment (any random string is ok)");
        });

        uctx.ChangeCodeFile($"{uctx.ApplicationName}/Program.cs", program =>
        {            
            program.ReplaceLine(l => l.Contains(@"AuthTokenEncryptionKey ="),
                @"AuthTokenEncryptionKey = app.Configuration.GetValue<string>(""AuthTokenEncryptionKey"")!,"
            );
        });

        var ConfigFunctionName = $"Start{uctx.ApplicationName}Configuration";
        var ConfigFunctionBody = "";
        uctx.ChangeCodeFile($"{uctx.ApplicationName}/Starter.cs", starter =>
        {
            starter.RemoveAllLines(l => l.Contains($"{ConfigFunctionName}(sb);"));
            
            starter.InsertBeforeFirstLine(l => l.Contains("RegisterTypeConditions(sb);"), "GlobalsLogic.Start(sb);");

            ConfigFunctionBody = starter.GetMethodBody(l => l.Contains($"{ConfigFunctionName}(SchemaBuilder sb)")).Replace("Configuration =", "\tStarter.Configuration =");
            starter.ReplaceBetweenIncluded(l => l.Contains($"{ConfigFunctionName}(SchemaBuilder sb)"), 
                l => l.StartsWith("    }"), "");
        });

        var dir = uctx.AbsolutePathSouthwind("Southwind/Globals");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            SafeConsole.WriteLineColor(ConsoleColor.Magenta, $"'{dir}' created, move ApplicationConfigurationEntity, UserMixin, etc.. there");
        }

        uctx.CreateCodeFile("Southwind/Globals/GlobalsLogic.cs", $$"""
            namespace {{uctx.ApplicationName}}.Globals;
            
            public static class GlobalsLogic
            {
                public static void Start(SchemaBuilder sb)
                {
                    if (sb.NotDefined(MethodBase.GetCurrentMethod()))
                    {
                {{ConfigFunctionBody}}
                    }
                }
            }            

            """);
    }
}
