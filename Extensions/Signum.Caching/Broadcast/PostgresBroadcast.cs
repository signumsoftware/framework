using Npgsql;
using System.Diagnostics;

namespace Signum.Cache.Broadcast;

public class PostgresBroadcast : IServerBroadcast
{
    public event Action<string, string>? Receive;

    public void Send(string methodName, string argument)
    {
        Executor.ExecuteNonQuery($"NOTIFY table_changed, '{methodName}/{Process.GetCurrentProcess().Id}/{argument}'");
    }

    public void Start()
    {
        Task.Run(() =>
        {
            try
            {
                var conn = (NpgsqlConnection)Connector.Current.CreateConnection();
                conn.Open();
                conn.Notification += (o, e) =>
                {
                    try
                    {
                        var methodName = e.Payload.Before('/');
                        var after = e.Payload.After("/");

                        var pid = int.Parse(after.Before("/"));
                        var arguments = after.After("/");

                        if (Process.GetCurrentProcess().Id != pid)
                            Receive?.Invoke(methodName, arguments);
                    }
                    catch (Exception ex)
                    {
                        ex.LogException(a => a.ControllerName = nameof(PostgresBroadcast));
                    }
                };

                using (var cmd = new NpgsqlCommand("LISTEN table_changed", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                while (true)
                {
                    conn.Wait();   // Thread will block here
                }
            }
            catch (Exception e)
            {
                e.LogException(a =>
                {
                    a.ControllerName = nameof(PostgresBroadcast);
                    a.ActionName = "Fatal";
                });
            }
        });
    }


    public override string ToString()
    {
        return $"{nameof(PostgresBroadcast)}()";
    }
}
