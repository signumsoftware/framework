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
using Signum.Utilities.DataStructures;

namespace Signum.Engine.Disconnected
{
    public class ImportManager
    {
        class UploadTable
        {
            public Type Type;
            public Table Table;
            public IDisconnectedStrategy Strategy;
        }

        public void Initialize()
        {
            var tables = Schema.Current.Tables.Values
                .Select(t => new UploadTable { Type = t.Type, Table = t, Strategy = DisconnectedLogic.GetStrategy(t.Type) })
                .Where(p => p.Strategy.Upload != Upload.None)
                .ToList();

            var dic = tables.ToDictionary(a => a.Table);

            DirectedGraph<Table> graph = DirectedGraph<Table>.Generate(
                dic.Keys,
                t => t.DependentTables().Select(a => a.Key).Where(tab => dic.ContainsKey(tab)));

            var feedback = graph.FeedbackEdgeSet();

            foreach (var edge in feedback.Edges)
            {
                var strategy = dic[edge.From].Strategy;

                if (strategy.DisableForeignKeys == null)
                    strategy.DisableForeignKeys = true;
            }

            foreach (var item in dic.Values)
            {
                item.Strategy.CreateDefaultImporter();
            }

            foreach (var item in dic.Values.Where(a => a.Strategy.DisableForeignKeys == null))
                item.Strategy.DisableForeignKeys = false;

            graph.RemoveAll(feedback.Edges);

            uploadTables = graph.CompilationOrder().Select(t => dic[t]).ToList();
        }

        List<UploadTable> uploadTables;


        class RunningImports
        {
            public Task Task;
            public CancellationTokenSource CancelationSource;
        }

        Dictionary<Lite<DisconnectedImportDN>, RunningImports> runningExports = new Dictionary<Lite<DisconnectedImportDN>, RunningImports>();

        public virtual Lite<DisconnectedImportDN> BeginImportDatabase(DisconnectedMachineDN machine, Stream file)
        {
            Lite<DisconnectedImportDN> import = new DisconnectedImportDN
            {
                Machine = machine.ToLite(),
                Copies = uploadTables.Select(t => new DisconnectedImportTableDN
                {
                    Type = t.Type.ToTypeDN().ToLite(),
                    DisableForeignKeys = t.Strategy.DisableForeignKeys.Value,
                }).ToMList()
            }.Save().ToLite();

            using (FileStream fs = File.OpenWrite(BackupNetworkFileName(machine, import)))
            {
                file.CopyTo(fs);
                file.Close();
            }

            var threadContext = Statics.ExportThreadContext();

            var cancelationSource = new CancellationTokenSource();

            var token = cancelationSource.Token;

            var task = Task.Factory.StartNew(()=>
            {
                Statics.ImportThreadContext(threadContext);

                try
                {
                    string connectionString;

                    using (Time(token, l => import.InDB().UnsafeUpdate(s => new DisconnectedImportDN { RestoreDatabase = l })))
                    {
                        DropDatabaseIfExists(machine);
                        connectionString = RestoreDatabase(machine, import);
                    }

                    var newDatabase = new SqlConnector(connectionString, Schema.Current, DynamicQueryManager.Current);
                    
                    using (Time(token, l => import.InDB().UnsafeUpdate(s => new DisconnectedImportDN { SynchronizeSchema = l })))
                    using (Connector.Override(newDatabase))
                    {
                        var script =  Administrator.TotalSynchronizeScript();

                        if (script != null)
                        {
                            string fileName = BackupNetworkFileName(machine, import) + ".sql";
                            script.Save(fileName);
                            throw new InvalidOperationException("The schema has changed since the last export. A schema sync script has been saved on: {0}".Formato(fileName));
                        }
                    }

                    try
                    {
                        using (Time(token, l => import.InDB().UnsafeUpdate(s => new DisconnectedImportDN { DisableForeignKeys = l })))
                            foreach (var item in uploadTables.Where(u => u.Strategy.DisableForeignKeys.Value))
                            {
                                DisableForeignKeys(item.Table);
                            }

                        foreach (var tuple in uploadTables)
                        {
                            ImportResult result = null;
                            using (Time(token, l => UpdateExportTable(import, tuple.Type.ToTypeDN(), () => new DisconnectedImportTableDN
                            {
                                CopyTable = l,
                                DisableForeignKeys = tuple.Strategy.DisableForeignKeys,
                                Inserted = result.Inserted,
                                Updated = result.Updated,
                            })))
                            {
                                result = tuple.Strategy.Importer.Import(machine, tuple.Table, tuple.Strategy, newDatabase);
                            }
                        }
                    }
                    finally
                    {
                        using (Time(token, l => import.InDB().UnsafeUpdate(s => new DisconnectedImportDN { EnableForeignKeys = l })))
                            foreach (var item in uploadTables.Where(u => u.Strategy.DisableForeignKeys.Value))
                            {
                                EnableForeignKeys(item.Table);
                            }
                    }

                    using (Time(token, l => import.InDB().UnsafeUpdate(s => new DisconnectedImportDN { DropDatabase = l })))
                        DropDatabase(newDatabase);

                    token.ThrowIfCancellationRequested();

                    import.InDB().UnsafeUpdate(s => new DisconnectedImportDN { State = DisconnectedImportState.Completed, Total = s.CalculateTotal() });
                }
                catch (Exception e)
                {
                    var ex = e.LogException();

                    import.InDB().UnsafeUpdate(s => new DisconnectedImportDN { Exception = ex.ToLite(), State = DisconnectedImportState.Error });
                }
                finally
                {
                    runningExports.Remove(import);
                }
            });


            runningExports.Add(import, new RunningImports { Task = task, CancelationSource = cancelationSource });

            return import;
        }

        private void DropDatabaseIfExists(DisconnectedMachineDN machine)
        {
            DisconnectedSql.DropIfExists(DatabaseName(machine));
        }

        private void DropDatabase(SqlConnector newDatabase)
        {
            DisconnectedSql.DropDatabase(newDatabase.DatabaseName());
        }

        
        protected virtual void EnableForeignKeys(Table table)
        {
            DisconnectedSql.EnableForeignKeys(table);

            foreach (var rt in table.RelationalTables())
                DisconnectedSql.EnableForeignKeys(rt);
        }

        protected virtual void DisableForeignKeys(Table table)
        {
            DisconnectedSql.DisableForeignKeys(table);

            foreach (var rt in table.RelationalTables())
                DisconnectedSql.DisableForeignKeys(rt);
        }
       
        protected virtual string BackupFileName(DisconnectedMachineDN machine, Lite<DisconnectedExportDN> statistics)
        {
            return "{0}.{1}.Import.{2}.bak".Formato(Connector.Current.DatabaseName(), machine.MachineName.ToString(), statistics.Id);
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

        private string RestoreDatabase(DisconnectedMachineDN machine, Lite<DisconnectedImportDN> import)
        {
            string backupFileName = Path.Combine(DisconnectedLogic.BackupFolder, BackupFileName(machine, import));

            string databaseName = DatabaseName(machine);

            DisconnectedSql.RestoreDatabase(databaseName,
                backupFileName,
                DatabaseFileName(machine),
                DatabaseLogFileName(machine));

            return ((SqlConnector)Connector.Current).ConnectionString.Replace(Connector.Current.DatabaseName(), databaseName);
        }

        protected virtual string DatabaseFileName(DisconnectedMachineDN machine)
        {
            return Path.Combine(DisconnectedLogic.DatabaseFolder, Connector.Current.DatabaseName() + "_Import_" + machine.MachineName + ".mdf");
        }

        protected virtual string DatabaseLogFileName(DisconnectedMachineDN machine)
        {
            return Path.Combine(DisconnectedLogic.DatabaseFolder, Connector.Current.DatabaseName() + "_Import_" + machine.MachineName + "_Log.ldf");
        }

        protected virtual string DatabaseName(DisconnectedMachineDN machine)
        {
            return Connector.Current.DatabaseName() + "_Import_" + machine.MachineName;
        }

        public virtual string BackupNetworkFileName(DisconnectedMachineDN machine, Lite<DisconnectedImportDN> import)
        {
            return Path.Combine(DisconnectedLogic.BackupNetworkFolder, BackupFileName(machine, import));
        }

        protected virtual string BackupFileName(DisconnectedMachineDN machine, Lite<DisconnectedImportDN> import)
        {
            return "{0}.{1}.Import.{2}.bak".Formato(Connector.Current.DatabaseName(), machine.MachineName.ToString(), import.Id);
        }

        protected virtual int UpdateExportTable(Lite<DisconnectedImportDN> stats, TypeDN type, Expression<Func<DisconnectedImportTableDN>> updater)
        {
            return Database.MListQuery((DisconnectedImportDN s) => s.Copies).Where(dst => dst.Parent.ToLite() == stats && dst.Element.Type.RefersTo(type))
                          .UnsafeUpdate(s => new MListElement<DisconnectedImportDN, DisconnectedImportTableDN> { Element = updater.Evaluate() });
        }
    }

    public interface ICustomImporter
    {
        ImportResult Import(DisconnectedMachineDN machine, Table table, IDisconnectedStrategy strategy, SqlConnector newDatabase);
    }

    public class BasicImporter<T> : ICustomImporter where T : IdentifiableEntity
    {
        public virtual ImportResult Import(DisconnectedMachineDN machine, Table table, IDisconnectedStrategy strategy, SqlConnector newDatabase)
        {
            int inserts = Insert(machine, table, strategy, newDatabase);

            int update = strategy.Upload == Upload.Subset ? Update(machine, table, strategy, newDatabase) : 0;

            return new ImportResult { Inserted = inserts, Updated = 0 };
        }

        protected virtual int Insert(DisconnectedMachineDN machine, Table table, IDisconnectedStrategy strategy, SqlConnector newDatabase)
        {
            var interval = GetNewIdsInterval(table, machine, newDatabase);

            if (interval == null)
                return 0;

            string prefix = "{0}.dbo.".Formato(newDatabase.DatabaseName().SqlScape());
            ParameterBuilder pb = Connector.Current.ParameterBuilder;

            string where = "\r\nWHERE @min <= [table].Id AND [table].Id < @max";

            int result;
            using (DisableIdentityRestoreSeed(table))
            {
                string command = @"INSERT INTO {0} ({1})
SELECT {2}
FROM {3} as [table]".Formato(
table.Name.SqlScape(),
table.Columns.Keys.ToString(a => a.SqlScape(), ", "),
table.Columns.Keys.ToString(a => "[table]." + a.SqlScape(), ", "),
prefix + table.Name.SqlScape());

                result = Executor.ExecuteNonQuery(command + where, new List<DbParameter>()
                {
                    pb.CreateParameter("@min", interval.Value.Min, typeof(int)),
                    pb.CreateParameter("@max", interval.Value.Max, typeof(int)),
                });
            }

            foreach (var rt in table.RelationalTables())
            {
                using (DisableIdentityRestoreSeed(rt))
                {
                    string command = @"INSERT INTO {0} ({1})
SELECT {2}
FROM {3} as [relationalTable]
JOIN {4} [table] on [relationalTable].{5} = [table].Id".Formato(
    rt.Name.SqlScape(),
    rt.Columns.Keys.ToString(a => a.SqlScape(), ", "),
    rt.Columns.Keys.ToString(a => "[relationalTable]." + a.SqlScape(), ", "),
    prefix + rt.Name.SqlScape(),
    prefix + table.Name.SqlScape(),
    rt.BackReference.Name.SqlScape());

                    Executor.ExecuteNonQuery(command + where, new List<DbParameter>()
                    {
                        pb.CreateParameter("@min", interval.Value.Min, typeof(int)),
                        pb.CreateParameter("@max", interval.Value.Max, typeof(int)),
                    });
                }
            }

            return result;
        }

        public static IDisposable DisableIdentityRestoreSeed(ITable table)
        {
            int currentSeed = DisconnectedSql.GetSeed(table);
            Executor.ExecuteNonQuery("SET IDENTITY_INSERT {0} ON\r\n".Formato(table.Name));

            return new Disposable(() =>
            {
                Executor.ExecuteNonQuery("SET IDENTITY_INSERT {0} OFF\r\n".Formato(table.Name));
                DisconnectedSql.SetSeed(table, currentSeed);
            });
        }

        public static Interval<int>? GetNewIdsInterval(Table table, DisconnectedMachineDN machine, SqlConnector newDatabase)
        {
            int? maxOther;
            using (Connector.Override(newDatabase))
                maxOther = DisconnectedSql.MaxIdInRange(table, machine.SeedMin, machine.SeedMax);

            if (maxOther == null)
                return null;

            int? max = DisconnectedSql.MaxIdInRange(table, machine.SeedMin, machine.SeedMax);
            if (max != null && max == maxOther)
                return null;

            return new Interval<int>(max.Value + 1, machine.SeedMax);
        }

        protected virtual int Update(DisconnectedMachineDN machine, Table table, IDisconnectedStrategy strategy, SqlConnector newDatabase)
        {
            int result = UpdateTable(table, newDatabase, machine);



            return result;
        }

        static PropertyInfo piDisconnectedMachine = ReflectionTools.GetPropertyInfo((IDisconnectedEntity de) => de.DisconnectedMachine);
        static PropertyInfo piTicks = ReflectionTools.GetPropertyInfo((IDisconnectedEntity de) => de.Ticks);
        static PropertyInfo piLastOnlineTicks = ReflectionTools.GetPropertyInfo((IDisconnectedEntity de) => de.LastOnlineTicks);

        protected virtual int UpdateTable(Table table, SqlConnector newDatabase, DisconnectedMachineDN machine)
        {
            string prefix = "{0}.dbo.".Formato(newDatabase.DatabaseName().SqlScape());

            string tableName = table.Name.SqlScape();

            var where = "\r\nWHERE [table].{0} = @machineId AND [table].{1} != [table].{2}".Formato(
                ((FieldReference)table.GetField(piDisconnectedMachine)).Name.SqlScape(),
                ((FieldValue)table.GetField(piTicks)).Name.SqlScape(),
                ((FieldValue)table.GetField(piLastOnlineTicks)).Name.SqlScape());

            ParameterBuilder pb = Connector.Current.ParameterBuilder;

            using (Transaction tr = new Transaction())
            {
                string command =
 @"UPDATE {0} SET
{2}
FROM {0}
INNER JOIN {1} as [table] ON {0}.id = [table].id
".Formato(
     table.Name.SqlScape(),
     prefix + table.Name.SqlScape(),
     table.Columns.Values.Where(c => !c.PrimaryKey).ToString(c => "   {0}.{1} = [table].{1}".Formato(tableName, c.Name), ",\r\n")) + where;

                int result = Executor.ExecuteNonQuery(command, new List<DbParameter> { pb.CreateParameter("@machineId", machine.Id, typeof(int)) });

                foreach (var rt in table.RelationalTables())
                {
                    using (DisableIdentityRestoreSeed(rt))
                    {
                        var delete = @"DELETE {0}
FROM {0}
INNER JOIN {1} as [table] ON {0}.{2} = [table].id".Formato(
                            rt.Name.SqlScape(),
                            prefix + table.Name.SqlScape(),
                            rt.BackReference.Name.SqlScape());

                        Executor.ExecuteNonQuery(delete + where, new List<DbParameter> { pb.CreateParameter("@machineId", machine.Id, typeof(int)) });

                        var insert = @"INSERT INTO {0} ({1})
SELECT {2}
FROM {3} as [relationalTable]
INNER JOIN {4} as [table] ON [relationalTable].{5} = [table].id".Formato(
                        rt.Name.SqlScape(),
                        rt.Columns.Keys.ToString(c => c.SqlScape(), ", "),
                        rt.Columns.Keys.ToString(a => "[relationalTable]." + a.SqlScape(), ", "),
                        prefix + rt.Name.SqlScape(),
                        prefix + table.Name.SqlScape(),
                        rt.BackReference.Name.SqlScape());

                        Executor.ExecuteNonQuery(insert + where, new List<DbParameter> { pb.CreateParameter("@machineId", machine.Id, typeof(int)) });
                    }
                }

                return tr.Commit(result);
            }
        }
    }


    public class ImportResult
    {
        public int Inserted;
        public int Updated;
    }
}
