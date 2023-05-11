# EntityCache

EntityCache uses the scope pattern to avoid duplicated entities in a region of your code. 

If you come from a ORM using Unit of Work patterns *(i.e.: LINQ to SQL, Entity Framework or NHibernate)*, you're used to have a context object that does a lot of things: 

* **Keep track of entity changes:** In Signum Framework the entities itself know about their changes.
* **Create the SqlConnection:** Here [Connector class](Connection/Connector.md) does it, usually implicitly.
* **Create the SqlTransaction:** Here [Transaction class](Connection/Transaction.md) does it, if necessary.
* **Commit all the changes:** Here the changes are committed immediately as you call `Save`, `Delete`, etc...
* **Keep track of entity instances to avoid clones:** Here, `EntityCache` is responsible for that, if necessary. 


We choose `EntityCache` to be opt-in because Signum Framework is designed to make easy creating global caches of shared entities, and sending entities back and forth from the client application to the server, without having to attach/detach the entities from a context in any operation. 


### The problem

```C#
var bugA = Database.Retrieve<BugEntity>(1);
var bugB = Database.Retrieve<BugEntity>(1);

bugA.Equals(bugB); //returns true
bugA == bugB; //returns false
```

### The solution

Keeping the identity of objects is a hard problem to solve when writing an Object-Relational Mapping technology. In order to make it completely transparent it's necessary to maintain huge data structures with all the previously retrieved and saved objects.

In our opinion, a solution like this doesn't make sense because usually you just don't need to keep identity at all, it takes to much memory, and doesn't play nicely with LINQ. 

If you get bite by our decision in some code, you can bring back this data structure using `EntityCache`. 

```C#
using(new EntityCache())
{
   var bugA = Database.Retrieve<BugEntity>(1);
   var bugB = Database.Retrieve<BugEntity>(1);
   
   bugA.Equals(bugB); //returns true
   bugA == bugB; //returns true too!!!!
}
```

Internally it just creates a wider `EntityCache` (just a dictionary of objects by type and Id) that wraps every operation (`Save` and `Retrieve`, even queries retrieving entities), instead of letting each operation from having their own isolated one. 

Nested `EntityCache` became silent, as `Transactions`.

### `EntityCache.Add` and `EntityCache.AddFullGraph`

You can manually add some objects to an object cache like this:  

```C#
var developer = Database.Retrieve<DeveloperEntity>(3);
Console.WriteLine(developer.Name);

using (new EntityCache())
{
    EntityCache.Add(developer); //added to the internal dictionary

    var bug = Database.Query<BugEntity>().Where(b => b.Fixer.Is(developer)).First(); //Fixer retrieved from the internal dicitonary
    
    object.ReferenceEquals(bug.Fixer, developer); //returns true!
}
```

Using `EntityCache.AddFullGraph` instead, you can add all the `Entity` in the object graph.  

### `EntityCache(ForceNew)`

You can create an `EntityCache` that does not become silent if there's a parent one. Useful for `Saving` and `PreSaving` [EntityEvents](EntityEvents.md).

```C#
using(new EntityCache())
{
   var bugA = Database.Retrieve<BugEntity>(1);
   using(new EntityCache(EntityCacheType.ForceNew))
   {
	   var bugB = Database.Retrieve<BugEntity>(1);
	   
	   bugA.Equals(bugB); //returns true
	   object.ReferenceEquals(bugA, bugB); //returns false
	}
}
```


### `EntityCache(ForceNewSealed)`

You can create an `EntityCache` that does not become silent if there's a parent one and also sets `Modified` in all the retrieved `Modifiable` objects to `Sealed`. Usefull for global shared caches. 

```C#
using(new EntityCache())
{
   var bugA = Database.Retrieve<BugEntity>(1);
   using(new EntityCache(EntityCacheType.ForceNew))
   {
	   var bugB = Database.Retrieve<BugEntity>(1);
	   
	   bugA.Equals(bugB); //returns true
	   object.ReferenceEquals(bugA, bugB); //returns false

       bugB.Description = "Changed"; // throws new InvalidOperationException("The instance Bug is sealed and can not be modified");
	}
}
```
