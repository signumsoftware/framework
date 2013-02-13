using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Engine.Maps;
using Signum.Utilities;
using System.Reflection;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Basics
{
    public static class MultiEnumExtensions
    {
        public static T ToEntity<T>(this Enum key) where T:MultiEnumDN, new()
        {
            return MultiEnumLogic<T>.ToEntity(key);
        }
    }

    public static class MultiEnumLogic<T>
        where T:MultiEnumDN, new()
    {
        public static HashSet<Enum> Keys { get; set; }
        static Dictionary<Enum, T> toEntity;
        static Dictionary<string, Enum> toEnum;
        static Func<HashSet<Enum>> getKeys;

        public static void Start(SchemaBuilder sb, Func<HashSet<Enum>> getKeys)
        {
            if (sb.NotDefined(typeof(MultiEnumLogic<T>).GetMethod("Start")))
            {
                sb.Include<T>(); 

                MultiEnumLogic<T>.getKeys = getKeys;

                sb.Schema.Initializing[InitLevel.Level0SyncEntities] += Schema_Initializing;
                sb.Schema.Synchronizing += Schema_Synchronizing;
                sb.Schema.Generating += Schema_Generating;
            }
        }

        static void Schema_Initializing()
        {
            using (new EntityCache(EntityCacheType.ForceNewSealed))
            {
                Keys = getKeys();

                toEntity = EnumerableExtensions.JoinStrict(
                     Database.RetrieveAll<T>(),
                     Keys,
                     a => a.Key,
                     k => MultiEnumDN.UniqueKey(k),
                     (a, k) => new { a, k }, "loading {0}".Formato(typeof(T).Name)).ToDictionary(p => p.k, p => p.a);

                toEnum = toEntity.Keys.ToDictionary(k => MultiEnumDN.UniqueKey(k));
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
                    var originalName = c.Name;
                    c.Name = s.Name;
                    c.Key = s.Key;
                    return table.UpdateSqlSync(c, originalName);
                }, Spacing.Double);
        }


        static List<T> GenerateEntities()
        {
            return getKeys().Select(k => new T
            {
                Key = MultiEnumDN.UniqueKey(k),
                Name = k.ToString(),
            }).ToList();
        }

        public static T ToEntity(Enum key)
        {
            AssertInitialized();

            return toEntity.GetOrThrow(key);
        }

        private static void AssertInitialized()
        {
            if (Keys == null)
                throw new InvalidOperationException("{0} is not initialized. Consider calling Schema.InitializeUntil(InitLevel.Level0SyncEntities)".Formato(typeof(MultiEnumLogic<T>).TypeName()));
        }

        public static T ToEntity(string keyName)
        {
            return ToEntity(ToEnum(keyName));
        }

        public static T TryToEntity(Enum key)
        {
            AssertInitialized();

            return toEntity.TryGetC(key);
        }

        public static T TryToEntity(string keyName)
        {
            Enum en = TryToEnum(keyName);

            if (en == null)
                return null;

            return TryToEntity(en); 
        }

        public static Enum ToEnum(T entity)
        {
            return ToEnum(entity.Key);
        }

        public static Enum ToEnum(string keyName)
        {
            AssertInitialized();

            return toEnum.GetOrThrow(keyName);
        }

        public static Enum TryToEnum(string keyName)
        {
            AssertInitialized();

            return toEnum.TryGetC(keyName);
        }

        public static IEnumerable<T> AllEntities()
        {
            AssertInitialized();

            return toEntity.Values; 
        }

        public static IEnumerable<string> AllUniqueKeys()
        {
            AssertInitialized();

            return toEnum.Keys;
        }
    }
}
