# Transaction

Transaction class is used to handle transactions in a clean and easy nestable way. 

It uses the same strategy as Microsoft's [TransactionScope](http://msdn.microsoft.com/es-es/library/system.transactions.transactionscope.aspx) from the client code point of view: Implicit transaction in the call stack defined by `using` statements.

Internally, however, it has some differences, let's take a look.

## How to use it

Imagine we have some code like this: 


```C#
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

This simple code contains a lot of intensive work with the database: the query, retrieving each Bug, and saving it. Currently all of these are independent operations, so we could have some problems: 

* If some exception is thrown in the middle of the process we end up having some bugs fixed and some remaing. This could have some problems, like duplicating - Fix It! for some bugs once you re-run the process.
* It could take some time from the time the query gets executed, to the moment the `BugEntity` is actually retrieved (we are using lite objects). Someone could have modified the Bug in the meantime. 

In `Signum.Engine`, every atomic operation, like retrieving an object, executing a query, or saving an object graph is transactional by default.

Sometimes, however, you want to glue together operations in the same transaction, so you have a consistent view of the database (not influenced by concurrency) and you don't commit partial modifications if something goes wrong in the middle. 

This is as easy as surrounding the code with a `using` Transaction block, like this: 

```C#
private static void FixBugs()
{
    using (Transaction tr = new Transaction())
    {
        var wrongBugs = from b in Database.Query<BugEntity>()
                        where b.Status == Status.Fixed && !b.End.HasValue
                        select b.ToLazy();

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

What we do here is creating a transaction, do our work, and at the end call `Commit` on it. When the transaction is disposed, if it hasn't been committed, it gets roll backed automatically, so the code gets much more simple to read.

**Note:** `IDisposable` pattern doesn't get notified for exceptions, that's why Commit is necessary.

The nice thing is that all the code inside (i.e.: `Query`, `Retrieve`, `Save`) doesn't need to take a `Transaction tr` as a parameter, because transactions are handled implicitly.

Also, if you call FixBigs from a method like this: 

```C#
private static void FixTheWorld()
{
    FixBugs();

    FixCustomers();

    FixDevelopers();
}
```

We can also glue together all the inner transactions creating a new, wider one: 

```C#
private static void FixTheWorld()
{
    using (Transaction tr = new Transaction())
    {
        FixBugs();

        FixCustomers();

        FixDevelopers();

        tr.Commit();
    }
}
```

The end result is that the inner transactions now become silent and the outer transaction is the only one taking effect, without you having to change anything the methods, so the code is much easier to compose. 

## Types of Transactions

### `new Transaction()`
Creates a new Transaction using the default `IsolationLevel` if it's the first transaction (parent), otherwise creates a faked transaction that does not commit, but can force roll-back of the parent transaction. 

Use this transaction to ensure that some operations will be consistent, but let the caller embed you in a wider transaction. 

```C#
using(Transaction tr1 = new Transaction())
{
   using(Transaction tr2 = new Transaction()) //This transaction is silent
   {
    
       tr2.Commit(); //But has to be commited
   }

   tr1.Commit();
}
```

### `ForceNew`
Creates a new independent Transaction with the provided `IsolationLevel`, even if there's a parent transaction, except if the parent transaction is a `Test` transaction. 

Once inside a `ForceNew` transaction, you won't be able to see changes not commited by any transaction, **included** the parent transaction. 

```C#
using(Transaction tr1 = new Transaction())
{
   ProjectEntity proj = new ProjectEntity { Name = "New project" }.Save();

   using(Transaction tr2 = Transaction.ForceNew()) //Independent transaction
   {
       Database.Exists(proj); //returns false because tr1 is not commited
    
       tr2.Commit();
   }

   tr1.Commit();
}
```

Independent transactions are usually used to process entities independently in long-running processes, in `catch` blocks of a `try-catch` statements, or to fill global cache of shared objects. 

### `Test`
Test transactions can be used to domesticate `ForceNew` transactions, keeping them embedded in the parent transaction. 

Usually to test long-running processes and still be able to rollback the changes. 

```C#
using(Transaction tr1 = Transaction.Test())
{
   ProjectEntity proj = new ProjectEntity { Name = "New project" }.Save();

   using(Transaction tr2 = Transaction.ForceNew()) //silent transaction
   {
       Database.Exists(proj); //returns true because tr2 is not silent
    
       tr2.Commit();
   }

   tr1.Commit();
}
```


### `NamedSavePoint`

Uses `SqlTransaction.Save` and `SqlTransaction.Rollback` to save and rollback to certain transaction points. 

```C#
using(Transaction tr1 = new Transaction())
{
   ProjectEntity proj = new ProjectEntity { Name = "New project" }.Save();
   try
   {
      using(Transaction tr2 = Transaction.NamedSavePoint("Risky")) //named transaction
      {
          proj.Delete(); 

          throw Exception();
       
          tr2.Commit();
      }
   }
   catch(Exception e)
   {
      
   }

   tr1.Commit(); //proj will be created and not deleted
}
```

Usefull in some complicated business logic that contain some optional operations that need to be rollbacked independently. 

### `None` 
Special type of transaction to avoid creating `SqlTransactions` for any inner `Transaction` (like atomic framework operations). Could be usefull to improve performance doing some micro-optimizations. Profile first!

```C#
using(Transaction tr1 = Transaction.None())
{
   // The 3 necessary INSERT commands will not be embedded in the same transaction
   BugEntity proj = new BugEntity
   { 
      Description = "New Bug",
      Fixer = new DeveloperEntity { Name = "New Developer"  }
      Project = new ProjectEntity { Name = "New project"  }
   }.Save(); 
  
   tr1.Commit();
}
```

## `Commit` and `Commit(value)`

Calling `Commit` method is necessary before the transaction is disposed, or will be roll-backed. 

This restriction can make your code uglier if you need to return values. 

```C#
int OpenResult()
{
   using(Transaction tr = new Transaction())
   { 
      Database.Query<BugEntity>()
      .Where(b => b.Status == Status.Open && b.Start < DateTime.Today.AddMonth(-6))
      .UnsafeUpdate()
      .Set(b =>b.Status, b => Status.Rejected)
      .Execute();

      return Database.Query<BugEntity>().Count(b=> b.Status == Status.Open);  //UPS!! Commit unreachable

      tr.Commit()
   }
}
```

In order to aliviate the problem, an overload of `Commit` allows to get and return any value: 

```C#
int OpenResult()
{
   using(Transaction tr = new Transaction())
   { 
      Database.Query<BugEntity>()
      .Where(b => b.Status == Status.Open && b.Start < DateTime.Today.AddMonth(-6))
      .UnsafeUpdate()
      .Set(b =>b.Status, b => Status.Rejected)
      .Execute();

      return tr.Commit(Database.Query<BugEntity>().Count(b=> b.Status == Status.Open));
   }
}
```

Nothing magic here, just a little bit more convenient.


## Transaction static events

There are three useful static events in `Transaction` class: 

### Transaction.PreRealCommit

Invoked just before the current real transaction is about to be committed. Convenient to assert post-conditions that the database has to satisfy when the transaction is commited, but could not be the case during the transaction. 

### Transaction.PostRealCommit

Invoked just after the current real transaction is committed. Convenient to invalidate caches.  

### Transaction.Rolledback

Invoked just after the current real transaction is rolled-back. Also convenient to invalidate caches.  