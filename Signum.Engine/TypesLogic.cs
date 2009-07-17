using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Entities.Reflection;
using Signum.Engine.Properties;
using Signum.Utilities;

namespace Signum.Engine
{
    public static class TypeLogic
    {
        static Dictionary<Type, TypeDN> typeToDN; 
        public static Dictionary<Type, TypeDN> TypeToDN 
        {
            get { return typeToDN.ThrowIfNullC(Resources.TypeDNTableNotCached); }
            set { typeToDN = value; }
        }

        static Dictionary<TypeDN, Type> dnToType; 
        public static Dictionary<TypeDN, Type> DnToType
        {
            get { return dnToType.ThrowIfNullC(Resources.TypeDNTableNotCached); }
            set { dnToType = value;  }
        }

        public static void Start(SchemaBuilder sb, bool fillStaticCache)
        {
            if (sb.NotDefined<TypeDN>())
            {
                sb.Include<TypeDN>();

                sb.Schema.Synchronizing += SynchronizeTypesScript;
                sb.Schema.Generating += InsertTypesScript;
                sb.Schema.Initializing += s => Cache(s, fillStaticCache);
             
            }
        }

        static void Cache(Schema sender, bool fillStaticCache)
        {
            List<TypeDN> types = Database.RetrieveAll<TypeDN>();

            var dict = EnumerableExtensions.JoinStrict(
                types, sender.Tables.Keys, t => t.ClassName, t => (Reflector.ExtractEnumProxy(t) ?? t).Name,
                (typeDN, type) => new { typeDN, type },
                Resources.CachingTypesTableFrom0.Formato(sender.Table(typeof(TypeDN)).Name)
                ).ToDictionary(a => a.type, a => a.typeDN);

            sender.IDsForType = dict.SelectDictionary(k=>k, v=>v.Id);
            sender.TablesForID = sender.IDsForType.ToDictionary(p => p.Value, p => sender.Table(p.Key));

            if (fillStaticCache)
            {
                typeToDN = dict;
                dnToType = typeToDN.Inverse();
            }
        }

        public static Dictionary<TypeDN, Type> TryDNToType(Replacements replacements)
        {
            return (from dn in Administrator.TryRetrieveAll<TypeDN>(replacements)
                    join t in Schema.Current.Tables.Keys on dn.ClassName equals (Reflector.ExtractEnumProxy(t) ?? t).Name
                    select new { dn, t }).ToDictionary(a => a.dn, a => a.t);
        }

        public static SqlPreCommand SynchronizeTypesScript(Replacements replacements)
        {
            Table table = Schema.Current.Table<TypeDN>();

            Dictionary<string, TypeDN> should = GenerateSchemaTypes().ToDictionary(s => s.TableName);
            Dictionary<string, TypeDN> current = replacements.ApplyReplacements(
                Administrator.TryRetrieveAll<TypeDN>(replacements).ToDictionary(c => c.TableName), Replacements.KeyTables);

            return Synchronizer.SyncronizeCommands(
                current,
                should,
                (tn, c) => table.DeleteSqlSync(c),
                (tn, s) => table.InsertSqlSync(s),
                (tn, c, s) =>
                {
                    c.ClassName = s.ClassName;
                    c.TableName = s.TableName;
                    c.FriendlyName = s.FriendlyName;
                    return table.UpdateSqlSync(c);
                }, Spacing.Double);
        }

        public static SqlPreCommand InsertTypesScript()
        {
            Table table = Schema.Current.Table<TypeDN>();

            return (from ei in GenerateSchemaTypes()
                    select table.InsertSqlSync(ei)).Combine(Spacing.Simple);
        }

        public static List<TypeDN> GenerateSchemaTypes()
        {
            var lista = (from tab in Schema.Current.Tables.Values
                         let type = Reflector.ExtractEnumProxy(tab.Type) ?? tab.Type
                         select new TypeDN
                         {
                             ClassName = type.Name,
                             TableName = tab.Name,
                             FriendlyName = type.NiceName()
                         }).ToList();
            return lista;
        }
    }
}
