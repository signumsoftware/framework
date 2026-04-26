using Signum.Engine.Maps;
using Signum.Engine.Sync;
using System.Collections.Frozen;

namespace Signum.Basics;


public static class SymbolLogic
{
    public static event Action? OnLoadAll;
    public static event Func<IEnumerable<Type>>? OnGetSymbolContainer;
     
    public static IEnumerable<Type> AllSymbolContainers()
    {
        return OnGetSymbolContainer.GetInvocationListTyped().SelectMany(f => f());

    }

    public static void LoadAll()
    {
        OnLoadAll?.Invoke();
    }
}

public static class SymbolLogic<T>
    where T : Symbol
{
    static ResetLazy<FrozenDictionary<string, T>> lazy = null!;
    static Func<IEnumerable<T>> getSymbols = null!;

    [ThreadStatic]
    static bool avoidCache;

    static IDisposable AvoidCache()
    {
        var old = avoidCache;
        avoidCache = true;
        return new Disposable(() => avoidCache = old);
    }

    public static void Start(SchemaBuilder sb, Func<IEnumerable<T>> getSymbols)
    {
        if (sb.AlreadyDefined(typeof(SymbolLogic<T>).GetMethod("Start")))
            return;

        sb.Include<T>()
            .WithQuery(() => t => new
            {
                Entity = t,
                t.Id,
                t.Key
            });

        SymbolLogic.OnLoadAll += () => lazy.Load();
        SymbolLogic.OnGetSymbolContainer += () => SymbolLogic<T>.getSymbols().Select(a => a.FieldInfo.DeclaringType!).Distinct();
        sb.Schema.Initializing += () => lazy.Load();
        sb.Schema.Synchronizing += Schema_Synchronizing;
        sb.Schema.Generating += Schema_Generating;
        sb.Schema.EntityEvents<T>().Saved += SymbolLogic_Saved;

        SymbolLogic<T>.getSymbols = getSymbols;
        lazy = sb.GlobalLazy(() =>
        {
            using (AvoidCache())
            {
                var current = Database.RetrieveAll<T>();

                var result = EnumerableExtensions.JoinRelaxed(
                    current,
                    getSymbols(),
                    c => c.Key,
                    s => s.Key,
                    (c, s) => s.SetId(c.Id),
                    "caching " + typeof(T).Name);

                Symbol.SetSymbolIds<T>(current.ToDictionary(a => a.Key, a => a.Id));
                return result.ToFrozenDictionaryEx(a => a.Key);
            }
        },
        new InvalidateWith(typeof(T)),
        Schema.Current.InvalidateMetadata);

        sb.Schema.EntityEvents<T>().Retrieved += SymbolLogic_Retrieved;
        Symbol.CallRetrieved += (ss) =>
        {
            if (ss is T t)
                if (t.Key != null && t.FieldInfo == null)
                    SymbolLogic_Retrieved(t, new PostRetrievingContext());
        };
    }

    private static void SymbolLogic_Saved(T ident, SavedEventArgs args)
    {
        if (args.WasModified || args.WasNew)
            throw new InvalidOperationException($"Attempt to save symbol {ident} of type {ident.GetType()}");
    }

    static void SymbolLogic_Retrieved(T ident, PostRetrievingContext ctx)
    {
        if (!avoidCache)
            try
            {
                ident.FieldInfo = lazy.Value.GetOrThrow(ident.Key).FieldInfo;
            }
            catch (Exception e) when (StartParameters.IgnoredDatabaseMismatches != null)
            {
                //Could happen when not 100% synchronized
                StartParameters.IgnoredDatabaseMismatches.Add(e);
            }
    }

    static SqlPreCommand? Schema_Generating()
    {
        Table table = Schema.Current.Table<T>();

        IEnumerable<T> should = getSymbols();

        return should.Select((a, i) => table.InsertSqlSync(a, suffix: i.ToString())).Combine(Spacing.Simple)?.PlainSqlCommand();
    }

    static SqlPreCommand? Schema_Synchronizing(Replacements replacements)
    {
        Table table = Schema.Current.Table<T>();

        List<T> current = AvoidCache().Using(_ => Administrator.TryRetrieveAll<T>(replacements));
        IEnumerable<T> should = getSymbols();

        using (replacements.WithReplacedDatabaseName())
            return Synchronizer.SynchronizeScriptReplacing(replacements, typeof(T).Name, Spacing.Double,
                should.ToDictionary(s => s.Key),
                current.ToDictionary(c => c.Key),
                createNew: (k, s) => table.InsertSqlSync(s),
                removeOld: (k, c) => table.DeleteSqlSync(c, s => s.Key == c.Key),
                mergeBoth: (k, s, c) =>
                {
                    var originalKey = c.Key;
                    c.Key = s.Key;
                    return table.UpdateSqlSync(c, ss => ss.Key == originalKey, comment: originalKey);
                });
    }

    static FrozenDictionary<string, T> AssertStarted()
    {
        if (lazy == null)
            throw new InvalidOperationException("{0} has not been started. Someone should have called {0}.Start before".FormatWith(typeof(SymbolLogic<T>).TypeName()));

        return lazy.Value;
    }

    public static ICollection<T> Symbols
    {
        get { return AssertStarted().Values; }
    }

    public static T? TryToSymbol(string key)
    {
        return AssertStarted().TryGetC(key);
    }

    public static HashSet<string> AllUniqueKeys()
    {
        return AssertStarted().Select(a => a.Key).ToHashSet();
    }

    public static T ToSymbol(string key)
    {
        return AssertStarted().GetOrThrow(key);
    }
}
