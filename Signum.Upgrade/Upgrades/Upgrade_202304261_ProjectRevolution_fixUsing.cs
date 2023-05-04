using LibGit2Sharp;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Signum.Upgrade.Upgrades;

class Upgrade_202304261_ProjectRevolution_fixUsing : CodeUpgradeBase
{
    public override string Description => "Project REVOLUTION - fix framework and extensions using and imports";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.tsx, *.ts", file =>
        {
            file.Replace(new Regex(@"@extensions\/(?<ext>[\w]*)"), m => "@extensions/Signum." + m.Groups["ext"]);
            
            file.Replace(new Regex(@"(?<extension>[\w]*\.Entities\.[\w]*)"), m => m.Groups["extension"].Value.Replace(".Entities", ""));

            file.Replace(new Regex(@"(\.\.\/)*Framework\/Signum.React\/Scripts"), m => "@framework");
        });

        uctx.ForeachCodeFile(@"*.cs", file =>
        {
            if(file.FilePath.StartsWith(uctx.ApplicationName  + "/"))
                file.Replace(new Regex(@"namespace (?<namespace>[^;]*)"), m => $"namespace {Path.GetDirectoryName(file.FilePath)!.Replace("\\", ".")}");

            file.Replace(new Regex(@"(?<extension>[\w]*\.Entities\.[\w]*)"), m => m.Groups["extension"].Value.Replace(".Entities", ""));
            file.Replace(new Regex(@"(?<extension>[\w]*\.Engine\.[\w]*)"), m => m.Groups["extension"].Value.Replace(".Engine", ""));
            file.Replace(new Regex(@"(?<extension>[\w]*\.Logic\.[\w]*)"), m => m.Groups["extension"].Value.Replace(".Logic", ""));
            file.Replace(new Regex(@"(?<extension>[\w]*\.React\.[\w]*)"), m => m.Groups["extension"].Value.Replace(".React", ""));

            file.Replace("Security.EncodePassword(", "PasswordEncoding.EncodePassword(");
            file.Replace("DynamicCode.AssemblyTypes", "EvalLogic.AssemblyTypes");           
            file.Replace("DynamicCode.Namespaces", "EvalLogic.Namespaces");            
            file.Replace("DynamicCode.AddFullAssembly", "EvalLogic.AddFullAssembly");            
            file.Replace("DynamicLogic.Start(sb, withCodeGen: true);", "EvalLogic.Start(sb);\nDynamicLogic.Start(sb);");
            file.Replace("DynamicLogic.Start(sb, withCodeGen: false);", "EvalLogic.Start(sb);");
            file.Replace("TranslatedInstanceLogic.TranslatedField", "PropertyRouteTranslationLogic.TranslatedField");
        });

        uctx.ForeachCodeFile(@"*.cshtml", file =>
        {
            file.Replace(new Regex(@"(?<extension>[\w]*\.Entities\.[\w]*)"), m => m.Groups["extension"].Value.Replace(".Entities", ""));
            file.Replace(new Regex(@"(?<extension>[\w]*\.Engine\.[\w]*)"), m => m.Groups["extension"].Value.Replace(".Engine", ""));
            file.Replace(new Regex(@"(?<extension>[\w]*\.Logic\.[\w]*)"), m => m.Groups["extension"].Value.Replace(".Logic", ""));
            file.Replace(new Regex(@"(?<extension>[\w]*\.React\.[\w]*)"), m => m.Groups["extension"].Value.Replace(".React", ""));
        });

        uctx.ForeachCodeFile(@"*.xml", file =>
        {
            file.Replace("DynamicPanelPermission.ViewDynamicPanel", "EvalPanelPermission.ViewDynamicPanel");
        });

        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.Replace(uctx.ReplaceSouthwind("Southwind.Entities\\Southwind.Entities.csproj"), uctx.ReplaceSouthwind("Southwind\\Southwind.csproj"));           
            file.Replace("Signum.Entities\\Signum.Entities.csproj", "Signum\\Signum.csproj");           

            file.RemoveAllLines(l => l.Contains(uctx.ReplaceSouthwind("Southwind.Logic\\Southwind.Logic.csproj")));
            file.RemoveAllLines(l => l.Contains("Signum.Entities.Extensions\\Signum.Entities.Extensions.csproj"));
            file.RemoveAllLines(l => l.Contains("Signum.Engine.Extensions\\Signum.Engine.Extensions.csproj"));
            file.RemoveAllLines(l => l.Contains("Signum.Engine\\Signum.Engine.csproj"));

            file.UpdateNugetReference("Signum.MSBuildTask", "7.5.0-beta");

            file.Replace(
                uctx.ReplaceSouthwind("Signum.React.Extensions.Selenium\\Signum.React.Extensions.Selenium"), 
                uctx.ReplaceSouthwind("Extensions\\Signum.Selenium\\Signum.Selenium"));
        });

        uctx.ChangeCodeFile(@"Southwind.Terminal/Program.cs", file =>
        {
            file.Replace(@"Configuration.GetValue<string>(""BroadcastUrls"")", "Configuration.GetValue<string>(\"BroadcastUrls\"), wsb: null");
            file.RemoveAllLines(l => l.Contains("{\"CT\", TranslationLogic.CopyTranslations}"));
            file.InsertAfterFirstLine(l => l.Contains("{\"L\", () => Load(null), \"Load\"}"), "{\"CT\", TranslationLogic.CopyTranslations},");
        });

        uctx.ChangeCodeFile(@"Southwind.Test.Environment\SouthwindEnvironment.cs", file =>
        {
            file.Replace(@"Configuration.GetValue<string>(""BroadcastUrls"")", "Configuration.GetValue<string>(\"BroadcastUrls\"), wsb: null");
        });

        uctx.ChangeCodeFile(@"Southwind/HomeController.cs", file =>
        {
            file.Replace("return View();", @"return View(""~/Index.cshtml"");");
        });
    }
}
