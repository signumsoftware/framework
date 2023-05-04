namespace Signum.Upgrade.Upgrades;

class Upgrade_20220131_SimplifyExpressionRegister : CodeUpgradeBase
{
    public override string Description => "Update Nugets";

    public override void Execute(UpgradeContext uctx)
    {
        var regex = new Regex(@"QueryLogic\.Expressions\.Register\((?<lambda>\([^)]*\) *=> *[^,]*), \(\) *=> *(?<message>\w+Message\.\w+)\.NiceToString\(\)\)");

        uctx.ForeachCodeFile(@"*.cs", file =>
        {
            file.Replace(regex, match => $"QueryLogic.Expressions.Register({match.Groups["lambda"]}, {match.Groups["message"]})");
        });
    }
}
