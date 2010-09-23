using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Entities.Reflection;
using Signum.Engine.Properties;
using Signum.Utilities;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Signum.Engine
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

        public static Type ToType(this TypeDN type)
        {
            return DnToType[type];
        }

        public static TypeDN ToTypeDN(this Type type)
        {
            return TypeToDN[type];
        }

        internal static void Schema_Initializing(Schema sender)
        {
            List<TypeDN> types = Administrator.UnsafeRetrieveAll<TypeDN>();

            var dict = EnumerableExtensions.JoinStrict(
                types, sender.Tables.Keys, t => t.FullClassName, t => (Reflector.ExtractEnumProxy(t) ?? t).FullName,
                (typeDN, type) => new { typeDN, type },
                "caching types table from {0}".Formato(sender.Table(typeof(TypeDN)).Name)
                ).ToDictionary(a => a.type, a => a.typeDN);

            sender.IDsForType = dict.SelectDictionary(k => k, v => v.Id);
            sender.TablesForID = sender.IDsForType.ToDictionary(p => p.Value, p => sender.Table(p.Key));

            sender.TypeToDN = dict;
            sender.DnToType = dict.Inverse();
        }

        public static Dictionary<TypeDN, Type> TryDNToType(Replacements replacements)
        {
            return (from dn in Administrator.TryRetrieveAll<TypeDN>(replacements)
                    join t in Schema.Current.Tables.Keys on dn.FullClassName equals (Reflector.ExtractEnumProxy(t) ?? t).FullName
                    select new { dn, t }).ToDictionary(a => a.dn, a => a.t);
        }

        public static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            Table table = Schema.Current.Table<TypeDN>();

            Dictionary<string, TypeDN> should = GenerateSchemaTypes().ToDictionary(s => s.TableName);
            Dictionary<string, TypeDN> current = replacements.ApplyReplacements(
                Administrator.TryRetrieveAll<TypeDN>(replacements).ToDictionary(c => c.TableName), Replacements.KeyTables);

            return Synchronizer.SynchronizeScript(
                current,
                should,
                (tn, c) => table.DeleteSqlSync(c),
                (tn, s) => table.InsertSqlSync(s),
                (tn, c, s) =>
                {
                    c.FullClassName = s.FullClassName;
                    c.TableName = s.TableName;
                    c.FriendlyName = s.FriendlyName;
                    return table.UpdateSqlSync(c);
                }, Spacing.Double);
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
                         let type = Reflector.ExtractEnumProxy(tab.Type) ?? tab.Type
                         select new TypeDN
                         {
                             FullClassName = type.FullName,
                             TableName = tab.Name,
                             FriendlyName = type.NiceName()
                         }).ToList();
            return lista;
        }

        public static List<Lite<TypeDN>> TypesAssignableFrom(Type type)
        {
            return Schema.Current.TypeToDN.Where(a => type.IsAssignableFrom(a.Key)).Select(a => a.Value.ToLite()).ToList();
        }


        public static string TryParseLite(Type liteType, string liteKey, out Lite lite)
        {
            string error = Lite.TryParseLite(liteType, liteKey, TryGetType, out lite);
            if (error != null)
                return error;

            lite = Database.RetrieveLite(liteType, lite.RuntimeType, lite.Id);
            return null;
        }

        public static Lite ParseLite(Type liteType, string liteKey)
        {
            Lite lite = Lite.ParseLite(liteType, liteKey, TryGetType);

            return Database.RetrieveLite(liteType, lite.RuntimeType, lite.Id);
        }

        static Type GetType(string typeName)
        {
            return Schema.Current.TypeToDN.Keys.Where(t => t.Name == typeName).Single("Type {0} not found in the schema".Formato(typeName));
        }

        static Type TryGetType(string typeName)
        {
            return Schema.Current.TypeToDN.Keys.Where(t => t.Name == typeName).SingleOrDefault();
        }
    }
}
