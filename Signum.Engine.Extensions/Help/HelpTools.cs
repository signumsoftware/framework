using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Utilities;
using System.Xml.Linq;

namespace Signum.Engine.Help
{
    static class HelpTools
    {
        internal static Dictionary<K, V>? CollapseDictionary<K, V>(this IEnumerable<KeyValuePair<K, V>> collection)
            where K : notnull
        {
            var dic = collection.ToDictionary();
            if (dic.Count == 0)
                return null;

            return dic;
        }

        internal static void SynchronizeElements<T>(XElement loaded, XName collectionElementName, XName elementName, XName elementKeyAttribute, 
            Dictionary<string, T> should, string replacementsKey, Action<T, XElement> update, Action<SyncAction, string> notify) where T:class
        {
            Replacements replacements = new Replacements();

            var collection = loaded.Element(collectionElementName);

            if (collection == null)
                return;

            Dictionary<string, XElement> loadedDictionary = collection.Elements(elementName).ToDictionary(a => a.Attribute(elementKeyAttribute).Value);

            if (loadedDictionary.IsEmpty())
                return;

            replacements.AskForReplacements(
                 loadedDictionary.Keys.ToHashSet(),
                 should.Keys.ToHashSet(), replacementsKey);

            var reps = replacements.TryGetC(replacementsKey);

            foreach (var kvp in loadedDictionary)
            {
                var key = kvp.Value.Attribute(elementKeyAttribute).Value;

                var newKey = reps?.TryGetC(key);

                if (newKey != null)
                {
                    notify(SyncAction.Renamed, newKey);
                    key = newKey;
                }

                T? val = should.TryGetC(key);

                if (val == null)
                {
                    notify(SyncAction.Removed, key);
                }
                else
                {
                    update(val, kvp.Value);
                }
            }
        }

        public static void SynchronizeReplacing<O, N>(
             Replacements replacements, string replacementsKey,
             Dictionary<string, O> oldDictionary,
             Dictionary<string, N> newDictionary,
             Action<string, O> removeOld,
             Action<string, N> createNew,
             Action<string, O, N> merge)
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
                    createNew(key, newVal!);
                else if (newVal == null)
                    removeOld(key, oldVal);
                else
                    merge(key, oldVal, newVal);
            }
        }
    }

    internal enum SyncAction
    {
        Removed,
        Renamed,
    }
}
