namespace Signum.Upgrade.Upgrades;

class Upgrade_20220513_UserHolder : CodeUpgradeBase
{
    public override string Description => "Adapt to UserEntity.Current being Lite";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile("*.cs", file =>
        {
            file.Replace("UserEntity.Current.Role", "RoleEntity.Current");
            file.Replace("UserEntity.Current.ToLite()", "UserEntity.Current");
            file.Replace("UserEntity.Current.Mixin<UserADMixin>().OID", "UserADMixin.CurrentOID");
        });

        uctx.ChangeCodeFile("Southwind.React/Startup.cs", file =>
        {
            file.Replace("if (UserEntity.Current?.CultureInfo != null)", "if (UserEntity.CurrentUserCulture is { } ci)");
            file.Replace("return UserEntity.Current.CultureInfo.ToCultureInfo();", "return ci;");
            file.Replace("return UserEntity.Current.CultureInfo!.ToCultureInfo();", "return ci;");
        });
    }
}
