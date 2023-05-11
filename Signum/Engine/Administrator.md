# Administrator

Administrator class is the main facade for making modifications to create the database schema, synchronize it, or do administrative operations or abuses of the system that belong to load applications, not to typical production code.

## TotalGeneration 

`TotalGenerationScript` executes the pipeline of events in `Schema.GenerationScripts` that generates all the `SqlPreCommands` to clean the database and re-generate the database schema from Scratch.   


```C#
public static SqlPreCommand TotalGenerationScript()
```

`TotalGeneration` also invokes all the leaves in the `SqlPreCommand`, effectively re-creating the Schema. 

```C#
public static void TotalGeneration()
```

**Avoid calling this method on a production database.**


## TotalSynchronizeScript

`TotalSynchronizeScript` executes the pipeline of events in `Schema.SynchronizationScript` that generates all the `SqlPreCommands` to adapt the current database schema to the required one, adding, removing, modifying or renaming tables, columns, indexes, foreign key, constraints and certain special rows, like enums, types, symbols, queries, etc....  

```C#
public static SqlPreCommand TotalSynchronizeScript(bool interactive = true)
```

## ExistTable

Checks if a table is already created in the database. Useful when syncing.

```C#
public static bool ExistTable<T>()
public static bool ExistTable(Type type)
public static bool ExistTable(Table table)
```

## TryRetrieveAll

Retrieves all the entities in a table, if the table exists and taking potential renames into account. Useful when syncing. 

```C#
public static List<T> TryRetrieveAll<T>(Replacements replacements) where T : Entity
public static List<Entity> TryRetrieveAll(Type type, Replacements replacements)
```

## SetReadonly

Allows changing read-only properties with a backing field if necessary. Also usefull for [UnsafeInsert](Database.UnsafeInsert.md).  

```C#
public static T SetReadonly<T, V>(this T ident, Expression<Func<T, V>> readonlyProperty, V value)
    where T : ModifiableEntity
```

Example: 

```C#
BugEntity bug = new BugEntity().SetReadonly(b => b.CreationDate, DateTime.Now);
```

## SetId

Forces an entity to have a particular id. Useful loading legacy data when combined with `DisableIdentity`. 

```C#
public static T SetId<T>(this T ident, int? id)
    where T : Entity
```

Example: 

```C#
BugEntity bug = new BugEntity().SetId(10);
```


## DisableIdentity

Disables the identity mechanism in SQL Server to allow saving entities with forced ids (using `SetId`), and changes the behavior of the Table to include the `Id` in different `INSERT` commands. Avoid using it in multi-threaded scenarios. This method has to be called inside of a `Transaction` scope.   

```C#
public static IDisposable DisableIdentity(Table table)
```
There are also overloads to simplify using it for Entity tables and MList tables.  

```C#
public static IDisposable DisableIdentity<T>()
public static IDisposable DisableIdentity<T, V>(Expression<Func<T, MList<V>>> mListField)
          where T : Entity
```
Low-level method to disables the identity mechanism in SQL Server only. 

```C#
public static IDisposable DisableIdentity(ObjectName tableName)
```

## SaveDisableIdentity

Shortcut to save an entities (or collection of entities), creating a `Transaction` and calling `DisableIdentity`.

```C#
public static void SaveDisableIdentity<T>(T entities) where T : Entity
public static void SaveListDisableIdentity<T>(IEnumerable<T> entities) where T : Entity
```

## SetNew and SetNotModified
Allows to fake new entities as non-new, non-modified entities. Useful for saving entities without retrieving some of the parts. 

```C#
public static T SetNew<T>(this T ident) where T : Entity
public static T SetNotModified<T>(this T ident) where T : Modifiable
public static T SetNotModifiedGraph<T>(this T ident, int id)  where T : Entity
```

Example: 

```C#
new NoteEntity()
{
   Target = new BugEntity().SetNotModifiedGraph(1); // refers to BugEntity 1 without retrieving it
}.Save();
```


## RemoveDuplicates

Shortcut to remove duplicated of an entity `T` by a `key`, removing the element with greater `Id`. 


```C#
public static int RemoveDuplicates<T, S>(Expression<Func<T, S>> key)
   where T : Entity
{
    return (from f1 in Database.Query<T>()
            join f2 in Database.Query<T>() on key.Evaluate(f1) equals key.Evaluate(f2)
            where f1.Id > f2.Id
            select f1).UnsafeDelete();
}
```


### UpdateToStrings

If the definition of `ToString` changes in an entity, the cached `ToStr` column will get outdated. 

This family of methods help you update the `ToStr` column by retrieving the entities in intervals and update the `ToStr` column using `UnsafeUpdate` one by one. 

```C#
public static void UpdateToStrings<T>() where T : Entity, new()
public static void UpdateToStrings<T>(IQueryable<T> query) where T : Entity, new()
```

Or if an `expression` is provided to calculate the `ToStr`, do all the work in just one `UnsafeUpdate`. 

```C#
public static void UpdateToStrings<T>(Expression<Func<T, string>> expression) where T : Entity, new()
public static void UpdateToStrings<T>(IQueryable<T> query, Expression<Func<T, string>> expression) where T : Entity, new()
```

### PrepareTableForBatchLoadScope

Prepares a table for a batch load by `disableForeignKeys`, `disableMultipleIndexes` and `disableUniqueIndexes`. Can significantly improve performance in long load applications. 

```C#
public static IDisposable PrepareForBatchLoadScope<T>(
	bool disableForeignKeys = true, 
    bool disableMultipleIndexes = true, 
    bool disableUniqueIndexes = false) where T : Entity


public static IDisposable PrepareTableForBatchLoadScope(ITable table, 
	bool disableForeignKeys, 
	bool disableMultipleIndexes, 
	bool disableUniqueIndexes)
```

Example: 

```C#
using(Administrator.PrepareTableForBatchLoadScope<BugEntity>()) //Disables FKs and Indexes
{

   //Load one zillion bugs here

} //Restores FKs and Indexes
```

### DropUniqueIndexes

Drops all the unique indexes in a table, sometimes necessary to avoid temporal duplication. Will be restored when synchronized.  

```C#
public static void DropUniqueIndexes<T>() where T : Entity
```

### MoveAllForeignKeysScript

Moves all the possible foreign keys from an `oldEntity` to a `newEntity`. Useful if the two entities where duplicates.

```C#
//Just generates the script, use ExecuteLeaves to execute it
public static SqlPreCommand MoveAllForeignKeysScript<T>(Lite<T> oldEntity, Lite<T> newEntity)
   where T : Entity

//Executes the scripts using a SafeConsole.WaitRows indicator
public static void MoveAllForeignKeysConsole<T>(Lite<T> oldEntity, Lite<T> newEntity)
   where T : Entity
```

### BulkInsert

Uses `SqlBulkCopy` to load a set of entities in the database as fast as possible. Only simple inserts will be made, with no graph analysis or validation taking place.


```C#
public static void BulkInsert<T>(IEnumerable<T> entities, 
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default) 
            where T : Entity
```

The operation will be embedded in the current transaction if `SqlBulkCopyOptions.UseInternalTransaction` is not set.

The Id for the entities should be set if the table does not have `Identity=true`.

Finally, in order to insert entities with MList, a call to `BulkInsertMList` is necessary. 

```C#
public static void BulkInsertMList<E, V>(Expression<Func<E, MList<V>>> mListProperty,
    IEnumerable<MListElement<E, V>> entities,
    SqlBulkCopyOptions options = SqlBulkCopyOptions.Default)
    where E : Entity
```