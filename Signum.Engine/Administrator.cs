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

namespace Signum.Engine
{
    public static class Administrator
    {      
        public static void TotalGeneration()
        {
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


        #region Remove All

        public static readonly SqlPreCommandSimple RemoveAllConstraintsScript = new SqlPreCommandSimple(
@"declare @schema nvarchar(128), @tbl nvarchar(128), @constraint nvarchar(128) 
DECLARE @sql nvarchar(255) 

declare cur cursor fast_forward for 
select distinct cu.constraint_schema, cu.table_name, cu.constraint_name 
from information_schema.table_constraints tc 
join information_schema.referential_constraints rc on rc.unique_constraint_name = tc.constraint_name 
join information_schema.constraint_column_usage cu on cu.constraint_name = rc.constraint_name 
open cur 
    fetch next from cur into @schema, @tbl, @constraint 
    while @@fetch_status <> -1 
    begin 
        select @sql = 'ALTER TABLE ' + @schema + '.' + @tbl + ' DROP CONSTRAINT ' + @constraint 
        exec sp_executesql @sql 
        fetch next from cur into @schema, @tbl, @constraint 
    end 
close cur 
deallocate cur");

        public static readonly SqlPreCommandSimple RemoveAllTablesScript = new SqlPreCommandSimple(
@"declare @schema nvarchar(128), @tbl nvarchar(128)
DECLARE @sql nvarchar(255)
 
declare cur cursor fast_forward for 
select distinct table_schema, table_name
from information_schema.tables where table_type = 'BASE TABLE'
open cur 
    fetch next from cur into @schema, @tbl
    while @@fetch_status <> -1 
    begin 
        select @sql = 'DROP TABLE ' + @schema + '.' + @tbl + ';'
        exec sp_executesql @sql 
        fetch next from cur into @schema, @tbl
    end 
close cur 
deallocate cur");

        public static readonly SqlPreCommandSimple RemoveAllViewsScript = new SqlPreCommandSimple(
@"declare @schema nvarchar(128), @view nvarchar(128)
DECLARE @sql nvarchar(255) 

declare cur cursor fast_forward for 
select distinct table_schema, table_name
from information_schema.tables where table_type = 'VIEW'
open cur 
    fetch next from cur into @schema, @view
    while @@fetch_status <> -1 
    begin 
        select @sql = 'DROP VIEW ' + @schema + '.' + @view + ';'
        exec sp_executesql @sql 
        fetch next from cur into @schema, @view
    end 
close cur 
deallocate cur");

        public static readonly SqlPreCommandSimple RemoveAllProceduresScript = new SqlPreCommandSimple(
@"declare @schema nvarchar(128), @proc nvarchar(128), @type nvarchar(128)
DECLARE @sql nvarchar(255) 

declare cur cursor fast_forward for 
select routine_schema, routine_name, routine_type
from information_schema.routines
open cur 
    fetch next from cur into @schema, @proc, @type
    while @@fetch_status <> -1 
    begin 
        select @sql = 'DROP '+ @type +' ' + @schema + '.' + @proc + ';'
        exec sp_executesql @sql 
        fetch next from cur into @schema, @proc, @type
    end 
close cur 
deallocate cur");

        public static SqlPreCommand RemoveAllScript()
        {
            return SqlPreCommand.Combine(Spacing.Double, RemoveAllProceduresScript, RemoveAllViewsScript, RemoveAllConstraintsScript, RemoveAllTablesScript);
        }

        public static SqlPreCommand ShrinkDataBase()
        {
            return SqlBuilder.ShrinkDatabase(ConnectionScope.Current.DatabaseName());
        }

        #endregion

        public static SqlPreCommand TotalSynchronizeScript()
        {
            return Schema.Current.SynchronizationScript(ConnectionScope.Current.DatabaseName()); 
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

        public static T SetId<T>(int id, T ident)
            where T:IdentifiableEntity
        {
            ident.id = id;
            return ident; 
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
            Executor.ExecuteNonQuery(SqlBuilder.SetSnapshotIsolation(ConnectionScope.Current.DatabaseName(), value));
        }

        public static void MakeSnapshotIsolationDefault(bool value)
        {
            Executor.ExecuteNonQuery(SqlBuilder.MakeSnapshotIsolationDefault(ConnectionScope.Current.DatabaseName(), value));
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
