using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Engine;
using Signum.Engine.DynamicQuery;
using $custommessage$.Entities;

namespace $custommessage$.Logic
{

    //Starts-up the engine for $custommessage$ Entities, used by Web and Load Application
    public static class Starter
    {
        public static void Start(string connectionString)
        {
            SchemaBuilder sb = new SchemaBuilder();
            DynamicQueryManager dqm = new DynamicQueryManager();
            ConnectionScope.Default = new Connection(connectionString, sb.Schema, dqm);
            Schema.Current.MainAssembly = typeof(Starter).Assembly;

            MyEntityLogic.Start(sb, dqm);
        }
    }
}
