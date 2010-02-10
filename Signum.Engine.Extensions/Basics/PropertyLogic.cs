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
                TypeLogic.Start(sb);
                sb.Include<PropertyDN>();

                sb.Schema.Synchronizing += SyncronizeProperties;
            }
        }

        const string FieldsForKey = "Properties For:{0}";
        static SqlPreCommand SyncronizeProperties(Replacements replacements)
        {
            var current = Administrator.TryRetrieveAll<PropertyDN>(replacements).AgGroupToDictionary(a => a.Type.FullClassName, g => g.ToDictionary(f => f.Path));

            var should = TypeLogic.TryDNToType(replacements).SelectDictionary(dn => dn.FullClassName, (dn, t) => GenerateProperties(t, dn).ToDictionary(f => f.Path));

            Table table = Schema.Current.Table<PropertyDN>();

            return Synchronizer.SynchronizeScript(
                current, should,
                null, null,
                (tn, dicCurr, dicShould) =>
                    Synchronizer.SynchronizeReplacing(replacements, FieldsForKey.Formato(tn),
                    dicCurr,
                    dicShould,
                    (fn, c) => table.DeleteSqlSync(c),
                    null,
                    (fn, c, s) =>
                    {
                        c.Path = s.Path;
                        return table.UpdateSqlSync(c);
                    },
                    Spacing.Simple), Spacing.Double);
        }

        public static List<PropertyDN> RetrieveOrGenerateProperty(TypeDN typeDN)
        {
            var retrieve = Database.Query<PropertyDN>().Where(f => f.Type == typeDN).ToDictionary(a => a.Path);
            var generate = GenerateProperties(TypeLogic.DnToType[typeDN], typeDN).ToDictionary(a => a.Path);

            return generate.Select(kvp => retrieve.TryGetC(kvp.Key).TryDoC(pi => pi.PropertyPath = kvp.Value.PropertyPath) ?? kvp.Value).ToList();
        }

        public static List<PropertyDN> GenerateProperties(Type type, TypeDN typeDN)
        {
            return PropertyRoute.GenerateRoutes(type).Select(pp =>
                new PropertyDN
                {
                    PropertyPath = pp,
                    Type = typeDN,
                    Path = pp.PropertyString()
                }).ToList();
        }
    }
}
