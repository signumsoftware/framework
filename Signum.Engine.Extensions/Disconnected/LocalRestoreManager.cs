using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Signum.Utilities;
using System.Data.SqlClient;
using Signum.Entities.Disconnected;
using System.Data;

namespace Signum.Engine.Disconnected
{
    public class LocalRestoreManager
    {
        public virtual void RestoreLocalDatabase(string connectionString, string backupFile, string databaseFile, string databaseLogFile)
        {
            var csb = new SqlConnectionStringBuilder(connectionString);

            string databaseName = csb.InitialCatalog;

            csb.InitialCatalog = "";

            using (SqlConnector.Override(new SqlConnector(csb.ToString(), null, null)))
            {
                DropIfExists(databaseName);

                RestoreDatabase(databaseName, backupFile, databaseFile, databaseLogFile);
            }
        }

        protected virtual void DropIfExists(string databaseName)
        {
            string dropDatabase =
@"IF EXISTS(SELECT name FROM sys.databases WHERE name = '{0}')
BEGIN
     ALTER DATABASE [{0}] SET single_user WITH ROLLBACK IMMEDIATE
     DROP DATABASE [{0}]
END".Formato(databaseName);

            Executor.ExecuteNonQuery(dropDatabase);
        }

        protected virtual void RestoreDatabase(string databaseName, string backupFile, string databaseFile, string databaseLogFile)
        {
            DataTable dataTable = GetLogicalFileNames(backupFile);

            string logicalDatabaseFile = dataTable.AsEnumerable().Single(a => a.Field<string>("Type") == "D").Field<string>("LogicalName");
            string logicalDatabaseLogFile = dataTable.AsEnumerable().Single(a => a.Field<string>("Type") == "L").Field<string>("LogicalName");

            new SqlPreCommandSimple(
@"RESTORE DATABASE {0}
FROM DISK = '{1}'
WITH MOVE '{2}' TO '{3}',
MOVE '{4}' TO '{5}'".Formato(databaseName, Absolutize(backupFile),
                    logicalDatabaseFile, Absolutize(databaseFile),
                    logicalDatabaseLogFile, Absolutize(databaseLogFile))).ExecuteNonQuery();
        }

        protected virtual DataTable GetLogicalFileNames(string backupFile)
        {
            return Executor.ExecuteDataTable("RESTORE FILELISTONLY FROM DISK ='{0}'".Formato(Absolutize(backupFile)));
        }

        protected virtual string Absolutize(string backupFile)
        {
            if (Path.IsPathRooted(backupFile))
                return backupFile;

            return Path.Combine(Directory.GetCurrentDirectory(), backupFile);
        }
    }
}
