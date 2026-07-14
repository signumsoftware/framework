using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;

namespace Signum.Authorization.BaseAD;

[ValidateModelFilter]
public class ActiveDirectoryController : ControllerBase
{
    [HttpGet("api/findADUsers")]
    public Task<List<ExternalUser>> FindADUsers(string subString, int count, CancellationToken token)
    {
        ActiveDirectoryPermission.InviteUsersFromAD.AssertAuthorized();

        return GetDirectoryInviter().FindUser(subString, count, token);
    }

    [HttpPost("api/createADUser")]
    public Lite<UserEntity> CreateADUser([FromBody][Required] ExternalUser user)
    {
        ActiveDirectoryPermission.InviteUsersFromAD.AssertAuthorized();

        return GetDirectoryInviter().CreateFromExternalUser(user).ToLite();
    }

    private static IDirectoryInviter GetDirectoryInviter()
    {
        if (AuthLogic.Authorizer == null)
            throw new InvalidOperationException("No Authorizer set in AuthLogic");

        if (AuthLogic.Authorizer is not IDirectoryInviter inviter)
            throw new InvalidOperationException($"{AuthLogic.Authorizer.GetType().Name} does not support inviting users from a directory");

        return inviter;
    }
}
