## Database.UnsafeInsert

`UnsafeInsert` extension method let you create efficient low-level `INSERT` statements without creating the entities and inserting them one by one. 

The method is call `Unsafe` for reason: Validation won't take place in this low-level method. 

Still, you'll have a compile-time checked LINQ experience, with security and query filtering also taking place.

```C#
public static int UnsafeInsert<T, E>(this IQueryable<T> query, Expression<Func<T, E>> constructor)
    where E : Entity
```

`UnsafeInsert` takes an arbitrary `IQueryable<T>` and for each result creates a new entity using the `constructor` expression.

Example: 

```C#
int inserted = Database.Query<ProjectEntity>()
              .UnsafeInsert(p => new BugEntity
              {
                   Description = "Initial bug of " + p.Name,
                   Start = DateTime.Now,
                   End = null,
                   Fixer = null,
                   Discoverer =  null,
                   Status = Status.Open,
                   Project = p.ToLite(),
                   Ticks = 0,
              });

```

Will be translated to: 

```SQL
INSERT INTO BugEntity(Description, Start, [End], idFixer, 
idDiscoverer_Customer, idDiscoverer_Developer, idStatus, idProject, 
Ticks)
SELECT ('Initial bug of ' + ISNULL(s0.Name, '')), convert(datetime, '2014-08-26T11:38:19', 126), NULL, NULL, 
NULL, NULL, 0, s0.Id, 
0 FROM (
  (SELECT pdn.Name, pdn.Id
  FROM ProjectEntity AS pdn)
) AS s0;

SELECT @@rowcount
```

This API requires you to set every single required field, because *the initial values set in the field initializers are not accessible to the LINQ provider*. The `constructor` should be a `new` expression, but can also contain calls to `SetReadOnly` and `SetMixin`. For example: 


```C#
int inserted = Database.Query<ProjectEntity>()
             .UnsafeInsert(p => new BugEntity
             {
                 Description = "Initial bug of " + p.Name,
                 Start = DateTime.Now,
                 End = null,
                 Fixer = null,
                 Discoverer = null,
                 Status = Status.Open,
                 Project = p.ToLite(),
                 Ticks = 0,
             }.SetReadonly(b => b.CreationTime, b => DateTime.Now)
             .SetMixin((CorruptMixin c) => c.Corrupt, b => true));
```

Before deleting the entities, `PreUnsafeInsert` will be called in [EntityEvents](EntityEvents.md). 

## Database.UnsafeInsertMList

Allows you to send low-level `INSERT` statements to MListTables.

```C#
public static int UnsafeInsertMList<T, E, V>(this IQueryable<T> query, 
	Expression<Func<E, MList<V>>> mListProperty,  
	Expression<Func<T, MListElement<E, V>>> constructor)
    where E : Entity)
```

The method takes a expression to `mListProperty` to unambiguously determine the MListTable, and a `constructor` that has to create the `MListElement<E, V>`.

Example: 

```SQL
INSERT INTO BugDNComments(idParent, HasValue, Text, Date, 
idWriter_Customer, idWriter_Developer)
SELECT s2.Id, 1, (ISNULL((ISNULL(('Coment of ' + ISNULL(s2.Name, '')), '') + ' in '), '') + ISNULL(s2.Description, '')), convert(datetime, '2014-08-26T13:07:50', 126), 
NULL, s2.Id1 FROM (
  (SELECT bdn.Id, ddn.Name, bdn.Description, ddn.Id AS Id1
  FROM DeveloperEntity AS ddn
  CROSS JOIN BugEntity AS bdn)
) AS s2;
SELECT @@rowcount
```

 
