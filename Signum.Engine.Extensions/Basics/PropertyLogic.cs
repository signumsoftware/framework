using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Entities;
using Signum.Entities.Reflection;
using System.Reflection;
using Signum.Utilities;
using Signum.Engine.Maps;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Basics
{
    public static class PropertyLogic
    {
        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<PropertyDN>();

                sb.AddUniqueIndex<PropertyDN>(p => new { p.Path, p.Type }); 

                sb.Schema.Synchronizing += SyncronizeProperties;
            }
        }

        const string FieldsForKey = "Properties For:{0}";
        static SqlPreCommand SyncronizeProperties(Replacements replacements)
        {
            var current = Administrator.TryRetrieveAll<PropertyDN>(replacements).AgGroupToDictionary(a => a.Type.FullClassName, g => g.ToDictionary(f => f.Path, "PropertyDN in the database with path"));

            var should = TypeLogic.TryDNToType(replacements).SelectDictionary(dn => dn.FullClassName, (dn, t) => GenerateProperties(t, dn).ToDictionary(f => f.Path, "PropertyDN in the database with path"));

            Table table = Schema.Current.Table<PropertyDN>();

            return Synchronizer.SynchronizeScript(should, current,
                null,
                null,
                (tn, dicShould, dicCurr) =>
                    Synchronizer.SynchronizeScriptReplacing(replacements, FieldsForKey.Formato(tn),
                    dicShould,
                    dicCurr,
                    null,
                    (fn, c) => table.DeleteSqlSync(c),
                    (fn, s, c) =>
                    {
                        c.Path = s.Path;
                        return table.UpdateSqlSync(c);
                    }, Spacing.Simple),
                Spacing.Double);
        }

        public static List<PropertyDN> RetrieveOrGenerateProperties(TypeDN typeDN)
        {
            var retrieve = Database.Query<PropertyDN>().Where(f => f.Type == typeDN).ToDictionary(a => a.Path);
            var generate = GenerateProperties(TypeLogic.DnToType[typeDN], typeDN).ToDictionary(a => a.Path);

            return generate.Select(kvp => retrieve.TryGetC(kvp.Key).TryDoC(pi => pi.Route = kvp.Value.Route) ?? kvp.Value).ToList();
        }

        public static List<PropertyDN> GenerateProperties(Type type, TypeDN typeDN)
        {
            return PropertyRoute.GenerateRoutes(type).Select(pr =>
                new PropertyDN
                {
                    Route = pr,
                    Type = typeDN,
                    Path = pr.PropertyString()
                }).ToList();
        }

        public static PropertyRoute GetPropertyRoute(PropertyDN route)
        {
            return PropertyRoute.Parse(TypeLogic.DnToType[route.Type], route.Path);
        }

        public static PropertyDN GetEntity(PropertyRoute route)
        {
            TypeDN type = TypeLogic.TypeToDN[route.RootType];
            string path = route.PropertyString();
            return Database.Query<PropertyDN>().SingleOrDefaultEx(f => f.Type == type && f.Path == path).TryDoC(pi => pi.Route = route) ??
                 new PropertyDN
                 {
                     Route = route,
                     Type = type,
                     Path = path
                 };
        }
    }
}
