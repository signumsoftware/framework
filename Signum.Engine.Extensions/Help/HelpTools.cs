using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Xml.Linq;

namespace Signum.Engine.Help
{
    static class HelpTools
    {
        internal static Dictionary<K, V> CollapseDictionary<K, V>(this IEnumerable<KeyValuePair<K, V>> collection)
        {
            var dic = collection.ToDictionary();
            if (dic.Count == 0)
                return null;

            return dic;
        }

        internal static void Syncronize(XElement created, XElement loaded,
            XName collectionElementName, XName elementName, XName elementKeyAttribute, string replacementsKey,
            Func<string, XElement, XElement, bool> update, Action<SyncAction, string> notify)
        {
            Dictionary<string, XElement> loadedDictionary = (loaded.Element(collectionElementName) ?? new XElement(collectionElementName))
                .Elements(elementName).ToDictionary(a => a.Attribute(elementKeyAttribute).Value);
            Dictionary<string, XElement> createdDictionary = (created.Element(collectionElementName) ?? new XElement(collectionElementName))
                .Elements(elementName).ToDictionary(a => a.Attribute(elementKeyAttribute).Value);

            Replacements replacements = new Replacements();

            replacements.AskForReplacements(
                loadedDictionary.Keys.ToHashSet(), 
                createdDictionary.Keys.ToHashSet(), replacementsKey);

            var repLoadedDictionary = replacements.ApplyReplacementsToOld(loadedDictionary, replacementsKey);

            foreach (var k in loadedDictionary.Keys.Except(createdDictionary.Keys))
                notify(SyncAction.Removed, k);

            foreach (var elem in createdDictionary.Values)
            {
                string key = elem.Attribute(elementKeyAttribute).Value;
                XElement load;
                if (repLoadedDictionary.TryGetValue(key, out load))
                {
                    if (update(key, elem, load))
                    {
                        notify(SyncAction.Updated, key);
                    }
                }
                else
                {
                    notify(SyncAction.Added, key);
                }
            }

            if (!loadedDictionary.Keys.Zip(createdDictionary.Keys, (x, y) => x == y).All(b => b) &&
                 loadedDictionary.Keys.ToHashSet().SetEquals(createdDictionary.Keys))
            {
                notify(SyncAction.OrderChanged, null);
            }

            var col = created.Element(collectionElementName);
            if (col != null && col.Elements().Count() == 0)
                col.Remove();
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
                    createNew(key, newVal);
                else if (newVal == null)
                    removeOld(key, oldVal);
                else
                    merge(key, oldVal, newVal);
            }
        }
    }

    internal enum SyncAction
    {
        Added,
        Removed,
        Updated,
        OrderChanged,
    }
}
