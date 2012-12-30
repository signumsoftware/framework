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
        public static SqlPreCommand SynchronizeSchemaScript(Replacements replacements, string serverName, string databaseName)
        {
            Dictionary<string, DiffTable> database = DefaultGetDatabaseDescription(serverName, databaseName);

            Dictionary<string, ITable> model = Schema.Current.GetDatabaseTables().ToDictionary(a => a.PrefixedName());

            Dictionary<ITable, Dictionary<string, UniqueIndex>> modelIndices = model.Values.ToDictionary(t => t, t => t.GeneratUniqueIndexes().ToDictionary(a => a.IndexName, "Indexes for {0}".Formato(t.Name)));

            //use database without replacements to just remove indexes
            SqlPreCommand dropIndices =
                Synchronizer.SynchronizeScript(model, database,
                 null,
                (tn, dif) => dif.Indices.Values.Select(ix => SqlBuilder.DropIndex(tn, ix)).Combine(Spacing.Simple),
                (tn, tab, dif) =>
                {
                    Dictionary<string, UniqueIndex> modelIxs = modelIndices[tab];

                    var changedColumns = ChangedColumns(dif, tab, null);

                    var controlledIndices = Synchronizer.SynchronizeScript(modelIxs, dif.Indices.Where(kvp => kvp.Value.IsControlledIndex).ToDictionary(),
                            null,
                        (i, dix) => SqlBuilder.DropIndex(tn, dix),
                        (i, mix, dix) => dix.Columns.Any(a => changedColumns[a] == ColumnAction.Changed) ? SqlBuilder.DropIndex(tn, dix) : null,
                        Spacing.Simple);

                    var freeIndexes = dif.Indices.Values.Where(dix => !dix.IsControlledIndex && dix.Columns.Any(a => changedColumns[a] != ColumnAction.Equals))
                         .Select(dix => SqlBuilder.DropIndex(tn, dix)).Combine(Spacing.Simple);

                    return SqlPreCommand.Combine(Spacing.Simple, controlledIndices, freeIndexes);
                },
                 Spacing.Double);

            SqlPreCommand dropForeignKeys = Synchronizer.SynchronizeScript(
                 model,
                 database,
                 null,
                 (tn, dif) => dif.Colums.Values.Select(c => c.ForeingKey != null ? SqlBuilder.AlterTableDropConstraint(dif.Name, c.ForeingKey.Name) : null).Combine(Spacing.Simple),
                 (tn, tab, dif) => Synchronizer.SynchronizeScript(
                     tab.Columns,
                     dif.Colums,
                     null,
                     (cn, colDb) => colDb.ForeingKey != null ? SqlBuilder.AlterTableDropConstraint(tn, colDb.ForeingKey.Name) : null,
                     (cn, colModel, colDb) => colDb.ForeingKey == null || colDb.ForeingKey.EqualForeignKey(tn, colModel) ? null :
                        SqlBuilder.AlterTableDropConstraint(tn, colDb.ForeingKey.Name), Spacing.Simple),
                        Spacing.Double);

            SqlPreCommand tables =
                Synchronizer.SynchronizeScriptReplacing(replacements, Replacements.KeyTables,
                model,
                database,
                (tn, tab) => SqlBuilder.CreateTableSql(tab),
                (tn, dif) => SqlBuilder.DropTable(dif.Prefix, dif.Name),
                (tn, tab, dif) =>
                    SqlPreCommand.Combine(Spacing.Simple,
                    dif.Name != tab.Name ? SqlBuilder.RenameTable(dif.Name, tab.Name) : null,
                    Synchronizer.SynchronizeScriptReplacing(replacements, Replacements.KeyColumnsForTable(tn),
                        tab.Columns,
                    dif.Colums,
                        (cn, tabCol) => SqlBuilder.AlterTableAddColumn(tn, tabCol),
                    (cn, difCol) => SqlPreCommand.Combine(Spacing.Simple,
                                    difCol.DefaultConstraintName.HasText() ? SqlBuilder.AlterTableDropConstraint(tn, difCol.DefaultConstraintName) : null,
                                    SqlBuilder.AlterTableDropColumn(tn, cn)),
                        (cn, tabCol, difCol) => SqlPreCommand.Combine(Spacing.Simple,
                            difCol.Name == tabCol.Name ? null : SqlBuilder.RenameColumn(tn, difCol.Name, tabCol.Name),
                            difCol.Equals(tabCol) ? null : SqlBuilder.AlterTableAlterColumn(tn, tabCol)),
                            Spacing.Simple)),
                    Spacing.Double);

            var tableReplacements = replacements.TryGetC(Replacements.KeyTables);
            if (tableReplacements != null)
                replacements[Replacements.KeyTablesInverse] = tableReplacements.Inverse();

            SqlPreCommand syncEnums = SynchronizeEnumsScript(replacements);

            SqlPreCommand addForeingKeys = Synchronizer.SynchronizeScript(
                 model,
                 database,
                 (tn, tab) => SqlBuilder.AlterTableForeignKeys(tab),
                 null,
                 (tn, tab, dif) => Synchronizer.SynchronizeScript(
                     tab.Columns,
                     dif.Colums,
                     (cn, colModel) => colModel.ReferenceTable != null ?
                         SqlBuilder.AlterTableAddConstraintForeignKey(tab, colModel.Name, colModel.ReferenceTable) : null,
                     null,
                     (cn, colModel, coldb) => colModel.ReferenceTable != null && (coldb.ForeingKey == null || !coldb.ForeingKey.EqualForeignKey(tn, colModel)) ?
                         SqlBuilder.AlterTableAddConstraintForeignKey(tab, colModel.Name, colModel.ReferenceTable) : null,
                     Spacing.Simple),
                 Spacing.Double);

            SqlPreCommand addIndices =
                Synchronizer.SynchronizeScript(model, database,
                 (tn, tab) => SqlBuilder.CreateAllIndices(tab, modelIndices[tab].Values),
                 null,
                (tn, tab, dif) =>
                {
                    var columnReplacements = replacements.TryGetC(Replacements.KeyColumnsForTable(tn));

                    var changedColumns = ChangedColumns(dif, tab, columnReplacements);

                    Dictionary<string, UniqueIndex> modelIxs = modelIndices[tab];

                    var controlledIndexes = Synchronizer.SynchronizeScript(modelIxs, dif.Indices.Where(kvp => kvp.Value.IsControlledIndex).ToDictionary(),
                         (i, mix) => SqlBuilder.CreateUniqueIndex(mix),
                        null,
                        (i, mix, dix) => dix.Columns.Any(a => changedColumns[a] == ColumnAction.Changed) ? SqlBuilder.CreateUniqueIndex(mix) : null,
                                Spacing.Simple);

                    var recreatedFreeIndexes = dif.Indices.Values.Where(dix => !dix.IsControlledIndex &&
                        dix.Columns.Any(a => changedColumns[a] != ColumnAction.Equals) &&
                        !dix.Columns.Any(a => changedColumns[a] == ColumnAction.Removed))
                        .Select(dix => SqlBuilder.ReCreateFreeIndex(dif.Name, tab.Name, dix, columnReplacements))
                        .Combine(Spacing.Simple);

                    var newFreeIndexes = Synchronizer.SynchronizeScript(tab.Columns, replacements.ApplyReplacementsToOld(dif.Colums, Replacements.KeyColumnsForTable(tn)),
                        (cn, colModel) => colModel.ReferenceTable != null ? SqlBuilder.CreateFreeIndex(tab, colModel) : null,
                        null,
                        null,
                        Spacing.Simple);

                    return SqlPreCommand.Combine(Spacing.Simple, controlledIndexes, recreatedFreeIndexes, newFreeIndexes);
                }, Spacing.Double);

            return SqlPreCommand.Combine(Spacing.Triple, dropIndices, dropForeignKeys, tables, syncEnums, addForeingKeys, addIndices);
        }

        private static Dictionary<string, ColumnAction> ChangedColumns(DiffTable dif, ITable tab, Dictionary<string, string> replacements)
        {
            return dif.Colums.SelectDictionary(k => k, (c, com) =>
            {
                if (replacements != null && replacements.ContainsKey(c))
                    return ColumnAction.Renamed;

                var mc = tab.Columns.TryGetC(c);

                if (mc == null)
                    return ColumnAction.Removed;

                if (!com.Equals(mc))
                    return ColumnAction.Changed;

                return ColumnAction.Equals;
            });
        }

        public enum ColumnAction
        {
            Equals,
            Removed,
            Changed,
            Renamed,
        }

        public static Dictionary<string, DiffTable> DefaultGetDatabaseDescription(string serverName, string databaseName)
        {
            var udttypes = Schema.Current.Settings.UdtSqlName.Values.ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            var database = (from s in Database.View<SysSchemas>()
                            from t in s.Tables()
                            where !t.ExtendedProperties().Any(a => a.name == "microsoft_database_tools_support")
                            select new DiffTable
                            {
                                Name = t.name,
                                Prefix = new NamePrefix
                                {
                                    ServerName = serverName,
                                    DatabaseName = databaseName,
                                    SchemaName = s.name,
                                },
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
                                              PrimaryKey = t.Indices().Any(i => i.is_primary_key && i.IndexColumns().Any(ic => ic.column_id == c.column_id)),
                                              ForeingKey = (from fk in t.ForeignKeys()
                                                            where fk.ForeignKeyColumns().Any(fkc => fkc.parent_column_id == c.column_id)
                                                            join rt in Database.View<SysTables>() on fk.referenced_object_id equals rt.object_id
                                                            select fk.name == null ? null : new DiffForeignKey { Name = fk.name, TargetTable = rt.name }).FirstOrDefault(),
                                          }).ToDictionary(a => a.Name, "columns"),

                                SimpleIndices = (from i in t.Indices()
                                                 where !i.is_primary_key //&& !(i.is_unique && i.name.StartsWith("IX_"))
                                                 select new DiffIndex
                                                 {
                                                     IsUnique = i.is_unique,
                                                     IndexName = i.name,
                                                     Columns = (from ic in i.IndexColumns()
                                                                join c in t.Columns() on ic.column_id equals c.column_id
                                                                select c.name).ToList()
                                                 }).ToList(),

                                ViewIndices = (from v in Database.View<SysViews>()
                                               where v.name.StartsWith("VIX_" + t.name + "_")
                                               from i in v.Indices()
                                               select new DiffIndex
                                               {
                                                   IsUnique = i.is_unique,
                                                   ViewName = v.name,
                                                   IndexName = i.name,
                                                   Columns = (from ic in i.IndexColumns()
                                                              join c in v.Columns() on ic.column_id equals c.column_id
                                                              select c.name).ToList()

                                               }).ToList(),

                            }).ToDictionary(c => c.Name);

            return database;
        }


        public static SqlDbType ToSqlDbType(string str)
        {
            if(str == "numeric")
                return SqlDbType.Decimal;

            return str.ToEnum<SqlDbType>(true);
        }

  
        static SqlPreCommand SynchronizeEnumsScript(Replacements replacements)
        {
            Schema schema = Schema.Current;
            
            List<SqlPreCommand> commands = new List<SqlPreCommand>();

            foreach (var table in schema.Tables.Values)
            {
                Type enumType = EnumEntity.Extract(table.Type);
                if (enumType != null)
                {
                    var should = EnumEntity.GetEntities(enumType);
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
                        (id, s, c) => SqlPreCommand.Combine(Spacing.Simple, updateRelatedTables(c), table.UpdateSqlSync(c, c.toStr)),
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
        public NamePrefix Prefix;
        public Dictionary<string, DiffColumn> Colums;

        public List<DiffIndex> SimpleIndices
        {
            get { return Indices.Values.ToList(); }
            set { Indices.AddRange(value, a => a.IndexName, a => a); }
        }
        public List<DiffIndex> ViewIndices
        {
            get { return Indices.Values.ToList(); }
            set { Indices.AddRange(value, a => a.IndexName, a => a); }
        }

        public Dictionary<string, DiffIndex> Indices = new Dictionary<string, DiffIndex>();
    }

    public class DiffIndex
    {
        public bool IsUnique;
        public string IndexName;
        public string ViewName;
        public List<string> Columns;

        public override string ToString()
        {
            return "{0} ({1})".Formato(IndexName, Columns.ToString(", "));
        }

        public bool IsControlledIndex
        {
            get { return IsUnique && IndexName.StartsWith("IX_"); }
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
