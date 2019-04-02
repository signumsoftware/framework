using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Signum.Utilities;

namespace Signum.Engine.Translation
{
    public static class TranslatedInstanceSynchronizer
    {
        public static int MaxTotalSyncCharacters = 4000;

        public static TypeInstancesChanges GetTypeInstanceChangesTranslated(ITranslator translator, Type type, CultureInfo targetCulture, out int totalInstances)
        {
            var cultures = TranslationLogic.CurrentCultureInfos(TranslatedInstanceLogic.DefaultCulture);

            cultures.Remove(targetCulture);

            var instances = TranslatedInstanceLogic.GetInstanceChanges(type, targetCulture, cultures);

            totalInstances = instances.Count;
            if (instances.Sum(a => a.TotalOriginalLength()) > MaxTotalSyncCharacters)
                instances = instances.GroupsOf(a => a.TotalOriginalLength(), MaxTotalSyncCharacters).First().ToList();

            return TranslateInstances(translator, type, targetCulture, instances);
        }



        private static TypeInstancesChanges TranslateInstances(ITranslator translator, Type type, CultureInfo targetCulture, List<InstanceChanges> instances)
        {
            List<IGrouping<CultureInfo, PropertyRouteConflict>> memberGroups = (from t in instances
                                                                                from rcKVP in t.RouteConflicts
                                                                                from rc in rcKVP.Value
                                                                                select rc).GroupBy(a => a.Key, a => a.Value).ToList();

            foreach (IGrouping<CultureInfo, PropertyRouteConflict> gr in memberGroups)
            {
                var result = translator.TranslateBatch(gr.Select(a => a.Original).ToList(), gr.Key.Name, targetCulture.Name);

                gr.ZipForeach(result, (sp, translated) => sp.AutomaticTranslation = translated);
            }

            return new TypeInstancesChanges(type, instances);
        }
    }

    public class TypeInstancesChanges
    {
        public TypeInstancesChanges(Type type, List<InstanceChanges> instances)
        {
            Type = type;
            Instances = instances;
        }

        public Type Type { get; set; }

        public List<InstanceChanges> Instances { get; set; }

        public override string ToString()
        {
            return "Changes for instances of type {0}".FormatWith(Type.NiceName());
        }
    }
}
