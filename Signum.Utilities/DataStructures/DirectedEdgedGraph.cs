using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Collections;
using System.Xml.Linq;

namespace Signum.Utilities.DataStructures
{
    public class DirectedEdgedGraph<T, E> : IEnumerable<T>
    {
        Dictionary<T, Dictionary<T, E>> adjacency;
        public IEqualityComparer<T> Comparer { get; private set; }

        public DirectedEdgedGraph()
            : this(EqualityComparer<T>.Default)
        {
        }

        public DirectedEdgedGraph(IEqualityComparer<T> comparer)
        {
            this.Comparer = comparer;
            this.adjacency = new Dictionary<T, Dictionary<T, E>>(comparer);
        }

        public IEnumerable<T> Nodes
        {
            get { return adjacency.Keys; }
        }

        public IEnumerable<Edge<T>> Edges
        {
            get { return adjacency.SelectMany(k => k.Value.Select(n => new Edge<T>(k.Key, n.Key))); }
        }

        public IEnumerable<Edge<T, E>> EdgesWithValue
        {
            get { return adjacency.SelectMany(k => k.Value.Select(n => new Edge<T, E>(k.Key, n.Key, n.Value))); }
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
            return RelatedTo(from).ContainsKey(to);
        }

        public bool TryConnected(T from, T to)
        {
            var dic = TryGet(from);

            return dic != null && dic.ContainsKey(to);
        }

        public void Add(T from)
        {
            TryGetOrAdd(from);
        }

        public void Add(T from, T to, E value)
        {
            var f = TryGetOrAdd(from);
            TryGetOrAdd(to);
            f[to] = value;

        }

        public void Add(T from, params KeyValuePair<T, E>[] elements)
        {
            var f = TryGetOrAdd(from);
            foreach (var item in elements)
            {
                TryGetOrAdd(item.Key);
                f[item.Key] = item.Value;
            }
        }

        public void Add(T from, IEnumerable<KeyValuePair<T, E>> elements)
        {
            var f = TryGetOrAdd(from);
            foreach (var item in elements)
            {
                TryGetOrAdd(item.Key);
                f[item.Key] = item.Value;
            }
        }

        public bool RemoveEdge(T from, T to)
        {
            var dic = adjacency.TryGetC(from);
            if (dic == null)
                return false;

            return dic.Remove(to);
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

            return RemoveFullNode(node, InverseRelatedTo(node).Select(a=>a.Key));
        }

        /// <summary>
        /// Unsafe but fast
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

        public static void RemoveFullNodeSymetric(DirectedEdgedGraph<T, E> original, DirectedEdgedGraph<T, E> inverse, T node)
        {
            var from = inverse.RelatedTo(node).Keys;
            var to = original.RelatedTo(node).Keys;

            original.RemoveFullNode(node, from);
            inverse.RemoveFullNode(node, to);
        }

        Dictionary<T, E> TryGet(T node)
        {
            return adjacency.TryGetC(node);
        }

        Dictionary<T, E> TryGetOrAdd(T node)
        {
            if (adjacency.TryGetValue(node, out Dictionary<T, E> result))
                return result;

            result = new Dictionary<T, E>(Comparer);
            adjacency.Add(node, result);
            return result;
        }

        public Dictionary<T, E> TryRelatedTo(T node)
        {
            return TryGet(node) ?? new Dictionary<T, E>(Comparer);
        }

        public Dictionary<T, E> RelatedTo(T node)
        {
            var result = adjacency.TryGetC(node);
            if (result == null)
                throw new InvalidOperationException("The node {0} is not in the graph".FormatWith(node));
            return result;
        }

        /// <summary>
        /// Costly
        /// </summary>
        public IEnumerable<KeyValuePair<T, E>> InverseRelatedTo(T node)
        {
            foreach (var item in adjacency)
            {
                if (item.Value.TryGetValue(node, out E edge))
                    yield return KVP.Create(item.Key, edge);
            }
        }

        public HashSet<T> IndirectlyRelatedTo(T node)
        {
            return IndirectlyRelatedTo(node, false);
        }

        public HashSet<T> IndirectlyRelatedTo(T node, bool includeParentNode)
        {
            HashSet<T> set = new HashSet<T>();
            if (includeParentNode)
                set.Add(node);
            IndirectlyRelatedTo(node, set);
            return set;
        } 

        void IndirectlyRelatedTo(T node, HashSet<T> set)
        {
            foreach (var item in RelatedTo(node))
                if (set.Add(item.Key))
                    IndirectlyRelatedTo(item.Key, set);
        }

        public HashSet<T> IndirectlyRelatedTo(T node, Func<KeyValuePair<T, E>, bool> condition)
        {
            HashSet<T> set = new HashSet<T>();
            IndirectlyRelatedTo(node, set, condition);
            return set;
        }

        void IndirectlyRelatedTo(T node, HashSet<T> set, Func<KeyValuePair<T, E>, bool> condition)
        {
            foreach (var item in RelatedTo(node).Where(condition))
                if (set.Add(item.Key))
                    IndirectlyRelatedTo(item.Key, set, condition);
        }

        public void DepthExplore(T node, Func<T, bool> condition, Action<T> preAction, Action<T> postAction)
        {
            if (condition != null && !condition(node))
                return;

            preAction?.Invoke(node);

            foreach (T item in RelatedTo(node).Keys)
                DepthExplore(item, condition, preAction, postAction);

            postAction?.Invoke(node);
        }

        public void DepthExploreConnections(T node, Func<T, E, T, bool> condition)
        {
            foreach (var kvp in RelatedTo(node))
            {
                if (condition(node, kvp.Value, kvp.Key))
                    DepthExploreConnections(kvp.Key, condition);
            }
        }

        public void DepthExploreConnections(Stack<T> stack, Func<T, E, T, bool> condition)
        {
            var node = stack.Peek();
            foreach (var kvp in RelatedTo(node))
            {
                if (!stack.Contains(kvp.Key))
                {
                    stack.Push(kvp.Key);

                    if (condition(node, kvp.Value, kvp.Key))
                        DepthExploreConnections(stack, condition);

                    stack.Pop();
                }
            }
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

                queue.EnqueueRange(RelatedTo(node).Keys);
            }
        }

        public DirectedEdgedGraph<T, E> Inverse()
        {
            DirectedEdgedGraph<T, E> result = new DirectedEdgedGraph<T, E>(Comparer);
            foreach (var item in Nodes)
            {
                result.Add(item);
                foreach (var related in RelatedTo(item))
                {
                    result.Add(related.Key, item, related.Value);
                }
            }
            return result;
        }

        public DirectedEdgedGraph<T, E> UndirectedGraph()
        {
            return this.Inverse().Do(g => g.UnionWith(this));
        }

        public void UnionWith(DirectedEdgedGraph<T, E> other)
        {
            foreach (var item in other.Nodes)
                Add(item, other.RelatedTo(item));
        }

        public DirectedEdgedGraph<T, E> Clone()
        {
            return new DirectedEdgedGraph<T, E>(Comparer).Do(g => g.UnionWith(this));
        }

        public static DirectedEdgedGraph<T, E> Generate(T root, Func<T, IEnumerable<KeyValuePair<T, E>>> expandFunction)
        {
            return Generate(root, expandFunction, EqualityComparer<T>.Default);
        }

        public static DirectedEdgedGraph<T, E> Generate(T root, Func<T, IEnumerable<KeyValuePair<T, E>>> expandFunction, IEqualityComparer<T> comparer)
        {
            DirectedEdgedGraph<T, E> result = new DirectedEdgedGraph<T, E>(comparer);
            result.Expand(root, expandFunction);
            return result;
        }

        public static DirectedEdgedGraph<T, E> Generate(IEnumerable<T> roots, Func<T, IEnumerable<KeyValuePair<T, E>>> expandFunction)
        {
            return Generate(roots, expandFunction, EqualityComparer<T>.Default);
        }

        public static DirectedEdgedGraph<T, E> Generate(IEnumerable<T> roots, Func<T, IEnumerable<KeyValuePair<T, E>>> expandFunction, IEqualityComparer<T> comparer)
        {
            DirectedEdgedGraph<T, E> result = new DirectedEdgedGraph<T, E>(comparer);
            foreach (var root in roots)
                result.Expand(root, expandFunction);
            return result;
        }

        public void Expand(T node, Func<T, IEnumerable<KeyValuePair<T, E>>> expandFunction)
        {
            if (adjacency.ContainsKey(node))
                return;

            var dic = new Dictionary<T, E>(Comparer);
            adjacency.Add(node, dic);

            foreach (var item in expandFunction(node))
            {
                Expand(item.Key, expandFunction);
                if (!dic.ContainsKey(item.Key))
                    dic.Add(item.Key, item.Value);
            }
        }

        public override string ToString()
        {
            return adjacency.ToString(kvp => "{0}=>{1};".FormatWith(kvp.Key,
                 kvp.Value.ToString(kvp2 => "[{0}->{1}]".FormatWith(kvp2.Value, kvp2.Key), ",")),
                "\r\n"); ;
        }

        public string ToGraphviz()
        {
            return ToGraphviz(typeof(E).Name, a => a.ToString(), e => e.ToString());
        }

        public string ToGraphviz(string name)
        {
            return ToGraphviz(name, a => a.ToString(), e => e.ToString());
        }

        public string ToGraphviz(string name, Func<T, string> getNodeLabel, Func<E, string> getEdgeLabel)
        {
            int num = 0;
            Dictionary<T, int> nodeDic = Nodes.ToDictionary(n => n, n => num++, Comparer);

            string nodes = Nodes.ToString(e => "   {0} [ label =\"{1}\"];".FormatWith(nodeDic[e], getNodeLabel(e)), "\r\n");

            string edges = EdgesWithValue.ToString(e => "   {0} -> {1} [ label =\"{2}\"];".FormatWith(nodeDic[e.From], nodeDic[e.To], getEdgeLabel(e.Value)), "\r\n");

            return "digraph \"{0}\"\r\n{{\r\n{1}\r\n{2}\r\n}}".FormatWith(name, nodes, edges);
        }

        public XDocument ToDGML()
        {
            return ToDGML(
                a => a.ToString() ?? "[null]",
                a => ColorExtensions.ToHtmlColor(a.GetType().FullName.GetHashCode()),
                e => e.ToString() ?? "[null]");
        }

        public XDocument ToDGML(Func<T, string> getNodeLabel, Func<T, string> getColor, Func<E, string> getEdgeLabel = null)
        {
            return ToDGML(n => new[]
            {
                new XAttribute("Label", getNodeLabel(n)),
                new XAttribute("Background", getColor(n))
            }, e => getEdgeLabel == null ? new XAttribute[0] : new[]
            {
                new XAttribute("Label", getEdgeLabel(e))
            });
        }
            
        public XDocument ToDGML(Func<T, XAttribute[]> getNodeAttributes, Func<E, XAttribute[]> getEdgeAttributes)
        {
            int num = 0;
            Dictionary<T, int> nodeDic = Nodes.ToDictionary(n => n, n => num++, Comparer);

            XNamespace ns = "http://schemas.microsoft.com/vs/2009/dgml";

            return new XDocument(
                new XElement(ns + "DirectedGraph",
                    new XElement(ns + "Nodes",
                        Nodes.Select(n => new XElement(ns + "Node",
                            new XAttribute("Id", nodeDic[n]),
                            getNodeAttributes(n)))),
                    new XElement(ns + "Links",
                        EdgesWithValue.Select(e => new XElement(ns + "Link",
                            new XAttribute("Source", nodeDic[e.From]),
                            new XAttribute("Target", nodeDic[e.To]),
                            getEdgeAttributes(e.Value))))
                 )
            );
        }

        public IEnumerator<T> GetEnumerator()
        {
            return adjacency.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return adjacency.Keys.GetEnumerator();
        }

        public IEnumerable<HashSet<T>> CompilationOrderGroups()
        {
            DirectedEdgedGraph<T, E> clone = this.Clone();
            DirectedEdgedGraph<T, E> inv = this.Inverse();

            while (clone.Count > 0)
            {
                var leaves = clone.Sinks();
                foreach (var node in leaves)
                    clone.RemoveFullNode(node, inv.RelatedTo(node).Keys);
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
        public DirectedEdgedGraph<T, E> FeedbackEdgeSet()
        {
            DirectedEdgedGraph<T, E> result = new DirectedEdgedGraph<T, E>(Comparer);

            DirectedEdgedGraph<T, E> clone = this.Clone();
            DirectedEdgedGraph<T, E> inv = this.Inverse();

            HashSet<T> head = new HashSet<T>();  // for sources
            HashSet<T> tail = new HashSet<T>();  // for sinks
            while (clone.Count > 0)
            {
                var sinks = clone.Sinks();
                if (sinks.Count() != 0)
                {
                    foreach (var sink in sinks)
                    {
                        DirectedEdgedGraph<T, E>.RemoveFullNodeSymetric(clone, inv, sink);
                        tail.Add(sink);
                    }
                    continue;
                }

                var sources = inv.Sinks();
                if (sources.Count() != 0)
                {
                    foreach (var source in sources)
                    {
                        DirectedEdgedGraph<T, E>.RemoveFullNodeSymetric(clone, inv, source);
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
                        result.Add(n.Key, node, n.Value);
                    
                    DirectedEdgedGraph<T, E>.RemoveFullNodeSymetric(clone, inv, node);
                    head.Add(node);
                }
                else
                {
                    T node = mm.Min;
                    foreach (var n in clone.RelatedTo(node))
                        result.Add(node, n.Key, n.Value);
                    DirectedEdgedGraph<T, E>.RemoveFullNodeSymetric(clone, inv, node);
                    head.Add(node);
                }
            }

            return result;
        }

        HashSet<T> Sinks()
        {
            return adjacency.Where(a => a.Value.Count == 0).Select(a => a.Key).ToHashSet();
        }

        public DirectedEdgedGraph<T, E> WhereEdges(Func<Edge<T, E>, bool> condition, bool keepAllNodes)
        {
            if (keepAllNodes)
            {
                DirectedEdgedGraph<T, E> result = new DirectedEdgedGraph<T, E>(Comparer);
                foreach (var item in Nodes)
                    result.Add(item, RelatedTo(item).Where(to => condition(new Edge<T, E>(item, to.Key, to.Value))));
                return result;
            }
            else
            {
                DirectedEdgedGraph<T, E> result = new DirectedEdgedGraph<T, E>(Comparer);
                foreach (var e in EdgesWithValue.Where(condition))
                    result.Add(e.From, e.To, e.Value);
                return result;
            }
        }

        public List<T> ShortestPath(T from, T to, Func<E, int> getWeight)
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

                foreach (var v in RelatedTo(u))
                {
                    int weight = getWeight(v.Value);
                    if (weight != int.MaxValue)
                    {
                        int newDist = distance[u] + weight;
                        if (newDist < distance[v.Key])
                        {
                            distance[v.Key] = newDist;
                            queue.Update(v.Key);
                            previous[v.Key] = u;
                        }
                    }
                }
                queue.Pop();

                if (Comparer.Equals(u, to))
                    break;
            }

            return to.For(n => previous.ContainsKey(n), n => previous[n]).Reverse().ToList();
        }
    }

    public struct Edge<T, E>
    {
        public readonly T From;
        public readonly T To;
        public readonly E Value;

        public Edge(T from, T to, E value)
        {
            this.From = from;
            this.To = to;
            this.Value = value;
        }

        public override string ToString()
        {
            return "{0}-{1}->{2}".FormatWith(From, Value, To);
        }
    }

    public static class DirectedEdgedGraphExtensions
    {
        public static E GetOrCreate<T, E>(this DirectedEdgedGraph<T, E> graph, T from, T to)
            where E : new()
        {
            var dic = graph.TryRelatedTo(from);

            if (dic != null)
                return dic.GetOrCreate(to);
            
            E newEdge = new E();
            graph.Add(from, to, newEdge);
            return newEdge;
        }
    }
}
