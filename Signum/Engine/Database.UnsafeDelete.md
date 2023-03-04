## Database.UnsafeDelete

`UnsafeDelete` extension method let you create efficient low-level `DELETE` statements without retrieving the entities and removing them one by one. 

```C#
public static int UnsafeDelete<T>(this IQueryable<T> query)
    where T : Entity
```

`UnsafeDelete` takes an arbitrary `IQueryable<T>` of entities and returns the number of rows affected. That means that queries like this can be written: 

```C#
int deleted = Database.Query<BugEntity>()
              .Where(b=>b.Description.StartWith("A"))
              .Take(10)
              .UnsafeDelete();
```

That will be translated to: 

```SQL
DELETE BugDNComments
FROM (
  (SELECT TOP (@p0) bdn.Id
  FROM BugEntity AS bdn
  WHERE bdn.Description LIKE (@p1 + '%'))
) AS s2
WHERE (BugDNComments.idParent = s2.Id);

DELETE BugEntity
FROM BugEntity AS bdn
WHERE bdn.Description LIKE (@p1 + '%');

SELECT @@rowcount
```

Notice how `UnsafeDelete` will also remove all the collection of the affected entities. 

Before deleting the entities, `PreUnsafeDelete` will be called in [EntityEvents](EntityEvents.md). 

> **Note:** There's actually nothing **unsafe**  about `UnsafeDelete`. `Database.Delete` actually calls `UnsafeDelete` under the covers, but the name was chosen for familiarity with [UnsafeUpdate](Database.UnsafeUpdate.md) and [Database.UnsafeInsert.md].


## Database.UnsafeDeleteMList

If you just want to `DELETE` collection elements, without affecting the owner entities, you can have low-level access to remove the rows in a MListTable using the overload that takes a  `IQueryable<MListElement<E, V>>`.

```C#
public static int UnsafeDeleteMList<E, V>(this IQueryable<MListElement<E, V>> mlistQuery)
    where E : Entity
``` 

Example using also `MListElements` extension method:

```C#
int deleted = Database.Query<BugEntity>()
	  .Where(b => b.Description.StartsWith("A"))
	  .Take(10)
	  .SelectMany(b => b.MListElements(bug => bug.Comments))
	  .UnsafeDeleteMList();
```

Translates to the SQL:

```SQL
DELETE BugDNComments
FROM (
  (SELECT s4.Id
  FROM (
    (SELECT TOP (@p0) bdn.Id
    FROM BugEntity AS bdn
    WHERE bdn.Description LIKE (@p1 + '%'))
  ) AS s2
  CROSS APPLY (
    (SELECT bdnc.Id
    FROM BugDNComments AS bdnc
    WHERE (bdnc.idParent = s2.Id))
  ) AS s4)
) AS s5
WHERE (BugDNComments.Id = s5.Id); --The translation could be optimized 
SELECT @@rowcount
```







 

