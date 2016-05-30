using System;
using System.Linq;
using System.Linq.Expressions;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using System.Data;
using System.Collections.Generic;
using Signum.Utilities.DataStructures;
using Signum.Engine.SchemaInfoTables;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using System.Reflection;
using System.Collections.Concurrent;
using Signum.Engine.Linq;
using System.Data.Common;
using System.Data.SqlClient;

namespace Signum.Engine
{
    public static class Administrator
    {
        public static void TotalGeneration()
        {
            foreach (var db in Schema.Current.DatabaseNames())
                Connector.Current.CleanDatabase(db);

            SqlPreCommandConcat totalScript = (SqlPreCommandConcat)Schema.Current.GenerationScipt();
            foreach (SqlPreCommand command in totalScript.Commands)
            {
                command.ExecuteLeaves();
            }
        }

        public static bool ExistTable<T>()
            where T : Entity
        {
            return ExistTable(Schema.Current.Table<T>());
        }

        public static bool ExistTable(Type type)
        {
            return ExistTable(Schema.Current.Table(type));
        }

        public static bool ExistTable(Table table)
        {
            SchemaName schema = table.Name.Schema;

            if (schema.Database != null && schema.Database.Server != null && !Database.View<SysServers>().Any(ss => ss.name == schema.Database.Server.Name))
                return false;

            if (schema.Database != null && !Database.View<SysDatabases>().Any(ss => ss.name == schema.Database.Name))
                return false;

            using (schema.Database == null ? null : Administrator.OverrideDatabaseInSysViews(schema.Database))
            {
                return (from t in Database.View<SysTables>()
                        join s in Database.View<SysSchemas>() on t.schema_id equals s.schema_id
                        where t.name == table.Name.Name && s.name == schema.Name
                        select t).Any();
            }
        }

        internal static readonly ThreadVariable<DatabaseName> sysViewDatabase = Statics.ThreadVariable<DatabaseName>("viewDatabase");
        public static IDisposable OverrideDatabaseInSysViews(DatabaseName database)
        {
            var old = sysViewDatabase.Value;
            sysViewDatabase.Value = database;
            return new Disposable(() => sysViewDatabase.Value = old);
        }

        public static List<T> TryRetrieveAll<T>(Replacements replacements)
            where T : Entity
        {
            return TryRetrieveAll(typeof(T), replacements).Cast<T>().ToList();
        }

        public static List<Entity> TryRetrieveAll(Type type, Replacements replacements)
        {
            Table table = Schema.Current.Table(type);

            using (Synchronizer.RenameTable(table, replacements))
            using (ExecutionMode.DisableCache())
            {
                if (ExistTable(table))
                    return Database.RetrieveAll(type);
                return new List<Entity>();
            }
        }

        public static SqlPreCommand TotalGenerationScript()
        {
            return Schema.Current.GenerationScipt();
        }

        public static SqlPreCommand TotalSynchronizeScript(bool interactive = true, bool schemaOnly = false)
        {
            var command = Schema.Current.SynchronizationScript(interactive, schemaOnly);

            if (command == null)
                return null;

            return SqlPreCommand.Combine(Spacing.Double,
                new SqlPreCommandSimple(SynchronizerMessage.StartOfSyncScriptGeneratedOn0.NiceToString().FormatWith(DateTime.Now)),

                new SqlPreCommandSimple("use {0}".FormatWith(Connector.Current.DatabaseName())),
                command,
                new SqlPreCommandSimple(SynchronizerMessage.EndOfSyncScript.NiceToString()));
        }


    

        public static IDisposable DisableIdentity<T>()
            where T : Entity
        {
            Table table = Schema.Current.Table<T>();
            return DisableIdentity(table);
        }

        public static IDisposable DisableIdentity<T, V>(Expression<Func<T, MList<V>>> mListField)
          where T : Entity
        {
            TableMList table = ((FieldMList)Schema.Current.Field(mListField)).TableMList;
            return DisableIdentity(table.Name);
        }

        public static IDisposable DisableIdentity(Table table)
        {
            if (!table.IdentityBehaviour)
                throw new InvalidOperationException("Identity is false already");

            table.IdentityBehaviour = false;
            if (table.PrimaryKey.Default == null)
                SqlBuilder.SetIdentityInsert(table.Name, true).ExecuteNonQuery();

            return new Disposable(() =>
            {
                table.IdentityBehaviour = true;

                if (table.PrimaryKey.Default == null)
                    SqlBuilder.SetIdentityInsert(table.Name, false).ExecuteNonQuery();
            });
        }

        public static IDisposable DisableIdentity(ObjectName tableName)
        {
            SqlBuilder.SetIdentityInsert(tableName, true).ExecuteNonQuery();

            return new Disposable(() =>
            {
                SqlBuilder.SetIdentityInsert(tableName, false).ExecuteNonQuery();
            });
        }

        public static void SaveDisableIdentity<T>(T entities)
            where T : Entity
        {
            using (Transaction tr = new Transaction())
            using (Administrator.DisableIdentity<T>())
            {
                Database.Save(entities);
                tr.Commit();
            }
        }

        public static void SaveListDisableIdentity<T>(IEnumerable<T> entities)
            where T : Entity
        {
            using (Transaction tr = new Transaction())
            using (Administrator.DisableIdentity<T>())
            {
                Database.SaveList(entities);
                tr.Commit();
            }
        }

        public static int RemoveDuplicates<T, S>(Expression<Func<T, S>> key)
           where T : Entity
        {
            return (from f1 in Database.Query<T>()
                    join f2 in Database.Query<T>() on key.Evaluate(f1) equals key.Evaluate(f2)
                    where f1.Id > f2.Id
                    select f1).UnsafeDelete();
        }

        public static SqlPreCommandSimple QueryPreCommand<T>(IQueryable<T> query)
        {
            var prov = ((DbQueryProvider)query.Provider);

            return prov.Translate(query.Expression, tr => tr.MainCommand);
        }

        public static SqlPreCommandSimple UnsafeDeletePreCommand<T>(IQueryable<T> query)
            where T : Entity
        {
            var prov = ((DbQueryProvider)query.Provider);

            return prov.Delete<SqlPreCommandSimple>(query, cm => cm, removeSelectRowCount: true);
        }

        public static SqlPreCommandSimple UnsafeDeletePreCommand<E, V>(IQueryable<MListElement<E, V>> query)
            where E : Entity
        {
            var prov = ((DbQueryProvider)query.Provider);

            return prov.Delete<SqlPreCommandSimple>(query, cm => cm, removeSelectRowCount: true);
        }

        public static SqlPreCommandSimple UnsafeUpdatePartPreCommand(IUpdateable update)
        {
            var prov = ((DbQueryProvider)update.Query.Provider);

            return prov.Update(update, sql => sql, removeSelectRowCount: true);
        }

        public static void UpdateToStrings<T>() where T : Entity, new()
        {
            UpdateToStrings(Database.Query<T>());
        }

        public static void UpdateToStrings<T>(IQueryable<T> query) where T : Entity, new()
        {
            SafeConsole.WriteLineColor(ConsoleColor.Cyan, "Saving toStr for {0}".FormatWith(typeof(T).TypeName()));

            if (!query.Any())
                return;

            query.Select(a => a.Id).IntervalsOf(100).ProgressForeach(inter => inter.ToString(), null, (interva, writer) =>
            {
                var list = query.Where(a => interva.Contains(a.Id)).ToList();

                foreach (var item in list)
                {
                    if (item.ToString() != item.toStr)
                        item.InDB().UnsafeUpdate()
                            .Set(a => a.toStr, a => item.ToString())
                            .Execute();
                }
            });
        }

        public static void UpdateToStrings<T>(Expression<Func<T, string>> expression) where T : Entity, new()
        {
            UpdateToStrings(Database.Query<T>(), expression);
        }

        public static void UpdateToStrings<T>(IQueryable<T> query, Expression<Func<T, string>> expression) where T : Entity, new()
        {
            SafeConsole.WaitRows("UnsafeUpdate toStr for {0}".FormatWith(typeof(T).TypeName()), () =>
                query.UnsafeUpdate().Set(a => a.toStr, expression).Execute());
        }

        public static void UpdateToString<T>(T entity) where T : Entity, new()
        {
                entity.InDB().UnsafeUpdate()
                    .Set(e => e.toStr, e => entity.ToString())
                    .Execute();
        }

        public static void UpdateToString<T>(T entity, Expression<Func<T, string>> expression) where T : Entity, new()
        {
            entity.InDB().UnsafeUpdate()
                .Set(e => e.toStr, expression)
                .Execute();
        }

        public static IDisposable PrepareForBatchLoadScope<T>(bool disableForeignKeys = true, bool disableMultipleIndexes = true, bool disableUniqueIndexes = false) where T : Entity
        {
            Table table = Schema.Current.Table(typeof(T));

            return table.PrepareForBathLoadScope(disableForeignKeys, disableMultipleIndexes, disableUniqueIndexes);
        }

        static IDisposable PrepareForBathLoadScope(this Table table, bool disableForeignKeys, bool disableMultipleIndexes, bool disableUniqueIndexes)
        {
            IDisposable disp = PrepareTableForBatchLoadScope(table, disableForeignKeys, disableMultipleIndexes, disableUniqueIndexes);

            var list = table.TablesMList().Select(rt => PrepareTableForBatchLoadScope(rt, disableForeignKeys, disableMultipleIndexes, disableUniqueIndexes)).ToList();

            return new Disposable(() =>
            {
                disp.Dispose();

                foreach (var d in list)
                    d.Dispose();
            });
        }

        public static IDisposable PrepareTableForBatchLoadScope(ITable table, bool disableForeignKeys, bool disableMultipleIndexes, bool disableUniqueIndexes)
        {
            SafeConsole.WriteColor(ConsoleColor.Magenta, table.Name + ":");
            Action onDispose = () => SafeConsole.WriteColor(ConsoleColor.Magenta, table.Name + ":");

            if (disableForeignKeys)
            {
                SafeConsole.WriteColor(ConsoleColor.DarkMagenta, " NOCHECK  Foreign Keys");
                Executor.ExecuteNonQuery("ALTER TABLE {0} NOCHECK CONSTRAINT ALL".FormatWith(table.Name));

                onDispose += () =>
                {
                    SafeConsole.WriteColor(ConsoleColor.DarkMagenta, " RE-CHECK Foreign Keys");
                    Executor.ExecuteNonQuery("ALTER TABLE {0}  WITH CHECK CHECK CONSTRAINT ALL".FormatWith(table.Name));
                };
            }

            if (disableMultipleIndexes)
            {
                var multiIndexes = GetIndixesNames(table, unique: false);

                if (multiIndexes.Any())
                {
                    SafeConsole.WriteColor(ConsoleColor.DarkMagenta, " DISABLE Multiple Indexes");
                    multiIndexes.Select(i => SqlBuilder.DisableIndex(table.Name, i)).Combine(Spacing.Simple).ExecuteLeaves();
                    Executor.ExecuteNonQuery(multiIndexes.ToString(i => "ALTER INDEX [{0}] ON {1} DISABLE".FormatWith(i, table.Name), "\r\n"));

                    onDispose += () =>
                    {
                        SafeConsole.WriteColor(ConsoleColor.DarkMagenta, " REBUILD Multiple Indexes");
                        multiIndexes.Select(i => SqlBuilder.EnableIndex(table.Name, i)).Combine(Spacing.Simple).ExecuteLeaves();
                    };
                }
            }

            if (disableUniqueIndexes)
            {
                var uniqueIndexes = GetIndixesNames(table, unique: true);

                if (uniqueIndexes.Any())
                {
                    SafeConsole.WriteColor(ConsoleColor.DarkMagenta, " DISABLE Unique Indexes");
                    uniqueIndexes.Select(i => SqlBuilder.DisableIndex(table.Name, i)).Combine(Spacing.Simple).ExecuteLeaves();
                    onDispose += () =>
                    {
                        SafeConsole.WriteColor(ConsoleColor.DarkMagenta, " REBUILD Unique Indexes");
                        uniqueIndexes.Select(i => SqlBuilder.EnableIndex(table.Name, i)).Combine(Spacing.Simple).ExecuteLeaves();
                    };
                }
            }

            Console.WriteLine();
            onDispose += () => Console.WriteLine();

            return new Disposable(onDispose);
        }

        public static List<string> GetIndixesNames(this ITable table, bool unique)
        {
            using (OverrideDatabaseInSysViews(table.Name.Schema.Database))
            {
                return (from s in Database.View<SysSchemas>()
                        where s.name == table.Name.Schema.Name
                        from t in s.Tables()
                        where t.name == table.Name.Name
                        from i in t.Indices()
                        where i.is_unique == unique && !i.is_primary_key
                        select i.name).ToList();
            }
        }

        public static void DropUniqueIndexes<T>() where T : Entity
        {
            var table = Schema.Current.Table<T>();
            var indexesNames = Administrator.GetIndixesNames(table, unique: true);

            if (indexesNames.HasItems())
                indexesNames.Select(n => SqlBuilder.DropIndex(table.Name, n)).Combine(Spacing.Simple).ExecuteLeaves();
        }



        public static SqlPreCommand MoveAllForeignKeysScript<T>(Lite<T> oldEntity, Lite<T> newEntity)
        where T : Entity
        {
            return MoveAllForeignKeysPrivate<T>(oldEntity, newEntity).Select(a => a.UpdateScript).Combine(Spacing.Double);
        }

        public static void MoveAllForeignKeysConsole<T>(Lite<T> oldEntity, Lite<T> newEntity)
            where T : Entity
        {
            var tuples = MoveAllForeignKeysPrivate<T>(oldEntity, newEntity);

            foreach (var t in tuples)
            {
                SafeConsole.WaitRows("{0}.{1}".FormatWith(t.ColumnTable.Table.Name.Name, t.ColumnTable.Column.Name), () => t.UpdateScript.ExecuteNonQuery());
            }
        }

        class ColumnTableScript
        {
            public ColumnTable ColumnTable;
            public SqlPreCommandSimple UpdateScript;
        }

        static List<ColumnTableScript> MoveAllForeignKeysPrivate<T>(Lite<T> oldEntity, Lite<T> newEntity)
        where T : Entity
        {
            if (oldEntity.GetType() != newEntity.GetType())
                throw new ArgumentException("oldEntity and newEntity should have the same type");

            Schema s = Schema.Current;

            Table refTable = s.Table(typeof(T));

            List<ColumnTable> columns = GetColumnTables(s, refTable);

            var pb = Connector.Current.ParameterBuilder;

            return columns.Select(ct => new ColumnTableScript
            {
                ColumnTable = ct,
                UpdateScript = new SqlPreCommandSimple("UPDATE {0}\r\nSET {1} = @newEntity\r\nWHERE {1} = @oldEntity".FormatWith(ct.Table.Name, ct.Column.Name.SqlEscape()), new List<DbParameter>
                {
                    pb.CreateReferenceParameter("@oldEntity", oldEntity.Id, ct.Column),
                    pb.CreateReferenceParameter("@newEntity", newEntity.Id, ct.Column),
                })
            }).ToList();
        }

        class ColumnTable
        {
            public ITable Table;
            public IColumn Column;
        }

        static ConcurrentDictionary<Table, List<ColumnTable>> columns = new ConcurrentDictionary<Table, List<ColumnTable>>();

        static List<ColumnTable> GetColumnTables(Schema schema, Table refTable)
        {
            return columns.GetOrAdd(refTable, rt =>
            {
                return (from t in schema.GetDatabaseTables()
                        from c in t.Columns.Values
                        where c.ReferenceTable == rt
                        select new ColumnTable
                        {
                            Table = t,
                            Column = c,
                        }).ToList();
            });
        }

        public static int BulkInsertDisableIdentity<T>(IEnumerable<T> entities,
          SqlBulkCopyOptions options = SqlBulkCopyOptions.Default, bool validateFirst = false, int? timeout = null)
          where T : Entity
        {
            options |= SqlBulkCopyOptions.KeepIdentity;

            if (options.HasFlag(SqlBulkCopyOptions.UseInternalTransaction))
                throw new InvalidOperationException("BulkInsertDisableIdentity not compatible with UseInternalTransaction");

            var list = entities.ToList();

            if (validateFirst)
            {
                Validate<T>(list);
            }
            
            var t = Schema.Current.Table<T>();
            using (Transaction tr = new Transaction())
            {
                Schema.Current.OnPreBulkInsert(typeof(T), inMListTable: false);

                using (DisableIdentity<T>())
                {
                    DataTable dt = CreateDataTable<T>(list, t);

                    Executor.BulkCopy(dt, t.Name, options, timeout);

                    foreach (var item in list)
                        item.SetNotModified();

                    return tr.Commit(list.Count);
                }
            }
        }

        public static int BulkInsert<T>(IEnumerable<T> entities,
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default, bool validateFirst = false, int? timeout = null, string message = null)
            where T : Entity
        {

            if (message != null)
                return SafeConsole.WaitRows(message == "auto" ? $"BulkInsering { typeof(T).TypeName()}" : message,
                    () => BulkInsert(entities, options, validateFirst, timeout, message: null));

            if (options.HasFlag(SqlBulkCopyOptions.UseInternalTransaction))
                throw new InvalidOperationException("BulkInsertDisableIdentity not compatible with UseInternalTransaction");

            var list = entities.ToList();

            if (validateFirst)
            {
                Validate<T>(list);
            }

            var t = Schema.Current.Table<T>();

            DataTable dt = CreateDataTable<T>(list, t);

            using (Transaction tr = new Transaction())
            {
                Schema.Current.OnPreBulkInsert(typeof(T), inMListTable: false);

                Executor.BulkCopy(dt, t.Name, options, timeout);

                foreach (var item in list)
                    item.SetNotModified();

                return tr.Commit(list.Count);
            }
        }

        private static void Validate<T>(IEnumerable<T> entities) where T : Entity
        {
            foreach (var e in entities)
            {
                var ic = e.IntegrityCheck();

                if (ic != null)
                    throw new IntegrityCheckException(new Dictionary<Guid, Dictionary<string, string>> { { e.temporalId, ic } });
            }
        }

        static DataTable CreateDataTable<T>(IEnumerable<T> entities, Table t) where T : Entity
        {
            DataTable dt = new DataTable();
            foreach (var c in t.Columns.Values.Where(c => !c.IdentityBehaviour))
                dt.Columns.Add(new DataColumn(c.Name, c.Type.UnNullify()));

            foreach (var e in entities)
            {
                if (!e.IsNew)
                    throw new InvalidOperationException("Entites should be new");
                t.SetToStrField(e);
                dt.Rows.Add(t.BulkInsertDataRow(e));
            }
            return dt;
        }

        public static int BulkInsertMListFromEntities<E, V>(List<E> entities, 
            Expression<Func<E, MList<V>>> mListProperty,
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
            int? timeout = null,
            string message = null)
            where E : Entity
        {
            try {
                var func = mListProperty.Compile();

                var mlists = (from e in entities
                              from mle in func(e).Select((iw, i) => new MListElement<E, V>
                              {
                                  Order = i,
                                  Element = iw,
                                  Parent = e,
                              })
                              select mle).ToList();

                return Administrator.BulkInsertMList(mListProperty, mlists, options, timeout, message);
            }
            catch(InvalidOperationException e) when (e.Message.Contains("has no Id"))
            {
                throw new InvalidOperationException($"{nameof(BulkInsertMListFromEntities)} requires that you set the Id of the entities manually using {nameof(UnsafeEntityExtensions.SetId)}");

                throw;
            }
        }



        public static int BulkInsertMList<E, V>(Expression<Func<E, MList<V>>> mListProperty,
            IEnumerable<MListElement<E, V>> entities,
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default, 
            int? timeout = null, 
            string message = null)
            where E : Entity
        {

            if (message != null)
                return SafeConsole.WaitRows(message == "auto" ? $"BulkInsering MList<{ typeof(V).TypeName()}> in { typeof(E).TypeName()}" : message,
                    () => BulkInsertMList(mListProperty, entities, options, timeout, message: null));

            if (options.HasFlag(SqlBulkCopyOptions.UseInternalTransaction))
                throw new InvalidOperationException("BulkInsertDisableIdentity not compatible with UseInternalTransaction");

            DataTable dt = new DataTable();
            var t = ((FieldMList)Schema.Current.Field(mListProperty)).TableMList;
            foreach (var c in t.Columns.Values.Where(c => !c.IdentityBehaviour))
                dt.Columns.Add(new DataColumn(c.Name, c.Type.UnNullify()));

            var list = entities.ToList();

            foreach (var e in list)
            {
                dt.Rows.Add(t.BulkInsertDataRow(e.Parent, e.Element, e.Order));
            }

            using (Transaction tr = options.HasFlag(SqlBulkCopyOptions.UseInternalTransaction) ? null : new Transaction())
            {
                Schema.Current.OnPreBulkInsert(typeof(E), inMListTable: true);

                Executor.BulkCopy(dt, t.Name, options, timeout);

                return tr.Commit(list.Count);
            }
        }

        public static T GetSetTicks<T>(this T entity) where T :Entity
        {
            entity.Ticks = entity.InDBEntity(e => e.Ticks);
            return entity;
        }
    }
}
