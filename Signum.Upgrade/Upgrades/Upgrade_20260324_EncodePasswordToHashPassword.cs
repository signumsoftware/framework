namespace Signum.Upgrade.Upgrades;

class Upgrade_20260324_EncodePasswordToHashPassword : CodeUpgradeBase
{
    public override string Description => "Replace PasswordEncoding.EncodePassword by PasswordEncoding.HashPassword";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.cs", file =>
        {
            file.Replace("PasswordEncoding.EncodePassword", "PasswordEncoding.HashPassword");
        });
    }
}
