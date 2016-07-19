using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Collections;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Entities;
using System.Linq;
using System.Xml.Linq;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Signum.Entities.Reflection
{
    public static class GraphExplorer
    {
        public static void PropagateModifications(DirectedGraph<Modifiable> inverseGraph)
        {
            if (inverseGraph == null)
                throw new ArgumentNullException("inverseGraph");

            foreach (Modifiable item in inverseGraph)
                if (item.Modified == ModifiedState.SelfModified)
                    Propagate(item, inverseGraph);
        }

        static void Propagate(Modifiable item, DirectedGraph<Modifiable> inverseGraph)
        {
            if (item.Modified == ModifiedState.Modified)
                return;

            item.Modified = ModifiedState.Modified;
            foreach (var other in inverseGraph.RelatedTo(item))
                Propagate(other, inverseGraph);
        }

        public static void CleanModifications(IEnumerable<Modifiable> graph)
        {
            foreach (Modifiable item in graph.Where(a => a.Modified == ModifiedState.SelfModified || a.Modified == ModifiedState.Modified))
                item.Modified = ModifiedState.Clean;
        }

        public static DirectedGraph<Modifiable> FromRootIdentifiable(Modifiable root)
        {
            return DirectedGraph<Modifiable>.Generate(root, ModifyInspector.IdentifiableExplore, ReferenceEqualityComparer<Modifiable>.Default);
        }

        public static DirectedGraph<Modifiable> FromRoot(Modifiable root)
        {
            return DirectedGraph<Modifiable>.Generate(root, ModifyInspector.FullExplore, ReferenceEqualityComparer<Modifiable>.Default);
        }

        public static DirectedGraph<Modifiable> FromRoots<T>(IEnumerable<T> roots)
            where T : Modifiable
        {
            return DirectedGraph<Modifiable>.Generate(roots.Cast<Modifiable>(), ModifyInspector.FullExplore, ReferenceEqualityComparer<Modifiable>.Default);
        }

        public static DirectedGraph<Modifiable> FromRootsIdentifiable<T>(IEnumerable<T> roots)
            where T : Modifiable
        {
            return DirectedGraph<Modifiable>.Generate(roots.Cast<Modifiable>(), ModifyInspector.IdentifiableExplore, ReferenceEqualityComparer<Modifiable>.Default);
        }

        public static Dictionary<K, V> ToDictionaryOrNull<T, K, V>(this IEnumerable<T> collection, Func<T, K> keySelector, Func<T, V> nullableValueSelector) where V : class
        {
            Dictionary<K, V> result = null;

            foreach (var item in collection)
            {
                var val = nullableValueSelector(item);

                if (val != null)
                {
                    if (result == null)
                        result = new Dictionary<K, V>();

                    result.Add(keySelector(item), val);
                }
            }

            return result;
        }

        public static Dictionary<Guid, Dictionary<string, string>> IdentifiableIntegrityCheck(DirectedGraph<Modifiable> graph)
        {
            return graph.OfType<ModifiableEntity>().ToDictionaryOrNull(a=>a.temporalId, a=>a.IntegrityCheck());
        }

        public static Dictionary<Guid, Dictionary<string, string>> FullIntegrityCheck(DirectedGraph<Modifiable> graph)
        {
            AssertCloneAttack(graph);

            DirectedGraph<Modifiable> identGraph = DirectedGraph<Modifiable>.Generate(graph.Where(a => a is Entity), graph.RelatedTo);

            var identErrors = identGraph.OfType<Entity>().Select(ident => ident.EntityIntegrityCheck()).Where(errors => errors != null).SelectMany(errors => errors);

            var modErros = graph.Except(identGraph).OfType<ModifiableEntity>().Select(a => KVP.Create(a.temporalId, a.IntegrityCheck())); 

            return identErrors.Concat(modErros).ToDictionaryOrNull(a=>a.Key, a=>a.Value); 
        }

        static void AssertCloneAttack(DirectedGraph<Modifiable> graph)
        {
            var problems = (from m in graph.OfType<Entity>()
                            group m by new { Type = m.GetType(), Id = (m as Entity)?.Let(ident => (object)ident.IdOrNull) ?? (object)m.temporalId } into g
                            where g.Count() > 1 && g.Count(m => m.Modified == ModifiedState.SelfModified) > 0
                            select g).ToList();

            if (problems.Count == 0)
                return;


            throw new InvalidOperationException(
            "CLONE ATTACK!\r\n\r\n" + problems.ToString(p => "{0} different instances of the same entity ({1}) have been found:\r\n {2}".FormatWith(
                p.Count(),
                p.Key,
                p.ToString(m => "  {0}{1}".FormatWith(m.Modified, m), "\r\n")), "\r\n\r\n"));
        }

        public static DirectedGraph<Modifiable> PreSaving(Func<DirectedGraph<Modifiable>> recreate)
        {
            return PreSaving(recreate, (Modifiable m, ref bool graphModified) => 
            {
                ModifiableEntity me = m as ModifiableEntity;

                if (me != null)
                    me.SetTemporalErrors(null);

                m.PreSaving(ref graphModified);
            });
        }

        public delegate void ModifyEntityEventHandler(Modifiable m, ref bool graphModified);

        public static DirectedGraph<Modifiable> PreSaving(Func<DirectedGraph<Modifiable>> recreate, ModifyEntityEventHandler modifier)
        {
            DirectedGraph<Modifiable> graph = recreate();

            bool graphModified = false;
            foreach (var m in graph)
            {
                modifier(m, ref graphModified);
            }

            if (!graphModified)
                return graph; //common case

            do
            {
                var newGraph = recreate();
                graphModified = false;
                foreach (var m in newGraph.Except(graph))
                {
                    modifier(m, ref graphModified);
                }

                graph = newGraph;
            } while (graphModified);

            return graph;
        }


        static string[] colors = 
        {
             "aquamarine1",  "aquamarine4", "blue", "blueviolet",
             "brown4", "burlywood", "cadetblue1", "cadetblue",
             "chartreuse", "chocolate", "cornflowerblue",
             "darkgoldenrod", "darkolivegreen3", "darkorchid", "darkseagreen",
             "darkturquoise", "darkviolet", "deeppink", "deepskyblue", "forestgreen"
        };


        public static string SuperGraphviz(this DirectedGraph<Modifiable> modifiables)
        {
            Func<Type, string> color = t => colors[Math.Abs(t.FullName.GetHashCode()) % colors.Length];

            var listNodes = modifiables.Nodes.Select(n => new
            {
                Node = n,

                Fillcolor = n is Lite<Entity> ? "white" : color(n.GetType()),
                Color =
                    n is Lite<Entity> ? color(((Lite<Entity>)n).GetType()) :
                    (n.Modified == ModifiedState.SelfModified ? "red" :
                     n.Modified == ModifiedState.Modified ? "red4" :
                     n.Modified == ModifiedState.Sealed ? "gray" : "black"),

                Shape = n is Lite<Entity> ? "ellipse" :
                        n is Entity ? "ellipse" :
                        n is EmbeddedEntity ? "box" :
                        Reflector.IsMList(n.GetType()) ? "hexagon" : "plaintext",
                Style = n is Entity ? ", style=\"diagonals,filled,bold\"" :
                        n is Lite<Entity> ? "style=\"solid,bold\"" : "",

                Label = n.ToString().Etc(30, "..").RemoveDiacritics()

            }).ToList();

            string nodes = listNodes.ToString(t => "    {0} [color={1}, fillcolor={2} shape={3}{4}, label=\"{5}\"]".FormatWith(modifiables.Comparer.GetHashCode(t.Node), t.Color, t.Fillcolor, t.Shape, t.Style, t.Label), "\r\n");

            string arrows = modifiables.Edges.ToString(e => "    {0} -> {1}".FormatWith(modifiables.Comparer.GetHashCode(e.From), modifiables.Comparer.GetHashCode(e.To)), "\r\n");

            return "digraph \"Grafo\"\r\n{{\r\n    node [ style = \"filled,bold\"]\r\n\r\n{0}\r\n\r\n{1}\r\n}}".FormatWith(nodes, arrows);
        }

        public static DirectedGraph<Entity> ColapseIdentifiables(DirectedGraph<Modifiable> modifiables)
        {
            DirectedGraph<Entity> result = new DirectedGraph<Entity>(modifiables.Comparer);
            foreach (var item in modifiables.OfType<Entity>())
            {
                var toColapse = modifiables.IndirectlyRelatedTo(item, i => !(i is Entity));
                var toColapseFriends = toColapse.SelectMany(i => modifiables.RelatedTo(i).OfType<Entity>());
                result.Add(item, toColapseFriends);
                result.Add(item, modifiables.RelatedTo(item).OfType<Entity>());
            }
            return result;
        }

        public static XDocument EntityDGML(this DirectedGraph<Modifiable> graph)
        {
            return graph.ToDGML(n =>
                n is Entity ? GetAttributes((Entity)n) :
                n is Lite<Entity> ? GetAttributes((Lite<Entity>)n) :
                n is EmbeddedEntity ? GetAttributes((EmbeddedEntity)n) :
                n is MixinEntity ? GetAttributes((MixinEntity)n) :
                n.GetType().IsMList() ? GetAttributes((IList)n) :
                new[]
                {
                    new XAttribute("Label", n.ToString() ?? "[null]"),
                    new XAttribute("TypeName", n.GetType().TypeName()), 
                    new XAttribute("Background", ColorExtensions.ToHtmlColor(n.GetType().FullName.GetHashCode()))
                });
        }

        private static XAttribute[] GetAttributes(Entity ie)
        {
            return new[]
            {
               new XAttribute("Label", (ie.ToString() ?? "[null]")  + Modified(ie)),
               new XAttribute("TypeName", ie.GetType().TypeName()), 
               new XAttribute("Background", ColorExtensions.ToHtmlColor(ie.GetType().FullName.GetHashCode())),
               new XAttribute("Description", ie.IdOrNull?.ToString() ?? "New")
            };
        }

        private static string Modified(Modifiable ie)
        {
            return "({0})".FormatWith(ie.Modified);
        }

        private static XAttribute[] GetAttributes(Lite<Entity> lite)
        {
            return new[]
            {
               new XAttribute("Label", (lite.ToString() ?? "[null]") + Modified((Modifiable)lite)),
               new XAttribute("TypeName", lite.GetType().TypeName()), 
               new XAttribute("Stroke", ColorExtensions.ToHtmlColor(lite.EntityType.FullName.GetHashCode())),
               new XAttribute("StrokeThickness", "2"),
               new XAttribute("Background", ColorExtensions.ToHtmlColor(lite.EntityType.FullName.GetHashCode()).Replace("#", "#44")),
               new XAttribute("Description", lite.IdOrNull?.ToString() ?? "New")
            };
        }

        private static XAttribute[] GetAttributes(EmbeddedEntity ee)
        {
            return new[]
            {
               new XAttribute("Label", (ee.ToString() ?? "[null]")+  Modified(ee)),
               new XAttribute("TypeName", ee.GetType().TypeName()), 
               new XAttribute("NodeRadius", 0),
               new XAttribute("Background", ColorExtensions.ToHtmlColor(ee.GetType().FullName.GetHashCode())),
            };
        }

        private static XAttribute[] GetAttributes(MixinEntity ee)
        {
            return new[]
            {
               new XAttribute("Label", (ee.ToString() ?? "[null]") +  Modified(ee)),
               new XAttribute("TypeName", ee.GetType().TypeName()), 
               new XAttribute("Background", ColorExtensions.ToHtmlColor(ee.GetType().FullName.GetHashCode())),
            };
        }

        private static XAttribute[] GetAttributes(IList list)
        {
            return new[]
            {
               new XAttribute("Label", (list.ToString() ?? "[null]") +  Modified((Modifiable)list)),
               new XAttribute("TypeName", list.GetType().TypeName()), 
               new XAttribute("NodeRadius", 2),
               new XAttribute("Background", ColorExtensions.ToHtmlColor(list.GetType().ElementType().FullName.GetHashCode())),
            };
        }

        public static bool HasChanges(Modifiable mod)
        {
            return GraphExplorer.FromRoot(mod).Any(a => a.Modified == ModifiedState.SelfModified);
        }

        public static void SetValidationErrors(DirectedGraph<Modifiable> directedGraph, IntegrityCheckException e)
        {
            SetValidationErrors(directedGraph, e.Errors);
        }

        public static void SetValidationErrors(DirectedGraph<Modifiable> directedGraph, Dictionary<Guid, Dictionary<string, string>> dictionary)
        {
            foreach (var mod in directedGraph.OfType<ModifiableEntity>())
            {
                var dic = dictionary.TryGetC(mod.temporalId);

                if (dic != null)
                    mod.SetTemporalErrors(dic);
            }
        }
    }

    [Serializable]
    public class IntegrityCheckException : Exception
    {
        public Dictionary<Guid, Dictionary<string, string>> Errors
        {
            get { return (Dictionary<Guid, Dictionary<string, string>>)this.Data["integrityErrors"]; }
            set { this.Data["integrityErrors"] = value; }
        }

        public IntegrityCheckException(Dictionary<Guid, Dictionary<string, string>> errors)
            : base(errors.Values.SelectMany(a => a.Values).ToString("\r\n"))
        {
            this.Errors = errors;
        }
        protected IntegrityCheckException(
          SerializationInfo info,
          StreamingContext context)
            : base(info, context) { }
    }
}
