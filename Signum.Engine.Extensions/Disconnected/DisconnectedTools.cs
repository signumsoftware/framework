using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Engine.Maps;
using System.Data.Common;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;

namespace Signum.Engine.Disconnected
{
    public static class DisconnectedTools
    {
        public static void DropDatabase(DatabaseName databaseName)
        {
            Executor.ExecuteNonQuery(
@"ALTER DATABASE {0} SET single_user WITH ROLLBACK IMMEDIATE
DROP DATABASE {0}".Formato(databaseName));
        }

        public static void DropIfExists(DatabaseName databaseName)
        {
            string dropDatabase =
@"IF EXISTS(SELECT name FROM sys.databases WHERE name = '{0}')
BEGIN
     ALTER DATABASE {0} SET single_user WITH ROLLBACK IMMEDIATE
     DROP DATABASE {0}
END".Formato(databaseName);

            Executor.ExecuteNonQuery(dropDatabase);
        }

        public static void CreateDatabase(DatabaseName databaseName, string databaseFile, string databaseLogFile)
        {
            string script = @"CREATE DATABASE {0} ON  PRIMARY 
    ( NAME = N'{0}_Data', FILENAME = N'{1}' , SIZE = 167872KB , MAXSIZE = UNLIMITED, FILEGROWTH = 16384KB )
LOG ON 
    ( NAME = N'{0}_Log', FILENAME =  N'{2}' , SIZE = 2048KB , MAXSIZE = 2048GB , FILEGROWTH = 16384KB )".Formato(databaseName, databaseFile, databaseLogFile);

            Executor.ExecuteNonQuery(script);
        }

        public static void BackupDatabase(DatabaseName databaseName, string backupFile)
        {
            Executor.ExecuteNonQuery(@"BACKUP DATABASE {0} TO DISK = '{1}'WITH FORMAT".Formato(databaseName, backupFile));
        }

        public static void RestoreDatabase(DatabaseName databaseName, string backupFile, string databaseFile, string databaseLogFile)
        {
            DataTable dataTable = Executor.ExecuteDataTable("RESTORE FILELISTONLY FROM DISK ='{0}'".Formato(backupFile));


            string logicalDatabaseFile = dataTable.AsEnumerable().Single(a => a.Field<string>("Type") == "D").Field<string>("LogicalName");
            string logicalDatabaseLogFile = dataTable.AsEnumerable().Single(a => a.Field<string>("Type") == "L").Field<string>("LogicalName");

            new SqlPreCommandSimple(
@"RESTORE DATABASE {0}
                    from DISK = '{1}'
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


        public static void DeleteTable(ObjectName tableName)
        {
            Executor.ExecuteNonQuery("DELETE FROM {0}".Formato(tableName));
        }

        // TSQL Seed madness pseudo-code. 
        // 
        //int seed = 1; 
        //
        //IDENT_CURRENT() return seed; 
        //
        //DBCC CHECKIDENT() return empty()? null: seed;
        //
        //DBCC CHECKIDENT(newSeed) seed = newSeed;
        //
        //GetId()return empty() seed: seed++; 

        public static int? MaxIdInRange(ITable table, int seedMin, int seedMax)
        {
            var pb = Connector.Current.ParameterBuilder;

            int? max = (int?)Executor.ExecuteScalar("SELECT MAX(Id) FROM {0} WHERE @min <= Id AND Id < @max".Formato(table.Name), new List<DbParameter>
            {
                pb.CreateParameter("@min", seedMin, typeof(int)),
                pb.CreateParameter("@max", seedMax, typeof(int))
            });

            return max;
        }

        public static SeedInfo GetSeedInfo(ITable table)
        {
            using (Transaction tr = new Transaction())
            {
                string message = null;

                ((SqlConnection)Transaction.CurrentConnection).InfoMessage += (object sender, SqlInfoMessageEventArgs e) => { message = e.Message; };

                Executor.ExecuteNonQuery("DBCC CHECKIDENT ('{0}', NORESEED)".Formato(table.Name));

                if (message == null)
                    throw new InvalidOperationException("DBCC CHECKIDENT didn't write a message");
                 
                Match m = IdentityMessageRegex.Match(message);
                
                if (!m.Success)
                    throw new InvalidOperationException("DBCC CHECKIDENT messege has invalid format");

                SeedInfo result = new SeedInfo
                {
                    Identity = SeedInfo.Parse(m.Groups["identity"].Value),
                    Column = SeedInfo.Parse(m.Groups["column"].Value),
                };

                return tr.Commit(result);
            }
        }

        public static Regex IdentityMessageRegex = new Regex(@"Checking identity information: current identity value '(?<identity>.*)', current column value '(?<column>.*)'\.");

            

        static bool IsEmpty(ITable table)
        {
            return GetSeedInfo(table).Identity == null;
        }

        public static int GetNextId(ITable table)
        {
            var info = GetSeedInfo(table);

            if (info.Identity.HasValue)
                return info.Identity.Value + 1;

            return Executor.ExecuteNonQuery("SELECT IDENT_CURRENT ('{0}') ".Formato(table.Name));
        }

        public static void SetNextId(ITable table, int nextId)
        {
            var pb = Connector.Current.ParameterBuilder;

            Executor.ExecuteNonQuery("DBCC CHECKIDENT ('{0}', RESEED, @seed)".Formato(table.Name), new List<DbParameter>
            {
                pb.CreateParameter("@seed", IsEmpty(table) ? nextId :  nextId  -1, typeof(int))
            });
        }

        public static IDisposable SaveAndRestoreNextId(Table table)
        {
            int nextId = DisconnectedTools.GetNextId(table);

            return new Disposable(() =>
            {
                DisconnectedTools.SetNextId(table, nextId);
            });
        }

        public static IDisposable MeasureTime(this CancellationToken token, Action<int> action)
        {
            token.ThrowIfCancellationRequested();

            var t = PerfCounter.Ticks;

            return new Disposable(() =>
            {
                var elapsed = (PerfCounter.Ticks - t) / PerfCounter.FrequencyMilliseconds;

                action((int)elapsed);
            });
        }

        public static string CleanMachineName(string machineName)
        {
            return Regex.Replace(machineName, "[^A-Z0-9_]", "_");
        }
    }

    public struct SeedInfo
    {
        public int? Identity;
        public int? Column;

        internal static int? Parse(string num)
        {
            if (num == "NULL")
                return null;

            return int.Parse(num);
        }
    }
}
