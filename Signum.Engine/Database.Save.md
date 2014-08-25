# Database.Save

Saving is the process of storing entities in the database, inserting them if they are new, or updating them if they are already there. 

When an entity is saved for the first time, an `Id` is assigned to it. 

Saving has full support for graphs of entities, so you can have a graph of `IdentifiableEntity` with cycles and the `Save` method should be able to deal with it. 

Also `Save` takes a `IdentifiableEntity`, so it's not possible to save just a `Modifiable` (an `EmbeddedEntity` or a `MList<T>` for example). In fact, when a Modifiable is `SelfModified`, the modification is propagated to the parent `IdentifiableEntity` before saving, so the whole IdentifiableEntity is saved. See more about this in [Change Tracking](../Signum.Entities/ChangeTracking.md).

Before saving, every `Modifiable` is tested for integrity using `IntegrityCheck`. If the test fails, an `ApplicationException` is thrown and the `Transaction` is roll-backed, so you never have invalid data in your database. You can avoid this behavior for some particular entities and integrity rules using Corruption. See more about this in [Validation](../Signum.Entities/Validation.md).

Finally, before saving, the method `PreSaving` is called on every `Modifiable` in the graph. You can use it to calculate some redundant values before saving.

### Save

There are three variations of Save method

```C#
//Simple overload for saving an entity. Modifies the entity by reference, 
//calling PreSaving and assigning Id if it's new, and returns the entity itself.   
public static T Save<T>(this T entity) where T : class, IIdentifiable

//Saving some random entities at once. 
public static void SaveParams(params IIdentifiable[] entities)


//Same as above but for saving IEnumerables of entities. Nice when creating entities using Linq queries. 
public static void SaveList<T>(this IEnumerable<T> entities) where T : class, IIdentifiable
```

Example: 

```C#
new UserDN
{
   UserName = "john"
}.Save();

0.To(10).Select(i=> new UserDN
{
   UserName = "john"
}).SaveList();
```

### Preserving Object Identity

Once saved, entities are stored in the current `ObjectCache`. This information is usually lost but you can keep it using a wider scope [ObjectCache](ObjectCache.md). 

### How the Saver works (Advanced)

In case you are interested in the details, this is what the saver is actually doing: 

1. Generates a `DirectedGraph` of all the reachable `Modifiables`.
* Executes `Modifiable.PreSaving` and `EntityEvents<T>.PreSaving` on every element in the graph, potentially re-generating the graph. 
* Tests `IntegrityCheck` on every `Modifiable`.
* Uses an inverted graph to propagate modifications from each `Modifiable` to the parent `IdentifiableEntity`.
* Collapses the original graph of `Modifiable` to a graph of `IdentifiableEntity`.
* Invokes `EntityEvents<T>.Saving` on every node. 
* Identifies all the edges that mean a dependency on saving order (the end of the edge goes to a `IdentifiableEntity` that is new).
* Identifies feedback edges, if any, in this dependency graph using [this algorithm](http://www.cs.stonybrook.edu/~algorith/files/feedback-set.shtml).
* Groups the identifiable by type and `IsNew` but taking the dependencies into account.
* Saves each group (layer) using cached `SqlPreCommand` factories to `UPDATE` or `INSERT` 1, 2, 4, 8, or 16 entities together.
	* For collections is more complicated, using cached `SqlPreCommand` factories to `UPDATE`/`INSERT`  1, 2, 4, 8, or 16 MList elements `DELETE` the removed entities. 
* Final updates are sent to fix the problem made by the feedback edges. 
