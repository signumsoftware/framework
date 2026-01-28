using System.Data.Common;
using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Signum.Engine.Sync;

namespace Signum.Disconnected;

public static class DisconnectedTools
{
    public static void DropDatabase(DatabaseName databaseName)
    {
        Executor.ExecuteNonQuery(
@"ALTER DATABASE {0} SET single_user WITH ROLLBACK IMMEDIATE
DROP DATABASE {0}".FormatWith(databaseName));
    }

    public static void DropIfExists(DatabaseName databaseName)
    {
        string dropDatabase =
@"IF EXISTS(SELECT name FROM sys.databases WHERE name = '{0}')
BEGIN
 ALTER DATABASE {0} SET single_user WITH ROLLBACK IMMEDIATE
 DROP DATABASE {0}
END".FormatWith(databaseName);

        Executor.ExecuteNonQuery(dropDatabase);
    }

    public static void CreateDatabase(DatabaseName databaseName, string databaseFile, string databaseLogFile)
    {
        string script = @"CREATE DATABASE {0} ON  PRIMARY 
( NAME = N'{0}_Data', FILENAME = N'{1}' , SIZE = 167872KB , MAXSIZE = UNLIMITED, FILEGROWTH = 16384KB )
LOG ON 
( NAME = N'{0}_Log', FILENAME =  N'{2}' , SIZE = 2048KB , MAXSIZE = 2048GB , FILEGROWTH = 16384KB )".FormatWith(databaseName, databaseFile, databaseLogFile);

        Executor.ExecuteNonQuery(script);
    }

    public static void BackupDatabase(DatabaseName databaseName, string backupFile)
    {
        Executor.ExecuteNonQuery(@"BACKUP DATABASE {0} TO DISK = '{1}'WITH FORMAT".FormatWith(databaseName, backupFile));
    }

    public static void RestoreDatabase(DatabaseName databaseName, string backupFile, string databaseFile, string databaseLogFile, bool replace = false)
    {
        DataTable dataTable = Executor.ExecuteDataTable("RESTORE FILELISTONLY FROM DISK ='{0}'".FormatWith(backupFile));

        string logicalDatabaseFile = (string)dataTable.Rows.Cast<DataRow>().Single(a => (string)a["Type"] == "D")["LogicalName"];
        string logicalDatabaseLogFile = (string)dataTable.Rows.Cast<DataRow>().Single(a => (string)a["Type"] == "L")["LogicalName"];

        new SqlPreCommandSimple(
@"RESTORE DATABASE {0}
                from DISK = '{1}'
WITH
MOVE '{2}' TO '{3}',
MOVE '{4}' TO '{5}'{6}".FormatWith(databaseName, backupFile,
                logicalDatabaseFile, databaseFile,
                logicalDatabaseLogFile, databaseLogFile, replace ? ",\nREPLACE" : "")).ExecuteNonQuery();
    }

    public static void DisableForeignKeys(ITable table)
    {
        Executor.ExecuteNonQuery("ALTER TABLE {0} NOCHECK CONSTRAINT all".FormatWith(table.Name));
    }

    public static void EnableForeignKeys(ITable table)
    {
        Executor.ExecuteNonQuery("ALTER TABLE {0} WITH CHECK CHECK CONSTRAINT all".FormatWith(table.Name));
    }


    public static void DeleteTable(ObjectName tableName)
    {
        Executor.ExecuteNonQuery("DELETE FROM {0}".FormatWith(tableName));
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

    public static long? MaxIdInRange(ITable table, long seedMin, long seedMax)
    {
        Type type = table.PrimaryKey.Type;

        var pb = Connector.Current.ParameterBuilder;

        object? obj = Executor.ExecuteScalar("SELECT MAX(Id) FROM {0} WHERE @min <= Id AND Id < @max".FormatWith(table.Name), new List<DbParameter>
        {
            pb.CreateParameter("@min", seedMin, type, default),
            pb.CreateParameter("@max", seedMax, type, default)
        });

        if (obj == null)
            return null;

        return Convert.ToInt64(obj);
    }

    public static SeedInfo GetSeedInfo(ITable table)
    {
        using (var tr = new Transaction())
        {
            string? message = null;

            ((SqlConnection)Transaction.CurrentConnection!).InfoMessage += (object sender, SqlInfoMessageEventArgs e) => { message = e.Message; };

            Executor.ExecuteNonQuery("DBCC CHECKIDENT ('{0}', NORESEED)".FormatWith(table.Name));

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

    public static Regex IdentityMessageRegex = new Regex(@".*'(?<identity>(\d*|NULL))'.*'(?<column>(\d*|NULL))'");

        

    static bool IsEmpty(ITable table)
    {
        return GetSeedInfo(table).Identity == null;
    }

    public static long GetNextId(ITable table)
    {
        var info = GetSeedInfo(table);

        if (info.Identity.HasValue)
            return info.Identity.Value + 1;

        return (long)(decimal)Executor.ExecuteScalar("SELECT IDENT_CURRENT ('{0}') ".FormatWith(table.Name))!;
    }

    public static void SetNextId(ITable table, long nextId)
    {
        SetNextIdSync(table, nextId).ExecuteNonQuery();
    }

    public static SqlPreCommandSimple SetNextIdSync(ITable table, long nextId)
    {
        var pb = Connector.Current.ParameterBuilder;

        return new SqlPreCommandSimple("DBCC CHECKIDENT ('{0}', RESEED, @seed)".FormatWith(table.Name), new List<DbParameter>
        {
            pb.CreateParameter("@seed", nextId,  table.PrimaryKey.Type, default)
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
