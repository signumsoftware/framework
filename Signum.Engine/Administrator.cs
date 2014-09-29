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
using System.Data.SqlServerCe;
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
            where T : IdentifiableEntity
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

            using (schema.Database == null ? null : Administrator.OverrideDatabaseInViews(schema.Database))
            {
                return (from t in Database.View<SysTables>()
                        join s in Database.View<SysSchemas>() on t.schema_id equals s.schema_id
                        where t.name == table.Name.Name && s.name == schema.Name
                        select t).Any();
            }
        }

        internal static readonly ThreadVariable<DatabaseName> viewDatabase = Statics.ThreadVariable<DatabaseName>("viewDatabase");
        public static IDisposable OverrideDatabaseInViews(DatabaseName database)
        {
            var old = viewDatabase.Value;
            viewDatabase.Value = database;
            return new Disposable(() => viewDatabase.Value = old);
        }

        public static List<T> TryRetrieveAll<T>(Replacements replacements)
            where T : IdentifiableEntity
        {
            return TryRetrieveAll(typeof(T), replacements).Cast<T>().ToList();
        }

        public static List<IdentifiableEntity> TryRetrieveAll(Type type, Replacements replacements)
        {
            Table table = Schema.Current.Table(type);

            using (Synchronizer.RenameTable(table, replacements))
            using (ExecutionMode.DisableCache())
            {
                if (ExistTable(table))
                    return Database.RetrieveAll(type);
                return new List<IdentifiableEntity>();
            }
        }

        public static SqlPreCommand TotalGenerationScript()
        {
            return Schema.Current.GenerationScipt();
        }

        public static SqlPreCommand TotalSynchronizeScript(bool interactive = true)
        {
            return Schema.Current.SynchronizationScript(Connector.Current.DatabaseName(), interactive);
        }


        public static T SetId<T>(this T ident, int? id)
            where T : IdentifiableEntity
        {
            ident.id = id;
            return ident;
        }

        public static T SetReadonly<T, V>(this T ident, Expression<Func<T, V>> readonlyProperty, V value)
             where T : ModifiableEntity
        {
            var pi = ReflectionTools.BasePropertyInfo(readonlyProperty);

            Action<T, V> setter = ReadonlySetterCache<T>.Setter<V>(pi);

            setter(ident, value);

            ident.SetSelfModified();

            return ident;
        }

        static class ReadonlySetterCache<T> where T : ModifiableEntity
        {
            static ConcurrentDictionary<string, Delegate> cache = new ConcurrentDictionary<string, Delegate>();

            internal static Action<T, V> Setter<V>(PropertyInfo pi)
            {
                return (Action<T, V>)cache.GetOrAdd(pi.Name, s => ReflectionTools.CreateSetter<T, V>(Reflector.FindFieldInfo(typeof(T), pi)));
            }
        }

        public static T SetNew<T>(this T ident)
            where T : IdentifiableEntity
        {
            ident.IsNew = true;
            ident.SetSelfModified();
            return ident;
        }

        public static T SetNotModified<T>(this T ident)
            where T : Modifiable
        {
            if (ident is IdentifiableEntity)
                ((IdentifiableEntity)(Modifiable)ident).IsNew = false;
            ident.Modified = ModifiedState.Clean;
            return ident;
        }

        public static T SetNotModifiedGraph<T>(this T ident, int id)
            where T : IdentifiableEntity
        {
            foreach (var item in GraphExplorer.FromRoot(ident).Where(a => a.Modified != ModifiedState.Sealed))
            {
                item.SetNotModified();
                if (item is IdentifiableEntity)
                    ((IdentifiableEntity)item).SetId(-1);
            }

            ident.SetId(id);

            return ident;
        }

        public static IDisposable DisableIdentity<T>()
            where T : IdentifiableEntity
        {
            Table table = Schema.Current.Table<T>();
            return DisableIdentity(table);
        }

        public static IDisposable DisableIdentity<T, V>(Expression<Func<T, MList<V>>> mListField)
          where T : IdentifiableEntity
        {
            TableMList table = ((FieldMList)Schema.Current.Field(mListField)).TableMList;
            return DisableIdentity(table.Name);
        }

        public static IDisposable DisableIdentity(Table table)
        {
            table.Identity = false;
            SqlBuilder.SetIdentityInsert(table.Name, true).ExecuteNonQuery();

            return new Disposable(() =>
            {
                table.Identity = true;
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
            where T : IdentifiableEntity
        {
            using (Transaction tr = new Transaction())
            using (Administrator.DisableIdentity<T>())
            {
                Database.Save(entities);
                tr.Commit();
            }
        }

        public static void SaveListDisableIdentity<T>(IEnumerable<T> entities)
            where T : IdentifiableEntity
        {
            using (Transaction tr = new Transaction())
            using (Administrator.DisableIdentity<T>())
            {
                Database.SaveList(entities);
                tr.Commit();
            }
        }

        public static int RemoveDuplicates<T, S>(Expression<Func<T, S>> key)
           where T : IdentifiableEntity
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
            where T : IdentifiableEntity
        {
            var prov = ((DbQueryProvider)query.Provider);

            return prov.Delete<SqlPreCommandSimple>(query, cm => cm, removeSelectRowCount: true);
        }

        public static SqlPreCommandSimple UnsafeDeletePreCommand<E, V>(IQueryable<MListElement<E, V>> query)
            where E : IdentifiableEntity
        {
            var prov = ((DbQueryProvider)query.Provider);

            return prov.Delete<SqlPreCommandSimple>(query, cm => cm, removeSelectRowCount: true);
        }

        public static SqlPreCommandSimple UnsafeUpdatePartPreCommand(IUpdateable update)
        {
            var prov = ((DbQueryProvider)update.Query.Provider);

            return prov.Update(update, sql => sql, removeSelectRowCount: true);
        }

        public static void UpdateToStrings<T>() where T : IdentifiableEntity, new()
        {
            UpdateToStrings(Database.Query<T>());
        }

        public static void UpdateToStrings<T>(IQueryable<T> query) where T : IdentifiableEntity, new()
        {
            SafeConsole.WriteLineColor(ConsoleColor.Cyan, "Saving toStr for {0}".Formato(typeof(T).TypeName()));

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

        public static void UpdateToStrings<T>(Expression<Func<T, string>> expression) where T : IdentifiableEntity, new()
        {
            UpdateToStrings(Database.Query<T>(), expression);
        }

        public static void UpdateToStrings<T>(IQueryable<T> query, Expression<Func<T, string>> expression) where T : IdentifiableEntity, new()
        {
            SafeConsole.WaitRows("UnsafeUpdate toStr for {0}".Formato(typeof(T).TypeName()), () =>
                query.UnsafeUpdate().Set(a => a.toStr, expression).Execute());
        }

        public static IDisposable PrepareForBatchLoadScope<T>(bool disableForeignKeys = true, bool disableMultipleIndexes = true, bool disableUniqueIndexes = false) where T : IdentifiableEntity
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
                Executor.ExecuteNonQuery("ALTER TABLE {0} NOCHECK CONSTRAINT ALL".Formato(table.Name));

                onDispose += () =>
                {
                    SafeConsole.WriteColor(ConsoleColor.DarkMagenta, " RE-CHECK Foreign Keys");
                    Executor.ExecuteNonQuery("ALTER TABLE {0}  WITH CHECK CHECK CONSTRAINT ALL".Formato(table.Name));
                };
            }

            if (disableMultipleIndexes)
            {
                var multiIndexes = GetIndixesNames(table, unique: false);

                if (multiIndexes.Any())
                {
                    SafeConsole.WriteColor(ConsoleColor.DarkMagenta, " DISABLE Multiple Indexes");
                    multiIndexes.Select(i => SqlBuilder.DisableIndex(table.Name, i)).Combine(Spacing.Simple).ExecuteLeaves();
                    Executor.ExecuteNonQuery(multiIndexes.ToString(i => "ALTER INDEX [{0}] ON {1} DISABLE".Formato(i, table.Name), "\r\n"));

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
            using (OverrideDatabaseInViews(table.Name.Schema.Database))
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

        public static void DropUniqueIndexes<T>() where T : IdentifiableEntity
        {
            var table = Schema.Current.Table<T>();
            var indexesNames = Administrator.GetIndixesNames(table, unique: true);

            if (indexesNames.HasItems())
                indexesNames.Select(n => SqlBuilder.DropIndex(table.Name, n)).Combine(Spacing.Simple).ExecuteLeaves();
        }



        public static SqlPreCommand MoveAllForeignKeysScript<T>(Lite<T> oldEntity, Lite<T> newEntity)
        where T : IdentifiableEntity
        {
            return MoveAllForeignKeysPrivate<T>(oldEntity, newEntity).Select(a => a.UpdateScript).Combine(Spacing.Double);
        }

        public static void MoveAllForeignKeysConsole<T>(Lite<T> oldEntity, Lite<T> newEntity)
            where T : IdentifiableEntity
        {
            var tuples = MoveAllForeignKeysPrivate<T>(oldEntity, newEntity);

            foreach (var t in tuples)
            {
                SafeConsole.WaitRows("{0}.{1}".Formato(t.ColumnTable.Table.Name.Name, t.ColumnTable.Column.Name), () => t.UpdateScript.ExecuteNonQuery());
            }
        }

        class ColumnTableScript
        {
            public ColumnTable ColumnTable;
            public SqlPreCommandSimple UpdateScript;
        }

        static List<ColumnTableScript> MoveAllForeignKeysPrivate<T>(Lite<T> oldEntity, Lite<T> newEntity)
        where T : IdentifiableEntity
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
                UpdateScript = new SqlPreCommandSimple("UPDATE {0}\r\nSET {1} = @newEntity\r\nWHERE {1} = @oldEntity".Formato(ct.Table.Name, ct.Column.Name.SqlEscape()), new List<DbParameter>
                {
                    pb.CreateParameter("@oldEntity", SqlBuilder.PrimaryKeyType, null, false, oldEntity.Id),
                    pb.CreateParameter("@newEntity", SqlBuilder.PrimaryKeyType, null, false, newEntity.Id),
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

        public static void BulkInsert<T>(IEnumerable<T> entities, 
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default) 
            where T : IdentifiableEntity
        {
            DataTable dt = new DataTable();
            var t = Schema.Current.Table<T>();
            foreach (var c in t.Columns.Values.Where(c => !c.Identity))
            {
                dt.Columns.Add(new DataColumn(c.Name));
            }

            foreach (var e in entities)
            {
                dt.Rows.Add(t.BulkInsertDataRow(e));
            }


            if (options.HasFlag(SqlBulkCopyOptions.UseInternalTransaction))
                Executor.BulkCopy(dt, t.Name, options);
            else
            {
                using (Transaction tr = new Transaction())
                {
                    Executor.BulkCopy(dt, t.Name, options);

                    tr.Commit();
                }
            }

        }

        public static void BulkInsertMList<E, V>(Expression<Func<E, MList<V>>> mListProperty,
            IEnumerable<MListElement<E, V>> entities,
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default)
            where E : Entity
        {
            DataTable dt = new DataTable();
            var t = ((FieldMList)Schema.Current.Field(mListProperty)).TableMList;
            foreach (var c in t.Columns.Values.Where(c => !c.Identity))
                dt.Columns.Add(new DataColumn(c.Name));

            foreach (var e in entities)
            {
                dt.Rows.Add(t.BulkInsertDataRow(e.Parent, e.Element, e.Order));
            }

            if (options.HasFlag(SqlBulkCopyOptions.UseInternalTransaction))
                Executor.BulkCopy(dt, t.Name, options);
            else
            {
                using (Transaction tr = new Transaction())
                {
                    Executor.BulkCopy(dt, t.Name, options);

                    tr.Commit();
                }
            }
        }
    }
}
