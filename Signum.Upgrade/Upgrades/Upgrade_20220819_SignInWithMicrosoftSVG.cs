using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20220819_SignInWithMicrosoftSVG : CodeUpgradeBase
{
    public override string Description => "Replace signin_light.png -> signin_light.svg";

    public override void Execute(UpgradeContext uctx)
    {
        if (File.Exists(Path.Combine(uctx.ReactDirectory, @"wwwroot\signin_light.png")))
        {
            Console.WriteLine("File found signin_light.png");
            Console.WriteLine("Downloading signin_light.svg");
            HttpClient wc = new HttpClient();
            wc.DownloadFileTask(
                new Uri("https://docs.microsoft.com/en-us/azure/active-directory/develop/media/howto-add-branding-in-azure-ad-apps/ms-symbollockup_signin_light.svg"),
                Path.Combine(uctx.ReactDirectory, @"wwwroot\signin_light.svg")
            );

            File.Delete(Path.Combine(uctx.ReactDirectory, @"wwwroot\signin_light.png"));
        }
    }
}

public static class HttpClientUtils
{
    public static async void DownloadFileTask(this HttpClient client, Uri uri, string FileName)
    {
        using (var s = await client.GetStreamAsync(uri))
        {
            using (var fs = new FileStream(FileName, FileMode.CreateNew))
            {
                s.CopyTo(fs);
            }
        }
    }
}
