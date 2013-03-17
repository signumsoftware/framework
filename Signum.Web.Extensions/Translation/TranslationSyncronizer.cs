using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using Signum.Utilities;
using Signum.Web.Localization;

namespace Signum.Web.Localization
{
    public static class TranslationSyncronizer
    {
        public static AssemblyChanges GetAssemblyChanges(ITranslator translator, LocalizedAssembly target, LocalizedAssembly master, List<LocalizedAssembly> support)
        {
            var types = master.Types.Keys.Select(type =>
            {
                LocalizedType targetType = target.Types.TryGetC(type);

                LocalizedType masterType = master.Types[type];
                List<LocalizedType> supportTypes = support.Select(la => la.Types.TryGetC(type)).NotNull().ToList();

                List<Conflict> conflicts = null;

                AddConflict(ref conflicts, new LocalizationMember(DescriptionOptions.Description), lt => lt.Description, targetType, masterType, supportTypes);
                AddConflict(ref conflicts, new LocalizationMember(DescriptionOptions.PluralDescription), lt => lt.Description, targetType, masterType, supportTypes);

                foreach (var k in masterType.Members.Keys)
                    AddConflict(ref conflicts, new LocalizationMember(DescriptionOptions.Members) { MemberName = k }, lt => lt.Members.TryGetC(k), targetType, masterType, supportTypes);

                if (conflicts == null)
                    return null;

                return new TypeChanges { Type = type, Conflicts = conflicts };
            }).NotNull().ToList();

            return new AssemblyChanges
            {
                 Target = target,
                 Types = types,
            };
        }

        private static void AddConflict(ref List<Conflict> conflicts, LocalizationMember member, Func<LocalizedType, string> memberAccesor, LocalizedType target, LocalizedType master, List<LocalizedType> support)
        {
            var masterMember = memberAccesor(master);

            if (masterMember == null)
                return;

            if (target != null && memberAccesor(target) != null)
                return;

            var sentences = new Dictionary<LocalizedAssembly, string>();

            sentences.Add(master.Assembly, masterMember);

            sentences.AddRange(from lt in support
                               let str = memberAccesor(lt)
                               where str != null
                               select KVP.Create(lt.Assembly, str));


            if (conflicts == null)
                conflicts = new List<Conflict>();

            conflicts.Add(new Conflict(member)
            {
                Sentences = sentences,
            });
        }
    }

    public class AssemblyChanges
    {
        public LocalizedAssembly Target { get; set; }

        public List<TypeChanges> Types { get; set; }
    }

    public class TypeChanges
    {
        public Type Type { get; set; }

        public List<Conflict> Conflicts { get; set; }
    }

    public class Conflict
    {
        public Conflict(LocalizationMember member)
        {
            this.Member = member;
        }

        public LocalizationMember Member { get; set; }

        public Dictionary<LocalizedAssembly, string> Sentences { get; set; }
    }

    public class LocalizationMember
    {
        public LocalizationMember(DescriptionOptions options)
        {
            this.Option = options;
        }
        public DescriptionOptions Option { get; private set; }

        public string MemberName { get; set; }
    }
}