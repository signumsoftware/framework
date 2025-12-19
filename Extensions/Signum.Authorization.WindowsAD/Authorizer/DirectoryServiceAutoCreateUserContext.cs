using Signum.Authorization.BaseAD;
using System.DirectoryServices.AccountManagement;

#pragma warning disable CA1416 // Validate platform compatibility
namespace Signum.Authorization.WindowsAD.Authorizer;

public class DirectoryServiceAutoCreateUserContext : IAutoCreateUserContext
{
    public readonly PrincipalContext PrincipalContext;
    public string UserName { get; private set; }
    public string IdentityValue { get; private set; }
    public string? EmailAddress => GetUserPrincipal().EmailAddress != null ? GetUserPrincipal().EmailAddress : null;

    public string FirstName => GetUserPrincipal().GivenName;

    public string LastName => GetUserPrincipal().Surname;

    public Guid? OID => null;

    public string? SID => GetUserPrincipal().Sid.Value;

    public WindowsADConfigurationEmbedded Config {get; }

    BaseADConfigurationEmbedded IAutoCreateUserContext.Config => Config;

    UserPrincipal? userPrincipal;

    public DirectoryServiceAutoCreateUserContext(WindowsADConfigurationEmbedded config, PrincipalContext principalContext, string localName, string identityValue, UserPrincipal? userPrincipal = null)
    {
        this.Config = config;
        PrincipalContext = principalContext;
        UserName = localName;
        IdentityValue = identityValue;
        this.userPrincipal = userPrincipal;
    }

    public UserPrincipal GetUserPrincipal() //https://stackoverflow.com/questions/14278274/how-i-get-active-directory-user-properties-with-system-directoryservices-account
    {
        return userPrincipal ?? (userPrincipal = UserPrincipal.FindByIdentity(PrincipalContext, IdentityValue));
    }
}
