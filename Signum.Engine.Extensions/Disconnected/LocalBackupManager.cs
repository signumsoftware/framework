using System.IO;
using Signum.Utilities;
using System.Data.SqlClient;
using Signum.Engine.Maps;

namespace Signum.Engine.Disconnected
{
    public class LocalBackupManager
    {
        public virtual void RestoreLocalDatabase(string connectionString, string backupFile, string databaseFile, string databaseLogFile)
        {
            FileTools.CreateParentDirectory(databaseFile);

            var csb = new SqlConnectionStringBuilder(connectionString);

            DatabaseName databaseName = new DatabaseName(null, csb.InitialCatalog);

            csb.InitialCatalog = "";

            using (SqlConnector.Override(new SqlConnector(csb.ToString(), null!, SqlServerVersion.SqlServer2012)))
            {
                DropIfExists(databaseName);

                RestoreDatabase(databaseName, backupFile, databaseFile, databaseLogFile);
            }
        }

        protected virtual void DropIfExists(DatabaseName databaseName)
        {
            DisconnectedTools.DropIfExists(databaseName);
        }

        protected virtual void RestoreDatabase(DatabaseName databaseName, string backupFile, string databaseFile, string databaseLogFile)
        {
            DisconnectedTools.RestoreDatabase(databaseName, 
                Absolutize(backupFile), 
                Absolutize(databaseFile), 
                Absolutize(databaseLogFile));
        }

        public virtual void BackupDatabase(DatabaseName databaseName, string backupFile)
        {
            DisconnectedTools.BackupDatabase(databaseName, Absolutize(backupFile));
        }

        protected virtual string Absolutize(string backupFile)
        {
            if (Path.IsPathRooted(backupFile))
                return backupFile;

            return Path.Combine(Directory.GetCurrentDirectory(), backupFile);
        }

        public void DropDatabase(DatabaseName databaseName)
        {
            DisconnectedTools.DropDatabase(databaseName);
        }
    }
}
