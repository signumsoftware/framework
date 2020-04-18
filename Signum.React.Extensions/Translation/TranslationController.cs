using Newtonsoft.Json;
using Signum.Engine.Translation;
using Signum.React.Filters;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Signum.React.Translation
{
    [ValidateModelFilter]
    public class TranslationController : ControllerBase
    {
        public static IEnumerable<Assembly> AssembliesToLocalize()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Where(a => a.HasAttribute<DefaultAssemblyCultureAttribute>());
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


        [HttpPost("api/translation/retrieve")]
        public AssemblyResultTS Retrieve(string assembly, string culture, string filter)
        {
            Assembly ass = AssembliesToLocalize().Where(a => a.GetName().Name == assembly).SingleEx(() => "Assembly {0}".FormatWith(assembly));

            CultureInfo defaultCulture = CultureInfo.GetCultureInfo(ass.GetCustomAttribute<DefaultAssemblyCultureAttribute>()!.DefaultCulture);
            CultureInfo? targetCulture = culture == null ? null : CultureInfo.GetCultureInfo(culture);

            var cultures = TranslationLogic.CurrentCultureInfos(defaultCulture);

            Dictionary<string, LocalizableTypeTS> types =
                (from ci in cultures
                 let la = DescriptionManager.GetLocalizedAssembly(ass, ci)
                 where la != null || ci == defaultCulture || ci == targetCulture
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
                     members = t.Members.Select(kvp => new LocalizedMemberTS { name = kvp.Key, description = kvp.Value }).ToDictionary(a => a.name),
                 }
                 group lt by t.Type into g
                 select KeyValuePair.Create(g.Key.Name, g.Key.ToLocalizableTypeTS().Let(localizedTypes => 
                 {
                     localizedTypes.cultures = g.ToDictionary(a => a.culture);
                     return localizedTypes;
                 })))
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
            Assembly ass = AssembliesToLocalize().Where(a => a.GetName().Name == assembly).SingleEx(() => "Assembly {0}".FormatWith(assembly));
            CultureInfo targetCulture = CultureInfo.GetCultureInfo(culture);

            CultureInfo defaultCulture = CultureInfo.GetCultureInfo(ass.GetCustomAttribute<DefaultAssemblyCultureAttribute>()!.DefaultCulture);

            var cultures = TranslationLogic.CurrentCultureInfos(defaultCulture);
            Dictionary<CultureInfo, LocalizedAssembly> reference = (from ci in cultures
                                                                    let la = DescriptionManager.GetLocalizedAssembly(ass, ci)
                                                                    where la != null || ci == defaultCulture || ci == targetCulture
                                                                    select KeyValuePair.Create(ci, la ?? LocalizedAssembly.ImportXml(ass, ci, forceCreate: ci == defaultCulture || ci == targetCulture))).ToDictionary();

            var master = reference.Extract(defaultCulture);
            var target = reference.Extract(targetCulture);
            var changes = TranslationSynchronizer.GetAssemblyChanges(TranslationServer.Translator, target, master, reference.Values.ToList(), null, @namespace, out int totalTypes);

            return new AssemblyResultTS
            {
                totalTypes = totalTypes,
                cultures = cultures.Select(c => c.ToCulturesTS()).ToDictionary(a => a.name),
                types = changes.Types.Select(t => t.Type.Type.ToLocalizableTypeTS().Let(localizedTypes =>
                {
                    localizedTypes.cultures = cultures.ToDictionary(c => c.Name, c => GetLocalizedType(t, c, c.Equals(targetCulture)));
                    return localizedTypes;
                })).ToDictionary(lt => lt.type),
            };
        }

        [HttpGet("api/translation/syncStats")]
        public List<NamespaceSyncStats> SyncStats(string assembly, string culture)
        {
            Assembly ass = AssembliesToLocalize().Where(a => a.GetName().Name == assembly).SingleEx(() => "Assembly {0}".FormatWith(assembly));
            CultureInfo targetCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo defaultCulture = CultureInfo.GetCultureInfo(ass.GetCustomAttribute<DefaultAssemblyCultureAttribute>()!.DefaultCulture);

            var targetAssembly = DescriptionManager.GetLocalizedAssembly(ass, targetCulture) ?? LocalizedAssembly.ImportXml(ass, targetCulture, forceCreate: true)!;
            var defaultAssembly = DescriptionManager.GetLocalizedAssembly(ass, defaultCulture) ?? LocalizedAssembly.ImportXml(ass, defaultCulture, forceCreate: true)!;

            return TranslationSynchronizer.SyncNamespaceStats(targetAssembly, defaultAssembly);
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
                    description = tc?.Original.Description ?? (isTarget && t.TypeConflict.Count >= 2 ? t.TypeConflict.Select(a => a.Value.Translated).Distinct().Only() : null),
                    pluralDescription = tc?.Original.PluralDescription,
                    gender = tc?.Original.Gender?.ToString(),
                    translatedDescription = tc?.Translated,
                },
                members = t.MemberConflicts.EmptyIfNull().Where(kvp=> kvp.Value.ContainsKey(ci) || isTarget).Select(kvp => new LocalizedMemberTS
                {
                    name = kvp.Key,
                    description = kvp.Value.TryGetC(ci)?.Original ?? (isTarget && kvp.Value.Count >= 2 ? kvp.Value.Select(a => a.Value.Translated).Distinct().Only() : null),
                    translatedDescription = kvp.Value.TryGetC(ci)?.Translated
                }).ToDictionary(a => a.name),
            };
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
                if (this.hasMembers)
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
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string? gender;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string? description;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string? translatedDescription;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string? pluralDescription;
        }

        public class LocalizedMemberTS
        {
            public string name;
            public string? description;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string? translatedDescription;
        }


        [HttpPost("api/translation/save")]
        public void SaveTypes(string assembly, string culture, [Required, FromBody]AssemblyResultTS result)
        {
            var currentAssembly = AssembliesToLocalize().Single(a => a.GetName().Name == assembly);

            var cultureGroups = (from a in result.types.Values
                                 from lt in a.cultures.Values
                                 group new { a.type, lt } by lt.culture into cg
                                 select cg).ToList();

            foreach (var cultureGroup in cultureGroups)
            {
                LocalizedAssembly locAssembly = LocalizedAssembly.ImportXml(currentAssembly, CultureInfo.GetCultureInfo(cultureGroup.Key), forceCreate: true)!;

                var types = cultureGroup.ToDictionary(a => a.type!, a => a.lt!); /*CSBUG*/

                foreach (var lt in locAssembly.Types.Values)
                {
                    var ts = types.TryGetC(lt.Type.Name);

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
        public static TranslationController.LocalizableTypeTS ToLocalizableTypeTS(this Type type)
        {
            var options = LocalizedAssembly.GetDescriptionOptions(type);
            return new TranslationController.LocalizableTypeTS()
            {
                type = type.Name,
                hasDescription = options.IsSet(DescriptionOptions.Description),
                hasPluralDescription = options.IsSet(DescriptionOptions.PluralDescription),
                hasMembers = options.IsSet(DescriptionOptions.Members),
                hasGender = options.IsSet(DescriptionOptions.Gender),
            };
        }

        public static TranslationController.CulturesTS ToCulturesTS(this CultureInfo ci)
        {
            return new TranslationController.CulturesTS()
            {
                name = ci.Name,
                englishName = ci.EnglishName,
                pronoms = NaturalLanguageTools.GenderDetectors.TryGetC(ci.TwoLetterISOLanguageName)?.Pronoms.ToList(),
            };
        }
    }
}
