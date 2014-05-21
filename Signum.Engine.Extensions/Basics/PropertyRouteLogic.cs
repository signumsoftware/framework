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
using System.Linq.Expressions;

namespace Signum.Engine.Basics
{
    public static class PropertyRouteLogic
    {
        static Expression<Func<PropertyRouteDN, PropertyRoute, bool>> IsPropertyRouteExpression = 
            (prdn, pr) => prdn.RootType == pr.RootType.ToTypeDN() && prdn.Path == pr.PropertyString() ;
        public static bool IsPropertyRoute(this PropertyRouteDN prdn, PropertyRoute pr)
        {
            return IsPropertyRouteExpression.Evaluate(prdn, pr);
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<PropertyRouteDN>();

                sb.AddUniqueIndex<PropertyRouteDN>(p => new { p.Path, p.RootType }); 

                sb.Schema.Synchronizing += SyncronizeProperties;
            }
        }

        const string FieldsForKey = "Properties For:{0}";
        static SqlPreCommand SyncronizeProperties(Replacements replacements)
        {
            var current = Administrator.TryRetrieveAll<PropertyRouteDN>(replacements).AgGroupToDictionary(a => a.RootType.FullClassName, g => g.ToDictionary(f => f.Path, "PropertyDN in the database with path"));

            var should = TypeLogic.TryDNToType(replacements).SelectDictionary(dn => dn.FullClassName, (dn, t) => GenerateProperties(t, dn).ToDictionary(f => f.Path, "PropertyDN in the database with path"));

            Table table = Schema.Current.Table<PropertyRouteDN>();

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

        public static List<PropertyRouteDN> RetrieveOrGenerateProperties(TypeDN typeDN)
        {
            var retrieve = Database.Query<PropertyRouteDN>().Where(f => f.RootType == typeDN).ToDictionary(a => a.Path);
            var generate = GenerateProperties(TypeLogic.DnToType[typeDN], typeDN).ToDictionary(a => a.Path);

            return generate.Select(kvp => retrieve.TryGetC(kvp.Key).TryDoC(pi => pi.Route = kvp.Value.Route) ?? kvp.Value).ToList();
        }

        public static List<PropertyRouteDN> GenerateProperties(Type type, TypeDN typeDN)
        {
            return PropertyRoute.GenerateRoutes(type).Select(pr =>
                new PropertyRouteDN
                {
                    Route = pr,
                    RootType = typeDN,
                    Path = pr.PropertyString()
                }).ToList();
        }

        public static PropertyRoute ToPropertyRoute(this PropertyRouteDN route)
        {
            return PropertyRoute.Parse(TypeLogic.DnToType[route.RootType], route.Path);
        }

        public static PropertyRouteDN ToPropertyRouteDN(this PropertyRoute route)
        {
            TypeDN type = TypeLogic.TypeToDN[route.RootType];
            string path = route.PropertyString();
            return Database.Query<PropertyRouteDN>().SingleOrDefaultEx(f => f.RootType == type && f.Path == path).TryDoC(pi => pi.Route = route) ??
                 new PropertyRouteDN
                 {
                     Route = route,
                     RootType = type,
                     Path = path
                 };
        }
    }
}
