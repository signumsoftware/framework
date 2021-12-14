using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Signum.Entities.Basics;
using Signum.React.Alerts;
using Signum.React.Authorization;
using System.Threading.Tasks;

namespace Signum.React.Facades;

public interface IAlertsClient
{
    Task AlertsChanged();
}

public class AlertsHub : Hub<IAlertsClient>
{
    public override Task OnConnectedAsync()
    {
        var user = GetUser(Context.GetHttpContext()!);

        AlertsServer.Connections.Add(user.ToLite(), Context.ConnectionId);

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        AlertsServer.Connections.Remove(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    IUserEntity GetUser(HttpContext httpContext)
    {
        var tokenString = httpContext.Request.Query["access_token"];
        if (tokenString.Count > 1)
            throw new InvalidOperationException($"{tokenString.Count} values in 'access_token' query string found");

        if (tokenString.Count == 0)
        {
            tokenString = httpContext.Request.Headers["Authorization"];

            if (tokenString.Count != 1)
                throw new InvalidOperationException($"{tokenString.Count} values in 'Authorization' header found");
        }

        var token = AuthTokenServer.DeserializeToken(tokenString.SingleEx());

        return token.User;
    }
}

public class ConnectionMapping<T> where T : class
{
    private readonly Dictionary<T, HashSet<string>> userToConnection = new Dictionary<T, HashSet<string>>();
    private readonly Dictionary<string, T> connectionToUser = new Dictionary<string, T>();

    public int Count => userToConnection.Count;

    public void Add(T key, string connectionId)
    {
        lock (this)
        {
            HashSet<string>? connections;
            if (!userToConnection.TryGetValue(key, out connections))
            {
                connections = new HashSet<string>();
                userToConnection.Add(key, connections);
            }

            connections.Add(connectionId);

            connectionToUser.Add(connectionId, key);
        }
    }

    public IEnumerable<string> GetConnections(T key) => userToConnection.TryGetC(key) ?? Enumerable.Empty<string>();

    public void Remove(string connectionId)
    {
        lock (this)
        {
            var user = connectionToUser.TryGetC(connectionId);
            if (user != null)
            {
                HashSet<string>? connections = userToConnection.TryGetC(user);
                if (connections != null)
                {
                    connections.Remove(connectionId);
                    if (connections.Count == 0)
                        userToConnection.Remove(user);
                }

                connectionToUser.Remove(connectionId);
            }
        }
    }
}

