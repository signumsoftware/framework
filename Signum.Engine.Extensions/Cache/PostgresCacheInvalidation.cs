using Npgsql;
using Signum.Engine.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Cache
{
    public class PostgresCacheInvalidation : ICacheMultiServerInvalidator
    {
        public event Action<string>? ReceiveInvalidation;

        public void SendInvalidation(string cleanName)
        {

            Executor.ExecuteNonQuery($"NOTIFY table_changed, '{cleanName}/{Process.GetCurrentProcess().Id}'");
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
                        if (Process.GetCurrentProcess().Id != int.Parse(e.Payload.After("/")))
                            ReceiveInvalidation?.Invoke(e.Payload.Before("/"));
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
                    e.LogException();
                }
            });
        }
    }
}
