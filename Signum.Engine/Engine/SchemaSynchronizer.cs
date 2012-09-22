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
    public static class SchemaSynchronizer
    {
        static readonly Dictionary<string, DiffViewIndex> defaultIndexes = new Dictionary<string, DiffViewIndex>();

        public static Func<Dictionary<string, DiffTable>> GetDatabaseDescription = DefaultGetDatabaseDescription;

        public static SqlPreCommand SynchronizeSchemaScript(Replacements replacements)
        {
            Dictionary<string, DiffTable> database = GetDatabaseDescription();

            Dictionary<string, ITable> model = Schema.Current.GetDatabaseTables().ToDictionary(a => a.Name);

            Dictionary<ITable, Dictionary<string, UniqueIndex>> modelIndices = model.Values.ToDictionary(t => t, t => t.GeneratUniqueIndexes().ToDictionary(a => a.IndexName, "Indexes for {0}".Formato(t.Name)));

            //use database without replacements to just remove indexes
            SqlPreCommand dropIndices = Synchronizer.SynchronizeScript(
                 model,
                 database,
                 null,
                 (tn, dif) => dif.Indices.Values.Select(dix => dix.ViewName == null ? SqlBuilder.DropIndex(tn, dix.IndexName) :
                                                                                SqlBuilder.DropViewIndex(dix.ViewName, dix.IndexName)).Concat(
                              dif.FreeIndices.Select(v => SqlBuilder.DropIndex(tn, v.IndexName))).Combine(Spacing.Simple),
                 (tn, dif, tab) =>
                 {
                     var removeIndexes = Synchronizer.SynchronizeScript(modelIndices[tab], dif.Indices ?? defaultIndexes, null, (i, dix) => dix.ViewName == null ? SqlBuilder.DropIndex(tn, dix.IndexName) :
                                                                  SqlBuilder.DropViewIndex(dix.ViewName, dix.IndexName), null, Spacing.Simple);

                     List<string> removedOrRenamedColumns = dif.Colums.Keys.Except(tab.Columns.Keys).ToList();

                     var removeFreeIndexes = dif.FreeIndices == null ? null :
                         (from i in dif.FreeIndices
                          where i.Columns.Any(c => removedOrRenamedColumns.Contains(c))
                          select i.Columns.All(c => removedOrRenamedColumns.Contains(c)) ?
                             SqlBuilder.DropIndex(tn, i.IndexName) :
                             SqlBuilder.DropIndexCommented(tn, i.IndexName)).Combine(Spacing.Simple);

                     return new[] { removeIndexes, removeFreeIndexes }.Combine(Spacing.Double);
                 },
                 Spacing.Double);

            SqlPreCommand dropForeignKeys = Synchronizer.SynchronizeScript(
                 model,
                 database,
                 null,
                 (tn, dif) => dif.Colums.Values.Select(c => c.ForeingKey != null ? SqlBuilder.AlterTableDropConstraint(dif.Name, c.ForeingKey.Name) : null).Combine(Spacing.Simple),
                 (tn, dif, tab) => Synchronizer.SynchronizeScript(
                     tab.Columns,
                     dif.Colums,
                     null,
                     (cn, col) => col.ForeingKey != null ? SqlBuilder.AlterTableDropConstraint(tn, col.ForeingKey.Name) : null,
                     (cn, coldb, colModel) => coldb.ForeingKey == null || coldb.ForeingKey.EqualForeignKey(tn, colModel) ? null :
                        SqlBuilder.AlterTableDropConstraint(tn, coldb.ForeingKey.Name), Spacing.Simple),
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
                                    difCol.DefaultConstraintName.HasText() ? SqlBuilder.AlterTableDropConstraint(tn, difCol.DefaultConstraintName) : null,
                                    SqlBuilder.AlterTableDropColumn(tn, cn)),
                    (cn, tabCol) => SqlBuilder.AlterTableAddColumn(tn, tabCol),
                    (cn, difCol, tabCol) =>
                        SqlPreCommand.Combine(Spacing.Simple,
                            difCol.Name == tabCol.Name ? null : SqlBuilder.RenameColumn(tn, difCol.Name, tabCol.Name),
                            difCol.Equals(tabCol) ? null : SqlBuilder.AlterTableAlterColumn(tn, tabCol)),
                            Spacing.Simple)),
                    Spacing.Double);

            var tableReplacements = replacements.TryGetC(Replacements.KeyTables);
            if (tableReplacements != null)
                replacements[Replacements.KeyTablesInverse] = tableReplacements.Inverse();

            SqlPreCommand addForeingKeys = Synchronizer.SynchronizeScript(
                 model,
                 database,
                 (tn, tab) => SqlBuilder.AlterTableForeignKeys(tab),
                 null,
                 (tn, dif, tab) => Synchronizer.SynchronizeScript(
                     tab.Columns,
                     dif.Colums,
                     (cn, colModel) => colModel.ReferenceTable != null ?
                         SqlBuilder.AlterTableAddConstraintForeignKey(tn, colModel.Name, colModel.ReferenceTable.Name) : null,
                     null,
                     (cn, coldb, colModel) => colModel.ReferenceTable != null && (coldb.ForeingKey == null || !coldb.ForeingKey.EqualForeignKey(tn, colModel)) ?
                         SqlBuilder.AlterTableAddConstraintForeignKey(tn, colModel.Name, colModel.ReferenceTable.Name) : null,
                     Spacing.Simple),
                 Spacing.Double);


            SqlPreCommand addIndices = Synchronizer.SynchronizeScript(
                 model,
                 database,
                 (tn, tab) => SqlBuilder.CreateAllIndices(tab, modelIndices[tab].Values), null, (tn, dif, tab) =>
                 {
                     var createIndexes = Synchronizer.SynchronizeScript(
                         modelIndices[tab],
                         dif.Indices ?? defaultIndexes,
                         (i, index) => SqlBuilder.CreateUniqueIndex(index),
                         null,
                         null,
                         Spacing.Simple);

                     var createFreeIndexes = Synchronizer.SynchronizeScript(
                         tab.Columns,
                         dif.Colums,
                         (cn, col) => col.ReferenceTable == null ? null : SqlBuilder.CreateMultipleIndex(tab, col),
                         null,
                         null,
                         Spacing.Simple);

                     return new[] { createIndexes, createFreeIndexes }.Combine(Spacing.Double);
                 },
                 Spacing.Double);

            return SqlPreCommand.Combine(Spacing.Triple, dropIndices, dropForeignKeys, tables, addForeingKeys, addIndices);
        }

        public static Dictionary<string, DiffTable> DefaultGetDatabaseDescription()
        {
            var udttypes = Schema.Current.Settings.UdtSqlName.Values.ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            var database = (from s in Database.View<SysSchemas>()
                            from t in s.Tables()
                            where !t.ExtendedProperties().Any(a=>a.name == "microsoft_database_tools_support")
                            select new DiffTable
                            {
                                Name = t.name,
                                Schema = s.name,
                                Colums = (from c in t.Columns()
                                          join type in Database.View<SysTypes>() on c.user_type_id equals type.user_type_id
                                          join ctr in Database.View<SysObjects>().DefaultIfEmpty() on c.default_object_id equals ctr.object_id
                                          select new DiffColumn
                                          {
                                              Name = c.name,
                                              SqlDbType = udttypes.Contains(type.name) ? SqlDbType.Udt : ToSqlDbType(type.name),
                                              UdtTypeName = udttypes.Contains(type.name) ? type.name : null,
                                              Nullable = c.is_nullable,
                                              Length = c.max_length,
                                              Precission = c.precision,
                                              Scale = c.scale,
                                              Identity = c.is_identity,
                                              DefaultConstraintName = ctr.name,
                                              PrimaryKey = t.Indices().Any(i=> i.is_primary_key && i.IndexColumns().Any(ic=>ic.column_id == c.column_id)),
                                              ForeingKey = (from fk in t.ForeignKeys()
                                                            where fk.ForeignKeyColumns().Any(fkc => fkc.parent_column_id == c.column_id)
                                                            join rt in Database.View<SysTables>() on fk.referenced_object_id equals rt.object_id
                                                            select fk.name == null ? null: new DiffForeignKey { Name = fk.name, TargetTable = rt.name }).SingleOrDefaultEx(),
                                          }).ToDictionary(a => a.Name),

                                Indices = (from i in t.Indices()
                                           where !i.is_primary_key && i.is_unique && i.name.StartsWith("IX_")
                                           select new DiffViewIndex { IndexName = i.name }).ToList().Concat(
                                           (from v in Database.View<SysViews>()
                                            where v.name.StartsWith("VIX_" + t.name + "_")
                                            from i in v.Indices()
                                            where !i.is_primary_key && i.is_unique && i.name.StartsWith("IX_")
                                            select new DiffViewIndex { IndexName = i.name, ViewName = v.name }).ToList()).ToDictionary(a => a.IndexName),


                                FreeIndices = (from i in t.Indices() 
                                               where !i.is_primary_key && !(i.is_unique && i.name.StartsWith("IX_"))
                                               select new DiffFreeIndex
                                               {
                                                   IndexName = i.name,
                                                   Columns = (from ic in i.IndexColumns()
                                                              join c in t.Columns() on ic.column_id equals c.column_id
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

        public static SqlPreCommand SynchronizeEnumsScript(Replacements replacements)
        {
            Schema schema = Schema.Current;
            
            List<SqlPreCommand> commands = new List<SqlPreCommand>();

            foreach (var table in schema.Tables.Values)
            {
                Type enumType = EnumProxy.Extract(table.Type);
                if (enumType != null)
                {
                    var should = EnumProxy.GetEntities(enumType);
                    var shouldByName = should.ToDictionary(a => a.ToString());

                    var current = Administrator.TryRetrieveAll(table.Type, replacements);

                    Func<IdentifiableEntity, SqlPreCommand> updateRelatedTables = c =>
                    {
                        var s = shouldByName.TryGetC(c.toStr);

                        if (s == null || s.id == c.id)
                            return null;

                        var updates = (from t in schema.GetDatabaseTables()
                                       from col in t.Columns.Values
                                       where col.ReferenceTable == table
                                       select new SqlPreCommandSimple("REVIEW THIS! UPDATE {0} SET {1} = {2} WHERE {1} = {3} -- {4} re-indexed".Formato(
                                           t.Name, col.Name, s.Id, c.Id, c.toStr)))
                                           .Combine(Spacing.Simple);

                        return updates;
                    };

                    SqlPreCommand com = Synchronizer.SynchronizeScript(
                        should.ToDictionary(s => s.Id),
                        current.ToDictionary(c => c.Id),
                        (id, s) => table.InsertSqlSync(s),
                        (id, c) => SqlPreCommand.Combine(Spacing.Simple, updateRelatedTables(c), table.DeleteSqlSync(c, c.toStr)),
                        (id, c, s) => SqlPreCommand.Combine(Spacing.Simple, updateRelatedTables(c), table.UpdateSqlSync(c, c.toStr)),
                        Spacing.Double);

                    commands.Add(com);
                }
            }

            return SqlPreCommand.Combine(Spacing.Double, commands.ToArray());
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
        public SqlDbType SqlDbType;
        public string UdtTypeName; 
        public bool Nullable;
        public int Length; 
        public int Precission;
        public int Scale;
        public bool Identity;
        public bool PrimaryKey;

        public DiffForeignKey ForeingKey; 

        public string DefaultConstraintName;

        public bool Equals(IColumn other)
        {
            var result =
                   SqlDbType == other.SqlDbType
                && StringComparer.InvariantCultureIgnoreCase.Equals(UdtTypeName, other.UdtTypeName)
                && Nullable == other.Nullable
                && (other.Size == null || other.Size.Value == Precission || other.Size.Value == Length / 2 || other.Size.Value == int.MaxValue && Length == -1)
                && (other.Scale == null || other.Scale.Value == Scale)
                && Identity == other.Identity
                && PrimaryKey == other.PrimaryKey;

            return result;
        }
 
       
    }

    public class DiffForeignKey
    {
        public string Name;
        public string TargetTable;

        internal bool EqualForeignKey(string tableName, IColumn colModel)
        {
            if(colModel.ReferenceTable == null)
                return false;

            if (TargetTable != colModel.ReferenceTable.Name)
                return false;

            return Name == SqlBuilder.ForeignKeyName(tableName, colModel.Name);
        }
    }
}
