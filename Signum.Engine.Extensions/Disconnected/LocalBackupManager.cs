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
    public class LocalBackupManager
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
            DisconnectedSql.DropIfExists(databaseName);
        }

        protected virtual void RestoreDatabase(string databaseName, string backupFile, string databaseFile, string databaseLogFile)
        {
            DisconnectedSql.RestoreDatabase(databaseName, 
                Absolutize(backupFile), 
                Absolutize(databaseFile), 
                Absolutize(databaseLogFile));
        }

        public virtual void BackupDatabase(string databaseName, string backupFile)
        {
            DisconnectedSql.BackupDatabase(databaseName, Absolutize(backupFile));
        }

        protected virtual string Absolutize(string backupFile)
        {
            if (Path.IsPathRooted(backupFile))
                return backupFile;

            return Path.Combine(Directory.GetCurrentDirectory(), backupFile);
        }

        public void DropDatabase(string databaseName)
        {
            DisconnectedSql.DropDatabase(databaseName);
        }
    }
}
