using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Signum.Engine.Authorization;
using Signum.Engine.Cache;
using Signum.Engine.ConcurrentUser;
using Signum.Entities.ConcurrentUser;
using Signum.React.Facades;
using Signum.Utilities.Reflection;

namespace Signum.React.ConcurrentUser;

public static class ConcurrentUserServer
{
    public static IHubContext<ConcurrentUserHub, IConcurrentUserClient> ConcurrentUserHub { get; private set; } = null!;

    public static void Start(IApplicationBuilder app)
    {
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
        ReflectionServer.RegisterLike(typeof(ConcurrentUserMessage), () => true);
    }

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
