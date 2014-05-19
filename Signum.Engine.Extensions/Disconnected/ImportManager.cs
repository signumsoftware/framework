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
using Signum.Engine.Authorization;
using System.Threading;
using System.Reflection;
using Signum.Utilities.DataStructures;
using Signum.Engine.Operations;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Engine.Cache;

namespace Signum.Engine.Disconnected
{
    public class ImportManager
    {
        static object SyncLock = new object();

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

            foreach (var item in dic.Values.Where(a => a.Strategy.DisableForeignKeys == null))
                item.Strategy.DisableForeignKeys = false;

            graph.RemoveEdges(feedback.Edges);

            uploadTables = graph.CompilationOrder().Select(t => dic[t]).ToList();
        }

        List<UploadTable> uploadTables;


        class RunningImports
        {
            public Task Task;
            public CancellationTokenSource CancelationSource;
        }

        Dictionary<Lite<DisconnectedImportDN>, RunningImports> runningExports = new Dictionary<Lite<DisconnectedImportDN>, RunningImports>();

        public virtual Lite<DisconnectedImportDN> BeginImportDatabase(DisconnectedMachineDN machine, Stream file = null)
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

            if (file != null)
                using (FileStream fs = File.OpenWrite(BackupNetworkFileName(machine, import)))
                {
                    file.CopyTo(fs);
                    file.Close();
                }

            var threadContext = Statics.ExportThreadContext();

            var cancelationSource = new CancellationTokenSource();

            var user = UserDN.Current;

            var token = cancelationSource.Token;

            var task = Task.Factory.StartNew(() =>
            {
                lock (SyncLock)
                    using (AuthLogic.UserSession(user))
                    {
                        OnStartImporting(machine);

                        DisconnectedMachineDN.Current = machine.ToLite();

                        try
                        {
                            if (file != null)
                            {
                                using (token.MeasureTime(l => import.InDB().UnsafeUpdate().Set(s=>s.RestoreDatabase, s=>l).Execute()))
                                {
                                    DropDatabaseIfExists(machine);
                                    RestoreDatabase(machine, import);
                                }
                            }

                            string connectionString = GetImportConnectionString(machine);

                            var newDatabase = new SqlConnector(connectionString, Schema.Current, DynamicQueryManager.Current);

                            using (token.MeasureTime(l => import.InDB().UnsafeUpdate().Set(s => s.SynchronizeSchema, s => l).Execute()))
                            using (Connector.Override(newDatabase))
                            using (ObjectName.OverrideOptions(new ObjectNameOptions { AvoidDatabaseName = true }))
                            using (ExecutionMode.DisableCache())
                            using (ExecutionMode.SynchronizeSchemaOnly())
                            {
                                var script = Administrator.TotalSynchronizeScript(interactive: false);

                                if (script != null)
                                {
                                    string fileName = BackupNetworkFileName(machine, import) + ".sql";
                                    script.Save(fileName);
                                    throw new InvalidOperationException("The schema has changed since the last export. A schema sync script has been saved on: {0}".Formato(fileName));
                                }
                            }

                            try
                            {
                                using (token.MeasureTime(l => import.InDB().UnsafeUpdate().Set(s => s.DisableForeignKeys, s => l).Execute()))
                                    foreach (var item in uploadTables.Where(u => u.Strategy.DisableForeignKeys.Value))
                                    {
                                        DisableForeignKeys(item.Table);
                                    }

                                foreach (var tuple in uploadTables)
                                {
                                    ImportResult result = null;
                                    using (token.MeasureTime(l =>
                                    {
                                        if (result != null)
                                            import.MListElementsLite(_ => _.Copies).Where(mle => mle.Element.Type.RefersTo(tuple.Type.ToTypeDN())).UnsafeUpdateMList()
                                                .Set(mle => mle.Element.CopyTable, mle => l)
                                                .Set(mle => mle.Element.DisableForeignKeys, mle => tuple.Strategy.DisableForeignKeys.Value)
                                                .Set(mle => mle.Element.InsertedRows, mle => result.Inserted)
                                                .Set(mle => mle.Element.UpdatedRows, mle => result.Updated)
                                                .Execute();
                                    }))
                                    {
                                        result = tuple.Strategy.Importer.Import(machine, tuple.Table, tuple.Strategy, newDatabase);
                                    }
                                }

                                using (token.MeasureTime(l => import.InDB().UnsafeUpdate().Set(s => s.Unlock, s => l).Execute()))
                                    UnlockTables(machine.ToLite());
                            }
                            finally
                            {
                                using (token.MeasureTime(l => import.InDB().UnsafeUpdate().Set(s => s.EnableForeignKeys, s => l).Execute()))
                                    foreach (var item in uploadTables.Where(u => u.Strategy.DisableForeignKeys.Value))
                                    {
                                        EnableForeignKeys(item.Table);
                                    }
                            }

                            using (token.MeasureTime(l => import.InDB().UnsafeUpdate().Set(s => s.DropDatabase, s => l).Execute()))
                                DropDatabase(newDatabase);

                            token.ThrowIfCancellationRequested();

                            import.InDB().UnsafeUpdate()
                                .Set(s => s.State,s=> DisconnectedImportState.Completed)
                                .Set(s => s.Total,s=>  s.CalculateTotal())
                                .Execute();

                            machine.InDB().UnsafeUpdate()
                                .Set(m => m.State, m => file == null ? DisconnectedMachineState.Fixed : DisconnectedMachineState.Connected)
                                .Execute();
                        }
                        catch (Exception e)
                        {
                            var ex = e.LogException();

                            import.InDB().UnsafeUpdate()
                               .Set(m => m.Exception, m => ex.ToLite())
                               .Set(m => m.State, m => DisconnectedImportState.Error)
                               .Execute();

                            machine.InDB().UnsafeUpdate()
                                .Set(m => m.State, m => DisconnectedMachineState.Faulted)
                                .Execute();

                            OnImportingError(machine, import, e);
                        }
                        finally
                        {
                            runningExports.Remove(import);

                            DisconnectedMachineDN.Current = null;

                            OnEndImporting();
                        }
                    }
            });


            runningExports.Add(import, new RunningImports { Task = task, CancelationSource = cancelationSource });

            return import;
        }

        public virtual void SkipExport(Lite<DisconnectedMachineDN> machine)
        {
            UnlockTables(machine);

            machine.InDB().UnsafeUpdate().Set(m => m.State, m => DisconnectedMachineState.Connected).Execute();
        }

        public virtual void ConnectAfterFix(Lite<DisconnectedMachineDN> machine)
        {
            machine.InDB().UnsafeUpdate().Set(m => m.State, m => DisconnectedMachineState.Connected).Execute();
        }

        protected virtual void OnStartImporting(DisconnectedMachineDN machine)
        {
        }

        protected virtual void OnEndImporting()
        {
        }

        protected virtual void OnImportingError(DisconnectedMachineDN machine, Lite<DisconnectedImportDN> import, Exception exception)
        {
        }

        private void DropDatabaseIfExists(DisconnectedMachineDN machine)
        {
            DisconnectedTools.DropIfExists(DatabaseName(machine));
        }

        private void DropDatabase(SqlConnector newDatabase)
        {
            DisconnectedTools.DropDatabase(new DatabaseName(null, newDatabase.DatabaseName()));
        }

        protected virtual void EnableForeignKeys(Table table)
        {
            DisconnectedTools.EnableForeignKeys(table);

            foreach (var rt in table.TablesMList())
                DisconnectedTools.EnableForeignKeys(rt);
        }

        protected virtual void DisableForeignKeys(Table table)
        {
            DisconnectedTools.DisableForeignKeys(table);

            foreach (var rt in table.TablesMList())
                DisconnectedTools.DisableForeignKeys(rt);
        }

        private void RestoreDatabase(DisconnectedMachineDN machine, Lite<DisconnectedImportDN> import)
        {
            string backupFileName = Path.Combine(DisconnectedLogic.BackupFolder, BackupFileName(machine, import));

            string fileName = DatabaseFileName(machine);
            string logFileName = DatabaseLogFileName(machine);

            DisconnectedTools.RestoreDatabase(DatabaseName(machine), backupFileName, fileName, logFileName);
        }

        private string GetImportConnectionString(DisconnectedMachineDN machine)
        {
            return ((SqlConnector)Connector.Current).ConnectionString.Replace(Connector.Current.DatabaseName(), DatabaseName(machine).Name);
        }

        protected virtual string DatabaseFileName(DisconnectedMachineDN machine)
        {
            return Path.Combine(DisconnectedLogic.DatabaseFolder, Connector.Current.DatabaseName() + "_Import_" + DisconnectedTools.CleanMachineName(machine.MachineName) + ".mdf");
        }

        protected virtual string DatabaseLogFileName(DisconnectedMachineDN machine)
        {
            return Path.Combine(DisconnectedLogic.DatabaseFolder, Connector.Current.DatabaseName() + "_Import_" + DisconnectedTools.CleanMachineName(machine.MachineName) + "_Log.ldf");
        }

        protected virtual DatabaseName DatabaseName(DisconnectedMachineDN machine)
        {
            return new DatabaseName(null, Connector.Current.DatabaseName() + "_Import_" + DisconnectedTools.CleanMachineName(machine.MachineName));
        }

        public virtual string BackupNetworkFileName(DisconnectedMachineDN machine, Lite<DisconnectedImportDN> import)
        {
            return Path.Combine(DisconnectedLogic.BackupNetworkFolder, BackupFileName(machine, import));
        }

        protected virtual string BackupFileName(DisconnectedMachineDN machine, Lite<DisconnectedImportDN> import)
        {
            return "{0}.{1}.Import.{2}.bak".Formato(Connector.Current.DatabaseName(), DisconnectedTools.CleanMachineName(machine.MachineName), import.Id);
        }

        private IQueryable<MListElement<DisconnectedImportDN, DisconnectedImportTableDN>> ImportTableQuery(Lite<DisconnectedImportDN> import, TypeDN type)
        {
            return Database.MListQuery((DisconnectedImportDN s) => s.Copies).Where(dst => dst.Parent.ToLite() == import && dst.Element.Type.RefersTo(type));
        }

        public void UnlockTables(Lite<DisconnectedMachineDN> machine)
        {
            foreach (var kvp in DisconnectedLogic.strategies)
            {
                if (kvp.Value.Upload == Upload.Subset)
                    miUnlockTable.MakeGenericMethod(kvp.Key).Invoke(null, new[] { machine });
            }
        }

        static readonly MethodInfo miUnlockTable = typeof(ImportManager).GetMethod("UnlockTable", BindingFlags.NonPublic | BindingFlags.Static);
        static int UnlockTable<T>(Lite<DisconnectedMachineDN> machine) where T : Entity
        {
            using (ExecutionMode.Global())
                return Database.Query<T>().Where(a => a.Mixin<DisconnectedMixin>().DisconnectedMachine == machine)
                    .UnsafeUpdate()
                    .Set(a => a.Mixin<DisconnectedMixin>().DisconnectedMachine, a => null)
                    .Set(a => a.Mixin<DisconnectedMixin>().LastOnlineTicks, a => null)
                    .Execute();
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

            return new ImportResult { Inserted = inserts, Updated = 0 };
        }

        protected virtual int Insert(DisconnectedMachineDN machine, Table table, IDisconnectedStrategy strategy, SqlConnector newDatabase)
        {
            var interval = GetNewIdsInterval(table, machine, newDatabase);

            if (interval == null)
                return 0;

            DatabaseName newDatabaseName = new DatabaseName(null, newDatabase.DatabaseName());

            using (DisconnectedTools.SaveAndRestoreNextId(table))
            {
                using (Transaction tr = new Transaction())
                {
                    using (Administrator.DisableIdentity(table.Name))
                    {
                        SqlPreCommandSimple sql = InsertTableScript(table, newDatabaseName, interval);

                        int result = Executor.ExecuteNonQuery(sql);

                        foreach (var rt in table.TablesMList())
                        {
                            SqlPreCommandSimple rsql = InsertRelationalTableScript(table, newDatabaseName, interval, rt);

                            Executor.ExecuteNonQuery(rsql);
                        }

                        return tr.Commit(result);
                    }
                }
            }
        }

        protected virtual SqlPreCommandSimple InsertRelationalTableScript(Table table, DatabaseName newDatabaseName, Interval<int>? interval, TableMList rt)
        {
            ParameterBuilder pb = Connector.Current.ParameterBuilder;
            var columns = rt.Columns.Values.Where(c => !c.Identity);

            string command = @"INSERT INTO {0} ({1})
SELECT {2}
                    from {3} as [relationalTable]
JOIN {4} [table] on [relationalTable].{5} = [table].Id
WHERE @min <= [table].Id AND [table].Id < @max".Formato(
rt.Name,
columns.ToString(c => c.Name.SqlEscape(), ", "),
columns.ToString(c => "[relationalTable]." + c.Name.SqlEscape(), ", "),
rt.Name.OnDatabase(newDatabaseName),
table.Name.OnDatabase(newDatabaseName),
rt.BackReference.Name.SqlEscape());

            var sql = new SqlPreCommandSimple(command, new List<DbParameter>()
            {
                pb.CreateParameter("@min", interval.Value.Min, typeof(int)),
                pb.CreateParameter("@max", interval.Value.Max, typeof(int)),
            });
            return sql;
        }

        protected virtual SqlPreCommandSimple InsertTableScript(Table table, DatabaseName newDatabaseName, Interval<int>? interval)
        {
            ParameterBuilder pb = Connector.Current.ParameterBuilder;
            string command = @"INSERT INTO {0} ({1})
SELECT {2}
                    from {3} as [table]
WHERE @min <= [table].Id AND [table].Id < @max".Formato(
table.Name,
table.Columns.Keys.ToString(a => a.SqlEscape(), ", "),
table.Columns.Keys.ToString(a => "[table]." + a.SqlEscape(), ", "),
table.Name.OnDatabase(newDatabaseName));

            var sql = new SqlPreCommandSimple(command, new List<DbParameter>()
            {
                pb.CreateParameter("@min", interval.Value.Min, typeof(int)),
                pb.CreateParameter("@max", interval.Value.Max, typeof(int)),
            });
            return sql;
        }

        protected virtual Interval<int>? GetNewIdsInterval(Table table, DisconnectedMachineDN machine, SqlConnector newDatabase)
        {
            int? maxOther;
            using (Connector.Override(newDatabase))
            using (ObjectName.OverrideOptions(new ObjectNameOptions { AvoidDatabaseName = true }))
                maxOther = DisconnectedTools.MaxIdInRange(table, machine.SeedMin, machine.SeedMax);

            if (maxOther == null)
                return null;

            int? max = DisconnectedTools.MaxIdInRange(table, machine.SeedMin, machine.SeedMax);
            if (max != null && max == maxOther)
                return null;

            return new Interval<int>((max + 1) ?? machine.SeedMin, machine.SeedMax);
        }
    }

    public class UpdateImporter<T> : BasicImporter<T> where T : Entity
    {
        public override ImportResult Import(DisconnectedMachineDN machine, Table table, IDisconnectedStrategy strategy, SqlConnector newDatabase)
        {
            int inserts = Insert(machine, table, strategy, newDatabase);

            int update = strategy.Upload == Upload.Subset ? Update(machine, table, strategy, new DatabaseName(null, newDatabase.DatabaseName())) : 0;

            return new ImportResult { Inserted = inserts, Updated = update };
        }

        protected virtual int Update(DisconnectedMachineDN machine, Table table, IDisconnectedStrategy strategy, DatabaseName newDatabaseName)
        {
            using (Transaction tr = new Transaction())
            {
                SqlPreCommandSimple command = UpdateTableScript(machine, table, newDatabaseName);

                int result = Executor.ExecuteNonQuery(command);

                foreach (var rt in table.TablesMList())
                {
                    SqlPreCommandSimple delete = DeleteUpdatedRelationalTableScript(machine, table, rt, newDatabaseName);

                    Executor.ExecuteNonQuery(delete);

                    SqlPreCommandSimple insert = InsertUpdatedRelationalTableScript(machine, table, rt, newDatabaseName);

                    Executor.ExecuteNonQuery(insert);
                }

                return tr.Commit(result);
            }
        }

        protected virtual SqlPreCommandSimple InsertUpdatedRelationalTableScript(DisconnectedMachineDN machine, Table table, TableMList rt, DatabaseName newDatabaseName)
        {
            ParameterBuilder pb = Connector.Current.ParameterBuilder;
            var columns = rt.Columns.Values.Where(c => !c.Identity);

            var insert = new SqlPreCommandSimple(@"INSERT INTO {0} ({1})
SELECT {2}
FROM {3} as [relationalTable]
INNER JOIN {4} as [table] ON [relationalTable].{5} = [table].id".Formato(
            rt.Name,
            columns.ToString(c => c.Name.SqlEscape(), ", "),
            columns.ToString(c => "[relationalTable]." + c.Name.SqlEscape(), ", "),
            rt.Name.OnDatabase(newDatabaseName),
            table.Name.OnDatabase(newDatabaseName),
            rt.BackReference.Name.SqlEscape()) + GetUpdateWhere(table), new List<DbParameter> { pb.CreateParameter("@machineId", machine.Id, typeof(int)) });
            return insert;
        }

        protected virtual SqlPreCommandSimple DeleteUpdatedRelationalTableScript(DisconnectedMachineDN machine, Table table, TableMList rt, DatabaseName newDatabaseName)
        {
            ParameterBuilder pb = Connector.Current.ParameterBuilder;

            var delete = new SqlPreCommandSimple(@"DELETE {0}
FROM {0}
INNER JOIN {1} as [table] ON {0}.{2} = [table].id".Formato(
                rt.Name,
                table.Name.OnDatabase(newDatabaseName),
                rt.BackReference.Name.SqlEscape()) + GetUpdateWhere(table), new List<DbParameter> { pb.CreateParameter("@machineId", machine.Id, typeof(int)) });
            return delete;
        }

        protected virtual SqlPreCommandSimple UpdateTableScript(DisconnectedMachineDN machine, Table table, DatabaseName newDatabaseName)
        {
            ParameterBuilder pb = Connector.Current.ParameterBuilder;

            var command = new SqlPreCommandSimple(@"UPDATE {0} SET
{2}
FROM {0}
INNER JOIN {1} as [table] ON {0}.id = [table].id
".Formato(
 table.Name,
 table.Name.OnDatabase(newDatabaseName),
 table.Columns.Values.Where(c => !c.PrimaryKey).ToString(c => "   {0}.{1} = [table].{1}".Formato(table.Name, c.Name.SqlEscape()), ",\r\n")) + GetUpdateWhere(table),
 new List<DbParameter> { pb.CreateParameter("@machineId", machine.Id, typeof(int)) });
            return command;
        }

        protected virtual string GetUpdateWhere(Table table)
        {
            var s = Schema.Current;

            var where = "\r\nWHERE [table].{0} = @machineId AND [table].{1} != [table].{2}".Formato(
                ((FieldReference)s.Field((T t) => t.Mixin<DisconnectedMixin>().DisconnectedMachine)).Name.SqlEscape(),
                ((FieldValue)s.Field((T t) => t.Ticks)).Name.SqlEscape(),
                ((FieldValue)s.Field((T t) => t.Mixin<DisconnectedMixin>().LastOnlineTicks)).Name.SqlEscape());
            return where;
        }
    }

    public class ImportResult
    {
        public int Inserted;
        public int Updated;
    }
}
