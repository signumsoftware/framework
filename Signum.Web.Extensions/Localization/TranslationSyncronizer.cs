using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Signum.Utilities;
using Signum.Web.Localization;

namespace Signum.Web.Extensions.Localization
{
    public static class TranslationSyncronizer
    {
        public static void GetChanges(ITranslator translator, LocalizedAssembly target, LocalizedAssembly master, LocalizedAssembly[] support)
        {


        }
    }

    public class LocalizationToken
    {
        public LocalizedType Type { get; set; }

        public DescriptionOptions Option { get; set; }

        public string MemberName { get; set; }
    }

    public class Changes
    {
        public LocalizedAssembly Target { get; set; }

        public List<LocalizationToken> ToRemove { get; set; }

        public List<Conflict> Conflicts { get; set; }
    }

    public class Conflict
    {
        public LocalizationToken Token { get; set; }

        public string Value { get; set; }

        public Dictionary<LocalizedAssembly, string> Sentences { get; set; }
    }
}