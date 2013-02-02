using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Engine;
using Signum.Test.Properties;
using System.IO;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities;
using Microsoft.SqlServer.Types;
using Signum.Engine.Operations;
using Signum.Engine.Basics;

namespace Signum.Test.Environment
{
    public static class Starter
    {
        static bool startedAndLoaded = false;
        public static void StartAndLoad()
        {
            if (!startedAndLoaded)
            {
                Start(UserConnections.Replace(Settings.Default.SignumTest));

                Administrator.TotalGeneration();

                Schema.Current.Initialize();

                MusicLoader.Load();

                startedAndLoaded = true;
            }
        }

        public static void Start(string connectionString)
        {
            DBMS dbms = DBMS.SqlServer2008;

            SchemaBuilder sb = new SchemaBuilder(dbms);
            DynamicQueryManager dqm = new DynamicQueryManager();
            if (dbms == DBMS.SqlCompact)
                Connector.Default = new SqlCeConnector(@"Data Source=C:\BaseDatos.sdf", sb.Schema, dqm);
            else
                Connector.Default = new SqlConnector(connectionString, sb.Schema, dqm);


            sb.Schema.Version = typeof(Starter).Assembly.GetName().Version;

            sb.Schema.Settings.OverrideAttributes((OperationLogDN ol) => ol.User, new ImplementedByAttribute());
            sb.Schema.Settings.OverrideAttributes((ExceptionDN e) => e.User, new ImplementedByAttribute());

            Validator.PropertyValidator((OperationLogDN e) => e.User).Validators.Clear();

            OperationLogic.Start(sb, dqm);
            ExceptionLogic.Start(sb, dqm);

            MusicLogic.Start(sb, dqm);
        }
    }
}
