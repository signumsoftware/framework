using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20210908_BlueGreenDeployments : CodeUpgradeBase
{
    public override string Description => "Blue Green Deployments";

    public override void Execute(UpgradeContext uctx)
    { 
        uctx.ChangeCodeFile(@"Southwind.React/Dockerfile", file =>
        {
            file.Replace("aspnet:5.0-buster-slim", "aspnet:5.0.9-buster-slim");
            file.Replace("sdk:5.0-buster-slim", "sdk:5.0.400-buster-slim");
        });

        uctx.ChangeCodeFile(@".dockerignore", file =>
        {
            file.InsertBeforeLastLine(a => a.Contains("LICENSE"), "**/appsettings.*.json");
        });

        uctx.ChangeCodeFile(@"Southwind.React/App/Layout.tsx", file =>
        {
            file.ReplaceLine(a => a.Contains("<VersionInfo />"), "<VersionInfo extraInformation={(window as any).__serverName} />");
        });

        uctx.ChangeCodeFile(@"Southwind.React/appsettings.json", file =>
        {
            file.InsertAfterFirstLine(a => a.Contains("StartBackgroundProcesses"), @"""ServerName"": ""local"",");
        });

        uctx.ChangeCodeFile(@"Southwind.React/appsettings.live.json", file =>
        {
            file.InsertAfterFirstLine(a => a.Contains("StartBackgroundProcesses"), @"""ServerName"": ""live"",");
        });

        uctx.ChangeCodeFile(@"Southwind.React/Views/Home/Index.cshtml", file =>
        {
            file.InsertAfterLastLine(a => a.Contains("@using"), @"@using Microsoft.Extensions.Configuration
@inject IConfiguration Configuration");

            if (file.Content.Contains("@Json.Serialize"))
                file.InsertBeforeFirstLine(a => a.Contains("@Json.Serialize"), @"var __serverName = @Json.Serialize(Configuration.GetValue<string>(""ServerName"", ""none""));");
            else
                file.InsertBeforeFirstLine(a => a.Contains("</script>"), @"    var __serverName = @Json.Serialize(Configuration.GetValue<string>(""ServerName"", ""none""));");
        });

        uctx.ChangeCodeFile(@"deployToAzure.ps1", file =>
        {
            var regex = new Regex(@"az webapp restart ((--name (?<appName>[a-zA-Z\-0-9]+) *)|(--resource-group (?<resourceGroup>[a-zA-Z\-0-9]+) *))*");

            if (!regex.IsMatch(file.Content))
                file.Warning($"'az webapp restart' not found!");

            file.Content =  regex.Replace(file.Content, m => $@"
$appName = '{m.Groups["appName"].Value.DefaultToNull() ?? SafeConsole.AskString("appName?")}'
$resourceGroup = '{m.Groups["resourceGroup"].Value.DefaultToNull() ?? SafeConsole.AskString("resourceGroup?")}'
$slotName = '{SafeConsole.AskString("slotName?")}'
$urlSlot = '{SafeConsole.AskString("urlSlot?")}'
$url = '{SafeConsole.AskString("liveUrl?")}'

Write-Host '# STOP slot' $slotName -ForegroundColor DarkRed
az webapp stop --resource-group $resourceGroup --name $appName --slot $slotName
.\Framework\Utils\CheckUrl.exe dead $urlSlot
Write-Host

Write-Host '# START slot' $slotName -ForegroundColor DarkGreen
az webapp start --resource-group $resourceGroup --name $appName --slot $slotName
.\Framework\Utils\CheckUrl.exe alive $urlSlot
Write-Host

Write-Host '# SWAP slots' $slotName '<-> production' -ForegroundColor Magenta
az webapp deployment slot swap --resource-group $resourceGroup --name $appName --slot $slotName
.\Framework\Utils\CheckUrl.exe alive $url
Write-Host

Write-Host '# STOP slot' $slotName -ForegroundColor DarkRed
az webapp stop --resource-group $resourceGroup --name $appName --slot $slotName
.\Framework\Utils\CheckUrl.exe dead $urlSlot
Write-Host

Write-Host '# START slot' $slotName -ForegroundColor DarkGreen
az webapp start --resource-group $resourceGroup --name $appName --slot $slotName
.\Framework\Utils\CheckUrl.exe alive $urlSlot
Write-Host");

        });
    }
}
