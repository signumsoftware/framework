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
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Basics
{
    public static class PropertyRouteLogic
    {
        static Expression<Func<PropertyRouteEntity, PropertyRoute, bool>> IsPropertyRouteExpression = 
            (prdn, pr) => prdn.RootType == pr.RootType.ToTypeEntity() && prdn.Path == pr.PropertyString() ;
        [ExpressionField]
        public static bool IsPropertyRoute(this PropertyRouteEntity prdn, PropertyRoute pr)
        {
            return IsPropertyRouteExpression.Evaluate(prdn, pr);
        }

        public static ResetLazy<Dictionary<TypeEntity, Dictionary<string, PropertyRouteEntity>>> Properties; 

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<PropertyRouteEntity>()
                    .WithUniqueIndex(p => new { p.Path, p.RootType }); 

                sb.Schema.Synchronizing += SynchronizeProperties;

                Properties = sb.GlobalLazy(() => Database.Query<PropertyRouteEntity>().AgGroupToDictionary(a => a.RootType, gr => gr.ToDictionary(a => a.Path)),
                    new InvalidateWith(typeof(PropertyRouteEntity)), Schema.Current.InvalidateMetadata);

                PropertyRouteEntity.ToPropertyRouteFunc = ToPropertyRouteImplementation;
            }
        }

        public static PropertyRouteEntity TryGetPropertyRouteEntity(TypeEntity entity, string path)
        {
            return Properties.Value.TryGetC(entity)?.TryGetC(path);
        }

        public const string PropertiesFor = "Properties For:{0}";
        static SqlPreCommand SynchronizeProperties(Replacements rep)
        {
            var current = Administrator.TryRetrieveAll<PropertyRouteEntity>(rep).AgGroupToDictionary(a => a.RootType.FullClassName, g => g.ToDictionaryEx(f => f.Path, "PropertyEntity in the database with path"));

            var should = TypeLogic.TryEntityToType(rep).SelectDictionary(dn => dn.FullClassName, (dn, t) => GenerateProperties(t, dn).ToDictionaryEx(f => f.Path, "PropertyEntity in the database with path"));

            Table table = Schema.Current.Table<PropertyRouteEntity>();

            using (rep.WithReplacedDatabaseName())
                return Synchronizer.SynchronizeScript(Spacing.Double, should, current,
                    createNew: null,
                    removeOld: null,
                    mergeBoth: (oldFullName, dicShould, dicCurr) =>
                        Synchronizer.SynchronizeScriptReplacing(rep, PropertiesFor.FormatWith(oldFullName), Spacing.Simple,
                        dicShould,
                        dicCurr,
                        createNew: null,
                        removeOld: (path, c) => table.DeleteSqlSync(c, p => p.RootType.FullClassName == oldFullName && p.Path == c.Path),
                        mergeBoth: (path, s, c) =>
                        {
                            var originalPathName = c.Path;
                            c.Path = s.Path;
                            return table.UpdateSqlSync(c, p => p.RootType.FullClassName == oldFullName && p.Path == originalPathName);
                        })
                    );
        }

        public static List<PropertyRouteEntity> RetrieveOrGenerateProperties(TypeEntity typeEntity)
        {
            var retrieve = Database.Query<PropertyRouteEntity>().Where(f => f.RootType == typeEntity).ToDictionary(a => a.Path);
            var generate = GenerateProperties(TypeLogic.EntityToType[typeEntity], typeEntity).ToDictionary(a => a.Path);

            return generate.Select(kvp => retrieve.TryGetC(kvp.Key)?.Do(pi => pi.Route = kvp.Value.Route) ?? kvp.Value).ToList();
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

        static PropertyRoute ToPropertyRouteImplementation(PropertyRouteEntity route)
        {
            return PropertyRoute.Parse(TypeLogic.EntityToType[route.RootType], route.Path);
        }

        public static PropertyRouteEntity ToPropertyRouteEntity(this PropertyRoute route)
        {
            TypeEntity type = TypeLogic.TypeToEntity[route.RootType];
            string path = route.PropertyString();
            return Database.Query<PropertyRouteEntity>().SingleOrDefaultEx(f => f.RootType == type && f.Path == path)?.Do(pi => pi.Route = route) ??
                 new PropertyRouteEntity
                 {
                     Route = route,
                     RootType = type,
                     Path = path
                 };
        }
    }
}
