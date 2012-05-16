using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Engine.Maps;
using System.Data.Common;
using System.Data;

namespace Signum.Engine.Disconnected
{
    public static class DisconnectedSql
    {
        public static void DropDatabase(string databaseName)
        {
            Executor.ExecuteNonQuery(
@"ALTER DATABASE [{0}] SET single_user WITH ROLLBACK IMMEDIATE
DROP DATABASE {0}".Formato(databaseName.SqlScape()));
        }

        public static void DropIfExists(string databaseName)
        {
            string dropDatabase =
@"IF EXISTS(SELECT name FROM sys.databases WHERE name = '{0}')
BEGIN
     ALTER DATABASE [{0}] SET single_user WITH ROLLBACK IMMEDIATE
     DROP DATABASE [{0}]
END".Formato(databaseName);

            Executor.ExecuteNonQuery(dropDatabase);
        }

        public static void CreateDatabase(string databaseName, string databaseFile, string databaseLogFile)
        {
            string script = @"CREATE DATABASE [{0}] ON  PRIMARY 
    ( NAME = N'{0}_Data', FILENAME = N'{1}' , SIZE = 167872KB , MAXSIZE = UNLIMITED, FILEGROWTH = 16384KB )
LOG ON 
    ( NAME = N'{0}_Log', FILENAME =  N'{2}' , SIZE = 2048KB , MAXSIZE = 2048GB , FILEGROWTH = 16384KB )".Formato(databaseName, databaseFile, databaseLogFile);

            Executor.ExecuteNonQuery(script);
        }

        public static void BackupDatabase(string databaseName, string backupFile)
        {
            Executor.ExecuteNonQuery(@"BACKUP DATABASE {0} TO DISK = '{1}'WITH FORMAT".Formato(databaseName.SqlScape(), backupFile));
        }

        public static void RestoreDatabase(string databaseName, string backupFile, string databaseFile, string databaseLogFile)
        {
            DataTable dataTable = Executor.ExecuteDataTable("RESTORE FILELISTONLY FROM DISK ='{0}'".Formato(backupFile));

            string logicalDatabaseFile = dataTable.AsEnumerable().Single(a => a.Field<string>("Type") == "D").Field<string>("LogicalName");
            string logicalDatabaseLogFile = dataTable.AsEnumerable().Single(a => a.Field<string>("Type") == "L").Field<string>("LogicalName");

            new SqlPreCommandSimple(
@"RESTORE DATABASE {0}
FROM DISK = '{1}'
WITH MOVE '{2}' TO '{3}',
MOVE '{4}' TO '{5}'".Formato(databaseName, backupFile,
                    logicalDatabaseFile, databaseFile,
                    logicalDatabaseLogFile, databaseLogFile)).ExecuteNonQuery();
        }

        public static void DisableForeignKeys(ITable table)
        {
            Executor.ExecuteNonQuery("ALTER TABLE {0} NOCHECK CONSTRAINT all".Formato(table.Name));
        }

        public static void EnableForeignKeys(ITable table)
        {
            Executor.ExecuteNonQuery("ALTER TABLE {0} WITH CHECK CHECK CONSTRAINT all".Formato(table.Name));
        }

        public static int? MaxIdInRange(ITable table, int seedMin, int seedMax)
        {
            var pb = Connector.Current.ParameterBuilder;

            int? max = (int?)Executor.ExecuteNonQuery("SELECT MAX(Id) FROM {0} WHERE @min <= Id AND Id < @max".Formato(table.Name), new List<DbParameter>
            {
                pb.CreateParameter("@min", seedMin, typeof(int)),
                pb.CreateParameter("@max", seedMax, typeof(int))
            });

            return max;
        }

        public static int GetSeed(ITable table)
        {
            return (int)(decimal)Executor.ExecuteScalar("SELECT IDENT_CURRENT('{0}')".Formato(table.Name));
        }

        public static void SetSeed(ITable table, int newSeed)
        {
            var pb = Connector.Current.ParameterBuilder;

            Executor.ExecuteNonQuery("DBCC CHECKIDENT ({0}, RESEED, @seed)".Formato(table.Name), new List<DbParameter>
            {
                pb.CreateParameter("@seed", newSeed, typeof(int))
            });
        }
    }
}
