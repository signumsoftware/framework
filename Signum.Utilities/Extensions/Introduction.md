# What is an extension method?

[ExtensionMethods](http://msdn.microsoft.com/en-us/library/bb383977.aspx)
are a nice feature added in C# 3.0 that allow us to 'pretend' that
a method is an instance method of an object where really it's defined in
a static class outside of the type. This small difference can make a
huge step forward in readability:

Code with Linq query like this:

```c#
var query2 = people
			.Where(p => p.Age > 20)
			.OrderByDescending(p => p.Age)
			.ThenBy(p => p.Name));
```

will look like this without extension methods:

```c#
var query2 = Enumerable.ThenBy(
                Enumerable.OrderBy(
                   Enumerable.Where(
                        people, 
                   p => p.Age > 20),
                p => p.Age),
             p => p.Name):
```

because all these methods are not implemented over
`IEnumerable<T>`.

The library avalanche (Advanced Topic)
--------------------------------------

This language feature has become so useful that many utility projects
have grown around the concept, an active [question](http://stackoverflow.com/questions/271398/what-are-your-favorite-extension-methods-for-c-codeplex-com-extensionoverflow) about this.

It looks like everybody wants to make an 'standard' set of extension
methods, and in the way they introduce a new library, making the problem
bigger.

So, why bother doing a new one?

-   **Consistency**: It looks better for Signum Framework to depend on
    Signum.Utilities than on an external library.
-   **Control**: We prefer to have control over the library so we can
    add code we need.
-   **Clutter**: Extension methods tend to create clutter on your
    IntelliSense. We follow [Framework Design Guidelines](http://blogs.msdn.com/b/mirceat/archive/2008/03/13/linq-framework-design-guidelines.aspx).
