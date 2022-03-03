using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Authorization;
using Signum.Entities.Authorization;
using Signum.React.Filters;
using System.Threading.Tasks;
using System.Threading;



namespace Signum.React.Authorization;

[ValidateModelFilter]
public class ActiveDirectoryController : ControllerBase
{
    [HttpGet("api/findADUsers")]
    public Task<List<ActiveDirectoryUser>> FindADUsers(string subString, int count, CancellationToken token)
    {
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
        var config = ((ActiveDirectoryAuthorizer)AuthLogic.Authorizer!).GetConfig();

        if (config.Azure_ApplicationID != null)
            return AzureADLogic.CreateUserFromAD(user).ToLite();

        if (config.DomainName.HasText())
            return ActiveDirectoryLogic.CreateUserFromAD(user).ToLite();

        throw new InvalidOperationException($"Neither {nameof(config.Azure_ApplicationID)} or {nameof(config.DomainName)} are set in {config.GetType().Name}");
    }

    [HttpPost("api/createADGroup")]
    public Lite<ADGroupEntity> CreateADUser([FromBody][Required] ADGroupRequest groupRequest)
    {
        var group = Database.Query<ADGroupEntity>().SingleOrDefault(a => a.Id == groupRequest.Id);
        if (group != null)
            return group.ToLite();

        group = new ADGroupEntity
        {
            DisplayName = groupRequest.DisplayName,
        }.SetId(groupRequest.Id);

        return group.Execute(ADGroupOperation.Save).ToLite();
    }

    public class ADGroupRequest
    {
        public Guid Id; 
        public string DisplayName;
    }
}
