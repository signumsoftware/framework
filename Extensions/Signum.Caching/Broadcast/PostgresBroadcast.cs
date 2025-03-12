using Npgsql;
using System.Diagnostics;
using System.Reflection;

namespace Signum.Cache.Broadcast;

public class PostgresBroadcast : IServerBroadcast
{
    public event Action<string, string>? Receive;

    public void Send(string methodName, string argument)
    {
        Executor.ExecuteNonQuery($"NOTIFY signum_brodcast, '{methodName}/{Process.GetCurrentProcess().Id}/{argument}'");
    }

    public bool Running { get; private set; }
    object syncLock = new object();
    public void StartIfNecessary()
    {
        if (Running)
            return;

        Task.Factory.StartNew(() =>
        {
            lock (syncLock)
            {
                if(Running) 
                    return;

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

                    using (var cmd = new NpgsqlCommand("LISTEN signum_brodcast", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    Running = true;
                    while (true)
                    {
                        conn.Wait();   // Thread will block here
                    }
                }
                catch (Exception e)
                {
                    if (e is PostgresException pge && pge.SqlState == "57P01") //: terminating connection due to administrator command
                    {
                        Receive?.Invoke(CacheLogic.Method_InvalidateAllTable, "nodb");
                    }

                    e.LogException(a =>
                    {
                        a.ControllerName = nameof(PostgresBroadcast);
                        a.ActionName = "Fatal";
                    });
                }

                Running = false;
            }
        }, TaskCreationOptions.LongRunning);
    }


    public override string ToString()
    {
        return $"{nameof(PostgresBroadcast)}(Running = {Running})";
    }
}
