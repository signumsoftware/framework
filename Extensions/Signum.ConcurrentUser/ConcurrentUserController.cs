using Microsoft.AspNetCore.Mvc;
using Signum.Authorization;

namespace Signum.ConcurrentUser;

public class ConcurrentUserController : ControllerBase
{
    [HttpGet("api/concurrentUser/getUsers/{liteKey}")]
    public List<ConcurrentUserResponse> GetUsers(string liteKey)
    {
        using (AuthLogic.Disable())
            return Database.Query<ConcurrentUserEntity>().Where(c => c.TargetEntity.Is(Lite.Parse(liteKey))).Select(cu => new ConcurrentUserResponse
            {
                user = cu.User,
                startTime = cu.StartTime,
                connectionID = cu.SignalRConnectionID,
                isModified = cu.IsModified,
            }).ToList();
    }


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class ConcurrentUserResponse
    {
        public Lite<UserEntity> user;
        public DateTime startTime;
        public string connectionID;
        public bool isModified;
    }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
