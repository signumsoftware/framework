using Signum.Authorization;
using System.ComponentModel;

namespace Signum.Authorization.ActiveDirectory;

public class UserADMixin : MixinEntity
{
    public static bool AllowPasswordForActiveDirectoryUsers = false;

    UserADMixin(ModifiableEntity mainEntity, MixinEntity? next)
        : base(mainEntity, next)
    {
    }

    [UniqueIndex]
    public Guid? OID { get; set; } //Azure authentication

    [UniqueIndex]
    public string? SID { get; set; } //Windows Authentication

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(OID) && OID != null && ((UserEntity)this.MainEntity).PasswordHash != null && !AllowPasswordForActiveDirectoryUsers)
            return UserOIDMessage.TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet.NiceToString(this.MainEntity);

        if (pi.Name == nameof(SID) && SID.HasText() && ((UserEntity)this.MainEntity).PasswordHash != null && !AllowPasswordForActiveDirectoryUsers)
            return UserOIDMessage.TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet.NiceToString(this.MainEntity);

        return base.PropertyValidation(pi);
    }

    public static Guid? CurrentOID
    {
        get
        {
            var oid = UserHolder.Current.GetClaim("OID");
            return oid is string s ? Guid.Parse(s) : oid is Guid g ? g : null;

        }
    }

    public static string? CurrentSID
    {
        get
        {
            var oid = UserHolder.Current.GetClaim("SID");
            return oid as string;

        }
    }
}

public enum UserOIDMessage
{
    [Description("The user {0} is connected to Active Directory and can not have a local password set")]
    TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet
}

[AutoInit]
public static class ActiveDirectoryPermission
{
    public static PermissionSymbol InviteUsersFromAD;
}
