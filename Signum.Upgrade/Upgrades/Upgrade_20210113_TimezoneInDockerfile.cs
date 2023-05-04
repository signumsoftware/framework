namespace Signum.Upgrade.Upgrades;

class Upgrade_20210113_TimezoneInDockerfile : CodeUpgradeBase
{
    public override string Description => "Add TimeZone info in Dockerfile";


    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile(@"Southwind.React/Dockerfile", file =>
        {
            file.ReplaceBetweenExcluded(a => a.Contains("WORKDIR /app"), a => a.Contains("EXPOSE 80"), 
@"ENV TZ=Europe/Berlin
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone");

            file.Replace("CNTK", "TensorFlow");
        });
    }
}
