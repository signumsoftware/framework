namespace Signum.Upgrade.Upgrades;

class Upgrade_20201124_FixImportUserAssets : CodeUpgradeBase
{
    public override string Description => "fix UserAssetsImporter";


    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile("Southwind.Terminal/SouthwindMigrations.cs", file =>
        {
            file.ReplaceLine(a => a.Contains("UserAssetsImporter.Import(bytes, preview);"),
@"using (UserHolder.UserSession(AuthLogic.SystemUser!))
UserAssetsImporter.Import(bytes, preview); ");
        });
    }
}
