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


        var starterCS = File.ReadAllText(uctx.AbsolutePath(uctx.ReplaceSouthwind("Southwind.Logic/Starter.cs")));
        var reactCSPROJ = File.ReadAllText(uctx.AbsolutePath(uctx.ReplaceSouthwind("Southwind.React/Southwind.React.csproj")));

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
            	</PropertyGroup>

            	<ItemGroup>
            	  <Compile Remove="CodeGen\**" />FROM_CS DynamicLogic
            	  <Content Remove="CodeGen\**" />FROM_CS DynamicLogic
            	  <EmbeddedResource Remove="CodeGen\**" />FROM_CS DynamicLogic
            	  <None Remove="CodeGen\**" />FROM_CS DynamicLogic
            	  <TypeScriptCompile Remove="CodeGen\**" />FROM_CS DynamicLogic
            	  <TypeScriptCompile Remove="node_modules\**" />
            	</ItemGroup>

            	<ItemGroup>
            		<PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.0.4">
            			<PrivateAssets>all</PrivateAssets>
            			<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
            		</PackageReference>
            		<PackageReference Include="Signum.Analyzer" Version="3.2.0" />
            		<PackageReference Include="Signum.MSBuildTask" Version="7.5.0-beta" />
            		<PackageReference Include="Signum.TSGenerator" Version="7.5.0-beta9" />
            		<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />FROM_CSPROJ Swashbuckle
            		<PackageReference Include="SciSharp.TensorFlow.Redist" Version="2.11.0" />FROM_CSPROJ SciSharp
            	</ItemGroup>

            	<ItemGroup>
            		<ProjectReference Include="..\Framework\Extensions\Signum.Authorization.ActiveDirectory\Signum.Authorization.ActiveDirectory.csproj" />FROM_CS activeDirectoryIntegration: true
            		<ProjectReference Include="..\Framework\Extensions\Signum.Authorization.ResetPassword\Signum.Authorization.ResetPassword.csproj" />FROM_CS ResetPasswordRequestLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Authorization\Signum.Authorization.csproj" />FROM_CS AuthLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Caching\Signum.Caching.csproj" />FROM_CS CacheLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Chart\Signum.Chart.csproj" />FROM_CS ChartLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.CodeMirror\Signum.Codemirror.csproj" />FROM_CS EmailLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.ConcurrentUser\Signum.ConcurrentUser.csproj" />FROM_CS ConcurrentUserLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Dashboard\Signum.Dashboard.csproj" />FROM_CS DashboardLogic.Start
            		<ProjectReference Include="..\Framework\Extensions\Signum.DiffLog\Signum.DiffLog.csproj" />FROM_CS DiffLogLogic.Start
            		<ProjectReference Include="..\Framework\Extensions\Signum.Dynamic\Signum.Dynamic.csproj" />FROM_CS DynamicLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Eval\Signum.Eval.csproj" />FROM_CS EmailLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Excel\Signum.Excel.csproj" />FROM_CS ExcelLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Files\Signum.Files.csproj" />FROM_CS FileLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Help\Signum.Help.csproj" />FROM_CS HelpLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.HtmlEditor\Signum.HtmlEditor.csproj" />FROM_CS EmailLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.MachineLearning\Signum.MachineLearning.csproj" />FROM_CS PredictorLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Mailing.MicrosoftGraph\Signum.Mailing.MicrosoftGraph.csproj" />FROM_CS activeDirectoryIntegration: true
            		<ProjectReference Include="..\Framework\Extensions\Signum.Mailing.Package\Signum.Mailing.Package.csproj" />FROM_CS EmailLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Mailing\Signum.Mailing.csproj" />FROM_CS EmailLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Map\Signum.Map.csproj" />FROM_CS MapLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Migrations\Signum.Migrations.csproj" />FROM_CS MigrationLogic
            		<ProjectReference Include="..\Framework\Extensions\Signum.Notes\Signum.Notes.csproj" />FROM_CS NotesLogic
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

            	<Target Name="PostBuild" AfterTargets="PostBuildEvent">
            		<Exec Command="yarn run tsc --build" ConsoleToMSBuild="true">
            			<Output TaskParameter="ConsoleOutput" ItemName="OutputOfExec" />
            		</Exec>
            	</Target>
            </Project>
            """.Lines().Select(l =>
        {
            if (l.Contains("FROM_CS"))
            {
                var dependency = l.After("FROM_CS");

                return starterCS.Contains(dependency) ? l.Before("FROM_CS") : null;
            }

            if (l.Contains("FROM_CSPROJ"))
            {
                var dependency = l.After("FROM_CSPROJ");

                return reactCSPROJ.Contains(dependency) ? l.Before("FROM_CSPROJ") : null;
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
            		</Content>
            		<Content Update="tsconfig.json">
            			<CopyToOutputDirectory>Never</CopyToOutputDirectory>
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

        uctx.Solution_MoveFiles("Southwind.Entities", "Southwind", "*.*");
        uctx.Solution_MoveFiles("Southwind.Logic", "Southwind", "*.*");
        uctx.Solution_MoveFiles("Southwind.React/App", "Southwind", "*.*");
        uctx.Solution_MoveFiles("Southwind.React/Controllers", "Southwind", "*.*");
        uctx.Solution_MoveFiles("Southwind.React/Views/Home", "Southwind", "*.*");
        uctx.Solution_MoveFiles("Southwind.React", "Southwind", "*.*");
        
        uctx.Solution_MoveFiles("Southwind", "", "yarn.lock");


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

        // fix solution
        uctx.Solution_RemoveProject("Signum.Entities");
        uctx.Solution_RemoveProject("Signum.Engine");
        uctx.Solution_RemoveProject("Signum.React");

        uctx.Solution_RemoveProject("Signum.Entities.Extensions");
        uctx.Solution_RemoveProject("Signum.Engine.Extensions");
        uctx.Solution_RemoveProject("Signum.React.Extensions");

        uctx.Solution_RemoveProject("Signum.Engine.MachineLearning.TensorFlow");
        uctx.Solution_RemoveProject("Signum.React.Extensions.Selenium");

        uctx.Solution_RemoveProject(uctx.ApplicationName + ".Entities");
        uctx.Solution_RemoveProject(uctx.ApplicationName + ".Logic");
        uctx.Solution_RemoveProject(uctx.ApplicationName + ".React");

        var appProject = $"{uctx.ApplicationName}\\{uctx.ApplicationName}.csproj";
        uctx.Solution_AddProject(appProject, null);
        
        uctx.Solution_AddProject($"Framework\\Signum\\Signum.csproj", "1.Framework");

        var project = File.ReadAllText(uctx.AbsolutePath(appProject));

        var extRegex = new Regex(@"(?<ext>Framework\\Extensions\\[\w\.\\]*)");

        project.Lines().Where(l => l.Contains("<ProjectReference Include=\"..\\Framework\\Extensions")).ToList().ForEach(l => 
        { 
            var extName = extRegex.Match(l).Groups["ext"].Value;
            uctx.Solution_AddProject(extName, "2.Extensions");
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
