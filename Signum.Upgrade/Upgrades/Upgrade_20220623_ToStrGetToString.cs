namespace Signum.Upgrade.Upgrades;

class Upgrade_20220623_ToStrGetToString : CodeUpgradeBase
{
    public override string Description => "Replace a.toStr to getToString(a) in .ts/.tsx";

    static Regex Regex = new Regex(@"(?<expr>(::IDENT::)((\?|\!)?\.::IDENT::)*)(\?|\!)?\.toStr\b").WithMacros();

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.tsx, *.ts", file =>
        {
            file.Replace(Regex, m => "getToString(" + m.Groups["expr"] + ")");
        });
    }
}
