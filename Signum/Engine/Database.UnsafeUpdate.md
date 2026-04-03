## Database.UnsafeUpdate 

`UnsafeUpdate` extension method let you create efficient low-level `UPDATE` statements without retrieving the entities and modifying them one by one.

The method is call `Unsafe` for reason: Validation won't take place in this low-level method. 

Still, you'll have a compile-time checked LINQ experience, with security and query filtering also taking place.

This methods are low-level and produce just one `UPDATE` statement. Modifying more than one table at the same time is not allowed, this includes modifying collection properties. 

There are four variations of `UnsafeUpdate`, for entities and `MListElements`, and for simpler or wider context (`Part` termination).  

In all of the variation `PreUnsafeUpdate` will be called in [EntityEvents](EntityEvents.md) before any `UPDATE` is executed:

### Database.UnsafeUpdate

This is the simplest version, allows updating properties in the main table of an entity. In order to do that we need the collaboration of three methods: `UnsafeUpdate`, `Set` and `Execute`. 

```C#
public static IUpdateable<E> UnsafeUpdate<E>(this IQueryable<E> query)
	where E : Entity

public interface IUpdateable<T> : IUpdateable
{
    IUpdateable<T> Set<V>(
		Expression<Func<T, V>> propertyExpression, 
		Expression<Func<T, V>> valueExpression);
}

public static int Execute(this IUpdateable update)
```

Example: 

```C#
int updated = Database.Query<BugEntity>()
             .Where(b => b.Description.StartsWith("A"))
             .Take(10)
             .UnsafeUpdate()
             .Set(b => b.Description, b => b.Description + " Updated!")
             .Set(b => b.Start, b => b.Start.AddDays(1))
             .Execute();
```

Translates to:

```SQL
UPDATE BugEntity SET
  Description = (ISNULL(s2.Description, '') + ' Updated!'),  --SqlParameters inlined
  Start = DATEADD(day, 1, s2.Start)
FROM (
  (SELECT TOP (10) bdn.Id, bdn.Description, bdn.Start
  FROM BugEntity AS bdn
  WHERE bdn.Description LIKE ('A' + '%'))
) AS s2
WHERE (BugEntity.Id = s2.Id);

SELECT @@rowcount
```

Notice how:
 
1. `UnsafeUpdate` returns a `IUpdateable<T>`, starting the `Set` sequence. 
2. Once there we can add as many `Set` as we need, each with an `propertyExpression` and a `valueExpression`.
3. Finally we call `Execute` to actually send the `UPDATE` command. 

It's easy to forget the last `Execute` call, so it's a good idea to start all the update queries assigning the affected rows in an `int` (not `var`!) variable like in the example. 

The current API is definitely more complicated that we would like, but scales better to more complex examples, like updating read-only properties or mixins: 

```C#
int updated = Database.Query<BugEntity>()
             .Where(b => b.Description.StartsWith("A"))
             .Take(10)
             .UnsafeUpdate()
             .Set(b => b.SetReadonly(b2 => b2.CreationDate), b => DateTime.Now)
             .Set(b => b.Mixin<CorruptMixin>().Corrupt, b => false)
             .Execute();
```

### Database.UnsafeUpdatePart

This variation let you have a wider context for your `valueExpression`. Even if at the end the query only updates one table, using `UnsafeUpdatePart` we can join many tables to get values from them.

```C#
public static IUpdateablePart<A, E> UnsafeUpdatePart<A, E>(this IQueryable<A> query, 
	Expression<Func<A, E>> partSelector)
	where E : Entity

public interface IUpdateablePart<A, T> : IUpdateable
{
    IUpdateablePart<A, T> Set<V>(
		Expression<Func<T, V>> propertyExpression, 
		Expression<Func<A, V>> valueExpression);
}

public static int Execute(this IUpdateable update)
```

Notice how now `UnsafeUpdatePart` returns a `IUpdateablePart<A, E>` where `A` represents the wider context (often an anonymous type), and `E` is the entity. 
* The `propertyExpression` should be a property of the entity `E`.
* The `valueExpression` could be any expression from the whider context `A`. 

Example: 

```C#
int updated = Database.Query<BugEntity>()
             .Where(b => b.Description.StartsWith("A"))
             .Take(10)
             .UnsafeUpdatePart(b => b.Fixer)
             .Set(d => d.Name, b => b.Fixer.Name + "Fixer of" + b.Description)
             .Execute();
```

Here we need information of the `BugEntity` to update the `DeveloperEntity` in the `Fixer` property. 

That gets translated to: 

```SQL
UPDATE DeveloperEntity SET 
  Name = (ISNULL((ISNULL(s2.Name, '') + 'Fixer of'), '') + ISNULL(s2.Description, '')) 
FROM (
  (SELECT TOP (10) bdn.idFixer, ddn.Name, bdn.Description
  FROM BugEntity AS bdn
  LEFT OUTER JOIN DeveloperEntity AS ddn
    ON (bdn.idFixer = ddn.Id)
  WHERE bdn.Description LIKE ('A' + '%')) --SqlParameters inlined
) AS s2
WHERE (DeveloperEntity.Id = s2.idFixer);

SELECT @@rowcount
```


### Database.UnsafeUpdateMList

Using `UnsafeUpdateMList` you can also execute `UPDATE` commands on MListTables. 

```C#
public static IUpdateable<MListElement<E, V>> UnsafeUpdateMList<E, V>(this IQueryable<MListElement<E, V>> query)
    where E : Entity

public interface IUpdateable<T> : IUpdateable
{
    IUpdateable<T> Set<V>(
		Expression<Func<T, V>> propertyExpression, 
		Expression<Func<T, V>> valueExpression);
}

public static int Execute(this IUpdateable update)
```

`UnsafeUpdateMList` is exactly like `UnsafeUpdate`, but taking an `IQueryable<MListElement<E, V>>` instead. 

The `propertyExpression` and `valueExpression` can now access and set properties of the `MListElement<E, V>`, including implicit internal columns like `RowID`, `Parent`, `Order`, etc..

Example using `MListElements` expression:


```C#
int updated = Database.MListQuery((BugEntity b)=> b.Comments)
             .Where(mle => mle.Element.Text.StartsWith("Hi"))
             .Take(10)
             .UnsafeUpdateMList()
             .Set(mle => mle.Order, mle => mle.Order + 1)
             .Set(mle => mle.Element.Text, mle => mle.Element.Text + "- reordered")
             .Execute();
```

Translates to:

```SQL
UPDATE BugDNComments SET
  Text = (ISNULL(s2.Text, '') + '- reordered') --SqlParameters inlined
FROM (
  (SELECT TOP (10) bdnc.Id, bdnc.Text
  FROM BugDNComments AS bdnc
  WHERE bdnc.Text LIKE ('Hi' + '%'))
) AS s2
WHERE (BugDNComments.Id = s2.Id);

SELECT @@rowcount
```
 

### Database.UnsafeUpdateMListPart

Closing the circle, with `UnsafeUpdateMListPart` you can send `UPDATE` commands on MListTables, and also have a wider scope to get values from. 

```C#
public static IUpdateablePart<A, MListElement<E, V>> UnsafeUpdateMListPart<A, E, V>(this IQueryable<A> query, 
	Expression<Func<A, MListElement<E, V>>> partSelector)
    where E : Entity

public interface IUpdateablePart<A, T> : IUpdateable
{
    IUpdateablePart<A, T> Set<V>(
		Expression<Func<T, V>> propertyExpression, 
		Expression<Func<A, V>> valueExpression);
}

public static int Execute(this IUpdateable update)
```

Now let's make a mor complex example, movin the Comment from a `BugEntity` to the inmediately next, if exists:

```C#
var updated = (from b1 in Database.Query<BugEntity>()
               join b2 in Database.Query<BugEntity>() on b1.Start equals b2.End
               from mle in b1.MListElements(b => b.Comments)
               select new { mle, b2 })
               .UnsafeUpdateMListPart(a => a.mle)
               .Set(mle => mle.Parent, a => a.b2)
               .Execute(); 
```

Translated to: 

```SQL
UPDATE BugDNComments SET
  idParent = s5.Id1
FROM (
  (SELECT s4.Id, bdn1.Id AS Id1
  FROM BugEntity AS bdn
  INNER JOIN BugEntity AS bdn1
    ON (bdn.Start = bdn1.[End])
  CROSS APPLY (
    (SELECT bdnc.Id
    FROM BugDNComments AS bdnc
    WHERE (bdnc.idParent = bdn.Id))
  ) AS s4)
) AS s5
WHERE (BugDNComments.Id = s5.Id);

SELECT @@rowcount
```







 

