using Signum.Utilities.Reflection;
using Signum.Engine.Maps;
using System.Text.RegularExpressions;

namespace Signum.DynamicQuery;

public static class AutocompleteUtils
{
    public static List<Lite<Entity>> FindLiteLike(Implementations implementations, string subString, int count)
    {
        if (implementations.IsByAll)
            throw new InvalidOperationException("ImplementedByAll not supported for FindLiteLike");

        try
        {
            using (ExecutionMode.UserInterface())
                return FindLiteLike(implementations.Types, subString, count);
        }
        catch (Exception e)
        {
            e.Data["implementations"] = implementations.ToString();
            throw;
        }
    }

    public static async Task<List<Lite<Entity>>> FindLiteLikeAsync(Implementations implementations, string subString, int count, CancellationToken cancellationToken)
    {
        if (implementations.IsByAll)
            throw new InvalidOperationException("ImplementedByAll not supported for FindLiteLike");

        try
        {
            using (ExecutionMode.UserInterface())
                return await FindLiteLikeAsync(implementations.Types, subString, count, cancellationToken);
        }
        catch (Exception e)
        {
            e.Data["implementations"] = implementations.ToString();
            throw;
        }
    }

    private static bool TryParsePrimaryKey(string value, Type type, out PrimaryKey id)
    {
        var match = Regex.Match(value, "^id[:]?(.*)", RegexOptions.IgnoreCase);
        if (match.Success)
            return PrimaryKey.TryParse(match.Groups[1].ToString(), type, out id);

        Lite.TryParseLite(value, out Lite<Entity>? lite);
        if (lite != null && lite.EntityType == type) {
            id = lite.Id;
            return true;
        }

        id = default;
        return false;
    }

    static List<Lite<Entity>> FindLiteLike(IEnumerable<Type> types, string subString, int count)
    {
        if (subString == null)
            subString = "";

        types = types.Where(t => Schema.Current.IsAllowed(t, inUserInterface: true) == null);

        List<Lite<Entity>> results = new List<Lite<Entity>>();

        foreach (var t in types)
        {
            var parts = subString.Split("|");
            foreach (var p in parts)
            {
                if (TryParsePrimaryKey(p, t, out PrimaryKey id))
                {
                    var lite = giLiteById.GetInvoker(t).Invoke(id);
                    if (lite != null)
                    {
                        results.Add(lite);

                        if (results.Count >= count)
                            return results;
                    }
                }
            };
        }


        foreach (var t in types)
        {
            if (!TryParsePrimaryKey(subString, t, out PrimaryKey id))
            {
                var parts = subString.SplitParts();

                results.AddRange(giLiteContaining.GetInvoker(t)(parts, count - results.Count));

                if (results.Count >= count)
                    return results;
            }
        }

        return results;
    }

    private static async Task<List<Lite<Entity>>> FindLiteLikeAsync(IEnumerable<Type> types, string subString, int count, CancellationToken cancellationToken)
    {
        if (subString == null)
            subString = "";

        types = types.Where(t => Schema.Current.IsAllowed(t, inUserInterface: true) == null);

        List<Lite<Entity>> results = new List<Lite<Entity>>();

        foreach (var t in types)
        {
            var parts = subString.Split("|");
            foreach (var p in parts)
            {
                if (TryParsePrimaryKey(p, t, out PrimaryKey id))
                {
                    var lite = await giLiteByIdAsync.GetInvoker(t).Invoke(id, cancellationToken);
                    if (lite != null)
                    {
                        results.Add(lite);

                        if (results.Count >= count)
                            return results;
                    }
                }
            }
        }

        foreach (var t in types)
        {
            if (!TryParsePrimaryKey(subString, t, out PrimaryKey id))
            {
                var parts = subString.SplitParts();

                var list = await giLiteContainingAsync.GetInvoker(t)(parts, count - results.Count, cancellationToken);
                results.AddRange(list);

                if (results.Count >= count)
                    return results;
            }
        }

        return results;
    }

    static GenericInvoker<Func<PrimaryKey, Lite<Entity>?>> giLiteById =
        new(id => LiteById<TypeEntity>(id));
    static Lite<Entity>? LiteById<T>(PrimaryKey id)
        where T : Entity
    {
        return Database.Query<T>().Where(a => a.id == id).Select(a => a.ToLite()).SingleOrDefault();
    }

    static GenericInvoker<Func<PrimaryKey, CancellationToken, Task<Lite<Entity>?>>> giLiteByIdAsync =
        new((id, token) => LiteByIdAsync<TypeEntity>(id, token));
    static async Task<Lite<Entity>?> LiteByIdAsync<T>(PrimaryKey id, CancellationToken token)
        where T : Entity
    {
        return (Lite<Entity>?)await Database.Query<T>().Where(a => a.id == id).Select(a => a.ToLite()).SingleOrDefaultAsync(token);
    }

    static GenericInvoker<Func<string[], int, List<Lite<Entity>>>> giLiteContaining =
        new((parts, c) => LiteContaining<TypeEntity>(parts, c));
    static List<Lite<Entity>> LiteContaining<T>(string[] parts, int count)
        where T : Entity
    {
        return Database.Query<T>()
            .Where(a => a.ToString().ContainsAllParts(parts))
            .OrderBy(a => a.ToString().Length)
            .Select(a => a.ToLite())
            .Take(count)
            .AsEnumerable()
            .Cast<Lite<Entity>>()
            .ToList();
    }

    static GenericInvoker<Func<string[], int, CancellationToken, Task<List<Lite<Entity>>>>> giLiteContainingAsync =
        new((parts, c, token) => LiteContaining<TypeEntity>(parts, c, token));
    static async Task<List<Lite<Entity>>> LiteContaining<T>(string[] parts, int count, CancellationToken token)
        where T : Entity
    {
        var list = await Database.Query<T>()
            .Where(a => a.ToString().ContainsAllParts(parts))
            .OrderBy(a => a.ToString().Length)
            .Select(a => a.ToLite())
            .Take(count)
            .ToListAsync(token);

        return list.Cast<Lite<Entity>>().ToList();
    }

    public static List<Lite<Entity>> FindAllLite(Implementations implementations)
    {
        if (implementations.IsByAll)
            throw new InvalidOperationException("ImplementedByAll is not supported for RetrieveAllLite");

        try
        {
            using (ExecutionMode.UserInterface())
                return implementations.Types.SelectMany(type => Database.RetrieveAllLite(type)).ToList();
        }
        catch (Exception e)
        {
            e.Data["implementations"] = implementations.ToString();
            throw;
        }
    }

    public static async Task<List<Lite<Entity>>> FindAllLiteAsync(Implementations implementations, CancellationToken token)
    {
        if (implementations.IsByAll)
            throw new InvalidOperationException("ImplementedByAll is not supported for RetrieveAllLite");

        try
        {
            using (ExecutionMode.UserInterface())
            {
                var tasks = implementations.Types.Select(type => Database.RetrieveAllLiteAsync(type, token)).ToList();

                var list = await Task.WhenAll(tasks);

                return list.SelectMany(li => li).ToList();
            }
        }
        catch (Exception e)
        {
            e.Data["implementations"] = implementations.ToString();
            throw;
        }
    }

    public static List<Lite<T>> Autocomplete<T>(this IQueryable<Lite<T>> query, string subString, int count)
        where T : Entity
    {
        using (ExecutionMode.UserInterface())
        {

            List<Lite<T>> results = new List<Lite<T>>();

            var parts = subString.Split("|");
            foreach (var p in parts)
            {
                if (TryParsePrimaryKey(p, typeof(T), out PrimaryKey id))
                {
                    Lite<T>? entity = query.SingleOrDefaultEx(e => e.Id == id);

                    if (entity != null)
                        results.Add(entity);

                    if (results.Count >= count)
                        return results;
                }
            }

            parts = subString.SplitParts();

            results.AddRange(query.Where(a => a.ToString()!.ContainsAllParts(parts))
                .OrderBy(a => a.ToString()!.Length)
                .Take(count - results.Count));

            return results;
        }
    }

    public static async Task<List<Lite<T>>> AutocompleteAsync<T>(this IQueryable<Lite<T>> query, string subString, int count, CancellationToken token)
            where T : Entity
    {
        using (ExecutionMode.UserInterface())
        {
            List<Lite<T>> results = new List<Lite<T>>();

            var parts = subString.Split("|");
            foreach (var p in parts)
            {
                if (TryParsePrimaryKey(p, typeof(T), out PrimaryKey id))
                {
                    Lite<T>? entity = await query.SingleOrDefaultAsync(e => e.Id == id, token);

                    if (entity != null)
                        results.Add(entity);

                    if (results.Count >= count)
                        return results;
                }
            }

            parts = subString.SplitParts();

            var list = await query.Where(a => a.ToString()!.ContainsAllParts(parts))
                .OrderBy(a => a.ToString()!.Length)
                .Take(count - results.Count)
                .ToListAsync(token);

            results.AddRange(list);

            return results;
        }
    }

    public static List<Lite<T>> Autocomplete<T>(this IEnumerable<Lite<T>> collection, string subString, int count)
        where T : Entity
    {
        using (ExecutionMode.UserInterface())
        {
            List<Lite<T>> results = new List<Lite<T>>();

            var parts = subString.Split("|");
            foreach (var p in parts)
            {
                if (TryParsePrimaryKey(subString, typeof(T), out PrimaryKey id))
                {
                    Lite<T>? entity = collection.SingleOrDefaultEx(e => e.Id == id);

                    if (entity != null)
                        results.Add(entity);

                    if (results.Count >= count)
                        return results;
                }
            }

            parts = subString.SplitParts();

            var list = collection.Where(a => a.ToString()!.ContainsAllParts(parts))
                .OrderBy(a => a.ToString()!.Length)
                .Take(count - results.Count);

            results.AddRange(list);

            return results;
        }
    }

    public static string[] SplitParts(this string str)
    {
        if (FilterCondition.ToLowerString())
            return str.Trim().ToLower().SplitNoEmpty(' ');

        return str.Trim().SplitNoEmpty(' ');
    }

    [AutoExpressionField]
    public static bool ContainsAllParts(this string str, string[] parts) => As.Expression(() =>
        FilterCondition.ToLowerString() ?
        str.ToLower().ContainsAll(parts) :
        str.ContainsAll(parts));

}
