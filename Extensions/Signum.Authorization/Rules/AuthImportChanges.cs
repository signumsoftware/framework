using Signum.Engine.Maps;
using Signum.Engine.Sync;

namespace Signum.Authorization.Rules;

public enum ChangeKind
{
    Insert,
    Update,
    Delete,
}

/// <summary>
/// One change an AuthRules.xml import wants to make, kept as the entity instead of as SQL so that the caller
/// can either write the script to review it or apply it directly, whichever it needs.
/// </summary>
public class Change<T>
    where T : Entity
{
    public required T Entity;
    public required ChangeKind Kind;
    public string? Comment;

    /// <summary>
    /// Only to keep the script grouped the way it has always been, one block per role.
    /// </summary>
    public Lite<RoleEntity>? Role;
}

public interface IChangeTable
{
    Table Table { get; }
    int Count { get; }
    SqlPreCommand? ToSqlCommand();
    void Apply();
}

public class ChangeTable<T> : IChangeTable
    where T : Entity
{
    public Table Table { get; } = Schema.Current.Table(typeof(T));

    public List<Change<T>> Changes { get; } = new List<Change<T>>();

    public int Count => Changes.Count;

    public void Add(T entity, ChangeKind kind, Lite<RoleEntity>? role = null, string? comment = null)
    {
        Changes.Add(new Change<T> { Entity = entity, Kind = kind, Role = role, Comment = comment });
    }

    /// <summary>
    /// The script to review before running it, with the same statements and comments as always.
    /// </summary>
    public SqlPreCommand? ToSqlCommand()
    {
        return Changes.GroupBy(c => c.Role).Select(gr => gr.Select(c => c.Kind switch
        {
            ChangeKind.Insert => Table.InsertSqlSync(c.Entity, comment: c.Comment),
            ChangeKind.Update => Table.UpdateSqlSync(c.Entity, null, comment: c.Comment),
            ChangeKind.Delete => Table.DeleteSqlSync(c.Entity, null, c.Comment),
            _ => throw new UnexpectedValueException(c.Kind),
        }).Combine(Spacing.Simple)?.Do(p => p.GoBefore = true)).Combine(Spacing.Double);
    }

    /// <summary>
    /// How the new rows are written. The default is one bulk copy, which is enough for a table that is just
    /// rows. A table with a virtual MList needs its own, to write the lines once the parents have ids.
    /// </summary>
    public Action<List<T>> BulkInsertAction = list => list.BulkInsert();

    /// <summary>
    /// The same changes without going through SQL text. Inserting the rules one statement at a time was
    /// almost the whole cost of importing AuthRules.xml, a bulk copy does the same work in a fraction of it.
    /// </summary>
    public void Apply()
    {
        using (HeavyProfiler.Log(nameof(ChangeTable<T>) + "." + nameof(Apply), () => typeof(T).TypeName()))
        using (var tr = new Transaction())
        {
            //First, so a rule being replaced can not collide with the one taking its place.
            //UnsafeDelete, so the rows of any virtual MList go with it.
            var deletes = Changes.Where(c => c.Kind == ChangeKind.Delete).Select(c => c.Entity.ToLite()).ToList();
            if (deletes.Count > 0)
                Database.DeleteList(deletes);

            foreach (var c in Changes.Where(c => c.Kind == ChangeKind.Update))
                c.Entity.Save();

            var inserts = Changes.Where(c => c.Kind == ChangeKind.Insert).Select(c => c.Entity).ToList();
            if (inserts.Count > 0)
                BulkInsertAction(inserts);

            tr.Commit();
        }
    }
}

public static class ChangeTableExtensions
{
    /// <summary>
    /// Bulk copy that then reads the ids back by a unique key, so the rows that point to these can follow.
    /// Skips the read when there is nothing to insert, since it would be a query for an empty answer.
    /// </summary>
    public static void BulkInsertQueryIdsIfAny<E, UK>(this List<E> entities, Expression<Func<E, UK>> uniqueKey)
        where E : Entity
        where UK : notnull
    {
        if (entities.Count > 0)
            entities.BulkInsertQueryIds(uniqueKey);
    }
}

/// <summary>
/// Everything an AuthRules.xml import changes, so that ImportRulesScript can write it out and ImportAuthRules
/// can apply it without going through a single INSERT statement.
/// </summary>
public class AuthImportChanges
{
    public List<IChangeTable> Tables { get; private set; } = new List<IChangeTable>();

    /// <summary>
    /// Roles in the database that the file does not configure. Only a comment, there is nothing to apply.
    /// </summary>
    public SqlPreCommand? Warnings;

    public int Count => Tables.Sum(t => t.Count);

    public SqlPreCommand? ToSqlCommand()
    {
        var body = Tables.Select(t => t.ToSqlCommand()).Combine(Spacing.Triple);

        if (body == null && Warnings == null)
            return null;

        return SqlPreCommand.Combine(Spacing.Triple,
            new SqlPreCommandSimple("-- BEGIN AUTH SYNC SCRIPT"),
            Connector.Current.SqlBuilder.UseDatabase(),
            Warnings,
            body,
            new SqlPreCommandSimple("-- END AUTH SYNC SCRIPT"));
    }

    public void Apply()
    {
        using (HeavyProfiler.Log(nameof(AuthImportChanges) + "." + nameof(Apply)))
        using (var tr = new Transaction())
        {
            foreach (var t in Tables)
                t.Apply();

            tr.Commit();
        }
    }
}
