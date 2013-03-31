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
        public static AssemblyChanges GetAssemblyChanges(ITranslator translator, LocalizedAssembly target, LocalizedAssembly master, List<LocalizedAssembly> support)
        {
            var types = master.Types.Select(kvp =>
            {
                var type = kvp.Key;

                LocalizedType targetType = target.Types.TryGetC(type);

                LocalizedType masterType = master.Types[type];
                List<LocalizedType> supportTypes = support.Select(la => la.Types.TryGetC(type)).NotNull().ToList();

                var typeConflicts = TypeConflicts(targetType, masterType, supportTypes);

                var memberConflicts = (from m in masterType.Members.Keys
                                       let con = MemberConflicts(m, targetType, masterType, supportTypes)
                                       where con != null
                                       select KVP.Create(m, con)).ToDictionary();

                if (memberConflicts.IsEmpty() && typeConflicts == null)
                    return null;

                return new TypeChanges { Type = targetType, TypeConflict = typeConflicts, MemberConflicts = memberConflicts };
            }).NotNull().ToList();

            var typeGroups = (from t in types
                              where t.TypeConflict != null
                              from tc in t.TypeConflict
                              select tc).GroupBy(a => a.Key, a => a.Value).ToList();

            foreach (var gr in typeGroups)
            {
                var result = translator.TranslateBatch(gr.Select(a => a.Original.Description).ToList(), gr.Key.Name, target.Culture.Name);

                gr.ZipForeach(result, (sp, translated) => sp.Translated = translated);
            }

            var memberGroups = (from t in types
                                where t.MemberConflicts != null
                                from mcKVP in t.MemberConflicts
                                from mc in mcKVP.Value
                                select mc).GroupBy(a => a.Key, a => a.Value).ToList();

            foreach (var gr in memberGroups)
            {
                var result = translator.TranslateBatch(gr.Select(a => a.Original).ToList(), gr.Key.Name, target.Culture.Name);

                gr.ZipForeach(result, (sp, translated) => sp.Translated = translated);
            }

            return new AssemblyChanges
            {
                LocalizedAssembly = target,
                Types = types,
            };
        }

        static Dictionary<CultureInfo, TypeConflict> TypeConflicts(LocalizedType target, LocalizedType master, List<LocalizedType> support)
        {
            if(master.Description == null)
                return null;

            if (target != null && target.Description != null)
                return null;

            var sentences = new Dictionary<CultureInfo, TypeConflict>();

            sentences.Add(master.Assembly.Culture, new TypeConflict { Original = master });

            sentences.AddRange(from lt in support
                               where lt.Description != null
                               select KVP.Create(lt.Assembly.Culture, new TypeConflict { Original = lt }));

            return sentences;
        }

        static Dictionary<CultureInfo, MemberConflict> MemberConflicts(string member, LocalizedType target, LocalizedType master, List<LocalizedType> support)
        {
            if (master.Members.TryGetC(member) == null)
                return null;

            if (target != null && target.Members.TryGetC(member) != null)
                return null;

            var sentences = new Dictionary<CultureInfo, MemberConflict>();

            sentences.Add(master.Assembly.Culture, new MemberConflict { Original = master.Members.TryGetC(member) });

            sentences.AddRange(from lt in support
                               where lt.Description != null
                               select KVP.Create(lt.Assembly.Culture, new MemberConflict { Original = lt.Members.TryGetC(member) }));

            return sentences;
        }
    }

    public class AssemblyChanges
    {
        public LocalizedAssembly LocalizedAssembly { get; set; }

        public List<TypeChanges> Types { get; set; }

        public override string ToString()
        {
            return "Changes for {0}".Formato(LocalizedAssembly.ToString());
        }
    }

    public class TypeChanges
    {
        public LocalizedType Type { get; set; }

        public Dictionary<CultureInfo, TypeConflict> TypeConflict;

        public Dictionary<string, Dictionary<CultureInfo, MemberConflict>> MemberConflicts { get; set; }

        public override string ToString()
        {
            return "Changes for {0}".Formato(Type);
        }
    }

    public class TypeConflict
    {
        public LocalizedType Original;
        public string Translated;

        public override string ToString()
        {
            return "Conflict {0} -> {1}".Formato(Original, Translated);
        }
    }

    public class MemberConflict
    {
        public string Original;
        public string Translated;


        public override string ToString()
        {
            return "Conflict {0} -> {1}".Formato(Original, Translated);
        }
    }
}