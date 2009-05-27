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
            return Database.View<SchemaTables>().Any(a => a.TABLE_NAME == tableName);
        }

        public static List<T> TryRetrieveAll<T>(Replacements replacements)
            where T : IdentifiableEntity
        {
            Table table = Schema.Current.Table<T>();

            using (Synchronizer.RenameTable(table, replacements))
            {
                if (ExistTable(table.Name))
                    return Database.RetrieveAll<T>();
                return new List<T>();
            }
        }


        public static List<IdentifiableEntity> TryRetrieveAll(Type type, Replacements replacements)
        {
            Table table = Schema.Current.Table(type);

            using (Synchronizer.RenameTable(table, replacements))
            {
                if (ExistTable(table.Name))
                    return Database.RetrieveAll(type);
                return new List<IdentifiableEntity>();
            }
        }

        public static void Initialize()
        {
            Schema.Current.Initialize();  
        }

        public static SqlPreCommand TotalGenerationScript()
        {
            return Schema.Current.GenerationScipt();
        }

        public static SqlPreCommand CreateTablesScript()
        {
            Schema schema = Schema.Current;

            SqlPreCommand createTables = schema.Tables.Select(t => SqlBuilder.CreateTableSql(t.Value)).Combine(Spacing.Double);

            SqlPreCommand foreignKeys = schema.Tables.Select(t => SqlBuilder.AlterTableForeignKeys(t.Value)).Combine(Spacing.Double);

            SqlPreCommand indices = schema.Tables.Select(t => SqlBuilder.CreateIndicesSql(t.Value)).NotNull().Combine(Spacing.Double);

            SqlPreCommand collectionTables = schema.Tables.Select(t => t.Value.CreateCollectionTables()).Combine(Spacing.Triple);

            return SqlPreCommand.Combine(Spacing.Triple, createTables, foreignKeys, indices, collectionTables);
        }

        public static SqlPreCommand InsertEnumValuesScript()
        {
            return (from t in Schema.Current.Tables.Values
                    let enumType = Reflector.ExtractEnumProxy(t.Type)
                    where enumType != null
                    select (from item in Enum.GetValues(enumType).Cast<object>()
                            let ei = EnumProxy.FromEnum((Enum)item).Do(e => e.PreSaving())
                            select t.InsertSqlSync(ei)).Combine(Spacing.Simple)).Combine(Spacing.Double);
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
@"declare @schema nvarchar(128), @tbl nvarchar(128)
DECLARE @sql nvarchar(255) 

declare cur cursor fast_forward for 
select distinct table_schema, table_name
from information_schema.tables where table_type = 'VIEW'
open cur 
    fetch next from cur into @schema, @tbl
    while @@fetch_status <> -1 
    begin 
        select @sql = 'DROP VIEW ' + @schema + '.' + @tbl + ';'
        exec sp_executesql @sql 
        fetch next from cur into @schema, @tbl
    end 
close cur 
deallocate cur");

        public static SqlPreCommand RemoveAllScript()
        {
            return SqlPreCommand.Combine(Spacing.Double, RemoveAllViewsScript, RemoveAllConstraintsScript, RemoveAllTablesScript);
        }
        #endregion

        #region MultiColumnIndex
        public static void AddMultiColumnIndex<T>(bool unique, params Expression<Func<T, object>>[] columns) where T : IdentifiableEntity
        {
            AddMultiColumnIndexScript<T>(unique, columns).ToSimple().ExecuteNonQuery();
        }

        public static SqlPreCommand AddMultiColumnIndexScript<T>(bool unique, params Expression<Func<T, object>>[] columns) where T : IdentifiableEntity
        {
            Schema schema = ConnectionScope.Current.Schema;

            return AddMultiColumnIndexScript(schema.Table<T>(), unique, columns.Select(fun => (IColumn)schema.Field<T>(fun)).ToArray());
        }

        public static void AddMultiColumnIndex(ITable table, bool unique, params IColumn[] columns)
        {
            AddMultiColumnIndexScript(table, unique, columns).ToSimple().ExecuteNonQuery();
        }

        public static SqlPreCommand AddMultiColumnIndexScript(ITable table, bool unique, params IColumn[] columns)
        {
            return SqlBuilder.CreateIndex(unique ? Index.Unique : Index.Multiple, table.Name, columns.Select(c => c.Name).ToArray());
        }
        #endregion

        public static SqlPreCommand TotalSynchronizeScript()
        {
            return Schema.Current.SynchronizationScript(); 
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
                    var should = Enum.GetValues(enumType).Cast<Enum>().Select(e => EnumProxy.FromEnum(e));
                    var current = Administrator.TryRetrieveAll(table.Type, replacements);

                    SqlPreCommand com = Synchronizer.SyncronizeCommands(
                        current.ToDictionary(c => c.Id),
                        should.ToDictionary(s => s.Id),
                        (id, c) => table.DeleteSqlSync(c.Id),
                        (id, s) =>
                        {
                            s.PreSaving();
                            return table.InsertSqlSync(s);
                        },
                        (id, c, s) =>
                        {
                            c.PreSaving();
                            return table.UpdateSqlSync(c);
                        }, Spacing.Simple);

                    commands.Add(com);
                }
            }

            return SqlPreCommand.Combine(Spacing.Double, commands.ToArray());
        }
    }
}
