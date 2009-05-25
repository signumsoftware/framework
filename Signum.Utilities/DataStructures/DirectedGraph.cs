using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Collections;
using Signum.Utilities.Properties; 

namespace Signum.Utilities.DataStructures
{
public class DirectedGraph<T>:IEnumerable<T>
{
    Dictionary<T, HashSet<T>> adjacency = new Dictionary<T,HashSet<T>>(); 

    public IEnumerable<T> Nodes
    {
        get{return adjacency.Keys;}
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
        return Get(from).Contains(to); 
    }

    public bool TryConnected(T from, T to)
    {
        return TryGet(from).TryCS(hs => hs.Contains(to)) ?? false; 
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

    public bool Remove(T from, T to)
    {
        var hashSet = adjacency.TryGetC(from);
        if(hashSet == null)
            return false;

        return hashSet.Remove(to); 
    }

    public bool Remove(Edge<T> edge)
    {
        return Remove(edge.From, edge.To); 
    }

    public void RemoveAll(IEnumerable<Edge<T>> edges)
    {
        foreach (var edge in edges)
            Remove(edge.From, edge.To);
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
            Remove(n, node);

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

    HashSet<T> Get(T node)
    {
        var result = adjacency.TryGetC(node); 
        if (result == null)
            throw new InvalidOperationException(Resources.TheNode0IsNotInTheGraph.Formato(node));
        return result; 
    }

    HashSet<T> TryGetOrAdd(T node)
    {
        return adjacency.GetOrCreate(node);
    }

    public HashSet<T> TryRelatedTo(T node)
    {
        return TryGet(node) ?? new HashSet<T>(); 
    }

    public HashSet<T> RelatedTo(T node)
    {
        return Get(node);
    }

    /// <summary>
    /// Costly
    /// </summary>
    public IEnumerable<T> InverseRelatedTo(T node)
    {
        return this.Where(n => Connected(n, node));
    }

    /// <summary>
    /// Recursive relationships
    /// </summary>
    public HashSet<T> IndirectlyRelatedTo(T node)
    {
        HashSet<T> set = new HashSet<T>();
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

        if (preAction != null)
            preAction(node);
        
        foreach (T item in RelatedTo(node))
            DepthExplore(item, condition, preAction, postAction);

        if (postAction != null)
            postAction(node);
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
        DirectedGraph<T> result = new DirectedGraph<T>();
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
        return this.Inverse().Do(g => g.Union(this));
    }

    public void Union(DirectedGraph<T> other)
    {
        foreach (var item in other.Nodes)
            Add(item, other.RelatedTo(item));
    }

    public static DirectedGraph<T> Union(IEnumerable<DirectedGraph<T>> others)
    {
        DirectedGraph<T> result = new DirectedGraph<T>();
        others.ForEach(d => result.Union(d));
        return result;
    }

    public DirectedGraph<T> Clone()
    {  
        return new DirectedGraph<T>().Do(g=>g.Union(this)); 
    }

    public static DirectedGraph<T> Generate(T root, Func<T,IEnumerable<T>> expandFunction)
    {
        DirectedGraph<T> result = new DirectedGraph<T>();
        result.Expand(root, expandFunction);
        return result;
    }

    public void Expand(T node, Func<T, IEnumerable<T>> expandFunction)
    {
        if (Contains(node)) return;

        Add(node); //necesario para ciclos
        foreach (var item in expandFunction(node))
        {
            Expand(item,expandFunction);
            Add(node, item);
        }
    }

    public override string ToString()
    {
        return adjacency.ToString(kvp => "{0}=>{1};".Formato(kvp.Key, kvp.Value.ToString(",")), "\r\n");  
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
        DirectedGraph<T> result = new DirectedGraph<T>();

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
                inv.RelatedTo(node).ForEach(n=>result.Add(n,node));
                DirectedGraph<T>.RemoveFullNodeSymetric(clone, inv, node);
                head.Add(node); 
            }
            else
            {
                T node = mm.Min;
                clone.RelatedTo(node).ForEach(n=>result.Add(node, n));
                DirectedGraph<T>.RemoveFullNodeSymetric(clone, inv, node);
                head.Add(node); 
            }
        }

        return result;
    }

    private HashSet<T> Sinks()
    {
        return adjacency.Where(a => a.Value.Count == 0).Select(a => a.Key).ToHashSet(); 
    }

    public DirectedGraph<S> ColapseTo<S>() where S : T
    {
        DirectedGraph<S> result = new DirectedGraph<S>();
        foreach (var item in Nodes.OfType<S>())
        {
            var toColapse = IndirectlyRelatedTo(item, i => !(i is S)); 
            var toColapseFriends = toColapse.SelectMany(i => RelatedTo(i).OfType<S>());
            result.Add(item, toColapseFriends);
            result.Add(item, RelatedTo(item).OfType<S>()); 
        }
        return result; 
    }

    public DirectedGraph<T> Colapse(Func<T,bool> colapse)
    {
        DirectedGraph<T> result = new DirectedGraph<T>();
        foreach (var item in Nodes.Where(a => !colapse(a)))
        {
            var toColapse = IndirectlyRelatedTo(item, colapse);
            var toColapseFriends = toColapse.SelectMany(i => RelatedTo(i).Where(a=>!colapse(a)));
            result.Add(item, toColapseFriends);
            result.Add(item, RelatedTo(item).Where(a => !colapse(a)));
        }
        return result;
    }

    public DirectedGraph<T> WhereEdges(Func<Edge<T>, bool> condition)
    {
        DirectedGraph<T> result = new DirectedGraph<T>();
        foreach (var item in Nodes)
            result.Add(item, RelatedTo(item).Where(to => condition(new Edge<T>(item, to)))); 
        return result; 
    }

    public List<T> ShortestPath(T from, T to)
    {
        //http://en.wikipedia.org/wiki/Dijkstra's_algorithm

        Dictionary<T,int> distance = this.ToDictionary(e=>e,e=>int.MaxValue); 
        Dictionary<T,T> previous = new Dictionary<T,T>();

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

            if (EqualityComparer<T>.Default.Equals(u, to))
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
            return "{0}->{1}".Formato(From, To);
        }
    };

   
}
