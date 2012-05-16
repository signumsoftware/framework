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
        public static SqlPreCommand SynchronizeScript<K, O, N>(
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

        public static void Synchronize<K, O, N>(
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

        public static SqlPreCommand SynchronizeReplacing<O, N>(
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

        public bool Interactive = true; 

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
            List<string> oldOnly = oldDictionary.Keys.Where(k => !newDictionary.ContainsKey(k)).ToList();
            List<string> newOnly = newDictionary.Keys.Where(k => !oldDictionary.ContainsKey(k)).ToList();

            StringDistance sd = new StringDistance();
            var distances = oldOnly.ToDictionary(a => a, a => newOnly.ToDictionary(b => b, b => sd.LongestCommonSubsequence(a, b)));

            Solution bestSolution = Solution.Empty; 
            Action<int, Solution> findMinimumPermutation = null;


            findMinimumPermutation = (pos, current) =>
            {
                if (pos == oldOnly.Count)
                {
                    if (bestSolution.Sum > current.Sum)
                        bestSolution = current;

                    return;
                }
                else
                {
                    if (bestSolution.Sum < current.Sum)
                        return;

                    string old = oldOnly[pos];
                    var dist = distances[old]; 

                    for (int i = 0; i < newOnly.Count; i++)
                    {
                        var @new = newOnly[i];
                        if (!current.Selections.Any(a=>a.NewValue == @new))
                            findMinimumPermutation(pos + 1, new Solution(current.Selections.Push(new Selection(old,  @new)), current.Sum + dist[@new]));
                    }

                    if ((oldOnly.Count - pos) > (newOnly.Count - current.Selections.Take(pos).Count(a=>a.NewValue != null)))
                        findMinimumPermutation(pos + 1, new Solution(current.Selections.Push(new Selection(old, (string)null)), current.Sum));
                }
            };

            findMinimumPermutation(0, Solution.Empty);
         
            Dictionary<string, string> replacements = new Dictionary<string, string>();

            while (oldOnly.Count > 0 && newOnly.Count > 0)
            {
                Selection selection = bestSolution.Selections.Last(a=>a.NewValue != null);

                if (Interactive)
                {
                    List<string> sms = newOnly.OrderByDescending(n => distances[selection.OldValue][n]).ToList();
            
                    selection = SelectInteractive(sms, selection, replacementsKey);
                }

                replacements.Add(selection.OldValue, selection.NewValue);
                oldOnly.RemoveAt(0);
                newOnly.Remove(selection.NewValue);
            }

            if (replacements.Count != 0)
                this.Add(replacementsKey, replacements);
        }

        private static Selection SelectInteractive(List<string> sms, Selection selection, string replacementsKey)
        {
            Console.WriteLine(Properties.Resources._0HasBeenRenamedIn1.Formato(selection.OldValue, replacementsKey));
            sms.Select((s, i) => "-{0}{1}: {2} ".Formato(s == selection.NewValue ? ">" : " ", i, s)).ToConsole();
            Console.WriteLine();
            Console.WriteLine(Properties.Resources.NNone);

            while (true)
            {
                string answer = Console.ReadLine().ToLower();
                int option = 0;
                if (answer == "n")
                    return new Selection(selection.OldValue, null);

                if (answer == "")
                    return selection;

                if (int.TryParse(answer, out option))
                    return new Selection(selection.OldValue, sms[option]);

                Console.WriteLine("Error");
            }
        }

        public struct Solution
        {
            public Solution(ImmutableStack<Selection> selections, int sum)
            {
                this.Selections = selections;
                this.Sum = sum;
            }

            public readonly ImmutableStack<Selection> Selections;
            public readonly int Sum;

            public static readonly Solution Empty = new Solution(ImmutableStack<Selection>.Empty, int.MaxValue);
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
