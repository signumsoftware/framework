using Signum.Engine.Linq;
using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using NpgsqlTypes;
using System.Data;

namespace Signum.Engine.Sync;
internal class PrimaryKeyUpdater
{


    private bool isPostgres;
    private Dictionary<ITable, Dictionary<string, List<IColumn>>> ibas;

    private Table type_Table;
    private IColumn type_Id;
    private IColumn type_TableName;

    public PrimaryKeyUpdater(bool isPostgres, Dictionary<string, ITable> modelTables)
    {
        this.isPostgres = isPostgres;
        this.ibas = (from t in modelTables.Values
                     select new
                     {
                         Table = t,
                         IBAs = (from c in t.Columns.Values
                                 let preName = c is ImplementedByAllIdColumn id ? id.PreName :
                                            c is ImplementedByAllTypeColumn type ? type.PreName :
                                            null
                                 where preName != null
                                 group c by preName into g

                                 select KeyValuePair.Create(g.Key, g.ToList()))
                                 .ToDictionaryEx()

                     })
                    .Where(a => a.IBAs.Any())
                    .ToDictionaryEx(a => a.Table, a => a.IBAs);


        this.type_Table = Schema.Current.Table(typeof(TypeEntity));
        this.type_Id = (IColumn)((IFieldFinder)type_Table).GetField(ReflectionTools.GetPropertyInfo((TypeEntity e) => e.Id));
        this.type_TableName = (IColumn)((IFieldFinder)type_Table).GetField(ReflectionTools.GetPropertyInfo((TypeEntity e) => e.TableName));
    }

    SqlPreCommandSimple UpdateJoin(
        ObjectName targetTable,
        Alias targetAlias,
        string setClause,
        ObjectName sourceTable,
        Alias sourceAlias,
        string joinCondition,
        (string fromEntry, string condition)? extraTable = null)
    {
        if (isPostgres)
        {
            // PostgreSQL syntax: UPDATE target_alias SET ... FROM source_table source_alias WHERE ...
            // The target table is NOT part of the FROM clause, so an extra table has to be another FROM entry and its
            // condition has to go in the WHERE: a JOIN ... ON in the FROM clause can not reference the target alias.
            var sql = $"""
                    UPDATE {targetTable} {targetAlias} SET
                    {setClause.Indent(4)}
                    FROM {sourceTable} {sourceAlias}{(extraTable != null ? ", " + extraTable.Value.fromEntry : "")}
                    WHERE {joinCondition}{(extraTable != null ? "\n    AND " + extraTable.Value.condition : "")}
                    """;

            return new SqlPreCommandSimple(sql).Do(a => a.GoAfter = true);
        }
        else
        {
            // SQL Server syntax: UPDATE t SET ... FROM target_table t JOIN source_table s ON ...
            // Here the target table IS in the FROM clause, so an extra JOIN can reference the target alias.
            var sql = $"""
                    UPDATE {targetAlias} SET
                    {setClause.Indent(4)}
                    FROM {targetTable} {targetAlias}
                    JOIN {sourceTable} {sourceAlias} ON {joinCondition}{(extraTable != null ? $"\nJOIN {extraTable.Value.fromEntry} ON {extraTable.Value.condition}" : "")}
                    """;

            return new SqlPreCommandSimple(sql).Do(a => a.GoAfter = true);
        }
    }


    public SqlPreCommand? UpdateFKToAnotherTable(ObjectName tn, DiffColumn difCol, IColumn tabCol, Func<ObjectName, ObjectName> changeName, bool withHistory)
    {
        if (difCol.ForeignKey == null || tabCol.ReferenceTable == null || tabCol.AvoidForeignKey)
            return null;

        ObjectName oldFk = changeName(difCol.ForeignKey.TargetTable);

        if (oldFk.Equals(tabCol.ReferenceTable.Name))
            return null;

        AliasGenerator ag = new AliasGenerator();

        var newFk = tabCol.ReferenceTable.Name;
        var id = tabCol.ReferenceTable.PrimaryKey;
        var tnAlias = ag.NextTableAlias(tn.Name);
        var oldFkAlias = ag.NextTableAlias(oldFk.Name);

        var result = UpdateJoin(
            targetTable: tn, targetAlias: tnAlias,
            setClause: $"{Esc(tabCol)} =  -- get {newFk} id from {oldFkAlias}.{Esc(id)}",
            sourceTable: oldFk, sourceAlias: oldFkAlias,
            joinCondition: $"{tnAlias}.{Esc(tabCol)} = {oldFkAlias}.{Esc(id)}");

        var message = @$"-- Column {tn}.{tabCol.Name} was referencing {oldFk} but now references {newFk}. An update is needed?";
        result.AlterSql(message + "\n" + result.Sql);
        if (withHistory)
            return new SqlPreCommand_WithHistory(normal: result, history: null);

        return result;
    }

    public SqlPreCommand? UpdateForeignKeyTypeChanged(ITable tab, DiffTable dif, IColumn tabCol, DiffColumn difCol, Func<ObjectName, ObjectName> changeName, Dictionary<ObjectName, Dictionary<string, string>> preRenameColumnsList)
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

                var sourceTable = tabCol.ReferenceTable;
                var newId = tabCol.ReferenceTable.PrimaryKey;

                var result = UpdateJoin(
                    targetTable: tab.Name, targetAlias: tabAlias,
                    setClause: $"{Esc(tabCol)} = {fkAlias}.{Esc(newId)}",
                    sourceTable: tabCol.ReferenceTable.Name, sourceAlias: fkAlias,
                    joinCondition: $"{tabAlias}.{Esc(difCol)} = {fkAlias}.{oldId.SqlEscape(isPostgres)}"
                );

                if (tab.SystemVersioned == null)
                    return result;

                if (sourceTable.SystemVersioned == null)
                {
                    var history = UpdateJoin(
                        targetTable: tab.SystemVersioned.TableName, targetAlias: tabAlias,
                        setClause: $"{Esc(tabCol)} = {fkAlias}.{Esc(newId)}",
                        sourceTable: sourceTable.Name, sourceAlias: fkAlias,
                        joinCondition: $"{tabAlias}.{Esc(difCol)} = {fkAlias}.{oldId.SqlEscape(isPostgres)}"
                    );

                    return SqlPreCommand.Combine(Spacing.Double,
                        result,
                        history);
                }
                else
                {
                    var cte = $"""
                    WITH pairs AS (
                        SELECT {oldId.SqlEscape(isPostgres)} as old_id, {Esc(newId)} as new_id FROM {sourceTable.Name}
                        UNION
                        SELECT {oldId.SqlEscape(isPostgres)} as old_id, {Esc(newId)} as new_id FROM {sourceTable.SystemVersioned.TableName}
                    )
                    """;

                    var pairsAlias = new Alias("p", isPostgres);

                    var history = UpdateJoin(
                        targetTable: tab.SystemVersioned.TableName, targetAlias: tabAlias,
                        setClause: $"{Esc(tabCol)} = {pairsAlias}.new_id",
                        sourceTable: ObjectName.Raw("pairs", isPostgres), sourceAlias: pairsAlias,
                        joinCondition: $"{tabAlias}.{Esc(difCol)} = {pairsAlias}.old_id"
                    );

                    history.AlterSql(cte + "\n" + history.Sql);

                    return SqlPreCommand.Combine(Spacing.Double,
                        result,
                        history);

                }
            }
        }

        return null;
    }




    public SqlPreCommand UpdateHistoryTable(ITable table, ObjectName oldTableName, IColumn newId, DiffColumn oldId)
    {
        var history = table.SystemVersioned!.TableName;
        var historyAlias = new Alias("his", isPostgres);
        var mainTable = table.Name;
        var mainAlias = new Alias("m", isPostgres);

        var newIdGenerator = newId.Default ?? 
            (newId.Identity ? $"-DENSE_RANK() OVER (ORDER BY {historyAlias}.{oldId})" : throw new NotImplementedException());

        var cte = $"""
            WITH cte AS (
                SELECT 
                    {historyAlias}.{Esc(oldId)},
                    COALESCE({mainAlias}.{Esc(newId)}, {newIdGenerator}) as new_id
                FROM {history} {historyAlias}
                LEFT JOIN {mainTable} {mainAlias}
                    ON {historyAlias}.{Esc(oldId)} = {mainAlias}.{Esc(oldId)}
                GROUP BY {historyAlias}.{Esc(oldId)}, {mainAlias}.{Esc(newId)}
            )
            """;

        var update = UpdateJoin(
            history, historyAlias,
            setClause: $"{Esc(newId)} = c.new_id",
            ObjectName.Raw("cte", isPostgres), new Alias("c", isPostgres),
            $"his.{oldId} = c.{oldId}");

        update.AlterSql(cte + "\n" + update.Sql);

        return update;
    }

    public SqlPreCommand? UpdateImplementedByAll(Table table, ObjectName oldTableName, IColumn newId, DiffColumn oldId)
    {
        List<SqlPreCommand> commands = new List<SqlPreCommand>();
        foreach (var typeKvp in ibas)
        {
            var ibaTable = typeKvp.Key;
            
            foreach (var ibKvp in typeKvp.Value)
            {
                var ibaType = ibKvp.Value.OfType<ImplementedByAllTypeColumn>().SingleEx();
                var ibaOldId = ibKvp.Value.OfType<ImplementedByAllIdColumn>().SingleEx(a => a.DbType.Equals(oldId.DbType));
                var ibaNewId = ibKvp.Value.OfType<ImplementedByAllIdColumn>().SingleEx(a => a.DbType.Equals(newId.DbType));

                var iba = UpdateIBAIfNecesary(table, oldTableName, newId, oldId, ibaTable.Name, ibaType, ibaOldId, ibaNewId);
                if (iba != null)
                    commands.Add(iba);

                if (ibaTable.SystemVersioned != null)
                {
                    var ibaH = UpdateIBAIfNecesary(table, oldTableName, newId, oldId, ibaTable.SystemVersioned.TableName, ibaType, ibaOldId, ibaNewId);
                    if (ibaH != null)
                        commands.Add(ibaH);
                }
            }
        }

        return commands.Combine(Spacing.Double);

    }

    private string Esc(IColumn col) => col.Name.SqlEscape(isPostgres);
    private string Esc(DiffColumn col) => col.Name.SqlEscape(isPostgres);


    private SqlPreCommand? UpdateIBAIfNecesary(ITable table, ObjectName oldTableName, IColumn newId, DiffColumn oldId, ObjectName ibaTable, IColumn ibaType, IColumn ibaOldId, IColumn ibaNewId)
    {
        var count = Convert.ToInt32(Executor.ExecuteScalar($"""
                    SELECT Count(*) 
                    FROM {ibaTable} iba
                    JOIN {type_Table} type 
                    ON type.{Esc(this.type_Id)} = iba.{ibaType} 
                    AND type.{Esc(this.type_TableName)} = '{oldTableName}' 
                    """
            )!);

        if (count == 0)
            return null;

        AliasGenerator ag = new AliasGenerator();
        var ibaAlias = ag.NextTableAlias(ibaTable.Name);
        var tableAlias = ag.NextTableAlias(table.Name.Name);

        if (table.SystemVersioned == null)
        {
            var simple = UpdateJoin(
                targetTable: ibaTable, targetAlias: ibaAlias,
                setClause: $"""
            {Esc(ibaNewId)} = {tableAlias}.{Esc(newId)},
            {Esc(ibaOldId)} = null
            """,
                sourceTable: table.Name, sourceAlias: tableAlias,
                joinCondition: $"{tableAlias}.{Esc(oldId)} = {ibaAlias}.{Esc(ibaOldId)}",
                extraTable: ($"{type_Table.Name} type", $"type.{Esc(type_Id)} = {ibaAlias}.{Esc(ibaType)} AND type.{Esc(type_TableName)} = '{oldTableName}'"));

            return SqlPreCommand.Combine(Spacing.Double, simple, NullOutUnmatched(ag, oldTableName, ibaTable, ibaType, ibaOldId))!;
        }
        else
        {
            var cte = $"""
            WITH pairs AS (
                SELECT {Esc(oldId)} as old_id, {Esc(newId)} as new_id FROM {table.Name}
                UNION
                SELECT {Esc(oldId)} as old_id, {Esc(newId)} as new_id FROM {table.SystemVersioned.TableName}
            )
            """;

            var pairsAlias = new Alias("p", isPostgres);

            var update =  UpdateJoin(
              targetTable: ibaTable, targetAlias: ibaAlias,
              setClause: $"""
            {Esc(ibaNewId)} = {pairsAlias}.new_id,
            {Esc(ibaOldId)} = null
            """,
              sourceTable: ObjectName.Raw("pairs", isPostgres), sourceAlias: pairsAlias,
              joinCondition: $"{pairsAlias}.old_id = {ibaAlias}.{Esc(ibaOldId)}",
              extraTable: ($"{type_Table.Name} type", $"type.{Esc(type_Id)} = {ibaAlias}.{Esc(ibaType)} AND type.{Esc(type_TableName)} = '{oldTableName}'"));

            update.AlterSql(cte + "\n" + update.Sql);

            return SqlPreCommand.Combine(Spacing.Double, update, NullOutUnmatched(ag, oldTableName, ibaTable, ibaType, ibaOldId))!;
        }
    }

    /// <summary>
    /// Clears the old-typed id column of the rows the remap above could not reach: a row whose target entity had
    /// already been deleted matches no <c>_old</c> value, so it would keep an id of the previous type for a type
    /// that is now keyed differently. Reading such a row throws (e.g. "XEntity requires ids of type Guid, not int"),
    /// and because these references are usually loaded through a cached table or a GlobalLazy, one stale row can
    /// break every request rather than only the record that holds it.
    /// <para>Only the id is cleared, not the row: the reference becomes empty instead of invalid. A table that can
    /// not represent an empty reference has to clean up after itself, the way TranslatedInstanceRowIds does.</para>
    /// </summary>
    private SqlPreCommand NullOutUnmatched(AliasGenerator ag, ObjectName oldTableName, ObjectName ibaTable, IColumn ibaType, IColumn ibaOldId)
    {
        var ibaAlias = ag.NextTableAlias(ibaTable.Name);
        var typeAlias = ag.NextTableAlias(type_Table.Name.Name);

        return UpdateJoin(
            targetTable: ibaTable, targetAlias: ibaAlias,
            setClause: $"""
            --The target no longer exists, so the remap above could not reach this row and the stale id is cleared
            {Esc(ibaOldId)} = null
            """,
            sourceTable: type_Table.Name, sourceAlias: typeAlias,
            joinCondition: $"""
            {typeAlias}.{Esc(type_Id)} = {ibaAlias}.{Esc(ibaType)}
                AND {typeAlias}.{Esc(type_TableName)} = '{oldTableName}'
                AND {ibaAlias}.{Esc(ibaOldId)} IS NOT NULL
            """);
    }

    #region TEMPORARY Guid primary key migration (added 2026-09)
    // Until now, IUserAssetEntity entities (UserQuery, UserChart, Dashboard, Toolbar, EmailTemplate,
    // WordTemplate, Workflow*, ...) had an int identity primary key PLUS a separate
    // [UniqueIndex] Guid Guid column. That Guid, not the Id, was the stable identifier used to match
    // entities across databases in the UserAssets XML export/import.
    //
    // Now the primary key IS the Guid and the Guid column is gone. A plain synchronization would add
    // the new uniqueidentifier Id filled by NEWID() (the "Default NewID()" branch in
    // SchemaSynchronizer) and then drop the Guid column, silently invalidating every previously
    // exported .xml file and every cross-database reference. So instead, when the primary key type
    // changes to uniqueidentifier and the old table still has a Guid column, we steal that value into
    // the new primary key, keeping the identifiers stable.
    //
    // TO REMOVE once every application has run this migration: delete this whole region, its call
    // sites in SchemaSynchronizer (search for StealGuid and AssertGuidColumn) and the
    // primaryKeyTypeChanged set that feeds them.

    public const string ObsoleteGuidColumnName = "Guid";

    //Case insensitive: the column is "Guid" in SqlServer but the idiomatic "guid" in Postgres
    static bool IsObsoleteGuidColumn(string columnName) => string.Equals(columnName, ObsoleteGuidColumnName, StringComparison.OrdinalIgnoreCase);

    static readonly AbstractDbType GuidDbType = new AbstractDbType(SqlDbType.UniqueIdentifier, NpgsqlDbType.Uuid);

    /// <summary>
    /// Returns the obsolete <c>Guid</c> column of <paramref name="dif"/> whose value should be reused as the
    /// new Guid primary key of <paramref name="tab"/>, or null when there is nothing to steal.
    /// </summary>
    public DiffColumn? TryGetGuidColumnToSteal(ITable tab, DiffTable dif)
    {
        if (!tab.PrimaryKey.DbType.Equals(GuidDbType))
            return null; //the model does not ask for a Guid primary key

        if (tab.Columns.Values.Any(c => IsObsoleteGuidColumn(c.Name)))
            return null; //the model still declares a Guid column, so it is not being removed

        return dif.Columns.Values.SingleOrDefaultEx(c => IsObsoleteGuidColumn(c.Name) && c.DbType.Equals(GuidDbType));
    }

    /// <summary>
    /// <c>UPDATE tab SET Id = Guid</c>. Must run after the new primary key column has been added and before
    /// the foreign keys pointing to it are updated by joining on the renamed Id_old column.
    /// </summary>
    public SqlPreCommand StealGuidIntoPrimaryKey(ITable tab, IColumn newPk, DiffColumn guidCol, bool withHistory)
    {
        SafeConsole.WriteLineColor(ConsoleColor.Cyan, $"Reusing the values in column '{guidCol.Name}' for '{newPk.Name}' (now of type {newPk.DbType.ToString(isPostgres).ToLowerInvariant()}) in {tab.Name}");

        var update = new SqlPreCommandSimple($"""
            --Reuse the value of the obsolete {tab.Name}.{guidCol.Name} column as the new primary key, so the ids stay stable across databases
            UPDATE {tab.Name} SET
                {Esc(newPk)} = {Esc(guidCol)}
            """).Do(a => a.GoAfter = true);

        if (!withHistory || tab.SystemVersioned == null)
            return update;

        //The history table mirrors the Guid column, so its rows carry the right value too, including rows
        //whose entity no longer exists in the main table. This replaces UpdateHistoryTable for this table.
        var updateHistory = new SqlPreCommandSimple($"""
            UPDATE {tab.SystemVersioned.TableName} SET
                {Esc(newPk)} = {Esc(guidCol)}
            """).Do(a => a.GoAfter = true);

        return SqlPreCommand.Combine(Spacing.Simple, update, updateHistory)!;
    }

    /// <summary>
    /// Refuses to silently drop a <c>Guid</c> column when its value is not being stolen into a Guid primary key.
    /// </summary>
    public void AssertGuidColumnNotSilentlyDropped(ITable tab, DiffColumn difCol, Replacements rep, bool isBeingStolen)
    {
        if (isBeingStolen)
            return; //the value survives as the new primary key, see StealGuidIntoPrimaryKey

        if (!IsObsoleteGuidColumn(difCol.Name) || !difCol.DbType.Equals(GuidDbType))
            return;

        SafeConsole.WriteLineColor(ConsoleColor.Red, $"DANGER: {tab.Name}.{difCol.Name} is about to be DROPPED and all its values will be LOST!");
        SafeConsole.WriteLineColor(ConsoleColor.DarkRed, $"""
            This uniqueidentifier column is typically the stable identifier used to match entities across
            databases in the UserAssets XML export/import. Dropping it invalidates every exported .xml file.
            To preserve the values instead, declare the primary key of {tab.Name} as [PrimaryKey(typeof(Guid))].
            """);

        if (!rep.Interactive)
            throw new InvalidOperationException($"Synchronization aborted: dropping {tab.Name}.{difCol.Name} would lose all its values. Run the synchronization interactively to confirm, or declare a Guid primary key so the values are preserved.");

        if (!SafeConsole.Ask($"Drop {tab.Name}.{difCol.Name} losing all its values?"))
            throw new InvalidOperationException($"Synchronization aborted by the user: dropping {tab.Name}.{difCol.Name} would lose all its values.");
    }

    #endregion
}
