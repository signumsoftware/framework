using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Signum.Engine.Authorization;
using Signum.Engine.Cache;
using Signum.Engine.ConcurrentUser;
using Signum.Entities.Authorization;
using Signum.Entities.ConcurrentUser;
using Signum.Utilities.Reflection;

namespace Signum.React.ConcurrentUser;

public interface IConcurrentUserClient
{
    Task EntitySaved(string? newTicks);

    Task ConcurrentUsersChanged();
}

public class ConcurrentUserHub : Hub<IConcurrentUserClient>
{
    public override Task OnConnectedAsync()
    {        
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        using (AuthLogic.Disable())
        {
            var entities = Database.Query<ConcurrentUserEntity>()
                .Where(a => a.SignalRConnectionID == this.Context.ConnectionId).Select(a => a.TargetEntity).ToList();

            Database.Query<ConcurrentUserEntity>().Where(a => a.SignalRConnectionID == this.Context.ConnectionId).UnsafeDelete();

            ConcurrentUserSRServer.UpdateConcurrentUsers(entities.Select(a => a.Key()).ToHashSet());
        }
        return base.OnDisconnectedAsync(exception);
    }

    public Task EnterEntity(string liteKey, DateTime startTime, string userKey)
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
                StartTime = startTime,
                SignalRConnectionID = this.Context.ConnectionId,
            }.Save();
        };

        ConcurrentUserSRServer.UpdateConcurrentUsers(new HashSet<string> { liteKey });

        return this.Groups.AddToGroupAsync(this.Context.ConnectionId, liteKey);
    }

    public Task EntityModified(string liteKey, DateTime startTime, string userKey, bool modified)
    {
        var lite = Lite.Parse(liteKey);
        var user = (Lite<UserEntity>)Lite.Parse(userKey);
        using (AuthLogic.Disable())
        {
            Database.Query<ConcurrentUserEntity>()
                .Where(a => a.TargetEntity.Is(lite) && a.User.Is(user) && a.SignalRConnectionID == this.Context.ConnectionId && a.StartTime == startTime)
                .UnsafeUpdate(a => a.IsModified, a => modified);
        };

        ConcurrentUserSRServer.UpdateConcurrentUsers(new HashSet<string> { liteKey });

        return Task.CompletedTask;
    }

    public Task ExitEntity(string liteKey, DateTime startTime, string userKey)
    {
        var lite = Lite.Parse(liteKey);
        var user = (Lite<UserEntity>)Lite.Parse(userKey);

        using (AuthLogic.Disable())
        {
            Database.Query<ConcurrentUserEntity>()
                .Where(a => a.TargetEntity.Is(lite) && a.User.Is(user) && a.SignalRConnectionID == this.Context.ConnectionId && a.StartTime == startTime)
                .UnsafeDelete();

            if (Database.Query<ConcurrentUserEntity>().Any(a => a.TargetEntity.Is(lite) && a.User.Is(user) && a.SignalRConnectionID == this.Context.ConnectionId))
                return Task.CompletedTask;
        }

        ConcurrentUserSRServer.UpdateConcurrentUsers(new HashSet<string> { liteKey });

        return this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, liteKey);
    }
}


public static class ConcurrentUserSRServer
{
    public static IHubContext<ConcurrentUserHub, IConcurrentUserClient> ConcurrentUserHub { get; private set; } = null!;

    public static void MapConcurrentUserHub(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<ConcurrentUserHub>("/api/concurrentUserHub");
        ConcurrentUserHub = (IHubContext<ConcurrentUserHub, IConcurrentUserClient>)endpoints.ServiceProvider.GetService(
            typeof(IHubContext<ConcurrentUserHub, IConcurrentUserClient>))!;

        var s = Schema.Current;
        foreach (var type in Schema.Current.Tables.Keys.Where(t => ConcurrentUserLogic.WatchSaveFor(t)))
        {
            giAttachSchemaEvents.GetInvoker(type)(s);
        }

        CacheLogic.BroadcastReceivers.Add("ConcurrentUsersChanged", args =>
        {
            var liteKeys = args.Split("/").ToHashSet();

            NotifySignalRConcurrentUsersChanged(liteKeys);
        });

        CacheLogic.BroadcastReceivers.Add("EntitySaved", args =>
        {
            var ticks = args.Split("/").ToDictionary(a => Lite.Parse(a.Before("-")), a => a.After("-").ToLong());

            NotifySignalREntitySaved(ticks);
        });
    }

    static readonly GenericInvoker<Action<Schema>> giAttachSchemaEvents = new GenericInvoker<Action<Schema>>(s => AttachSchemaEvents<Entity>(s));
    public static void AttachSchemaEvents<T>(Schema schema)
        where T : Entity
    {
        schema.EntityEvents<T>().Saved += (e, args) => NotifyEntitySavedOnCommit(new Dictionary<Lite<Entity>, long?> { { e.ToLite(), e.Ticks } });
        schema.EntityEvents<T>().PreUnsafeDelete += (query) =>
        {
            var dic = query.Select(a => a.ToLite()).ToList().Distinct().ToDictionary(a => (Lite<Entity>)a, a => (long?)null);
            NotifyEntitySavedOnCommit(dic);
            return null;
        };
    }

    public static void BroadcastToServersConcurrentUsersChanged(HashSet<string> liteKeys)
    {
        if (CacheLogic.ServerBroadcast != null)
        {
            liteKeys.Chunk(100).ToList().ForEach(list =>
            {
                var liteKeysChunk = list.ToString("/");

                CacheLogic.ServerBroadcast!.Send("ConcurrentUsersChanged", liteKeysChunk);
            });
        }
    }

    public static void BroadcastToServersEntitySaved(Dictionary<Lite<Entity>, long?> newTicks)
    {
        if (CacheLogic.ServerBroadcast != null)
        {
            newTicks.Chunk(100).ToList().ForEach(list =>
            {
                var newKeyChunk = list.ToString(a => a.Key.ToString() + "-" + a.Value, "/");

                CacheLogic.ServerBroadcast!.Send("EntitySaved", newKeyChunk);
            });
        }
    }

    private static void NotifyEntitySavedOnCommit(Dictionary<Lite<Entity>, long?> newTicks)
    {
        var hs = (Dictionary<Lite<Entity>, long?>)Transaction.UserData.GetOrCreate("SavedEntities", new Dictionary<Lite<Entity>, long?>());
        hs.SetRange(newTicks);

        Transaction.PostRealCommit -= Transaction_PostRealCommit;
        Transaction.PostRealCommit += Transaction_PostRealCommit;
    }

    private static void Transaction_PostRealCommit(Dictionary<string, object> dic)
    {
        var newTicks = (Dictionary<Lite<Entity>, long?>)dic["SavedEntities"];

        BroadcastToServersEntitySaved(newTicks);
        NotifySignalREntitySaved(newTicks);
    }

    public static void UpdateConcurrentUsers(HashSet<string> liteKeys)
    {
        BroadcastToServersConcurrentUsersChanged(liteKeys);
        NotifySignalRConcurrentUsersChanged(liteKeys);
    }

    private static void NotifySignalREntitySaved(Dictionary<Lite<Entity>, long?> newTicks)
    {
        foreach (var kvp in newTicks)
        {
            try
            {
                ConcurrentUserHub.Clients.Group(kvp.Key.Key()).EntitySaved(kvp.Value.ToString());
            }
            catch (Exception ex)
            {
                ex.LogException();
            }
        }
    }

    private static void NotifySignalRConcurrentUsersChanged(HashSet<string> liteKeys)
    {
        foreach (var litekey in liteKeys)
        {
            try
            {
                ConcurrentUserHub.Clients.Group(litekey).ConcurrentUsersChanged();
            }
            catch (Exception ex)
            {
                ex.LogException();
            }
        }
    }
}
