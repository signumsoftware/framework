using Signum.Disconnected;
using System.IO;
using System.Data.Common;
using Signum.Utilities.DataStructures;
using Signum.Engine.Sync;

namespace Signum.Disconnected;

public class ImportManager
{
    static object SyncLock = new object();

    class UploadTable
    {
        public Type Type;
        public Table Table;
        public IDisconnectedStrategy Strategy;

        public UploadTable(Type type, Table table, IDisconnectedStrategy strategy)
        {
            Type = type;
            Table = table;
            Strategy = strategy;
        }

        public override string ToString()
        {
            return Table.ToString();
        }
    }

    public void Initialize()
    {
        var tables = Schema.Current.Tables.Values
            .Select(t => new UploadTable(t.Type, t, DisconnectedLogic.GetStrategy(t.Type)))
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

    List<UploadTable> uploadTables = null!;


    class RunningImports
    {
        public Task Task;
        public CancellationTokenSource CancelationSource;

        public RunningImports(Task task, CancellationTokenSource cancelationSource)
        {
            Task = task;
            CancelationSource = cancelationSource;
        }
    }

    Dictionary<Lite<DisconnectedImportEntity>, RunningImports> runningImports = new Dictionary<Lite<DisconnectedImportEntity>, RunningImports>();

    public virtual Lite<DisconnectedImportEntity> BeginImportDatabase(DisconnectedMachineEntity machine, Stream? file = null)
    {
        Lite<DisconnectedImportEntity> import = new DisconnectedImportEntity
        {
            Machine = machine.ToLite(),
            Copies = uploadTables.Select(t => new DisconnectedImportTableEmbedded
            {
                Type = t.Type.ToTypeEntity().ToLite(),
                DisableForeignKeys = t.Strategy.DisableForeignKeys!.Value,
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

        var user = UserHolder.Current;

        var token = cancelationSource.Token;

        var task = Task.Factory.StartNew(() =>
        {
            lock (SyncLock)
                using (UserHolder.UserSession(user))
                {
                    OnStartImporting(machine);

                    DisconnectedMachineEntity.Current = machine.ToLite();

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

                        var newDatabase = new SqlServerConnector(connectionString, Schema.Current, ((SqlServerConnector)Connector.Current).Version);

                        using (token.MeasureTime(l => import.InDB().UnsafeUpdate().Set(s => s.SynchronizeSchema, s => l).Execute()))
                        using (Connector.Override(newDatabase))
                        using (ObjectName.OverrideOptions(new ObjectNameOptions { AvoidDatabaseName = true }))
                        using (ExecutionMode.DisableCache())
                        {
                            var script = Administrator.TotalSynchronizeScript(interactive: false, schemaOnly: true);

                            if (script != null)
                            {
                                string fileName = BackupNetworkFileName(machine, import) + ".sql";
                                script.Save(fileName);
                                throw new InvalidOperationException("The schema has changed since the last export. A schema sync script has been saved on: {0}".FormatWith(fileName));
                            }
                        }

                        try
                        {
                            using (token.MeasureTime(l => import.InDB().UnsafeUpdate().Set(s => s.DisableForeignKeys, s => l).Execute()))
                                foreach (var item in uploadTables.Where(u => u.Strategy.DisableForeignKeys!.Value))
                                {
                                    DisableForeignKeys(item.Table);
                                }

                            foreach (var tuple in uploadTables)
                            {
                                ImportResult? result = null;
                                using (token.MeasureTime(l =>
                                {
                                    if (result != null)
                                        import.MListElementsLite(_ => _.Copies).Where(mle => mle.Element.Type.Is(tuple.Type.ToTypeEntity())).UnsafeUpdateMList()
                                            .Set(mle => mle.Element.CopyTable, mle => l)
                                            .Set(mle => mle.Element.DisableForeignKeys, mle => tuple.Strategy.DisableForeignKeys!.Value)
                                            .Set(mle => mle.Element.InsertedRows, mle => result.Inserted)
                                            .Set(mle => mle.Element.UpdatedRows, mle => result.Updated)
                                            .Execute();
                                }))
                                {
                                    result = tuple.Strategy.Importer!.Import(machine, tuple.Table, tuple.Strategy, newDatabase);
                                }
                            }

                            using (token.MeasureTime(l => import.InDB().UnsafeUpdate().Set(s => s.Unlock, s => l).Execute()))
                                UnlockTables(machine.ToLite());
                        }
                        finally
                        {
                            using (token.MeasureTime(l => import.InDB().UnsafeUpdate().Set(s => s.EnableForeignKeys, s => l).Execute()))
                                foreach (var item in uploadTables.Where(u => u.Strategy.DisableForeignKeys!.Value))
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
                        runningImports.Remove(import);

                        DisconnectedMachineEntity.Current = null;

                        OnEndImporting();
                    }
                }
        });


        runningImports.Add(import, new RunningImports(task, cancelationSource));

        return import;
    }

    public virtual void SkipExport(Lite<DisconnectedMachineEntity> machine)
    {
        UnlockTables(machine);

        machine.InDB().UnsafeUpdate().Set(m => m.State, m => DisconnectedMachineState.Connected).Execute();
    }

    public virtual void ConnectAfterFix(Lite<DisconnectedMachineEntity> machine)
    {
        machine.InDB().UnsafeUpdate().Set(m => m.State, m => DisconnectedMachineState.Connected).Execute();
    }

    protected virtual void OnStartImporting(DisconnectedMachineEntity machine)
    {
    }

    protected virtual void OnEndImporting()
    {
    }

    protected virtual void OnImportingError(DisconnectedMachineEntity machine, Lite<DisconnectedImportEntity> import, Exception exception)
    {
    }

    private void DropDatabaseIfExists(DisconnectedMachineEntity machine)
    {
        DisconnectedTools.DropIfExists(DatabaseName(machine));
    }

    private void DropDatabase(SqlServerConnector newDatabase)
    {
        var isPostgres = Schema.Current.Settings.IsPostgres;
        DisconnectedTools.DropDatabase(new DatabaseName(null, newDatabase.DatabaseName(), isPostgres));
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

    private void RestoreDatabase(DisconnectedMachineEntity machine, Lite<DisconnectedImportEntity> import)
    {
        string backupFileName = Path.Combine(DisconnectedLogic.BackupFolder, BackupFileName(machine, import));

        string fileName = DatabaseFileName(machine);
        string logFileName = DatabaseLogFileName(machine);

        DisconnectedTools.RestoreDatabase(DatabaseName(machine), backupFileName, fileName, logFileName);
    }

    private string GetImportConnectionString(DisconnectedMachineEntity machine)
    {
        return ((SqlServerConnector)Connector.Current).ConnectionString.Replace(Connector.Current.DatabaseName(), DatabaseName(machine).Name);
    }

    protected virtual string DatabaseFileName(DisconnectedMachineEntity machine)
    {
        return Path.Combine(DisconnectedLogic.DatabaseFolder, Connector.Current.DatabaseName() + "_Import_" + DisconnectedTools.CleanMachineName(machine.MachineName) + ".mdf");
    }

    protected virtual string DatabaseLogFileName(DisconnectedMachineEntity machine)
    {
        return Path.Combine(DisconnectedLogic.DatabaseFolder, Connector.Current.DatabaseName() + "_Import_" + DisconnectedTools.CleanMachineName(machine.MachineName) + "_Log.ldf");
    }

    protected virtual DatabaseName DatabaseName(DisconnectedMachineEntity machine)
    {
        var isPostgres = Schema.Current.Settings.IsPostgres;
        return new DatabaseName(null, Connector.Current.DatabaseName() + "_Import_" + DisconnectedTools.CleanMachineName(machine.MachineName), isPostgres);
    }

    public virtual string BackupNetworkFileName(DisconnectedMachineEntity machine, Lite<DisconnectedImportEntity> import)
    {
        return Path.Combine(DisconnectedLogic.BackupNetworkFolder, BackupFileName(machine, import));
    }

    protected virtual string BackupFileName(DisconnectedMachineEntity machine, Lite<DisconnectedImportEntity> import)
    {
        return "{0}.{1}.Import.{2}.bak".FormatWith(Connector.Current.DatabaseName(), DisconnectedTools.CleanMachineName(machine.MachineName), import.Id);
    }

    private IQueryable<MListElement<DisconnectedImportEntity, DisconnectedImportTableEmbedded>> ImportTableQuery(Lite<DisconnectedImportEntity> import, TypeEntity type)
    {
        return Database.MListQuery((DisconnectedImportEntity s) => s.Copies).Where(dst => dst.Parent.ToLite().Is(import) && dst.Element.Type.Is(type));
    }

    public void UnlockTables(Lite<DisconnectedMachineEntity> machine)
    {
        foreach (var kvp in DisconnectedLogic.strategies)
        {
            if (kvp.Value.Upload == Upload.Subset)
                miUnlockTable.MakeGenericMethod(kvp.Key).Invoke(null, new[] { machine });
        }
    }

    static readonly MethodInfo miUnlockTable = typeof(ImportManager).GetMethod("UnlockTable", BindingFlags.NonPublic | BindingFlags.Static)!;
    static int UnlockTable<T>(Lite<DisconnectedMachineEntity> machine) where T : Entity
    {
        using (ExecutionMode.Global())
            return Database.Query<T>().Where(a => a.Mixin<DisconnectedSubsetMixin>().DisconnectedMachine.Is(machine))
                .UnsafeUpdate()
                .Set(a => a.Mixin<DisconnectedSubsetMixin>().DisconnectedMachine, a => null)
                .Set(a => a.Mixin<DisconnectedSubsetMixin>().LastOnlineTicks, a => null)
                .Execute();
    }
}

public interface ICustomImporter
{
    ImportResult Import(DisconnectedMachineEntity machine, Table table, IDisconnectedStrategy strategy, SqlServerConnector newDatabase);
}


public class BasicImporter<T> : ICustomImporter where T : Entity
{
    public virtual ImportResult Import(DisconnectedMachineEntity machine, Table table, IDisconnectedStrategy strategy, SqlServerConnector newDatabase)
    {
        int inserts = Insert(machine, table, strategy, newDatabase);

        return new ImportResult { Inserted = inserts, Updated = 0 };
    }

    protected virtual int Insert(DisconnectedMachineEntity machine, Table table, IDisconnectedStrategy strategy, SqlServerConnector newDatabase)
    {
        var isPostgres = Schema.Current.Settings.IsPostgres;
        DatabaseName newDatabaseName = new DatabaseName(null, newDatabase.DatabaseName(), isPostgres);

        var count = (int)CountNewItems(table, newDatabaseName).ExecuteScalar()!;

        if (count == 0)
            return 0;

        using (var tr = new Transaction())
        {
            int result;
            using (DisableIdentityIfNecessary(table))
            {
                SqlPreCommandSimple sql = InsertTableScript(table, newDatabaseName);

                result = Executor.ExecuteNonQuery(sql);
            }

            foreach (var rt in table.TablesMList())
            {
                using (DisableIdentityIfNecessary(rt))
                {
                    SqlPreCommandSimple rsql = InsertRelationalTableScript(table, newDatabaseName, rt);

                    Executor.ExecuteNonQuery(rsql);
                }
            }

            return tr.Commit(result);
        }
    }

    protected IDisposable? DisableIdentityIfNecessary(ITable table)
    {
        if (!table.PrimaryKey.Identity)
            return null;

        return Administrator.DisableIdentity(table);
    }

    protected virtual SqlPreCommandSimple InsertRelationalTableScript(Table table, DatabaseName newDatabaseName, TableMList rt)
    {
        ParameterBuilder pb = Connector.Current.ParameterBuilder;
        var created = table.Mixins![typeof(DisconnectedCreatedMixin)].Columns().Single();
        var isPostgres = Schema.Current.Settings.IsPostgres;

        string command = @"INSERT INTO {0} ({1})
SELECT {2}
FROM {3} as [relationalTable]
JOIN {4} [table] on [relationalTable].{5} = [table].{6}
WHERE [table].{7} = 1".FormatWith(
rt.Name,
rt.Columns.Values.ToString(c => c.Name.SqlEscape(isPostgres), ", "),
rt.Columns.Values.ToString(c => "[relationalTable]." + c.Name.SqlEscape(isPostgres), ", "),
rt.Name.OnDatabase(newDatabaseName),
table.Name.OnDatabase(newDatabaseName),
rt.BackReference.Name.SqlEscape(isPostgres),
table.PrimaryKey.Name.SqlEscape(isPostgres),
created.Name.SqlEscape(isPostgres));

        var sql = new SqlPreCommandSimple(command);
        return sql;
    }

    protected virtual SqlPreCommandSimple InsertTableScript(Table table, DatabaseName newDatabaseName)
    {
        var isPostgres = Schema.Current.Settings.IsPostgres;
        var created = table.Mixins![typeof(DisconnectedCreatedMixin)].Columns().Single();

        string command = @"INSERT INTO {0} ({1})
SELECT {2}
FROM {3} as [table]
WHERE [table].{4} = 1".FormatWith(
table.Name,
table.Columns.Values.ToString(c => c.Name.SqlEscape(isPostgres), ", "),
table.Columns.Values.ToString(c => created == c ? "0" : "[table]." + c.Name.SqlEscape(isPostgres), ", "),
table.Name.OnDatabase(newDatabaseName),
created.Name.SqlEscape(isPostgres));

        return new SqlPreCommandSimple(command);
    }

    protected virtual SqlPreCommandSimple CountNewItems(Table table, DatabaseName newDatabaseName)
    {
        var isPostgres = Schema.Current.Settings.IsPostgres;
        string command = @"SELECT COUNT(*)
FROM {1} as [table]
LEFT OUTER JOIN {0} as [current_table] ON [table].{2} = [current_table].{2}
WHERE [current_table].{2} IS NULL".FormatWith(
table.Name,
table.Name.OnDatabase(newDatabaseName),
table.PrimaryKey.Name.SqlEscape(isPostgres));

        return new SqlPreCommandSimple(command);
    }
}

public class UpdateImporter<T> : BasicImporter<T> where T : Entity
{
    public override ImportResult Import(DisconnectedMachineEntity machine, Table table, IDisconnectedStrategy strategy, SqlServerConnector newDatabase)
    {
        var isPostgres = Schema.Current.Settings.IsPostgres;
        int update = strategy.Upload == Upload.Subset ? Update(machine, table, strategy, new DatabaseName(null, newDatabase.DatabaseName(), isPostgres)) : 0;

        int inserts = Insert(machine, table, strategy, newDatabase);

        return new ImportResult { Inserted = inserts, Updated = update };
    }

    protected virtual int Update(DisconnectedMachineEntity machine, Table table, IDisconnectedStrategy strategy, DatabaseName newDatabaseName)
    {
        using (var tr = new Transaction())
        {
            SqlPreCommandSimple command = UpdateTableScript(machine, table, newDatabaseName);

            int result = Executor.ExecuteNonQuery(command);

            foreach (var rt in table.TablesMList())
            {
                SqlPreCommandSimple delete = DeleteUpdatedRelationalTableScript(machine, table, rt, newDatabaseName);

                Executor.ExecuteNonQuery(delete);

                using (DisableIdentityIfNecessary(rt))
                {
                    SqlPreCommandSimple insert = InsertUpdatedRelationalTableScript(machine, table, rt, newDatabaseName);

                    Executor.ExecuteNonQuery(insert);
                }
            }

            return tr.Commit(result);
        }
    }

    protected virtual SqlPreCommandSimple InsertUpdatedRelationalTableScript(DisconnectedMachineEntity machine, Table table, TableMList rt, DatabaseName newDatabaseName)
    {
        ParameterBuilder pb = Connector.Current.ParameterBuilder;
        var isPostgres = Schema.Current.Settings.IsPostgres;
        var insert = new SqlPreCommandSimple(@"INSERT INTO {0} ({1})
SELECT {2}
FROM {3} as [relationalTable]
INNER JOIN {4} as [table] ON [relationalTable].{5} = [table].{6}".FormatWith(
        rt.Name,
        rt.Columns.Values.ToString(c => c.Name.SqlEscape(isPostgres), ", "),
        rt.Columns.Values.ToString(c => "[relationalTable]." + c.Name.SqlEscape(isPostgres), ", "),
        rt.Name.OnDatabase(newDatabaseName),
        table.Name.OnDatabase(newDatabaseName),
        rt.BackReference.Name.SqlEscape(isPostgres),
        table.PrimaryKey.Name.SqlEscape(isPostgres)) + GetUpdateWhere(table), new List<DbParameter> { pb.CreateParameter("@machineId", machine.Id.Object, machine.Id.Object.GetType(), default) });
        return insert;
    }

    protected virtual SqlPreCommandSimple DeleteUpdatedRelationalTableScript(DisconnectedMachineEntity machine, Table table, TableMList rt, DatabaseName newDatabaseName)
    {
        ParameterBuilder pb = Connector.Current.ParameterBuilder;
        var isPostgres = Schema.Current.Settings.IsPostgres;
        var delete = new SqlPreCommandSimple(@"DELETE {0}
FROM {0}
INNER JOIN {1} as [table] ON {0}.{2} = [table].{3}".FormatWith(
            rt.Name,
            table.Name.OnDatabase(newDatabaseName),
            rt.BackReference.Name.SqlEscape(isPostgres),
            table.PrimaryKey.Name.SqlEscape(isPostgres)) + 
            GetUpdateWhere(table),
            new List<DbParameter> { pb.CreateParameter("@machineId", machine.Id.Object, machine.Id.Object.GetType(), default) });
        return delete;
    }

    protected virtual SqlPreCommandSimple UpdateTableScript(DisconnectedMachineEntity machine, Table table, DatabaseName newDatabaseName)
    {
        ParameterBuilder pb = Connector.Current.ParameterBuilder;
        var isPostgres = Schema.Current.Settings.IsPostgres;
        var command = new SqlPreCommandSimple(@"UPDATE {0} SET
{2}
FROM {0}
INNER JOIN {1} as [table] ON {0}.{3} = [table].{3}".FormatWith(
 table.Name,
 table.Name.OnDatabase(newDatabaseName),
 table.Columns.Values.Where(c => !c.PrimaryKey).ToString(c => "   {0} = [table].{0}".FormatWith(c.Name.SqlEscape(isPostgres)), ",\n"),
 table.PrimaryKey.Name.SqlEscape(isPostgres))
 + GetUpdateWhere(table),
 new List<DbParameter> { pb.CreateParameter("@machineId", machine.Id.Object, machine.Id.Object.GetType(), default) });
        return command;
    }

    protected virtual string GetUpdateWhere(Table table)
    {
        var isPostgres = Schema.Current.Settings.IsPostgres;
        var s = Schema.Current;

        var where = "\nWHERE [table].{0} = @machineId AND [table].{1} != [table].{2}".FormatWith(
            ((FieldReference)s.Field((T t) => t.Mixin<DisconnectedSubsetMixin>().DisconnectedMachine)).Name.SqlEscape(isPostgres),
            ((FieldValue)s.Field((T t) => t.Ticks)).Name.SqlEscape(isPostgres),
            ((FieldValue)s.Field((T t) => t.Mixin<DisconnectedSubsetMixin>().LastOnlineTicks)).Name.SqlEscape(isPostgres));
        return where;
    }
}

public class ImportResult
{
    public int Inserted;
    public int Updated;
}
