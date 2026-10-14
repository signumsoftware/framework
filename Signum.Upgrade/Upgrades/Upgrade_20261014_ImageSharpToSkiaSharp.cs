namespace Signum.Upgrade.Upgrades;

class Upgrade_20261014_ImageSharpToSkiaSharp : CodeUpgradeBase
{
    public override string Description => "Replace SixLabors.ImageSharp (vulnerable, commercial license from v3) by SkiaSharp in Signum.Word";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.csproj", file =>
        {
            if (file.Content.Contains("\"SixLabors.ImageSharp\""))
                file.RemoveNugetReference("SixLabors.ImageSharp");
        });

        uctx.ForeachCodeFile(@"*.cs", file =>
        {
            if (!file.Content.Contains("ImageSharpConverter") && !file.Content.Contains("SixLabors.ImageSharp"))
                return;

            file.Replace("ImageSharpConverter", "SkiaSharpConverter");
            file.Replace(new Regex(@"using SixLabors\.ImageSharp[\w\.]*;"), "");
            if (!file.Content.Contains("using SkiaSharp;"))
                file.InsertBeforeFirstLine(a => a.StartsWith("using "), "using SkiaSharp;");

            file.WarningLevel = WarningLevel.Warning;
            file.Warning($"Replace SixLabors.ImageSharp.Image by SKBitmap (e.g. Image.Load(stream) -> SKBitmap.Decode(stream)). SkiaSharpConverter only saves Png and Jpeg");
        });
    }
}
