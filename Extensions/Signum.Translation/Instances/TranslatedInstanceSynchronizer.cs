using System.Globalization;
using Signum.Translation;
using Signum.Translation.Translators;

namespace Signum.Translation.Instances;

public static class TranslatedInstanceSynchronizer
{
    public static int MaxTotalSyncCharacters = 4000;

    public static TypeInstancesChanges GetTypeInstanceChangesTranslated(ITranslator[] translators, Type type, CultureInfo targetCulture, out int totalInstances)
    {
        var cultures = TranslationLogic.CurrentCultureInfos(TranslatedInstanceLogic.DefaultCulture);

        cultures.Remove(targetCulture);

        var instances = TranslatedInstanceLogic.GetInstanceChanges(type, targetCulture, cultures);

        totalInstances = instances.Count;
        if (instances.Sum(a => a.TotalOriginalLength()) > MaxTotalSyncCharacters)
            instances = instances.Chunk(a => a.TotalOriginalLength(), MaxTotalSyncCharacters).First().ToList();

        return TranslateInstances(translators, type, targetCulture, instances);
    }



    private static TypeInstancesChanges TranslateInstances(ITranslator[] translators, Type type, CultureInfo targetCulture, List<InstanceChanges> instances)
    {
        List<IGrouping<CultureInfo, PropertyRouteConflict>> memberGroups = (from t in instances
                                                                            from rcKVP in t.RouteConflicts
                                                                            from rc in rcKVP.Value
                                                                            select rc).GroupBy(a => a.Key, a => a.Value).ToList();

        foreach (IGrouping<CultureInfo, PropertyRouteConflict> gr in memberGroups)
        {
            var originals = gr.Select(a => a.Original).ToList();

            foreach (var tr in translators)
            {
                var result = tr.TranslateBatch(originals, gr.Key.Name, targetCulture.Name);
                if (result != null)
                {
                    gr.ZipForeach(result, (sp, translated) =>
                    {
                        if (translated != null)
                            sp.AutomaticTranslations.Add(new AutomaticTranslation { Text = translated, TranslatorName = tr.Name });
                    });
                }

            }
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
