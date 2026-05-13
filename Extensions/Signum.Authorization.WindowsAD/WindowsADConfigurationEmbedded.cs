using Signum.Authorization.BaseAD;
using Signum.Scheduler;
using System.ComponentModel;

namespace Signum.Authorization.WindowsAD;

public class WindowsADConfigurationEmbedded : BaseADConfigurationEmbedded
{
    public bool LoginWithWindowsAuthenticator { get; set; }

    public bool LoginWithActiveDirectoryRegistry { get; set; }

    [StringLengthValidator(Max = 200)]
    public string? DomainName { get; set; }

    public string? DirectoryRegistry_Username { get; set; }

    [Format(FormatAttribute.Password)]
    public string? DirectoryRegistry_Password { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (LoginWithWindowsAuthenticator || LoginWithActiveDirectoryRegistry)
        {
            if (pi.Name == nameof(DomainName) && !DomainName.HasText())
                return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());
        }

        return base.PropertyValidation(pi);
    }
}

[AutoInit]
public static class WindowsADTask
{
    public static readonly SimpleTaskSymbol DeactivateUsers;
}

public enum WindowsADMessage
{
    [Description("The user {0} is connected to Active Directory and can not have a local password set")]
    TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet,
    [Description("Login with Windows user")]
    LoginWithWindowsUser,
    [Description("No Windows user found")]
    NoWindowsUserFound,
    [Description("Looks like your Windows user is not allowed to use this application")]
    LooksLikeYourWindowsUserIsNotAllowedToUseThisApplication,
}
