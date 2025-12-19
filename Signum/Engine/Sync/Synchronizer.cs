using Signum.Engine.Maps;

namespace Signum.Engine.Sync;

public static class Synchronizer
{
    public static void Synchronize<K, N, O>(
      Dictionary<K, N> newDictionary,
      Dictionary<K, O> oldDictionary,
      Action<K, N>? createNew,
      Action<K, O>? removeOld,
      Action<K, N, O>? merge)
        where K : notnull
    {
        HashSet<K> keys = new HashSet<K>();
        keys.UnionWith(oldDictionary.Keys);
        keys.UnionWith(newDictionary.Keys);

        foreach (var key in keys)
        {
            var oldExists = oldDictionary.TryGetValue(key, out var oldVal);
            var newExists = newDictionary.TryGetValue(key, out var newVal);

            if (!oldExists)
            {
                createNew?.Invoke(key, newVal!);
            }
            else if (!newExists)
            {
                removeOld?.Invoke(key, oldVal!);
            }
            else
            {
                merge?.Invoke(key, newVal!, oldVal!);
            }
        }
    }

    public static async Task SynchronizeAsync<K, N, O>(
      Dictionary<K, N> newDictionary,
      Dictionary<K, O> oldDictionary,
      Func<K, N, Task>? createNew,
      Func<K, O, Task>? removeOld,
      Func<K, N, O, Task>? merge)
        where K : notnull
    {
        HashSet<K> keys = new HashSet<K>();
        keys.UnionWith(oldDictionary.Keys);
        keys.UnionWith(newDictionary.Keys);

        foreach (var key in keys)
        {
            var oldExists = oldDictionary.TryGetValue(key, out var oldVal);
            var newExists = newDictionary.TryGetValue(key, out var newVal);

            if (!oldExists)
            {
                if (createNew != null)
                    await createNew.Invoke(key, newVal!);
            }
            else if (!newExists)
            {
                if (removeOld != null)
                    await removeOld.Invoke(key, oldVal!);
            }
            else
            {
                if (merge != null)
                    await merge.Invoke(key, newVal!, oldVal!);
            }
        }
    }

    public static void SynchronizeProgressForeach<K, N, O>(
      Dictionary<K, N> newDictionary,
      Dictionary<K, O> oldDictionary,
      Action<K, N>? createNew,
      Action<K, O>? removeOld,
      Action<K, N, O>? merge,
      bool showProgress = true,
      bool transactional = true)
        where K : notnull
    {
        HashSet<K> keys = new HashSet<K>();
        keys.UnionWith(oldDictionary.Keys);
        keys.UnionWith(newDictionary.Keys);
        keys.ProgressForeach(key => key.ToString()!, key =>
        {
            if (oldDictionary.TryGetValue(key, out var oldVal))
            {
                if (newDictionary.TryGetValue(key, out var newVal))
                    merge?.Invoke(key, newVal, oldVal);
                else
                    removeOld?.Invoke(key, oldVal);
            }
            else
            {
                if (newDictionary.TryGetValue(key, out var newVal))
                    createNew?.Invoke(key, newVal);
                else
                    throw new InvalidOperationException("Unexpected key: " + key);
            }
        },
        showProgress: showProgress,
        transactional: transactional);
    }

    public static void SynchronizeReplacing<N, O>(
      Replacements replacements,
      string? replacementsKey,
      Dictionary<string, N> newDictionary,
      Dictionary<string, O> oldDictionary,
      Action<string, N>? createNew,
      Action<string, O>? removeOld,
      Action<string, N, O>? merge)
    {
        var repOldDictionary = oldDictionary;

        if (replacementsKey != null)
        {
            replacements.AskForReplacements(
                oldDictionary.Keys.ToHashSet(),
                newDictionary.Keys.ToHashSet(),
                replacementsKey);

            repOldDictionary = replacements.ApplyReplacementsToOld(oldDictionary, replacementsKey);
        }

        HashSet<string> set = new HashSet<string>();
        set.UnionWith(repOldDictionary.Keys);
        set.UnionWith(newDictionary.Keys);
        foreach (var key in set)
        {
            if (repOldDictionary.TryGetValue(key, out var oldVal))
            {
                if (newDictionary.TryGetValue(key, out var newVal))
                    merge?.Invoke(key, newVal, oldVal);
                else
                    removeOld?.Invoke(key, oldVal);
            }
            else
            {
                if (newDictionary.TryGetValue(key, out var newVal))
                    createNew?.Invoke(key, newVal);
                else
                    throw new InvalidOperationException("Unexpected key: " + key);
            }
        }
    }

    public static SqlPreCommand? SynchronizeScript<K, N, O>(
        Spacing spacing,
        Dictionary<K, N> newDictionary,
        Dictionary<K, O> oldDictionary,
        Func<K, N, SqlPreCommand?>? createNew,
        Func<K, O, SqlPreCommand?>? removeOld,
        Func<K, N, O, SqlPreCommand?>? mergeBoth)
        where O : class
        where N : class
        where K : notnull
    {
        HashSet<K> set = new HashSet<K>();
        set.UnionWith(newDictionary.Keys);
        set.UnionWith(oldDictionary.Keys);

        var list = set.Select(key =>
        {
            var newVal = newDictionary.TryGetC(key);
            var oldVal = oldDictionary.TryGetC(key);

            if (newVal == null)
                return removeOld == null ? null : removeOld(key, oldVal!);

            if (oldVal == null)
                return createNew == null ? null : createNew(key, newVal);

            return mergeBoth == null ? null : mergeBoth(key, newVal, oldVal);
        }).ToList();

        return list.Combine(spacing);
    }



    public static SqlPreCommand? SynchronizeScriptReplacing<N, O>(
        Replacements replacements,
        string replacementsKey,
        Spacing spacing,
        Dictionary<string, N> newDictionary,
        Dictionary<string, O> oldDictionary,
        Func<string, N, SqlPreCommand?>? createNew,
        Func<string, O, SqlPreCommand?>? removeOld,
        Func<string, N, O, SqlPreCommand?>? mergeBoth)
        where O : class
        where N : class
    {
        replacements.AskForReplacements(
            oldDictionary.Keys.ToHashSet(),
            newDictionary.Keys.ToHashSet(), replacementsKey);

        var repOldDictionary = replacements.ApplyReplacementsToOld(oldDictionary, replacementsKey);

        return SynchronizeScript(spacing, newDictionary, repOldDictionary, createNew, removeOld, mergeBoth);
    }

    public static IDisposable? UseOldTableName(Table table, Replacements replacements)
    {
        var tableName = table.Name.ToString();
        if (replacements.ReplaceDatabaseName != null)
            tableName = tableName.Replace(replacements.ReplaceDatabaseName, Connector.Current.DatabaseName());

        string? fullName = replacements.TryGetC(Replacements.KeyTablesInverse)?.TryGetC(tableName);
        if (fullName == null)
            return null;

        ObjectName originalName = table.Name;

        table.Name = ObjectName.Parse(fullName, Schema.Current.Settings.IsPostgres);

        return new Disposable(() => table.Name = originalName);
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

    public static string KeyEnumsForTable(string tableName)
    {
        return "Enums:" + tableName;
    }

    public bool Interactive = true;
    public bool SchemaOnly = false;
    public string? ReplaceDatabaseName = null;

    public IDisposable? WithReplacedDatabaseName()
    {
        if (ReplaceDatabaseName == null)
            return null;

        return ObjectName.OverrideOptions(new ObjectNameOptions { DatabaseNameReplacement = ReplaceDatabaseName });
    }

    public string Apply(string replacementsKey, string textToReplace)
    {
        Dictionary<string, string>? repDic = this.TryGetC(replacementsKey);

        return repDic?.TryGetC(textToReplace) ?? textToReplace;
    }

    public virtual Dictionary<string, O> ApplyReplacementsToOld<O>(Dictionary<string, O> oldDictionary, string replacementsKey)
    {
        if (!ContainsKey(replacementsKey))
            return oldDictionary;

        Dictionary<string, string> replacements = this[replacementsKey];

        return oldDictionary.SelectDictionary(a => replacements.TryGetC(a) ?? a, v => v);
    }

    public virtual Dictionary<string, O> ApplyReplacementsToNew<O>(Dictionary<string, O> newDictionary, string replacementsKey)
    {
        if (!ContainsKey(replacementsKey))
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

        Dictionary<string, string> replacements = this.TryGetC(replacementsKey) ?? new Dictionary<string, string>();

        if (replacements.Any())
        {
            var toRemove = replacements.Where(a => oldOnly.Contains(a.Key) && newOnly.Contains(a.Value)).ToList();
            foreach (var kvp in toRemove)
            {
                oldOnly.Remove(kvp.Key);
                newOnly.Remove(kvp.Value);
            }
        }


        if (oldOnly.Count == 0 || newOnly.Count == 0)
            return;

        StringDistance sd = new StringDistance();

        Dictionary<string, Dictionary<string, float>> distances = oldOnly.ToDictionary(o => o, o => newOnly.ToDictionary(n => n, n =>
        {
            return Distance(sd, o, n);
        }));


        bool alwaysNoRename = false;

        while (oldOnly.Count > 0 && newOnly.Count > 0)
        {
            var oldDist = distances.MinBy(kvp => kvp.Value.Values.Min());

            var alternatives = oldDist.Value.OrderBy(a => a.Value).Select(a => a.Key).ToList();

            Selection selection = SelectInteractive(oldDist.Key, alternatives, replacementsKey, Interactive, ref alwaysNoRename);

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

        if (replacements.Count != 0 && !ContainsKey(replacementsKey))
            this.GetOrCreate(replacementsKey).SetRange(replacements);
    }

    static float Distance(StringDistance sd, string o, string n)
    {
        return sd.LevenshteinDistance(o, n, weight: c => c.Type == StringDistance.ChoiceType.Substitute ? 2 : 1);
    }

    public string? SelectInteractive(string oldValue, ICollection<string> newValues, string replacementsKey, StringDistance sd)
    {
        if (newValues.Contains(oldValue))
            return oldValue;

        var rep = this.TryGetC(replacementsKey)?.TryGetC(oldValue);

        if (rep != null && newValues.Contains(rep))
            return rep;

        var dic = newValues.ToDictionary(a => a, a => Distance(sd, oldValue, a));

        bool temp = false;
        Selection sel = SelectInteractive(oldValue, dic.OrderBy(a => a.Value).Select(a => a.Key).ToList(), replacementsKey, Interactive, ref temp);

        if (sel.NewValue != null)
        {
            this.GetOrCreate(replacementsKey).Add(sel.OldValue, sel.NewValue);
        }

        return sel.NewValue;
    }

    public class AutoReplacementContext
    {
        public string ReplacementKey;
        public string OldValue;
        public List<string>? NewValues;

        public AutoReplacementContext(string replacementKey, string oldValue, List<string>? newValues)
        {
            ReplacementKey = replacementKey;
            OldValue = oldValue;
            NewValues = newValues;
        }
    }

    public static Func<AutoReplacementContext, Selection?>? GlobalAutoReplacement;
    public Func<AutoReplacementContext, Selection?>? AutoReplacement;
    public static Action<string, string, string?>? ResponseRecorder;//  replacementsKey,oldValue,newValue

    //public static Dictionary<String, Replacements.Selection>? cases ;
    public Selection SelectInteractive(string oldValue, List<string> newValues, string replacementsKey, bool interactive, ref bool alwaysNoRename)
    {
        var autoReplacement = AutoReplacement ?? GlobalAutoReplacement;
        if (autoReplacement != null)
        {
            Selection? selection = autoReplacement(new AutoReplacementContext(replacementsKey, oldValue, newValues));
            if (selection != null)
            {
                SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "AutoReplacement:");
                SafeConsole.WriteLineColor(ConsoleColor.DarkRed, " OLD " + selection.Value.OldValue);
                SafeConsole.WriteLineColor(ConsoleColor.DarkGreen, " NEW " + selection.Value.NewValue);
                return selection.Value;
            }
        }

        if (!interactive)
            throw new InvalidOperationException($"Unable to ask for renames for '{oldValue}' (in {replacementsKey}) without interactive console, consider using the Terminal application.");

        if (alwaysNoRename)
            return new Selection(oldValue, null);

        int startingIndex = 0;
        Console.WriteLine();
        SafeConsole.WriteLineColor(ConsoleColor.White, "   '{0}' has been renamed in {1}?".FormatWith(oldValue, replacementsKey));
    retry:
        int maxElement = Console.LargestWindowHeight - 7;

        int i = 0;
        foreach (var v in newValues.Skip(startingIndex).Take(maxElement).ToList())
        {
            SafeConsole.WriteColor(ConsoleColor.White, "{0,2}: ", i);
            SafeConsole.WriteColor(ConsoleColor.Gray, v);
            if (i == 0)
                SafeConsole.WriteColor(ConsoleColor.White, " (hit [Enter])");
            Console.WriteLine();
            i++;
        }

        SafeConsole.WriteColor(ConsoleColor.White, " n: ", i);
        SafeConsole.WriteColor(ConsoleColor.Gray, "No rename, '{0}' was removed", oldValue);
        Console.WriteLine();

        SafeConsole.WriteColor(ConsoleColor.White, " n!: ", i);
        SafeConsole.WriteColor(ConsoleColor.Gray, "Always no rename", oldValue);
        Console.WriteLine();

        int remaining = newValues.Count - startingIndex - maxElement;
        if (remaining > 0)
        {
            SafeConsole.WriteColor(ConsoleColor.White, " +: ", i);
            SafeConsole.WriteColor(ConsoleColor.Gray, "Show more values ({0} remaining)", remaining);
            Console.WriteLine();
        }

        while (true)
        {
            //var key = replacementsKey + "." + oldValue;
            //string? answer = cases!.ContainsKey(key) ? cases[key].NewValue:  Console.ReadLine();

            string answer = Console.ReadLine()!;

            if (answer == null)
                answer = "n";

            answer = answer.ToLower();


            Selection? response = null;
            if (answer == "+" && remaining > 0)
            {
                startingIndex += maxElement;
                goto retry;
            }
            if (answer == "n")
                response = new Selection(oldValue, null);

            if (answer == "n!")
            {
                alwaysNoRename = true;
                response = new Selection(oldValue, null);
            }

            if (answer == "")
                response = new Selection(oldValue, newValues[0]);

            if (int.TryParse(answer, out int option))
                response = new Selection(oldValue, newValues[option]);

            if (response != null)
            {
                if (ResponseRecorder != null)
                    ResponseRecorder.Invoke(replacementsKey, response.Value.OldValue, response.Value.NewValue);

                return response.Value;

            }

            Console.WriteLine("Error");


        }
    }

    public string ConcretizeObjectName(ObjectName objectName)
    {
        if (ReplaceDatabaseName != null)
            return objectName.ToString().Replace(ReplaceDatabaseName, Connector.Current.DatabaseName());

        return objectName.ToString();
    }

    public struct Selection
    {
        /// <param name="newValue">Null for removed</param>
        public Selection(string oldValue, string? newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public readonly string OldValue;

        public readonly string? NewValue;
    }

}
