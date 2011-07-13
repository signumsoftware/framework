using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;

namespace Signum.Entities
{
    public static class Audit
    {
        static List<Func<List<string>>> Algorithms = new List<Func<List<string>>>();

        public static List<AuditEntry> AllEntries()
        {
            return Algorithms.Select(a => new AuditEntry(a)).ToList();
        }

        public static List<AuditEntry> AllEntries(Func<MethodInfo, bool> filter)
        {
            return Algorithms.Where(a=>filter(a.Method)).Select(a => new AuditEntry(a)).ToList();
        }

        public static void Register(Func<List<string>> algorithm)
        {
            Algorithms.Add(algorithm);
        }
    }

    public class AuditEntry
    {
        public string Title;
        public TimeSpan Elapsed;

        public List<string> Entries;

        public AuditEntry(Func<List<string>> function)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Entries = function();
            sw.Stop();
            Elapsed = sw.Elapsed;
        }
    }
}
