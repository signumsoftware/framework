# Database

`Database` is the main facade of Signum Engine for normal operations with entities, like [saving](Database.Save.md), [retrieving](Database.Retrieve.md) or [deleting](Database.Delete.md) particular entities, or write complex queries to [retrieving](Database.Query.md), [update](Database.UnsafeUpdate.md), [delete](Database.UnsafeDelete.md) or [insert](Database.UnsafeInsert.md) many entities at the same time.

Three things that make `Database` class different to many other ORM:

1. **Database class is static**: No need to instantiate it, neither to pass it as a parameter or store it in some global variable. Client code (your code) is simpler and easier to re-use, while preserving flexibility to change the DB connection and the schema using `Connector.Override` method.
2. **No Property for each table**: On Database class you will find general purpose methods to deal with `Entity` objects, also you will find strongly-typed generic overloading. But what you are not going to find there are properties to deal specifically with some concrete entity. This way the business logic in your modules only depend on the common `Database` class, not a particular class representing your particular database, so they can be used in other projects. 
3. **No need to synchronize DB and Code while developing**: Since Signum Framework is entirely based on entities, and they are written in C# code, you don't need to synchronize the database schema before doing the next step. You can write big parts of the application from scratch without even connecting to SQL Server and then, at the very end, generate or re-synchronize the schema using `Administrator`, and test all your queries and code. 

## Example: 

Let's start with an example. 

Suppose you have a very simplistic UserEntity entity like this one: 

```C#
[Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
public class UserEntity : Entity
{
    [UniqueIndex] 
    string userName;
    public string UserName
    {
        get { return userName; }
        set { Set(ref userName, value); }
    }

    string passwordHash;
    public string PasswordHash
    {
        get { return passwordHash; }
        set { Set(ref passwordHash, value); }
    }
}
```

>**Note:** *In case you don't know why the field is named `passwordHash` instead of password click [here](http://blog.codinghorror.com/youre-probably-storing-passwords-incorrectly/).


And you need to be able to change user's password, it would be implemented like this: 


```C#
private static void ChangePassword(string userName, string oldPasswordHash, string newPasswordHash)
{
    using (Transaction tr = new Transaction())
    {
        UserEntity user = Database.Query<UserEntity>().Single(a => a.UserName == userName);
        if (user.PasswordHash != oldPasswordHash)
           throw new ApplicationException("Incorrect password");
        user.PasswordHash = newPasswordHash;
        user.Save();

        tr.Commit();
    }
}
```

Looks easy, doesn't it? 

This is what is actually happening: 

* **Transaction:** We create a `Transaction`. Every atomic operation exposed by Database class is implicitily transactional, but by explicitly surrounding it with a `Transaction` object we are just making the transaction bigger.
* **Query**: We retrieve the only user with the provided username in one query.
* Testing the old password (nothing to do with the framework really)
* Setting the new password, internally the `Set` method of the entity is called, so the entity becomes self modified.
* **Save:** Using `Database.Save` extension method to save the entity. You could write `Database.Save(user)` instead if you find it more clear. Also, in this example we're saving a simple entity but it could have a whole graph of related entities if necessary. 