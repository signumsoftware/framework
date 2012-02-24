using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Engine.Maps;
using Signum.Utilities;
using Signum.Engine.Authorization;
using System.Reflection;
using Signum.Engine.Extensions.Properties;

namespace Signum.Engine.Basics
{
    public static class EnumLogic<T>
        where T:EnumDN, new()
    {
        public static HashSet<Enum> Keys { get; set; }
        static Dictionary<Enum, T> toEntity;
        static Dictionary<string, Enum> toEnum;
        static Func<HashSet<Enum>> getKeys;

        public static void Start(SchemaBuilder sb, Func<HashSet<Enum>> getKeys)
        {
            if (sb.NotDefined(typeof(EnumLogic<T>).GetMethod("Start")))
            {
                sb.Include<T>(); 

                EnumLogic<T>.getKeys = getKeys;

                sb.Schema.Initializing[InitLevel.Level0SyncEntities] += Schema_Initializing;
                sb.Schema.Synchronizing += Schema_Synchronizing;
                sb.Schema.Generating += Schema_Generating;
            }
        }

        static void Schema_Initializing()
        {
            using (new EntityCache(true))
            {
                Keys = getKeys();

                toEntity = EnumerableExtensions.JoinStrict(
                     Database.RetrieveAll<T>(),
                     Keys,
                     a => a.Key,
                     k => EnumDN.UniqueKey(k),
                     (a, k) => new { a, k }, "loading {0}".Formato(typeof(T).Name)).ToDictionary(p => p.k, p => p.a);

                toEnum = toEntity.Keys.ToDictionary(k => EnumDN.UniqueKey(k));
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

            return Synchronizer.SynchronizeReplacing(replacements, typeof(T).Name,
                current.ToDictionary(c => c.Key),
                should.ToDictionary(s => s.Key),
                (k, c) => table.DeleteSqlSync(c),
                (k, s) => table.InsertSqlSync(s),
                (k, c, s) =>
                {
                    c.Name = s.Name;
                    c.Key = s.Key;
                    return table.UpdateSqlSync(c);
                }, Spacing.Double);
        }

    

        static List<T> GenerateEntities()
        {
            return getKeys().Select(k => new T
            {
                Key = EnumDN.UniqueKey(k),
                Name = k.NiceToString(),
            }).ToList();
        }

        public static T ToEntity(Enum key)
        {
            return toEntity.GetOrThrow(key);
        }

        public static T ToEntity(string keyName)
        {
            return ToEntity(ToEnum(keyName));
        }

        public static T TryToEntity(Enum key)
        {
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
            return toEnum.GetOrThrow(keyName);
        }

        public static Enum TryToEnum(string keyName)
        {
            return toEnum.TryGetC(keyName);
        }

        internal static IEnumerable<T> AllEntities()
        {
            return toEntity.Values; 
        }
    }
}
