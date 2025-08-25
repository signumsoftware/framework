using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using static System.Net.Mime.MediaTypeNames;

namespace Signum.Utilities;
public class WebPackAssets
{
    public string MainJs { get; set; } = string.Empty;
    public HashSet<string> PreloadJs { get; set; } = new();
    public HashSet<string> Css { get; set; } = new();

    public static WebPackAssets FromViteServerUrl(string mainJsUrl)
    {
        return new WebPackAssets { MainJs = mainJsUrl };
    }

    public static WebPackAssets FromManifestFile(JsonDocument manifest, string entryFile = "main.tsx")
    {
        var assets = new WebPackAssets();



        if (!manifest.RootElement.TryGetProperty(entryFile, out var entry))
            throw new InvalidOperationException($"Entry {entryFile} not found in manifest.");

        // main.js
        if (entry.TryGetProperty("file", out var file))
        {
            assets.MainJs = "~/dist/" + file.GetString();
        }

        // collect css + imports recursively
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
            script.src = {{JsonSerializer.Serialize(MainJs)}};
            script.onerror = e => showError(new URIError(`The script ${e.target.src} didn't load correctly.`));

            document.head.appendChild(script);
            """);


        return new HtmlString(sb.ToString());
    }
}
