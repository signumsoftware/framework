using Signum.Scheduler;
using System.ComponentModel;

namespace Signum.Authorization.WindowsAD;

public class UserWindowsADMixin : MixinEntity
{
    public static bool AllowPasswordForActiveDirectoryUsers = false;

    UserWindowsADMixin(ModifiableEntity mainEntity, MixinEntity? next)
        : base(mainEntity, next)
    {
    }

    [UniqueIndex]
    public string? SID { get; set; } //Windows Authentication

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(SID) && SID.HasText() && ((UserEntity)MainEntity).PasswordHash != null && !AllowPasswordForActiveDirectoryUsers)
            return WindowsADMessage.TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet.NiceToString(MainEntity);

        return base.PropertyValidation(pi);
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

public enum WindowsADMessage
{
    [Description("The user {0} is connected to Active Directory and can not have a local password set")]
    TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet,

    [Description("Login with Windows user")]
    LoginWithWindowsUser,
    [Description("No Windows user found")]
    NoWindowsUserFound,
    [Description("Looks like you windows user is not allowed to use this application, the browser is not providing identity information, or the server is not properly configured.")]
    LooksLikeYourWindowsUserIsNotAllowedToUseThisApplication,
}


[AutoInit]
public static class WindowsADTask
{
    public static SimpleTaskSymbol DeactivateUsers;
}
