using Signum.Engine.Maps;
using Signum.Engine.Sync;
using Signum.Utilities.Reflection;
using NpgsqlTypes;
using System.Data;

namespace Signum.Translation.Instances;

/// <summary>
/// <see cref="TranslatedInstanceEntity.RowId"/> stores the row id of an MList element as a string, not as a foreign key,
/// so the synchronizer can not update it on its own when the primary key of that MList table changes type
/// (typically int -> Guid). This plugs into <see cref="SchemaSynchronizer.UpdateMListRowIdReferences"/> to generate the
/// UPDATE, joining the old row id through the renamed <c>_old</c> column.
/// <para>Without this, every translation of a property inside that MList is silently orphaned, and the next
/// <see cref="TranslatedInstanceLogic.CleanTranslations"/> deletes it.</para>
/// </summary>
public static class TranslatedInstanceRowIds
{
    public static void Start(SchemaBuilder sb)
    {
        SchemaSynchronizer.UpdateMListRowIdReferences = UpdateTranslatedInstanceRowIds;
    }

    static SqlPreCommand? UpdateTranslatedInstanceRowIds(TableMList mlistTable, IColumn newPk, DiffColumn oldPk)
    {
        //An MList table corresponds to exactly one PropertyRoute, so at most one of the translatable routes matches
        var mlistRoute = (from kvp in PropertyRouteTranslationLogic.TranslateableRoutes
                          from route in kvp.Value.Keys
                          let mr = route.GetMListItemsRoute()
                          where mr != null && Schema.Current.TryField(mr.Parent!) is FieldMList fm && fm.TableMList == mlistTable
                          select mr).FirstOrDefault();

        if (mlistRoute == null)
            return null; //Nothing translatable inside this MList, so no RowId points at it

        var isPostgres = Schema.Current.Settings.IsPostgres;

        var ti = Schema.Current.Table<TranslatedInstanceEntity>();
        var ti_RowId = Column(ti, (TranslatedInstanceEntity e) => e.RowId);
        var ti_PropertyRoute = Column(ti, (TranslatedInstanceEntity e) => e.PropertyRoute);

        var pr = Schema.Current.Table<PropertyRouteEntity>();
        var pr_Path = Column(pr, (PropertyRouteEntity e) => e.Path);
        var pr_RootType = Column(pr, (PropertyRouteEntity e) => e.RootType);

        var type = Schema.Current.Table<TypeEntity>();
        var type_CleanName = Column(type, (TypeEntity e) => e.CleanName);

        var cleanName = TypeLogic.GetCleanName(mlistRoute.RootType);
        var pathPattern = mlistRoute.PropertyString() + "%"; //MListItems routes end in '/', so "Filters/" matches "Filters/Pinned.Label"

        SafeConsole.WriteLineColor(ConsoleColor.Cyan, $"Remapping {nameof(TranslatedInstanceEntity)}.{nameof(TranslatedInstanceEntity.RowId)} of '{cleanName}.{pathPattern}' to the new row ids of {mlistTable.Name}");

        var comment = $"--{ti.Name}.{ti_RowId.Name} keeps the row id of {mlistTable.Name} as a string, so it has to follow the new primary key";

        //Casting an int to text needs no care, but SqlServer renders a uniqueidentifier in upper case while
        //Guid.ToString() (what the application writes) is lower case, so it has to be lowered explicitly.
        var guidDbType = new AbstractDbType(SqlDbType.UniqueIdentifier, NpgsqlDbType.Uuid);

        string AsText(string alias, string columnName, bool isGuid) =>
            isPostgres ? $"{alias}.{Esc(columnName)}::text" :
            isGuid ? $"LOWER(CONVERT(varchar(36), {alias}.{Esc(columnName)}))" :
            $"CONVERT(varchar(100), {alias}.{Esc(columnName)})";

        var newValue = AsText("mle", newPk.Name, newPk.DbType.Equals(guidDbType));
        var oldValue = AsText("mle", oldPk.Name, oldPk.DbType.Equals(guidDbType));

        var sql = isPostgres ?
            $"""
            {comment}
            UPDATE {ti.Name} ti SET
                {Esc(ti_RowId.Name)} = {newValue}
            FROM {mlistTable.Name} mle, {pr.Name} pr, {type.Name} t
            WHERE ti.{Esc(ti_PropertyRoute.Name)} = pr.{Esc(pr.PrimaryKey.Name)}
                AND pr.{Esc(pr_RootType.Name)} = t.{Esc(type.PrimaryKey.Name)}
                AND t.{Esc(type_CleanName.Name)} = '{cleanName}'
                AND pr.{Esc(pr_Path.Name)} LIKE '{pathPattern}'
                AND ti.{Esc(ti_RowId.Name)} = {oldValue}
            """ :
            $"""
            {comment}
            UPDATE ti SET
                {Esc(ti_RowId.Name)} = {newValue}
            FROM {ti.Name} ti
            JOIN {mlistTable.Name} mle ON ti.{Esc(ti_RowId.Name)} = {oldValue}
            JOIN {pr.Name} pr ON ti.{Esc(ti_PropertyRoute.Name)} = pr.{Esc(pr.PrimaryKey.Name)}
            JOIN {type.Name} t ON pr.{Esc(pr_RootType.Name)} = t.{Esc(type.PrimaryKey.Name)} AND t.{Esc(type_CleanName.Name)} = '{cleanName}'
            WHERE pr.{Esc(pr_Path.Name)} LIKE '{pathPattern}'
            """;

        return new SqlPreCommandSimple(sql).Do(a => a.GoAfter = true);

        string Esc(string name) => name.SqlEscape(isPostgres);
    }

    static IColumn Column<T, V>(Table table, Expression<Func<T, V>> property) where T : Entity =>
        (IColumn)((IFieldFinder)table).GetField(ReflectionTools.GetPropertyInfo(property));
}
