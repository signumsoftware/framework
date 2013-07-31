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

namespace Signum.Engine.Basics
{
    public static class TypeLogic
    {
        public static Dictionary<Type, TypeDN> TypeToDN
        {
            get { return Schema.Current.TypeToDN; }
        }

        public static Dictionary<TypeDN, Type> DnToType
        {
            get { return Schema.Current.DnToType; }
        }

        public static Dictionary<string, Type> NameToType
        {
            get { return Schema.Current.NameToType; }
        }

        public static Dictionary<Type, string> TypeToName
        {
            get { return Schema.Current.TypeToName; }
        }

        public static Type ToType(this TypeDN type)
        {
            return DnToType.GetOrThrow(type);
        }

        public static TypeDN ToTypeDN(this Type type)
        {
            return TypeToDN.GetOrThrow(type);
        }

        internal static void Schema_Initializing()
        {
            Schema current = Schema.Current;

            var attributes = current.Tables.Keys.Select(t => KVP.Create(t, t.SingleAttributeInherit<EntityKindAttribute>())).ToList();

            var errors = attributes.Where(a => a.Value == null).ToString(a => "Type {0} does not have an EntityTypeAttribute".Formato(a.Key.Name), "\r\n");

            if (errors.HasText())
                throw new InvalidOperationException(errors);

            List<TypeDN> types = Database.RetrieveAll<TypeDN>();

            var dict = EnumerableExtensions.JoinStrict(
                types, current.Tables.Keys, t => t.FullClassName, t => (EnumEntity.Extract(t) ?? t).FullName,
                (typeDN, type) => new { typeDN, type },
                "caching {0}. Consider synchronize".Formato(current.Table(typeof(TypeDN)).Name)
                ).ToDictionary(a => a.type, a => a.typeDN);

            current.TypeToId = dict.SelectDictionary(k => k, v => v.Id);
            current.IdToType = current.TypeToId.ToDictionary(p => p.Value, p => current.Table(p.Key).Type);

            current.TypeToDN = dict;
            current.DnToType = dict.Inverse();


            Lite.SetTypeNameAndResolveType(TypeLogic.GetCleanName, TypeLogic.GetType);

            //current.TypeToName = current.Tables.SelectDictionary(k => k, v => v.CleanTypeName);
            //current.NameToType = current.TypeToName.Inverse("CleanTypeNames");
        }
        
        public static Dictionary<TypeDN, Type> TryDNToType(Replacements replacements)
        {
            return (from dn in Administrator.TryRetrieveAll<TypeDN>(replacements)
                    join t in Schema.Current.Tables.Keys on dn.FullClassName equals (EnumEntity.Extract(t) ?? t).FullName
                    select new { dn, t }).ToDictionary(a => a.dn, a => a.t);
        }

        public static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            Table table = Schema.Current.Table<TypeDN>();

            Dictionary<string, TypeDN> should = GenerateSchemaTypes().ToDictionary(s => s.TableName, "tableName in memory");

            Dictionary<string, TypeDN> current = replacements.ApplyReplacementsToOldCleaning(
                Administrator.TryRetrieveAll<TypeDN>(replacements).ToDictionary(c => c.TableName, "tableName in database"), Replacements.KeyTables);

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
            Table table = Schema.Current.Table<TypeDN>();

            return (from ei in GenerateSchemaTypes()
                    select table.InsertSqlSync(ei)).Combine(Spacing.Simple);
        }

        internal static List<TypeDN> GenerateSchemaTypes()
        {
            var lista = (from tab in Schema.Current.Tables.Values
                         let type = EnumEntity.Extract(tab.Type) ?? tab.Type
                         select new TypeDN
                         {
                             FullClassName = type.FullName,
                             TableName = tab.Name.Name,
                             CleanName = Reflector.CleanTypeName(type)
                         }).ToList();
            return lista;
        }

        public static Type GetType(string cleanName)
        {
            return Schema.Current.NameToType.GetOrThrow(cleanName, "Type {0} not found in the schema");
        }

        public static Type TryGetType(string cleanName)
        {
            return Schema.Current.NameToType.TryGetC(cleanName);
        }

        public static string GetCleanName(Type type)
        {
            return Schema.Current.TypeToName.GetOrThrow(type, "Type {0} not found in the schema");
        }

        public static string TryGetCleanName(Type type)
        {
            return Schema.Current.TypeToName.TryGetC(type);
        }
    }
}
