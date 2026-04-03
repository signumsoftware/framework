# DirectedGraph 


`DirectedGraph<T>` is a data structure that defines a [Graph](http://en.wikipedia.org/wiki/Graph_theory)
over your objects. Some of the basic characteristics:

* It's a [directed graph](http://en.wikipedia.org/wiki/Directed_graph): Having a link A -> B *Does not mean*  there's a link B -> A. 
* It's not weighed: In fact, there's no information attached
to the edges. 
* It's implemented with an [Adjacency List](http://en.wikipedia.org/wiki/Adjacency_list): In fact, the only state it has is a `Dictionary<T,HashSet<T>>` 
* It's a wrapper: This means that you can use DirectedGraph over any set of objects with a
graph structure, without having to implement INode or something like
that. Just like List or Dictionary, but this is not a common practice in
graph libraries.
    


```C#
public class DirectedGraph<T>: IEnumerable<T>
{
    //The only state, every node in the graph have to be key in the dictionary, even if has no connections (empty HashSet)
    Dictionary<T, HashSet<T>> adjacency = new Dictionary<T, HashSet<T>>();   
    public IEqualityComparer<T> Comparer { get; private set; }

    public DirectedGraph()
    public DirectedGraph(IEqualityComparer<T> comparer) // New in SF 2.0


    public IEnumerable<T> Nodes; //Iterates over all the nodes (dictionary keys)
    public IEnumerable<Edge<T>> Edges //Returns new Edge<T> objects for every relationship
  
    public int Count //Total count of nodes
    public int EdgesCount //total count of edges

    public bool Contains(T node) //returns true if a node exist in the graph

    //returns true if there's a direct edge between 'from' node and 'to' node. 
    public bool Connected(T from, T to) //if !Contains(from) throws InvalidOperationException
    public bool TryConnected(T from, T to) //if !Contains(from) returns false 

    public void Add(T from) //Adds a node with no edge 
    public void Add(T from, T to) //Adds from, node, and a edge between them
    public void Add(T from, params T[] elements) //Adds from, elements, and edge from 'from' to every element
    public void Add(T from, IEnumerable<T> elements) //Adds from, elements, and edge from 'from' to every element

    public bool RemoveEdge(T from, T to) //Removes the edge between from and edge
    public bool RemoveEdge(Edge<T> edge) //Removes the edge in the graph

    public void RemoveEdges(IEnumerable<Edge<T>> edges) //Removes all the edges in the graph

    public bool RemoveFullNode(T node) //Completely removes the node, with all the in and out edges (costly)
    //Completely removes the node if inverseRelated is all the nodes that have an edge TO node. Unsafe but faster. 
    public bool RemoveFullNode(T node, IEnumerable<T> inverseRelated) 

  
    //Returns all the out connections of node. DO NOT modify the HashSet 
    public HashSet<T> RelatedTo(T node) //If node not in the graph throws InvalidOperationException
    public HashSet<T> TryRelatedTo(T node) //If node not in the graph returns empty HashSet
    
    public IEnumerable<T> InverseRelatedTo(T node) //Returns all the nodes that have a connection TO node (costly). 

    //Explores recursively all the connected nodes starting from 'node', returning all nodes that could be reached, directly or indirectly.
    public HashSet<T> IndirectlyRelatedTo(T node)

    //Explores recursively all the connected nodes starting from 'node' that satisfy a condition
    //returning all nodes that could be reached, directly or indirectly that also satisfy the condition but so do all the intermediate nodes
    public HashSet<T> IndirectlyRelatedTo(T node, Func<T, bool> condition)

    //See: http://en.wikipedia.org/wiki/Depth-first_search
    //Explores all the nodes using a depth first approach. 
    //Test condition, executes preAction, explore childs, executes postAction
    public void DepthExplore(T node, Func<T, bool> condition, Action<T> preAction, Action<T> postAction)
 
    //See: http://en.wikipedia.org/wiki/Breadth-first_search
    //Explores all the nodes indirectly related to node that satisfy the condition in breath-first.  
    public void BreadthExplore(T root, Func<T, bool> condition, Action<T> action)

    public DirectedGraph<T> Inverse() //Creates a new graph with the same nodes, but with all the edges inverted

    //Creates a new graph with the same nodes and all the directed edges duplicated in both directions
    public DirectedGraph<T> UndirectedGraph()

    public void UnionWith(DirectedGraph<T> other) //Adds new nodes and edges in 'other' to the current instance
    public DirectedGraph<T> Clone() //Clones this instance creating a new DirectedGraph
    
    //Adds node, and all the nodes and relationships that can be found applying expandFunction recursively
    public void Expand(T node, Func<T, IEnumerable<T>> expandFunction)
    
    public override string ToString() //Returns a string representation of the graph
    public IEnumerator<T> GetEnumerator() //Enumerates all the nodes 

    //See: http://en.wikipedia.org/wiki/Topological_sorting
    public IEnumerable<T> CompilationOrder() //Returns a CompilationOrder given the dependencies of the graph   
    //Returns a CompilationOrder grouped by elements that could be 'compiled' at the same time. 
    public IEnumerable<HashSet<T>> CompilationOrderGroups()

    public DirectedGraph<T> FeedbackEdgeSet() //Small set of edges that, if removed, will make the graph acyclic
    
    //Creates a new graph with the same nodes but only the edges that satisfy condition
    public DirectedGraph<T> WhereEdges(Func<Edge<T>, bool> condition)
    public List<T> ShortestPath(T from, T to)//Finds the shortes path from 'from' to 'to'.

    //Creates a Graphviz compatible string representing the graph http://www.graphviz.org/ New in 2.0
    //It will possibly be deprecated in next version in favor of VS2010 DGMLs
    public string Graphviz()
    public string Graphviz(string name, Func<T, string> getName)

    // == Static Method ==
    //If 'inverse' is the inverse graph of 'original', completely removes node from both graphs (fast) 
    public static void RemoveFullNodeSymetric(DirectedGraph<T> original, DirectedGraph<T> inverse, T node)
   
    //Generates a new graph given a root element (or roots) and a expandFunction to explore all the relationships recursively. Optional equalityComparer 
    public static DirectedGraph<T> Generate(T root, Func<T, IEnumerable<T>> expandFunction)
    public static DirectedGraph<T> Generate(T root, Func<T, IEnumerable<T>> expandFunction, IEqualityComparer<T> comparer)
    public static DirectedGraph<T> Generate(IEnumerable<T> roots, Func<T, IEnumerable<T>> expandFunction)
    public static DirectedGraph<T> Generate(IEnumerable<T> roots, Func<T, IEnumerable<T>> expandFunction, IEqualityComparer<T> comparer) 

}

public struct Edge<T>
{
    public readonly T From;
    public readonly T To;
    (...)
};
```


## Example
Example of using a DirectedGraph to model an acrylic graph: 

```C#
//Collection initializers just use Add method
DirectedGraph<int> dg = new DirectedGraph<int>
{
   {7,11,8},
   {5,11},
   {3,8,10},
   {11,2,9,10},
   {8,9}
};
dg.ToString(); //7=>11,8; 11=>2,9,10; 8=>9; 5=>11; 3=>8,10; 10=>; 2=>; 9=>;
dg.ToString(", "); // 7, 11, 8, 5, 3, 10, 2, 9
dg.Nodes.ToString(", "); // 7, 11, 8, 5, 3, 10, 2, 9 (same as above)
dg.Edges.ToString(", "); // 7->11, 7->8, 11->2, 11->9, 11->10, 8->9, 5->11, 3->8, 3->10
dg.Count; //8
dg.EdgesCount; //9

dg.Connected(7,11); //True
dg.Connected(7, 9); //False
dg.Connected(20, 9); //throws InvalidOperationException
dg.TryConnected(7, 9); //False
dg.TryConnected(20, 9); //False

dg.RelatedTo(7).ToString(", "); //11, 8
dg.RelatedTo(20).ToString(", "); //throws InvalidOperationException
dg.TryRelatedTo(7).ToString(", "); //11, 8
dg.TryRelatedTo(20).ToString(", "); //   (empty)

dg.InverseRelatedTo(11).ToString(", "); //7, 5
dg.InverseRelatedTo(20).ToString(", "); //   (empty)

dg.IndirectlyRelatedTo(5).ToString(", "); //11, 2, 9, 10
dg.IndirectlyRelatedTo(20).ToString(", "); //throws InvalidOperationException

dg.DepthExplore(7, null, n => Console.Write(n+ ", "), null); //Writes 7, 11, 2, 9, 10, 8, 9,
dg.DepthExplore(7, n => n == 9, n => Console.Write(n + ", "), null); //Writes nothing (impossible to reach 9)
dg.DepthExplore(7, null, null, n => Console.Write(n + ", ")); //Writes 2, 9, 10, 11, 9, 8, 7,
dg.BreadthExplore(7, n => true, n => Console.Write(n + ", ")); //Writes 7, 11, 8, 2, 9, 10, 9,

dg.Inverse().ToString(); // 11=>7,5; 8=>7,3; 2=>11; 9=>11,8; 10=>11,3; 5=>; 3=>;

dg.UndirectedGraph().ToString(); // 7=>11,8; 11=>7,5,2,9,10; 8=>7,3,9; 2=>11; 9=>11,8; 10=>11,3; 5=>11; 3=>8,10;

dg.CompilationOrder().ToString(", "); //2, 9, 10, 11, 8, 7, 5, 3

dg.CompilationOrderGroups().ToString(g => "[" + g.ToString(",") + "]", ", "); // [2,9,10], [11,8], [7,5,3]

dg.Colapse(n => n % 2 == 1).ToString(); //7=>9,11; 9=>; 11=>9; 5=>11; 3=>9;

dg.WhereEdges(e => e.From % 2 == 1 && e.To % 2 == 1).ToString(); // 7=>11; 11=>9; 9=>; 8=>; 5=>11; 3=>; 10=>; 2=>;

```