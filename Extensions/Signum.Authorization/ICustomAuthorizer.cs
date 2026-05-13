

using Signum.Authorization.BaseAD;

namespace Signum.Authorization;

public interface ICustomAuthorizer
{
    UserEntity Login(string userName, string password, out string authenticationType);
}

public interface IDirectoryInviter
{
    Task<List<ExternalUser>> FindUser(string subString, int count, CancellationToken token);

    UserEntity CreateFromExternalUser(ExternalUser user);
}

public interface IAutoCreateUserContext
{
    public BaseADConfigurationEmbedded Config { get; }
    public string UserName { get; }
    public string? EmailAddress { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string? ExternalId { get; }
}

public class ExternalUser
{
    public required string DisplayName;
    public required string UPN;
    public required string JobTitle;
    public string? ExternalId;
}
