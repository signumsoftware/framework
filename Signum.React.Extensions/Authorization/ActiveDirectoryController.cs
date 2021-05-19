using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Engine.Mailing;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.React.Filters;
using Signum.Services;
using Signum.Utilities;
using Signum.Engine.Basics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;



namespace Signum.React.Authorization
{
    [ValidateModelFilter]
    public class ActiveDirectoryController : ControllerBase
    {
        [HttpGet("api/findADUsers")]
        public Task<List<ActiveDirectoryUser>> FindADUsers(string subString, int count, CancellationToken token)
        {
            var config = ((ActiveDirectoryAuthorizer)AuthLogic.Authorizer!).GetConfig();
            if (config.Azure_ApplicationID.HasText())
                return MicrosoftGraphLogic.FindActiveDirectoryUsers(subString, count, token);

            if (config.DomainName.HasText())
                return ActiveDirectoryLogic.SearchUser(subString);

            throw new InvalidOperationException($"Neither {nameof(config.Azure_ApplicationID)} or {nameof(config.DomainName)} are set in {config.GetType().Name}");
        }


        [HttpPost("api/createADUser")]
        public Lite<UserEntity> CreateADUser([FromBody][Required] ActiveDirectoryUser user)
        {
            return MicrosoftGraphLogic.CreateUserFromAD(user).ToLite();
        }
    }
}
