using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Authorization;
using Signum.Entities.Authorization;
using Signum.React.Filters;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace Signum.React.Authorization;

[ValidateModelFilter]
public class ActiveDirectoryController : ControllerBase
{
    [HttpGet("api/findADUsers")]
    public Task<List<ActiveDirectoryUser>> FindADUsers(string subString, int count, CancellationToken token)
    {
        ActiveDirectoryPermission.InviteUsersFromAD.AssertAuthorized();

        var config = ((ActiveDirectoryAuthorizer)AuthLogic.Authorizer!).GetConfig();
        if (config.Azure_ApplicationID != null)
            return AzureADLogic.FindActiveDirectoryUsers(subString, count, token);

        if (config.DomainName.HasText())
            return ActiveDirectoryLogic.SearchUser(subString);

        throw new InvalidOperationException($"Neither {nameof(config.Azure_ApplicationID)} or {nameof(config.DomainName)} are set in {config.GetType().Name}");
    }


    [HttpPost("api/createADUser")]
    public Lite<UserEntity> CreateADUser([FromBody][Required] ActiveDirectoryUser user)
    {
        ActiveDirectoryPermission.InviteUsersFromAD.AssertAuthorized();

        var config = ((ActiveDirectoryAuthorizer)AuthLogic.Authorizer!).GetConfig();

        if (config.Azure_ApplicationID != null)
            return AzureADLogic.CreateUserFromAD(user).ToLite();

        if (config.DomainName.HasText())
            return ActiveDirectoryLogic.CreateUserFromAD(user).ToLite();

        throw new InvalidOperationException($"Neither {nameof(config.Azure_ApplicationID)} or {nameof(config.DomainName)} are set in {config.GetType().Name}");
    }


    [HttpPost("api/createADGroup")]
    public Lite<ADGroupEntity> CreateADGroup([FromBody][Required] ADGroupRequest groupRequest)
    {
        ActiveDirectoryPermission.InviteUsersFromAD.AssertAuthorized();

        var group = Database.Query<ADGroupEntity>().SingleOrDefault(a => a.Id == groupRequest.Id);
        if (group != null)
            return group.ToLite();

        group = new ADGroupEntity
        {
            DisplayName = groupRequest.DisplayName,
        }.SetId(groupRequest.Id);

        return group.Execute(ADGroupOperation.Save).ToLite();
    }


    [HttpGet("api/thumbnailphoto/{username}"), SignumAllowAnonymous]
    public FileStreamResult? GetThumbnail(string username)
    {

        this.Response.GetTypedHeaders().CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
        {
            MaxAge = new TimeSpan(7, 0, 0, 0),
        };

        using (AuthLogic.Disable())
        {
            var byteArray = ActiveDirectoryLogic.GetProfilePicture(username);

            if (byteArray != null)
            {
                var memStream = new MemoryStream();

                memStream.Write(byteArray);
                memStream.Position = 0;

                var streamResult = new FileStreamResult(memStream, "image/jpeg");

                return streamResult;
            }

            return null;
        }
    }

    [HttpPost("api/getUserPhoto")]
    public FileStreamResult GetUserPhoto([FromBody][Required] UserPhotoRequest request)
    {
        if (request.OId != null)
        {
            var photo = AzureADLogic.GetUserPhoto(request.OId.Value).Result;
            if (photo != null)
            {
                photo.Position = 0;
                return new FileStreamResult(photo, "image/jpeg");
            }
        }

        if (request.SId != null)
        {

        }

        throw new InvalidOperationException(ValidationMessage._0Or1ShouldBeSet.NiceToString("OId", "SId"));
    }

    public class UserPhotoRequest
    {
        public Guid? OId { get; set; }

        public string? SId { get; set; }
    }

    public class ADGroupRequest
    {
        public Guid Id; 
        public string DisplayName;
    }
}
