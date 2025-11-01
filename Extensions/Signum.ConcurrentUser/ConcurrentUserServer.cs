using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Signum.API;
using Signum.Cache;
using Signum.Utilities.Reflection;
using System.Diagnostics;

namespace Signum.ConcurrentUser;

public static class ConcurrentUserServer
{
    public static string? DisableSignalR;

    public static IHubContext<ConcurrentUserHub, IConcurrentUserClient> ConcurrentUserHub { get; private set; } = null!;

    public static void Start(WebServerBuilder wsb)
    {
        if (wsb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        ReflectionServer.RegisterLike(typeof(ConcurrentUserMessage), () => true);

        wsb.WebApplication.MapHub<ConcurrentUserHub>("/api/concurrentUserHub");
        ConcurrentUserHub = wsb.WebApplication.Services.GetService<IHubContext<ConcurrentUserHub, IConcurrentUserClient>>()!;

        var s = Schema.Current;
        Schema.Current.SchemaCompleted += () =>
        {
            foreach (var type in Schema.Current.Tables.Keys.Where(t => ConcurrentUserLogic.WatchSaveFor(t)))
            {
                giAttachSchemaEvents.GetInvoker(type)(s);
            }
        };

        CacheLogic.BroadcastReceivers.Add("ConcurrentUsersChanged", args =>
        {
            var liteKeys = args.Split("/").ToHashSet();

            NotifySignalRConcurrentUsersChanged(liteKeys);
        });

        CacheLogic.BroadcastReceivers.Add("EntitySaved", args =>
        {
            var ticks = args.Split("/").ToDictionary(a => Lite.Parse(a.Before("|")), a => a.After("|").ToLong());

            NotifySignalREntitySaved(ticks);
        });


        //No easy way to check if its a Server OS https://stackoverflow.com/questions/2819934/detect-windows-version-in-net
        //so instead we check for DEBUG compilation
#if DEBUG 
        if (Process.GetCurrentProcess().ProcessName == "w3wp") /*ISS*/
            DisableSignalR = @"SignalR is disabled to prevent IIS connection limit on Windows Client OS. 
Consider using IIS Express.
More info: https://docs.microsoft.com/en-us/iis/troubleshoot/request-restrictions";
#endif
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
                var newKeyChunk = list.ToString(a => a.Key.Key() + "|" + a.Value, "/");

                CacheLogic.ServerBroadcast!.Send("EntitySaved", newKeyChunk);
            });
        }
    }

    private static void NotifyEntitySavedOnCommit(Dictionary<Lite<Entity>, long?> newTicks)
    {
        var hs = (Dictionary<Lite<Entity>, long?>)Transaction.UserData.GetOrCreate("SavedEntities", () => new Dictionary<Lite<Entity>, long?>());
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
                ConcurrentUserHub.Clients.Group(kvp.Key.Key()).EntitySaved(kvp.Key.Key(), kvp.Value.ToString());
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
