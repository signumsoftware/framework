# LINQ `Lite<T>` support

`Lite<T>` is fully supported in queries. If you don't know what a `Lite<T>` is, take a look [here](../Signum.Entities/Lite.md). 

### Navigating `Lite<T>` relationships
`Lite<T>` objects are just a convenient way to tell the Framework when to load an entity eagerly and when lazily, and to delimiter the graph that will be sent to the client application (Windows/React). 

In the SQL database schema there's no difference between an entity having a property of type `Lite<T>` or `T`: Both will create a foreign key to the related table, and
 while you're writing Linq to Signum queries, `Lite<T>` relationships are just an annoyance that you can workaround using `Entity` or `EntityOrNull` properties of `Lite<T>`.

```C#
var result = from b in Database.Query<BugEntity>()
             select new
             {
                 b.Description,
                 b.Project.Entity.Name
             };
```

```SQL
SELECT bdn.Description, pdn.Name
FROM BugEntity AS bdn
LEFT OUTER JOIN ProjectEntity AS pdn
  ON (bdn.idProject = pdn.Id)
```

`Retrieve` method is not supported on queries, because will be misleading... you're already in the database! You don't need to retrieve!. So this query fails:

```C#
 from b in Database.Query<BugEntity>()
 where b.Project.Retrieve().Name == "Framework"
 select new
 {
     b.Description,
 }


//throws InvalidOperationException("The expression can not be translated to SQL: new ProjectEntity(bdn.idProject)")
```

### Using `Lite<T>` as a result
 
The second use of `Lite<T>` objects is to use them for the set of results of a Linq query. You can create `Lite<T>` objects explicitly using `.ToLite()` extension method on any entity, or just get them if they are already `Lite<T>` in your data model: 

```C#
//The first property is an explicit lite, while the second is lite because BugEntity.Project is `Lite<ProjectEntity>`. 
var result = from b in Database.Query<BugEntity>()
             select new 
			 { 
				Bug = b.ToLite(), //Explicit ToLite() because b is a BugEntity 
                Project = b.Project //Implicit because b.Project is already a Lite<ProjectEntity>
             }; 
```

That get's translated to just:

```SQL
SELECT bdn.Id, bdn.ToStr, bdn.idProject, pdn.ToStr AS ToStr1
FROM BugEntity AS bdn
LEFT OUTER JOIN ProjectEntity AS pdn
  ON (bdn.idProject = pdn.Id)
```

> **Note:** `ToLite` method is defined in `Lite` static class, in `Signum.Entities`. 

Of course, this also works with `ImplementedBy` and `ImplementedByAll` references.

```C#
var result = from b in Database.Query<BugEntity>()
             select b.Discoverer.ToLite(); //Polymorphic lite 
```

Notice how it joins to the different implementation to find the `ToStr` columns. 

```SQL
SELECT bdn.idDiscoverer_Customer, bdn.idDiscoverer_Developer, cdn.ToStr, ddn.ToStr AS ToStr1
FROM BugEntity AS bdn
LEFT OUTER JOIN CustomerEntity AS cdn
  ON (bdn.idDiscoverer_Customer = cdn.Id)
LEFT OUTER JOIN DeveloperEntity AS ddn
  ON (bdn.idDiscoverer_Developer = ddn.Id)
```

The result of this query will be statically typed to `List<Lite<IBugDiscoverer>>` but at run-time, thanks to `Lite<T>` being co-variant, the elements will be either `Lite<CustomerEntity>` or `Lite<DeveloperEntity>`.

### Comparing Lite\<T>

Just as entities, `Lite<T>` can be compared with `==` and `!=` operators and `Is` extension method. Both operands have to be `Lite<T>`.  

```C#
Lite<DeveloperEntity> dev = //...

var discovererComments = from b in Database.Query<BugEntity>()
                         where b.Discoverer.ToLite() == discoverer
                         select c.Date;
```

You can also use `RefersTo` extension method to compare a `Lite<T>` with a `T`: 

```C#
Lite<DeveloperEntity> dev = //...

var discovererComments = from b in Database.Query<BugEntity>()
                         where discoverer.RefersTo(b.Discoverer)
                         select c.Date;
```

One annoying consequence of `Lite<T>` being co-variant, (and implemented as an `interface`) is that now the C# compiler is happy comparing `Lite<T>` with `T`, so this **unfortunately compiles**, throwing an exception: 

```C#
Lite<DeveloperEntity> dev = //...

var result = from b in Database.Query<BugEntity>()
              select b.Discoverer == dev; //Polymorphic lite

//throws InvalidOperationException("Imposible to compare expressions of type IBugDiscoverer == Lite<DeveloperEntity>");
```

```Note:``` Signum.Analyzer restores the compile-time errors when he finds comparishons between `Lite<T>` and `T`, or between `Lite<OrangeEntity>` and `Lite<AppleEntity>`. 

