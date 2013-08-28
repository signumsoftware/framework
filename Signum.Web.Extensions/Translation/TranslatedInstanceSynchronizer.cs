using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using Signum.Entities;
using Signum.Utilities;
using Signum.Engine.Translation;
using Signum.Web.Translation;
using Signum.Entities.Translation;

namespace Signum.Web.Translation2
{
    public static class TranslationInstanceSynchronizer
    {
        public static TypeInstancesChanges GetAssemblyChanges(ITranslator translator, Type type, CultureInfo targetCulture)
        {

            CultureInfo masterCulture = new CultureInfo(TranslatedInstanceLogic.DefaultCulture);

            Dictionary<CultureInfo, Dictionary<LocalizedInstanceKey, TranslatedInstanceDN>> support = TranslatedInstanceLogic.TranslationsForType(type);

            Dictionary<LocalizedInstanceKey, TranslatedInstanceDN> target = support.TryGetC(targetCulture);

            if (target != null)
                support.Remove(targetCulture);

            var instances = TranslatedInstanceLogic.FromEntities(type).GroupBy(a=>a.Key.Instance).Select(gr =>
            {
                var routeConflicts = (from kvp in gr
                                      let t = target.TryGetC(kvp.Key)
                                      where kvp.Value.HasText() && (t == null || t.OriginalText != kvp.Value)
                                      select KVP.Create(kvp.Key, kvp.Value)).ToDictionary();

                var result = (from rc in routeConflicts
                              from dic in support
                              let trans = dic.Value.TryGetC(rc.Key)
                              where trans != null
                              select new
                              {
                                  rc.Key.Route,
                                  Culture = dic.Key,
                                  Conflict = new PropertyRouteConflict { Original = trans.TranslatedText, AutomaticTranslation = null }
                              }).Concat(gr.Select(rc => new
                              {
                                  rc.Key.Route,
                                  Culture = masterCulture,
                                  Conflict = new PropertyRouteConflict { Original = rc.Value, AutomaticTranslation = null }
                              })).AgGroupToDictionary(a => a.Route, g => g.ToDictionary(a => a.Culture, a => a.Conflict));
                
                return new InstanceChanges
                {
                    Instance = gr.Key,
                    RouteConflicts = result
                };

            }).NotNull().ToList();


            List<IGrouping<CultureInfo, PropertyRouteConflict>> memberGroups = (from t in instances
                                                                                from rcKVP in t.RouteConflicts
                                                                                from rc in rcKVP.Value
                                                                                select rc).GroupBy(a => a.Key, a => a.Value).ToList();

            foreach (IGrouping<CultureInfo, PropertyRouteConflict> gr in memberGroups)
            {
                var result = translator.TranslateBatch(gr.Select(a => a.Original).ToList(), gr.Key.Name, targetCulture.Name);

                gr.ZipForeach(result, (sp, translated) => sp.AutomaticTranslation = translated);
            }

            return new TypeInstancesChanges
            {
                Type = type,
                Instances = instances
            };
        }
    }

    public class TypeInstancesChanges
    {
        public Type Type { get; set; }

        public List<InstanceChanges> Instances { get; set; }

        public override string ToString()
        {
            return "Changes for instances of type {0}".Formato(Type.NiceName());
        }
    }

    public class InstanceChanges
    {
        public Lite<IdentifiableEntity> Instance { get; set; }

        public Dictionary<PropertyRoute, Dictionary<CultureInfo, PropertyRouteConflict>> RouteConflicts { get; set; }

        public override string ToString()
        {
            return "Changes for {0}".Formato(Instance);
        }
    }

    public class PropertyRouteConflict
    {
        public string Original;
        public string AutomaticTranslation;

        public override string ToString()
        {
            return "Conflict {0} -> {1}".Formato(Original, AutomaticTranslation);
        }
    }
}