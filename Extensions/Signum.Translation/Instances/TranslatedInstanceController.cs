using Signum.Translation;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Signum.Utilities.Reflection;
using System.IO;
using Signum.Basics;
using Signum.API.Filters;
using Signum.Utilities;

namespace Signum.Translation.Instances;

[ValidateModelFilter]
public class TranslatedInstanceController : ControllerBase
{
    [HttpGet("api/translatedInstance")]
    public List<TranslatedTypeSummaryTS> Status()
    {
        return TranslatedInstanceLogic.TranslationInstancesStatus().Select(a => new TranslatedTypeSummaryTS(a)).ToList();
    }

    [HttpGet("api/translatedInstance/view/{type}")]
    public TranslatedInstanceViewTypeTS View(string type, string? culture, string filter)
    {
        Type t = TypeLogic.GetType(type);
        var c = culture == null ? null : CultureInfo.GetCultureInfo(culture);

        var master = TranslatedInstanceLogic.FromEntities(t);

        var support = TranslatedInstanceLogic.TranslationsForType(t, culture: c);

        var all = string.IsNullOrEmpty(filter);

        var cultures = TranslationLogic.CurrentCultureInfos(TranslatedInstanceLogic.DefaultCulture);

        Func<LocalizedInstanceKey, bool> filtered = li =>
        {
            if (all)
                return true;

            if (li.RowId.ToString() == filter || li.Instance.Id.ToString() == filter || li.Instance.Key() == filter)
                return true;

            if (li.Instance.ToString()?.Contains(filter, StringComparison.InvariantCultureIgnoreCase) == true)
                return true;

            if (li.Route.PropertyString().Contains(filter, StringComparison.InvariantCultureIgnoreCase))
                return true;

            if (master.GetOrThrow(li)?.Contains(filter, StringComparison.InvariantCultureIgnoreCase) == true)
                return true;

            if (cultures.Any(ci => (support.TryGetC(ci)?.TryGetC(li)?.TranslatedText ?? "").Contains(filter, StringComparison.InvariantCultureIgnoreCase)))
                return true;

            return false;
        };

        var sd = new StringDistance();

        var supportByInstance = (from kvpCult in support
                                 from kvpLocIns in kvpCult.Value
                                 where master.ContainsKey(kvpLocIns.Key)
                                 where filtered(kvpLocIns.Key)
                                 let newText = master.TryGet(kvpLocIns.Key, null)
                                 group (lockIns: kvpLocIns.Key, translatedInstance: kvpLocIns.Value, culture: kvpCult.Key, newText) by kvpLocIns.Key.Instance into gInstance
                                 select KeyValuePair.Create(gInstance.Key,
                                 gInstance.AgGroupToDictionary(a => a.lockIns.RouteAndRowId(),
                                    gr => gr.ToDictionary(a => a.culture.Name, a => new TranslatedPairViewTS
                                    {
                                        OriginalText = a.translatedInstance.OriginalText,
                                        NewText = a.newText,
                                        TranslatedText = a.translatedInstance.TranslatedText
                                    })
                                ))).ToDictionary();

        return new TranslatedInstanceViewTypeTS
        {
            TypeName = type,
            Routes = PropertyRouteTranslationLogic.TranslateableRoutes.GetOrThrow(t).ToDictionary(a => a.Key.PropertyString(), a => a.Value),
            MasterCulture = TranslatedInstanceLogic.DefaultCulture.Name,
            Instances = master.Where(kvp => filtered(kvp.Key)).GroupBy(a => a.Key.Instance).Select(gr => new TranslatedInstanceViewTS
            {
                Lite = gr.Key,
                Master = gr.ToDictionary(
                     a => a.Key.RouteAndRowId(),
                     a => a.Value!
                     ),
                Translations = supportByInstance.TryGetC(gr.Key) ?? new Dictionary<string, Dictionary<string, TranslatedPairViewTS>>()
            }).ToList()
        };
    }

    [HttpGet("api/translatedInstance/sync/{type}")]
    public TypeInstancesChangesTS Sync(string type, string culture)
    {
        Type t = TypeLogic.GetType(type);

        var deletedTranslations = TranslatedInstanceLogic.CleanTranslations(t);

        var c = CultureInfo.GetCultureInfo(culture);

        int totalInstances;
        var changes = TranslatedInstanceSynchronizer.GetTypeInstanceChangesTranslated(TranslationLogic.Translators, t, c, out totalInstances);

        var sd = new StringDistance();


        return new TypeInstancesChangesTS
        {
            MasterCulture = TranslatedInstanceLogic.DefaultCulture.Name,
            Routes = PropertyRouteTranslationLogic.TranslateableRoutes.GetOrThrow(t).ToDictionary(a => a.Key.PropertyString(), a => a.Value),
            TotalInstances = totalInstances,
            TypeName = t.Name,
            Instances = changes.Instances.Select(a => new InstanceChangesTS
            {
                Instance = a.Instance,
                RouteConflicts = a.RouteConflicts.ToDictionaryEx(
                    ipr => ipr.Key.RouteRowId(),
                    ipr => new PropertyChangeTS
                    {
                        Support = ipr.Value.ToDictionaryEx(c => c.Key.Name, c => new PropertyRouteConflictTS
                        {
                            Original = c.Value.Original,
                            OldOriginal = c.Value.OldOriginal,
                            OldTranslation = c.Value.OldTranslation,
                            AutomaticTranslations = c.Value.AutomaticTranslations.ToArray(),
                        })
                    }
                )
            }).ToList(),
            DeletedTranslations = deletedTranslations,
        };
    }

    [HttpGet("api/translatedInstance/autoTranslate/{type}")]
    public void AutoTranslate(string type, string culture)
    {
        var changes = Sync(type, culture);
        while (changes.Instances.Any())
        {
            var records = changes.Instances.SelectMany(ins => {
                    return ins.RouteConflicts.Keys.Select(k =>
                    {
                        var pr = k.TryBefore(";") ?? k;
                        var rowId = k.TryAfter(";");
                        var support = ins.RouteConflicts[k].Support;
                        var mc = changes.MasterCulture;

                        return new TranslationRecordTS()
                        {
                            Culture = culture,
                            PropertyRoute = pr,
                            RowId = rowId,
                            Lite = ins.Instance,
                            OriginalText = support[mc].Original,
                            TranslatedText = support[mc].AutomaticTranslations!.FirstEx().Text,
                        };
                    });
                })
                .ToList();

            Save(records, type, true, culture);

            changes = Sync(type, culture);
        }
    }

    [HttpGet("api/translatedInstance/autoTranslateAll")]
    public void AutoTranslateAll(string culture)
    {
        var summary = Status();

        summary
            .Where(s => s.State != TranslatedSummaryState.Completed)
            .GroupBy(s => s.Type)
            .ToList()
            .ForEach(gr => AutoTranslate(gr.Key, culture));
    }

    [HttpPost("api/translatedInstance/save/{type}")]
    public void Save([Required, FromBody] List<TranslationRecordTS> body, string type, bool isSync, string? culture)
    {
        Type t = TypeLogic.GetType(type);

        CultureInfo? c = culture == null ? null : CultureInfo.GetCultureInfo(culture);

        var records = GetTranslationRecords(body, t);

        TranslatedInstanceLogic.SaveRecordsByInstance(records, t, isSync, c);
    }

    private List<TranslationRecord> GetTranslationRecords(List<TranslationRecordTS> records, Type type)
    {
        var propertyRoute = PropertyRouteTranslationLogic.TranslateableRoutes.GetOrThrow(type).Keys
            .ToDictionaryEx(pr => pr.PropertyString(), pr =>
            {
                var mlistPr = pr.GetMListItemsRoute();
                var mlistPkType = mlistPr == null ? null : ((FieldMList)Schema.Current.Field(mlistPr.Parent!)).TableMList.PrimaryKey.Type;
                return (pr, mlistPkType);
            });


        var list = (from rec in records
                    let c = CultureInfo.GetCultureInfo(rec.Culture)
                    let prInfo = propertyRoute.GetOrThrow(rec.PropertyRoute)
                    select new TranslationRecord
                    {
                        Culture = c,
                        Key = new LocalizedInstanceKey(
                            prInfo.pr,
                            rec.Lite,
                            prInfo.mlistPkType == null ? null : new PrimaryKey((IComparable)ReflectionTools.Parse(rec.RowId!, prInfo.mlistPkType)!)
                            ),
                        OriginalText = rec.OriginalText,
                        TranslatedText = rec.TranslatedText,
                    }).ToList();

        return list;
    }

    [HttpGet("api/translatedInstance/viewFile/{type}")]
    public FileStreamResult ViewFile(string type, string culture)
    {
        Type t = TypeLogic.GetType(type);
        var c = CultureInfo.GetCultureInfo(culture);

        var file = TranslatedInstanceLogic.ExportExcelFile(t, c);

        return MimeMapping.GetFileStreamResult(file);
    }

    [HttpGet("api/translatedInstance/syncFile/{type}")]
    public FileStreamResult SyncFile(string type, string culture)
    {
        Type t = TypeLogic.GetType(type);
        var c = CultureInfo.GetCultureInfo(culture);

        var file = TranslatedInstanceLogic.ExportExcelFileSync(t, c);

        return MimeMapping.GetFileStreamResult(file);
    }

    [HttpPost("api/translatedInstance/uploadFile")]
    public void UploadFile([Required, FromBody] FileUpload file, [FromQuery] MatchTranslatedInstances mode)
    {
        TranslatedInstanceLogic.ImportExcelFile(new MemoryStream(file.content), file.fileName, mode);
    }
}

public class TranslationRecordTS
{
    public string Culture;
    public string PropertyRoute;
    public string? RowId;
    public Lite<Entity> Lite;
    public string OriginalText;
    public string TranslatedText;
}


public class TypeInstancesChangesTS
{
    public string TypeName;
    public string MasterCulture;
    public int TotalInstances;
    public List<InstanceChangesTS> Instances;

    public Dictionary<string, TranslatableRouteType> Routes { get; internal set; }

    public int DeletedTranslations;
}

public class InstanceChangesTS
{
    public Lite<Entity> Instance;
    public Dictionary<string, PropertyChangeTS> RouteConflicts;
}

public class PropertyChangeTS
{
    public string? TranslatedText;
    public Dictionary<string, PropertyRouteConflictTS> Support;
}

public class PropertyRouteConflictTS
{
    public string? OldOriginal;
    public string? OldTranslation;

    public string Original;
    public AutomaticTranslation[]? AutomaticTranslations;
}

public class FileUpload
{
    public string fileName;
    public byte[] content;
}

public class TranslatedInstanceViewTypeTS
{
    public string TypeName;
    public string MasterCulture;
    public Dictionary<string, TranslatableRouteType> Routes;
    public List<TranslatedInstanceViewTS> Instances;
}

public class TranslatedInstanceViewTS
{
    public Lite<Entity> Lite;
    public Dictionary<string, string> Master;
    public Dictionary<string, Dictionary<string, TranslatedPairViewTS>> Translations;
}

public class TranslatedPairViewTS
{
    public string OriginalText { get; set; }
    public string NewText { get; set; }
    public string TranslatedText { get; set; }
}

public class TranslatedTypeSummaryTS
{
    public string Type { get; }
    public bool IsDefaultCulture { get; }
    public string Culture { get; }
    public TranslatedSummaryState? State { get; }

    public TranslatedTypeSummaryTS(TranslatedTypeSummary ts)
    {
        IsDefaultCulture = ts.CultureInfo.Name == TranslatedInstanceLogic.DefaultCulture.Name;
        Type = TypeLogic.GetCleanName(ts.Type);
        Culture = ts.CultureInfo.Name;
        State = ts.State;
    }
}
