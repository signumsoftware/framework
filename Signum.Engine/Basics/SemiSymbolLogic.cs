using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Extensions.Basics
{
    public static class SemiSymbolLogic<T>
        where T : SemiSymbol
    {
        static ResetLazy<Dictionary<string, T>> lazy;
        static Func<IEnumerable<T>> getSemiSymbols;

        [ThreadStatic]
        static bool avoidCache;

        static IDisposable AvoidCache()
        {
            var old = avoidCache;
            avoidCache = true;
            return new Disposable(() => avoidCache = old);
        }

        public static void Start(SchemaBuilder sb, Func<IEnumerable<T>> getSemiSymbols)
        {
            if (sb.NotDefined(typeof(SemiSymbolLogic<T>).GetMethod("Start")))
            {
                sb.Include<T>();

                sb.Schema.Initializing += () => lazy.Load();
                sb.Schema.Synchronizing += Schema_Synchronizing;
                sb.Schema.Generating += Schema_Generating;

                SemiSymbolLogic<T>.getSemiSymbols = getSemiSymbols;
                lazy = sb.GlobalLazy(() =>
                {
                    using (AvoidCache())
                    {
                        var current = Database.RetrieveAll<T>().Where(a => a.Key.HasText());

                        var result = EnumerableExtensions.JoinRelaxed(
                          current,
                          getSemiSymbols(),
                          c => c.Key,
                          s => s.Key,
                          (c, s) => { s.SetIdAndName((c.Id, c.Name)); return s; },
                          "caching " + typeof(T).Name);

                        SemiSymbol.SetSemiSymbolIdsAndNames<T>(current.ToDictionary(a => a.Key, a => (a.Id, a.Name)));
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
            if (!avoidCache && ident.Key.HasText())
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

            IEnumerable<T> should = CreateSemiSymbols();

            return should.Select((a, i) => table.InsertSqlSync(a, suffix: i.ToString())).Combine(Spacing.Simple);
        }

        private static IEnumerable<T> CreateSemiSymbols()
        {
            IEnumerable<T> should = getSemiSymbols().ToList();

            using (CultureInfoUtils.ChangeCulture(Schema.Current.ForceCultureInfo))
                foreach (var item in should)
                    item.Name = item.NiceToString();

            return should;
        }

        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            Table table = Schema.Current.Table<T>();

            List<T> current = AvoidCache().Using(_ => Administrator.TryRetrieveAll<T>(replacements));
            IEnumerable<T> should = CreateSemiSymbols();

            using (replacements.WithReplacedDatabaseName())
                return Synchronizer.SynchronizeScriptReplacing(replacements, typeof(T).Name, Spacing.Double,
                    should.ToDictionary(s => s.Key),
                    current.Where(c => c.Key.HasText()).ToDictionary(c => c.Key),
                    createNew: (k, s) => table.InsertSqlSync(s),
                    removeOld: (k, c) => table.DeleteSqlSync(c, s => s.Key == c.Key),
                    mergeBoth: (k, s, c) =>
                    {
                        var originalKey = c.Key;
                        c.Key = s.Key;
                        c.Name = s.Name;
                        return table.UpdateSqlSync(c, ss => ss.Key == originalKey, comment: originalKey);
                    });
        }

        static Dictionary<string, T> AssertStarted()
        {
            if (lazy == null)
                throw new InvalidOperationException("{0} has not been started. Someone should have called {0}.Start before".FormatWith(typeof(SemiSymbolLogic<T>).TypeName()));

            return lazy.Value;
        }

        public static ICollection<T> SemiSymbols
        {
            get { return AssertStarted().Values; }
        }
    }
}
