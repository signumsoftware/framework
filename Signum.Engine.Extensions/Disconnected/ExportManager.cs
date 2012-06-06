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
using System.Reflection;

namespace Signum.Engine.Disconnected
{
    public class ExportManager
    {
        public ExportManager()
        {
            this.miUnsafeLock = this.GetType().GetMethod("UnsafeLock", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        class DownloadTable
        {
            public Type Type;
            public Table Table;
            public IDisconnectedStrategy Strategy; 
        }

        public void Initialize()
        {
            downloadTables = Schema.Current.Tables.Values
                .Select(t => new DownloadTable { Type = t.Type, Table = t, Strategy = DisconnectedLogic.GetStrategy(t.Type) })
                .Where(p => p.Strategy.Download != Download.None)
                .ToList();
        }

        List<DownloadTable> downloadTables;

        Dictionary<Lite<DisconnectedExportDN>, RunningExports> runningExports = new Dictionary<Lite<DisconnectedExportDN>, RunningExports>();

        class RunningExports
        {
            public Task Task;
            public CancellationTokenSource CancelationSource;
        }

        public virtual Lite<DisconnectedExportDN> BeginExportDatabase(DisconnectedMachineDN machine)
        {
            Lite<DisconnectedExportDN> export = new DisconnectedExportDN
            {
                Machine = machine.ToLite(),
                Copies = downloadTables.Select(t => new DisconnectedExportTableDN
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
                    using (token.MeasureTime(l => export.InDB().UnsafeUpdate(s => new DisconnectedExportDN { Lock = l })))
                    {
                        foreach (var tuple in downloadTables)
                        {
                            token.ThrowIfCancellationRequested();

                            if (tuple.Strategy.Upload == Upload.Subset)
                                miUnsafeLock.MakeGenericMethod(tuple.Type).Invoke(this, new object[] { machine.ToLite(), tuple.Strategy, export });
                        }
                    }

                    string connectionString;
                    using (token.MeasureTime(l => export.InDB().UnsafeUpdate(s => new DisconnectedExportDN { CreateDatabase = l })))
                        connectionString = CreateDatabase(machine);

                    var newDatabase = new SqlConnector(connectionString, Schema.Current, DynamicQueryManager.Current);


                    using (token.MeasureTime(l => export.InDB().UnsafeUpdate(s => new DisconnectedExportDN { CreateSchema = l })))
                    using (Connector.Override(newDatabase))
                    {
                        Administrator.TotalGeneration();
                    }

                    using (token.MeasureTime(l => export.InDB().UnsafeUpdate(s => new DisconnectedExportDN { DisableForeignKeys = l })))
                    using (Connector.Override(newDatabase))
                    {
                        foreach (var tuple in downloadTables.Where(t => !t.Type.IsEnumProxy()))
                        {
                            token.ThrowIfCancellationRequested();

                            DisableForeignKeys(tuple.Table);
                        }
                    }

                    foreach (var tuple in downloadTables)
                    {
                        token.ThrowIfCancellationRequested();

                        using (token.MeasureTime(l => ExportTableQuery(export, tuple.Type.ToTypeDN()).UnsafeUpdate(e => 
                            new MListElement<DisconnectedExportDN, DisconnectedExportTableDN>
                            {
                                Element = { CopyTable = l }
                            })))
                        {
                            CopyTable(tuple.Table, tuple.Strategy, newDatabase);
                        }
                    }

                    using (token.MeasureTime(l => export.InDB().UnsafeUpdate(s => new DisconnectedExportDN { EnableForeignKeys = l })))
                        foreach (var tuple in downloadTables.Where(t => !t.Type.IsEnumProxy()))
                        {
                            token.ThrowIfCancellationRequested();

                            EnableForeignKeys(tuple.Table);
                        }

                    using (token.MeasureTime(l => export.InDB().UnsafeUpdate(s => new DisconnectedExportDN { ReseedIds = l })))
                    using (Connector.Override(newDatabase))
                    {
                        foreach (var table in Schema.Current.Tables.Values.Where(t => DisconnectedLogic.GetStrategy(t.Type).Upload != Upload.None))
                        {
                            token.ThrowIfCancellationRequested();

                            Reseed(machine, table);
                        }
                    }

                    using (token.MeasureTime(l => export.InDB().UnsafeUpdate(s => new DisconnectedExportDN { BackupDatabase = l })))
                        BackupDatabase(machine, export, newDatabase);

                    using (token.MeasureTime(l => export.InDB().UnsafeUpdate(s => new DisconnectedExportDN { DropDatabase = l })))
                        DropDatabase(newDatabase);

                    token.ThrowIfCancellationRequested();

                    export.InDB().UnsafeUpdate(s => new DisconnectedExportDN { State = DisconnectedExportState.Completed, Total = s.CalculateTotal() });

                    machine.IsOffline = true;
                    machine.Save();
                }
                catch (Exception e)
                {
                    var ex = e.LogException();

                    export.InDB().UnsafeUpdate(s => new DisconnectedExportDN { Exception = ex.ToLite(), State = DisconnectedExportState.Error });
                }
                finally
                {
                    runningExports.Remove(export);
                }
            });


            runningExports.Add(export, new RunningExports { Task = task, CancelationSource = cancelationSource });

            return export;
        }


        readonly MethodInfo miUnsafeLock;
        protected virtual int UnsafeLock<T>(Lite<DisconnectedMachineDN> machine, DisconnectedStrategy<T> strategy, Lite<DisconnectedExportDN> stats) where T : IdentifiableEntity, IDisconnectedEntity, new()
        {
            using (Schema.Current.GlobalMode())
            {
                var result = Database.Query<T>().Where(strategy.UploadSubset).Where(a => a.DisconnectedMachine != null).Select(a =>
                    "{0} locked in {1}".Formato(a.Id, a.DisconnectedMachine.Entity.MachineName)).ToString("\r\n");

                if (result.HasText())
                    ExportTableQuery(stats, typeof(T).ToTypeDN()).UnsafeUpdate(e =>
                            new MListElement<DisconnectedExportDN, DisconnectedExportTableDN>
                            {
                                Element = { Errors = result }
                            });

                return Database.Query<T>().Where(strategy.UploadSubset).UnsafeUpdate(a => new T { DisconnectedMachine = machine, LastOnlineTicks = a.Ticks });
            }
        }

        private IQueryable<MListElement<DisconnectedExportDN, DisconnectedExportTableDN>> ExportTableQuery(Lite<DisconnectedExportDN> stats, TypeDN type)
        {
            return Database.MListQuery((DisconnectedExportDN s) => s.Copies).Where(dst => dst.Parent.ToLite() == stats && dst.Element.Type.RefersTo(type));
        }

        public virtual void AbortExport(Lite<DisconnectedExportDN> stat)
        {
            runningExports.GetOrThrow(stat).CancelationSource.Cancel();
        }

        protected virtual void DropDatabase(Connector newDatabase)
        {
            DisconnectedTools.DropDatabase(newDatabase.DatabaseName());
        }

        protected virtual string DatabaseFileName(DisconnectedMachineDN machine)
        {
            return Path.Combine(DisconnectedLogic.DatabaseFolder, Connector.Current.DatabaseName() + "_Export_" + machine.MachineName + ".mdf");
        }

        protected virtual string DatabaseLogFileName(DisconnectedMachineDN machine)
        {
            return Path.Combine(DisconnectedLogic.DatabaseFolder, Connector.Current.DatabaseName() + "_Export_" + machine.MachineName + "_Log.ldf");
        }

        protected virtual string DatabaseName(DisconnectedMachineDN machine)
        {
            return Connector.Current.DatabaseName() + "_Export_" + machine.MachineName;
        }

        protected virtual string CreateDatabase(DisconnectedMachineDN machine)
        {
            string databaseName = DatabaseName(machine);

            DisconnectedTools.DropIfExists(databaseName);

            string fileName = DatabaseFileName(machine);
            string logFileName = DatabaseLogFileName(machine);

            DisconnectedTools.CreateDatabase(databaseName, fileName, logFileName);

            return ((SqlConnector)Connector.Current).ConnectionString.Replace(Connector.Current.DatabaseName(), databaseName);
        }

        protected virtual void EnableForeignKeys(Table table)
        {
            DisconnectedTools.EnableForeignKeys(table);

            foreach (var rt in table.RelationalTables())
                DisconnectedTools.EnableForeignKeys(rt);
        }
       
        protected virtual void DisableForeignKeys(Table table)
        {
            DisconnectedTools.DisableForeignKeys(table);

            foreach (var rt in table.RelationalTables())
                DisconnectedTools.DisableForeignKeys(rt);
        }

        protected virtual void Reseed(DisconnectedMachineDN machine, Table table)
        {
            int? max = DisconnectedTools.MaxIdInRange(table, machine.SeedMin, machine.SeedMax);

            DisconnectedTools.SetNextId(table, (max + 1) ?? machine.SeedMin);
        }


        protected virtual void BackupDatabase(DisconnectedMachineDN machine, Lite<DisconnectedExportDN> export, Connector newDatabase)
        {
            string backupFileName = Path.Combine(DisconnectedLogic.BackupFolder, BackupFileName(machine, export));

            DisconnectedTools.BackupDatabase(newDatabase.DatabaseName(), backupFileName);
        }

        public virtual string BackupNetworkFileName(DisconnectedMachineDN machine, Lite<DisconnectedExportDN> export)
        {
            return Path.Combine(DisconnectedLogic.BackupNetworkFolder, BackupFileName(machine, export));
        }

        protected virtual string BackupFileName(DisconnectedMachineDN machine, Lite<DisconnectedExportDN> export)
        {
            return "{0}.{1}.Export.{2}.bak".Formato(Connector.Current.DatabaseName(), machine.MachineName.ToString(), export.Id);
        }

        protected virtual void CopyTable(Table table, IDisconnectedStrategy strategy, Connector newDatabase)
        {
            var filter = strategy.Download == Download.All ? null : giGetWhere.GetInvoker(strategy.Type)(this, strategy);

            CopyTableBasic(table, newDatabase, filter);

            foreach (var rt in table.RelationalTables())
                CopyTableBasic(rt, newDatabase, filter == null? null: (SqlPreCommandSimple)filter.Clone());
        }


        protected virtual int CopyTableBasic(ITable table, Connector newDatabase, SqlPreCommandSimple filter)
        {
            string fullOuterName = "{0}.dbo.{1}".Formato(newDatabase.DatabaseName().SqlScape(), table.Name.SqlScape());

            string command =
@"INSERT INTO {0} ({2})
SELECT {3}
FROM {1} as [table]".Formato(
                fullOuterName,
                table.Name.SqlScape(),
                table.Columns.Keys.ToString(a => a.SqlScape(), ", "),
                table.Columns.Keys.ToString(a => "[table]." + a.SqlScape(), ", "));

            if (filter != null)
            {
                if (table is Table)
                {
                    command += "\r\nWHERE [table].Id in ({0})".Formato(filter.Sql);
                }
                else
                {
                    RelationalTable rt = (RelationalTable)table;
                    command += 
                        "\r\nJOIN {0} [masterTable] on [table].{1} = [masterTable].Id".Formato(rt.BackReference.ReferenceTable.Name.SqlScape(), rt.BackReference.Name.SqlScape()) +
                        "\r\nWHERE [masterTable].Id in ({0})".Formato(filter.Sql);
                }
            }

            string fullCommand =
                "SET IDENTITY_INSERT {0} ON\r\n".Formato(fullOuterName) +
                command +
                "SET IDENTITY_INSERT {0} OFF\r\n".Formato(fullOuterName);

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
