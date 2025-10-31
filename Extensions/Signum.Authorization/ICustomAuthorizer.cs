
namespace Signum.Authorization;

public interface ICustomAuthorizer
{
    UserEntity Login(string userName, string password, out string authenticationType);

    Task<List<ActiveDirectoryUser>> FindUser(string subString, int count, CancellationToken token);

    UserEntity CreateADUser(ActiveDirectoryUser user);
}

public interface IAutoCreateUserContext
{
    public string UserName { get; }
    public string? EmailAddress { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public Guid? OID { get; }
    public string? SID { get; }
}

public class ActiveDirectoryUser
{
    public required string DisplayName;
    public required string UPN;
    public required string JobTitle;
    public Guid? ObjectID;
    public string? SID;
}
