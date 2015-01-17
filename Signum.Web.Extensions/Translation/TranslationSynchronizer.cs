using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using Signum.Utilities;

namespace Signum.Web.Translation
{
    public static class TranslationSynchronizer
    {
        public static int MaxTotalSyncCharacters = 800;

        public static LocalizedAssemblyChanges GetAssemblyChanges(ITranslator translator, LocalizedAssembly target, LocalizedAssembly master, List<LocalizedAssembly> support, bool translatedOnly, out int totalTypes)
        {
            var types = GetMergeChanges(target, master, support);

            totalTypes = types.Count;

            if (!translatedOnly && types.Sum(a => a.TotalOriginalLength()) > MaxTotalSyncCharacters)
                types = types.GroupsOf(a => a.TotalOriginalLength(), MaxTotalSyncCharacters).First().ToList();

            var result =  Translate(translator, target, types);

            if (translatedOnly)
                result.Types = result.Types.Where(t =>
                    t.TypeConflict != null && t.TypeConflict.Values.Any(tc => tc.Translated.HasText()) ||
                    t.MemberConflicts != null && t.MemberConflicts.Values.Any(m => m.Values.Any(mc => mc.Translated.HasText())))
                    .ToList();

            return result;
        }

        private static LocalizedAssemblyChanges Translate(ITranslator translator, LocalizedAssembly target, List<LocalizedTypeChanges> types)
        {
            List<IGrouping<CultureInfo, TypeNameConflict>> typeGroups =
                (from t in types
                 where t.TypeConflict != null
                 from tc in t.TypeConflict
                 select tc).GroupBy(a => a.Key, a => a.Value).ToList();

            foreach (IGrouping<CultureInfo, TypeNameConflict> gr in typeGroups)
            {
                List<string> result = translator.TranslateBatch(gr.Select(a => a.Original.Description).ToList(), gr.Key.Name, target.Culture.Name);

                gr.ZipForeach(result, (sp, translated) => sp.Translated = translated);
            }

            List<IGrouping<CultureInfo, MemberNameConflict>> memberGroups =
                (from t in types
                 where t.MemberConflicts != null
                 from mcKVP in t.MemberConflicts
                 from mc in mcKVP.Value
                 select mc).GroupBy(a => a.Key, a => a.Value).ToList();

            foreach (IGrouping<CultureInfo, MemberNameConflict> gr in memberGroups)
            {
                var result = translator.TranslateBatch(gr.Select(a => a.Original).ToList(), gr.Key.Name, target.Culture.Name);

                gr.ZipForeach(result, (sp, translated) => sp.Translated = translated);
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

                LocalizedType targetType = target.Types.TryGetC(type);

                LocalizedType masterType = master.Types[type];
                List<LocalizedType> supportTypes = support.Select(la => la.Types.TryGetC(type)).NotNull().ToList();

                Dictionary<CultureInfo, TypeNameConflict> typeConflicts = TypeConflicts(targetType, masterType, supportTypes);

                var memberConflicts = (from m in masterType.Members.Keys
                                       let con = MemberConflicts(m, targetType, masterType, supportTypes)
                                       where con != null
                                       select KVP.Create(m, con)).ToDictionary();

                if (memberConflicts.IsEmpty() && typeConflicts == null)
                    return null;

                return new LocalizedTypeChanges { Type = targetType, TypeConflict = typeConflicts, MemberConflicts = memberConflicts };
            }).NotNull().ToList();
            return types;
        }

        static Dictionary<CultureInfo, TypeNameConflict> TypeConflicts(LocalizedType target, LocalizedType master, List<LocalizedType> support)
        {
            if(master.Description == null)
                return null;

            if (target != null && target.Description != null)
                return null;

            var sentences = new Dictionary<CultureInfo, TypeNameConflict>();

            sentences.Add(master.Assembly.Culture, new TypeNameConflict { Original = master });

            sentences.AddRange(from lt in support
                               where lt.Description != null
                               select KVP.Create(lt.Assembly.Culture, new TypeNameConflict { Original = lt }));

            return sentences;
        }

        static Dictionary<CultureInfo, MemberNameConflict> MemberConflicts(string member, LocalizedType target, LocalizedType master, List<LocalizedType> support)
        {
            if (master.Members.TryGetC(member) == null)
                return null;

            if (target != null && target.Members.TryGetC(member) != null)
                return null;

            var sentences = new Dictionary<CultureInfo, MemberNameConflict>();

            sentences.Add(master.Assembly.Culture, new MemberNameConflict { Original = master.Members.TryGetC(member) });

            sentences.AddRange(from lt in support
                               where lt.Members.TryGetC(member).HasText()
                               select KVP.Create(lt.Assembly.Culture, new MemberNameConflict { Original = lt.Members.TryGetC(member) }));

            return sentences;
        }
    }

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

        public Dictionary<CultureInfo, TypeNameConflict> TypeConflict;

        public Dictionary<string, Dictionary<CultureInfo, MemberNameConflict>> MemberConflicts { get; set; }

        public override string ToString()
        {
            return "Changes for {0}".FormatWith(Type);
        }

        public int TotalOriginalLength()
        {
            var ci = CultureInfo.GetCultureInfo(LocalizedAssembly.GetDefaultAssemblyCulture(Type.Assembly.Assembly));

            return (TypeConflict == null ? 0 : TypeConflict[ci].Original.Description.Length) +
                MemberConflicts.Values.Sum(m => m[ci].Original.Length);
        }
    }

    public class TypeNameConflict
    {
        public LocalizedType Original;
        public string Translated;

        public override string ToString()
        {
            return "Conflict {0} -> {1}".FormatWith(Original, Translated);
        }
    }

    public class MemberNameConflict
    {
        public string Original;
        public string Translated;

        public override string ToString()
        {
            return "Conflict {0} -> {1}".FormatWith(Original, Translated);
        }
    }
}