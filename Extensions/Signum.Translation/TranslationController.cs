using Signum.Translation;
using Signum.Utilities.DataStructures;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using static Signum.Translation.TranslationController;
using Signum.Translation.Instances;
using Signum.API.Filters;
using System.IO;

namespace Signum.Translation;

[ValidateModelFilter]
public class TranslationController : ControllerBase
{
    static IEnumerable<Assembly> AssembliesToLocalize()
    {
        return AppDomain.CurrentDomain.GetAssemblies().Where(a => a.HasAttribute<DefaultAssemblyCultureAttribute>());
    }

    static Assembly GetAssembly(string assembly)
    {
        return AssembliesToLocalize().Where(a => a.GetName().Name == assembly).SingleEx(() => "Assembly {0}".FormatWith(assembly));
    }


    [HttpGet("api/translation/state")]
    public List<TranslationFileStatus> GetState()
    {
        var cultures = TranslationLogic.CurrentCultureInfos(CultureInfo.GetCultureInfo("en"));

        var assemblies = AssembliesToLocalize().ToDictionary(a => a.FullName!);

        var dg = DirectedGraph<Assembly>.Generate(assemblies.Values, a => a.GetReferencedAssemblies().Select(an => assemblies.TryGetC(an.FullName!)).NotNull());

        var list = (from a in dg.CompilationOrderGroups().SelectMany(gr => gr.OrderBy(a => a.FullName))
                    from ci in cultures
                    select new TranslationFileStatus
                    {
                        assembly = a.GetName().Name!,
                        culture = ci.Name,
                        isDefault = ci.Name == a.GetCustomAttribute<DefaultAssemblyCultureAttribute>()!.DefaultCulture,
                        status = CalculateStatus(a, ci)
                    }).ToList();

        return list;
    }

    private TranslatedSummaryState CalculateStatus(Assembly a, CultureInfo ci)
    {
        var fileName = LocalizedAssembly.TranslationFileName(a, ci);

        if (!System.IO.File.Exists(fileName))
            return TranslatedSummaryState.None;

        var target = DescriptionManager.GetLocalizedAssembly(a, ci)!;

        CultureInfo defaultCulture = CultureInfo.GetCultureInfo(a.GetCustomAttribute<DefaultAssemblyCultureAttribute>()!.DefaultCulture);
        var master = DescriptionManager.GetLocalizedAssembly(a, defaultCulture)!;

        var result = TranslationSynchronizer.GetMergeChanges(target, master, new List<LocalizedAssembly>());

        if (result.Any())
            return TranslatedSummaryState.Pending;

        return TranslatedSummaryState.Completed;
    }

    public class TranslationFileStatus
    {
        public string assembly;
        public string culture;
        public bool isDefault;
        public TranslatedSummaryState status;
    }

    [HttpGet("api/translation/download")]
    public PhysicalFileResult Download(string assembly, string culture)
    {
        Assembly ass = GetAssembly(assembly);

        CultureInfo ci = CultureInfo.GetCultureInfo(culture);

        var file = LocalizedAssembly.TranslationFileName(ass, ci);

        var mime = MimeMapping.GetMimeType(file);
        return new PhysicalFileResult(file, mime)
        {
            FileDownloadName = Path.GetFileName(file)
        };
    }

    [HttpGet("api/translation/retrieve")]
    public AssemblyResultTS Retrieve(string assembly, string culture, string filter)
    {
        Assembly ass = GetAssembly(assembly);

        CultureInfo defaultCulture = CultureInfo.GetCultureInfo(ass.GetCustomAttribute<DefaultAssemblyCultureAttribute>()!.DefaultCulture);
        CultureInfo? targetCulture = culture == null ? null : CultureInfo.GetCultureInfo(culture);

        var cultures = TranslationLogic.CurrentCultureInfos(defaultCulture);

        Dictionary<string, LocalizableTypeTS> types =
            (from ci in cultures
             let la = DescriptionManager.GetLocalizedAssembly(ass, ci)
             where la != null || ci.Equals(defaultCulture) || ci.Equals(targetCulture)
             let la2 = la ?? LocalizedAssembly.ImportXml(ass, ci, forceCreate: true)
             from t in la2.Types.Values
             let lt = new LocalizedTypeTS
             {
                 culture = ci.Name,
                 typeDescription = new LocalizedDescriptionTS
                 {
                     gender = t.Gender?.ToString(),
                     description = t.Description,
                     pluralDescription = t.PluralDescription,
                 },
                 members = t.Members!.Select(kvp => new LocalizedMemberTS { name = kvp.Key, description = kvp.Value }).ToDictionary(a => a.name),
             }
             group lt by t.Type into g
             select KeyValuePair.Create(g.Key.Name, g.Key.ToLocalizableTypeTS(g.ToDictionary(a => a.culture))))
             .ToDictionaryEx("types");


        types.ToList().ForEach(lt => lt.Value.FixMembers(defaultCulture));

        if (filter.HasText())
        {
            var complete = types.Extract((k, v) => v.type.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ||
                        v.cultures.Values.Select(a => a.typeDescription!).Any(td =>
                          td.description != null && td.description.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ||
                          td.pluralDescription != null && td.pluralDescription.Contains(filter, StringComparison.InvariantCultureIgnoreCase)));


            var filtered = types.Extract((k, v) =>
            {
                var allMembers = v.cultures.Values.SelectMany(a => a.members.Keys).Distinct().ToList();

                var filteredMembers = allMembers.Where(m => m.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ||
                v.cultures.Values.Any(lt => lt.members.GetOrThrow(m).description?.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ?? false))
                .ToList();

                if (filteredMembers.Count == 0)
                    return false;

                foreach (var item in v.cultures.Values)
                {
                    item.members = item.members.Where(a => filteredMembers.Contains(a.Key)).ToDictionary();
                }

                return true;

            });

            types = complete.Concat(filtered).ToDictionary();
        }

        return new AssemblyResultTS
        {
            types = types.OrderBy(a => a.Key).ToDictionary(),
            cultures = cultures.Select(c => c.ToCulturesTS())
            .ToDictionary(a => a.name)
        };
    }


    [HttpPost("api/translation/sync")]
    public AssemblyResultTS Sync(string assembly, string culture, string? @namespace = null)
    {
        Assembly ass = GetAssembly(assembly);
        CultureInfo targetCulture = CultureInfo.GetCultureInfo(culture);

        CultureInfo defaultCulture = CultureInfo.GetCultureInfo(ass.GetCustomAttribute<DefaultAssemblyCultureAttribute>()!.DefaultCulture);

        var cultures = TranslationLogic.CurrentCultureInfos(defaultCulture);
        Dictionary<CultureInfo, LocalizedAssembly> reference = (from ci in cultures
                                                                let la = DescriptionManager.GetLocalizedAssembly(ass, ci)
                                                                where la != null || ci.Equals(defaultCulture) || ci.Equals(targetCulture)
                                                                select KeyValuePair.Create(ci, la ?? LocalizedAssembly.ImportXml(ass, ci, forceCreate: ci.Equals(defaultCulture) || ci.Equals(targetCulture)))).ToDictionary();

        var master = reference.Extract(defaultCulture);
        var target = reference.Extract(targetCulture);
        var changes = TranslationSynchronizer.GetAssemblyChanges(TranslationLogic.Translators, target, master, reference.Values.ToList(), null, @namespace, out int totalTypes);

        return new AssemblyResultTS
        {
            totalTypes = totalTypes,
            cultures = cultures.Select(c => c.ToCulturesTS()).ToDictionary(a => a.name),
            types = changes.Types
            .Select(t => t.Type.Type.ToLocalizableTypeTS(cultures.ToDictionary(c => c.Name, c => GetLocalizedType(t, c, c.Equals(targetCulture)))))
            .ToDictionary(lt => lt.type),
        };
    }


    [HttpGet("api/translation/syncStats")]
    public List<NamespaceSyncStats> SyncStats(string assembly, string culture)
    {
        Assembly ass = GetAssembly(assembly);
        CultureInfo targetCulture = CultureInfo.GetCultureInfo(culture);
        CultureInfo defaultCulture = CultureInfo.GetCultureInfo(ass.GetCustomAttribute<DefaultAssemblyCultureAttribute>()!.DefaultCulture);

        var targetAssembly = DescriptionManager.GetLocalizedAssembly(ass, targetCulture) ?? LocalizedAssembly.ImportXml(ass, targetCulture, forceCreate: true)!;
        var defaultAssembly = DescriptionManager.GetLocalizedAssembly(ass, defaultCulture) ?? LocalizedAssembly.ImportXml(ass, defaultCulture, forceCreate: true)!;

        return TranslationSynchronizer.SyncNamespaceStats(targetAssembly, defaultAssembly);
    }

    [HttpGet("api/translation/autoTranslate")]
    public void AutoTranslate(string assembly, string culture)
    {
        var changes = Sync(assembly, culture);
        while (changes.types.Any())
        {
            changes.types.Values
            .ToList()
            .ForEach(t =>
            {
                var locType = t.cultures.GetOrThrow(culture);

                if (t.hasDescription)
                {
                    var td = locType.typeDescription;
                    if (td != null)
                    {
                        var translates = t.cultures
                            .Where(a => a.Key != culture)
                            .SelectMany(a => a.Value.typeDescription?.automaticTranslations ?? new List<AutomaticTypeTranslation>().ToArray())
                            .ToList();

                        if (!td.description.HasText())
                            td.description = translates.FirstEx().Singular;

                        if (!td.pluralDescription.HasText())
                            td.pluralDescription = translates.FirstEx().Plural;

                        if (!td.gender.HasText())
                            td.gender = translates.FirstEx().Gender?.ToString();
                    }
                }

                if (t.hasMembers)
                {
                    var members = locType.members
                        .Where(kvp => !kvp.Value.description.HasText())
                        .ToList();

                    members.ForEach(m =>
                    {
                        var translates = t.cultures
                            .Where(kvp => kvp.Key != culture)
                            .SelectMany(kvp => kvp.Value.members)
                            .Where(kvp => kvp.Key == m.Key)
                            .SelectMany(kvp => kvp.Value.automaticTranslations ?? new List<AutomaticTranslation>().ToArray())
                            .ToList();

                        m.Value.description = translates.FirstEx().Text;
                    });
                }
            });

            SaveTypes(assembly, culture, changes);

            changes = Sync(assembly, culture);
        }
    }

    [HttpGet("api/translation/autoTranslateAll")]
    public void AutoTranslateAll(string culture)
    {
        var states = GetState();

        states
            .Where(s => s.status != TranslatedSummaryState.Completed)
            .ToList()
            .ForEach(s => AutoTranslate(s.assembly, culture));
    }

    private LocalizedTypeTS GetLocalizedType(LocalizedTypeChanges t, CultureInfo ci, bool isTarget)
    {
        var tc = t.TypeConflict?.TryGetC(ci);

        return new LocalizedTypeTS
        {
            culture = ci.Name,
            typeDescription = t.TypeConflict == null || (tc == null && !isTarget) ? null : /*Message, Symbol, etc...*/
            new LocalizedDescriptionTS
            {
                description = tc?.Original.Description ?? (isTarget ? DisctincOnly(t.TypeConflict.SelectMany(a => a.Value.AutomaticTranslations).Select(a => a.Singular)) : null),
                pluralDescription = tc?.Original.PluralDescription ?? (isTarget ? DisctincOnly(t.TypeConflict.SelectMany(a => a.Value.AutomaticTranslations).Select(a => a.Plural)) : null),
                gender = tc?.Original.Gender?.ToString() ?? (isTarget ? DisctincOnly(t.TypeConflict.SelectMany(a => a.Value.AutomaticTranslations).Select(a => a.Gender?.ToString())) : null),
                automaticTranslations = tc?.AutomaticTranslations.ToArray(),
            },
            members = t.MemberConflicts.EmptyIfNull().Where(kvp => kvp.Value.ContainsKey(ci) || isTarget).Select(kvp => new LocalizedMemberTS
            {
                name = kvp.Key,
                description = kvp.Value.TryGetC(ci)?.Original ?? (isTarget ? DisctincOnly(kvp.Value.SelectMany(a => a.Value.AutomaticTranslations).Select(a => a.Text)) : null),
                automaticTranslations = kvp.Value.TryGetC(ci)?.AutomaticTranslations.ToArray()
            }).ToDictionary(a => a.name),
        };
    }

    string? DisctincOnly(IEnumerable<string?> automaticTranslations)
    {
        if (automaticTranslations.Count() >= 2)
            return automaticTranslations.Distinct().Only();

        return null;
    }

    public class AssemblyResultTS
    {
        public int totalTypes;
        public Dictionary<string, CulturesTS> cultures;
        public Dictionary<string, LocalizableTypeTS> types;
    }

    public class CulturesTS
    {
        public string name;
        public string englishName;
        public List<PronomInfo>? pronoms;
    }

    public class LocalizableTypeTS
    {
        public string type;
        public bool hasMembers;
        public bool hasGender;
        public bool hasDescription;
        public bool hasPluralDescription;

        public Dictionary<string, LocalizedTypeTS> cultures = null!;

        internal void FixMembers(CultureInfo defaultCulture)
        {
            if (this.hasMembers && cultures.ContainsKey(defaultCulture.Name))
            {
                var members = cultures[defaultCulture.Name].members.Keys;

                foreach (var locType in cultures.Where(kvp => kvp.Key != defaultCulture.Name).Select(kvp => kvp.Value))
                {
                    locType.members = members.ToDictionary(m => m, m => locType.members.TryGetC(m) ?? new LocalizedMemberTS { name = m });
                }
            }
        }
    }

    public class LocalizedTypeTS
    {
        public string culture;
        public LocalizedDescriptionTS? typeDescription;
        public Dictionary<string, LocalizedMemberTS> members;
    }

    public class LocalizedDescriptionTS
    {
        public string? gender;
        public string? description;
        public string? pluralDescription;
        public AutomaticTypeTranslation[]? automaticTranslations;
    }

    public class LocalizedMemberTS
    {
        public string name;
        public string? description;
        public AutomaticTranslation[]? automaticTranslations;
    }


    [HttpPost("api/translation/save")]
    public void SaveTypes(string assembly, string? culture, [Required, FromBody] AssemblyResultTS result)
    {
        var currentAssembly = GetAssembly(assembly);

        foreach (var cult in result.cultures.Keys.Where(ci => culture == null  || culture.Equals(ci)))
        {
            LocalizedAssembly locAssembly = LocalizedAssembly.ImportXml(currentAssembly, CultureInfo.GetCultureInfo(cult), forceCreate: true)!;

            var types = result.types.Values.ToDictionary(a => a.type!, a => a.cultures.TryGetC(cult));

            foreach (var lt in locAssembly.Types.Values)
            {
                var ts = types.TryGet(lt.Type.Name, null);

                if (ts != null)
                {
                    if (ts.typeDescription != null)
                    {
                        var td = ts.typeDescription;
                        lt.Gender = td.gender?[0];
                        lt.Description = td.description;
                        lt.PluralDescription = td.pluralDescription;
                    }

                    lt.Members!.SetRange(ts.members.Select(a => KeyValuePair.Create(a.Key!, a.Value.description!)));
                }
            }

            locAssembly.ExportXml();
        }
    }

    [HttpPost("api/translation/pluralize")]
    public string Pluralize(string culture, [Required, FromBody]string text)
    {
        return NaturalLanguageTools.Pluralize(text, CultureInfo.GetCultureInfo(culture));
    }

    [HttpPost("api/translation/gender")]
    public string? Gender(string culture, [Required, FromBody]string text)
    {
        return NaturalLanguageTools.GetGender(text, CultureInfo.GetCultureInfo(culture))?.ToString();
    }
}

public static class Extensions
{
    public static TranslationController.LocalizableTypeTS ToLocalizableTypeTS(this Type type, Dictionary<string, LocalizedTypeTS> cultures)
    {
        var options = LocalizedAssembly.GetDescriptionOptions(type);
        return new TranslationController.LocalizableTypeTS()
        {
            type = type.Name,
            hasDescription = options.IsSet(DescriptionOptions.Description),
            hasPluralDescription = options.IsSet(DescriptionOptions.PluralDescription),
            hasMembers = options.IsSet(DescriptionOptions.Members),
            hasGender = options.IsSet(DescriptionOptions.Gender),
            cultures = cultures,
        };
    }

    public static TranslationController.CulturesTS ToCulturesTS(this CultureInfo ci)
    {
        return new TranslationController.CulturesTS()
        {
            name = ci.Name,
            englishName = ci.EnglishName,
            pronoms = NaturalLanguageTools.GenderDetectors.TryGetC(ci.TwoLetterISOLanguageName)?.Determiner.ToList(),
        };
    }
}
