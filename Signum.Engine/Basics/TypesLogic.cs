using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Entities.Reflection;
using Signum.Utilities;
using System.Reflection;
using System.Text.RegularExpressions;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Basics
{
   
    public static class TypeLogic
    {
        public static Dictionary<PrimaryKey, Type> IdToType
        {
            get { return Schema.Current.typeCachesLazy.Value.IdToType; }
        }

        public static Dictionary<Type, PrimaryKey> TypeToId
        {
            get { return Schema.Current.typeCachesLazy.Value.TypeToId; }
        }

        public static Dictionary<Type, TypeEntity> TypeToEntity
        {
            get { return Schema.Current.typeCachesLazy.Value.TypeToEntity; }
        }

        public static Dictionary<TypeEntity, Type> DnToType
        {
            get { return Schema.Current.typeCachesLazy.Value.DnToType; }
        }

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Schema current = Schema.Current;

                current.SchemaCompleted += () =>
                {
                    var attributes = current.Tables.Keys.Select(t => KVP.Create(t, t.GetCustomAttribute<EntityKindAttribute>(true))).ToList();

                    var errors = attributes.Where(a => a.Value == null).ToString(a => "Type {0} does not have an EntityTypeAttribute".FormatWith(a.Key.Name), "\r\n");

                    if (errors.HasText())
                        throw new InvalidOperationException(errors);
                };

                current.Initializing += () =>
                {
                    current.typeCachesLazy.Load();
                };

                current.typeCachesLazy = sb.GlobalLazy(() => new TypeCaches(current), 
                    new InvalidateWith(typeof(TypeEntity)));

                dqm.RegisterQuery(typeof(TypeEntity), () =>
                    from t in Database.Query<TypeEntity>()
                    select new
                    {
                        Entity = t,
                        t.Id,
                        t.TableName,
                        t.CleanName,
                        t.FullClassName,
                    });

                TypeEntity.SetTypeDNCallbacks(
                    t => TypeToEntity.GetOrThrow(t),
                    t => DnToType.GetOrThrow(t));
            }
        }

        public static Dictionary<TypeEntity, Type> TryEntityToType(Replacements replacements)
        {
            return (from dn in Administrator.TryRetrieveAll<TypeEntity>(replacements)
                    join t in Schema.Current.Tables.Keys on dn.FullClassName equals (EnumEntity.Extract(t) ?? t).FullName
                    select new { dn, t }).ToDictionary(a => a.dn, a => a.t);
        }

        public static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            Table table = Schema.Current.Table<TypeEntity>();

            Dictionary<string, TypeEntity> should = GenerateSchemaTypes().ToDictionary(s => s.TableName, "tableName in memory");

            Dictionary<string, TypeEntity> current = replacements.ApplyReplacementsToOldCleaning(
                Administrator.TryRetrieveAll<TypeEntity>(replacements).ToDictionary(c => c.TableName, "tableName in database"), Replacements.KeyTables);

            using (replacements.WithReplacedDatabaseName())
                return Synchronizer.SynchronizeScript(
                    should,
                    current,
                    (tn, s) => table.InsertSqlSync(s),
                    (tn, c) => table.DeleteSqlSync(c),
                    (tn, s, c) =>
                    {
                        var originalName = c.FullClassName;

                        c.FullClassName = s.FullClassName;
                        c.TableName = s.TableName;
                        c.CleanName = s.CleanName;
                        return table.UpdateSqlSync(c, comment: originalName);
                    }, Spacing.Double);
        }

        static Dictionary<string, O> ApplyReplacementsToOldCleaning<O>(this Replacements replacements, Dictionary<string, O> oldDictionary, string replacementsKey)
        {
            if (!replacements.ContainsKey(replacementsKey))
                return oldDictionary;

            Dictionary<string, string> dic = replacements[replacementsKey];

            var cleanDic = dic.SelectDictionary(n => ObjectName.Parse(n).Name, n => ObjectName.Parse(n).Name);

            return oldDictionary.SelectDictionary(a => cleanDic.TryGetC(a) ?? a, v => v);
        }

        internal static SqlPreCommand Schema_Generating()
        {
            Table table = Schema.Current.Table<TypeEntity>();

            return GenerateSchemaTypes()
                .Select((e, i) => table.InsertSqlSync(e, suffix: i.ToString()))
                .Combine(Spacing.Simple)
                .PlainSqlCommand();
        }

        internal static List<TypeEntity> GenerateSchemaTypes()
        {
            var list = (from tab in Schema.Current.Tables.Values
                         let type = EnumEntity.Extract(tab.Type) ?? tab.Type
                         select new TypeEntity
                         {
                             FullClassName = type.FullName,
                             TableName = tab.Name.Name,
                             CleanName = Reflector.CleanTypeName(type)
                         }).ToList();
            return list;
        }

        public static Dictionary<string, Type> NameToType
        {
            get { return Schema.Current.NameToType; }
        }

        public static Dictionary<Type, string> TypeToName
        {
            get { return Schema.Current.TypeToName; }
        }

        public static Type GetType(string cleanName)
        {
            return NameToType.GetOrThrow(cleanName, "Type {0} not found in the schema");
        }

        public static Type TryGetType(string cleanName)
        {
            return NameToType.TryGetC(cleanName);
        }

        public static string GetCleanName(Type type)
        {
            return TypeToName.GetOrThrow(type, "Type {0} not found in the schema");
        }

        public static string TryGetCleanName(Type type)
        {
            return TypeToName.TryGetC(type);
        }
    }

    internal class TypeCaches
    {
        public readonly Dictionary<Type, TypeEntity> TypeToEntity;
        public readonly Dictionary<TypeEntity, Type> DnToType;
        public readonly Dictionary<PrimaryKey, Type> IdToType;
        public readonly Dictionary<Type, PrimaryKey> TypeToId;

        public TypeCaches(Schema current)
        {
            TypeToEntity = EnumerableExtensions.JoinStrict(
                    Database.RetrieveAll<TypeEntity>(),
                    current.Tables.Keys,
                    t => t.FullClassName,
                    t => (EnumEntity.Extract(t) ?? t).FullName,
                    (typeEntity, type) => new { typeEntity, type },
                     "caching {0}. Consider synchronize".FormatWith(current.Table(typeof(TypeEntity)).Name)
                    ).ToDictionary(a => a.type, a => a.typeEntity);

            DnToType = TypeToEntity.Inverse();

            TypeToId = TypeToEntity.SelectDictionary(k => k, v => v.Id);
            IdToType = TypeToId.Inverse();

        }
    }
}
