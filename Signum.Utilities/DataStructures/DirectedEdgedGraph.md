# DirectedEdgedGraph

`DirectedEdgedGraph<T,E>`, on the other hand, is the DirectedGraph's sibling with the ability to store information in the edges. 

Internally, the data structure still being an adjacency list, but instead of dictionaries of hashset its implemented with a dictionary of dictionaries: `Dictionary<T, Dictionary<T, E>>`.


```C#
public class DirectedEdgedGraph<T,E>: IEnumerable<T>
{
    //The only state, every node in the graph has to be key in the dictionary, even if has no connections (empty Dictionary)
    Dictionary<T, Dictionary<T, E>> adjacency = new Dictionary<T, Dictionary<T, E>>();    
    public IEqualityComparer<T> Comparer { get; private set; }

    public DirectedEdgedGraph()
    public DirectedEdgedGraph(IEqualityComparer<T> comparer)
    

    public IEnumerable<T> Nodes; //Iterates over all the nodes (dictionary keys)
    public IEnumerable<Edge<T>> Edges //Returns new Edge<T> objects for every relationship with no value
    public IEnumerable<Edge<T, E>> LabeledEdges  //Returns new Edge<T,E> objects for every relationship with value
  
    public int Count //Total count of nodes
    public int EdgesCount //total count of edges

    public bool Contains(T node) //returns true if a node exists in the graph

    //returns true if there's a direct edge between 'from' node and 'to' node. 
    public bool Connected(T from, T to) //if !Contains(from) throws InvalidOperationException
    public bool TryConnected(T from, T to) //if !Contains(from) returns false 

    public void Add(T from) //Adds a node with no edge 
    public void Add(T from, T to, E value) //Adds from, node, and an edge between them  (if edge exists value is overridden) 
    public void Add(T from, params KeyValuePair<T,E>[] elements) //Adds from, elements, and edge from 'from' to every element (if edge exists value is overridden)
    public void Add(T from, IEnumerable<KeyValuePair<T, E>> elements) //Adds from, elements, and edge from 'from' to every element (if edge exist value is overridden)

    public bool Remove(T from, T to) //Removes the edge between from and edge
    public bool Remove(Edge<T> edge) //Removes the edge in the graph (value not necessary) 

    public void RemoveAll(IEnumerable<Edge<T>> edges) //Removes all the edges in the graph (value not necessary) 

    public bool RemoveFullNode(T node) //Completely removes the node, with all the in and out edges (costly)
    //Completely removes the node if inverseRelated is all the nodes that have an edge TO node. Unsafe but faster. 
    public bool RemoveFullNode(T node, IEnumerable<T> inverseRelated) 

  
    //Returns all the out connections of node and the values. DO NOT modify the HashSet 
    public Dictionary<T,E> RelatedTo(T node) //If node is not in the graph throws InvalidOperationException
    public Dictionary<T> TryRelatedTo(T node) //If node is not in the graph returns empty HashSet
    
    public IEnumerable<T> InverseRelatedTo(T node) //Returns all the nodes that have a connection TO node (costly). 

    //Explores recursively all the connected nodes starting from 'node', returning all nodes that could be reached, directly or indirectly.
    public HashSet<T> IndirectlyRelatedTo(T node)

    //Explores recursively all the connected nodes starting from 'node' that satisfies a condition
    //returning all nodes that could be reached, directly or indirectly that no also satisfies the condition but so do all the intermediate nodes
    public HashSet<T> IndirectlyRelatedTo(T node, Func<KeyValuePair<T,E>, bool> condition)

    //See: http://en.wikipedia.org/wiki/Depth-first_search
    //Explores all the nodes using a depth first approach. 
    //Test condition, executes preAction, explore children, executes postAction
    public void DepthExplore(T node, Func<T, bool> condition, Action<T> preAction, Action<T> postAction)

    //See: http://en.wikipedia.org/wiki/Breadth-first_search
    //Explores all the nodes indirectly related to node that satisfy the condition in breath-first.  
    public void BreadthExplore(T root, Func<T, bool> condition, Action<T> action)

    public DirectedEdgedGraph<T> Inverse() //Creates a new graph with the same nodes, but with all the edges inverted

    //Creates a new graph with the same nodes and all the directed edges duplicated in both directions
    public DirectedEdgedGraph<T> UndirectedGraph()

    public void UnionWith(DirectedEdgedGraph<T> other) //Adds every node and edge in 'other' to the current instance
    public DirectedEdgedGraph<T> Clone() //Clones this instance creating a new DirectedEdgedGraph
    
    //Adds node, and all the nodes and relationships that can be found applying expandFunction recursively
    public void Expand(T node, Func<T, IEnumerable<KeyValuePair<T, E>>> expandFunction)
    
    public override string ToString() //Returns a string representation of the graph
    public IEnumerator<T> GetEnumerator() //Enumerates all the nodes 

    //See: http://en.wikipedia.org/wiki/Topological_sorting
    public IEnumerable<T> CompilationOrder() //Returns a CompilationOrder given the dependencies of the graph   
    //Returns a CompilationOrder grouped by elements that could be 'compiled' at the same time. 
    public IEnumerable<HashSet<T>> CompilationOrderGroups()

    public DirectedEdgedGraph<T> FeedbackEdgeSet() //Small set of edges that, if removed, will make the graph acyclic
    
    //Creates a new graph with the same nodes but only the edges that satisfy condition
    public DirectedEdgedGraph<T> WhereEdges(Func<Edge<T>, bool> condition)

    //Finds the shortest path from 'from' to 'to' given the weight of each edge
    public List<T> ShortestPath(T from, T to, Func<E, int> getWeight) 

    //Creates a Graphviz compatible string representing the graph http://www.graphviz.org/ New in 2.0
    //It will possibly be deprecated in next version in favor of VS2010 DGMLs
    public string Graphviz()
    public string Graphviz(string name)

    // == Static Methods ==
    //If 'inverse' is the inverse graph of 'original', completely removes node from both graphs (fast) 
    public static void RemoveFullNodeSymetric(DirectedEdgedGraph<T> original, DirectedEdgedGraph<T> inverse, T node)
    //Generates a new graph given a root element (or roots) and a expandFunction to explore all the relationships recursively. Optional equalityComparer 
    public static DirectedEdgedGraph<T> Generate(T root, Func<T,IEnumerable<KeyValuePair<T, E>>> expandFunction)
    public static DirectedEdgedGraph<T, E> Generate(T root, Func<T, IEnumerable<KeyValuePair<T, E>>> expandFunction, IEqualityComparer<T> comparer)
    public static DirectedEdgedGraph<T, E> Generate(IEnumerable<T> roots, Func<T, IEnumerable<KeyValuePair<T, E>>> expandFunction)
    public static DirectedEdgedGraph<T, E> Generate(IEnumerable<T> roots, Func<T, IEnumerable<KeyValuePair<T, E>>> expandFunction, IEqualityComparer<T> comparer)    
    
}

public struct Edge<T,E>
{
    public readonly T From;
    public readonly T To;
    public readonly E Value;
    (...)
}
```

## Example 

Now, we are going to make an example of using DirectedEdgedGraph to model a graph with values attached to each edge.

Moreover, we are going to model a cyclic graph here, so we will need special care to help some operations to finish, or they will be get stuck in an infinite loop. 

This is the graph to model: 

```C#
DirectedEdgedGraph<char, int> dg = new DirectedEdgedGraph<char, int>
{
   {'A','B', 2},
   {'B','C', 5},
   {'B','D', 2},
   {'C','D', 2},
   {'C','A', 4},
   {'D','A', 1},
   {'D','C', 1},
};

dg.ToString(); // A=>[2->B]; B=>[5->C],[2->D]; C=>[2->D],[4->A]; D=>[1->A],[1->C];

dg.ToString(", "); // A, B, C, D
dg.Edges.ToString(", "); // A->B, B->C, B->D, C->D, C->A, D->A, D->C
dg.EdgesWithValue.ToString(", "); // A-2->B, B-5->C, B-2->D, C-2->D, C-4->A, D-1->A, D-1->C

dg.RelatedTo('C').ToString(", "); //[D, 2], [A, 4]

dg.InverseRelatedTo('C').ToString(", "); //B, D

//DepthExplore and BreadthExplore does no make any considerations about cycle
//so in order for the algorithm to finish you have to control cycles manually:
HashSet<char> visited = new HashSet<char>();
dg.DepthExplore('B', c => !visited.Contains(c), c => { visited.Add(c); Console.Write(c + ", "); }, null);
//Returns: B, C, D, A,

HashSet<char> visited = new HashSet<char>();
dg.DepthExplore('B', c => !visited.Contains(c), c => visited.Add(c), c => Console.Write(c + ", "));
//Returns: A, D, C, B,

HashSet<char> visited = new HashSet<char>();
dg.BreadthExplore('B', c => !visited.Contains(c), c => { visited.Add(c); Console.Write(c + ", "); });
//Returns: B, C, D, A,


//Small set of edges that, if removed, will make the graph acyclic 
var back = dg.FeedbackEdgeSet(); //A=>[2->B]; B=>; C=>[2->D]; D=>;

var dg2 = dg.Clone();
dg2.RemoveAll(back.Edges);
//A=>; B=>[5->C],[2->D]; C=>[4->A]; D=>[1->A],[1->C];

dg2.CompilationOrder().ToString(", "); //A, C, D, B
dg2.CompilationOrderGroups().ToString(g => "[" + g.ToString(",") + "]", ", "); //[A], [C], [D], [B]

dg.Colapse(c => c == 'A').ToString(); //B=>C,D; C=>B,D; D=>B,C;  (removing 'A' but keeping edges)

dg.WhereEdges(e => e.Value > 2).ToString(); // A=>; B=>[5->C]; C=>[4->A]; D=>;
```

