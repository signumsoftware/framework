using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine;
using System.Data;
using Signum.Engine.Maps;
using Signum.Utilities;
using Signum.Entities;
using Signum.Engine.SchemaInfoTables;

namespace Signum.Engine
{
    internal static class SchemaComparer
    {
        public static SqlPreCommand SynchronizeSchema(Replacements replacements)
        {

            var rawTables = (from t in Database.View<SchemaTables>()
                             where t.TABLE_TYPE == "BASE TABLE"
                             join c in Database.View<SchemaColumns>().DefaultIfEmpty() on t.TABLE_NAME equals c.TABLE_NAME
                             join kc in Database.View<SchemaKeyColumnUsage>().DefaultIfEmpty() on new { c.TABLE_NAME, c.COLUMN_NAME } equals new { kc.TABLE_NAME, kc.COLUMN_NAME }
                             join k in Database.View<SchemaTableConstraints>().DefaultIfEmpty() on kc.CONSTRAINT_NAME equals k.CONSTRAINT_NAME
                             select new
                             {
                                 t.TABLE_NAME,
                                 c.COLUMN_NAME,
                                 c.DATA_TYPE,
                                 c.IS_NULLABLE,
                                 c.CHARACTER_MAXIMUM_LENGTH,
                                 c.NUMERIC_SCALE,
                                 c.NUMERIC_PRECISION,
                                 kc.CONSTRAINT_NAME,
                                 k.CONSTRAINT_TYPE,
                             }).ToList();


            var database = (from t in rawTables
                            group t by t.TABLE_NAME into g
                            select new DiffTable
                            {
                                Name = g.Key,
                                Colums = g.Select(c => new DiffColumn
                                {
                                    Name = c.COLUMN_NAME,
                                    DbType = ToSqlDbType(c.DATA_TYPE),
                                    Nullable = c.IS_NULLABLE == "YES",
                                    Size = c.CHARACTER_MAXIMUM_LENGTH ?? c.NUMERIC_PRECISION,
                                    Scale = c.NUMERIC_SCALE,
                                    PrimaryKey = c.CONSTRAINT_TYPE == "PRIMARY KEY",
                                    ForeingKeyName = c.CONSTRAINT_TYPE == "FOREIGN KEY" ? c.CONSTRAINT_NAME : null,
                                }).ToDictionary(c => c.Name),
                            }).ToDictionary(c => c.Name);

            var rawIndexes = (from t in Database.View<SysTables>()
                              join c in Database.View<SysColumns>().DefaultIfEmpty() on t.object_id equals c.object_id
                              join ic in Database.View<SysIndexColumn>().DefaultIfEmpty() on new { t.object_id, c.column_id } equals new { ic.object_id, ic.column_id }
                              join i in Database.View<SysIndexes>().DefaultIfEmpty() on new { ic.index_id, ic.object_id } equals new { i.index_id, i.object_id }
                              select new
                              {
                                  Table = t.name,
                                  ColumnName = c.name,
                                  Unique = (bool?)i.is_unique,
                                  Identity = SqlMethods.ColumnProperty(t.object_id, c.name, "IsIdentity") == 1,
                                  IndexName = i.name
                              }).ToList();

            rawIndexes = rawIndexes.GroupBy(a => a.IndexName).Where(g => g.Count() < 2).SelectMany(a => a).ToList(); // remove multiple indexes 

            var groups = rawIndexes.AgGroupToDictionary(a => a.Table, g => g.ToDictionary(a => a.ColumnName, a => new { a.Unique, a.IndexName, a.Identity }));


            database.JoinDictionaryForeach(groups, (table, d1, d2) =>
                 d1.Colums.JoinDictionaryForeach(d2, (column, col, bla) =>
                    {
                        col.Index = bla.Unique == null ? Index.None :
                                    bla.Unique == true ? Index.Unique : Index.Multiple;
                        col.IndexName = bla.IndexName;
                        col.Identity = bla.Identity;
                    }));

            var model = Schema.Current.Tables.Values.SelectMany(t => t.Fields.Values.Select(a=>a.Field).OfType<FieldMList>().Select(f => (ITable)f.RelationalTable).PreAnd(t)).ToDictionary(a => a.Name);

            //use database without replacements to just remove indexes
            SqlPreCommand dropIndices =
                 Synchronizer.SyncronizeCommands(database, model,
                 (tn, table) => table.Colums.Values.Select(c => c.IndexName != null && !c.PrimaryKey ? SqlBuilder.DropIndex(table.Name, c.IndexName) : null).Combine(Spacing.Simple),
                 null,
                 (tn, dif, tab) => Synchronizer.SyncronizeCommands(dif.Colums, tab.Columns,
                     (cn, col) => col.IndexName != null ? SqlBuilder.DropIndex(tn, col.IndexName) : null, 
                     null,
                     (cn, coldb, colModel) => coldb.EqualsIndex(tn, colModel) || coldb.IndexName == null ? null : SqlBuilder.DropIndex(tn, coldb.IndexName), 
                     Spacing.Simple),
                 Spacing.Double);

            SqlPreCommand dropForeignKeys =
                 Synchronizer.SyncronizeCommands(database, model, 
                 (tn, table) => table.Colums.Values.Select(c => c.ForeingKeyName != null ? SqlBuilder.AlterTableDropForeignKey(table.Name, c.ForeingKeyName) : null).Combine(Spacing.Simple),
                 null,
                 (tn, dif, tab) => Synchronizer.SyncronizeCommands(dif.Colums, tab.Columns,
                     (cn, col) => col.ForeingKeyName != null ? SqlBuilder.AlterTableDropForeignKey(tn, col.ForeingKeyName) : null,
                     null,
                     (cn, coldb, colModel) => coldb.EqualForeignKey(tn, colModel) || coldb.ForeingKeyName == null ? null : SqlBuilder.AlterTableDropForeignKey(tn, coldb.ForeingKeyName),
                     Spacing.Simple),
                 Spacing.Double);

            SqlPreCommand tables =
                Synchronizer.SyncronizeReplacing(replacements, Replacements.KeyTables,
                database,
                model,
                (tn, dif) => SqlBuilder.DropTable(tn),
                (tn, tab) => SqlBuilder.CreateTableSql(tab),
                (tn, dif, tab) =>
                    SqlPreCommand.Combine(Spacing.Simple,
                    dif.Name != tab.Name ? SqlBuilder.RenameTable(dif.Name, tab.Name) : null,
                    Synchronizer.SyncronizeReplacing(replacements, Replacements.KeyColumnsForTable(tn),
                    dif.Colums,
                    tab.Columns,
                    (cn, difCol) => SqlBuilder.AlterTableDropColumn(tn, cn),
                    (cn, tabCol) => SqlBuilder.AlterTableAddColumn(tn, tabCol),
                    (cn, difCol, tabCol) =>
                        SqlPreCommand.Combine(Spacing.Simple,
                        difCol.Name == tabCol.Name ? null : SqlBuilder.RenameColumn(tn, difCol.Name, tabCol.Name),
                        difCol.Equals(tabCol) ? null : SqlBuilder.AlterTableAlterColumn(tn, tabCol)),
                    Spacing.Simple)), Spacing.Double);

            replacements[Replacements.KeyTablesInverse] = replacements.TryGetC(Replacements.KeyTables).TryCC(d => d.Inverse());

            SqlPreCommand addForeingKeys =
                 Synchronizer.SyncronizeCommands(database, model,
                 null,
                 (tn, table) => SqlBuilder.AlterTableForeignKeys(table),
                 (tn, dif, tab) => Synchronizer.SyncronizeCommands(dif.Colums, tab.Columns,
                     null,
                     (cn, colModel) => colModel.ReferenceTable != null ? SqlBuilder.AlterTableAddForeignKey(tn, colModel.Name, colModel.ReferenceTable.Name) : null,
                     (cn, coldb, colModel) => coldb.EqualForeignKey(tn, colModel) || colModel.ReferenceTable == null ? null : SqlBuilder.AlterTableAddForeignKey(tn, colModel.Name, colModel.ReferenceTable.Name),
                     Spacing.Simple),
                 Spacing.Double);

            SqlPreCommand addIndices =
                 Synchronizer.SyncronizeCommands(database, model, 
                 null,
                 (tn, table) => SqlBuilder.CreateIndicesSql(table), 
                 (tn, dif, tab) => Synchronizer.SyncronizeCommands(dif.Colums, tab.Columns,
                     null,
                     (cn, colModel) => colModel.Index != Index.None ? SqlBuilder.CreateIndex(colModel.Index, tn, colModel.Name) : null,
                     (cn, coldb, colModel) => coldb.EqualsIndex(tn, colModel) || colModel.Index == Index.None ? null : SqlBuilder.CreateIndex(colModel.Index, tn, colModel.Name),
                     Spacing.Simple),
                 Spacing.Double);

            return SqlPreCommand.Combine(Spacing.Triple, dropIndices, dropForeignKeys, tables, addForeingKeys, addIndices);
        }

        public static SqlDbType ToSqlDbType(string str)
        {
            if(str == "numeric")
                return SqlDbType.Decimal;

            return str.ToEnum<SqlDbType>(true);
        }

        static SqlPreCommand SyncronizeTables(Dictionary<string, DiffTable> database, Dictionary<string, ITable> model, Func<string, DiffTable, SqlPreCommand> dropTable, Func<string, ITable, SqlPreCommand> createTable, Func<string, DiffColumn, SqlPreCommand> dropColumn, Func<string, IColumn, SqlPreCommand> createColumn, Func<string, DiffColumn, IColumn, SqlPreCommand> mergeColumn)
        {
            return Synchronizer.SyncronizeCommands(database, model, dropTable, createTable, (tn, dif, tab) =>
                Synchronizer.SyncronizeCommands(dif.Colums, tab.Columns, dropColumn, createColumn, mergeColumn, Spacing.Simple),
                Spacing.Double);
        }
    }

    internal class DiffTable
    {
        public string Name;
        public Dictionary<string, DiffColumn> Colums;
    }

    internal class DiffColumn : IEquatable<IColumn>
    {
        public string Name;
        public SqlDbType DbType;
        public bool Nullable;
        public int? Size;
        public int? Scale;
        public bool Identity;
        public bool PrimaryKey;
        public string ForeingKeyName; 
        
        public Index Index;
        public string IndexName;

        public bool Equals(IColumn other)
        {
            var result = 
                   DbType == other.SqlDbType
                && Nullable == other.Nullable
                && NullOrEqual(Size == -1? int.MaxValue: Size, other.Size)
                && NullOrEqual(Scale,other.Scale) 
                && Identity == other.Identity
                && PrimaryKey == other.PrimaryKey;

            return result;
        }

        private bool NullOrEqual(int? a, int? b)
        {
            return a == null || b == null || a.Value == b.Value; 
        }

        internal bool EqualsIndex(string tableName, IColumn colModel)
        {
            if (colModel.Index == Index.UniqueMultiNulls)
                return true;

            if (PrimaryKey && colModel.PrimaryKey)
                return true; 

            if (this.Index != colModel.Index)
                return false;

            string otherName = colModel.Index == Index.None ? null : SqlBuilder.IndexName(tableName, colModel.Name);

            return this.IndexName == otherName;
        }

        internal bool EqualForeignKey(string tableName, IColumn colModel)
        {
            return ForeingKeyName == colModel.ReferenceTable.TryCC(rt => SqlBuilder.ForeignKeyName(tableName, colModel.Name));  
        }
    }
}
