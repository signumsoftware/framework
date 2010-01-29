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

        internal static void Syncronize(this XElement loaded, XElement created, XName collectionElementName, XName elementName, XName elementKeyAttribute,
            Func<string, XElement, XElement, XElement> update,
            Action<SyncAction, string> notify)
        {
            XElement loadedCollection = loaded.Element(collectionElementName) ?? new XElement(collectionElementName);
            XElement createdCollection = created.Element(collectionElementName) ?? new XElement(collectionElementName);

            Dictionary<string, XElement> loadedDictionary = loaded.Elements(elementName).ToDictionary(a => a.Attribute(elementKeyAttribute).Value);
            HashSet<string> createdKeys = createdCollection.Elements(elementName).Select(a => a.Attribute(elementKeyAttribute).Value).ToHashSet();

            foreach (var k in loadedDictionary.Keys.Except(createdKeys))
                notify(SyncAction.Removed, k);

            var result = created.Elements(collectionElementName).Select(elem =>
            {
                string key = elem.Attribute(elementKeyAttribute).Value;
                XElement load;
                if (loadedDictionary.TryGetValue(key, out load))
                {
                    var updated = update(key, load, elem);
                    if (updated == load)
                    {
                        notify(SyncAction.Updated, key);
                        return updated;
                    }

                    return load;
                }
                else
                {
                    notify(SyncAction.Added, key);
                    return elem;
                }
            }).ToList();

            if (!loadedDictionary.Keys.Zip(createdKeys, (x, y) => x == y).All(b => b) &&
                 loadedDictionary.Keys.ToHashSet().SetEquals(createdKeys))
            {
                notify(SyncAction.OrderChanged, null);
            }

            if (result.Count == 0)
                loadedCollection.Remove();
            else
                loadedCollection.ReplaceNodes(result);
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
            replacements.AskForReplacements(oldDictionary, newDictionary, replacementsKey);

            var repOldDictionary = replacements.ApplyReplacements(oldDictionary, replacementsKey);

            HashSet<string> set = new HashSet<string>();
            set.UnionWith(repOldDictionary.Keys);
            set.UnionWith(newDictionary.Keys);

            foreach (var key in set)
            {
                var oldVal = repOldDictionary.TryGetC(key);
                var newVal = newDictionary.TryGetC(key);

                if (oldVal == null)
                    createNew(key, newVal);

                if (newVal == null)
                    removeOld(key, oldVal);

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
