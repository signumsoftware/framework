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
            Connector.Current.CleanDatabase();

            SqlPreCommandConcat totalScript = (SqlPreCommandConcat)Schema.Current.GenerationScipt();
            foreach (SqlPreCommand command in totalScript.Commands)
            {
                command.ExecuteLeaves();
            }
        }

        public static bool ExistTable<T>()
            where T : IdentifiableEntity
        {
            Table table = Schema.Current.Table<T>();
            return ExistTable(table.Prefix, table.Name);
        }

        public static bool ExistTable(Type type)
        {
            Table table = Schema.Current.Table(type);
            return ExistTable(table.Prefix, table.Name);
        }


        public static bool ExistTable(SchemaName schema, string tableName)
        {
            if (schema == null)
                return Database.View<SysTables>().Any(a => a.name == tableName);


            if (schema.Database != null && schema.Database.Server != null && !Database.View<SysServers>().Any(ss => ss.name == schema.Database.Server.Name))
                return false;

            if (schema.Database != null && !Database.View<SysDatabases>().Any(ss => ss.name == schema.Database.Name))
                return false;

            using (Administrator.OverrideViewPrefix(prefix.ServerName, prefix.DatabaseName))
            {
                return (from t in Database.View<SysTables>()
                        join s in Database.View<SysSchemas>() on t.schema_id equals s.schema_id
                        select t).Any();
            }
        }

        static readonly ThreadVariable<DatabaseName> viewDatabase = Statics.ThreadVariable<DatabaseName>("viewDatabase");
        private static IDisposable OverrideViewPrefix(DatabaseName database)
        {
            var old = viewDatabase.Value;
            viewDatabase.Value = database;


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
            {
                if (ExistTable(table.Prefix, table.Name))
                {
                    return Database.RetrieveAll(type);
                }
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
            SqlBuilder.SetIdentityInsert(table.PrefixedName(), true).ExecuteNonQuery();

            return new Disposable(() =>
            {
                table.Identity = true;
                SqlBuilder.SetIdentityInsert(table.PrefixedName(), false).ExecuteNonQuery();
            });
        }

        public static IDisposable DisableIdentity(string tableName)
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
    }
}
