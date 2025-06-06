using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Signum.Upgrade.Upgrades;


class Upgrade_20240328_Extract_Server : CodeUpgradeBase
{
    public override string Description => "Extract Program.cs and webpack to Southwind.Server";

    public override void Execute(UpgradeContext uctx)
    {
        string ToServer(string filePath)
        {
            return filePath.Replace(uctx.ApplicationName, uctx.ApplicationName + ".Server");
        }

        uctx.ChangeCodeFile(".gitignore", c =>
        {
            c.Replace($"/{uctx.ApplicationName}/CodeGen/*", $"/{uctx.ApplicationName}.Server/CodeGen/*");
            c.Replace($"/{uctx.ApplicationName}/wwwroot/dist/*", $"/{uctx.ApplicationName}.Server/wwwroot/dist/*");
            c.Replace($"/{uctx.ApplicationName}/web.config/*", $"/{uctx.ApplicationName}.Server/web.config/*");
            c.Replace($"/{uctx.ApplicationName}/TensorFlowModels/*", $"/{uctx.ApplicationName}.Server/TensorFlowModels/*");

        });

        uctx.ForeachCodeFile("deploy*.ps1", c =>
        {
            c.Replace(@$".\{uctx.ApplicationName}\Dockerfile", $@".\{uctx.ApplicationName}.Server\Dockerfile");
        });

        uctx.ChangeCodeFile("package.json", c =>
        {
            c.ReplaceLine(c => c.Contains($@"""{uctx.ApplicationName}"""), $@"""{uctx.ApplicationName}"",
""{uctx.ApplicationName}.Server""");
        });

        Directory.CreateDirectory(uctx.AbsolutePath($"{uctx.ApplicationName}.Server"));

        uctx.ForeachCodeFile("appsettings.*", "Southwind", c =>
        {
            c.MoveFile(ToServer(c.FilePath));
        });

        uctx.ChangeCodeFile("Southwind/Dockerfile", c =>
        {
            c.ReplaceLine(a => a.Contains($@"COPY [""{uctx.ApplicationName}/package.json"","),
               $"""
               COPY ["{uctx.ApplicationName}.Server/{uctx.ApplicationName}.Server.csproj", "{uctx.ApplicationName}.Server/"]
               COPY ["{uctx.ApplicationName}.Server/package.json", "{uctx.ApplicationName}.Server/"]
               """);
            c.Replace(
                $@"WORKDIR ""/src/{uctx.ApplicationName}""",
                $@"WORKDIR ""/src/{uctx.ApplicationName}.Server"""
                );
            c.ReplaceLine(a => 
            a.Contains($@"RUN dotnet restore ""{uctx.ApplicationName}/{uctx.ApplicationName}.csproj"""),
                $@"RUN dotnet restore ""{uctx.ApplicationName}.Server/{uctx.ApplicationName}.Server.csproj"""
                );
            c.ReplaceLine(a => a.Contains($@"RUN dotnet publish ""{uctx.ApplicationName}.csproj"" -c Release -o /app/publish"),
                $@"RUN dotnet publish ""{uctx.ApplicationName}.Server.csproj"" -c Release -o /app/publish"
                );
            c.ReplaceLine(a => a.Contains(
                $@"ENTRYPOINT [""dotnet"", ""{uctx.ApplicationName}.dll""]"),
                $@"ENTRYPOINT [""dotnet"", ""{uctx.ApplicationName}.Server.dll""]"
                );

            c.MoveFile(ToServer(c.FilePath));
        });

        uctx.ChangeCodeFile("Southwind/package.json", c =>
        {
            c.MoveFile(ToServer(c.FilePath));
        });

        uctx.ChangeCodeFile("Southwind/Program.cs", c =>
        {
            c.ReplaceLine(a => a.Contains($@"namespace {uctx.ApplicationName};"),
                $@"namespace {uctx.ApplicationName}.Server;"
                );

            c.InsertAfterFirstLine(a => a.Contains($@"using Signum.Authorization;"),
                """
                using Signum.Basics;
                using Signum.Engine.Maps;
                using Signum.Utilities;
                """
                );

            c.MoveFile(ToServer(c.FilePath));
        });

        uctx.ChangeCodeFile("Southwind/Properties/launchSettings.json", c =>
        {
            c.Replace("localhost/" + uctx.ApplicationName, "localhost/" + uctx.ApplicationName + ".Server");
            c.MoveFile(ToServer(c.FilePath));
        });

        List<string> publisProfiles = new List<string>();
        if (Directory.Exists(uctx.AbsolutePathSouthwind("Southwind/Properties/PublishProfiles")))
            uctx.ForeachCodeFile("*.*", "Southwind/Properties/PublishProfiles", c =>
            {
                publisProfiles.Add(Path.GetFileName(c.FilePath));
                c.MoveFile(ToServer(c.FilePath));
            });

        uctx.CreateCodeFile("Southwind.Server/Southwind.Server.csproj", $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">

            	<PropertyGroup>
            		<TargetFramework>net8.0</TargetFramework>
            		<Nullable>enable</Nullable>
            		<AssemblyVersion>1.0.0.*</AssemblyVersion>
            		<FileVersion>1.0.0.0</FileVersion>
            		<Deterministic>false</Deterministic>
            		<UserSecretsId>{uctx.ApplicationName}</UserSecretsId>
            		<DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
            		<IsPackable>false</IsPackable>
            	</PropertyGroup>

            	<ItemGroup>
            		<Content Remove="Properties\launchSettings.json" />
            	</ItemGroup>

            	<ItemGroup>
            		<_WebToolingArtifacts Remove="Properties\launchSettings.json" />
            {publisProfiles.ToString(pp => $@"		<_WebToolingArtifacts Remove=""Properties\PublishProfiles\{pp}.pubxml"" />", "\r\n")}
            	</ItemGroup>

            	<ItemGroup>
            		<None Include="Properties\launchSettings.json">
            			<CopyToOutputDirectory>Never</CopyToOutputDirectory>
            			<ExcludeFromSingleFile>true</ExcludeFromSingleFile>
            			<CopyToPublishDirectory>Never</CopyToPublishDirectory>
            		</None>
            	</ItemGroup>

            	<ItemGroup>
            		<ProjectReference Include="..\Framework\Signum\Signum.csproj" />
            		<ProjectReference Include="..\{uctx.ApplicationName}\{uctx.ApplicationName}.csproj" />
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
            """);

        uctx.ChangeCodeFile("Southwind/vendors.js", c =>
        {
            c.MoveFile(ToServer(c.FilePath));
        });

        uctx.ChangeCodeFile("Southwind/web.config", c =>
        {
            c.MoveFile(ToServer(c.FilePath));
        });

        uctx.ChangeCodeFile("Southwind/webpack.config.dll.js", c =>
        {
            c.MoveFile(ToServer(c.FilePath));
        });

        uctx.ChangeCodeFile("Southwind/webpack.config.js", c =>
        {
            c.Replace(
                "main: [\"./MainPublic.tsx\"],",
                $"main: [\"../{uctx.ApplicationName}/MainPublic.tsx\"],");
            c.MoveFile(ToServer(c.FilePath));
        });

        Directory.Move(
            uctx.AbsolutePathSouthwind("Southwind/wwwroot"),
            uctx.AbsolutePathSouthwind("Southwind.Server/wwwroot"));

        uctx.DeleteFile("Southwind/yarn.lock", WarningLevel.None);

        uctx.ChangeCodeFile("Southwind.sln", c =>
        {
            c.Solution_AddProject($"{uctx.ApplicationName}.Server\\{uctx.ApplicationName}.Server.csproj",
                parentFolder: null,
                projecTypeId: "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");

        });

        uctx.ChangeCodeFile("Southwind/Southwind.csproj", c =>
        {
        

            c.RemoveAllLines(a => a.Contains("<Deterministic>"));
            c.RemoveAllLines(a => a.Contains("<IsPackable>"));
            c.RemoveAllLines(a => a.Contains("<AssemblyVersion>"));
            c.RemoveAllLines(a => a.Contains("<FileVersion>"));
            c.ReplaceLine(a => a.Contains("<UserSecretsId>"),
                "<OutputType>Library</OutputType>");

            c.RemoveAllLines(a => a.Contains("<DockerDefaultTargetOS>"));
            c.RemoveAllLines(a => a.Contains("<AccelerateBuildsInVisualStudio>"));
            c.ReplaceBetweenIncluded(
                a => a.Contains("<Target Name="),
                a => a.Contains("</Target>"),
                "");
        });
    }
}



