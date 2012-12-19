using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Maps;
using Signum.Utilities.DataStructures;

namespace Signum.Engine
{
    public static class Synchronizer
    {
        public static void Synchronize<K, N, O>(
          Dictionary<K, N> newDictionary,
          Dictionary<K, O> oldDictionary,
          Action<K, N> createNew,
          Action<K, O> removeOld,
          Action<K, N, O> merge)
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
                    if (createNew != null) 
                        createNew(key, newVal);
                }
                else if (newVal == null)
                {
                    if (removeOld != null) 
                        removeOld(key, oldVal);
                }
                else
                {
                    if (merge != null) 
                        merge(key, newVal, oldVal);
                }
            }
        }

        public static void SynchronizeReplacing<N, O>(
          Replacements replacements,
          string replacementsKey, 
          Dictionary<string, N> newDictionary,
          Dictionary<string, O> oldDictionary,
          Action<string, N> createNew,
          Action<string, O> removeOld,
          Action<string, N, O> merge)
            where O : class
            where N : class
        {
            replacements.AskForReplacements(
                oldDictionary.Keys.ToHashSet(), 
                newDictionary.Keys.ToHashSet(), replacementsKey);

            var repOldDictionary = replacements.ApplyReplacementsToOld(oldDictionary, replacementsKey);

            HashSet<string> set = new HashSet<string>();
            set.UnionWith(repOldDictionary.Keys);
            set.UnionWith(newDictionary.Keys);
            foreach (var key in set)
            {
                var oldVal = repOldDictionary.TryGetC(key);
                var newVal = newDictionary.TryGetC(key);

                if (oldVal == null)
                {
                    if (createNew != null)
                        createNew(key, newVal);
                }
                else if (newVal == null)
                {
                    if (removeOld != null)
                        removeOld(key, oldVal);
                }
                else
                {
                    if (merge != null)
                        merge(key, newVal, oldVal);
                }
            }
        }

        public static SqlPreCommand SynchronizeScript<K, N, O>(
            Dictionary<K, N> newDictionary, 
            Dictionary<K, O> oldDictionary, 
            Func<K, N, SqlPreCommand> createNew, 
            Func<K, O, SqlPreCommand> removeOld,
            Func<K, N, O, SqlPreCommand> merge, Spacing spacing)
            where O : class
            where N : class
        {
            return newDictionary.OuterJoinDictionaryCC(oldDictionary, (key, newVal, oldVal) =>
            {
                if (newVal == null)
                    return removeOld == null ? null : removeOld(key, oldVal);

                if (oldVal == null)
                    return createNew == null ? null : createNew(key, newVal);

                return merge == null ? null : merge(key, newVal, oldVal);
            }).Values.Combine(spacing);
        }

      

        public static SqlPreCommand SynchronizeScriptReplacing<N, O>(
            Replacements replacements, 
            string replacementsKey, 
            Dictionary<string, N> newDictionary, 
            Dictionary<string, O> oldDictionary, 
            Func<string, N, SqlPreCommand> createNew, 
            Func<string, O, SqlPreCommand> removeOld, 
            Func<string, N, O, SqlPreCommand> merge, 
            Spacing spacing)
            where O : class
            where N : class
        {
            replacements.AskForReplacements(
                oldDictionary.Keys.ToHashSet(), 
                newDictionary.Keys.ToHashSet(), replacementsKey);

            var repOldDictionary = replacements.ApplyReplacementsToOld(oldDictionary, replacementsKey);

            return newDictionary.OuterJoinDictionaryCC(repOldDictionary, (key, newVal, oldVal) =>
            {
                if (oldVal == null)
                    return createNew == null ? null : createNew(key, newVal);

                if (newVal == null)
                    return removeOld == null ? null : removeOld(key, oldVal);

                return merge == null ? null : merge(key, newVal, oldVal);
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

        public bool Interactive = true; 

        public string Apply(string replacementsKey, string textToReplace)
        {
            Dictionary<string, string> repDic = this.TryGetC(replacementsKey);

            return repDic.TryGetC(textToReplace) ?? textToReplace;
        }

        public virtual Dictionary<string, O> ApplyReplacementsToOld<O>(Dictionary<string, O> oldDictionary, string replacementsKey)
        {
            if (!this.ContainsKey(replacementsKey))
                return oldDictionary;

            Dictionary<string, string> replacements = this[replacementsKey];

            return oldDictionary.SelectDictionary(a => replacements.TryGetC(a) ?? a, v => v);
        }

        public virtual Dictionary<string, O> ApplyReplacementsToNew<O>(Dictionary<string, O> newDictionary, string replacementsKey)
        {
            if (!this.ContainsKey(replacementsKey))
                return newDictionary;

            Dictionary<string, string> replacements = this[replacementsKey].Inverse();

            return newDictionary.SelectDictionary(a => replacements.TryGetC(a) ?? a, v => v);
        }

        public virtual void AskForReplacements(
             HashSet<string> oldKeys,
             HashSet<string> newKeys,
             string replacementsKey)
        {
            List<string> oldOnly = oldKeys.Where(k => !newKeys.Contains(k)).ToList();
            List<string> newOnly = newKeys.Where(k => !oldKeys.Contains(k)).ToList();

            if (oldOnly.Count == 0 || newOnly.Count == 0)
                return;

            StringDistance sd = new StringDistance();

            Dictionary<string, Dictionary<string, float>> distances = oldOnly.ToDictionary(o => o, o => newOnly.ToDictionary(n => n, n =>
            {
                return Distance(sd, o, n);
            }));

            Dictionary<string, string> replacements = new Dictionary<string, string>();

            while (oldOnly.Count > 0 && newOnly.Count > 0)
            {
                var old = distances.WithMin(kvp=>kvp.Value.Values.Min());

                Selection selection = !Interactive ? new Selection(old.Key, old.Value.WithMin(a => a.Value).Key) :
                                        SelectInteractive(old.Value.OrderBy(a => a.Value).Select(a => a.Key).ToList(), old.Key, replacementsKey);

                oldOnly.Remove(selection.OldValue);
                distances.Remove(selection.OldValue);

                if (selection.NewValue != null)
                {
                    replacements.Add(selection.OldValue, selection.NewValue);

                    newOnly.Remove(selection.NewValue);

                    foreach (var dic in distances.Values)
                        dic.Remove(selection.NewValue);
                }
            }

            if (replacements.Count != 0)
                this.Add(replacementsKey, replacements);
        }

        static float Distance(StringDistance sd, string o, string n)
        {
            int lcs = sd.LongestCommonSubsequence(o, n);

            int max = Math.Max(o.Length, n.Length);

            return max / (lcs + 4f);
        }

        const int MaxElements = 70;

        private static Selection SelectInteractive(List<string> newValues, string oldValue, string replacementsKey)
        {
            int startingIndex = 0;

            Console.WriteLine(Properties.Resources._0HasBeenRenamedIn1.Formato(oldValue, replacementsKey));
          retry:
            newValues.Skip(startingIndex).Take(MaxElements)
                .Select((s, i) => "-{0}{1,2}: {2} ".Formato(i + startingIndex == 0 ? ">" : " ", i + startingIndex, s)).ToConsole();
            Console.WriteLine();

            Console.WriteLine(Properties.Resources.NNone);

            int remaining = newValues.Count - startingIndex - MaxElements;
            if (remaining > 0)
                SafeConsole.WriteLineColor(ConsoleColor.White, "- s: Show more values ({0} remaining)", remaining);

            while (true)
            {
                string answer = Console.ReadLine().ToLower();

                if (answer == "s" && remaining > 0)
                {
                    startingIndex += MaxElements;
                    goto retry;
                }
                if (answer == "n")
                    return new Selection(oldValue, null);

                if (answer == "")
                    return new Selection(oldValue, newValues[0]);

                int option = 0;
                if (int.TryParse(answer, out option))
                    return new Selection(oldValue, newValues[option]);

                Console.WriteLine("Error");
            }
        }

        public struct Selection
        {
            public Selection(string oldValue, string newValue)
            {
                this.OldValue = oldValue;
                this.NewValue = newValue;
            }

            public readonly string OldValue;
            public readonly string NewValue;
        }
    }
}
