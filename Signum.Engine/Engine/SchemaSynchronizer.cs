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
using System.Text.RegularExpressions;
using Signum.Engine.Linq;
using Signum.Entities.Reflection;

namespace Signum.Engine
{
    public static class SchemaSynchronizer
    {
        public static Func<SchemaName, bool> DropSchema = s => !s.Name.Contains(@"\");

        public static Action<Dictionary<string, DiffTable>> SimplifyDiffTables;

        public static SqlPreCommand SynchronizeTablesScript(Replacements replacements)
        {
            Schema s = Schema.Current;

            Dictionary<string, ITable> modelTables = s.GetDatabaseTables().Where(t => !s.IsExternalDatabase(t.Name.Schema.Database)).ToDictionaryEx(a => a.Name.ToString(), "schema tables");
            var modelTablesHistory = modelTables.Values.Where(a => a.SystemVersioned != null).ToDictionaryEx(a => a.SystemVersioned.TableName.ToString(), "history schema tables");
            HashSet<SchemaName> modelSchemas = modelTables.Values.Select(a => a.Name.Schema).Where(a => !SqlBuilder.SystemSchemas.Contains(a.Name)).ToHashSet();

            Dictionary<string, DiffTable> databaseTables = DefaultGetDatabaseDescription(s.DatabaseNames());
            var databaseTablesHistory = databaseTables.Extract((key, val) => val.TemporalType == SysTableTemporalType.HistoryTable);
            HashSet<SchemaName> databaseSchemas = DefaultGetSchemas(s.DatabaseNames());

            SimplifyDiffTables?.Invoke(databaseTables);

            replacements.AskForReplacements(databaseTables.Keys.ToHashSet(), modelTables.Keys.ToHashSet(), Replacements.KeyTables);

            databaseTables = replacements.ApplyReplacementsToOld(databaseTables, Replacements.KeyTables);

            Dictionary<ITable, Dictionary<string, Index>> modelIndices = modelTables.Values
                .ToDictionary(t => t, t => t.GeneratAllIndexes().ToDictionaryEx(a => a.IndexName, "Indexes for {0}".FormatWith(t.Name)));

            //To --> From
            Dictionary<ObjectName, ObjectName> copyDataFrom = new Dictionary<ObjectName, ObjectName>();

            //A -> A_temp
            Dictionary<ObjectName, ObjectName> preRenames = new Dictionary<ObjectName, ObjectName>();

            modelTables.JoinDictionaryForeach(databaseTables, (tn, tab, diff) =>
            {
                var key = Replacements.KeyColumnsForTable(tn);

                replacements.AskForReplacements(diff.Columns.Keys.ToHashSet(), tab.Columns.Keys.ToHashSet(), key);

                diff.Columns = replacements.ApplyReplacementsToOld(diff.Columns, key);

                diff.Indices = ApplyIndexAutoReplacements(diff, tab, modelIndices[tab]);

                var diffPk = diff.Columns.TryGetC(tab.PrimaryKey.Name);
                if (diffPk != null && tab.PrimaryKey.Identity != diffPk.Identity)
                {
                    if (tab.Name.Equals(diff.Name))
                    {
                        var tempName = new ObjectName(diff.Name.Schema, diff.Name.Name + "_old");
                        preRenames.Add(diff.Name, tempName);
                        copyDataFrom.Add(tab.Name, tempName);

                        if (replacements.Interactive)
                        {
                            SafeConsole.WriteLineColor(ConsoleColor.Yellow, $@"Column {diffPk.Name} in {diff.Name} is now Identity={tab.PrimaryKey.Identity}.");
                            Console.WriteLine($@"Changing a Primary Key is not supported by SQL Server so the script will...:
  1. Rename {diff.Name} table to {tempName}
  2. Create a new table {diff.Name} 
  3. Copy data from {tempName} to {tab.Name}.
  4. Drop {tempName}
");
                        }
                    }
                    else
                    {
                        copyDataFrom.Add(tab.Name, diff.Name);
                        if (replacements.Interactive)
                        {
                            SafeConsole.WriteLineColor(ConsoleColor.Yellow, $@"Column {diffPk.Name} in {diff.Name} is now Identity={tab.PrimaryKey.Identity}.");
                            Console.WriteLine($@"Changing a Primary Key is not supported by SQL Server so the script will...:
  1. Create a new table {tab.Name} 
  2. Copy data from {diff.Name} to {tab.Name}.
  3. Drop {diff.Name}
");
                        }
                    }
                }
            });

            var columnsByFKTarget = databaseTables.Values.SelectMany(a => a.Columns.Values).Where(a => a.ForeignKey != null).GroupToDictionary(a => a.ForeignKey.TargetTable);

            foreach (var pr in preRenames)
            {
                var diff = databaseTables[pr.Key.ToString()];
                diff.Name = pr.Value;
                foreach (var col in columnsByFKTarget.TryGetC(pr.Key).EmptyIfNull())
                    col.ForeignKey.TargetTable = pr.Value;

                databaseTables.Add(pr.Value.ToString(), diff);
                databaseTables.Remove(pr.Key.ToString());
            }

            Func<ObjectName, ObjectName> ChangeName = (ObjectName objectName) =>
            {
                string name = replacements.Apply(Replacements.KeyTables, objectName.ToString());

                return modelTables.TryGetC(name)?.Name ?? objectName;
            };


            Func<ObjectName, SqlPreCommand> DeleteAllForeignKey = tableName =>
            {
                var dropFks = (from t in databaseTables.Values
                               from c in t.Columns.Values
                               where c.ForeignKey != null && c.ForeignKey.TargetTable.Equals(tableName)
                               select SqlBuilder.AlterTableDropConstraint(t.Name, c.ForeignKey.Name)).Combine(Spacing.Simple);

                if (dropFks == null)
                    return null;

                return SqlPreCommand.Combine(Spacing.Simple, new SqlPreCommandSimple("---In order to remove the PK of " + tableName.Name), dropFks);
            };

            using (replacements.WithReplacedDatabaseName())
            {
                SqlPreCommand preRenameTables = preRenames.Select(a => SqlBuilder.RenameTable(a.Key, a.Value.Name)).Combine(Spacing.Double);

                if (preRenameTables != null)
                    preRenameTables.GoAfter = true;

                SqlPreCommand createSchemas = Synchronizer.SynchronizeScriptReplacing(replacements, "Schemas", Spacing.Double,
                    modelSchemas.ToDictionary(a => a.ToString()),
                    databaseSchemas.ToDictionary(a => a.ToString()),
                    createNew: (_, newSN) => SqlBuilder.CreateSchema(newSN),
                    removeOld: null,
                    mergeBoth: (_, newSN, oldSN) => newSN.Equals(oldSN) ? null : SqlBuilder.CreateSchema(newSN)
                    );

                //use database without replacements to just remove indexes
                SqlPreCommand dropStatistics =
                    Synchronizer.SynchronizeScript(Spacing.Double, modelTables, databaseTables, 
                    createNew:  null,
                    removeOld:  (tn, dif) => SqlBuilder.DropStatistics(tn, dif.Stats),
                    mergeBoth: (tn, tab, dif) =>
                    {
                        var removedColums = dif.Columns.Keys.Except(tab.Columns.Keys).ToHashSet();

                        return SqlBuilder.DropStatistics(tn, dif.Stats.Where(a => a.Columns.Any(removedColums.Contains)).ToList());
                    });

                SqlPreCommand dropIndices =
                    Synchronizer.SynchronizeScript(Spacing.Double, modelTables, databaseTables, 
                    createNew: null,
                    removeOld: (tn, dif) => dif.Indices.Values.Where(ix => !ix.IsPrimary).Select(ix => SqlBuilder.DropIndex(dif.Name, ix)).Combine(Spacing.Simple),
                    mergeBoth: (tn, tab, dif) =>
                    {
                        Dictionary<string, Index> modelIxs = modelIndices[tab];

                        var removedColums = dif.Columns.Keys.Except(tab.Columns.Keys).ToHashSet();

                        var changes = Synchronizer.SynchronizeScript(Spacing.Simple, modelIxs, dif.Indices, 
                            createNew: null,
                            removeOld: (i, dix) => dix.Columns.Any(c => removedColums.Contains(c.ColumnName)) || dix.IsControlledIndex ? SqlBuilder.DropIndex(dif.Name, dix) : null,
                            mergeBoth: (i, mix, dix) => !dix.IndexEquals(dif, mix) ? SqlPreCommand.Combine(Spacing.Double, dix.IsPrimary ? DeleteAllForeignKey(dif.Name) : null, SqlBuilder.DropIndex(dif.Name, dix)) : null
                            );

                        return changes;
                    });

                SqlPreCommand dropIndicesHistory =
                    Synchronizer.SynchronizeScript(Spacing.Double, modelTablesHistory, databaseTablesHistory,
                    createNew: null,
                    removeOld: (tn, dif) => dif.Indices.Values.Where(ix => ix.Type != DiffIndexType.Clustered).Select(ix => SqlBuilder.DropIndex(dif.Name, ix)).Combine(Spacing.Simple),
                    mergeBoth: (tn, tab, dif) =>
                    {
                        Dictionary<string, Index> modelIxs = modelIndices[tab].Where(a => a.Value.GetType() == typeof(Index)).ToDictionary();

                        var removedColums = dif.Columns.Keys.Except(tab.Columns.Keys).ToHashSet();

                        var changes = Synchronizer.SynchronizeScript(Spacing.Simple, modelIxs, dif.Indices,
                            createNew: null,
                            removeOld: (i, dix) => dix.Columns.Any(c => removedColums.Contains(c.ColumnName)) || dix.IsControlledIndex ? SqlBuilder.DropIndex(dif.Name, dix) : null,
                            mergeBoth: (i, mix, dix) => !dix.IndexEquals(dif, mix) ? SqlPreCommand.Combine(Spacing.Double, dix.IsPrimary ? DeleteAllForeignKey(dif.Name) : null, SqlBuilder.DropIndex(dif.Name, dix)) : null
                            );

                        return changes;
                    });

                SqlPreCommand dropForeignKeys = Synchronizer.SynchronizeScript(
                     Spacing.Double,
                     modelTables,
                     databaseTables,
                     createNew: null,
                     removeOld: (tn, dif) => dif.Columns.Values.Select(c => c.ForeignKey != null ? SqlBuilder.AlterTableDropConstraint(dif.Name, c.ForeignKey.Name) : null)
                         .Concat(dif.MultiForeignKeys.Select(fk => SqlBuilder.AlterTableDropConstraint(dif.Name, fk.Name))).Combine(Spacing.Simple),
                     mergeBoth: (tn, tab, dif) => SqlPreCommand.Combine(Spacing.Simple,
                         Synchronizer.SynchronizeScript(
                         Spacing.Simple,
                         tab.Columns,
                         dif.Columns,
                         createNew: null,
                         removeOld: (cn, colDb) => colDb.ForeignKey != null ? SqlBuilder.AlterTableDropConstraint(dif.Name, colDb.ForeignKey.Name) : null,
                         mergeBoth: (cn, colModel, colDb) => colDb.ForeignKey == null ? null :
                             colModel.ReferenceTable == null || colModel.AvoidForeignKey || !colModel.ReferenceTable.Name.Equals(ChangeName(colDb.ForeignKey.TargetTable)) || DifferentDatabase(tab.Name, colModel.ReferenceTable.Name) ?
                             SqlBuilder.AlterTableDropConstraint(dif.Name, colDb.ForeignKey.Name) :
                             null),
                        dif.MultiForeignKeys.Select(fk => SqlBuilder.AlterTableDropConstraint(dif.Name, fk.Name)).Combine(Spacing.Simple))
                );

                SqlPreCommand preRenamePks = preRenames.Select(a => SqlBuilder.DropPrimaryKeyConstraint(a.Value)).Combine(Spacing.Double);

                SqlPreCommand tables =
                        Synchronizer.SynchronizeScript(
                        Spacing.Double,
                        modelTables,
                        databaseTables,
                        createNew: (tn, tab) => SqlPreCommand.Combine(Spacing.Double,
                            SqlBuilder.CreateTableSql(tab),
                            copyDataFrom.ContainsKey(tab.Name) ? CopyData(tab, databaseTables.GetOrThrow(copyDataFrom.GetOrThrow(tab.Name).ToString()), replacements).Do(a => a.GoBefore = true) : null
                        ),
                        removeOld: (tn, dif) => SqlBuilder.DropTable(dif.Name),
                        mergeBoth: (tn, tab, dif) =>
                        {
                            var rename = !object.Equals(dif.Name, tab.Name) ? SqlBuilder.RenameOrMove(dif, tab) : null;

                            var disableSystemVersioning = (dif.TemporalType != SysTableTemporalType.None && (
                                tab.SystemVersioned == null || !dif.TemporalTableName.Equals(tab.SystemVersioned.TableName)) ?
                                SqlBuilder.AlterTableDisableSystemVersioning(tab) : null);

                            var dropPeriod = (dif.Period != null &&
                                (tab.SystemVersioned == null || !dif.Period.PeriodEquals(tab.SystemVersioned)) ?
                                SqlBuilder.AlterTableDropPeriod(tab) : null);

                            var columns = Synchronizer.SynchronizeScript(
                                    Spacing.Simple,
                                    tab.Columns,
                                    dif.Columns,

                                    createNew: (cn, tabCol) => SqlPreCommand.Combine(Spacing.Simple,
                                        tabCol.PrimaryKey && dif.PrimaryKeyName != null ? SqlBuilder.DropPrimaryKeyConstraint(tab.Name) : null,
                                        AlterTableAddColumnDefault(tab, tabCol, replacements)),

                                    removeOld: (cn, difCol) => SqlPreCommand.Combine(Spacing.Simple,
                                         difCol.DefaultConstraint != null ? SqlBuilder.AlterTableDropConstraint(tab.Name, difCol.DefaultConstraint.Name) : null,
                                        SqlBuilder.AlterTableDropColumn(tab, cn)),

                                    mergeBoth: (cn, tabCol, difCol) => SqlPreCommand.Combine(Spacing.Simple,

                                        difCol.Name == tabCol.Name ? null : SqlBuilder.RenameColumn(tab, difCol.Name, tabCol.Name),

                                        difCol.ColumnEquals(tabCol, ignorePrimaryKey: true, ignoreIdentity: false, ignoreGenerateAlways: false) ? null : SqlPreCommand.Combine(Spacing.Simple,
                                            tabCol.PrimaryKey && !difCol.PrimaryKey && dif.PrimaryKeyName != null ? SqlBuilder.DropPrimaryKeyConstraint(tab.Name) : null,
                                        difCol.CompatibleTypes(tabCol) ?
                                                 SqlPreCommand.Combine(Spacing.Simple, 
                                                    difCol.Nullable && !tabCol.Nullable.ToBool() ? NotNullUpdate(tab, tabCol, replacements) : null,
                                                    SqlBuilder.AlterTableAlterColumn(tab, tabCol, difCol.DefaultConstraint?.Name)
                                                ):
                                                SqlPreCommand.Combine(Spacing.Simple, 
                                                    SqlBuilder.AlterTableAddColumn(tab, tabCol),
                                                    new SqlPreCommandSimple($"UPDATE {tab.Name} SET {tabCol.Name} = YourCode({difCol.Name})"),
                                                    SqlBuilder.AlterTableDropColumn(tab, tabCol.Name)
                                                ) 
                                            ,
                                            tabCol.SqlDbType == SqlDbType.NVarChar && difCol.SqlDbType == SqlDbType.NChar ? SqlBuilder.UpdateTrim(tab, tabCol) : null),

                                        difCol.DefaultEquals(tabCol) ? null : SqlPreCommand.Combine(Spacing.Simple,
                                            difCol.DefaultConstraint != null ? SqlBuilder.AlterTableDropConstraint(tab.Name, difCol.DefaultConstraint.Name) : null,
                                            tabCol.Default != null ? SqlBuilder.AlterTableAddDefaultConstraint(tab.Name, SqlBuilder.GetDefaultConstaint(tab, tabCol)) : null),

                                        UpdateByFkChange(tn, difCol, tabCol, ChangeName, copyDataFrom)
                                    )
                        );

                            var addPeriod = ((tab.SystemVersioned != null &&
                                (dif.Period == null || !dif.Period.PeriodEquals(tab.SystemVersioned))) ?
                                (SqlPreCommandSimple)SqlBuilder.AlterTableAddPeriod(tab) : null);

                            var addSystemVersioning = (tab.SystemVersioned != null &&
                                (dif.Period == null || !dif.TemporalTableName.Equals(tab.SystemVersioned.TableName)) ?
                                SqlBuilder.AlterTableEnableSystemVersioning(tab).Do(a=>a.GoBefore = true) : null);


                            SqlPreCommand combinedAddPeriod = null;
                            if(addPeriod != null && columns is SqlPreCommandConcat cols)
                            {
                                var periodRows = cols.Leaves().Where(pcs => pcs.Sql.Contains(" ADD ") && pcs.Sql.Contains("GENERATED ALWAYS AS ROW")).ToList();
                                if (periodRows.Count == 2) {

                                    combinedAddPeriod = new SqlPreCommandSimple($@"ALTER TABLE {tn} ADD
    {periodRows[0].Sql.After(" ADD ")},
    {periodRows[1].Sql.After(" ADD ")},
    {addPeriod.Sql.After(" ADD ")}
");
                                    addPeriod = null;
                                    columns = cols.Leaves().Except(periodRows).Combine(cols.Spacing);
                                }
                            }

                            return SqlPreCommand.Combine(Spacing.Simple, rename, disableSystemVersioning, dropPeriod, combinedAddPeriod, columns, addPeriod, addSystemVersioning);
                        });

                if (tables != null)
                    tables.GoAfter = true;

                var tableReplacements = replacements.TryGetC(Replacements.KeyTables);
                if (tableReplacements != null)
                    replacements[Replacements.KeyTablesInverse] = tableReplacements.Inverse();

                SqlPreCommand syncEnums;

                try
                {
                    syncEnums = SynchronizeEnumsScript(replacements);
                }
                catch (Exception e)
                {
                    syncEnums = new SqlPreCommandSimple("-- Exception synchronizing enums: " + e.Message);
                }

                SqlPreCommand addForeingKeys = Synchronizer.SynchronizeScript(
                     Spacing.Double,
                     modelTables,
                     databaseTables,
                     createNew: (tn, tab) => SqlBuilder.AlterTableForeignKeys(tab),
                     removeOld: null,
                     mergeBoth: (tn, tab, dif) => Synchronizer.SynchronizeScript(
                         Spacing.Simple,
                         tab.Columns,
                         dif.Columns,

                         createNew: (cn, colModel) => colModel.ReferenceTable == null || colModel.AvoidForeignKey || DifferentDatabase(tab.Name, colModel.ReferenceTable.Name) ? null :
                             SqlBuilder.AlterTableAddConstraintForeignKey(tab, colModel.Name, colModel.ReferenceTable),

                         removeOld: null,

                         mergeBoth: (cn, colModel, coldb) =>
                         {
                             if (colModel.ReferenceTable == null || colModel.AvoidForeignKey || DifferentDatabase(tab.Name, colModel.ReferenceTable.Name))
                                 return null;

                             if (coldb.ForeignKey == null || !colModel.ReferenceTable.Name.Equals(ChangeName(coldb.ForeignKey.TargetTable)))
                                 return SqlBuilder.AlterTableAddConstraintForeignKey(tab, colModel.Name, colModel.ReferenceTable);

                             var name = SqlBuilder.ForeignKeyName(tab.Name.Name, colModel.Name);
                             return SqlPreCommand.Combine(Spacing.Simple,
                                name != coldb.ForeignKey.Name.Name ? SqlBuilder.RenameForeignKey(coldb.ForeignKey.Name, name) : null,
                                (coldb.ForeignKey.IsDisabled || coldb.ForeignKey.IsNotTrusted) && !replacements.SchemaOnly ? SqlBuilder.EnableForeignKey(tab.Name, name) : null);
                         })
                     );

                bool? createMissingFreeIndexes = null;

                SqlPreCommand addIndices =
                    Synchronizer.SynchronizeScript(Spacing.Double, modelTables, databaseTables,
                    createNew: (tn, tab) => modelIndices[tab].Values.Where(a => !(a is PrimaryClusteredIndex)).Select(SqlBuilder.CreateIndex).Combine(Spacing.Simple),
                    removeOld: null,
                    mergeBoth: (tn, tab, dif) =>
                    {
                        var columnReplacements = replacements.TryGetC(Replacements.KeyColumnsForTable(tn));

                        Func<IColumn, bool> isNew = c => !dif.Columns.ContainsKey(columnReplacements?.TryGetC(c.Name) ?? c.Name);

                        Dictionary<string, Index> modelIxs = modelIndices[tab];

                        var controlledIndexes = Synchronizer.SynchronizeScript(Spacing.Simple, modelIxs, dif.Indices, 
                            createNew: (i, mix) => mix is UniqueIndex || mix.Columns.Any(isNew) || SafeConsole.Ask(ref createMissingFreeIndexes, "Create missing non-unique index {0} in {1}?".FormatWith(mix.IndexName, tab.Name)) ? SqlBuilder.CreateIndex(mix) : null,
                            removeOld: null,
                            mergeBoth: (i, mix, dix) => !dix.IndexEquals(dif, mix) ? SqlBuilder.CreateIndex(mix) :
                                mix.IndexName != dix.IndexName ? SqlBuilder.RenameIndex(tab.Name, dix.IndexName, mix.IndexName) : null);

                        return SqlPreCommand.Combine(Spacing.Simple, controlledIndexes);
                    });

                SqlPreCommand addIndicesHistory =
                    Synchronizer.SynchronizeScript(Spacing.Double, modelTablesHistory, databaseTablesHistory,
                    createNew: (tn, tab) => modelIndices[tab].Values.Where(a => a.GetType() == typeof(Index)).Select(mix => SqlBuilder.CreateIndexBasic(mix, forHistoryTable: true)).Combine(Spacing.Simple),
                    removeOld: null,
                    mergeBoth: (tn, tab, dif) =>
                    {
                        var columnReplacements = replacements.TryGetC(Replacements.KeyColumnsForTable(tn));

                        Func<IColumn, bool> isNew = c => !dif.Columns.ContainsKey(columnReplacements?.TryGetC(c.Name) ?? c.Name);

                        Dictionary<string, Index> modelIxs = modelIndices[tab].Where(kvp => kvp.Value.GetType() == typeof(Index)).ToDictionary();

                        var controlledIndexes = Synchronizer.SynchronizeScript(Spacing.Simple, modelIxs, dif.Indices,
                            createNew: (i, mix) => mix is UniqueIndex || mix.Columns.Any(isNew) || SafeConsole.Ask(ref createMissingFreeIndexes, "Create missing non-unique index {0} in {1}?".FormatWith(mix.IndexName, tab.Name)) ? SqlBuilder.CreateIndexBasic(mix, forHistoryTable: true) : null,
                            removeOld: null,
                            mergeBoth: (i, mix, dix) => !dix.IndexEquals(dif, mix) ? SqlBuilder.CreateIndexBasic(mix, forHistoryTable: true) :
                                mix.IndexName != dix.IndexName ? SqlBuilder.RenameIndex(tab.SystemVersioned.TableName, dix.IndexName, mix.IndexName) : null);

                        return SqlPreCommand.Combine(Spacing.Simple, controlledIndexes);
                    });

                SqlPreCommand dropSchemas = Synchronizer.SynchronizeScriptReplacing(replacements, "Schemas", Spacing.Double,
                    modelSchemas.ToDictionary(a => a.ToString()),
                    databaseSchemas.ToDictionary(a => a.ToString()),
                    createNew: null,
                    removeOld: (_, oldSN) => DropSchema(oldSN) ? SqlBuilder.DropSchema(oldSN) : null,
                    mergeBoth: (_, newSN, oldSN) => newSN.Equals(oldSN) ? null : SqlBuilder.DropSchema(oldSN)
                 );

                return SqlPreCommand.Combine(Spacing.Triple, preRenameTables, createSchemas, dropStatistics, dropIndices, dropIndicesHistory, dropForeignKeys, preRenamePks, tables, syncEnums, addForeingKeys, addIndices, addIndicesHistory, dropSchemas);
            }
        }

        private static SqlPreCommandSimple NotNullUpdate(ITable tab, IColumn tabCol, Replacements rep)
        {
            var defaultValue = GetDefaultValue(tab, tabCol, rep, forNewColumn: false);

            if (defaultValue == "force")
                return null;

            return new SqlPreCommandSimple($"UPDATE {tab.Name} SET {tabCol.Name} = {defaultValue} WHERE {tabCol.Name} IS NULL");
        }

        private static bool DifferentDatabase(ObjectName name, ObjectName name2)
        {
            return !object.Equals(name.Schema.Database, name2.Schema.Database);
        }

        public static Func<SchemaName, bool> IgnoreSchema = s => s.Name.Contains("\\");

        private static HashSet<SchemaName> DefaultGetSchemas(List<DatabaseName> list)
        {
            HashSet<SchemaName> result = new HashSet<SchemaName>();
            foreach (var db in list)
            {
                using (Administrator.OverrideDatabaseInSysViews(db))
                {
                    var schemaNames = Database.View<SysSchemas>().Select(s => s.name).ToList().Except(SqlBuilder.SystemSchemas);

                    result.AddRange(schemaNames.Select(sn => new SchemaName(db, sn)).Where(a => !IgnoreSchema(a)));
                }
            }
            return result;
        }

        private static SqlPreCommand AlterTableAddColumnDefault(ITable table, IColumn column, Replacements rep)
        {
            if (column.Nullable == IsNullable.Yes || column.Identity || column.Default != null)
                return SqlBuilder.AlterTableAddColumn(table, column);

                var defaultValue = GetDefaultValue(table, column, rep, forNewColumn: true);
                if (defaultValue == "force")
                    return SqlBuilder.AlterTableAddColumn(table, column);

            if(column.Nullable == IsNullable.Forced)
            {
                var hasValueColumn = table.Columns.Values
                    .Where(a => a.Name.EndsWith("_HasValue") && column.Name.StartsWith(a.Name.BeforeLast("_HasValue")))
                    .OrderByDescending(a => a.Name.Length)
                    .FirstOrDefault();

                var where = hasValueColumn != null ? $"{hasValueColumn.Name} = 1" : "??";

                return SqlPreCommand.Combine(Spacing.Simple,
                    SqlBuilder.AlterTableAddColumn(table, column).Do(a => a.GoAfter = true),
                    new SqlPreCommandSimple($@"UPDATE {table.Name} SET
    {column.Name} = {SqlBuilder.Quote(column.SqlDbType, defaultValue)} 
WHERE {where}"));
            }

            var tempDefault = new SqlBuilder.DefaultConstraint
            {
                ColumnName = column.Name,
                Name = "DF_TEMP_" + column.Name,
                QuotedDefinition = SqlBuilder.Quote(column.SqlDbType, defaultValue),
            };
            
            return SqlPreCommand.Combine(Spacing.Simple,
                SqlBuilder.AlterTableAddColumn(table, column, tempDefault),
                SqlBuilder.AlterTableDropConstraint(table.Name, tempDefault.Name));
        }

        internal static SqlPreCommand CopyData(ITable newTable, DiffTable oldTable, Replacements rep)
        {
            var selectColumns = newTable.Columns
                .Select(col => oldTable.Columns.TryGetC(col.Key)?.Name ?? GetDefaultValue(newTable, col.Value, rep, forNewColumn: true))
                .ToString(", ");

            var insertSelect = new SqlPreCommandSimple(
$@"INSERT INTO {newTable.Name} ({newTable.Columns.Values.ToString(a => a.Name, ", ")})
SELECT {selectColumns}
FROM {oldTable.Name}");

            if (!newTable.PrimaryKey.Identity)
                return insertSelect;

            return SqlPreCommand.Combine(Spacing.Simple,
                SqlBuilder.SetIdentityInsert(newTable.Name, true),
                insertSelect,
                SqlBuilder.SetIdentityInsert(newTable.Name, false)
            );
        }

        public static string GetDefaultValue(ITable table, IColumn column, Replacements rep, bool forNewColumn)
        {
     		if(column is SystemVersionedInfo.Column svc)
            {
                var date = svc.SystemVersionColumnType == SystemVersionedInfo.ColumnType.Start ? DateTime.MinValue : DateTime.MaxValue;

                return $"CONVERT(datetime2, '{date:yyyy-MM-dd HH:mm:ss.fffffff}')";
            }

            string typeDefault = SqlBuilder.IsNumber(column.SqlDbType) ? "0" :
                                 SqlBuilder.IsString(column.SqlDbType) ? "''" :
                                 SqlBuilder.IsDate(column.SqlDbType) ? "GetDate()" :
                                 column.SqlDbType == SqlDbType.UniqueIdentifier ? "NEWID()" :
                                 "?";

            string defaultValue = rep.Interactive ? SafeConsole.AskString($"Default value for '{table.Name.Name}.{column.Name}'? ([Enter] for {typeDefault} or 'force' if there are no {(forNewColumn ? "rows" : "nulls")}) ", stringValidator: str => null) : "";
            if (defaultValue == "force")
                return defaultValue;

            if (defaultValue.HasText() && SqlBuilder.IsString(column.SqlDbType) && !defaultValue.Contains("'"))
                defaultValue = "'" + defaultValue + "'";

            if (string.IsNullOrEmpty(defaultValue))
                return typeDefault;

            return defaultValue;
        }

        private static Dictionary<string, DiffIndex> ApplyIndexAutoReplacements(DiffTable diff, ITable tab, Dictionary<string, Index> dictionary)
        {
            List<string> oldOnly = diff.Indices.Keys.Where(n => !dictionary.ContainsKey(n)).ToList();
            List<string> newOnly = dictionary.Keys.Where(n => !diff.Indices.ContainsKey(n)).ToList();

            if (oldOnly.Count == 0 || newOnly.Count == 0)
                return diff.Indices;

            Dictionary<string, string> replacements = new Dictionary<string, string>();
            foreach (var o in oldOnly)
            {
                var oldIx = diff.Indices[o];

                var nIx = newOnly.FirstOrDefault(n =>
                {
                    var newIx = dictionary[n];
                    if (oldIx.IsPrimary && newIx is PrimaryClusteredIndex)
                        return true;

                    if (oldIx.IsPrimary || newIx is PrimaryClusteredIndex)
                        return false;

                    if (oldIx.IsUnique != (newIx is UniqueIndex))
                        return false;

                    if (oldIx.ViewName != null || (newIx is UniqueIndex) && ((UniqueIndex)newIx).ViewName != null)
                        return false;

                    var news = newIx.Columns.Select(c => diff.Columns.TryGetC(c.Name)?.Name).NotNull().ToHashSet();

                    if (!news.SetEquals(oldIx.Columns.Select(a => a.ColumnName)))
                        return false;

                    var oldWhere = oldIx.IndexName.TryAfter("__");
                    var newWhere = newIx.Where == null ? null : StringHashEncoder.Codify(newIx.Where);

                    if (oldWhere != newWhere)
                        return false;

                    return true;
                });

                if (nIx != null)
                {
                    replacements.Add(o, nIx);
                    newOnly.Remove(nIx);
                }
            }

            if (replacements.IsEmpty())
                return diff.Indices;

            return diff.Indices.SelectDictionary(on => replacements.TryGetC(on) ?? on, dif => dif);
        }

        private static SqlPreCommandSimple UpdateByFkChange(string tn, DiffColumn difCol, IColumn tabCol, Func<ObjectName, ObjectName> changeName, Dictionary<ObjectName, ObjectName> copyDataFrom)
        {
            if (difCol.ForeignKey == null || tabCol.ReferenceTable == null || tabCol.AvoidForeignKey)
                return null;

            ObjectName oldFk = changeName(difCol.ForeignKey.TargetTable);

            if (oldFk.Equals(tabCol.ReferenceTable.Name))
                return null;

            var newComesFrom = copyDataFrom.TryGetC(tabCol.ReferenceTable.Name);
            if (newComesFrom != null && oldFk.Equals(newComesFrom))
                return null;

            AliasGenerator ag = new AliasGenerator();

            return new SqlPreCommandSimple(
@"UPDATE {2}
SET {0} =  -- get {5} id from {4}.Id
FROM {1} {2}
JOIN {3} {4} ON {2}.{0} = {4}.Id".FormatWith(tabCol.Name,
                tn, ag.NextTableAlias(tn),
                oldFk, ag.NextTableAlias(oldFk.Name),
                tabCol.ReferenceTable.Name.Name));
        }

        public static Dictionary<string, DiffTable> DefaultGetDatabaseDescription(List<DatabaseName> databases)
        {
            List<DiffTable> allTables = new List<DiffTable>();

            foreach (var db in databases)
            {
                using (Administrator.OverrideDatabaseInSysViews(db))
                {
                    var databaseName = db == null ? Connector.Current.DatabaseName() : db.Name;

                    var sysDb = Database.View<SysDatabases>().Single(a => a.name == databaseName);

                    var con = Connector.Current;

                    var tables =
                        (from s in Database.View<SysSchemas>()
                         from t in s.Tables().Where(t => !t.ExtendedProperties().Any(a => a.name == "microsoft_database_tools_support")) //IntelliSense bug
                         select new DiffTable
                         {
                             Name = new ObjectName(new SchemaName(db, s.name), t.name),

                             TemporalType = !con.SupportsTemporalTables ? SysTableTemporalType.None: t.temporal_type,

                             Period = !con.SupportsTemporalTables ? null : 
                             (from p in t.Periods()
                              join sc in t.Columns() on p.start_column_id equals sc.column_id 
                              join ec in t.Columns() on p.end_column_id equals ec.column_id
#pragma warning disable CS0472 
                              select (int?)p.object_id == null ? null : new DiffPeriod
#pragma warning restore CS0472
                              {
                                  StartColumnName = sc.name,
                                  EndColumnName = ec.name,
                              }).SingleOrDefaultEx(),

                             TemporalTableName = !con.SupportsTemporalTables || t.history_table_id == null ? null : 
                             Database.View<SysTables>()
                             .Where(ht => ht.object_id == t.history_table_id)
                             .Select(ht => new ObjectName(new SchemaName(db, ht.Schema().name), ht.name))
                             .SingleOrDefault(),

                             PrimaryKeyName = (from k in t.KeyConstraints()
                                               where k.type == "PK"
                                               select k.name == null ? null : new ObjectName(new SchemaName(db, k.Schema().name), k.name))
                                               .SingleOrDefaultEx(),

                             Columns = (from c in t.Columns()
                                        join userType in Database.View<SysTypes>().DefaultIfEmpty() on c.user_type_id equals userType.user_type_id
                                        join sysType in Database.View<SysTypes>().DefaultIfEmpty() on c.system_type_id equals sysType.user_type_id
                                        join ctr in Database.View<SysDefaultConstraints>().DefaultIfEmpty() on c.default_object_id equals ctr.object_id
                                        select new DiffColumn
                                        {
                                            Name = c.name,
                                            SqlDbType = sysType == null ? SqlDbType.Udt : ToSqlDbType(sysType.name),
                                            UserTypeName = sysType == null ? userType.name : null,
                                            Nullable = c.is_nullable,
                                            Collation = c.collation_name == sysDb.collation_name ? null : c.collation_name,
                                            Length = c.max_length,
                                            Precission = c.precision,
                                            Scale = c.scale,
                                            Identity = c.is_identity,
                                            GeneratedAlwaysType = con.SupportsTemporalTables ? c.generated_always_type : GeneratedAlwaysType.None,
                                            DefaultConstraint = ctr.name == null ? null : new DiffDefaultConstraint
                                            {
                                                Name = ctr.name,
                                                Definition = ctr.definition
                                            },
                                            PrimaryKey = t.Indices().Any(i => i.is_primary_key && i.IndexColumns().Any(ic => ic.column_id == c.column_id)),
                                        }).ToDictionaryEx(a => a.Name, "columns"),

                             MultiForeignKeys = (from fk in t.ForeignKeys()
                                                 join rt in Database.View<SysTables>() on fk.referenced_object_id equals rt.object_id
                                                 select new DiffForeignKey
                                                 {
                                                     Name = new ObjectName(new SchemaName(db, fk.Schema().name), fk.name),
                                                     IsDisabled = fk.is_disabled,
                                                     TargetTable = new ObjectName(new SchemaName(db, rt.Schema().name), rt.name),
                                                     Columns = fk.ForeignKeyColumns().Select(fkc => new DiffForeignKeyColumn
                                                     {
                                                         Parent = t.Columns().Single(c => c.column_id == fkc.parent_column_id).name,
                                                         Referenced = rt.Columns().Single(c => c.column_id == fkc.referenced_column_id).name
                                                     }).ToList(),
                                                 }).ToList(),

                             SimpleIndices = (from i in t.Indices()
                                              where /*!i.is_primary_key && */i.type != 0  /*heap indexes*/
                                              select new DiffIndex
                                              {
                                                  IsUnique = i.is_unique,
                                                  IsPrimary = i.is_primary_key,
                                                  IndexName = i.name,
                                                  FilterDefinition = i.filter_definition,
                                                  Type = (DiffIndexType)i.type,
                                                  Columns = (from ic in i.IndexColumns()
                                                             join c in t.Columns() on ic.column_id equals c.column_id
                                                             orderby ic.index_column_id
                                                             select new DiffIndexColumn { ColumnName =  c.name, IsIncluded = ic.is_included_column  }).ToList()
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
                                                           orderby ic.index_column_id
                                                           select new DiffIndexColumn { ColumnName = c.name, IsIncluded = ic.is_included_column }).ToList()

                                            }).ToList(),

                             Stats = (from st in t.Stats()
                                      where st.user_created
                                      select new DiffStats
                                      {
                                          StatsName = st.name,
                                          Columns = (from ic in st.StatsColumns()
                                                     join c in t.Columns() on ic.column_id equals c.column_id
                                                     select c.name).ToList()
                                      }).ToList(),

                         }).ToList();

                    tables.ForEach(t => t.ForeignKeysToColumns());

                    allTables.AddRange(tables);
                }
            }

            var database = allTables.ToDictionary(t => t.Name.ToString());

            return database;
        }


        public static SqlDbType ToSqlDbType(string str)
        {
            if (str == "numeric")
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
                    IEnumerable<Entity> should = EnumEntity.GetEntities(enumType);
                    Dictionary<string, Entity> shouldByName = should.ToDictionary(a => a.ToString());

                    List<Entity> current = Administrator.TryRetrieveAll(table.Type, replacements);
                    Dictionary<string, Entity> currentByName = current.ToDictionaryEx(a => a.toStr, table.Name.Name);

                    string key = Replacements.KeyEnumsForTable(table.Name.Name);

                    replacements.AskForReplacements(currentByName.Keys.ToHashSet(), shouldByName.Keys.ToHashSet(), key);

                    currentByName = replacements.ApplyReplacementsToOld(currentByName, key);

                    var mix = shouldByName.JoinDictionary(currentByName, (n, s, c) => new { s, c }).Where(a => a.Value.s.id != a.Value.c.id).ToDictionary();

                    HashSet<PrimaryKey> usedIds = current.Select(a => a.Id).ToHashSet();

                    Dictionary<string, Entity> middleByName = mix.Where(kvp => usedIds.Contains(kvp.Value.s.Id)).ToDictionary(kvp => kvp.Key, kvp => Clone(kvp.Value.c));

                    if (middleByName.Any())
                    {
                        var moveToAux = SyncEnums(schema, table,
                            currentByName.Where(a => middleByName.ContainsKey(a.Key)).ToDictionary(),
                            middleByName);
                        if (moveToAux != null)
                            commands.Add(moveToAux);
                    }

                    var com = SyncEnums(schema, table,
                        currentByName.Where(a => !middleByName.ContainsKey(a.Key)).ToDictionary(),
                        shouldByName.Where(a => !middleByName.ContainsKey(a.Key)).ToDictionary());
                    if (com != null)
                        commands.Add(com);

                    if (middleByName.Any())
                    {
                        var backFromAux = SyncEnums(schema, table,
                            middleByName,
                            shouldByName.Where(a => middleByName.ContainsKey(a.Key)).ToDictionary());
                        if (backFromAux != null)
                            commands.Add(backFromAux);
                    }
                }
            }

            return SqlPreCommand.Combine(Spacing.Double, commands.ToArray());
        }

        private static SqlPreCommand SyncEnums(Schema schema, Table table, Dictionary<string, Entity> current, Dictionary<string, Entity> should)
        {
            var deletes = Synchronizer.SynchronizeScript(Spacing.Double, should, current,
                       createNew: null,
                       removeOld: (str, c) => table.DeleteSqlSync(c, null, comment: c.toStr),
                       mergeBoth: null);

            var moves = Synchronizer.SynchronizeScript(Spacing.Double, should, current,
                       createNew: null,
                       removeOld: null,
                       mergeBoth: (str, s, c) =>
                       {
                           if (s.id == c.id)
                               return table.UpdateSqlSync(c, null, comment: c.toStr);

                           var insert = table.InsertSqlSync(s);

                           var move = (from t in schema.GetDatabaseTables()
                                       from col in t.Columns.Values
                                       where col.ReferenceTable == table
                                       select new SqlPreCommandSimple("UPDATE {0} SET {1} = {2} WHERE {1} = {3} -- {4} re-indexed"
                                           .FormatWith(t.Name, col.Name, s.Id, c.Id, c.toStr)))
                                        .Combine(Spacing.Simple);

                           var delete = table.DeleteSqlSync(c, null, comment: c.toStr);

                           return SqlPreCommand.Combine(Spacing.Simple, insert, move, delete);
                       });

            var creates = Synchronizer.SynchronizeScript(Spacing.Double, should, current,
                  createNew: (str, s) => table.InsertSqlSync(s),
                  removeOld: null,
                  mergeBoth: null);

            return SqlPreCommand.Combine(Spacing.Double, deletes, moves, creates);
        }

        private static Entity Clone(Entity current)
        {
            var instance = (Entity)Activator.CreateInstance(current.GetType());
            instance.toStr = current.toStr;
            instance.id = (int)current.id.Value + 1000000;
            return instance;
        }

        public static SqlPreCommand SnapshotIsolation(Replacements replacements)
        {
            if (replacements.SchemaOnly)
                return null;

            var list = Schema.Current.DatabaseNames().Select(a => a?.ToString()).ToList();

            if (list.Contains(null))
            {
                list.Remove(null);
                list.Add(Connector.Current.DatabaseName());
            }

            var results = Database.View<SysDatabases>()
                .Where(d => list.Contains(d.name))
                .Select(d => new { d.name, d.snapshot_isolation_state, d.is_read_committed_snapshot_on }).ToList();

            var cmd = replacements.WithReplacedDatabaseName().Using(_ => results.Select((a, i) =>
                SqlPreCommand.Combine(Spacing.Simple,
                !a.snapshot_isolation_state || !a.is_read_committed_snapshot_on ? DisconnectUsers(a.name, "SPID" + i) : null,
                !a.snapshot_isolation_state ? SqlBuilder.SetSnapshotIsolation(a.name, true) : null,
                !a.is_read_committed_snapshot_on ? SqlBuilder.MakeSnapshotIsolationDefault(a.name, true) : null)).Combine(Spacing.Double));

            if (cmd == null)
                return null;

            return SqlPreCommand.Combine(Spacing.Double,
                new SqlPreCommandSimple("use master -- Start Snapshot"),
                cmd,
                new SqlPreCommandSimple("use {0} -- Stop Snapshot".FormatWith(Connector.Current.DatabaseName())));
        }

        public static SqlPreCommandSimple DisconnectUsers(string databaseName, string variableName)
        {
            return new SqlPreCommandSimple(@"DECLARE @{1} VARCHAR(7000)
SELECT @{1} = COALESCE(@{1},'')+'KILL '+CAST(SPID AS VARCHAR)+'; 'FROM master..SysProcesses WHERE DB_NAME(DBId) = '{0}'
EXEC(@{1})".FormatWith(databaseName, variableName));
        }
    }

    public class DiffPeriod
    {
        public string StartColumnName;
        public string EndColumnName;

        internal bool PeriodEquals(SystemVersionedInfo systemVersioned)
        {
            return systemVersioned.StartColumnName == StartColumnName && 
                systemVersioned.EndColumnName == EndColumnName;
        }
    }

    public class DiffTable
    {
        public ObjectName Name;

        public ObjectName PrimaryKeyName;

        public Dictionary<string, DiffColumn> Columns;

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

        public SysTableTemporalType TemporalType;
        public ObjectName TemporalTableName;
        public DiffPeriod Period;

        public Dictionary<string, DiffIndex> Indices = new Dictionary<string, DiffIndex>();

        public List<DiffStats> Stats = new List<DiffStats>();

        public List<DiffForeignKey> MultiForeignKeys = new List<DiffForeignKey>();

        public void ForeignKeysToColumns()
        {
            foreach (var fk in MultiForeignKeys.Where(a => a.Columns.Count == 1).ToList())
            {
                this.Columns[fk.Columns.SingleEx().Parent].ForeignKey = fk;
                MultiForeignKeys.Remove(fk);
            }
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }

    public class DiffStats
    {
        public string StatsName;

        public List<string> Columns;
    }

    public class DiffIndexColumn
    {
        public string ColumnName;
        public bool IsIncluded;
    }

    public class DiffIndex
    {
        public bool IsUnique;
        public bool IsPrimary;
        public string IndexName;
        public string ViewName;
        public string FilterDefinition;
        public DiffIndexType? Type;

        public List<DiffIndexColumn> Columns;

        public override string ToString()
        {
            return "{0} ({1})".FormatWith(IndexName, Columns.ToString(", "));
        }

        internal bool IndexEquals(DiffTable dif, Index mix)
        {
            if (this.ViewName != (mix as UniqueIndex)?.ViewName)
                return false;

            if (this.ColumnsChanged(dif, mix))
                return false;

            if (this.IsPrimary != mix is PrimaryClusteredIndex)
                return false;

            if (this.Type != GetIndexType(mix))
                return false;

            return true;
        }

        private static DiffIndexType? GetIndexType(Index mix)
        {
            if (mix is UniqueIndex && ((UniqueIndex)mix).ViewName != null)
                return null;

            if (mix is PrimaryClusteredIndex)
                return DiffIndexType.Clustered;

            return DiffIndexType.NonClustered;
        }

        bool ColumnsChanged(DiffTable dif, Index mix)
        {
            bool sameCols = IdenticalColumns(dif, mix.Columns, this.Columns.Where(a => !a.IsIncluded).ToList());
            bool sameIncCols = IdenticalColumns(dif, mix.IncludeColumns, this.Columns.Where(a => a.IsIncluded).ToList());

            if (sameCols && sameIncCols)
                return false;

            return true;
        }

        private static bool IdenticalColumns(DiffTable dif, IColumn[] modColumns, List<DiffIndexColumn> diffColumns)
        {
            if ((modColumns?.Length ?? 0) != diffColumns.Count)
                return false;

            if (diffColumns.Count == 0)
                return true;

            var difColumns = diffColumns.Select(cn => dif.Columns.Values.SingleOrDefault(dc => dc.Name == cn.ColumnName)).ToList(); //Ny old name

            var perfect = difColumns.ZipOrDefault(modColumns, (dc, mc) => dc != null && mc != null && dc.ColumnEquals(mc, ignorePrimaryKey: true, ignoreIdentity: true, ignoreGenerateAlways: true)).All(a => a);
            return perfect;
        }

        public bool IsControlledIndex
        {
            get { return IndexName.StartsWith("IX_") || IndexName.StartsWith("UIX_"); }
        }
    }

    public enum DiffIndexType
    {
        Heap = 0,
        Clustered = 1,
        NonClustered = 2,
        Xml = 3,
        Spatial = 4,
        ClusteredColumnstore = 5,
        NonClusteredColumnstore = 6,
        NonClusteredHash = 7,
    }

    public enum GeneratedAlwaysType
    {
        None = 0,
        AsRowStart = 1,
        AsRowEnd = 2
    }

    public class DiffDefaultConstraint
    {
        public string Name;
        public string Definition;
    }

    public class DiffColumn
    {
        public string Name;
        public SqlDbType SqlDbType;
        public string UserTypeName;
        public bool Nullable;
        public string Collation;
        public int Length;
        public int Precission;
        public int Scale;
        public bool Identity;
        public bool PrimaryKey;

        public DiffForeignKey ForeignKey;

        public DiffDefaultConstraint DefaultConstraint;

        public GeneratedAlwaysType GeneratedAlwaysType;

        public bool ColumnEquals(IColumn other, bool ignorePrimaryKey, bool ignoreIdentity, bool ignoreGenerateAlways)
        {
            var result =
                   SqlDbType == other.SqlDbType
                && Collation == other.Collation
                && StringComparer.InvariantCultureIgnoreCase.Equals(UserTypeName, other.UserDefinedTypeName)
                && Nullable == (other.Nullable.ToBool())
                && (other.Size == null || other.Size.Value == Precission || other.Size.Value == Length / BytesPerChar(other.SqlDbType) || other.Size.Value == int.MaxValue && Length == -1)
                && (other.Scale == null || other.Scale.Value == Scale)
                && (ignoreIdentity || Identity == other.Identity)
                && (ignorePrimaryKey || PrimaryKey == other.PrimaryKey)
                && (ignoreGenerateAlways || GeneratedAlwaysType == other.GetGeneratedAlwaysType());

            return result;
        }

        public static int BytesPerChar(System.Data.SqlDbType sqlDbType)
        {
            if (sqlDbType == System.Data.SqlDbType.NChar || sqlDbType == System.Data.SqlDbType.NText || sqlDbType == System.Data.SqlDbType.NVarChar)
                return 2;

            return 1;
        }

        public bool DefaultEquals(IColumn other)
        {
            if (other.Default == null && this.DefaultConstraint == null)
                return true;

            var result = CleanParenthesis(this.DefaultConstraint?.Definition) == CleanParenthesis(other.Default);

            return result;
        }

        private string CleanParenthesis(string p)
        {
            if (p == null)
                return null;

            while (
                p.StartsWith("(") && p.EndsWith(")") ||
                p.StartsWith("'") && p.EndsWith("'"))
                p = p.Substring(1, p.Length - 2);

            return p.ToLower();
        }

        public DiffColumn Clone()
        {
            return new DiffColumn
            {
                Name = Name,
                ForeignKey = ForeignKey,
                DefaultConstraint = DefaultConstraint?.Let(dc => new DiffDefaultConstraint { Name = dc.Name, Definition = dc.Definition }),
                Identity = Identity,
                Length = Length,
                PrimaryKey = PrimaryKey,
                Nullable = Nullable,
                Precission = Precission,
                Scale = Scale,
                SqlDbType = SqlDbType,
                UserTypeName = UserTypeName,
            };
        }

        public override string ToString()
        {
            return this.Name;
        }

        internal bool CompatibleTypes(IColumn tabCol)
        {
            //https://docs.microsoft.com/en-us/sql/t-sql/functions/cast-and-convert-transact-sql
            switch (this.SqlDbType)
            {
                //BLACKLIST!!
                case SqlDbType.Binary:
                case SqlDbType.VarBinary:                    
                    switch (tabCol.SqlDbType)
                    {
                        case SqlDbType.Float:
                        case SqlDbType.Real:
                        case SqlDbType.NText:
                        case SqlDbType.Text:
                            return false;
                        default:
                            return true;
                    }

                case SqlDbType.Char:
                case SqlDbType.VarChar:
                    return true;

                case SqlDbType.NChar:
                case SqlDbType.NVarChar:
                    return tabCol.SqlDbType != SqlDbType.Image;

                case SqlDbType.DateTime:
                case SqlDbType.SmallDateTime:
                    switch (tabCol.SqlDbType)
                    {
                        case SqlDbType.UniqueIdentifier:
                        case SqlDbType.Image:
                        case SqlDbType.NText:
                        case SqlDbType.Text:
                        case SqlDbType.Xml:
                        case SqlDbType.Udt:
                            return false;
                        default:
                            return true;
                    }

                case SqlDbType.Date:
                    if (tabCol.SqlDbType == SqlDbType.Time)
                        return false;
                    goto case SqlDbType.DateTime2;

                case SqlDbType.Time:
                    if (tabCol.SqlDbType == SqlDbType.Date)
                        return false;
                    goto case SqlDbType.DateTime2;

                case SqlDbType.DateTimeOffset:
                case SqlDbType.DateTime2:
                    switch (tabCol.SqlDbType)
                    {
                        case SqlDbType.Decimal:
                        case SqlDbType.Float:
                        case SqlDbType.Real:
                        case SqlDbType.BigInt:
                        case SqlDbType.Int:
                        case SqlDbType.SmallInt:
                        case SqlDbType.TinyInt:
                        case SqlDbType.Money:
                        case SqlDbType.SmallMoney:
                        case SqlDbType.Bit:
                        case SqlDbType.UniqueIdentifier:
                        case SqlDbType.Image:
                        case SqlDbType.NText:
                        case SqlDbType.Text:
                        case SqlDbType.Xml:
                        case SqlDbType.Udt:
                            return false;
                        default:
                            return true;
                    }

                case SqlDbType.Decimal:
                case SqlDbType.Float:
                case SqlDbType.Real:
                case SqlDbType.BigInt:
                case SqlDbType.Int:
                case SqlDbType.SmallInt:
                case SqlDbType.TinyInt:
                case SqlDbType.Money:
                case SqlDbType.SmallMoney:
                case SqlDbType.Bit:
                    switch (tabCol.SqlDbType)
                    {
                        case SqlDbType.Date:                         
                        case SqlDbType.Time:                            
                        case SqlDbType.DateTimeOffset:
                        case SqlDbType.DateTime2:
                        case SqlDbType.UniqueIdentifier:
                        case SqlDbType.Image:
                        case SqlDbType.NText:
                        case SqlDbType.Text:
                        case SqlDbType.Xml:
                        case SqlDbType.Udt:
                            return false;
                        default:
                            return true;
                    }
                   
                case SqlDbType.Timestamp:
                    switch (tabCol.SqlDbType)
                    {
                        case SqlDbType.NChar:
                        case SqlDbType.NVarChar:
                        case SqlDbType.Date:
                        case SqlDbType.Time:
                        case SqlDbType.DateTimeOffset:
                        case SqlDbType.DateTime2:
                        case SqlDbType.UniqueIdentifier:
                        case SqlDbType.Image:
                        case SqlDbType.NText:
                        case SqlDbType.Text:
                        case SqlDbType.Xml:
                        case SqlDbType.Udt:
                            return false;
                        default:
                            return true;
                    }
                case SqlDbType.Variant:
                    switch (tabCol.SqlDbType)
                    {
                        case SqlDbType.Timestamp:
                        case SqlDbType.Image:
                        case SqlDbType.NText:
                        case SqlDbType.Text:
                        case SqlDbType.Xml:
                        case SqlDbType.Udt:
                            return false;
                        default:
                            return true;
                    }

                //WHITELIST!!
                case SqlDbType.UniqueIdentifier:
                    switch (tabCol.SqlDbType)
                    {
                        case SqlDbType.Binary:
                        case SqlDbType.VarBinary:
                        case SqlDbType.Char:
                        case SqlDbType.VarChar:
                        case SqlDbType.NChar:
                        case SqlDbType.NVarChar:
                        case SqlDbType.Variant:
                            return true;
                        default:
                            return true;
                    }
                case SqlDbType.Image:
                    switch (tabCol.SqlDbType)
                    {
                        case SqlDbType.Binary:
                        case SqlDbType.VarBinary:
                        case SqlDbType.Timestamp:
                            return true;
                        default:
                            return true;
                    }
                case SqlDbType.NText:
                case SqlDbType.Text:
                    switch (tabCol.SqlDbType)
                    {
                        case SqlDbType.Char:
                        case SqlDbType.VarChar:
                        case SqlDbType.NChar:
                        case SqlDbType.NVarChar:
                        case SqlDbType.NText:
                        case SqlDbType.Text:
                        case SqlDbType.Xml:
                            return true;
                        default:
                            return true;
                    }
                case SqlDbType.Xml:
                case SqlDbType.Udt:
                    switch (tabCol.SqlDbType)
                    {
                        case SqlDbType.Binary:
                        case SqlDbType.VarBinary:
                        case SqlDbType.Char:
                        case SqlDbType.VarChar:
                        case SqlDbType.NChar:
                        case SqlDbType.NVarChar:
                        case SqlDbType.Xml:
                        case SqlDbType.Udt:
                            return true;
                        default:
                            return true;
                    }
                default:
                    throw new NotImplementedException("Unexpected SqlDbType");
            }
        }
    }

    public class DiffForeignKey
    {
        public ObjectName Name;
        public ObjectName TargetTable;
        public bool IsDisabled;
        public bool IsNotTrusted;
        public List<DiffForeignKeyColumn> Columns;
    }

    public class DiffForeignKeyColumn
    {
        public string Parent;
        public string Referenced;
    }
}
