# LambdaComparer 
In many .Net and SF API a `IComparer` or `IEqualityComparer` is taken as a parameter of the constructor of a data structure or a method. 

Implementing a `IComparer` is too ceremonial, lambda comparer makes it simpler.    

```C#
public class LambdaComparer<T, S> : IComparer<T>, IEqualityComparer<T>, IComparer, IEqualityComparer
{
   public LambdaComparer(Func<T, S> func){..}
   
   (...)
}
```

Example: 

```C#
List<Person> people = (...);
people.Sort(new LambdaComparer<Person, string>(p=>p.Name)); 
```