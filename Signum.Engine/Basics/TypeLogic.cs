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

        public static Dictionary<TypeEntity, Type> EntityToType
        {
            get { return Schema.Current.typeCachesLazy.Value.EntityToType; }
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
                    new InvalidateWith(typeof(TypeEntity)),
                    Schema.Current.InvalidateMetadata);

                sb.Include<TypeEntity>()
                    .WithQuery(dqm, () => t => new
                    {
                        Entity = t,
                        t.Id,
                        t.TableName,
                        t.CleanName,
                        t.ClassName,
                        t.Namespace,
                    });
                
                TypeEntity.SetTypeEntityCallbacks(
                    t => TypeToEntity.GetOrThrow(t),
                    t => EntityToType.GetOrThrow(t));
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
            var schema = Schema.Current;

            Dictionary<string, TypeEntity> should = GenerateSchemaTypes().ToDictionaryEx(s => s.TableName, "tableName in memory");

            var currentList = Administrator.TryRetrieveAll<TypeEntity>(replacements);

            { //External entities are nt asked in SchemaSynchronizer
                replacements.AskForReplacements(
                    currentList.Where(t => schema.IsExternalDatabase(ObjectName.Parse(t.TableName).Schema.Database)).Select(a => a.TableName).ToHashSet(),
                    should.Values.Where(t => schema.IsExternalDatabase(ObjectName.Parse(t.TableName).Schema.Database)).Select(a => a.TableName).ToHashSet(),
                    Replacements.KeyTables);
            }

            Dictionary<string, TypeEntity> current = ApplyReplacementsToOld(replacements,
                currentList.ToDictionaryEx(c => c.TableName, "tableName in database"), Replacements.KeyTables);

            { //Temporal solution until applications are updated
                var repeated =
                    should.Keys.Select(k => ObjectName.Parse(k)).GroupBy(a => a.Name).Where(a => a.Count() > 1).Select(a => a.Key).Concat(
                    current.Keys.Select(k => ObjectName.Parse(k)).GroupBy(a => a.Name).Where(a => a.Count() > 1).Select(a => a.Key)).ToList();

                Func<string, string> simplify = tn =>
                {
                    ObjectName name = ObjectName.Parse(tn);
                    return repeated.Contains(name.Name) ? name.ToString() : name.Name;
                };

                should = should.SelectDictionary(simplify, v => v);
                current = current.SelectDictionary(simplify, v => v);
            }

            Table table = schema.Table<TypeEntity>();

            using (replacements.WithReplacedDatabaseName())
                return Synchronizer.SynchronizeScript(
                    Spacing.Double,
                    should,
                    current,
                    createNew: (tn, s) => table.InsertSqlSync(s),
                    removeOld: (tn, c) => table.DeleteSqlSync(c, t => t.CleanName == c.CleanName),
                    mergeBoth: (tn, s, c) =>
                    {
                        var originalCleanName = c.CleanName;
                        var originalFullName = c.FullClassName;

                        if (c.TableName != s.TableName)
                        {
                            var pc = ObjectName.Parse(c.TableName);
                            var ps = ObjectName.Parse(s.TableName);

                            if (!EqualsIgnoringDatabasePrefix(pc, ps))
                            {
                                c.TableName = ps.ToString();
                            }
                        }
                          
                        c.CleanName = s.CleanName;
                        c.Namespace = s.Namespace;
                        c.ClassName = s.ClassName;
                        return table.UpdateSqlSync(c, t => t.CleanName == originalCleanName, comment: originalFullName);
                    });
        }

        static bool EqualsIgnoringDatabasePrefix(ObjectName pc, ObjectName ps) => 
            ps.Name == pc.Name &&
            pc.Schema.Name  == ps.Schema.Name &&
            Suffix(pc.Schema.Database?.Name) == Suffix(ps.Schema.Database?.Name);

        static string Suffix(string name) => name.TryAfterLast("_") ?? name;

        static Dictionary<string, O> ApplyReplacementsToOld<O>(this Replacements replacements, Dictionary<string, O> oldDictionary, string replacementsKey)
        {
            if (!replacements.ContainsKey(replacementsKey))
                return oldDictionary;

            Dictionary<string, string> dic = replacements[replacementsKey];

            return oldDictionary.SelectDictionary(a => dic.TryGetC(a) ?? a, v => v);
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
                            TableName = tab.Name.ToString(),
                            CleanName = Reflector.CleanTypeName(type),
                            Namespace = type.Namespace,
                            ClassName = type.Name,
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
        public readonly Dictionary<TypeEntity, Type> EntityToType;
        public readonly Dictionary<PrimaryKey, Type> IdToType;
        public readonly Dictionary<Type, PrimaryKey> TypeToId;

        public TypeCaches(Schema current)
        {
            TypeToEntity = EnumerableExtensions.JoinRelaxed(
                    Database.RetrieveAll<TypeEntity>(),
                    current.Tables.Keys,
                    t => t.FullClassName,
                    t => (EnumEntity.Extract(t) ?? t).FullName,
                    (typeEntity, type) => new { typeEntity, type },
                     "caching {0}".FormatWith(current.Table(typeof(TypeEntity)).Name)
                    ).ToDictionary(a => a.type, a => a.typeEntity);

            EntityToType = TypeToEntity.Inverse();

            TypeToId = TypeToEntity.SelectDictionary(k => k, v => v.Id);
            IdToType = TypeToId.Inverse();

        }
    }
}
