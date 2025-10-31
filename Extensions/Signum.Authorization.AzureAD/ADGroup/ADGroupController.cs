using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Http;
using Signum.API.Filters;
using Signum.Authorization.ADGroups;

namespace Signum.Authorization.AzureAD.ADGroup;

[ValidateModelFilter]
public class ADGroupController : ControllerBase
{
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

    public class ADGroupRequest
    {
        public Guid Id;
        public string DisplayName;
    }
}
