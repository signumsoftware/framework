using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Maps;

namespace Signum.Engine
{
    public static class Synchronizer
    {
        public static SqlPreCommand SyncronizeCommands<K, O, N>(
            Dictionary<K, O> oldDictionary,
            Dictionary<K, N> newDictionary,
            Func<K, O, SqlPreCommand> removeOld,
            Func<K, N, SqlPreCommand> createNew,
            Func<K, O, N, SqlPreCommand> merge,
            Spacing spacing)
            where O : class
            where N : class
        {
            return oldDictionary.OuterJoinDictionaryCC(newDictionary, (key, oldVal, newVal) =>
            {
                if (oldVal == null)
                    return createNew == null ? null : createNew(key, newVal);

                if (newVal == null)
                    return removeOld == null ? null : removeOld(key, oldVal);

                return merge == null ? null : merge(key, oldVal, newVal);
            }).Values.Combine(spacing);
        }

        public static void Syncronize<K, O, N>(
            Dictionary<K, O> oldDictionary,
            Dictionary<K, N> newDictionary,
            Action<K, O> removeOld,
            Action<K, N> createNew,
            Action<K, O, N> merge)
            where O : class
            where N : class
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(oldDictionary.Keys);
            set.UnionWith(newDictionary.Keys);
            foreach (var key in set)
            {
                var oldVal = oldDictionary.TryGetC(key);
                var newVal = newDictionary.TryGetC(key);

                if (oldVal == null)
                {
                    if (createNew != null) createNew(key, newVal);
                }
                else if (newVal == null)
                {
                    if (removeOld != null) removeOld(key, oldVal);
                }
                else
                {
                    if (merge != null) merge(key, oldVal, newVal);
                }
            }
        }

        public static SqlPreCommand SyncronizeReplacing<O, N>(
            Replacements replacements, string replacementsKey,
            Dictionary<string, O> oldDictionary,
            Dictionary<string, N> newDictionary,
            Func<string, O, SqlPreCommand> removeOld,
            Func<string, N, SqlPreCommand> createNew,
            Func<string, O, N, SqlPreCommand> merge,
            Spacing spacing)
            where O : class
            where N : class
        {
            replacements.AskForReplacements(oldDictionary, newDictionary, replacementsKey);

            var repOldDictionary = replacements.ApplyReplacements(oldDictionary, replacementsKey);

            return repOldDictionary.OuterJoinDictionaryCC(newDictionary, (key, oldVal, newVal) =>
            {
                if (oldVal == null)
                    return createNew == null ? null : createNew(key, newVal);

                if (newVal == null)
                    return removeOld == null ? null : removeOld(key, oldVal);

                return merge == null ? null : merge(key, oldVal, newVal);
            }).Values.Combine(spacing);
        }

        public static IDisposable RenameTable(Table table, Replacements replacements)
        {
            string tempName = replacements.TryGetC(Replacements.KeyTablesInverse).TryGetC(table.Name) ?? table.Name;
            if (tempName == null) 
                return null;

            string realName = table.Name;
            table.Name = tempName;
            return new Disposable(() => table.Name = realName);
        }
    }

    public class Replacements : Dictionary<string, Dictionary<string, string>>
    {
        public static string KeyTables = "Tables";
        public static string KeyTablesInverse = "TablesInverse";
        public static string KeyColumnsForTable(string tableName)
        {
            return "Columns:" + tableName;
        }

        public string Apply(string replacementsKey, string textToReplace)
        {
            Dictionary<string, string> repDic = this.TryGetC(replacementsKey);

            return repDic.TryGetC(textToReplace) ?? textToReplace;
        }

        public virtual Dictionary<string, O> ApplyReplacements<O>(Dictionary<string, O> oldDictionary, string replacementsKey)
        {
            if (!this.ContainsKey(replacementsKey))
                return oldDictionary;

            Dictionary<string, string> replacements = this[replacementsKey];

            return oldDictionary.SelectDictionary(a => replacements.TryGetC(a) ?? a, v => v);
        }

        public virtual void AskForReplacements<O, N>(
             Dictionary<string, O> oldDictionary,
             Dictionary<string, N> newDictionary,
             string replacementsKey)
            where O : class
            where N : class
        {
        restart:
            var oldOnly = oldDictionary.Keys.Where(k => !newDictionary.ContainsKey(k)).ToList();
            var newOnly = newDictionary.Keys.Where(k => !oldDictionary.ContainsKey(k)).ToList();

            StringDistance sd = new StringDistance();

            Dictionary<string, string> replacements = new Dictionary<string, string>();

            while (oldOnly.Count > 0 && newOnly.Count > 0)
            {
                string old = oldOnly[0];

                List<string> sms = newOnly.OrderBy(n => sd.Distance(old, n)).ToList();
            retry:
                Console.WriteLine("'{0}' has been renamed?".Formato(old, replacementsKey));
                sms.Select((s, i) => "- {0}: {1} ".Formato(i, s)).ToConsole();
                Console.WriteLine();
                Console.WriteLine("- n: None");
                Console.WriteLine("- r: Restart");

                string answer = Console.ReadLine().ToLower();
                int option = 0;
                if (answer == "n")
                {
                    oldOnly.RemoveAt(0);
                }
                else if (answer == "r")
                {
                    goto restart;
                }
                else if (answer == "" || int.TryParse(answer, out option))
                {
                    replacements.Add(old, sms[option]);
                    oldOnly.RemoveAt(0);
                    newOnly.Remove(sms[option]);
                }
                else
                {
                    Console.WriteLine("Error");
                    goto retry;
                }
            }

            if (replacements.Count != 0)
                this.Add(replacementsKey, replacements);
        }
    }
}
