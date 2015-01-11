using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Migrations;
using Signum.Utilities;
using Signum.Utilities.DataStructures;

namespace Signum.Engine.Migrations
{
    public static class MigrationLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<SqlMigrationEntity>();

                dqm.RegisterQuery(typeof(SqlMigrationEntity), () =>
                    from e in Database.Query<SqlMigrationEntity>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.VersionNumber,
                    });


                sb.Include<CSharpMigrationEntity>();

                dqm.RegisterQuery(typeof(CSharpMigrationEntity), () =>
                    from e in Database.Query<CSharpMigrationEntity>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.UniqueName,
                        e.ExecutionDate,
                    });
            }
        }

        internal static void EnsureMigrationTable<T>() where T : Entity
        {
            using (Transaction tr = new Transaction())
            {
                if (Administrator.ExistTable<T>())
                    return;

                var table = Schema.Current.Table<T>();

                SqlBuilder.CreateTableSql(table).ExecuteNonQuery();

                foreach (var i in table.GeneratAllIndexes())
                {
                    SqlBuilder.CreateIndex(i).ExecuteLeaves();
                }

                SafeConsole.WriteLineColor(ConsoleColor.White, "Table " + table.Name + " auto-generated...");
         
                tr.Commit();
            }
        }
    }

    [Serializable]
    public class MigrationException : Exception
    {
        public MigrationException() { }
    }
}
