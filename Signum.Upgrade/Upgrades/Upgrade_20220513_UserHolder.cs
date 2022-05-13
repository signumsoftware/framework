namespace Signum.Upgrade.Upgrades;

class Upgrade_20220513_UserHolder : CodeUpgradeBase
{
    public override string Description => "Replace UserEntity.Current to Lite";

    public override void Execute(UpgradeContext uctx)
    {
    

        uctx.ForeachCodeFile("*.cs", file =>
        {
            file.Replace("UserEntity.Current.Role", "RoleEntity.Current");
            file.Replace("UserEntity.Current.ToLite()", "UserEntity.Current");
            file.Replace("UserEntity.Current.Mixin<UserADMixin>().OID", "UserADMixin.CurrentOID");
            file.Replace("UserEntity.Current?.CultureInfo", "UserEntity.CurrentUserCulture");
            file.Replace("UserEntity.Current.CultureInfo", "UserEntity.CurrentUserCulture");
        });
    }
}
