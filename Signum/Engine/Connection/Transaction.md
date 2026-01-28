# Transaction

The `Transaction` class in Signum handles database transactions in a clean, nestable, and implicit way. It is inspired by Microsoft's [TransactionScope](https://learn.microsoft.com/en-us/dotnet/api/system.transactions.transactionscope) from the client code perspective: transactions are managed implicitly via `using` statements.

However, the internal implementation differs to better support Signum's needs, including nested and silent transactions, savepoints, and test scenarios.

## How to Use

Suppose you have code like this:

```csharp
private static void FixBugs()
{
    var wrongBugs = from b in Database.Query<BugEntity>()
                    where b.Status == Status.Fixed && !b.End.HasValue
                    select b.ToLite();
    
    foreach (var lazyBug in wrongBugs)
    {
        BugEntity bug = lazyBug.Retrieve();
        bug.Description += "- Fix it!";
        bug.Save();
    }
}
```

This code performs several database operations independently. If an exception occurs midway, you may end up with partial changes. Also, concurrency issues may arise if data changes between the query and retrieval.

In Signum, every atomic operation (query, retrieve, save) is transactional by default. However, to group multiple operations into a single transaction, wrap them in a `Transaction` block:

```csharp
private static void FixBugs()
{
    using (var tr = new Transaction())
    {
        var wrongBugs = from b in Database.Query<BugEntity>()
                        where b.Status == Status.Fixed && !b.End.HasValue
                        select b.ToLite();

        foreach (var lazyBug in wrongBugs)
        {
            BugEntity bug = lazyBug.Retrieve();
            bug.End = bug.Start.AddDays(7);
            bug.Save();
        }

        tr.Commit();
    }
}
```

Here, all operations are part of the same transaction. If `Commit()` is not called, the transaction is rolled back on dispose. This ensures consistency and prevents partial updates.

**Note:** The `IDisposable` pattern does not detect exceptions, so you must call `Commit()` explicitly.

Transactions are handled implicitly; you do not need to pass the transaction object to inner methods. You can also nest transactions:

```csharp
private static void FixTheWorld()
{
    using (var tr = new Transaction())
    {
        FixBugs();
        FixCustomers();
        FixDevelopers();
        tr.Commit();
    }
}
```

Inner transactions become silent (faked) if an outer transaction exists. Only the outermost transaction actually commits or rolls back.

## Types of Transactions

### `new Transaction()`
Creates a new transaction. If it's the first (outermost) transaction, it creates a real transaction. Otherwise, it creates a silent (faked) transaction that can force a rollback but not commit independently.

```csharp
using (var tr1 = new Transaction())
{
    using (var tr2 = new Transaction()) // tr2 is silent
    {
        tr2.Commit(); // Required, but only tr1 actually commits
    }
    tr1.Commit();
}
```

### `Transaction.ForceNew()`
Creates a new, independent transaction, even if a parent transaction exists (unless in a test transaction). Changes in the parent transaction are not visible until committed.

```csharp
using (var tr1 = new Transaction())
{
    var proj = new ProjectEntity { Name = "New project" }.Save();
    using (var tr2 = Transaction.ForceNew()) // Independent transaction
    {
        Database.Exists(proj); // false, tr1 not committed
        tr2.Commit();
    }
    tr1.Commit();
}
```

Use for independent processing, error handling, or global cache updates.

### `Transaction.Test()`
Creates a test transaction. Any `ForceNew` transaction inside a test transaction becomes silent, so all changes can be rolled back together. Useful for testing long-running processes.

```csharp
using (var tr1 = Transaction.Test())
{
    var proj = new ProjectEntity { Name = "New project" }.Save();
    using (var tr2 = Transaction.ForceNew()) // Silent in test
    {
        Database.Exists(proj); // true
        tr2.Commit();
    }
    tr1.Commit();
}
```

### `Transaction.NamedSavePoint(string name)`
Creates a named savepoint using SQL savepoints. Allows rolling back to a specific point within a transaction.

```csharp
using (var tr1 = new Transaction())
{
    var proj = new ProjectEntity { Name = "New project" }.Save();
    try
    {
        using (var tr2 = Transaction.NamedSavePoint("Risky"))
        {
            proj.Delete();
            throw new Exception();
            tr2.Commit();
        }
    }
    catch (Exception)
    {
        // tr2 is rolled back, proj is not deleted
    }
    tr1.Commit();
}
```

Useful for complex business logic with optional, independently rollbackable operations.

### `Transaction.None()`
Disables SQL transactions for the scope, so inner operations are not wrapped in a transaction. Use only for micro-optimizations after profiling.

```csharp
using (var tr1 = Transaction.None())
{
    var proj = new BugEntity
    {
        Description = "New Bug",
        Fixer = new DeveloperEntity { Name = "New Developer" },
        Project = new ProjectEntity { Name = "New project" }
    }.Save();
    tr1.Commit();
}
```

## Commit and Commit(value)

You must call `Commit()` before disposing the transaction, or it will be rolled back. 

If you need to return a value after committing, use the overload that accepts (and returns) the desired value:

```csharp
int OpenResult()
{
    using (var tr = new Transaction())
    {
        Database.Query<BugEntity>()
            .Where(b => b.Status == Status.Open && b.Start < DateTime.Today.AddMonths(-6))
            .UnsafeUpdate()
            .Set(b => b.Status, b => Status.Rejected)
            .Execute();

        var opened = Database.Query<BugEntity>().Count(b => b.Status == Status.Open);

        return tr.Commit(opened); //commit and return
    }
}
```

## Static Helpers and Events

The following are static events and properties that internally access the current transaction instance. 

- `Transaction.PreRealCommit`: Invoked just before the real transaction is committed. Useful for asserting post-conditions.
- `Transaction.PostRealCommit`: Invoked just after the real transaction is committed. Useful for cache invalidation.
- `Transaction.Rolledback`: Invoked after a real transaction is rolled back. Also useful for cache invalidation.
- `Transaction.UserData`: Dictionary for storing custom data in the current transaction.
- `Transaction.CurrentConnection` / `Transaction.CurrentTransaction`: Access the current connection or transaction.
