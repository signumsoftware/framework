namespace Signum.Authorization.BaseAD;

public class BaseADConfigurationEmbedded : EmbeddedEntity
{
    public bool AllowMatchUsersBySimpleUserName { get; set; } = true;

    public bool AutoCreateUsers { get; set; }
    public bool AutoUpdateUsers { get; set; }

    [PreserveOrder, NoRepeatValidator]
    public MList<RoleMappingEmbedded> RoleMapping { get; set; } = new MList<RoleMappingEmbedded>();

    public Lite<RoleEntity>? DefaultRole { get; set; }
}


public class RoleMappingEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Max = 100)]
    public string ADNameOrGuid { get; set; }

    public Lite<RoleEntity> Role { get; set; }
}

public enum ActiveDirectoryAuthorizerMessage
{
    [Description("Active Directory user '{0}' is not associated with a user in this application.")]
    ActiveDirectoryUser0IsNotAssociatedWithAUserInThisApplication,
}

[AllowUnauthenticated]
public enum UserADMessage
{
    [Description("Find '{0}' in Active Directory")]
    Find0InActiveDirectory,
    [Description("Find in Active Directory")]
    FindInActiveDirectory,
    [Description("No user containing '{0}' found in Active Directory")]
    NoUserContaining0FoundInActiveDirectory,
    [Description("Select Active Directory User")]
    SelectActiveDirectoryUser,
    [Description("Please select the user from Active Directory that you want to import")]
    PleaseSelectTheUserFromActiveDirectoryThatYouWantToImport,
    [Description("Name or e-Mail")]
    NameOrEmail,
}



public enum ActiveDirectoryMessage
{
    Id,
    DisplayName,
    Mail,
    GivenName,
    Surname,
    JobTitle,
    OnPremisesImmutableId,
    CompanyName,
    AccountEnabled,
    OnPremisesExtensionAttributes,
    OnlyActiveUsers,
    InGroup,
    Description,
    SecurityEnabled,
    Visibility,
    HasUser,
}

[AutoInit]
public static class ActiveDirectoryPermission
{
    public static PermissionSymbol InviteUsersFromAD;
}
