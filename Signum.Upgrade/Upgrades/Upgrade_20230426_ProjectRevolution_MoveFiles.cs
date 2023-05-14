using LibGit2Sharp;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20230426_ProjectRevolution_MoveFiles : CodeUpgradeBase
{
    public override string Description => "Project REVOLUTION";

    public override void Execute(UpgradeContext uctx)
    {
        SafeConsole.WriteLineColor(ConsoleColor.Magenta, "This upgrade will completely re-structure your application!!");
        Console.WriteLine("Some important considerations:");
        Console.WriteLine("* After running this upgrade, manual work is expected fixing namespaces, so it is recommended that you run it from the framework branch origin/revolution to avoid extra changes that could come in the future.");
        Console.WriteLine("* Read XXXXX before continuing.");

        Console.WriteLine();
        Console.WriteLine("Press any key when you have read it");

        Console.ReadLine();

        var entities = uctx.AbsolutePath(uctx.ApplicationName + ".Entities");
        var logic = uctx.AbsolutePath(uctx.ApplicationName + ".Logic");
        var react = uctx.AbsolutePath(uctx.ApplicationName + ".React");

        Console.WriteLine($"The following projects are going to be combined together into a new project '{uctx.AbsolutePath(uctx.ApplicationName)}'");
        Console.WriteLine("* " + entities);
        Console.WriteLine("* " + logic);
        Console.WriteLine("* " + react);
        Console.WriteLine();

        string[] sysFolders = { "bin", "obj", "node-modules" };

        var entitiesDirectories = Directory.GetDirectories(entities).Where(a => !sysFolders.Contains(Path.GetFileName(a))).Select(a => new { Module = Path.GetFileName(a), Source = "Entities" }).ToList();
        var logicDirectories = Directory.GetDirectories(logic).Where(a => !sysFolders.Contains(Path.GetFileName(a))).Select(a => new { Module = Path.GetFileName(a), Source = "Logic" }).ToList();
        var reactDirectories = Directory.GetDirectories(Path.Combine(react, "App")).Where(a => !sysFolders.Contains(Path.GetFileName(a))).Select(a => new { Module = Path.GetFileName(a), Source = @"React\App" }).ToList();

        Console.WriteLine($"With the current structure, the following folders/namespaces will be created in the new project '{uctx.AbsolutePath(uctx.ApplicationName)}'");

        var resultDirectories = entitiesDirectories.Concat(logicDirectories).Concat(reactDirectories)
            .GroupBy(a => a.Module, a => a.Source)
            .Select(a => new { Module = a.Key, Source = a.ToString(", ") })
            .ToFormattedTable();

        Console.Write(resultDirectories);
        Console.WriteLine();
        Console.WriteLine("To keep things organized in modules, it is recommended to have a similar folder in each project before executing this migration!");
        
        Console.WriteLine();
        if (!SafeConsole.Ask("Are you happy with this structure?"))
        {
            Console.WriteLine("Organize your source code in a parallel folder structure, " +
                "don't bother about making it compile since the namespaes are going to change anyway (MyApp.Entities.Customers -> MyApp.Customers), " +
                "and execute the Upgrade again when you are ready.");

            throw new InvalidOperationException("Execute the Upgrade again when you are ready.");
        }


        var starterCS = File.ReadAllText(uctx.AbsolutePathSouthwind("Southwind.Logic/Starter.cs"));
        var reactCSPROJ = File.ReadAllText(uctx.AbsolutePathSouthwind("Southwind.React/Southwind.React.csproj"));



        var references = new[] {
            uctx.TryGetCodeFile("Southwind.Entities/Southwind.Entities.csproj"),
            uctx.TryGetCodeFile("Southwind.Logic/Southwind.Logic.csproj"),
            uctx.TryGetCodeFile("Southwind.React/Southwind.React.csproj")
        }.NotNull()
        .SelectMany(a => a.Content.Lines())
        .Where(l => l.Contains("<PackageReference") && !l.Contains("Microsoft.TypeScript.MSBuild") && !l.Contains("Signum"))
        .Distinct()
        .ToList();

        uctx.CreateCodeFile("Southwind/Southwind.csproj", $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
            	<PropertyGroup>
            		<TargetFramework>net7.0</TargetFramework>
            		<Nullable>enable</Nullable>
            		<WarningsAsErrors>nullable</WarningsAsErrors>
            		<IsPackable>false</IsPackable>
            		<AssemblyVersion>1.0.0.*</AssemblyVersion>
            		<FileVersion>1.0.0.0</FileVersion>
            		<Deterministic>false</Deterministic>
            		<UserSecretsId>{uctx.ApplicationName}</UserSecretsId>
            		<GenerateDocumentationFile>true</GenerateDocumentationFile>
            		<DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
            		<NoWarn>8618,1591</NoWarn>
                    <AccelerateBuildsInVisualStudio>true</AccelerateBuildsInVisualStudio>
                    <TSC_Build>true</TSC_Build>
            	</PropertyGroup>

            	<ItemGroup>
            	  <Compile Remove="CodeGen\**" />FROM_CS CompileDynamicCode
            	  <Content Remove="CodeGen\**" />FROM_CS CompileDynamicCode
            	  <EmbeddedResource Remove="CodeGen\**" />FROM_CS CompileDynamicCode
            	  <None Remove="CodeGen\**" />FROM_CS CompileDynamicCode
            	  <TypeScriptCompile Remove="CodeGen\**" />FROM_CS CompileDynamicCode
            	  <TypeScriptCompile Remove="node_modules\**" />
            	</ItemGroup>

            	<ItemGroup>
            		<PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.0.4">
            			<PrivateAssets>all</PrivateAssets>
            			<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
            		</PackageReference>
            		<PackageReference Include="Signum.TSGenerator" Version="7.5.0" />
                    <PackageReference Include="Signum.Analyzer" Version="3.2.0" />
                    <PackageReference Include="Signum.MSBuildTask" Version="7.5.0" />
            {references.ToString("\r\n")}
            	</ItemGroup>

            	<ItemGroup>
            		<ProjectReference Include="..\Framework\Extensions\Signum.Alerts\Signum.Alerts.csproj" />FROM_CS AlertLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Authorization.ActiveDirectory\Signum.Authorization.ActiveDirectory.csproj" />FROM_CS activeDirectoryIntegration: true
            		<ProjectReference Include="..\Framework\Extensions\Signum.Authorization.ResetPassword\Signum.Authorization.ResetPassword.csproj" />FROM_CS ResetPasswordRequestLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Authorization\Signum.Authorization.csproj" />FROM_CS AuthLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Caching\Signum.Caching.csproj" />FROM_CS CacheLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Chart\Signum.Chart.csproj" />FROM_CS ChartLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.CodeMirror\Signum.CodeMirror.csproj" />FROM_CS EmailLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.ConcurrentUser\Signum.ConcurrentUser.csproj" />FROM_CS ConcurrentUserLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Dashboard\Signum.Dashboard.csproj" />FROM_CS DashboardLogic.Start
            		<ProjectReference Include="..\Framework\Extensions\Signum.DiffLog\Signum.DiffLog.csproj" />FROM_CS DiffLogLogic.Start
            		<ProjectReference Include="..\Framework\Extensions\Signum.Dynamic\Signum.Dynamic.csproj" />FROM_CS CompileDynamicCode
            		<ProjectReference Include="..\Framework\Extensions\Signum.Eval\Signum.Eval.csproj" />FROM_CS EmailLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Excel\Signum.Excel.csproj" />FROM_CS ExcelLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Files\Signum.Files.csproj" />FROM_CS FileLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Help\Signum.Help.csproj" />FROM_CS HelpLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.HtmlEditor\Signum.HtmlEditor.csproj" />FROM_CS EmailLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Isolation\Signum.Isolation.csproj" />FROM_CS CompileDynamicCode
            		<ProjectReference Include="..\Framework\Extensions\Signum.MachineLearning\Signum.MachineLearning.csproj" />FROM_CS PredictorLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Mailing.MicrosoftGraph\Signum.Mailing.MicrosoftGraph.csproj" />FROM_CS activeDirectoryIntegration: true
            		<ProjectReference Include="..\Framework\Extensions\Signum.Mailing.Package\Signum.Mailing.Package.csproj" />FROM_CS EmailLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Mailing\Signum.Mailing.csproj" />FROM_CS EmailLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Map\Signum.Map.csproj" />FROM_CS MapLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Migrations\Signum.Migrations.csproj" />FROM_CS MigrationLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Notes\Signum.Notes.csproj" />FROM_CS NoteLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Omnibox\Signum.Omnibox.csproj" />FROM_CS OmniboxLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Processes\Signum.Processes.csproj" />FROM_CS ProcessLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Profiler\Signum.Profiler.csproj" />FROM_CS ProfilerLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Rest\Signum.Rest.csproj" />FROM_CS RestLogLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Scheduler\Signum.Scheduler.csproj" />FROM_CS SchedulerLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.SMS\Signum.SMS.csproj" />FROM_CS SMSLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Templating\Signum.Templating.csproj" />FROM_CS EmailLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.TimeMachine\Signum.TimeMachine.csproj" />FROM_CS DiffLogLo
            		<ProjectReference Include="..\Framework\Extensions\Signum.Toolbar\Signum.Toolbar.csproj" />FROM_CS ToolbarLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Translation\Signum.Translation.csproj" />FROM_CS TranslationLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.UserAssets\Signum.UserAssets.csproj" />FROM_CS UserQueryLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.UserQueries\Signum.UserQueries.csproj" />FROM_CS UserQueryLogic 
            		<ProjectReference Include="..\Framework\Extensions\Signum.ViewLog\Signum.ViewLog.csproj" />FROM_CS ViewLogLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Word\Signum.Word.csproj" />FROM_CS WordTemplateLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Workflow\Signum.Workflow.csproj" />FROM_CS WorkflowLogicStarter
            		<ProjectReference Include="..\Framework\Signum.Utilities\Signum.Utilities.csproj" /> 
            		<ProjectReference Include="..\Framework\Signum\Signum.csproj" />
            	</ItemGroup>

            	<Target Name="PublishCollectDist" AfterTargets="ComputeFilesToPublish">
            		<!-- Include the newly-built files in the publish output -->
            		<ItemGroup>
            			<DistFiles Include="wwwroot\dist\**;" />
            			<ResolvedFileToPublish Include="@(DistFiles->'%(FullPath)')" Exclude="@(ResolvedFileToPublish)">
            				<RelativePath>%(DistFiles.Identity)</RelativePath>
            				<CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
            			</ResolvedFileToPublish>
            		</ItemGroup>
            	</Target>
            </Project>
            """.Lines().Select(l =>
        {
            if (l.Contains("FROM_CS"))
            {
                var dependency = l.After("FROM_CS").Trim();

                return starterCS.Contains(dependency) ? l.Before("FROM_CS") : null;
            }

            return l;
        }).NotNull().ToString("\n"));

        uctx.CreateCodeFile("Directory.Build.props", 
            $"""
            <Project>
            	<PropertyGroup>
             		<TypeScriptCompileBlocked>true</TypeScriptCompileBlocked>
            		<AccelerateBuildsInVisualStudio>true</AccelerateBuildsInVisualStudio>
            	</PropertyGroup>
            </Project>
            """);

        uctx.CreateCodeFile("Directory.Build.targets",
            $"""
            <Project>
            	<ItemGroup>
            		<Compile Remove="ts_out\**" />
            		<Content Remove="ts_out\**" />
            		<EmbeddedResource Remove="ts_out\**" />
            		<None Remove="ts_out\**" />
            		<TypeScriptCompile Remove="ts_out\**" />
            	</ItemGroup>
            	<ItemGroup>
            		<Content Update="package.json">
                        <CopyToOutputDirectory>Never</CopyToOutputDirectory>
                        <CopyToPublishDirectory>Never</CopyToPublishDirectory>
            		</Content>
            		<Content Update="tsconfig.json">
            			<CopyToOutputDirectory>Never</CopyToOutputDirectory>
                        <CopyToPublishDirectory>Never</CopyToPublishDirectory>
            		</Content>
            	</ItemGroup>
            	<ItemGroup>
            		<None Update="Translations\*.xml">
            			<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
            		</None>
            		<!--
            		<None Update="Translations\*.en.xml">
            			<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
            		</None>
            		<None Update="Translations\*.de.xml">
            			<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
            		</None>
            		-->
            	</ItemGroup>
            </Project>
            """);

        uctx.CreateCodeFile("package.json",
            $$"""
            {
              "private": true,
              "resolutions": {
                "@types/react": "18.0.35"
              },
              "workspaces": [
                "Framework/Signum",
                "Framework/Extensions/*",
                "{{uctx.ApplicationName}}"
              ]
            }
            """);

        var southwind = uctx.AbsolutePathSouthwind("Southwind");

        uctx.DeleteFile("Southwind.Entities/Southwind.Entities.csproj");
        uctx.DeleteFile("Southwind.Entities/Properties/GlobalUsings.cs");
        uctx.DeleteFile("Southwind.Logic/Southwind.Logic.csproj");
        uctx.DeleteFile("Southwind.Logic/Properties/GlobalUsings.cs");
        uctx.DeleteFile("Southwind.React/Southwind.React.csproj");
        uctx.DeleteFile("Southwind.React/Properties/GlobalUsings.cs");

        uctx.ForeachCodeFile("*.cs", "Southwind.Entities", a =>
        {
            var fileName = Path.GetFileNameWithoutExtension(a.FilePath);
            if (!fileName.EndsWith("Entity") && !fileName.EndsWith("Embedded")  && !fileName.EndsWith("Model"))
            {
                if (a.Content.Contains($"class {fileName}Entity"))
                    uctx.MoveFile(a.FilePath, Path.GetDirectoryName(a.FilePath) + "/" + fileName + "Entity.cs");
                else if (a.Content.Contains($"class {fileName}Embedded"))
                    uctx.MoveFile(a.FilePath, Path.GetDirectoryName(a.FilePath) + "/" + fileName + "Embedded.cs");
                else if (a.Content.Contains($"class {fileName}Model"))
                    uctx.MoveFile(a.FilePath, Path.GetDirectoryName(a.FilePath) + "/" + fileName + "Model.cs");
            }
        });

        uctx.ForeachCodeFile("*.xml", "Southwind.Entities", a =>
        {
            var fileName = a.FilePath.Replace(".Entities", "");

            uctx.MoveFile(a.FilePath, fileName, createDirectory: true);
        });

        uctx.MoveFiles("Southwind.Entities", "Southwind", "*.*");
        uctx.MoveFiles("Southwind.Logic", "Southwind", "*.*");


        uctx.ForeachCodeFile("*.t4s", "Southwind.React/App", a =>
        {
            var newFilePath = a.FilePath.Replace(".Entities", "");
            if (newFilePath != a.FilePath)
            {
                uctx.MoveFile(a.FilePath, newFilePath);
                if (File.Exists(uctx.AbsolutePath(Path.ChangeExtension(a.FilePath, ".ts"))))
                    uctx.MoveFile(Path.ChangeExtension(a.FilePath, ".ts"), Path.ChangeExtension(newFilePath, ".ts"));
            }
        });

        uctx.ChangeCodeFile("Southwind.React/Properties/launchSettings.json", a =>
        {
            a.Replace(uctx.ApplicationName + ".React", uctx.ApplicationName);
        });

        uctx.ForeachCodeFile("*.cs", "Southwind.React", a =>
        {
            if(a.Content.Contains(": ControllerBase") || a.Content.Contains(": ControllerBase") || a.Content.Contains("HttpPost") || a.Content.Contains("HttpGet") || a.Content.Contains("FromBody"))
            {
                a.InsertBeforeFirstLine(a => a.StartsWith("using "), "using Microsoft.AspNetCore.Mvc;");
                a.InsertBeforeFirstLine(a => a.StartsWith("using "), "using System.ComponentModel.DataAnnotations;");
                a.Replace("FilesController.GetFileStreamResult", "MimeMapping.GetFileStreamResult");
            }
        });

        uctx.MoveFiles("Southwind.React/App", "Southwind", "*.*");
        uctx.MoveFiles("Southwind.React/Controllers", "Southwind", "*.*");
        uctx.MoveFiles("Southwind.React/Views/Home", "Southwind", "*.*");
        uctx.MoveFiles("Southwind.React", "Southwind", "*.*");
                
        uctx.MoveFiles("Southwind", "", "yarn.lock");


        uctx.DeleteDirectory("Southwind.Entities");
        uctx.DeleteDirectory("Southwind.Logic");
        uctx.DeleteDirectory("Southwind.React");


        uctx.CreateCodeFile("Southwind/Properties/GlobalUsings.cs", """
            global using System;
            global using System.Collections.Generic;
            global using System.ComponentModel;
            global using System.Linq;
            global using System.Linq.Expressions;
            global using System.Text;
            global using System.Reflection;
            global using Signum.Utilities;
            global using Signum.Utilities.ExpressionTrees;
            global using Signum.Entities;
            global using Signum.Entities.Reflection;
            global using Signum.Entities.Validation;
            global using Signum.DynamicQuery;
            global using Signum.Operations;
            global using Signum.Engine;
            global using Signum.Basics;
            global using Signum.Security;
            global using System.Threading.Tasks;
            global using System.Threading;
            global using Signum.Engine.Maps;

            """);

        uctx.ChangeCodeFile("Southwind/package.json", a =>
        { 
            //remove all dependencies, bring back all custom dependencies manually
            a.ReplaceBetweenExcluded(a => a.Contains("\"dependencies\""), a => a.Contains("}"), "");
        });

        // fix solution

        uctx.ChangeCodeFile(uctx.ApplicationName + ".sln", f =>
        {
            f.Solution_RemoveProject("Signum.Entities");
            f.Solution_RemoveProject("Signum.Engine");
            f.Solution_RemoveProject("Signum.React");

            f.Solution_RemoveProject("Signum.Entities.Extensions");
            f.Solution_RemoveProject("Signum.Engine.Extensions");
            f.Solution_RemoveProject("Signum.React.Extensions");

            f.Solution_RemoveProject("Signum.Engine.MachineLearning.TensorFlow", WarningLevel.None);
            f.Solution_RemoveProject("Signum.React.Extensions.Selenium", WarningLevel.Warning);

            f.Solution_RemoveProject(uctx.ApplicationName + ".Entities");
            f.Solution_RemoveProject(uctx.ApplicationName + ".Logic");
            f.Solution_RemoveProject(uctx.ApplicationName + ".React");

            f.Solution_AddFolder("0.Solution Items");
            f.Solution_SolutionItem("Directory.Build.props", "0.Solution Items");
            f.Solution_SolutionItem("Directory.Build.targets", "0.Solution Items");

            var appProject = $"{uctx.ApplicationName}\\{uctx.ApplicationName}.csproj";
            f.Solution_AddProject(appProject, null);

            f.Solution_AddProject($"Framework\\Signum\\Signum.csproj", "1.Framework");

            var project = File.ReadAllText(uctx.AbsolutePath(appProject));

            var extRegex = new Regex(@"(?<ext>Framework\\Extensions\\[\w\.\\]*)");

            project.Lines().Where(l => l.Contains("<ProjectReference Include=\"..\\Framework\\Extensions")).ToList().ForEach(l =>
            {
                var extName = extRegex.Match(l).Groups["ext"].Value;
                f.Solution_AddProject(extName, "2.Extensions");
            });
        });

        uctx.ChangeCodeFile(".gitignore", file =>
        {
            file.RemoveAllLines(a => a.Contains("node_modules"));
            file.RemoveAllLines(a => a.Contains(uctx.ApplicationName));
            file.RemoveAllLines(a => a.Contains("packages"));

            file.ProcessLines(lines =>
            {
                lines.Add("node_modules");
                lines.Add(uctx.ReplaceSouthwind("/Southwind/CodeGen/**"));
                lines.Add(uctx.ReplaceSouthwind("/Southwind/wwwroot/dist/**"));
                lines.Add(uctx.ReplaceSouthwind("/Southwind/web.config/**"));
                lines.Add(uctx.ReplaceSouthwind("/Southwind/ts_out/**"));
                lines.Add(uctx.ReplaceSouthwind("/Southwind/TensorFlowModels/**"));
                lines.Add("Framework.tar");

                return true;
            });
        });

        uctx.ChangeCodeFile("Southwind/Dockerfile", file =>
        {
            file.ReplaceLine(a => a.Contains("FROM mcr.microsoft.com/dotnet/aspnet"), 
                "FROM mcr.microsoft.com/dotnet/aspnet:7.0-bullseye-slim AS base");
            file.ReplaceLine(a => a.Contains("FROM mcr.microsoft.com/dotnet/sdk"), 
                "FROM mcr.microsoft.com/dotnet/sdk:7.0-bullseye-slim AS build");

            file.ReplaceBetween(
                new (a => a.StartsWith("COPY [")),
                new (a => a.StartsWith("COPY [")) { LastIndex = true },
                uctx.ReplaceSouthwind("""
                COPY ["Framework.tar", "/"]
                RUN tar -xvf /Framework.tar

                COPY ["Southwind/Southwind.csproj", "Southwind/"]
                COPY ["Southwind/package.json", "Southwind/"]
                COPY ["package.json", ""]
                COPY ["yarn.lock", ""]
                """));

            file.Replace(uctx.ApplicationName + ".React", uctx.ApplicationName);

        });

        uctx.ForeachCodeFile("deploy*.ps1", a =>
        {
            a.InsertBeforeFirstLine(a => a.Contains("docker build"),
                """Get-ChildItem -Path "Framework" -Recurse -Include "package.json","*.csproj" | Resolve-Path -Relative | tar -cf Framework.tar -T -""");

            a.Replace(uctx.ApplicationName + ".React", uctx.ApplicationName);
        });

        uctx.ChangeCodeFile("Southwind/Properties/Attributes.cs", a =>
        {
            a.InsertAfterFirstLine(a => a.Contains("DefaultAssemblyCulture"),
                "[assembly: AssemblySchemaName(\"dbo\")]");
        });
    }
    

    public static void MoveFilesTo(string sourcePath, string destPath, string fileTypes)
    {
        fileTypes.Split(',').ToList().ForEach(type => 
        {
            foreach (var f in Directory.GetFiles(sourcePath, type.Trim(), SearchOption.AllDirectories))
            {
                var filename = destPath + "\\" +Path.GetFileName(f);
                
                if(!File.Exists(filename))
                {
                    new FileInfo(filename!).Directory!.Create();
                    File.Move(f, filename);
                }
            }
        });
    }
}
