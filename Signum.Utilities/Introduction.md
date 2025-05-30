# Introduction

Signum.Utilities is the very basic assembly of Signum Framework. It's
referenced by any other assembly in the framework and, can be used
independently.

Signum.Utilities is just a set of general purpose tools, following the
design lines below:

-   **Small over Big:** Fill small gaps in the .Net Framework, we don't
    want big features in Signum.Utilities.
-   **Functional over Imperative:** Once you get used, functional style
    is more readable, scalable and shorter. Lambda expressions are here
    to stay.
-   **Handy over Intellectually gratifying:** No [Y-Combinator](http://en.wikipedia.org/wiki/Y_combinator) here.
-   **No dependencies:** Without dependent assemblies it can easily be
    added to any project.

Also, where Signum.Utilities really shines is when writing loading
programs for your legacy data: Yes, Signum Framework forces you to
change your Database and this is not something Frameworks usually do,
but it does so for a good reason and we help you in the process giving
you powerful tools to manipulate your legacy data making it fit in your
freshly designed entity model, normalizing the data in the process.


### About the Documentation

This library is different to the other ones. Here we have tiny methods,
usually 2 or 3 lines long, that are easy to understand.

The signature of the method is usually enough and, if there's any doubt,
just look at the source code (Signum Framework is Open Source!).

The main task of Signum.Utilities documentation is to improve
discoverability of the library and show examples of usage. 

### Signum.Utilities Motivation (Advanced Topic)

.Net Framework is a huge framework containing lots of functionality. 
It is so complete that is hard to find functionality not
considered already in the framework.

Some API, however, are not as convenient to use as they could be. They
where designed for .Net 1.1 and, with the arrival of LINQ, the way we
code has changed so much that some API look a bit aged now:

-   **Generics:** Until .Net 2.0 there was no Generic support, and even
    in .Net 2.0 they were kind of 'embarrassed' of using generics. It
    looked like an isolated experiment in System.Collection.Generics.

   *Example: Enum class lacks some generic facilities.*

-   **Delegates:** Even having delegates from day 1, the lack of a
    convenient syntax to express anonymous delegates (or lambda
    expressions), and the inertia of Java design patterns, produced a
    misuse of delegates.

   *Example: IComparer or IComparer<esc><T></esc> are interfaces with just
one method, they should be delegates instead.*

-   **Extension Methods:** This feature allows improvement of client
    code readability and discoverability while preserving good
    architecture in your library (assembly dependencies, for example) by
    making static external methods look like instance methods. This
    feature, however, was added too late in the framework (.Net 3.5) so
    almost no class (but LINQ) uses it jet.

   *Example: Converter.ChangeType could be an extension method over any
IConvertible instead.*