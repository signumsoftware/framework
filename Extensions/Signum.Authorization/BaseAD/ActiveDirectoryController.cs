using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Http;
using Signum.API.Filters;
using Signum.Authorization.ADGroups;

namespace Signum.Authorization.BaseAD;

[ValidateModelFilter]
public class ActiveDirectoryController : ControllerBase
{
    [HttpGet("api/findADUsers")]
    public Task<List<ActiveDirectoryUser>> FindADUsers(string subString, int count, CancellationToken token)
    {
        ActiveDirectoryPermission.InviteUsersFromAD.AssertAuthorized();

        return GetCustomAuthorizer().FindUser(subString, count, token);
    }

    [HttpPost("api/createADUser")]
    public Lite<UserEntity> CreateADUser([FromBody][Required] ActiveDirectoryUser user)
    {
        ActiveDirectoryPermission.InviteUsersFromAD.AssertAuthorized();

        return GetCustomAuthorizer().CreateADUser(user).ToLite();
    }

    private static ICustomAuthorizer GetCustomAuthorizer()
    {
        if (AuthLogic.Authorizer == null)
            throw new InvalidOperationException($"No Authorizer set in AuthLogic");

        return AuthLogic.Authorizer;
    }
}
