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
using Signum.Engine.Properties;

namespace Signum.Engine
{
    public static class SchemaComparer
    {
        static readonly Dictionary<string, DiffViewIndex> defaultIndexes = new Dictionary<string, DiffViewIndex>();

        public static Func<Dictionary<string, DiffTable>> GetDatabaseDescription = DefaultGetDatabaseDescription;

        public static SqlPreCommand SynchronizeSchema(Replacements replacements)
        {
            Dictionary<string, DiffTable> database = GetDatabaseDescription();

            Dictionary<string, ITable> model = Schema.Current.GetDatabaseTables(); 
                
               

            Dictionary<ITable, Dictionary<string, UniqueIndex>> modelIndices = model.Values.ToDictionary(t => t, t =>t.GeneratUniqueIndexes().ToDictionary(a=>a.IndexName, "Indexes for {0}".Formato(t.Name)));

            //use database without replacements to just remove indexes
            SqlPreCommand dropIndices =
                 Synchronizer.SynchronizeScript(database, model,
                 (tn, dif) => dif.Indices.Values.Select(dix=> dix.ViewName == null? SqlBuilder.DropIndex(tn, dix.IndexName): 
                                                                                SqlBuilder.DropViewIndex(dix.ViewName, dix.IndexName)).Concat(
                              dif.FreeIndices.Select(v=> SqlBuilder.DropIndex(tn, v.IndexName))).Combine(Spacing.Simple),
                 null,
                 (tn, dif, tab) => 
                     {
                         var removeIndexes = Synchronizer.SynchronizeScript(dif.Indices ?? defaultIndexes, modelIndices[tab],
                                    (i, dix) => dix.ViewName == null? SqlBuilder.DropIndex(tn, dix.IndexName): 
                                                                      SqlBuilder.DropViewIndex(dix.ViewName, dix.IndexName), null, null, Spacing.Simple);
                     
                         List<string> removedOrRenamedColumns = dif.Colums.Keys.Except(tab.Columns.Keys).ToList();

                         var removeFreeIndexes = dif.FreeIndices == null? null : 
                             (from i in dif.FreeIndices
                              where i.Columns.Any(c => removedOrRenamedColumns.Contains(c))
                              select i.Columns.All(c => removedOrRenamedColumns.Contains(c)) ?
                                 SqlBuilder.DropIndex(tn, i.IndexName) :
                                 SqlBuilder.DropIndexCommented(tn, i.IndexName)).Combine(Spacing.Simple);

                         return new[] { removeIndexes, removeFreeIndexes }.Combine(Spacing.Double);
                     },
                 Spacing.Double);

            SqlPreCommand dropForeignKeys =
                 Synchronizer.SynchronizeScript(database, model, 
                 (tn, dif) => dif.Colums.Values.Select(c => c.ForeingKeyName != null ? SqlBuilder.AlterTableDropConstraint(dif.Name, c.ForeingKeyName) : null).Combine(Spacing.Simple),
                 null,
                 (tn, dif, tab) => Synchronizer.SynchronizeScript(dif.Colums, tab.Columns,
                     (cn, col) => col.ForeingKeyName != null ? SqlBuilder.AlterTableDropConstraint(tn, col.ForeingKeyName) : null,
                     null,
                     (cn, coldb, colModel) => coldb.EqualForeignKey(tn, colModel) || coldb.ForeingKeyName == null ? null : SqlBuilder.AlterTableDropConstraint(tn, coldb.ForeingKeyName),
                     Spacing.Simple),
                 Spacing.Double);

            SqlPreCommand tables =
                Synchronizer.SynchronizeReplacing(replacements, Replacements.KeyTables,
                database,
                model,
                (tn, dif) => SqlBuilder.DropTable(tn),
                (tn, tab) => SqlBuilder.CreateTableSql(tab),
                (tn, dif, tab) =>
                    SqlPreCommand.Combine(Spacing.Simple,
                    dif.Name != tab.Name ? SqlBuilder.RenameTable(dif.Name, tab.Name) : null,
                    Synchronizer.SynchronizeReplacing(replacements, Replacements.KeyColumnsForTable(tn),
                    dif.Colums,
                    tab.Columns,
                    (cn, difCol) => SqlPreCommand.Combine(Spacing.Simple, 
                                    difCol.DefaultConstraintName.HasText() ?  SqlBuilder.AlterTableDropConstraint(tn, difCol.DefaultConstraintName) : null, 
                                    SqlBuilder.AlterTableDropColumn(tn, cn)),
                    (cn, tabCol) => SqlBuilder.AlterTableAddColumn(tn, tabCol),
                    (cn, difCol, tabCol) =>
                        SqlPreCommand.Combine(Spacing.Simple,
                            difCol.Name == tabCol.Name ? null : SqlBuilder.RenameColumn(tn, difCol.Name, tabCol.Name),
                            difCol.Equals(tabCol) ? null : SqlBuilder.AlterTableAlterColumn(tn, tabCol)),
                    Spacing.Simple)), Spacing.Double);

            var tableReplacements = replacements.TryGetC(Replacements.KeyTables);
            if (tableReplacements != null)
                replacements[Replacements.KeyTablesInverse] = tableReplacements.Inverse();

            SqlPreCommand addForeingKeys =
                 Synchronizer.SynchronizeScript(database, model,
                 null,
                 (tn, tab) => SqlBuilder.AlterTableForeignKeys(tab),
                 (tn, dif, tab) => Synchronizer.SynchronizeScript(dif.Colums, tab.Columns,
                     null,
                     (cn, colModel) => colModel.ReferenceTable != null ? SqlBuilder.AlterTableAddConstraintForeignKey(tn, colModel.Name, colModel.ReferenceTable.Name) : null,
                     (cn, coldb, colModel) => coldb.EqualForeignKey(tn, colModel) || colModel.ReferenceTable == null ? null : SqlBuilder.AlterTableAddConstraintForeignKey(tn, colModel.Name, colModel.ReferenceTable.Name),
                     Spacing.Simple),
                 Spacing.Double);


            SqlPreCommand addIndices =
                 Synchronizer.SynchronizeScript(database, model, 
                 null,
                 (tn, tab) => SqlBuilder.CreateAllIndices(tab, modelIndices[tab].Values), 
                 (tn, dif, tab) => 
                     {
                         var createIndexes = Synchronizer.SynchronizeScript(dif.Indices ?? defaultIndexes, modelIndices[tab], null,
                                    (i, index) => SqlBuilder.CreateUniqueIndex(index), null, Spacing.Simple);
                         
                         var createFreeIndexes = Synchronizer.SynchronizeScript(dif.Colums, tab.Columns, null, 
                             (cn, col)=>col.ReferenceTable == null? null: SqlBuilder.CreateMultipleIndex(tab, col), null, Spacing.Simple);
                             
                         return new[] { createIndexes, createFreeIndexes }.Combine(Spacing.Double);
                     },
                 Spacing.Double);

            return SqlPreCommand.Combine(Spacing.Triple, dropIndices, dropForeignKeys, tables, addForeingKeys, addIndices);
        }

        public static Dictionary<string, DiffTable> DefaultGetDatabaseDescription()
        {
            var database = (from t in Database.View<SysTables>()
                            join s in Database.View<SysSchemas>() on t.schema_id equals s.schema_id
                            where !Database.View<SysExtendedProperties>().Any(a => a.major_id == t.object_id && a.name == "microsoft_database_tools_support")
                            select new DiffTable
                            {
                                Name = t.name,
                                Schema = s.name,
                                Colums = (from c in Database.View<SysColumns>().Where(c => c.object_id == t.object_id)
                                          join type in Database.View<SysTypes>() on c.user_type_id equals type.user_type_id
                                          join ctr in Database.View<SysObjects>().DefaultIfEmpty() on c.default_object_id equals ctr.object_id
                                          select new DiffColumn
                                          {
                                              Name = c.name,
                                              DbType = ToSqlDbType(type.name),
                                              Nullable = c.is_nullable,
                                              Length = c.max_length,
                                              Precission = c.precision,
                                              Scale = c.scale,
                                              Identity = c.is_identity,
                                              DefaultConstraintName = ctr.name,
                                              PrimaryKey = (from i in Database.View<SysIndexes>()
                                                            where i.object_id == c.object_id && i.is_primary_key
                                                            join ic in Database.View<SysIndexColumn>() on new { i.object_id, i.index_id } equals new { ic.object_id, ic.index_id }
                                                            where ic.column_id == c.column_id
                                                            select i.name).Any(),
                                              ForeingKeyName = (from fkc in Database.View<SysForeignKeyColumns>()
                                                                where fkc.parent_object_id == c.object_id && fkc.parent_column_id == c.column_id
                                                                join fk in Database.View<SysForeignKeys>().DefaultIfEmpty() on fkc.constraint_object_id equals fk.object_id
                                                                select fk.name).SingleOrDefaultEx(),
                                          }).ToDictionary(a => a.Name),

                                Indices = (from i in Database.View<SysIndexes>()
                                           where i.object_id == t.object_id && !i.is_primary_key && i.is_unique && i.name.StartsWith("IX_")
                                           select new DiffViewIndex { IndexName = i.name }).ToList().Concat(
                                           (from v in Database.View<SysViews>()
                                            where v.name.StartsWith("VIX_" + t.name + "_")
                                            join i in Database.View<SysIndexes>() on v.object_id equals i.object_id
                                            where !i.is_primary_key && i.is_unique && i.name.StartsWith("IX_")
                                            select new DiffViewIndex { IndexName = i.name, ViewName = v.name }).ToList()).ToDictionary(a => a.IndexName),


                                FreeIndices = (from i in Database.View<SysIndexes>()
                                               where i.object_id == t.object_id && !i.is_primary_key && !(i.is_unique && i.name.StartsWith("IX_"))
                                               select new DiffFreeIndex
                                               {
                                                   IndexName = i.name,
                                                   Columns = (from ic in Database.View<SysIndexColumn>().Where(ic => ic.object_id == t.object_id && ic.index_id == i.index_id)
                                                              join c in Database.View<SysColumns>().Where(ic => ic.object_id == t.object_id)
                                                                 on ic.column_id equals c.column_id
                                                              select c.name).ToList()
                                               }).ToList()
                            }).ToDictionary(c => c.Name);

            return database;
        }


        public static SqlDbType ToSqlDbType(string str)
        {
            if(str == "numeric")
                return SqlDbType.Decimal;

            return str.ToEnum<SqlDbType>(true);
        }

        static SqlPreCommand SyncronizeTables(Dictionary<string, DiffTable> database, Dictionary<string, ITable> model, Func<string, DiffTable, SqlPreCommand> dropTable, Func<string, ITable, SqlPreCommand> createTable, Func<string, DiffColumn, SqlPreCommand> dropColumn, Func<string, IColumn, SqlPreCommand> createColumn, Func<string, DiffColumn, IColumn, SqlPreCommand> mergeColumn)
        {
            return Synchronizer.SynchronizeScript(database, model, dropTable, createTable, (tn, dif, tab) =>
                Synchronizer.SynchronizeScript(dif.Colums, tab.Columns, dropColumn, createColumn, mergeColumn, Spacing.Simple),
                Spacing.Double);
        }

        public static SqlPreCommand RenameFreeIndexes()
        {
            var indices = (from t in Database.View<SysTables>()
                           join i in Database.View<SysIndexes>() on t.object_id equals i.object_id
                           where !i.is_primary_key && !i.is_unique && i.name.StartsWith("IX_")
                           select new
                           {
                               Table = t.name,
                               Index = i.name,
                           }).ToList();

            return indices.Select(a => SqlBuilder.RenameIndex(a.Table, a.Index, "F" + a.Index)).Combine(Spacing.Simple);  
        }
    }

    public class DiffTable
    {
        public string Name;
        public string Schema;
        public Dictionary<string, DiffColumn> Colums;

        public Dictionary<string, DiffViewIndex> Indices;
        public List<DiffFreeIndex> FreeIndices;
    }

    public class DiffFreeIndex
    {
        public string IndexName; 
        public List<string> Columns;

        public override string ToString()
        {
            return "{0} ({1})".Formato(IndexName, Columns.ToString(", "));
        }
    }

    public class DiffViewIndex
    {
        public string IndexName; 
        public string ViewName;

        public override string ToString()
        {
            if (ViewName == null)
                return IndexName;
            else
                return "{0} + {1}".Formato(ViewName, IndexName); 
        }
    }

    public class DiffColumn : IEquatable<IColumn>
    {
        public string Name;
        public SqlDbType DbType;
        public bool Nullable;
        public int Length; 
        public int Precission;
        public int Scale;
        public bool Identity;
        public bool PrimaryKey;

        public string ForeingKeyName; 

        public string DefaultConstraintName;

        public bool Equals(IColumn other)
        {
            var result = 
                   DbType == other.SqlDbType
                && Nullable == other.Nullable
                && (other.Size == null || other.Size.Value == Precission || other.Size.Value == Length / 2 || other.Size.Value == int.MaxValue && Length == -1)
                && (other.Scale == null || other.Scale.Value == Scale) 
                && Identity == other.Identity
                && PrimaryKey == other.PrimaryKey;

            return result;
        }
 
        internal bool EqualForeignKey(string tableName, IColumn colModel)
        {
            return ForeingKeyName == colModel.ReferenceTable.TryCC(rt => SqlBuilder.ForeignKeyName(tableName, colModel.Name));  
        }
    }
}
