using NpgsqlTypes;
using Signum.Engine.Linq;
using Signum.Engine.Maps;
using Signum.Engine.Sync.Postgres;
using Signum.Engine.Sync.SqlServer;
using Signum.Utilities;
using System.Data;
using System.Text.RegularExpressions;

namespace Signum.Engine.Sync;

public static class SchemaSynchronizer
{
    public static Func<SchemaName, bool> DropSchema = s => !s.Name.Contains(@"\");

    public static Action<Dictionary<string, DiffTable>>? SimplifyDiffTables;

    public static SqlPreCommand? SynchronizeTablesScript(Replacements replacements)
    {
        Schema s = Schema.Current;

        var sqlBuilder = Connector.Current.SqlBuilder;

        Dictionary<string, ITable> modelTables = s.GetDatabaseTables().Where(t => !s.IsExternalDatabase(t.Name.Schema.Database)).ToDictionaryEx(a => a.Name.ToString(), "schema tables");
        var modelTablesHistory = modelTables.Values.Where(a => a.SystemVersioned != null).ToDictionaryEx(a => a.SystemVersioned!.TableName.ToString(), "history schema tables");
        HashSet<SchemaName> modelSchemas = modelTables.Values.Select(a => a.Name.Schema).Where(a => !sqlBuilder.SystemSchemas.Contains(a.Name)).ToHashSet();

        Dictionary<string, DiffTable> databaseTables = Schema.Current.Settings.IsPostgres ?
            PostgresCatalogSchema.GetDatabaseDescription(Schema.Current.DatabaseNames()) :
            SysTablesSchema.GetDatabaseDescription(s.DatabaseNames());

        var databaseTablesHistory = databaseTables.Extract((key, val) => val.TemporalType == SysTableTemporalType.HistoryTable);
        HashSet<SchemaName> databaseSchemas = Schema.Current.Settings.IsPostgres ?
            PostgresCatalogSchema.GetSchemaNames(s.DatabaseNames()) :
            SysTablesSchema.GetSchemaNames(s.DatabaseNames());


        SimplifyDiffTables?.Invoke(databaseTables);

        replacements.AskForReplacements(databaseTables.Keys.ToHashSet(), modelTables.Keys.ToHashSet(), Replacements.KeyTables);

        databaseTables = replacements.ApplyReplacementsToOld(databaseTables, Replacements.KeyTables);

        replacements.AskForReplacements(databaseTablesHistory.Keys.ToHashSet(), modelTablesHistory.Keys.ToHashSet(), Replacements.KeyTables);

        databaseTablesHistory = replacements.ApplyReplacementsToOld(databaseTablesHistory, Replacements.KeyTables);

        Dictionary<ITable, Dictionary<string, TableIndex>> modelIndices = modelTables.Values
            .ToDictionary(t => t, t => t.GeneratAllIndexes().ToDictionaryEx(a => a.IndexName, "Indexes for {0}".FormatWith(t.Name)));

        var bla = modelIndices.OrderBy(a => a.Key.ToString()).ToList();

        var modelFullTextCatallogs = (from kvp in modelIndices
                                      from fti in kvp.Value.Values.OfType<FullTextTableIndex>()
                                      select new FullTextCatallogName(fti.CatallogName, kvp.Key.Name.Schema.Database)).Distinct().ToList();

        List<FullTextCatallogName> databaseFullTextCatallogs = Schema.Current.Settings.IsPostgres ?
            PostgresCatalogSchema.GetFullTextSearchCatallogs(s.DatabaseNames()) :
            SysTablesSchema.GetFullTextSearchCatallogs(s.DatabaseNames());


        //To --> From
        Dictionary<ObjectName, Dictionary<string, string>> preRenameColumnsList = new Dictionary<ObjectName, Dictionary<string, string>>();
        HashSet<ITable> primaryKeyTypeChanged = new HashSet<ITable>();

        //A -> A_temp

        modelTables.JoinDictionaryForeach(databaseTables, (tn, tab, diff) =>
        {
            var key = Replacements.KeyColumnsForTable(tn);

            var incompatibleTypes = diff.Columns.JoinDictionary(tab.Columns, (cn, diff, col) => new { cn, diff, col }).Values.Where(a => !a.diff.CompatibleTypes(a.col) || a.diff.Identity != a.col.Identity).ToList();

            foreach (var inc in incompatibleTypes.Where(kvp => kvp.col.Name == kvp.diff.Name))
            {
                var newColName = inc.diff.Name + "_OLD";
                preRenameColumnsList.GetOrCreate(diff.Name).Add(inc.diff.Name, newColName);
                inc.diff.Name = newColName;
            }

            var diffPk = diff.Columns.Values.Where(a => a.PrimaryKey).Only();
            var pk = tab.PrimaryKey;

            if (diffPk != null && diffPk.CompatibleTypes(pk))
                primaryKeyTypeChanged.Add(tab);

            diff.Columns = replacements.ApplyReplacementsToOld(diff.Columns, key);
            diff.Indices = ApplyIndexAutoReplacements(diff, tab, modelIndices[tab]);

            if (diff.TemporalTableName != null)
            {
                var diffTemp = databaseTablesHistory.GetOrThrow(replacements.Apply(Replacements.KeyTables, diff.TemporalTableName.ToString()));
                diffTemp.Columns = replacements.ApplyReplacementsToOld(diffTemp.Columns, key);
                diffTemp.Indices = ApplyIndexAutoReplacements(diffTemp, tab, modelIndices[tab]);
            }

        });

        var tableReplacements = replacements.TryGetC(Replacements.KeyTables);
        if (tableReplacements != null)
            replacements[Replacements.KeyTablesInverse] = tableReplacements.Inverse();

        Func<ObjectName, ObjectName> ChangeName = (ObjectName objectName) =>
        {
            string name = replacements.Apply(Replacements.KeyTables, replacements.ConcretizeObjectName(objectName));

            return modelTables.TryGetC(name)?.Name ?? objectName;
        };

        using (replacements.WithReplacedDatabaseName())
        {
            SqlPreCommand? preRenameColumns = preRenameColumnsList
                .Select(kvp => kvp.Value.Select(kvp2 => sqlBuilder.RenameColumn(kvp.Key, kvp2.Key, kvp2.Value)).Combine(Spacing.Simple))
                .Combine(Spacing.Double);

            if (preRenameColumns != null)
                preRenameColumns.GoAfter = true;

            SqlPreCommand? createFullTextCatallogs = Synchronizer.SynchronizeScript(Spacing.Double,
                modelFullTextCatallogs.ToDictionary(a => a),
                databaseFullTextCatallogs.ToDictionary(a => a),
                createNew: (_, newSN) => sqlBuilder.CreateFullTextCatallog(newSN),
                removeOld: null,
                mergeBoth: null
                );

            SqlPreCommand? createSchemas = Synchronizer.SynchronizeScriptReplacing(replacements, "Schemas", Spacing.Double,
                modelSchemas.ToDictionary(a => a.ToString()),
                databaseSchemas.ToDictionary(a => a.ToString()),
                createNew: (_, newSN) => sqlBuilder.CreateSchema(newSN),
                removeOld: null,
                mergeBoth: (_, newSN, oldSN) => newSN.Equals(oldSN) ? null : sqlBuilder.CreateSchema(newSN)
                );

            //use database without replacements to just remove indexes
            SqlPreCommand? dropStatistics =
                Synchronizer.SynchronizeScript(Spacing.Double, modelTables, databaseTables,
                createNew: null,
                removeOld: (tn, dif) => sqlBuilder.DropStatistics(tn, dif.Stats),
                mergeBoth: (tn, tab, dif) =>
                {
                    var removedColums = dif.Columns.Keys.Except(tab.Columns.Keys).ToHashSet();

                    return sqlBuilder.DropStatistics(tn, dif.Stats.Where(a => a.Columns.Any(removedColums.Contains)).ToList());
                });


            SqlPreCommand? dropIndices =
                Synchronizer.SynchronizeScript(Spacing.Double, modelTables, databaseTables,
                createNew: null,
                removeOld: (tn, dif) => dif.Indices.Values.Where(ix => !ix.IsPrimary).Select(ix => sqlBuilder.DropIndex(dif.Name, ix)).Combine(Spacing.Simple),
                mergeBoth: (tn, tab, dif) =>
                {
                    Dictionary<string, TableIndex> modelIxs = modelIndices[tab];
                    
                    bool IsColumnRemovedOrModified(DiffIndexColumn c)
                    {
                        var newName = dif.Columns.ContainsKey(c.ColumnName) ? c.ColumnName : dif.Columns.SingleEx(a => a.Value.Name == c.ColumnName).Key;
                        var tc = tab.Columns.TryGetC(newName);
                        return tc == null || !dif.Columns[newName].ColumnEquals(tc, true, true, true);
                    }

                    var changes = Synchronizer.SynchronizeScript(Spacing.Simple,
                        modelIxs.Where(kvp => !(kvp.Value is PrimaryKeyIndex)).ToDictionary(),
                        dif.Indices.Where(kvp => !kvp.Value.IsPrimary).ToDictionary(),
                        createNew: null,
                        removeOld: (i, dix) => dix.IsControlledIndex || dix.Columns.Any(IsColumnRemovedOrModified) ? sqlBuilder.DropIndex(dif.Name, dix) : null,
                        mergeBoth: (i, mix, dix) => !dix.IndexEquals(dif, mix) ? sqlBuilder.DropIndex(dif.Name, dix) : null
                        );

                    return changes;
                });

            SqlPreCommand? dropIndicesHistory =
                Synchronizer.SynchronizeScript(Spacing.Double, modelTablesHistory, databaseTablesHistory,
                createNew: null,
                removeOld: (tn, dif) => dif.Indices.Values.Where(ix => !ix.IsPrimary).Select(ix => sqlBuilder.DropIndex(dif.Name, ix)).Combine(Spacing.Simple),
                mergeBoth: (tn, tab, dif) =>
                {
                    Dictionary<string, TableIndex> modelIxs = modelIndices[tab];

                    var removedColums = dif.Columns.Keys.Except(tab.Columns.Keys).ToHashSet();

                    var changes = Synchronizer.SynchronizeScript(Spacing.Simple,
                        modelIxs.Where(kvp => kvp.Value.GetType() == typeof(TableIndex)).ToDictionary(),
                        dif.Indices.Where(kvp => !kvp.Value.IsPrimary).ToDictionary(),
                        createNew: null,
                        removeOld: (i, dix) => dix.Columns.Any(c => removedColums.Contains(c.ColumnName)) || dix.IsControlledIndex ? sqlBuilder.DropIndex(dif.Name, dix) : null,
                        mergeBoth: (i, mix, dix) => !dix.IndexEquals(dif, mix) ? sqlBuilder.DropIndex(dif.Name, dix) : null
                        );

                    return changes;
                });

            SqlPreCommand? dropForeignKeys = Synchronizer.SynchronizeScript(
                 Spacing.Double,
                 modelTables,
                 databaseTables,
                 createNew: null,
                 removeOld: (tn, dif) => dif.Columns.Values.Select(c => c.ForeignKey != null ? sqlBuilder.AlterTableDropConstraint(dif.Name, c.ForeignKey.Name) : null)
                     .Concat(dif.MultiForeignKeys.Select(fk => sqlBuilder.AlterTableDropConstraint(dif.Name, fk.Name))).Combine(Spacing.Simple),
                 mergeBoth: (tn, tab, dif) => SqlPreCommand.Combine(Spacing.Simple,
                     Synchronizer.SynchronizeScript(
                     Spacing.Simple,
                     tab.Columns,
                     dif.Columns,
                     createNew: null,
                     removeOld: (cn, colDb) => colDb.ForeignKey != null ? sqlBuilder.AlterTableDropConstraint(dif.Name, colDb.ForeignKey.Name) : null,
                     mergeBoth: (cn, colModel, colDb) => colDb.ForeignKey == null ? null :
                         colModel.ReferenceTable == null || colModel.AvoidForeignKey || !colModel.ReferenceTable.Name.Equals(ChangeName(colDb.ForeignKey.TargetTable)) || DifferentDatabase(tab.Name, colModel.ReferenceTable.Name) || DifferentDatabase(tab.Name, dif.Name) || !colDb.DbType.Equals(colModel.DbType) ?
                         sqlBuilder.AlterTableDropConstraint(dif.Name, colDb.ForeignKey.Name) :
                         null),
                    dif.MultiForeignKeys.Select(fk => sqlBuilder.AlterTableDropConstraint(dif.Name, fk.Name)).Combine(Spacing.Simple))
            );

            HashSet<FieldEmbedded.EmbeddedHasValueColumn> hasValueFalse = new HashSet<FieldEmbedded.EmbeddedHasValueColumn>();

            List<SqlPreCommand?> delayedUpdates = new List<SqlPreCommand?>();
            List<SqlPreCommand?> delayedDrops = new List<SqlPreCommand?>();
            List<SqlPreCommand?> delayedAddSystemVersioning = new List<SqlPreCommand?>();

            SqlPreCommand? tables =
                    Synchronizer.SynchronizeScript(
                    Spacing.Double,
                    modelTables,
                    databaseTables,
                    createNew: (tn, tab) => SqlPreCommand.Combine(Spacing.Double,
                        sqlBuilder.CreateTableSql(tab)
                    ),
                    removeOld: (tn, dif) => sqlBuilder.DropTable(dif),
                    mergeBoth: (tn, tab, dif) =>
                    {
                        var rename = !object.Equals(dif.Name, tab.Name) ? sqlBuilder.RenameOrMove(dif, tab, tab.Name, forHistoryTable: false) : null;

                        bool disableEnableSystemVersioning = false;

                        var disableSystemVersioning = !sqlBuilder.IsPostgres && dif.TemporalType != SysTableTemporalType.None &&
                        (tab.SystemVersioned == null ||
                        !object.Equals(replacements.Apply(Replacements.KeyTables, dif.TemporalTableName!.ToString()), tab.SystemVersioned.TableName.ToString()) && !DifferentDatabase(tab.Name, dif.Name) ||
                        (disableEnableSystemVersioning = StrongColumnChanges(tab, dif))) ?
                        sqlBuilder.AlterTableDisableSystemVersioning(tab.Name).Do(a => a.GoAfter = true) :
                        null;

                        var dropPeriod = !sqlBuilder.IsPostgres && dif.Period != null &&
                            (tab.SystemVersioned == null || !dif.Period.PeriodEquals(tab.SystemVersioned)) ?
                            sqlBuilder.AlterTableDropPeriod(tab) : null;

                        var modelPK = modelIndices[tab].Values.OfType<PrimaryKeyIndex>().SingleOrDefaultEx();
                        var diffPK = dif.Indices.Values.SingleOrDefaultEx(a => a.IsPrimary);

                        var dropPrimaryKey = diffPK != null && (modelPK == null || !diffPK.IndexEquals(dif, modelPK)) ? sqlBuilder.DropIndex(tab.Name, diffPK) :
                         diffPK != null && modelPK != null && diffPK.IndexName != modelPK.IndexName ? sqlBuilder.RenameForeignKey(tab.Name, new ObjectName(dif.Name.Schema, diffPK.IndexName, sqlBuilder.IsPostgres), modelPK.IndexName) :
                        null;

                        var columns = Synchronizer.SynchronizeScript(
                                Spacing.Simple,
                                tab.Columns,
                                dif.Columns,

                                createNew: (cn, tabCol) =>
                                {

                                    var result = SqlPreCommand.Combine(Spacing.Simple,
                                        tabCol.PrimaryKey && dif.PrimaryKeyName != null ? sqlBuilder.DropPrimaryKeyConstraint(tab.Name) : null,
                                        AlterTableAddColumnDefault(sqlBuilder, tab, tabCol, replacements,
                                            forceDefaultValue: cn.EndsWith("_HasValue") && dif.Columns.Values.Any(c => c.Name.StartsWith(cn.Before("HasValue")) && c.Nullable == false) ? "1" : null,
                                            hasValueFalse: hasValueFalse,
                                            avoidDefault: false));

                                    return result;
                                },

                                removeOld: (cn, difCol) =>
                                {
                                    var result = SqlPreCommand.Combine(Spacing.Simple,
                                         difCol.DefaultConstraint != null && difCol.DefaultConstraint.Name != null ? sqlBuilder.AlterTableDropConstraint(tab.Name, difCol.DefaultConstraint!.Name) : null,
                                        sqlBuilder.AlterTableDropColumn(tab, cn));

                                    return result;
                                },

                                mergeBoth: (cn, tabCol, difCol) =>
                                {
                                    if (difCol.CompatibleTypes(tabCol) && difCol.Identity == tabCol.Identity)
                                    {
                                        var columnEquals = difCol.ColumnEquals(tabCol, ignorePrimaryKey: true, ignoreIdentity: false, ignoreGenerateAlways: true);
                                        var defaultEquals = difCol.DefaultEquals(tabCol);
                                        var checkEquals = difCol.CheckEquals(tabCol);

                                        return SqlPreCommand.Combine(Spacing.Simple,

                                            difCol.Name == tabCol.Name ? null : sqlBuilder.RenameColumn(tab.Name, difCol.Name, tabCol.Name),

                                            (!columnEquals || !defaultEquals) && difCol.DefaultConstraint != null ? sqlBuilder.AlterTableDropDefaultConstaint(tab.Name, difCol) : null,
                                            (!columnEquals || !checkEquals) && difCol.CheckConstraint != null ? sqlBuilder.AlterTableDropConstraint(tab.Name, difCol.CheckConstraint.Name) : null,

                                            columnEquals ?
                                                null :
                                                SqlPreCommand.Combine(Spacing.Simple,
                                                    tabCol.PrimaryKey && !difCol.PrimaryKey && dif.PrimaryKeyName != null ? sqlBuilder.DropPrimaryKeyConstraint(tab.Name) : null,
                                                    UpdateCompatible(sqlBuilder, replacements, tab, dif, tabCol, difCol),
                                                    (sqlBuilder.IsPostgres ?
                                                    tabCol.DbType.PostgreSql == NpgsqlDbType.Varchar && difCol.DbType.PostgreSql == NpgsqlDbType.Char :
                                                    tabCol.DbType.SqlServer == SqlDbType.NVarChar && difCol.DbType.SqlServer == SqlDbType.NChar) ? sqlBuilder.UpdateTrim(tab, tabCol) : null),

                                            UpdateByFkChange(tn, difCol, tabCol, ChangeName),

                                            (!columnEquals || !defaultEquals) && tabCol.Default != null ? sqlBuilder.AlterTableAddDefaultConstraint(tab.Name, sqlBuilder.GetDefaultConstaint(tab, tabCol)!) : null,
                                            (!columnEquals || !defaultEquals) && tabCol.Check != null ? sqlBuilder.AlterTableAddCheckConstraint(tab.Name, sqlBuilder.GetCheckConstaint(tab, tabCol)!) : null
                                        );
                                    }
                                    else
                                    {
                                        var update = difCol.PrimaryKey ? null : UpdateForeignKeyTypeChanged(sqlBuilder, tab, dif, tabCol, difCol, ChangeName, preRenameColumnsList) ?? UpdateCustom(tab, tabCol, difCol);
                                        var drop = sqlBuilder.AlterTableDropColumn(tab, difCol.Name);

                                        delayedUpdates.Add(update);
                                        delayedDrops.Add(SqlPreCommand.Combine(Spacing.Simple,
                                            difCol.DefaultConstraint != null ? sqlBuilder.AlterTableDropDefaultConstaint(tab.Name, difCol) : null,
                                            drop
                                        ));

                                        if (disableSystemVersioning != null)
                                        {
                                            delayedUpdates.Add(update == null ? null : ForHistoryTable(update, tab));
                                            delayedDrops.Add(drop == null ? null : ForHistoryTable(drop, tab));
                                        }

                                        return SqlPreCommand.Combine(Spacing.Simple,
                                            AlterTableAddColumnDefaultZero(sqlBuilder, tab, tabCol)
                                        );
                                    }
                                }
                        );

                        var createPrimaryKey = modelPK != null && (diffPK == null || !diffPK.IndexEquals(dif, modelPK)) ? sqlBuilder.CreateIndex(modelPK, checkUnique: null) : null;


                        var columnsHistory = columns != null && (disableEnableSystemVersioning || sqlBuilder.IsPostgres && tab.SystemVersioned != null && dif.TemporalType == SysTableTemporalType.SystemVersionTemporalTable) ?
                            ForHistoryTable(columns, tab).Replace(new Regex(" IDENTITY "), m => " ") : null;/*HACK*/

                        var addPeriod = !sqlBuilder.IsPostgres && tab.SystemVersioned != null &&
                            (dif.Period == null || !dif.Period.PeriodEquals(tab.SystemVersioned) || DifferentDatabase(tab.Name, dif.Name)) ?
                            (SqlPreCommandSimple)sqlBuilder.AlterTableAddPeriod(tab) : null;

                        var addSystemVersioning = (!sqlBuilder.IsPostgres && tab.SystemVersioned != null &&
                            (dif.Period == null || dif.TemporalTableName == null ||
                            !object.Equals(replacements.Apply(Replacements.KeyTables, dif.TemporalTableName.ToString()), tab.SystemVersioned.TableName.ToString()) ||
                            disableEnableSystemVersioning) ?
                            sqlBuilder.AlterTableEnableSystemVersioning(tab).Do(a => a.GoBefore = true) : null);


                        SqlPreCommand? combinedAddPeriod = null;
                        if (addPeriod != null && columns is SqlPreCommandConcat cols)
                        {
                            var periodRows = cols.Leaves().Where(pcs => pcs.Sql.Contains(" ADD ") && pcs.Sql.Contains("GENERATED ALWAYS AS ROW")).ToList();
                            if (periodRows.Count == 2)
                            {
                                combinedAddPeriod = new SqlPreCommandSimple($@"ALTER TABLE {tn} ADD
    {periodRows[0].Sql.After(" ADD ").BeforeLast(";")},
    {periodRows[1].Sql.After(" ADD ").BeforeLast(";")},
    {addPeriod.Sql.After(" ADD ")}
");
                                addPeriod = null;
                                columns = cols.Leaves().Except(periodRows).Combine(cols.Spacing);
                            }
                        }

                        delayedAddSystemVersioning.Add(SqlPreCommand.Combine(Spacing.Simple, addPeriod, addSystemVersioning));

                        return SqlPreCommand.Combine(Spacing.Simple,
                            rename,
                            disableSystemVersioning,
                            dropPeriod,
                            dropPrimaryKey,
                            combinedAddPeriod,
                            columns,
                            columnsHistory,
                            createPrimaryKey);
                    });

            
            if (tables != null)
                tables.GoAfter = true;

            SqlPreCommand? historyTables = Synchronizer.SynchronizeScript(Spacing.Double, modelTablesHistory, databaseTablesHistory,
                createNew: (tn, tab) => sqlBuilder.IsPostgres ? sqlBuilder.CreateSystemTableVersionLike(tab) : null,
                removeOld: (tn, dif) => sqlBuilder.DropTable(dif.Name),
                mergeBoth: (tn, tab, dif) =>
                {
                    var rename = !object.Equals(dif.Name, tab.SystemVersioned!.TableName) ? sqlBuilder.RenameOrMove(dif, tab, tab.SystemVersioned!.TableName, forHistoryTable: true) : null;
                    return rename;
                });

            SqlPreCommand? versioningTriggers = !sqlBuilder.IsPostgres ? null : Synchronizer.SynchronizeScript(Spacing.Double, modelTables, databaseTables,
                 createNew: (tn, tab) =>
                 {
                     if(tab.SystemVersioned != null)
                         return sqlBuilder.CreateVersioningTrigger(tab, replace: false);

                     return null;
                 },
                 removeOld: null,
                   mergeBoth: (tn, tab, dif) =>
                   {
                       if(tab.SystemVersioned == null)
                       {
                           if (dif.VersionningTrigger == null)
                               return null;

                           return sqlBuilder.DropVersionningTrigger(tab.Name, dif.VersionningTrigger.tgname);
                       }
                       else
                       {
                           if (dif.VersionningTrigger == null)
                               return sqlBuilder.CreateVersioningTrigger(tab);

                           if (!Equals(PostgresCatalogSchema.ParseVersionFunctionParam(dif.VersionningTrigger.tgargs), tab.SystemVersioned.TableName))
                               return sqlBuilder.CreateVersioningTrigger(tab, replace: true);

                           return null;
                       }
                   });

            SqlPreCommand? syncEnums = SynchronizeEnumsScript(replacements);

            SqlPreCommand? addForeingKeys = Synchronizer.SynchronizeScript(
                 Spacing.Double,
                 modelTables,
                 databaseTables,
                 createNew: (tn, tab) => sqlBuilder.AlterTableForeignKeys(tab),
                 removeOld: null,
                 mergeBoth: (tn, tab, dif) => Synchronizer.SynchronizeScript(
                     Spacing.Simple,
                     tab.Columns,
                     dif.Columns,

                     createNew: (cn, colModel) => colModel.ReferenceTable == null || colModel.AvoidForeignKey || DifferentDatabase(tab.Name, colModel.ReferenceTable.Name) ? null :
                         sqlBuilder.AlterTableAddConstraintForeignKey(tab, colModel.Name, colModel.ReferenceTable),

                     removeOld: null,

                     mergeBoth: (cn, tabCol, difCol) =>
                     {
                         if (tabCol.ReferenceTable == null || tabCol.AvoidForeignKey || DifferentDatabase(tab.Name, tabCol.ReferenceTable.Name))
                             return null;

                         if (difCol.ForeignKey == null || !tabCol.ReferenceTable.Name.Equals(ChangeName(difCol.ForeignKey.TargetTable)) || !difCol.DbType.Equals(tabCol.DbType) || DifferentDatabase(tab.Name, dif.Name))
                             return sqlBuilder.AlterTableAddConstraintForeignKey(tab, tabCol.Name, tabCol.ReferenceTable);

                         var name = sqlBuilder.ForeignKeyName(tab.Name.Name, tabCol.Name);
                         return SqlPreCommand.Combine(Spacing.Simple,
                            name != difCol.ForeignKey.Name.Name ? sqlBuilder.RenameForeignKey(tab.Name, difCol.ForeignKey.Name.OnSchema(tab.Name.Schema), name) : null,
                            (difCol.ForeignKey.IsDisabled || difCol.ForeignKey.IsNotTrusted) && !replacements.SchemaOnly ? sqlBuilder.EnableForeignKey(tab.Name, name) : null);
                     })
                 );


            SqlPreCommand? addIndices =
                Synchronizer.SynchronizeScript(Spacing.Double, modelTables, databaseTables,
                createNew: (tn, tab) => modelIndices[tab].Values.Where(a => !(a is PrimaryKeyIndex)).Select(index => sqlBuilder.CreateIndex(index, null)).Combine(Spacing.Simple),
                removeOld: null,
                mergeBoth: (tn, tab, dif) =>
                {
                    var columnReplacements = replacements.TryGetC(Replacements.KeyColumnsForTable(tn));

                    Func<IColumn, bool> isNew = c => !dif.Columns.ContainsKey(columnReplacements?.TryGetC(c.Name) ?? c.Name);

                    Dictionary<string, TableIndex> modelIxs = modelIndices[tab];

                    var controlledIndexes = Synchronizer.SynchronizeScript(Spacing.Simple,
                        modelIxs.Where(kvp => !(kvp.Value is PrimaryKeyIndex)).ToDictionary(),
                        dif.Indices.Where(kvp => !kvp.Value.IsPrimary).ToDictionary(),
                        createNew: (i, mix) => mix is UniqueTableIndex or FullTextTableIndex || mix.Columns.Any(isNew) || ShouldCreateMissingIndex(mix, tab, replacements) ? sqlBuilder.CreateIndex(mix, checkUnique: replacements) : null,
                        removeOld: null,
                        mergeBoth: (i, mix, dix) => !dix.IndexEquals(dif, mix) ? sqlBuilder.CreateIndex(mix, checkUnique: replacements) :
                            mix.IndexName != dix.IndexName ? sqlBuilder.RenameIndex(tab.Name, dix.IndexName, mix.IndexName) : null);

                    return SqlPreCommand.Combine(Spacing.Simple, controlledIndexes);
                });

            SqlPreCommand? addIndicesHistory =
                Synchronizer.SynchronizeScript(Spacing.Double, modelTablesHistory, databaseTablesHistory,
                createNew: (tn, tab) => modelIndices[tab].Values.Where(a => a.GetType() == typeof(TableIndex)).Select(mix => sqlBuilder.CreateIndexBasic(mix, forHistoryTable: true)).Combine(Spacing.Simple),
                removeOld: null,
                mergeBoth: (tn, tab, dif) =>
                {
                    var columnReplacements = replacements.TryGetC(Replacements.KeyColumnsForTable(tn));

                    Func<IColumn, bool> isNew = c => !dif.Columns.ContainsKey(columnReplacements?.TryGetC(c.Name) ?? c.Name);

                    Dictionary<string, TableIndex> modelIxs = modelIndices[tab];

                    var controlledIndexes = Synchronizer.SynchronizeScript(Spacing.Simple,
                        modelIxs.Where(kvp => kvp.Value.GetType() == typeof(TableIndex)).ToDictionary(),
                        dif.Indices.Where(kvp => !kvp.Value.IsPrimary).ToDictionary(),
                        createNew: (i, mix) => mix is UniqueTableIndex || mix.Columns.Any(isNew) || ShouldCreateMissingIndex(mix, tab, replacements) ? sqlBuilder.CreateIndexBasic(mix, forHistoryTable: true) : null,
                        removeOld: null,
                        mergeBoth: (i, mix, dix) => !dix.IndexEquals(dif, mix) ? sqlBuilder.CreateIndexBasic(mix, forHistoryTable: true) :
                            mix.GetIndexName(tab.SystemVersioned!.TableName) != dix.IndexName ? sqlBuilder.RenameIndex(tab.SystemVersioned!.TableName, dix.IndexName, mix.GetIndexName(tab.SystemVersioned!.TableName)) : null);

                    return SqlPreCommand.Combine(Spacing.Simple, controlledIndexes);
                });



            SqlPreCommand? dropSchemas = Synchronizer.SynchronizeScriptReplacing(replacements, "Schemas", Spacing.Double,
                modelSchemas.ToDictionary(a => a.ToString()),
                databaseSchemas.ToDictionary(a => a.ToString()),
                createNew: null,
                removeOld: (_, oldSN) => DropSchema(oldSN) ? sqlBuilder.DropSchema(oldSN) : null,
                mergeBoth: (_, newSN, oldSN) => newSN.Equals(oldSN) ? null : sqlBuilder.DropSchema(oldSN)
             );

            SqlPreCommand? dropFullTextCatallogs = Synchronizer.SynchronizeScript(Spacing.Double,
                modelFullTextCatallogs.ToDictionary(a => a),
                databaseFullTextCatallogs.ToDictionary(a => a),
                createNew: null,
                removeOld: (_, newSN) => sqlBuilder.DropFullTextCatallog(newSN),
                mergeBoth: null
                );

            return SqlPreCommand.Combine(Spacing.Triple,
                preRenameColumns,
                createFullTextCatallogs,
                createSchemas,
                dropStatistics,

                dropIndices, dropIndicesHistory,
                dropForeignKeys,

                tables, historyTables,
                versioningTriggers,
                delayedUpdates.Combine(Spacing.Double), delayedDrops.Combine(Spacing.Double), delayedAddSystemVersioning.Combine(Spacing.Double),
                syncEnums,
                addForeingKeys,
                addIndices, addIndicesHistory,

                dropSchemas,
                dropFullTextCatallogs
            );
        }
    }

    public static Func<TableIndex, ITable, Replacements, bool> ShouldCreateMissingIndex = (mix, tab, replacements) => true;
        //return (replacements.Interactive ? SafeConsole.Ask("Create missing non-unique index {0} in {1}?".FormatWith(mix.IndexName, tab.Name)) : true)

    private static SqlPreCommand ForHistoryTable(SqlPreCommand sqlCommand, ITable tab)
    {
        return sqlCommand.Replace(new Regex(@$"\b{Regex.Escape(tab.Name.Name)}\b"), m => tab.SystemVersioned!.TableName.Name);
    }

    private static SqlPreCommand? UpdateForeignKeyTypeChanged(SqlBuilder sqlBuilder, ITable tab, DiffTable dif, IColumn tabCol, DiffColumn difCol, Func<ObjectName, ObjectName> changeName, Dictionary<ObjectName, Dictionary<string, string>> preRenameColumnsList)
    {
        if (difCol.ForeignKey != null && tabCol.ReferenceTable != null)
        {
            if (changeName(difCol.ForeignKey.TargetTable).Equals(tabCol.ReferenceTable.Name))
            {
                AliasGenerator ag = new AliasGenerator();
                var tabAlias = ag.NextTableAlias(tab.Name.Name);
                var fkAlias = ag.NextTableAlias(tabCol.ReferenceTable.Name.Name);

                var oldId = difCol.ForeignKey.Columns.Only()?.Referenced;

                if (oldId == null)
                    return null;

                oldId = preRenameColumnsList.TryGetC(difCol.ForeignKey.TargetTable)?.TryGetC(oldId) ?? oldId;

                return new SqlPreCommandSimple(
@$"UPDATE {tabAlias} 
SET {tabCol.Name} = {fkAlias}.{tabCol.ReferenceTable.PrimaryKey.Name.SqlEscape(sqlBuilder.IsPostgres)}
FROM {tab.Name} {tabAlias}
JOIN {tabCol.ReferenceTable.Name} {fkAlias} ON {tabAlias}.{difCol.Name} = {fkAlias}.{oldId}
                ");
            }
        }

        return null;
    }

    private static SqlPreCommand UpdateCustom(ITable tab, IColumn tabCol, DiffColumn difCol)
    {
        return new SqlPreCommandSimple($"UPDATE {tab.Name} SET {tabCol.Name} = YourCode({difCol.Name})");
    }

    private static string GetZero(IColumn column)
    {
        return (column.DbType.IsNumber() ? "0" :
            column.DbType.IsString() ? "''" :
            column.DbType.IsDate() ? "GetDate()" :
            column.DbType.IsGuid() ? Guid.Empty.ToString() :
            "?");
    }

    private static bool StrongColumnChanges(ITable tab, DiffTable dif)
    {
        return tab.Columns.JoinDictionary(dif.Columns, (cn, tabCol, difCol) => (tabCol, difCol))
            .Select(kvp => kvp.Value)
            .Any(t => (!t.tabCol.Nullable.ToBool() && t.difCol.Nullable) || !t.difCol.CompatibleTypes(t.tabCol));
    }

    private static SqlPreCommand UpdateCompatible(SqlBuilder sqlBuilder, Replacements replacements, ITable tab, DiffTable dif, IColumn tabCol, DiffColumn difCol)
    {
        if (!(difCol.Nullable && !tabCol.Nullable.ToBool()))
            return sqlBuilder.AlterTableAlterColumn(tab, tabCol, difCol);

        var defaultValue = GetDefaultValue(tab, tabCol, replacements, forNewColumn: false);

        if (defaultValue == "force")
            return sqlBuilder.AlterTableAlterColumn(tab, tabCol, difCol);

        bool goBefore = difCol.Name != tabCol.Name;

        return SqlPreCommand.Combine(Spacing.Simple,
            NotNullUpdate(tab.Name, tabCol, defaultValue, goBefore),
            sqlBuilder.AlterTableAlterColumn(tab, tabCol, difCol)
        )!;
    }

    private static SqlPreCommandSimple NotNullUpdate(ObjectName tab, IColumn tabCol, string defaultValue, bool goBefore)
    {
        return new SqlPreCommandSimple($"UPDATE {tab} SET {tabCol.Name} = {defaultValue} WHERE {tabCol.Name} IS NULL") { GoBefore = goBefore };
    }

    private static bool DifferentDatabase(ObjectName name, ObjectName name2)
    {
        return !object.Equals(name.Schema.Database, name2.Schema.Database);
    }

    public static Func<SchemaName, bool> IgnoreSchema = s => s.Name.Contains("\\");

    private static SqlPreCommand AlterTableAddColumnDefault(SqlBuilder sqlBuilder, ITable table, IColumn column, Replacements rep, string? forceDefaultValue, bool avoidDefault, HashSet<FieldEmbedded.EmbeddedHasValueColumn> hasValueFalse)
    {
        if (table.Name.Name == "EmailTemplate" && column.Name == "From_AddressSourceID") // Delete this if after 1.4.2024
        {
            return  SqlPreCommand.Combine(Spacing.Simple,
                sqlBuilder.AlterTableAddColumn(table, column).Do(a => a.GoAfter = true),
                new SqlPreCommandSimple($@"UPDATE {table.Name} SET
    {column.Name} = CASE WHEN From_Token_HasValue = 1 THEN 0 ELSE 1 END
WHERE From_HasValue = 1"))!;
        }

        if (table.Name.Name == "EmailTemplateRecipients" && column.Name == "AddressSourceID") // Delete this if after 1.4.2024
        {
            var tempDefault = new SqlBuilder.DefaultConstraint(
                columnName: column.Name,
                name: "DF_TEMP_" + column.Name,
                quotedDefinition: sqlBuilder.Quote(column.DbType, "0"));

            return SqlPreCommand.Combine(Spacing.Simple,
                sqlBuilder.AlterTableAddColumn(table, column, tempDefault),
                sqlBuilder.AlterTableDropConstraint(table.Name, tempDefault.Name).Do(a => a.GoAfter = true),
                new SqlPreCommandSimple($@"UPDATE {table.Name} SET
    {column.Name} = CASE WHEN Token_HasValue = 1 THEN 0 ELSE 1 END")

                )!;
        }


        if (column.Nullable == IsNullable.Yes || column.Identity || column.Default != null || column is ImplementationColumn || avoidDefault)
            return sqlBuilder.AlterTableAddColumn(table, column);

        if (column.Nullable == IsNullable.Forced)
        {
            var hasValueColumn = table.GetHasValueColumn(column);

            if (hasValueColumn != null && hasValueFalse.Contains(hasValueColumn))
                return sqlBuilder.AlterTableAddColumn(table, column);

            var defaultValue = GetDefaultValue(table, column, rep, forNewColumn: true, forceDefaultValue: forceDefaultValue);
            if (defaultValue == "force")
                return sqlBuilder.AlterTableAddColumn(table, column);

            var where = hasValueColumn != null ? $"{hasValueColumn.Name} = 1" : "??";

            return SqlPreCommand.Combine(Spacing.Simple,
                sqlBuilder.AlterTableAddColumn(table, column).Do(a => a.GoAfter = true),
                new SqlPreCommandSimple($@"UPDATE {table.Name} SET
    {column.Name} = {sqlBuilder.Quote(column.DbType, defaultValue)}
WHERE {where}"))!;
        }
        else
        {
            var defaultValue = GetDefaultValue(table, column, rep, forNewColumn: true, forceDefaultValue: forceDefaultValue);
            if (defaultValue == "force")
                return sqlBuilder.AlterTableAddColumn(table, column);

            if (column is FieldEmbedded.EmbeddedHasValueColumn hv && defaultValue == "0")
                hasValueFalse.Add(hv);

            var tempDefault = new SqlBuilder.DefaultConstraint(
                columnName: column.Name,
                name: "DF_TEMP_" + column.Name,
                quotedDefinition: sqlBuilder.Quote(column.DbType, defaultValue)
            );

            return SqlPreCommand.Combine(Spacing.Simple,
                sqlBuilder.AlterTableAddColumn(table, column, tempDefault),
                sqlBuilder.IsPostgres ?
                sqlBuilder.AlterTableAlterColumnDropDefault(table.Name, column.Name) :
                sqlBuilder.AlterTableDropConstraint(table.Name, tempDefault.Name))!;
        }
    }

    private static SqlPreCommand AlterTableAddColumnDefaultZero(SqlBuilder sqlBuilder, ITable table, IColumn column)
    {
        if (column.Nullable == IsNullable.Yes || column.Identity || column.Default != null || column is ImplementationColumn)
            return sqlBuilder.AlterTableAddColumn(table, column);

        var defaultValue =
            column.DbType.IsNumber() ? "0" :
            column.DbType.IsString() ? "''" :
            column.DbType.IsDate() ? "GetDate()" :
            column.DbType.IsGuid() ? "'00000000-0000-0000-0000-000000000000'" :
            "?";

        var tempDefault = new SqlBuilder.DefaultConstraint(
            columnName: column.Name,
            name: "DF_TEMP_COPY_" + column.Name,
            quotedDefinition: sqlBuilder.Quote(column.DbType, defaultValue)
        );

        return SqlPreCommand.Combine(Spacing.Simple,
            sqlBuilder.AlterTableAddColumn(table, column, tempDefault),
            sqlBuilder.AlterTableDropConstraint(table.Name, tempDefault.Name))!;
    }

    public static string GetDefaultValue(ITable table, IColumn column, Replacements rep, bool forNewColumn, string? forceDefaultValue = null)
    {
        if (column is SystemVersionedInfo.SqlServerPeriodColumn svc)
        {
            var date = svc.SystemVersionColumnType == SystemVersionedInfo.SystemVersionColumnType.Start ? DateTime.MinValue : DateTime.MaxValue;

            return $"CONVERT(datetime2, '{date:yyyy-MM-dd HH:mm:ss.fffffff}')";
        }

        string typeDefault = forceDefaultValue ??
            (
            column.DbType.IsBoolean() ? (Schema.Current.Settings.IsPostgres ? "false" : "0") :
            column.DbType.IsNumber() ? "0" :
            column.DbType.IsString() ? "''" :
            column.DbType.IsDate() ? "GetDate()" :
            column.DbType.IsGuid() ? "NEWID()" :
        column.DbType.IsTime() ? "'00:00'" :
        column.DbType.IsPostgres && column.DbType.PostgreSql == NpgsqlDbType.TimestampTzRange ? "tstzrange(now(), 'infinity', '[)')" :
            "?");

        string defaultValue = rep.Interactive ? SafeConsole.AskString($"Default value for '{table.Name.Name}.{column.Name}'? ([Enter] for {typeDefault} or 'force' if there are no {(forNewColumn ? "rows" : "nulls")}) ", stringValidator: str => null) : "";
        if (defaultValue == "force")
            return defaultValue;

        if (defaultValue.HasText())
        {
            if (column.DbType.IsString() && !defaultValue.Contains("'"))
                defaultValue = "'" + defaultValue + "'";

            if (column.DbType.IsGuid() && !defaultValue.Contains("'"))
                defaultValue = "'" + defaultValue + "'";

            if ((column.DbType.IsDate() || column.DbType.IsTime()) && !defaultValue.Contains("'") && defaultValue != typeDefault)
                defaultValue = "'" + defaultValue + "'";

            if (column.DbType.IsBoolean() && defaultValue != typeDefault)
            {
                defaultValue = Schema.Current.Settings.IsPostgres ?
                     (defaultValue == "0" ? "false" : defaultValue == "1" ? "true" : defaultValue) :
                     (defaultValue.ToLower() == "false" ? "0" : defaultValue.ToLower() == "true" ? "1" : defaultValue);
            }
        }

        if (string.IsNullOrEmpty(defaultValue))
            return typeDefault;

        return defaultValue;
    }


    private static Dictionary<string, DiffIndex> ApplyIndexAutoReplacements(DiffTable diff, ITable tab, Dictionary<string, Maps.TableIndex> dictionary)
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
                if (oldIx.IsPrimary && newIx is PrimaryKeyIndex)
                    return true;

                if (oldIx.IsPrimary || newIx is PrimaryKeyIndex)
                    return false;

                if (oldIx.IsUnique != (newIx is UniqueTableIndex))
                    return false;

                if (oldIx.ViewName != null || (newIx is UniqueTableIndex) && ((UniqueTableIndex)newIx).ViewName != null)
                    return false;

                var newCols = newIx.Columns.Select(c => diff.Columns.TryGetC(c.Name)?.Name).NotNull().ToHashSet();
                var newIncCols = newIx.IncludeColumns.EmptyIfNull().Select(c => diff.Columns.TryGetC(c.Name)?.Name).NotNull().ToHashSet();

                var oldCols = oldIx.Columns.Where(a => a.IsIncluded == false).Select(a => a.ColumnName);
                var oldIncCols = oldIx.Columns.Where(a => a.IsIncluded == true).Select(a => a.ColumnName);

                if (!newCols.SetEquals(oldCols))
                    return false;

                if (!newIncCols.SetEquals(oldIncCols))
                    return false;

                var oldWhere = oldIx.IndexName.TryAfter("__");
                var newWhere = newIx.WhereSignature()?.TryAfter("__");

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

    private static SqlPreCommandSimple? UpdateByFkChange(string tn, DiffColumn difCol, IColumn tabCol, Func<ObjectName, ObjectName> changeName)
    {
        if (difCol.ForeignKey == null || tabCol.ReferenceTable == null || tabCol.AvoidForeignKey)
            return null;

        ObjectName oldFk = changeName(difCol.ForeignKey.TargetTable);

        if (oldFk.Equals(tabCol.ReferenceTable.Name))
            return null;

        AliasGenerator ag = new AliasGenerator();

        var newFk = tabCol.ReferenceTable.Name;
        var tnAlias = ag.NextTableAlias(tn);
        var oldFkAlias = ag.NextTableAlias(oldFk.Name);

        return new SqlPreCommandSimple(
@$"-- Column {tn}.{tabCol.Name} was referencing {oldFk} but not references {newFk}. An update is needed?
UPDATE {tnAlias}
SET {tabCol.Name} =  -- get {newFk} id from {oldFkAlias}.Id
FROM {tn} {tnAlias}
JOIN {oldFk} {oldFkAlias} ON {tnAlias}.{tabCol.Name} = {oldFkAlias}.Id");
    }

    public static Func<DiffTable, bool>? IgnoreTable = null;



    static SqlPreCommand? SynchronizeEnumsScript(Replacements replacements)
    {
        try
        {
            Schema schema = Schema.Current;

            List<SqlPreCommand> commands = new List<SqlPreCommand>();

            foreach (var table in schema.Tables.Values)
            {
                Type? enumType = EnumEntity.Extract(table.Type);
                if (enumType != null)
                {
                    Console.Write(".");

                    IEnumerable<Entity> should = EnumEntity.GetEntities(enumType);
                    Dictionary<string, Entity> shouldByName = should.ToDictionary(a => a.ToString());

                    List<Entity> current = Administrator.TryRetrieveAll(table.Type, replacements);
                    int nullVal = 0;
                    Dictionary<string, Entity> currentByName = current.ToDictionaryEx(a => a.toStr ?? $"NULL{nullVal++}", table.Name.Name);

                    string key = Replacements.KeyEnumsForTable(table.Name.Name);

                    replacements.AskForReplacements(currentByName.Keys.ToHashSet(), shouldByName.Keys.ToHashSet(), key);

                    currentByName = replacements.ApplyReplacementsToOld(currentByName, key);

                    var mix = shouldByName.JoinDictionary(currentByName, (n, s, c) => (s, c)).Where(a => a.Value.s.id != a.Value.c.id).ToDictionary();

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
        catch (Exception e)
        {
            return new SqlPreCommandSimple("-- Exception synchronizing enums\r\n{0}".FormatWith(e.Message.Indent(2, '-')));
        }
    }

    private static SqlPreCommand? SyncEnums(Schema schema, Table table, Dictionary<string, Entity> current, Dictionary<string, Entity> should)
    {
        var isPostgres = schema.Settings.IsPostgres;

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
                                   select new SqlPreCommandSimple("UPDATE {0} SET {1} = {2} WHERE {1} = {3}; -- {4} re-indexed"
                                       .FormatWith(t.Name, col.Name.SqlEscape(isPostgres), s.Id, c.Id, c.toStr)))
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
        var instance = (Entity)Activator.CreateInstance(current.GetType())!;
        instance.toStr = current.toStr;
        instance.id = (int)current.id!.Value + 1000000;
        return instance;
    }

    public static SqlPreCommand? SnapshotIsolation(Replacements replacements)
    {
        if (replacements.SchemaOnly || Schema.Current.Settings.IsPostgres)
            return null;

        var list = Schema.Current.DatabaseNames().Select(a => a?.ToString()).ToList();
        var sqlBuilder = Connector.Current.SqlBuilder;

        if (list.Contains(null))
        {
            list.Remove(null);
            list.Add(Connector.Current.DatabaseName());
        }

        var results = Database.View<SysDatabases>()
            .Where(d => list.Contains(d.name))
            .Select(d => new
            {
                name = new DatabaseName(null, d.name, Connector.Current.Schema.Settings.IsPostgres),
                d.snapshot_isolation_state,
                d.is_read_committed_snapshot_on
            }).ToList();

        var cmd = replacements.WithReplacedDatabaseName().Using(_ => results.Select((a, i) =>
            SqlPreCommand.Combine(Spacing.Simple,
            !a.snapshot_isolation_state || !a.is_read_committed_snapshot_on ? DisconnectUsers(a.name, "SPID" + i) : null,
            !a.snapshot_isolation_state ? sqlBuilder.SetSnapshotIsolation(a.name, true) : null,
            !a.is_read_committed_snapshot_on ? sqlBuilder.MakeSnapshotIsolationDefault(a.name, true) : null)).Combine(Spacing.Double));

        if (cmd == null)
            return null;

        return SqlPreCommand.Combine(Spacing.Double,
            new SqlPreCommandSimple("use master -- Start Snapshot"),
            cmd,
            new SqlPreCommandSimple("use {0} -- Stop Snapshot".FormatWith(Connector.Current.DatabaseName())));
    }

    public static SqlPreCommandSimple DisconnectUsers(DatabaseName databaseName, string variableName)
    {
        return new SqlPreCommandSimple(@"DECLARE @{1} VARCHAR(7000)
SELECT @{1} = COALESCE(@{1},'')+'KILL '+CAST(SPID AS VARCHAR)+'; 'FROM master..SysProcesses WHERE DB_NAME(DBId) = '{0}'
EXEC(@{1})".FormatWith(databaseName.Name, variableName));
    }
}

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
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

public class DiffPostgresVersioningTrigger
{
    public int? tgrelid;
    public string tgname;
    public string proname;
    public byte[] tgargs;
    public int tgfoid;
}

public class DiffTable
{
    public ObjectName Name;

    public ObjectName? PrimaryKeyName;

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

    public DiffIndex? FullTextIndex
    {
        get { return Indices.Values.SingleOrDefault(a => a.FullTextIndex != null); }
        set
        {
            if (value != null)
                Indices[FullTextTableIndex.FULL_TEXT] = value;
            else
                Indices.Remove(FullTextTableIndex.FULL_TEXT);
        }
    }

    public DiffPostgresVersioningTrigger? VersionningTrigger { get; internal set; }

    public SysTableTemporalType TemporalType;
    public ObjectName? TemporalTableName;
    public DiffPeriod? Period;

    public Dictionary<string, DiffIndex> Indices = new Dictionary<string, DiffIndex>();

    public List<DiffStats> Stats = new List<DiffStats>();

    public List<DiffForeignKey> MultiForeignKeys = new List<DiffForeignKey>();
    public List<DiffCheckConstraint> CheckConstraints = new List<DiffCheckConstraint>();

    public void ForeignKeysToColumns()
    {
        foreach (var fk in MultiForeignKeys.Where(a => a.Columns.Count == 1).ToList())
        {
            this.Columns[fk.Columns.SingleEx().Parent].ForeignKey = fk;
            MultiForeignKeys.Remove(fk);
        }

        foreach (var cc in CheckConstraints.Where(a => a.ColumnName != null).ToList())
        {
            this.Columns[cc.ColumnName!].CheckConstraint = cc;
            CheckConstraints.Remove(cc);
        }
    }

    public DiffColumn AddColumnBefore(string columnName, DiffColumn newColumn)
    {
        var cols = Columns.Values.ToList();

        cols.Insert(cols.FindIndex(c => c.Name == columnName), newColumn);

        Columns = cols.ToDictionary(a => a.Name);

        return newColumn;
    }

    public override string ToString()
    {
        return Name.ToString();
    }

    internal void FixSqlColumnLengthSqlServer()
    {
        foreach (var c in Columns.Values.Where(c => c.Length != -1))
        {
            var sqlDbType = c.DbType.SqlServer;
            if (sqlDbType == SqlDbType.NChar || sqlDbType == SqlDbType.NText || sqlDbType == SqlDbType.NVarChar)
                c.Length /= 2;
        }
    }
}

public record class FullTextCatallogName(string Name, DatabaseName? Database);



public enum SysTableTemporalType
{
    None = 0,
    HistoryTable = 1,
    SystemVersionTemporalTable = 2
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
    public FullTextIndex? FullTextIndex; 
    public string IndexName;
    public string? ViewName;
    public string? FilterDefinition;
    public DiffIndexType? Type;

    public List<DiffIndexColumn> Columns;

    public override string ToString()
    {
        return "{0} ({1})".FormatWith(IndexName, Columns.ToString(", "));
    }

    internal bool IndexEquals(DiffTable dif, Maps.TableIndex mix)
    {
        if (this.ViewName != (mix as UniqueTableIndex)?.ViewName)
            return false;

        if (this.ColumnsChanged(dif, mix))
            return false;

        if (this.IsPrimary != mix is PrimaryKeyIndex)
            return false;

        if (this.Type != GetIndexType(mix))
            return false;

        return true;
    }

    private static DiffIndexType? GetIndexType(TableIndex mix)
    {
        if (mix is UniqueTableIndex && ((UniqueTableIndex)mix).ViewName != null)
            return null;

        if (mix is FullTextTableIndex)
            return DiffIndexType.FullTextIndex;

        if (mix is PrimaryKeyIndex)
            return Schema.Current.Settings.IsPostgres ? DiffIndexType.NonClustered : DiffIndexType.Clustered;

        return DiffIndexType.NonClustered;
    }

    bool ColumnsChanged(DiffTable dif, TableIndex mix)
    {
        bool sameCols = IdenticalColumns(dif, mix.Columns, this.Columns.Where(a => !a.IsIncluded).ToList());
        bool sameIncCols = IdenticalColumns(dif, mix.IncludeColumns, this.Columns.Where(a => a.IsIncluded).ToList());

        if (sameCols && sameIncCols)
            return false;

        return true;
    }

    private static bool IdenticalColumns(DiffTable dif, IColumn[]? modColumns, List<DiffIndexColumn> diffColumns)
    {
        if ((modColumns?.Length ?? 0) != diffColumns.Count)
            return false;

        if (diffColumns.Count == 0)
            return true;

        var difColumns = diffColumns.Select(cn => dif.Columns.Values.SingleOrDefault(dc => dc.Name == cn.ColumnName)).ToList(); //Ny old name

        var perfect = difColumns.ZipOrDefault(modColumns!, (dc, mc) => dc != null && mc != null && dc.ColumnEquals(mc, ignorePrimaryKey: true, ignoreIdentity: true, ignoreGenerateAlways: true)).All(a => a);
        return perfect;
    }

    public bool IsControlledIndex
    {
        get { return IndexName.StartsWith("IX_") || IndexName.StartsWith("UIX_"); }
    }
}

public class FullTextIndex
{
    public string CatallogName;
    public string StopList;
    public string PropertiesList;
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


    FullTextIndex = 100,
}

public enum GeneratedAlwaysType
{
    None = 0,
    AsRowStart = 1,
    AsRowEnd = 2
}

public class DiffDefaultConstraint
{
    public string? Name;
    public string Definition;
}

public class DiffColumn
{
    public string Name;
    public AbstractDbType DbType;
    public string? UserTypeName;
    public bool Nullable;
    public string? Collation;
    public int Length;
    public int Precision;
    public int Scale;
    public bool Identity;
    public bool PrimaryKey;

    public DiffForeignKey? ForeignKey;

    public DiffDefaultConstraint? DefaultConstraint;

    public DiffCheckConstraint? CheckConstraint;

    public GeneratedAlwaysType GeneratedAlwaysType;

    public bool ColumnEquals(IColumn other, bool ignorePrimaryKey, bool ignoreIdentity, bool ignoreGenerateAlways)
    {
        var result = DbType.Equals(other.DbType)
            && Collation == other.Collation
            && StringComparer.InvariantCultureIgnoreCase.Equals(UserTypeName, other.UserDefinedTypeName)
            && Nullable == (other.Nullable.ToBool())
            && SizeEquals(other)
            && PrecisionEquals(other)
            && ScaleEquals(other)
            && (ignoreIdentity || Identity == other.Identity)
            && (ignorePrimaryKey || PrimaryKey == other.PrimaryKey)
            && (ignoreGenerateAlways || GeneratedAlwaysType == other.GetGeneratedAlwaysType());

        if (!result)
            return false;

        return result;
    }

    public bool ScaleEquals(IColumn other)
    {
        return (other.Scale == null || other.Scale.Value == Scale);
    }

    public bool SizeEquals(IColumn other)
    {
        return (other.DbType.IsDecimal() || other.Size == null || other.Size.Value == Precision || other.Size.Value == Length || other.Size.Value == int.MaxValue && Length == -1);
    }

    public bool PrecisionEquals(IColumn other)
    {
        return (!other.DbType.IsDecimal() || other.Precision == null || other.Precision == 0 || other.Precision.Value == Precision);
    }

    public bool DefaultEquals(IColumn other)
    {
        if (other.Default == null && this.DefaultConstraint == null)
            return true;

        var result = CleanParenthesis(this.DefaultConstraint?.Definition) == CleanParenthesis(other.Default);

        return result;
    }

    public bool CheckEquals(IColumn other)
    {
        if (other.Check == null && this.CheckConstraint == null)
            return true;

        var result = this.CheckConstraint?.Definition == other.Check;

        return result;
    }

    private string? CleanParenthesis(string? p)
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
            Precision = Precision,
            Scale = Scale,
            DbType = DbType,
            UserTypeName = UserTypeName,
        };
    }

    public override string ToString()
    {
        return this.Name;
    }

    internal bool CompatibleTypes(IColumn tabCol)
    {
        if (Schema.Current.Settings.IsPostgres)
            return CompatibleTypes_Postgres(this.DbType.PostgreSql, tabCol.DbType.PostgreSql);
        else
            return CompatibleTypes_SqlServer(this.DbType.SqlServer, tabCol.DbType.SqlServer);
    }

    private bool CompatibleTypes_Postgres(NpgsqlDbType fromType, NpgsqlDbType toType)
    {
        return true;
    }

    private bool CompatibleTypes_SqlServer(SqlDbType fromType, SqlDbType toType)
    {
        //https://docs.microsoft.com/en-us/sql/t-sql/functions/cast-and-convert-transact-sql
        switch (fromType)
        {
            //BLACKLIST!!
            case SqlDbType.Binary:
            case SqlDbType.VarBinary:
                switch (toType)
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
                return toType != SqlDbType.Image;

            case SqlDbType.DateTime:
            case SqlDbType.SmallDateTime:
                switch (toType)
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
                if (toType == SqlDbType.Time)
                    return false;
                goto case SqlDbType.DateTime2;

            case SqlDbType.Time:
                if (toType == SqlDbType.Date)
                    return false;
                goto case SqlDbType.DateTime2;

            case SqlDbType.DateTimeOffset:
            case SqlDbType.DateTime2:
                switch (toType)
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
                switch (toType)
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
                switch (toType)
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
                switch (toType)
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
                switch (toType)
                {
                    case SqlDbType.Binary:
                    case SqlDbType.VarBinary:
                    case SqlDbType.Char:
                    case SqlDbType.VarChar:
                    case SqlDbType.NChar:
                    case SqlDbType.NVarChar:
                    case SqlDbType.UniqueIdentifier:
                    case SqlDbType.Variant:
                        return true;
                    default:
                        return false;
                }
            case SqlDbType.Image:
                switch (toType)
                {
                    case SqlDbType.Binary:
                    case SqlDbType.Image:
                    case SqlDbType.VarBinary:
                    case SqlDbType.Timestamp:
                        return true;
                    default:
                        return false;
                }
            case SqlDbType.NText:
            case SqlDbType.Text:
                switch (toType)
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
                        return false;
                }
            case SqlDbType.Xml:
            case SqlDbType.Udt:
                switch (toType)
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
                        return false;
                }
            default:
                throw new NotImplementedException("Unexpected SqlDbType");
        }
    }
}

public class DiffCheckConstraint
{
    public string Name;
    public string Definition; 
    public string? ColumnName;

    public override string ToString() => Name;
}

public class DiffForeignKey
{
    public ObjectName Name;
    public ObjectName TargetTable;
    public bool IsDisabled;
    public bool IsNotTrusted;
    public List<DiffForeignKeyColumn> Columns;

    public override string ToString() => Name.ToString();
}

public class DiffForeignKeyColumn
{
    public string Parent;
    public string Referenced;

    public override string ToString() => $"{Parent} -> {Referenced}";
}
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
