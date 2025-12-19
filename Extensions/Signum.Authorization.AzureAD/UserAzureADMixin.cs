using Signum.Authorization;
using Signum.Scheduler;
using System.ComponentModel;

namespace Signum.Authorization.AzureAD;

public class UserAzureADMixin : MixinEntity
{
    public static bool AllowPasswordForActiveDirectoryUsers = false;

    UserAzureADMixin(ModifiableEntity mainEntity, MixinEntity? next)
        : base(mainEntity, next)
    {
    }

    [UniqueIndex]
    public Guid? OID { get; set; }


    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(OID) && OID != null && ((UserEntity)this.MainEntity).PasswordHash != null && !AllowPasswordForActiveDirectoryUsers)
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
}

public enum UserOIDMessage
{
    [Description("The user {0} is connected to Active Directory and can not have a local password set")]
    TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet
}



[AutoInit]
public static class AzureADTask
{
    public static SimpleTaskSymbol DeactivateUsers;
}

