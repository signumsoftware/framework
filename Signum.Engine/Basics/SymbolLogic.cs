using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Engine.Maps;
using Signum.Utilities;
using System.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities;
using Signum.Engine.DynamicQuery;

namespace Signum.Engine
{

    public static class SymbolLogic
    {
        public static event Action OnLoadAll;

        public static void LoadAll()
        {
            OnLoadAll?.Invoke();
        }
    }

    public static class SymbolLogic<T>
        where T: Symbol
    {
        static ResetLazy<Dictionary<string, T>> lazy;
        static Func<IEnumerable<T>> getSymbols;

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
            if (sb.NotDefined(typeof(SymbolLogic<T>).GetMethod("Start")))
            {
                sb.Include<T>()
                    .WithQuery(() => t => new
                    {
                        Entity = t,
                        t.Id,
                        t.Key
                    });

                SymbolLogic.OnLoadAll += () => lazy.Load();
                sb.Schema.Initializing += () => lazy.Load();
                sb.Schema.Synchronizing += Schema_Synchronizing;
                sb.Schema.Generating += Schema_Generating;

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
                        return result.ToDictionary(a => a.Key);
                    }
                }, 
                new InvalidateWith(typeof(T)),
                Schema.Current.InvalidateMetadata);

                sb.Schema.EntityEvents<T>().Retrieved += SymbolLogic_Retrieved;
            }
        }

        static void SymbolLogic_Retrieved(T ident)
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
      
        static SqlPreCommand Schema_Generating()
        {
            Table table = Schema.Current.Table<T>();

            IEnumerable<T> should = getSymbols();

            return should.Select((a, i) => table.InsertSqlSync(a, suffix: i.ToString())).Combine(Spacing.Simple).PlainSqlCommand();
        }

        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
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

        static Dictionary<string, T> AssertStarted()
        {
            if (lazy == null)
                throw new InvalidOperationException("{0} has not been started. Someone should have called {0}.Start before".FormatWith(typeof(SymbolLogic<T>).TypeName()));

            return lazy.Value;
        }

        public static ICollection<T> Symbols
        {
            get { return AssertStarted().Values; }
        }

        public static T TryToSymbol(string key)
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
}
