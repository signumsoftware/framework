using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Collections;
using System.Xml.Linq;

namespace Signum.Utilities.DataStructures
{
    public class DirectedGraph<T> : IEnumerable<T>
    {
        Dictionary<T, HashSet<T>> adjacency;
        public IEqualityComparer<T> Comparer { get; private set; }

        public DirectedGraph()
            : this(EqualityComparer<T>.Default)
        {
        }

        public DirectedGraph(IEqualityComparer<T> comparer)
        {
            this.Comparer = comparer;
            this.adjacency = new Dictionary<T, HashSet<T>>(comparer);
        }

        public IEnumerable<T> Nodes
        {
            get { return adjacency.Keys; }
        }

        public IEnumerable<Edge<T>> Edges
        {
            get { return adjacency.SelectMany(k => k.Value.Select(n => new Edge<T>(k.Key, n))); }
        }

        public int Count
        {
            get { return adjacency.Count; }
        }

        public int EdgesCount
        {
            get { return adjacency.Sum(k => k.Value.Count); }
        }

        public bool Contains(T node)
        {
            return adjacency.ContainsKey(node);
        }

        public bool Connected(T from, T to)
        {
            return RelatedTo(from).Contains(to);
        }

        public bool TryConnected(T from, T to)
        {
            var hs = TryGet(from);

            return hs != null && hs.Contains(to);
        }

        public void Add(T from)
        {
            TryGetOrAdd(from);
        }

        public void Add(T from, T to)
        {
            TryGetOrAdd(from).Add(to);
            TryGetOrAdd(to);
        }

        public void Add(T from, params T[] elements)
        {
            var f = TryGetOrAdd(from);
            foreach (var item in elements)
            {
                TryGetOrAdd(item);
                f.Add(item);
            }
        }

        public void Add(T from, IEnumerable<T> elements)
        {
            var f = TryGetOrAdd(from);
            foreach (var item in elements)
            {
                TryGetOrAdd(item);
                f.Add(item);
            }
        }

        public bool RemoveEdge(T from, T to)
        {
            var hashSet = adjacency.TryGetC(from);
            if (hashSet == null)
                return false;

            return hashSet.Remove(to);
        }

        public bool RemoveEdge(Edge<T> edge)
        {
            return RemoveEdge(edge.From, edge.To);
        }

        public void RemoveEdges(IEnumerable<Edge<T>> edges)
        {
            foreach (var edge in edges)
                RemoveEdge(edge.From, edge.To);
        }

        public bool RemoveFullNode(T node)
        {
            if (!adjacency.ContainsKey(node))
                return false;

            return RemoveFullNode(node, InverseRelatedTo(node));
        }

        /// <summary>
        /// Unsafer but faster
        /// </summary>
        public bool RemoveFullNode(T node, IEnumerable<T> inverseRelated)
        {
            if (!adjacency.ContainsKey(node))
                return false;

            adjacency.Remove(node);
            foreach (var n in inverseRelated)
                RemoveEdge(n, node);

            return true;
        }

        public static void RemoveFullNodeSymetric(DirectedGraph<T> original, DirectedGraph<T> inverse, T node)
        {
            HashSet<T> from = inverse.RelatedTo(node);
            HashSet<T> to = original.RelatedTo(node);

            original.RemoveFullNode(node, from);
            inverse.RemoveFullNode(node, to);
        }

        HashSet<T> TryGet(T node)
        {
            return adjacency.TryGetC(node);
        }

        HashSet<T> TryGetOrAdd(T node)
        {
            if (adjacency.TryGetValue(node, out HashSet<T> result))
                return result;

            result = new HashSet<T>(Comparer);
            adjacency.Add(node, result);
            return result;
        }

        public HashSet<T> TryRelatedTo(T node)
        {
            return TryGet(node) ?? new HashSet<T>();
        }

        public HashSet<T> RelatedTo(T node)
        {
            var result = adjacency.TryGetC(node);
            if (result == null)
                throw new InvalidOperationException("The node {0} is not in the graph".FormatWith(node));
            return result;
        }

        /// <summary>
        /// Costly
        /// </summary>
        public IEnumerable<T> InverseRelatedTo(T node)
        {
            return this.Where(n => Connected(n, node));
        }

        public HashSet<T> IndirectlyRelatedTo(T node)
        {
            return IndirectlyRelatedTo(node, false);
        }

        public HashSet<T> IndirectlyRelatedTo(T node, bool includeInitialNode)
        {
            HashSet<T> set = new HashSet<T>();
            if (includeInitialNode)
                set.Add(node);
            IndirectlyRelatedTo(node, set);
            return set;
        } 

        void IndirectlyRelatedTo(T node, HashSet<T> set)
        {
            foreach (var item in RelatedTo(node))
                if (set.Add(item))
                    IndirectlyRelatedTo(item, set);
        }

        public HashSet<T> IndirectlyRelatedTo(T node, Func<T, bool> condition)
        {
            HashSet<T> set = new HashSet<T>();
            IndirectlyRelatedTo(node, set, condition);
            return set;
        }

        void IndirectlyRelatedTo(T node, HashSet<T> set, Func<T, bool> condition)
        {
            foreach (var item in RelatedTo(node).Where(condition))
                if (set.Add(item))
                    IndirectlyRelatedTo(item, set, condition);
        }


        public void DepthExplore(T node, Func<T, bool> condition, Action<T> preAction, Action<T> postAction)
        {
            if (condition != null && !condition(node))
                return;

            preAction?.Invoke(node);

            foreach (T item in RelatedTo(node))
                DepthExplore(item, condition, preAction, postAction);

            postAction?.Invoke(node);
        }

        public void BreadthExplore(T root, Func<T, bool> condition, Action<T> action)
        {
            Queue<T> queue = new Queue<T>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                T node = queue.Dequeue();
                if (!condition(node))
                    continue;

                action(node);

                queue.EnqueueRange(RelatedTo(node));
            }
        }


        public DirectedGraph<T> Inverse()
        {
            DirectedGraph<T> result = new DirectedGraph<T>(Comparer);
            foreach (var item in Nodes)
            {
                result.Add(item);
                foreach (var related in RelatedTo(item))
                {
                    result.Add(related, item);
                }
            }
            return result;
        }

        public DirectedGraph<T> UndirectedGraph()
        {
            return this.Inverse().Do(g => g.UnionWith(this));
        }

        public DirectedGraph<T> RemoveAllNodes(DirectedGraph<T> graph)
        {
            var inv = this.Inverse();

            foreach (var item in this.Where(a=>graph.Contains(a)).ToList())
            {
                this.RemoveFullNode(item, inv.RelatedTo(item));
            }

            return this;
        }

        public void UnionWith(DirectedGraph<T> other)
        {
            foreach (var item in other.Nodes)
                Add(item, other.RelatedTo(item));
        }

        public DirectedGraph<T> Clone()
        {
            return new DirectedGraph<T>(Comparer).Do(g => g.UnionWith(this));
        }

        public static DirectedGraph<T> Generate(T root, Func<T, IEnumerable<T>> expandFunction)
        {
            return Generate(root, expandFunction, EqualityComparer<T>.Default);
        }

        public static DirectedGraph<T> Generate(T root, Func<T, IEnumerable<T>> expandFunction, IEqualityComparer<T> comparer)
        {
            DirectedGraph<T> result = new DirectedGraph<T>(comparer);
            result.Expand(root, expandFunction);
            return result;
        }

        public static DirectedGraph<T> Generate(IEnumerable<T> roots, Func<T, IEnumerable<T>> expandFunction)
        {
            return Generate(roots, expandFunction, EqualityComparer<T>.Default);
        }

        public static DirectedGraph<T> Generate(IEnumerable<T> roots, Func<T, IEnumerable<T>> expandFunction, IEqualityComparer<T> comparer)
        {
            DirectedGraph<T> result = new DirectedGraph<T>(comparer);
            foreach (var root in roots)
                result.Expand(root, expandFunction);
            return result;
        }

        public void Expand(T node, Func<T, IEnumerable<T>> expandFunction)
        {
            if (adjacency.ContainsKey(node)) 
                return;

            var hs = new HashSet<T>(Comparer);
            adjacency.Add(node, hs);
            
            foreach (var item in expandFunction(node))
            {
                Expand(item, expandFunction);
                hs.Add(item);
            }
        }

        public override string ToString()
        {
            return adjacency.ToString(kvp => "{0}=>{1};".FormatWith(kvp.Key, kvp.Value.ToString(",")), "\r\n");
        }

        public string Graphviz()
        {
            return Graphviz("Graph", a => a.ToString());
        }

        public string Graphviz(string name, Func<T, string> getName)
        {
            int num = 0;
            Dictionary<T, int> nodeDic = Nodes.ToDictionary(n => n, n => num++, Comparer);

            string nodes = Nodes.ToString(e => "   {0} [ label =\"{1}\"];".FormatWith(nodeDic[e], getName(e)), "\r\n");

            string edges = Edges.ToString(e => "   {0} -> {1};".FormatWith(nodeDic[e.From], nodeDic[e.To]), "\r\n");

            return "digraph \"{0}\"\r\n{{\r\n{1}\r\n{2}\r\n}}".FormatWith(name, nodes, edges);
        }

        public XDocument ToDGML()
        {
            return ToDGML(a => a.ToString() ?? "[null]", a => ColorExtensions.ToHtmlColor(a.GetType().FullName.GetHashCode()));
        }

        public XDocument ToDGML(Func<T, string> getNodeLabel, Func<T, string> getColor)
        {
            return ToDGML(n => new[]
            {
                new XAttribute("Label", getNodeLabel(n)),
                new XAttribute("Background", getColor(n))
            });
        }
            
        public XDocument ToDGML(Func<T, XAttribute[]> attributes)
        {
            int num = 0;
            Dictionary<T, int> nodeDic = Nodes.ToDictionary(n => n, n => num++, Comparer);

            XNamespace ns = "http://schemas.microsoft.com/vs/2009/dgml";

            return new XDocument(
                new XElement(ns + "DirectedGraph",
                    new XElement(ns + "Nodes",
                        Nodes.Select(n => new XElement(ns + "Node",
                            new XAttribute("Id", nodeDic[n]),
                            attributes(n)))),
                    new XElement(ns + "Links",
                        Edges.Select(e => new XElement(ns + "Link",
                            new XAttribute("Source", nodeDic[e.From]),
                            new XAttribute("Target", nodeDic[e.To]))))
                 )
            );
        }

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return adjacency.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return adjacency.Keys.GetEnumerator();
        }

        #endregion

        public IEnumerable<HashSet<T>> CompilationOrderGroups()
        {
            DirectedGraph<T> clone = this.Clone();
            DirectedGraph<T> inv = this.Inverse();

            while (clone.Count > 0)
            {
                var leaves = clone.Sinks();
                foreach (var node in leaves)
                    clone.RemoveFullNode(node, inv.RelatedTo(node));
                yield return leaves;
            }
        }

        public IEnumerable<T> CompilationOrder()
        {
            return CompilationOrderGroups().SelectMany(e => e);
        }


        /// <summary>
        /// A simple but effective linear-time heuristic constructs a vertex ordering,
        /// just as in the topological sort heuristic above, and deletes any arc going from right to left. 
        /// 
        /// This heuristic builds up the ordering from the outside in based on the in- and out-degrees of each vertex. 
        /// - Any vertex of in-degree 0 is a source and can be placed first in the ordering. 
        /// - Any vertex of out-degree 0 is a sink and can be placed last in the ordering, again without violating any constraints. 
        /// - If not, we find the vertex with the maximum difference between in- and out-degree, 
        /// and place it on the side of the permutation that will salvage the greatest number of constraints. 
        /// Delete any vertex from the DAG after positioning it and repeat until the graph is empty.
        /// </summary>
        /// <returns></returns>

        public DirectedGraph<T> FeedbackEdgeSet()
        {
            DirectedGraph<T> result = new DirectedGraph<T>(Comparer);

            DirectedGraph<T> clone = this.Clone();
            DirectedGraph<T> inv = this.Inverse();

            HashSet<T> head = new HashSet<T>();  // for sources
            HashSet<T> tail = new HashSet<T>();  // for sinks
            while (clone.Count > 0)
            {
                var sinks = clone.Sinks();
                if (sinks.Count() != 0)
                {
                    foreach (var sink in sinks)
                    {
                        DirectedGraph<T>.RemoveFullNodeSymetric(clone, inv, sink);
                        tail.Add(sink);
                    }
                    continue;
                }

                var sources = inv.Sinks();
                if (sources.Count() != 0)
                {
                    foreach (var source in sources)
                    {
                        DirectedGraph<T>.RemoveFullNodeSymetric(clone, inv, source);
                        head.Add(source);
                    }
                    continue;
                }

                Func<T, int> fanInOut = n => clone.RelatedTo(n).Count() - inv.RelatedTo(n).Count();

                MinMax<T> mm = clone.WithMinMaxPair(fanInOut);

                if (fanInOut(mm.Max) > -fanInOut(mm.Min))
                {
                    T node = mm.Max;
                    foreach (var n in inv.RelatedTo(node))
                        result.Add(node, n);
                    DirectedGraph<T>.RemoveFullNodeSymetric(clone, inv, node);
                    head.Add(node);
                }
                else
                {
                    T node = mm.Min;
                    foreach (var n in clone.RelatedTo(node))
                        result.Add(node, n);
                    DirectedGraph<T>.RemoveFullNodeSymetric(clone, inv, node);
                    head.Add(node);
                }
            }

            return result;
        }

        public HashSet<T> Sinks()
        {
            return adjacency.Where(a => a.Value.Count == 0).Select(a => a.Key).ToHashSet();
        }

        public DirectedGraph<T> WhereEdges(Func<Edge<T>, bool> condition)
        {
            DirectedGraph<T> result = new DirectedGraph<T>(Comparer);
            foreach (var item in Nodes)
                result.Add(item, RelatedTo(item).Where(to => condition(new Edge<T>(item, to))));
            return result;
        }

        public List<T> ShortestPath(T from, T to)
        {
            //http://en.wikipedia.org/wiki/Dijkstra's_algorithm

            Dictionary<T, int> distance = this.ToDictionary(e => e, e => int.MaxValue, Comparer);
            Dictionary<T, T> previous = new Dictionary<T, T>(Comparer);

            distance[from] = 0;
            PriorityQueue<T> queue = new PriorityQueue<T>((a, b) => distance[a].CompareTo(distance[b]));
            queue.PushAll(this);

            while (queue.Count > 0)
            {
                T u = queue.Peek();
                if (distance[u] == int.MaxValue)
                    return null;

                int newDist = distance[u] + 1;
                foreach (var v in RelatedTo(u))
                {
                    if (newDist < distance[v])
                    {
                        distance[v] = newDist;
                        queue.Update(v);
                        previous[v] = u;
                    }
                }
                queue.Pop();

                if (Comparer.Equals(u, to))
                    break;
            }

            return to.For(n => previous.ContainsKey(n), n => previous[n]).Reverse().ToList();
        }
    }

    public struct Edge<T>
    {
        public readonly T From;
        public readonly T To;

        public Edge(T from, T to)
        {
            this.From = from;
            this.To = to;
        }

        public override string ToString()
        {
            return "{0}->{1}".FormatWith(From, To);
        }
    };

    
}
