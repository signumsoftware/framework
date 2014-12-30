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
        static Expression<Func<PropertyRouteEntity, PropertyRoute, bool>> IsPropertyRouteExpression = 
            (prdn, pr) => prdn.RootType == pr.RootType.ToTypeEntity() && prdn.Path == pr.PropertyString() ;
        public static bool IsPropertyRoute(this PropertyRouteEntity prdn, PropertyRoute pr)
        {
            return IsPropertyRouteExpression.Evaluate(prdn, pr);
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<PropertyRouteEntity>();

                sb.AddUniqueIndex<PropertyRouteEntity>(p => new { p.Path, p.RootType }); 

                sb.Schema.Synchronizing += SyncronizeProperties;
            }
        }

        public const string PropertiesFor = "Properties For:{0}";
        static SqlPreCommand SyncronizeProperties(Replacements rep)
        {
            var current = Administrator.TryRetrieveAll<PropertyRouteEntity>(rep).AgGroupToDictionary(a => a.RootType.FullClassName, g => g.ToDictionary(f => f.Path, "PropertyEntity in the database with path"));

            var should = TypeLogic.TryDNToType(rep).SelectDictionary(dn => dn.FullClassName, (dn, t) => GenerateProperties(t, dn).ToDictionary(f => f.Path, "PropertyEntity in the database with path"));

            Table table = Schema.Current.Table<PropertyRouteEntity>();

            using (rep.WithReplacedDatabaseName())
                return Synchronizer.SynchronizeScript(should, current,
                    null,
                    null,
                    (fullName, dicShould, dicCurr) =>
                        Synchronizer.SynchronizeScriptReplacing(rep, PropertiesFor.FormatWith(fullName),
                        dicShould,
                        dicCurr,
                        null,
                        (path, c) => table.DeleteSqlSync(c),
                        (path, s, c) =>
                        {
                            c.Path = s.Path;
                            return table.UpdateSqlSync(c);
                        }, Spacing.Simple),
                    Spacing.Double);
        }

        public static List<PropertyRouteEntity> RetrieveOrGenerateProperties(TypeEntity typeEntity)
        {
            var retrieve = Database.Query<PropertyRouteEntity>().Where(f => f.RootType == typeEntity).ToDictionary(a => a.Path);
            var generate = GenerateProperties(TypeLogic.DnToType[typeEntity], typeEntity).ToDictionary(a => a.Path);

            return generate.Select(kvp => retrieve.TryGetC(kvp.Key).TryDo(pi => pi.Route = kvp.Value.Route) ?? kvp.Value).ToList();
        }

        public static List<PropertyRouteEntity> GenerateProperties(Type type, TypeEntity typeEntity)
        {
            return PropertyRoute.GenerateRoutes(type).Select(pr =>
                new PropertyRouteEntity
                {
                    Route = pr,
                    RootType = typeEntity,
                    Path = pr.PropertyString()
                }).ToList();
        }

        public static PropertyRoute ToPropertyRoute(this PropertyRouteEntity route)
        {
            return PropertyRoute.Parse(TypeLogic.DnToType[route.RootType], route.Path);
        }

        public static PropertyRouteEntity ToPropertyRouteEntity(this PropertyRoute route)
        {
            TypeEntity type = TypeLogic.TypeToEntity[route.RootType];
            string path = route.PropertyString();
            return Database.Query<PropertyRouteEntity>().SingleOrDefaultEx(f => f.RootType == type && f.Path == path).TryDo(pi => pi.Route = route) ??
                 new PropertyRouteEntity
                 {
                     Route = route,
                     RootType = type,
                     Path = path
                 };
        }
    }
}
