using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Signum.API;
using Signum.Authorization;
using Signum.Cache;

namespace Signum.Alerts;

public static class AlertsServer
{
    internal static ConnectionMapping<Lite<IUserEntity>> Connections = null!;

    public static IHubContext<AlertsHub, IAlertsClient> AlertsHub { get; private set; }

    public static void Start(WebServerBuilder wsb)
    {
        if (wsb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        wsb.WebApplication.MapHub<AlertsHub>("/api/alertshub");
        Connections = new ConnectionMapping<Lite<IUserEntity>>();
        AlertsHub = wsb.WebApplication.Services.GetService<IHubContext<AlertsHub, IAlertsClient>>()!;

        var alertEvents = Schema.Current.EntityEvents<AlertEntity>();

        alertEvents.Saved += AlertEvents_Saved;
        alertEvents.PreUnsafeDelete += AlertEvents_PreUnsafeDelete;
        alertEvents.PreUnsafeUpdate += AlertEvents_PreUnsafeUpdate;
        alertEvents.PreUnsafeInsert += AlertEvents_PreUnsafeInsert;

        CacheLogic.BroadcastReceivers.Add("AlertForReceiver", args =>
        {
            if (args == "*")
                NotifySignalRClients(null);
            else
            {
                var users = args.Split("/").Select(a => (Lite<IUserEntity>)Lite.ParsePrimaryKey<UserEntity>(a)).ToHashSet();

                NotifySignalRClients(users);
            }
        });
    }

    public static int NotifyEverybodyLimit = 1000;
    public static int NotifyChunkSize = 100;

    public static void BroadcastToServers(HashSet<Lite<IUserEntity>> users)
    {
        if (CacheLogic.ServerBroadcast != null)
        {
            if (users.Count > NotifyEverybodyLimit)
            {
                CacheLogic.ServerBroadcast!.Send("AlertForReceiver", "*");
            }
            else
            {
                users.Chunk(NotifyChunkSize).ToList().ForEach(list =>
                {
                    var ids = list.ToString(a => a.Id.ToString(), "/");

                    CacheLogic.ServerBroadcast!.Send("AlertForReceiver", ids);
                });
            }
        }
    }

    private static IDisposable? AlertEvents_PreUnsafeUpdate(IUpdateable update, IQueryable<AlertEntity> entityQuery)
    {
        NotifyOnCommitQuery(entityQuery);
        return null;
    }

    private static LambdaExpression AlertEvents_PreUnsafeInsert(IQueryable query, LambdaExpression constructor, IQueryable<AlertEntity> entityQuery)
    {
        NotifyOnCommitQuery(entityQuery);
        return constructor;
    }

    private static IDisposable? AlertEvents_PreUnsafeDelete(IQueryable<AlertEntity> entityQuery)
    {
        NotifyOnCommitQuery(entityQuery);
        return null;
    }

    private static void AlertEvents_Saved(AlertEntity ident, SavedEventArgs args)
    {
        if (ident.Recipient != null)
            NotifyOnCommit(ident.Recipient);
    }

    private static void NotifyOnCommitQuery(IQueryable<AlertEntity> alerts)
    {
        var recipients = alerts.Where(a => a.Recipient != null && a.State == AlertState.Saved).Select(a => a.Recipient!).Distinct().ToArray();
        if (recipients.Any())
            NotifyOnCommit(recipients);
    }
    private static void NotifyOnCommit(params Lite<IUserEntity>[] recipients)
    {
        var hs = (HashSet<Lite<IUserEntity>>)Transaction.UserData.GetOrCreate("AlertRecipients", () => new HashSet<Lite<IUserEntity>>());
        hs.AddRange(recipients);

        Transaction.PostRealCommit -= Transaction_PostRealCommit;
        Transaction.PostRealCommit += Transaction_PostRealCommit;
    }

    private static void Transaction_PostRealCommit(Dictionary<string, object> dic)
    {
        var hashSet = (HashSet<Lite<IUserEntity>>)dic["AlertRecipients"];

        BroadcastToServers(hashSet);
        NotifySignalRClients(hashSet);
    }

    private static void NotifySignalRClients(HashSet<Lite<IUserEntity>>? hashSet)
    { 
        foreach (var user in hashSet ?? Connections.AllKeys())
        {
            foreach (var connectionId in Connections.GetConnections(user))
            {
                try
                {
                    AlertsServer.AlertsHub.Clients.Client(connectionId).AlertsChanged();
                }
                catch (Exception ex)
                {
                    ex.LogException();
                }
            }
        }
    }
}
