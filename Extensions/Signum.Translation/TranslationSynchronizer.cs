using System.Globalization;
using Signum.Authorization;
using Signum.Translation.Translators;

namespace Signum.Engine.Translation;

public static class TranslationSynchronizer
{
    public static int MaxTotalSyncCharacters = 800;

    public static LocalizedAssemblyChanges GetAssemblyChanges(ITranslator[] translators, LocalizedAssembly target, LocalizedAssembly master, List<LocalizedAssembly> support, Lite<RoleEntity>? role, string? @namespace, out int totalTypes)
    {
        var types = GetMergeChanges(target, master, support);

        if (role != null)
            types = types.Where(t => TranslationLogic.GetCountNotLocalizedMemebers(role!, t.Type.Assembly.Culture, t.Type.Type) > 0).ToList();

        if (@namespace != null)
            types = types.Where(t => t.Type.Type.Namespace == @namespace).ToList();

        totalTypes = types.Count;

        if (types.Sum(a => a.TotalOriginalLength()) > MaxTotalSyncCharacters)
            types = types.Chunk(a => a.TotalOriginalLength(), MaxTotalSyncCharacters).First().ToList();

        var result = Translate(translators, target, types);

        return result;
    }

    public static List<NamespaceSyncStats> SyncNamespaceStats(LocalizedAssembly target, LocalizedAssembly master)
    {
        return master.Types.Select(kvp =>
        {
            var ltm = kvp.Value;
            var ltt = target.Types.TryGetC(kvp.Key);

            var count = ((ltm.IsTypeCompleted() && ltt?.IsTypeCompleted() != true) ? 1 : 0) +
            ltm.Members!.Count(kvp2 => kvp2.Value != null && ltt?.Members!.TryGetC(kvp2.Key) == null);

            return new { Type = kvp.Key, count };
        })
        .Where(a => a.count > 0)
        .GroupBy(a => a.Type!.Namespace!)
        .Select(gr => new NamespaceSyncStats
        {
            @namespace = gr.Key,
            types = gr.Count(),
            translations = gr.Sum(a => a.count)
        }).ToList();
    }


    private static LocalizedAssemblyChanges Translate(ITranslator[] translators, LocalizedAssembly target, List<LocalizedTypeChanges> types)
    {
        List<IGrouping<CultureInfo, TypeNameConflict>> typeGroups =
            (from t in types
             where t.TypeConflict != null
             from tc in t.TypeConflict!
             select tc).GroupBy(a => a.Key, a => a.Value).ToList();

        foreach (IGrouping<CultureInfo, TypeNameConflict> gr in typeGroups)
        {
            var valid = gr.Where(a => a.Original.Description != null);

            var originalDescriptions = valid.Select(a =>
                (a.Original.Options.HasFlag(DescriptionOptions.Gender) ? (NaturalLanguageTools.GetDeterminer(a.Original.Gender, false, gr.Key) + " " + a.Original.Description!) : a.Original.Description) +
                (a.Original.Options.HasFlag(DescriptionOptions.PluralDescription) ? "\n" + a.Original.PluralDescription : "")
            ).ToList();

            foreach (var tr in translators)
            {
                var translations = tr.TranslateBatch(originalDescriptions, gr.Key.Name, target.Culture.Name);
                if (translations != null)
                {
                    valid.ZipForeach(translations, (sp, translated) =>
                    {
                        if (translated != null)
                        {
                            var lines = translated.Lines();
                            var singular = lines[0];
                            var plural = sp.Original.Options.HasFlag(DescriptionOptions.PluralDescription) ? lines[1] : null;

                            char? gender = null;

                            if(sp.Original.Options.HasFlag(DescriptionOptions.Gender) && NaturalLanguageTools.TryGetGenderFromDeterminer(singular.TryBefore(" "), false, target.Culture, out gender))
                                singular = singular.After(" ");

                            sp.AutomaticTranslations.Add(new AutomaticTypeTranslation { Singular = singular, Plural = plural, Gender = gender, TranslatorName = tr.Name });
                        }
                    });
                }

            }
        }

        List<IGrouping<CultureInfo, MemberNameConflict>> memberGroups =
            (from t in types
             where t.MemberConflicts != null
             from mcKVP in t.MemberConflicts
             from mc in mcKVP.Value
             select mc).GroupBy(a => a.Key, a => a.Value).ToList();

        foreach (IGrouping<CultureInfo, MemberNameConflict> gr in memberGroups)
        {
            var valid = gr.Where(a => a.Original != null).ToList();
            var originalDescriptions = valid.Select(a => a.Original!).ToList();

            foreach (var tr in translators)
            {
                var translations = tr.TranslateBatch(originalDescriptions, gr.Key.Name, target.Culture.Name);
                if (translations != null)
                {
                    gr.ZipForeach(translations, (sp, translated) =>
                    {
                        if (translated != null)
                            sp.AutomaticTranslations.Add(new AutomaticTranslation { Text = translated, TranslatorName = tr.Name });
                    });
                }
            }
        }

        return new LocalizedAssemblyChanges
        {
            LocalizedAssembly = target,
            Types = types,
        };
    }

    public static List<LocalizedTypeChanges> GetMergeChanges(LocalizedAssembly target, LocalizedAssembly master, List<LocalizedAssembly> support)
    {
        var types = master.Types.Select(kvp =>
        {
            Type type = kvp.Key;

            LocalizedType targetType = target.Types.GetOrThrow(type);

            LocalizedType masterType = master.Types[type];
            List<LocalizedType> supportTypes = support.Select(la => la.Types.TryGetC(type)).NotNull().ToList();

            Dictionary<CultureInfo, TypeNameConflict>? typeConflicts = TypeConflicts(targetType, masterType, supportTypes);

            var memberConflicts = (from m in masterType.Members!.Keys
                                   let con = MemberConflicts(m, targetType, masterType, supportTypes)
                                   where con != null
                                   select KeyValuePair.Create(m, con)).ToDictionary();

            if (memberConflicts.IsEmpty() && typeConflicts == null)
                return null;

            return new LocalizedTypeChanges { Type = targetType, TypeConflict = typeConflicts, MemberConflicts = memberConflicts };
        }).NotNull().ToList();
        return types;
    }

    static Dictionary<CultureInfo, TypeNameConflict>? TypeConflicts(LocalizedType target, LocalizedType master, List<LocalizedType> support)
    {
        if(!master.IsTypeCompleted())
            return null;

        if (target.IsTypeCompleted())
            return null;

        var sentences = new Dictionary<CultureInfo, TypeNameConflict>
        {
            { master.Assembly.Culture, new TypeNameConflict { Original = master } }
        };

        sentences.AddRange(from lt in support
                           where lt.Description != null
                           select KeyValuePair.Create(lt.Assembly.Culture, new TypeNameConflict { Original = lt }));

        return sentences;
    }

    static Dictionary<CultureInfo, MemberNameConflict>? MemberConflicts(string member, LocalizedType target, LocalizedType master, List<LocalizedType> support)
    {
        if (master.Members!.TryGetC(member) == null)
            return null;

        if (target != null && target.Members!.TryGetC(member) != null)
            return null;

        var sentences = new Dictionary<CultureInfo, MemberNameConflict>
        {
            { master.Assembly.Culture, new MemberNameConflict { Original = master.Members!.TryGetC(member) } }
        };
        sentences.AddRange(from lt in support
                           where lt.Members!.TryGetC(member).HasText()
                           select KeyValuePair.Create(lt.Assembly.Culture, new MemberNameConflict { Original = lt.Members!.TryGetC(member) }));

        return sentences;
    }
}

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
public class LocalizedAssemblyChanges
{
    public LocalizedAssembly LocalizedAssembly { get; set; }

    public List<LocalizedTypeChanges> Types { get; set; }

    public override string ToString()
    {
        return "Changes for {0}".FormatWith(LocalizedAssembly.ToString());
    }
}

public class LocalizedTypeChanges
{
    public LocalizedType Type { get; set; }

    public Dictionary<CultureInfo, TypeNameConflict>? TypeConflict;

    public Dictionary<string, Dictionary<CultureInfo, MemberNameConflict>> MemberConflicts { get; set; }

    public override string ToString()
    {
        return "Changes for {0}".FormatWith(Type);
    }

    public int TotalOriginalLength()
    {
        var ci = CultureInfo.GetCultureInfo(LocalizedAssembly.GetDefaultAssemblyCulture(Type.Assembly.Assembly));

        return (TypeConflict == null ? 0 : TypeConflict![ci].Original.Description!.Length) +
            MemberConflicts.Values.Sum(m => m[ci].Original!.Length);
    }
}

public class TypeNameConflict
{
    public LocalizedType Original;

    public List<AutomaticTypeTranslation> AutomaticTranslations = new List<AutomaticTypeTranslation>();

    public override string ToString()
    {
        return "Conflict {0} -> {1}".FormatWith(Original, AutomaticTranslations.Count);
    }
}

public class AutomaticTypeTranslation
{
    public string TranslatorName;
    public char? Gender;
    public string Singular;
    public string? Plural;
}

public class MemberNameConflict
{
    public string? Original;

    public List<AutomaticTranslation> AutomaticTranslations = new List<AutomaticTranslation>();

    public override string ToString()
    {
        return "Conflict {0} -> {1}".FormatWith(Original, AutomaticTranslations.Count);
    }
}

public class AutomaticTranslation
{
    public string TranslatorName;
    public string Text;
}

public class NamespaceSyncStats
{
    public string @namespace;
    public int types;
    public int translations;
}
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
