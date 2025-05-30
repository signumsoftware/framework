# TreeHelper
 
Since this class is not used very often, the methods are not extension method any more to avoid clutter. This class has also been renamed from `TreeExtensions` to `TreeHelper`.

### ToTree
Given a tree where the child has a reference to the parents, returns a tree of Node<T> relating each parent to their children. 

```c#
public static ObservableCollection<Node<T>> ToTreeC<T>(IEnumerable<T> collection, Func<T, T> getParent)
public static ObservableCollection<Node<T>> ToTreeS<T>(IEnumerable<T> collection, Func<T, T?> getParent)

public class Node<T> : INotifyPropertyChanged
{
    public T Value { get; set; }
    public ObservableCollection<Node<T>> Children { get; set; }

    public Node(){...}
    public Node(T value){...}
}
```

This method is usefull to populate TreeViews and the reason it returns `ObservableCollection<T>`, instead of `List<T>` is to avoid a [bug in WPF](http://stackoverflow.com/questions/19511341/binding-to-list-causes-memory-leak) that produces memory leaks when binding to plain lists. 

Example: 

```c#
¬List<¬Node<int>> roots = new[] { 5,6,7,8,9 }.ToTreeS(a => (a / 2).DefaultToNull());
¬Node<int> parent = roots.Single();
//parent is a node like this: 
//Node 1
//  Node 2
//    Node 5
//    Node 4
//      Node 8
//      Node 9
//  Node 3
//    Node 6
//    Node 7
```

> **Note:** If you're curious, below is the function we used to write the text, it uses [anonymous recursion](http://blogs.msdn.com/wesdyer/archive/2007/02/02/anonymous-recursion-in-c.aspx), and many `StringExtensions` like `FormatWith`, `Add`, `ToString` and `Ident`:

```c#
¬Func<¬Node<int>, string> toStr = null;
toStr = node => "Node {0}".FormatWith(node.Value).Add(node.Childs.ToString(toStr, "\r\n").Indent(2), "\r\n");
¬Console.WriteLine(toStr(parent));
```



### BreathFirst 
Allows to explore in a [BreathFirst](http://en.wikipedia.org/wiki/Breadth-first_search) an object and all its children recursively. Doesn't keep track of items so it won't work on a general graph, just trees. For a general graph use `DirectedGraph` instead.  

```c#
public static ¬IEnumerable<T> BreathFirst<T>(T root, ¬Func<T, ¬IEnumerable<T>> childs)
```
### DepthFirst 
Allows to explore in a [DepthFirst](http://en.wikipedia.org/wiki/Depth-first_search) an object and all its children recursively. Doesn't keep track of items so it won't work on a general graph, just trees. For a general graph use `DirectedGraph` instead.  

```c#
public static IEnumerable<T> DepthFirst<T>(T root, Func<T, IEnumerable<T>> children)
```

