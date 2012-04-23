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
            where T: IdentifiableEntity
        {
            Table table = Schema.Current.Table<T>();
            return ExistTable(table.Name);
        }

        public static bool ExistTable(Type type)
        {
            Table table = Schema.Current.Table(type);
            return ExistTable(table.Name);
        }

        public static bool ExistTable(string tableName)
        {
            return Database.View<SysTables>().Any(a => a.name == tableName);
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
                if (ExistTable(table.Name))
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

        public static SqlPreCommand CreateTablesScript()
        {
            IEnumerable<ITable> tables = Schema.Current.GetDatabaseTables().Values;
                
            SqlPreCommand createTables = tables.Select(t => SqlBuilder.CreateTableSql(t)).Combine(Spacing.Double);

            SqlPreCommand foreignKeys = tables.Select(t => SqlBuilder.AlterTableForeignKeys(t)).Combine(Spacing.Double);

            SqlPreCommand indices = tables.Select(t => SqlBuilder.CreateAllIndices(t)).NotNull().Combine(Spacing.Double);

            return SqlPreCommand.Combine(Spacing.Triple, createTables, foreignKeys, indices);
        }
     
        public static SqlPreCommand InsertEnumValuesScript()
        {
            return (from t in Schema.Current.Tables.Values
                    let enumType = Reflector.ExtractEnumProxy(t.Type)
                    where enumType != null
                    select (from ie in EnumProxy.GetEntities(enumType)
                            select t.InsertSqlSync(ie)).Combine(Spacing.Simple)).Combine(Spacing.Double);
        }



        public static SqlPreCommand TotalSynchronizeScript()
        {
            return Schema.Current.SynchronizationScript(Connector.Current.DatabaseName()); 
        }

        public static SqlPreCommand SynchronizeSchemaScript(Replacements replacements)
        {
            return SchemaComparer.SynchronizeSchema(replacements);
        }

        public static SqlPreCommand SynchronizeEnumsScript(Replacements replacements)
        {
            Schema schema = Schema.Current;

            List<SqlPreCommand> commands = new List<SqlPreCommand>();

            foreach (var table in schema.Tables.Values)
            {
                Type enumType = Reflector.ExtractEnumProxy(table.Type);
                if (enumType != null)
                {
                    var should =  EnumProxy.GetEntities(enumType);
                    var current = Administrator.TryRetrieveAll(table.Type, replacements);

                    SqlPreCommand com = Synchronizer.SynchronizeScript(
                        current.ToDictionary(c => c.Id),
                        should.ToDictionary(s => s.Id),
                        (id, c) => table.DeleteSqlSync(c),
                        (id, s) => table.InsertSqlSync(s),
                        (id, c, s) => table.UpdateSqlSync(c),
                        Spacing.Simple);

                    commands.Add(com);
                }
            }

            return SqlPreCommand.Combine(Spacing.Double, commands.ToArray());
        }

        public static T SetId<T>(this T ident, int id)
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

        public static T SetNew<T>(T ident)
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
            table.Identity = false;
            SqlBuilder.SetIdentityInsert(table.Name, true).ExecuteNonQuery();

            return new Disposable(() =>
            {
                table.Identity = true;
                SqlBuilder.SetIdentityInsert(table.Name, false).ExecuteNonQuery();
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
            where T:IdentifiableEntity
        {
              using (Transaction tr = new Transaction())
              using (Administrator.DisableIdentity<T>())
              {
                  Database.SaveList(entities); 
                  tr.Commit(); 
              }
        }

        public static void SetSnapshotIsolation(bool value)
        {
            Executor.ExecuteNonQuery(SqlBuilder.SetSnapshotIsolation(Connector.Current.DatabaseName(), value));
        }

        public static void MakeSnapshotIsolationDefault(bool value)
        {
            Executor.ExecuteNonQuery(SqlBuilder.MakeSnapshotIsolationDefault(Connector.Current.DatabaseName(), value));
        }

        public static SqlPreCommand RenameFreeIndexesScript()
        {
            return SchemaComparer.RenameFreeIndexes();
        }

        public static int RemoveDuplicates<T, S>(Expression<Func<T, S>> key)
           where T : IdentifiableEntity
        {
            return (from f1 in Database.Query<T>()
                    join f2 in Database.Query<T>() on key.Evaluate(f1) equals key.Evaluate(f2)
                    where f1.Id > f2.Id
                    select f1).UnsafeDelete();
        }
    }
}
