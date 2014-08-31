# Database.Query `Lite<T>` support

`Lite<T>` is fully supported in queries. If you don't know what a `Lite<T>` is, take a look [here](../Signum.Entities/Lite.md). 

### Navigating `Lite<T>` relationships
At the database level there's just no difference between a `Lite<T>` reference an a normal one. `Lite<T>` objects are just a convenient way to tell the retriever when to finish (to avoid retrieving the whole database) and to delimiter the graph that will be sent to the client application. 

But while you're writing Linq to Signum queries, `Lite<T>` relationships are just an annoyance that you can workaround using `Entity` or `EntityOrNull` properties of `Lite<T>`.



```C#
var result = from b in Database.Query<BugDN>()
             select new
             {
                 b.Description,
                 b.Project.Entity.Name
             };
```

```SQL
SELECT bdn.Description, pdn.Name
FROM BugDN AS bdn
LEFT OUTER JOIN ProjectDN AS pdn
  ON (bdn.idProject = pdn.Id)
```

`Retrieve` method is not supported in queries, because will be misleading... you're already in the database! You don't need to retrieve!. So this query:

```C#
 from b in Database.Query<BugDN>()
 where b.Project.Retrieve().Name == "Framework"
 select new
 {
     b.Description,
 }

//throws InvalidOperationException("The expression can not be translated to SQL: new ProjectDN(bdn.idProject)")
```

### Using `Lite<T>` as a result
 
The second use of `Lite<T>` objects is to get them out from the database using a Linq query. You can create `Lite<T>` objects explicitly using `.ToLite()` extension method on any entity, or just get them if they are already in your data model: 

```C#
//The first property is an explicit lazy, while the second is lazy just because BugDN.Project is Lazy. 
var result = from b in Database.Query<BugDN>()
             select new 
			 { 
				Bug = b.ToLazy(), //Explicit ToLite() because b is a BugDN 
                Project = b.Project //Implicit because b.Project is already a Lite<ProjectDN>
             }; 
```

That get's translated to just:

```SQL
SELECT bdn.Id, bdn.ToStr, bdn.idProject, pdn.ToStr AS ToStr1
FROM BugDN AS bdn
LEFT OUTER JOIN ProjectDN AS pdn
  ON (bdn.idProject = pdn.Id)
```

**Note:** `ToLite` method is defined in `Lite` static class, in `Signum.Entities`. 

Of course, this also works with `ImplementedBy` and `ImplementedByAll` references.

```C#
var result = from b in Database.Query<BugDN>()
             select b.Discoverer.ToLite(); //Polymorphic lite 
```

Notice how it joins to the different implementation to find the `ToStr` columns. 

```SQL
SELECT bdn.idDiscoverer_Customer, bdn.idDiscoverer_Developer, cdn.ToStr, ddn.ToStr AS ToStr1
FROM BugDN AS bdn
LEFT OUTER JOIN CustomerDN AS cdn
  ON (bdn.idDiscoverer_Customer = cdn.Id)
LEFT OUTER JOIN DeveloperDN AS ddn
  ON (bdn.idDiscoverer_Developer = ddn.Id)
```

The result of this query will be statically typed to `List<Lite<IBugDiscoverer>>` but at run-time, thanks to `Lite<T>` being co-variant, the elements will be either `Lite<CustomerDN>` or `Lite<DeveloperDN>`.

### Comparing Lite\<T>

Just as entities, `Lite<T>` can be compared with `==` and `!=` operators and `Is` extension method. Both operands have to be `Lite<T>`.  

```C#
Lite<DeveloperDN> dev = //...

var discovererComments = from b in Database.Query<BugDN>()
                         where b.Discoverer.ToLite() == discoverer
                         select c.Date;
```

You can also use `RefersTo` extension method to compare a `Lite<T>` with a `T`: 

```C#
Lite<DeveloperDN> dev = //...

var discovererComments = from b in Database.Query<BugDN>()
                         where discoverer.RefersTo(b.Discoverer)
                         select c.Date;
```

One annoying consequence of `Lite<T>` being co-variant, (and implemented as an `interface`) is that now the C# compiler is happy comparing `Lite<T>` with `T`, so this **unfortunately compiles**, throwing an exception: 

```C#
Lite<DeveloperDN> dev = //...

var result = from b in Database.Query<BugDN>()
              select b.Discoverer == dev; //Polymorphic lite

//throws InvalidOperationException("Imposible to compare expressions of type IBugDiscoverer == Lite<DeveloperDN>");
```

