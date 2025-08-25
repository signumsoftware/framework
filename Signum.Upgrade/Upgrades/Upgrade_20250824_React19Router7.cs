using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20250824_React19Router7 : CodeUpgradeBase
{
    public override string Description => "Webpack to Vite, React 19.1 and React Router 7.7";

    public override void Execute(UpgradeContext uctx)
    {
        var port = Random.Shared.Next(3000, 3500);

        uctx.ChangeCodeFile(@"Southwind.Server/appsettings.json", file =>
        {
            file.InsertAfterFirstLine(a => a.Contains("ServerName") || a.Contains("StartBackgroundProcesses"),
                $@"""ViteDevServerPort"": {port},");
        });

        uctx.ChangeCodeFile(@"Southwind.Server/Dockerfile", file =>
        {
            file.ReplaceLine(a => a.Contains("RUN yarn run build-production"),
                "RUN yarn run build");
        });

        uctx.CreateCodeFile(@"Southwind.Server/main.tsx", """
            import "../Southwind/MainPublic"
            """.Replace("Southwind", uctx.ApplicationName));

        uctx.ChangeCodeFile(@"Southwind.Server/package.json", file =>
        {
            file.ReplaceBetweenExcluded(
                fromLine: a => a.Contains("scripts"),
                toLine: a => a.Contains("}"),
                """
                "dev": "vite",
                "build": "vite build"
                """);

            file.InsertBeforeFirstLine(
                a => a.Contains("keywords"),
                """
                "engines": {
                  "node": ">=22.12.0"
                },
                """);

            file.RemoveAllLines(a => a.Contains("webpack"));
            file.RemoveAllLines(a => a.Contains("-loader"));
            file.RemoveAllLines(a => a.Contains("rimraf"));
            file.UpdateNpmPackage("sass", "1.90.0");
            file.UpdateNpmPackage("typescript", "5.9.2");
            file.AddNpmPackage("vite", "7.1.1", devDependencies: true);
            file.AddNpmPackage("@vitejs/plugin-react", "5.0.0", devDependencies: true);
        });

        uctx.DeleteFile(@"Southwind.Server\vendors.js");
        uctx.DeleteFile(@"Southwind.Server\webpack.config.dll.js");
        uctx.DeleteFile(@"Southwind.Server\webpack.config.js");

        uctx.CreateCodeFile(@"Southwind.Server\vite.config.js", $$"""
            import { defineConfig } from 'vite';
            import react from '@vitejs/plugin-react';
            import path from 'path';

            export default defineConfig({
              plugins: [react()],
              resolve: {
                alias: {
                  '@framework': path.resolve(__dirname, '../Framework/Signum/React'),
                  '@extensions': path.resolve(__dirname, '../Framework/Extensions'),
                },
              },
              base: '/dist/',
              build: {
                manifest: true, // Needed if you're using the manifest in .cshtml
                outDir: 'wwwroot/dist', // Or wherever your ASP.NET app serves static files
                emptyOutDir: true, // Clears old files on build
                rollupOptions: {
                  input: '/main.tsx', // Full path relative to root
                  output: {
                    manualChunks: {
                      // All dependencies in node_modules go into vendor.[hash].js
                      vendor: [
                        'react',
                        'react-dom',
                        'react-router-dom',
                        'react-widgets-up',
                        'react-bootstrap',
                        'bootstrap',
                        "@azure/msal-browser",
                        "luxon",
                        "@fortawesome/fontawesome-svg-core",
                        "@fortawesome/free-regular-svg-icons",
                        "@fortawesome/free-brands-svg-icons",
                        "@fortawesome/free-solid-svg-icons",
                        "@fortawesome/react-fontawesome",
                        //"d3"
                      ]
                    }
                  }
                },
              },
              server: {
                port: {{port}},
                strictPort: true,
              },
            });
            """);

        uctx.ChangeCodeFile(@"Southwind/Index.cshtml", file =>
        {
            file.ReplaceLine(a => a.Contains("//FileNotFoundException"), "//Requires:");

            file.ReplaceBetweenIncluded(
                a => a.Contains("string json"),
                a => a.Contains("string vendor"),
                """
                int? vitePort = Configuration.GetValue<int?>("ViteDevServerPort");
                var viteAssets = vitePort != null ? ViteAssets.FromViteServerUrl($"http://localhost:{vitePort}/dist/main.tsx") :
                	ViteAssets.FromManifestFile(System.IO.Path.Combine(hostingEnv.WebRootPath, "dist/.vite/manifest.json"), "main.tsx");
                """);

            file.InsertBeforeFirstLine(a => a.Contains("<script>"),
                """
                @if (vitePort != null)
                {
                    @ViteAssets.LoadViteReactRefresh(vitePort.Value)
                }
                """);

            file.ReplaceLine(a => a.Contains("window.onerror"), "function showError(error) {");
            file.ReplaceLine(a => a.Contains("};"), """
                }

                window.onerror = function (message, filename, lineno, colno, error) {
                    showError(error);
                };
                """);

            file.ReplaceLine(a => a.Contains("var supportIE = true;"), """
                var supportIE = false;
                """);

            file.ReplaceBetweenIncluded(
                fromLine: a => a.Contains("(function () {"),
                toLine: a => a.Contains("})();"),
                """
                @viteAssets.GetHtmlString(Url)
                """);
        });


        uctx.ChangeCodeFile(@"Southwind/MainPublic.tsx", file =>
        {
            file.ReplaceLine(a => a.Contains("react-widgets/scss/styles.scss"),
                @"import ""react-widgets-up/scss/styles.scss""");

            file.Replace(@"""react-widgets""", @"""react-widgets-up""");

            file.RemoveAllLines(a => a.Contains("__webpack_public_path__"));

        });

        uctx.ChangeCodeFile(@"Southwind/tsconfig.json", file =>
        {
            file.ReplaceLine(a => a.Contains("target"), """
                "target": "esnext",
                """);

            file.ReplaceLine(a => a.Contains("sourceMap"), """
                "sourceMap": true,
                """);

            file.ReplaceLine(a => a.Contains("moduleResolution"), """
                "moduleResolution": "bundler",
                """);

            file.ReplaceLine(a => a.Contains("jsx"), """
                "jsx": "react-jsx",
                """);

            file.ReplaceBetween(
                new ReplaceBetweenOption(a => a.Contains(@"""lib"": ["), 1),
                new ReplaceBetweenOption(a => a.Contains(@"]"), -1), """
                  "ESNext",
                  "dom"
                """
                );
        });

        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            file.UpdateNugetReference("Microsoft.TypeScript.MSBuild", "5.9.2");

            file.UpdateNugetReference("Microsoft.Extensions.Configuration", "9.0.8");
            file.UpdateNugetReference("Microsoft.Extensions.Configuration.Binder", "9.0.8");
            file.UpdateNugetReference("Microsoft.Extensions.Configuration.Json", "9.0.8");
            file.UpdateNugetReference("Microsoft.Extensions.Configuration.UserSecrets", "9.0.8");
            file.UpdateNugetReference("Microsoft.VisualStudio.Azure.Containers.Tools.Targets", "1.22.1");
            file.UpdateNugetReference("Swashbuckle.AspNetCore", "9.0.3");
            
            file.UpdateNugetReference("xunit.v3", "3.0.0");
            file.UpdateNugetReference("xunit.runner.visualstudio", "3.1.3");
            file.UpdateNugetReference("Selenium.WebDriver.ChromeDriver", "139.0.7258.6600");
            file.UpdateNugetReference("SixLabors.ImageSharp", "2.1.11");

        });

        uctx.ChangeCodeFile(@"package.json", file =>
        {
            file.ReplaceLine(a => a.Contains("@types/react"),
                """
                "@types/react": "19.1.8",
                """);

            file.ReplaceLine(a => a.Contains("@types/node"),
                """
                "@types/node": "22.13.17"
                """);

        });

        SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Remember to:");
        SafeConsole.WriteLineColor(ConsoleColor.DarkMagenta, "Yarn install");
        SafeConsole.WriteLineColor(ConsoleColor.DarkMagenta, "Delete bin, obj and ts_out in Framework and your projects");
        SafeConsole.WriteLineColor(ConsoleColor.DarkMagenta, "Compile again");
    }
}


