using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Signum.Utilities;

public class ViteAssets
{
    public string MainJs { get; set; } = string.Empty;
    public HashSet<string> PreloadJs { get; set; } = new();
    public HashSet<string> Css { get; set; } = new();

    public static ViteAssets FromViteServerUrl(string mainJsUrl)
    {
        return new ViteAssets { MainJs = mainJsUrl };
    }

    public static ViteAssets FromManifestFile(string manifestFilePath, string mainEntry)
    {
        var content = System.IO.File.ReadAllText(manifestFilePath);
        var manifest = JsonDocument.Parse(content);

        if (!manifest.RootElement.TryGetProperty(mainEntry, out var entry))
            throw new InvalidOperationException($"Entry {mainEntry} not found in manifest.");

        var assets = new ViteAssets
        {
            MainJs = "~/dist/" + entry.GetProperty("file").GetString()
        };

        assets.CollectAssets(entry, manifest);

        return assets;
    }

    void CollectAssets(JsonElement entry, JsonDocument manifest)
    {
        if (entry.TryGetProperty("css", out var cssArray))
        {
            foreach (var css in cssArray.EnumerateArray())
            {
                this.Css.Add("~/dist/" + css.GetString());
            }
        }

        if (entry.TryGetProperty("imports", out var importsArray))
        {
            foreach (var imp in importsArray.EnumerateArray())
            {
                var importKey = imp.GetString();
                if (importKey == null) continue;

                if (manifest.RootElement.TryGetProperty(importKey, out var importedEntry))
                {
                    if (importedEntry.TryGetProperty("file", out var impFile))
                    {
                        this.PreloadJs.Add("~/dist/" + impFile.GetString());
                    }

                    this.CollectAssets(importedEntry, manifest);
                }
            }
        }
    }

    public HtmlString GetHtmlString(IUrlHelper urlHelper)
    {
        StringBuilder sb = new StringBuilder();
        if (this.Css.Any())
        {
            var css = this.Css.Select(c => urlHelper.Content(c)).ToArray();

            sb.AppendLine($$"""
                var cssList = {{JsonSerializer.Serialize(css)}};
                
                for (var css of cssList)
                {
                    var link = document.createElement('link');
                    link.rel = 'stylesheet';
                    link.href = css;
                    link.onerror = e => showError(new URIError(`The script ${ e.target.src } didn't load correctly.`));

                    document.head.appendChild(link);
                }

                """
                );
        }

        if (this.PreloadJs.Any())
        {
            var preload = this.PreloadJs.Select(c => urlHelper.Content(c)).ToArray();
            sb.AppendLine($$"""
                var preloadList = {{JsonSerializer.Serialize(preload)}};
                for (var pre of preloadList)
                {
                    var linkPreLoad = document.createElement('link');
                    linkPreLoad.rel = 'modulepreload';
                    linkPreLoad.href = pre;
                    linkPreLoad.onerror = e => showError(new URIError(`The script ${ e.target.src } didn't load correctly.`));

                    document.head.appendChild(linkPreLoad);
                }
                """);
        }

        sb.AppendLine($$"""
            var script = document.createElement('script');
            script.type = 'module';
            script.src = {{JsonSerializer.Serialize(urlHelper.Content(MainJs))}};
            script.onerror = e => showError(new URIError(`The script ${e.target.src} didn't load correctly.`));

            document.head.appendChild(script);
            """);


        return new HtmlString(sb.ToString());
    }

    public static HtmlString LoadViteReactRefresh(int vitePort)
    {
        return new HtmlString($$"""
            <script type="module" src="http://localhost:{{vitePort}}/dist/@vite/client"></script>
            <script type="module">
            	import RefreshRuntime from 'http://localhost:{{vitePort}}/dist/@react-refresh'
            	RefreshRuntime.injectIntoGlobalHook(window)
            	window.$RefreshReg$ = () => {}
            	window.$RefreshSig$ = () => (type) => type
            	window.__vite_plugin_react_preamble_installed__ = true
            </script>
            """);
    }
}
