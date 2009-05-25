using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Collections;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Entities;
using System.Linq;

namespace Signum.Entities.Reflection
{
    public static class GraphExplorer
    {
        public static void PropagateModifications(DirectedGraph<Modifiable> inverseGraph)
        {
            if (inverseGraph == null)
                throw new ArgumentNullException("inverseGraph");

            foreach (Modifiable item in inverseGraph)
                if (item.SelfModified && !(item is IdentifiableEntity))
                    Propagate(item, inverseGraph); 
        }

        private static void Propagate(Modifiable item, DirectedGraph<Modifiable> inverseGraph)
        {
            item.Modified = true;
            if (!(item is IdentifiableEntity))
                foreach (var other in inverseGraph.RelatedTo(item))
                    Propagate(other, inverseGraph);
        }

        public static DirectedGraph<Modifiable> FromRoot(Modifiable raiz)
        {
            return DirectedGraph<Modifiable>.Generate(raiz, ModifyInspector.FullExplore);
        }

        internal static string GraphIntegrityCheck(this Modifiable modifiable, Func<Modifiable, IEnumerable<Modifiable>> explorer)
        {
            DirectedGraph<Modifiable> eg = DirectedGraph<Modifiable>.Generate(modifiable, explorer);

            string result = eg.Select(m => new { m, Error = m.IntegrityCheck() })
                .Where(p => p.Error.HasText())
                .ToString(p => "{0}\r\n{1}".Formato(eg.ShortestPath(modifiable, p.m).PreAnd(modifiable).ToString(e=>e.ToString().DefaultText("[...]"), "->"), p.Error.Indent(3)), "\r\n");

            return result;
        }

        internal static Dictionary<Modifiable, string> GraphIntegrityCheckDictionary(this Modifiable modificable, Func<Modifiable, IEnumerable<Modifiable>> explorer)
        {
            DirectedGraph<Modifiable> eg = DirectedGraph<Modifiable>.Generate(modificable, explorer);

            var result = eg.Select(m => new { m, Error = m.IntegrityCheck() })
                .Where(p => p.Error.HasText()).ToDictionary(a=>a.m, a=>a.Error);

            return result; 
        }

        public static string Integrity(DirectedGraph<Modifiable> modifiable)
        {
            return modifiable.OfType<IdentifiableEntity>().Select(ident =>
            {
                bool allowCorruption = ident is ICorrupt && ((ICorrupt)ident).Corrupt;
                using (allowCorruption ? Corruption.Allow() : null)
                {
                    return ident.IdentifiableIntegrityCheck();
                }
            }).Where(e => e.HasText()).ToString("\r\n");
        }

        public static void PreSaving(DirectedGraph<Modifiable> modifiable)
        {
            modifiable.ForEach(m => m.PreSaving());
        }
    }
}
