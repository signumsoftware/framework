using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Disconnected;
using Signum.Engine.Maps;
using Signum.Engine.Disconnected;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using System.IO;
using System.Data.Common;
using Signum.Engine.Linq;
using Signum.Utilities.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Linq.Expressions;
using Signum.Engine.Exceptions;
using Signum.Entities.Exceptions;
using Signum.Engine.Authorization;
using System.Threading;

namespace Signum.Engine.Disconnected
{
    public class ExportManager
    {
        class RunningExports
        {
            public Task Task;
            public CancellationTokenSource CancelationSource;
        }

        Dictionary<Lite<DownloadStatisticsDN>, RunningExports> runningExports = new Dictionary<Lite<DownloadStatisticsDN>, RunningExports>();

        public Lite<DownloadStatisticsDN> BeginExportDatabase(DisconnectedMachineDN machine)
        {
            var downloadTables = Schema.Current.Tables.Values
                .Select(t => new { Type = t.Type, Table = t, Strategy = DisconnectedLogic.GetStrategy(t.Type) })
                .Where(p => p.Strategy.Download != Download.None)
                .ToList();

            Lite<DownloadStatisticsDN> stat = new DownloadStatisticsDN
            {
                Machine = machine.ToLite(),
                Copies = downloadTables.Select(t => new DownloadStatisticsTableDN
                {
                    Type = t.Type.ToTypeDN().ToLite()
                }).ToMList()
            }.Save().ToLite();

            var threadContext = Statics.ExportThreadContext();

            var cancelationSource = new CancellationTokenSource();

            var token = cancelationSource.Token;

            var task = Task.Factory.StartNew(()=>
            {
                Statics.ImportThreadContext(threadContext);

                try
                {
                    using (Time(token, l => stat.InDB().UnsafeUpdate(s => new DownloadStatisticsDN { Unlock = l })))
                        DisconnectedLogic.UnsafeLock(machine.ToLite());

                    string connectionString;

                    using (Time(token, l => stat.InDB().UnsafeUpdate(s => new DownloadStatisticsDN { CreateDatabase = l })))
                        connectionString = CreateDatabase(machine);

                    var newDatabase = new SqlConnector(connectionString, Schema.Current, DynamicQueryManager.Current);


                    using (Time(token, l => stat.InDB().UnsafeUpdate(s => new DownloadStatisticsDN { CreateSchema = l })))
                    using (Connector.Override(newDatabase))
                    {
                        Administrator.TotalGeneration();
                    }

                    using (Time(token, l => stat.InDB().UnsafeUpdate(s => new DownloadStatisticsDN { DisableForeignKeys = l })))
                    using (Connector.Override(newDatabase))
                    {
                        foreach (var tuple in downloadTables.Where(t => !t.Type.IsEnumProxy()))
                        {
                            DisableForeignKey(tuple.Table);
                        }
                    }

                    var mlist = Database.MListQuery((DownloadStatisticsDN s) => s.Copies).Where(a => a.Parent.ToLite() == stat);

                    foreach (var tuple in downloadTables)
                    {
                        using (Time(token, l => mlist.Where(a => a.Element.Type.RefersTo(tuple.Type.ToTypeDN()))
                            .UnsafeUpdate(s => new MListElement<DownloadStatisticsDN, DownloadStatisticsTableDN> { Element = new DownloadStatisticsTableDN { CopyTable = l } })))
                        {
                            CopyDownload(tuple.Table, tuple.Strategy, newDatabase);
                        }
                    }

                    using (Time(token, l => stat.InDB().UnsafeUpdate(s => new DownloadStatisticsDN { EnableForeignKeys = l })))
                        foreach (var tuple in downloadTables.Where(t => !t.Type.IsEnumProxy()))
                        {
                            EnableForeignKey(tuple.Table);
                        }

                    using (Time(token, l => stat.InDB().UnsafeUpdate(s => new DownloadStatisticsDN { ReseedIds = l })))
                    using (Connector.Override(newDatabase))
                    {
                        foreach (var tuple in downloadTables)
                        {
                            if (tuple.Strategy.Upload != Upload.None)
                            {
                                Reseed(machine, tuple.Table);
                            }
                        }
                    }

                    using (Time(token, l => stat.InDB().UnsafeUpdate(s => new DownloadStatisticsDN { BackupDatabase = l })))
                        BackupDatabase(machine, stat, newDatabase);

                    using (Time(token, l => stat.InDB().UnsafeUpdate(s => new DownloadStatisticsDN { DropDatabase = l })))
                        DropDatabase(newDatabase);

                    token.ThrowIfCancellationRequested();

                    stat.InDB().UnsafeUpdate(s => new DownloadStatisticsDN { State = DownloadStatisticsState.Completed, Total = s.CalculateTotal() });
                }
                catch (Exception e)
                {
                    var ex = e.LogException();

                    stat.InDB().UnsafeUpdate(s => new DownloadStatisticsDN { Exception = ex.ToLite(), State = DownloadStatisticsState.Error });
                }

                runningExports.Remove(stat);
            });


            runningExports.Add(stat, new RunningExports { Task = task, CancelationSource = cancelationSource });

            return stat;
        }

        public void AbortExport(Lite<DownloadStatisticsDN> stat)
        {
            runningExports.GetOrThrow(stat).CancelationSource.Cancel();
        }

        static IDisposable Time(CancellationToken token, Action<long> action)
        {
            token.ThrowIfCancellationRequested();

            var t = PerfCounter.Ticks;

            return new Disposable(() =>
            {
                var elapsed = (PerfCounter.Ticks - t) / PerfCounter.FrequencyMilliseconds;

                action(elapsed);
            });
        }


        protected void DropDatabase(Connector newDatabase)
        {
            Executor.ExecuteNonQuery(
@"ALTER DATABASE [{0}] SET single_user WITH ROLLBACK IMMEDIATE
DROP DATABASE {0}".Formato(newDatabase.DatabaseName().SqlScape()));
        }

        protected virtual string DatabaseFolder(DisconnectedMachineDN machine)
        {
            return @"C:\Databases";
        }

        protected virtual string DatabaseFileName(DisconnectedMachineDN machine)
        {
            return Path.Combine(DatabaseFolder(machine), Connector.Current.DatabaseName() + "_" + machine.MachineName + ".mdf");
        }

        protected virtual string DatabaseLogFileName(DisconnectedMachineDN machine)
        {
            return Path.Combine(DatabaseFolder(machine), Connector.Current.DatabaseName() + "_" + machine.MachineName + "_Log.ldf");
        }

        protected virtual string DatabaseName(DisconnectedMachineDN machine)
        {
            return Connector.Current.DatabaseName() + "_" + machine.MachineName;
        }

        protected virtual string CreateDatabase(DisconnectedMachineDN machine)
        {
            string databaseName = DatabaseName(machine);

            string fileName = DatabaseFileName(machine);
            string logFileName = DatabaseLogFileName(machine);
            DropIfExists(databaseName);

            string script = @"CREATE DATABASE [{0}] ON  PRIMARY 
    ( NAME = N'{0}_Data', FILENAME = N'{1}' , SIZE = 167872KB , MAXSIZE = UNLIMITED, FILEGROWTH = 16384KB )
LOG ON 
    ( NAME = N'{0}_Log', FILENAME =  N'{2}' , SIZE = 2048KB , MAXSIZE = 2048GB , FILEGROWTH = 16384KB )".Formato(databaseName, fileName, logFileName);

            Executor.ExecuteNonQuery(script);

            return ((SqlConnector)Connector.Current).ConnectionString.Replace(Connector.Current.DatabaseName(), databaseName);

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

        protected virtual void EnableForeignKey(Table table)
        {
            EnableForeignKeyBasic(table);

            foreach (var rt in table.RelationalTables())
                EnableForeignKeyBasic(rt);
        }

        protected virtual void DisableForeignKeyBasic(ITable table)
        {
            Executor.ExecuteNonQuery("ALTER TABLE {0} NOCHECK CONSTRAINT all".Formato(table.Name));
        }
       
        protected virtual void DisableForeignKey(Table table)
        {
            DisableForeignKeyBasic(table);

            foreach (var rt in table.RelationalTables())
                DisableForeignKeyBasic(rt);
        }

        protected virtual void EnableForeignKeyBasic(ITable table)
        {
            Executor.ExecuteNonQuery("ALTER TABLE {0} WITH CHECK CHECK CONSTRAINT all".Formato(table.Name));
        }

        protected virtual void Reseed(DisconnectedMachineDN machine, Table table)
        {
            ReseedBasic(machine, table);

            foreach (var rt in table.RelationalTables())
                ReseedBasic(machine, rt);
        }

        protected virtual void ReseedBasic(DisconnectedMachineDN machine, ITable table)
        {
            var pb = Connector.Current.ParameterBuilder;

            int? max = (int?)Executor.ExecuteNonQuery("SELECT MAX(Id) FROM {0} WHERE @min <= Id AND Id < @max".Formato(table.Name), new List<DbParameter>
            {
                pb.CreateParameter("@min", machine.SeedMin, typeof(int)),
                pb.CreateParameter("@max", machine.SeedMax, typeof(int))
            });

            Executor.ExecuteNonQuery("DBCC CHECKIDENT ({0}, RESEED, @seed)".Formato(table.Name), new List<DbParameter>
            {
                pb.CreateParameter("@seed", max ?? machine.SeedMin, typeof(int))
            });
        }



        protected virtual void BackupDatabase(DisconnectedMachineDN machine, Lite<DownloadStatisticsDN> statistics, Connector newDatabase)
        {
            string backupFileName = BackupFileName(machine, statistics);

            Executor.ExecuteNonQuery(@"BACKUP DATABASE {0} TO DISK = '{1}'WITH FORMAT".Formato(newDatabase.DatabaseName().SqlScape(), backupFileName));
        }

        protected virtual string BackupFolder(DisconnectedMachineDN machine)
        {
            return @"C:\Backups";
        }

        public virtual string BackupFileName(DisconnectedMachineDN machine, Lite<DownloadStatisticsDN> statistics)
        {
            return Path.Combine(BackupFolder(machine),
                "{0}.{1}.{2}.bak".Formato(Connector.Current.DatabaseName(), machine.MachineName.ToString(), statistics.Id));
        }

        protected virtual void CopyDownload(Table table, IDisconnectedStrategy strategy, Connector newDatabase)
        {
            var filter = strategy.Download == Download.All ? null : giGetWhere.GetInvoker(strategy.Type)(this, strategy);

            CopyDownloadBasic(table, newDatabase, filter);

            foreach (var rt in table.RelationalTables())
                CopyDownloadBasic(rt, newDatabase, filter == null? null: (SqlPreCommandSimple)filter.Clone());
        }


        protected virtual int CopyDownloadBasic(ITable table, Connector newDatabase, SqlPreCommandSimple filter)
        {
            string fullOuterName = "{0}.dbo.{1}".Formato(newDatabase.DatabaseName().SqlScape(), table.Name.SqlScape());

            string command = 
@"INSERT INTO {0} ({2})
SELECT {3}
FROM {1} as [table]".Formato(
                fullOuterName, 
                table.Name.SqlScape(),
                table.Columns.Keys.ToString(a=>a.SqlScape(), ", "),
                table.Columns.Keys.ToString(a=>"[table]." + a.SqlScape(), ", "));

            if (filter != null)
            {
                if (table is Table)
                {
                    command += "\r\nWHERE [table].Id in ({0})".Formato(filter.Sql);
                }
                else
                {
                    RelationalTable rt = (RelationalTable)table;
                    command += @"
JOIN {0} [masterTable] on [table].{1} = [masterTable].Id
WHERE [masterTable].Id in ({2})".Formato(rt.BackReference.ReferenceTable.Name.SqlScape(), rt.BackReference.Name.SqlScape(), filter.Sql);
                }   
            }

            string fullCommand =
                "SET IDENTITY_INSERT {0} ON\r\n".Formato(fullOuterName) +
                command +
                "SET IDENTITY_INSERT {0} ON\r\n".Formato(fullOuterName);

            return Executor.ExecuteNonQuery(fullCommand, filter.TryCC(a => a.Parameters));
        }

        static readonly GenericInvoker<Func<ExportManager, IDisconnectedStrategy, SqlPreCommandSimple>> giGetWhere =
            new GenericInvoker<Func<ExportManager, IDisconnectedStrategy, SqlPreCommandSimple>>((em, ds) =>
                em.GetWhere<IdentifiableEntity>((DisconnectedStrategy<IdentifiableEntity>)ds));

        protected virtual SqlPreCommandSimple GetWhere<T>(DisconnectedStrategy<T> pair) where T : IdentifiableEntity
        {
            var query = Database.Query<T>().Where(pair.DownloadSubset).Select(a=>a.Id);

            return ((DbQueryProvider)query.Provider).GetMainPreCommand(query.Expression);
        }
    }
}
