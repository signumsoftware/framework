namespace Signum.Upgrade.Upgrades;

class Upgrade_20210927_ReplaceRefreshKey : CodeUpgradeBase
{
    public override string Description => "Replace refreshKey?:any with deps?: React.DependencyList";


    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile($@"*.tsx, *.ts", uctx.ReactDirectory, file =>
        {
            file.Replace(new Regex(@"refreshKey\s*=\s*{(?<val>(?:[^{}]|(?<Open>[{])|(?<-Open>[}]))+)}"),
                m => $@"deps={{[{m.Groups["val"].Value}]}}");
        });
    }        
}
