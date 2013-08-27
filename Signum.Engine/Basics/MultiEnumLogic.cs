using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Engine.Maps;
using Signum.Utilities;
using System.Reflection;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine
{
    public static class MultiEnumExtensions
    {
        public static T ToEntity<T>(this Enum key) where T:MultiEnumDN, new()
        {
            if (key == null)
                return null;

            return MultiEnumLogic<T>.ToEntity(key);
        }

        public static Enum ToEnum<T>(this T entity) where T : MultiEnumDN, new()
        {
            if (entity == null)
                return null;

            return MultiEnumLogic<T>.ToEnum(entity);
        }
    }

    public static class MultiEnumLogic<T>
        where T:MultiEnumDN, new()
    {
        class LazyState
        {
            public LazyState(Func<HashSet<Enum>> getKeys)
            {
                Keys = getKeys();

                ToEntity = EnumerableExtensions.JoinStrict(
                     Database.RetrieveAll<T>(),
                     Keys,
                     a => a.Key,
                     k => MultiEnumDN.UniqueKey(k),
                     (a, k) => new { a, k }, "loading {0}. Consider synchronize".Formato(typeof(T).Name)).ToDictionary(p => p.k, p => p.a);

                ToEnum = ToEntity.Inverse();

                ToEnumUniqueKey = ToEntity.Keys.ToDictionary(k => MultiEnumDN.UniqueKey(k));
            }

            public HashSet<Enum> Keys;
            public Dictionary<Enum, T> ToEntity;
            public Dictionary<string, Enum> ToEnumUniqueKey;
            public Dictionary<T, Enum> ToEnum;
        }

        static ResetLazy<LazyState> lazy;
        static Func<HashSet<Enum>> getKeys;

        public static void Start(SchemaBuilder sb, Func<HashSet<Enum>> getKeys)
        {
            if (sb.NotDefined(typeof(MultiEnumLogic<T>).GetMethod("Start")))
            {
                sb.Include<T>();

                sb.Schema.Initializing[InitLevel.Level0SyncEntities] += () => lazy.Load();
                sb.Schema.Synchronizing += Schema_Synchronizing;
                sb.Schema.Generating += Schema_Generating;

                MultiEnumLogic<T>.getKeys = getKeys;
                lazy = sb.GlobalLazy(() => new LazyState(MultiEnumLogic<T>.getKeys),
                    new InvalidateWith(typeof(T)));
            }
        }
      
        static SqlPreCommand Schema_Generating()
        {
            Table table = Schema.Current.Table<T>();

            List<T> should = GenerateEntities(); 

            return should.Select(a => table.InsertSqlSync(a)).Combine(Spacing.Simple);
        }

        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            Table table = Schema.Current.Table<T>();

            List<T> current = Administrator.TryRetrieveAll<T>(replacements);
            List<T> should = GenerateEntities();

            return Synchronizer.SynchronizeScriptReplacing(replacements, typeof(T).Name,
                should.ToDictionary(s => s.Key),
                current.ToDictionary(c => c.Key),
                (k, s) => table.InsertSqlSync(s),
                (k, c) => table.DeleteSqlSync(c),
                (k, s, c) =>
                {
                    var originalName = c.Key;
                    c.Key = s.Key;
                    return table.UpdateSqlSync(c, comment: c.Key);
                }, Spacing.Double);
        }


        static List<T> GenerateEntities()
        {
            return  getKeys().Select(k => new T
            {
                Key = MultiEnumDN.UniqueKey(k),
            }).ToList();
        }

        internal static T ToEntity(Enum key)
        {
            AssertStarted();

            return lazy.Value.ToEntity.GetOrThrow(key);
        }

        static LazyState AssertStarted()
        {
            if (lazy == null)
                throw new InvalidOperationException("{0} has not been started. Someone should have called {0}.Start before".Formato(typeof(MultiEnumLogic<T>).TypeName()));

            return lazy.Value;
        }

        public static HashSet<Enum> Keys
        {
            get { return AssertStarted().Keys; }
        }

        public static T ToEntity(string keyName)
        {
            return ToEntity(ToEnum(keyName));
        }

        public static T TryToEntity(Enum key)
        {
            return AssertStarted().ToEntity.TryGetC(key);
        }

        public static T TryToEntity(string keyName)
        {
            Enum en = TryToEnum(keyName);

            if (en == null)
                return null;

            return TryToEntity(en); 
        }

        internal static Enum ToEnum(T entity)
        {
            return AssertStarted().ToEnum.GetOrThrow(entity);
        }

        public static Enum ToEnum(string keyName)
        {
            return AssertStarted().ToEnumUniqueKey.GetOrThrow(keyName);
        }

        public static Enum TryToEnum(string keyName)
        {
            return AssertStarted().ToEnumUniqueKey.TryGetC(keyName);
        }

        public static IEnumerable<T> AllEntities()
        {
            return AssertStarted().ToEntity.Values; 
        }

        public static IEnumerable<string> AllUniqueKeys()
        {
            return AssertStarted().ToEnumUniqueKey.Keys;
        }
    }
}
