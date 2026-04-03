
#  ImmutableStack

The most simple immutable data structure, can be used as a stack or as a list, if you are not planning to insert elements in the middle (or the end). 

Internally is just an immutable linked list of nodes, where each node has one value and a link to the next. So Push, Pop and Peak are all O(1).  

```C#
public class ImmutableStack<T>: IEnumerable<T>
{
   public static ImmutableStack<T> Empty { get; } //The only way to get a Empty immutable stack. 

   public bool IsEmpty { get; } //returns true if the current instance is empty.
   public ImmutableStack<T> Push(T value) //Creates a new immutable stack with value pushed in the head
   public ImmutableStack<T> Pop() //returns a new stack without the head
   public T Peek() //returns the element in the head
 
   public virtual IEnumerator<T> GetEnumerator()
   public override string ToString()
}

public static class ImmutableStackExtensions
{
    //Reverse the order of elements
    //Stack: [3]->[2]->[1]->[]
    //Result: [1]->[2]->[3]->[]
    public static ImmutableStack<T> Reverse<T>(this ImmutableStack<T> stack); 

    //Reverse the order of elements and concatenates it with initial.
    //Stack: [3]->[2]->[1]->[]
    //Initial: [4]->[]
    //Result: [1]->[2]->[3]->[4]->[]
    public static ImmutableStack<T> Reverse<T>(this ImmutableStack<T> stack, ImmutableStack<T> initial);
}
```

Also, since `ImmutableStack` implements `IEnumerable`, all the Linq extension methods are applicable. 

Example: 

```C#
ImmutableStack<int> stack = ImmutableStack<int>.Empty;

for (int i = 0; i < 10; i++)
    stack = stack.Push(i);

Console.WriteLine(stack.ToString());
//Writes: [9, 8, 7, 6, 5, 4, 3, 2, 1, 0]

while (!stack.IsEmpty)
{
    Console.Write(stack.Peek());
    stack = stack.Pop();
}
//Writes: 9876543210

Console.WriteLine(stack.ToString());
//Writes: []
```