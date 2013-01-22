using System;
using System.Linq;
using System.Linq.Expressions;
using Signum.Engine.Maps;
using Signum.Engine.Properties;
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

            Action<T, V> setter = ReadonlySetterCache<T>.Getter<V>(pi);

            setter(ident, value);

            ident.SetSelfModified();

            return ident;
        }

        class ReadonlySetterCache<T> where T : ModifiableEntity
        {
            static ConcurrentDictionary<string, Delegate> cache = new ConcurrentDictionary<string, Delegate>();

            internal static Action<T, V> Getter<V>(PropertyInfo pi)
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

        /// <summary>
        /// Disables Identity in a table for the current transaction
        /// </summary>
        public static IDisposable DisableIdentity<T>()
            where T : IdentifiableEntity
        {
            Table table = Schema.Current.Table<T>();
            return DisableIdentity(table);
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

        public static void SetSnapshotIsolation(bool value, string databaseName = null)
        {
            if (databaseName == null)
                databaseName = Connector.Current.DatabaseName();

            Executor.ExecuteNonQuery(SqlBuilder.SetSnapshotIsolation(databaseName, value));
        }

        public static void MakeSnapshotIsolationDefault(bool value, string databaseName = null)
        {
            if (databaseName == null)
                databaseName = Connector.Current.DatabaseName();

            Executor.ExecuteNonQuery(SqlBuilder.MakeSnapshotIsolationDefault(databaseName, value));
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

            return prov.Translate(query.Expression, tr => tr.MainPreCommand());
        }

        public static SqlPreCommandSimple UnsafeDeletePreCommand<T>(IQueryable<T> query) 
            where T : IdentifiableEntity
        {
            var prov = ((DbQueryProvider)query.Provider);

            return prov.Delete<SqlPreCommandSimple>(query, cm => cm.ToPreCommand(), removeSelectRowCount: true);
        }

        public static SqlPreCommandSimple UnsafeDeletePreCommand<E, V>(this IQueryable<MListElement<E, V>> query) 
            where E : IdentifiableEntity
        {
            var prov = ((DbQueryProvider)query.Provider);

            return prov.Delete<SqlPreCommandSimple>(query, cm => cm.ToPreCommand(), removeSelectRowCount: true);
        }

        public static SqlPreCommandSimple UnsafeUpdatePreCommand<E>(this IQueryable<E> query, Expression<Func<E, E>> updateConstructor)
            where E : IdentifiableEntity
        {
            var prov = ((DbQueryProvider)query.Provider);

            return prov.Update(query, null, updateConstructor, cm => cm.ToPreCommand(), removeSelectRowCount: true);
        }


        public static SqlPreCommandSimple UnsafeUpdatePartPreCommand<T, E>(this IQueryable<T> query, Expression<Func<T, E>> entitySelector, Expression<Func<T, E>> updateConstructor)
            where T : IdentifiableEntity
        {
            var prov = ((DbQueryProvider)query.Provider);

            return prov.Update(query, entitySelector, updateConstructor, cm => cm.ToPreCommand(), removeSelectRowCount: true);
        }

        public static SqlPreCommandSimple UnsafeUpdatePreCommand<E, V>(this IQueryable<MListElement<E, V>> query, Expression<Func<MListElement<E, V>, MListElement<E, V>>> updateConstructor)
            where E : IdentifiableEntity
        {
            var prov = ((DbQueryProvider)query.Provider);

            return prov.Update(query, null, updateConstructor, cm => cm.ToPreCommand(), removeSelectRowCount: true);
        }

        public static void UpdateToStrings<T>() where T : IdentifiableEntity, new()
        {
            SafeConsole.WriteLineColor(ConsoleColor.Blue, "Saving toStr for {0}".Formato(typeof(T).TypeName()));

            if (!Database.Query<T>().Any())
                return;

            int min = Database.Query<T>().Min(a => a.Id);
            int max = Database.Query<T>().Max(a => a.Id);

            min.To(max + 1, 100).ProgressForeach(id => id.ToString(), null, (i, writer) =>
            {
                var list = Database.Query<T>().Where(a => i <= a.Id && a.Id < i + 100).ToList();

                foreach (var item in list)
                {
                    if (item.ToString() != item.toStr)
                        item.InDB().UnsafeUpdate(a => new T { toStr = item.ToString() });
                }
            });
        }

        public static void UpdateToStrings<T>(Expression<Func<T, string>> expression) where T : IdentifiableEntity, new()
        {
            SafeConsole.WriteLineColor(ConsoleColor.Gray, "UnsafeUpdate toStr for {0}".Formato(typeof(T).TypeName()));

            int result = Database.Query<T>().UnsafeUpdate(a => new T { toStr = expression.Evaluate(a) });

            Console.WriteLine("{0} {1} updated".Formato(result, typeof(T).TypeName()));
        }
    }
}
