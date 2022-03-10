using Signum.Engine.Authorization;
using Microsoft.AspNetCore.Mvc;
using Signum.Entities.ConcurrentUser;

namespace Signum.React.ConcurrentUser;

public class ConcurrentUserController : ControllerBase
{
    [HttpGet("api/concurrentUser/getUsers/{liteKey}")]
    public List<ConcurrentUserEntity> GetUsers(string liteKey)
    {
        using (AuthLogic.Disable())
            return Database.Query<ConcurrentUserEntity>().Where(c => c.TargetEntity.Is(Lite.Parse(liteKey))).ToList();
    }

}
