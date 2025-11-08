using Npgsql.Internal.Postgres;
using Signum.Engine.Linq;
using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Sync;
internal class PrimaryKeyUpdater
{


    private bool isPostgres;
    private Dictionary<string, DiffTable> databaseTables;
    private Dictionary<string, ITable> modelTables;
    private Dictionary<ITable, Dictionary<string, List<IColumn>>> ibas;

    private Table type_Table;
    private IColumn type_Id;
    private IColumn type_TableName;

    public PrimaryKeyUpdater(bool isPostgres, Dictionary<string, DiffTable> databaseTables, Dictionary<string, ITable> modelTables)
    {
        this.isPostgres = isPostgres;
        this.databaseTables = databaseTables;
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
        string? otherJoins = null)
    {
        if (isPostgres)
        {
            // PostgreSQL syntax: UPDATE target_alias SET ... FROM source_table source_alias WHERE ...
            var sql = $"""
                    UPDATE {targetTable} {targetAlias} SET
                    {setClause.Indent(4)}
                    FROM {sourceTable} {sourceAlias}{(otherJoins != null ? "\n" + otherJoins : "")}
                    WHERE {joinCondition}
                    """;

            return new SqlPreCommandSimple(sql).Do(a => a.GoAfter = true);
        }
        else
        {
            // SQL Server syntax: UPDATE t SET ... FROM target_table t JOIN source_table s ON ...
            var sql = $"""
                    UPDATE {targetAlias} SET
                    {setClause.Indent(4)}
                    FROM {targetTable} {targetAlias}
                    JOIN {sourceTable} {sourceAlias} ON {joinCondition}{(otherJoins != null ? "\n" + otherJoins : "")}
                    """;

            return new SqlPreCommandSimple(sql).Do(a => a.GoAfter = true);
        }
    }


    public SqlPreCommandSimple? UpdateFKToAnotherTable(ObjectName tn, DiffColumn difCol, IColumn tabCol, Func<ObjectName, ObjectName> changeName)
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
        var count = (int)Executor.ExecuteScalar($"""
                    SELECT Count(*) 
                    FROM {ibaTable} iba
                    JOIN {type_Table} type 
                    ON type.{Esc(this.type_Id)} = iba.{ibaType} 
                    AND type.{Esc(this.type_TableName)} = '{oldTableName}' 
                    """
            )!;

        if (count == 0)
            return null;

        AliasGenerator ag = new AliasGenerator();
        var ibaAlias = ag.NextTableAlias(ibaTable.Name);
        var tableAlias = ag.NextTableAlias(table.Name.Name);

        if (table.SystemVersioned == null)
        {
            return UpdateJoin(
                targetTable: ibaTable, targetAlias: ibaAlias,
                setClause: $"""
            {Esc(ibaNewId)} = {tableAlias}.{Esc(newId)},
            {Esc(ibaOldId)} = null
            """,
                sourceTable: table.Name, sourceAlias: tableAlias,
                joinCondition: $"{tableAlias}.{Esc(oldId)} = {ibaAlias}.{Esc(ibaOldId)}",
                otherJoins: $"JOIN {type_Table.Name} type ON type.{Esc(type_Id)} = {ibaAlias}.{Esc(ibaType)} AND type.{Esc(type_TableName)} = '{oldTableName}'");
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
              otherJoins: $"JOIN {type_Table.Name} type ON type.{Esc(type_Id)} = {ibaAlias}.{Esc(ibaType)} AND type.{Esc(type_TableName)} = '{oldTableName}'");

            update.AlterSql(cte + "\n" + update.Sql);

            return update;
        }
    }
}
