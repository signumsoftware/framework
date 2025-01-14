using Microsoft.AspNetCore.SignalR;
using Signum.Authorization;

namespace Signum.ConcurrentUser;

public interface IConcurrentUserClient
{
    Task EntitySaved(string? newTicks);

    Task ConcurrentUsersChanged();
}

public class ConcurrentUserHub : Hub<IConcurrentUserClient>
{
    private static int _connectionCount = 0;

    public override Task OnConnectedAsync()
    {
        Interlocked.Increment(ref _connectionCount);
        System.Diagnostics.Debug.WriteLine("ConcurrentUserHub conectios {0}".FormatWith(_connectionCount));

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {

        Interlocked.Decrement(ref _connectionCount);
        System.Diagnostics.Debug.WriteLine("ConcurrentUserHub conectios {0}".FormatWith(_connectionCount));

        using (AuthLogic.Disable())
        {
            var entities = Database.Query<ConcurrentUserEntity>()
                .Where(a => a.SignalRConnectionID == this.Context.ConnectionId).Select(a => a.TargetEntity).ToList();

            Database.Query<ConcurrentUserEntity>().Where(a => a.SignalRConnectionID == this.Context.ConnectionId).UnsafeDelete();

            ConcurrentUserServer.UpdateConcurrentUsers(entities.Select(a => a.Key()).ToHashSet());
        }
        return base.OnDisconnectedAsync(exception);
    }

    public Task EnterEntity(string liteKey, string userKey)
    {
        var lite = Lite.Parse(liteKey);
        var user = (Lite<UserEntity>)Lite.Parse(userKey);
        using (AuthLogic.Disable())
        using (OperationLogic.AllowSave<ConcurrentUserEntity>())
        {
            new ConcurrentUserEntity
            {
                TargetEntity = lite,
                User = user,
                StartTime = Clock.Now,
                SignalRConnectionID = this.Context.ConnectionId,
            }.Save();
        };

        ConcurrentUserServer.UpdateConcurrentUsers(new HashSet<string> { liteKey });

        return this.Groups.AddToGroupAsync(this.Context.ConnectionId, liteKey);
    }

    public Task EntityModified(string liteKey, string userKey, bool modified)
    {
        var lite = Lite.Parse(liteKey);
        var user = (Lite<UserEntity>)Lite.Parse(userKey);
        using (AuthLogic.Disable())
        {
            Database.Query<ConcurrentUserEntity>()
                .Where(a => a.TargetEntity.Is(lite) && a.User.Is(user) && a.SignalRConnectionID == this.Context.ConnectionId)
                .UnsafeUpdate(a => a.IsModified, a => modified);
        };

        ConcurrentUserServer.UpdateConcurrentUsers(new HashSet<string> { liteKey });

        return Task.CompletedTask;
    }

    public Task ExitEntity(string liteKey, string userKey)
    {
        var lite = Lite.Parse(liteKey);
        var user = (Lite<UserEntity>)Lite.Parse(userKey);

        using (AuthLogic.Disable())
        {
            Database.Query<ConcurrentUserEntity>()
                .Where(a => a.TargetEntity.Is(lite) && a.User.Is(user) && a.SignalRConnectionID == this.Context.ConnectionId)
                .UnsafeDelete();

            if (Database.Query<ConcurrentUserEntity>().Any(a => a.TargetEntity.Is(lite) && a.User.Is(user) && a.SignalRConnectionID == this.Context.ConnectionId))
                return Task.CompletedTask;
        }

        ConcurrentUserServer.UpdateConcurrentUsers(new HashSet<string> { liteKey });

        return this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, liteKey);
    }
}
