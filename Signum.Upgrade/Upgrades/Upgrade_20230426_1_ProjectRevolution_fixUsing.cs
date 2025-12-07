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
            file.Replace(new Regex(@"(\.\.\/)*Framework/Signum.React.Extensions/(?<ext>[\w]*)/"), m => "@extensions/Signum." + m.Groups["ext"]+"/");
            file.Replace(new Regex(@"@extensions/Signum.Files/(FileLine|FileImage|FileImageLine|MultiFileLine|ImageModal|FileDownloader|FileUploader)"), m => "@extensions/Signum.Files/Files");

            file.Replace(new Regex(@"(?<extension>[\w]*\.Entities\.[\w]*)"), m => m.Groups["extension"].Value.Replace(".Entities", ""));

            file.Replace(new Regex(@"(\.\.\/)*Framework/Signum.React/Scripts"), m => "@framework");

            file.Replace("@extensions/Signum.Basics/Color", "@framework/Basics/Color");
            file.Replace("@extensions/Signum.Authorization/AzureAD", "@extensions/Signum.Authorization.ActiveDirectory/AzureAD");
        });

        uctx.ForeachCodeFile(@"*.cs", file =>
        {
            if (file.FilePath.EndsWith("GlobalUsings.cs"))
            {
                file.Replace("Signum.Engine.Operations;", "Signum.Operations;");

                return;
            }

            if(file.FilePath.StartsWith(uctx.ApplicationName  + "\\"))
                file.Replace(new Regex(@"namespace (?<namespace>[^;]*)"), m => $"namespace {Path.GetDirectoryName(file.FilePath)!.Replace("\\", ".")}");

            file.Replace(new Regex(@"(?<extension>[\w]*\.Entities\.[\w]*)"), m => m.Groups["extension"].Value.Replace(".Entities", ""));
            file.Replace(new Regex(@"(?<extension>[\w]*\.Engine\.[\w]*)"), m => m.Groups["extension"].Value.Replace(".Engine", ""));
            file.Replace(new Regex(@"(?<extension>[\w]*\.Logic\.[\w]*)"), m => m.Groups["extension"].Value.Replace(".Logic", ""));
            file.Replace(new Regex(@"(?<extension>[\w]*\.React\.[\w]*)"), m => m.Groups["extension"].Value.Replace(".React", ""));
            file.Replace(new Regex(uctx.ReplaceSouthwind(@"Southwind\.(Entities|Logic|React)")), uctx.ReplaceSouthwind("Southwind"));

            file.Replace("Security.EncodePassword(", "PasswordEncoding.EncodePassword(");
            file.Replace("DynamicCode.AssemblyTypes", "EvalLogic.AssemblyTypes");           
            file.Replace("DynamicCode.Namespaces", "EvalLogic.Namespaces");            
            file.Replace("DynamicCode.AddFullAssembly", "EvalLogic.AddFullAssembly");            
            file.Replace("DynamicLogic.Start(sb, withCodeGen: true);", "DynamicLogic.Start(sb);");
            file.Replace("DynamicLogic.Start(sb, withCodeGen: false);", "EvalLogic.Start(sb);");
            file.Replace("TranslatedInstanceLogic.TranslatedField", "PropertyRouteTranslationLogic.TranslatedField");
            file.Replace("using Signum.Services", "using Signum.Security");
            file.Replace("using Signum.Validation", "using Signum.Entities.Validation");
            file.Replace("using Signum.Maps", "using Signum.Engine.Maps");
            file.Replace("using Signum.ActiveDirectory", "using Signum.Authorization.ActiveDirectory");
        });

        uctx.ForeachCodeFile(@"*.cshtml", file =>
        {
            file.Replace(new Regex(@"(?<extension>[\w]*\.Entities\.[\w]*)"), m => m.Groups["extension"].Value.Replace(".Entities", ""));
            file.Replace(new Regex(@"(?<extension>[\w]*\.Engine\.[\w]*)"), m => m.Groups["extension"].Value.Replace(".Engine", ""));
            file.Replace(new Regex(@"(?<extension>[\w]*\.Logic\.[\w]*)"), m => m.Groups["extension"].Value.Replace(".Logic", ""));
            file.Replace(new Regex(@"(?<extension>[\w]*\.React\.[\w]*)"), m => m.Groups["extension"].Value.Replace(".React", ""));
            file.Replace(new Regex(uctx.ReplaceSouthwind(@"Southwind\.(Entities|Logic|React)")), uctx.ReplaceSouthwind("Southwind"));
        });

        uctx.ForeachCodeFile(@"*.xml", "Southwind.Terminal", file =>
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

            file.UpdateNugetReference("Signum.MSBuildTask", "7.5.0");

            file.Replace(
                uctx.ReplaceSouthwind("Signum.React.Extensions.Selenium\\Signum.React.Extensions.Selenium"), 
                uctx.ReplaceSouthwind("Extensions\\Signum.Selenium\\Signum.Selenium"));
        });

        uctx.ForeachCodeFile(@"appsettings.json, appsettings.*.json", new[] { "Southwind.Test.Environment", "Southwind.Terminal" }, file =>
        {
            uctx.MoveFile(file.FilePath, file.FilePath.Replace("appsettings", "settings"));
        });

        uctx.ChangeCodeFile(@"Southwind.Test.Environment/Southwind.Test.Environment.csproj", file =>
        {
            file.Replace("appsettings", "settings");
        });

        uctx.ChangeCodeFile(@"Southwind.Terminal/Southwind.Terminal.csproj", file =>
        {
            file.Replace("appsettings", "settings");
        });

        uctx.ChangeCodeFile(@"Southwind.Test.Environment/SouthwindEnvironment.cs", file =>
        {
            file.Replace("appsettings", "settings");
        });

        uctx.ForeachCodeFile("*.cs", @"Southwind.Test.React", file =>
        {
            file.Replace("appsettings", "settings");
        });

        uctx.ChangeCodeFile(@"Southwind.Terminal/Program.cs", file =>
        {
            file.Replace("appsettings", "settings");

            file.Replace(@".GetValue<string>(""BroadcastUrls"")", ".GetValue<string>(\"BroadcastUrls\"), wsb: null");
            file.RemoveAllLines(l => l.Contains("{\"CT\", TranslationLogic.CopyTranslations}"));
            file.InsertAfterFirstLine(l => l.Contains("{\"L\", () => Load(null), \"Load\"}"), "{\"CT\", TranslationLogic.CopyTranslations},");
        });

        uctx.ChangeCodeFile(@"Southwind.Test.Environment\SouthwindEnvironment.cs", file =>
        {
            file.Replace(@".GetValue<string>(""BroadcastUrls"")", ".GetValue<string>(\"BroadcastUrls\"), wsb: null");
        });

        uctx.ChangeCodeFile(@"Southwind/HomeController.cs", file =>
        {
            file.Replace("return View();", @"return View(""~/Index.cshtml"");");
        });
    }
}
