# Schema

`Schema` class is a data structure that stays between classes and tables, between fields and columns, between references and foreign keys. 

This structure is the only authority any time the engine needs mapping information like... What is the column name of a field?


The main source of information for generating this mapping are your entities themselves. `SchemaBuilder` is the class that takes your entities as an input and generates a `Schema` as an output. 

That means that generating the schema is usually nothing more than two or three lines of code on your Global.asax (or just when loading your application) and it will do it just fine while you evolve your application. 

```C#
public static void Start(string connectionString)
{
    SchemaBuilder sb = new SchemaBuilder();
    sb.Include<CustomerDN>();
    Connector.Default = new SqlConnector(connectionString, sb.Schema, dqm);
}
```

## Using SchemaBuilder

`SchemaBuilder` is just a utility class you just use to fill with some of your entities to be able to use the engine on them. The process can be resumed in three simple steps: 

* Create a `SchemaBuilder`.
* Fill the builder with some entities using some of the `Include` methods. A `Schema` embryo will be growing inside.
* Create a new `SqlConnector` with the newly born `Schema` created by the `SchemaBuilder` and set it as the default [Connector](Connector.md). 

Including entities is the process of inserting tables in the `Schema` by including their types. Any time a type is included, all the related entities that are reachable from it are included as well to avoid inconsistent states. 

There are however two overloads of `Include` method: 
```C#
public class SchemaBuilder
{
    public Table Include<T>() where T : IdentifiableEntity //strongly typed
    public virtual Table Include(Type type) // weakly typed
}
```

Only non-abstract classes can be included. 

## Customizing the schema

The process above is OK when you want normal mapping of your entities, but if the default table or column names doesn't follow your company standards, or you want to override some entities from another project, we provide three ways to change the default Schema mapping. 

### SchemaBuilderSettings: Override your entities Attributes

To remove duplication and centralize related information, Signum Framework uses attributes and the field declaration to provide database schema information. 

However, if the entity is reused in many project and you can not modify it, this solution is not very flexible.

`SchemaBuilderSettings` allows you to add or remove [field attributes](../Signum.Entities/FieldAttributes.md) on fields at run-time to customize entities that you don't own. 

By overriding field attributes in an entity (`IgnoreAttribute`, `ImplementedByAttribute`, `SqlDbTypeAttribute`...) and including [Mixins](../Signum.Entities/FieldAttributes.md) we increase dramatically the scenarios where an entity can be reused in different projects, allowing vertical modules.

For example, `ExceptionDN`, defined in Signum.Entities, saves detailed information of any exception that is recorded using `LogException` extension method. 

```C#
[Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
public class ExceptionDN : IdentifiableEntity
{    
    //...
    [SqlDbType(Size = 100)]
    string controllerName;
    [StringLengthValidator(AllowNulls = true, Max = 100)]
    public string ControllerName
    {
        get { return controllerName; }
        set { Set(ref controllerName, value); }
    }

    Lite<IUserDN> user;
    public Lite<IUserDN> User
    {
        get { return user; }
        set { Set(ref user, value); }
    }
    //...
}
``` 

If in our application we have particularly long controller names, we could override the database size like this: 

```C#
sb.Schema.Settings.OverrideAttributes((ExceptionDN ua) => ua.User, 
    new SqlDbTypeAttribute{ Size = 200 });
```

And if we are using **Authorization module** we can implement `IUserDN User` property with `UserDN` type. 

```C#
sb.Schema.Settings.OverrideAttributes((ExceptionDN ua) => ua.User, 
    new ImplementedByAttribute(typeof(UserDN)));
```

Some things to remember: 

* The attribute overrides have to be set **before** the table is included in the Schema.
* Once afield is overridden, it overrides all the attributes, so if there are 3 attributes and you want to override just one, remember to copy the other two.
* This technique also allows you to change attributes on an inherited field for a particular subclass, something that is not available in the CLR. 

## Change the generated Schema (Advanced)

Once you have finished including entities in the SchemaBuilder, you have a new Schema in the Schema property. 

You can change whatever you want in this structure, and it will be reflected whenever you use the engine (thought Database or Administrator). 

Internally, a `Schema` is just a `Dictionary<Type, Table>`. 

`Table` is the class that maps an `IdentifiableEntity` to database table. Contains a `Type`, a `Name`, an `Identitiy` flag, and two dictionaries, one for Fields and one for Columns. 

Usually, the underlying objects of these dictionaries are the same, depending of the Field's Type:

* `PrimaryKeyField` is a field and also `IColumns`. 
* `ValueField` is a field and also `IColumns`.
* `RefenrenceField` is a field and also `IColumns`.
* `ImplementedByField` contains a `ImplementationColumn` for each implementation.  
* `ImplementedByAllField` contain two `IColumn`.
* `EmbeddedField` contains a nested `Dictionary` of `Fields` (with their own `IColumns`).
* `MListField` has no `IColumn` at all, instead has a `MListTable` object. 

> **Important Note:** There's just no validation on Schema data structure, make modifications at your own risk and don't expect a nice exception message to be thrown if you do something silly.

```C#
Schema s = sb.Schema;

((ValueField)s.Field((BillDN b) => b.Lines.First().Quantity)).SqlDbType = SqlDbType.SmallInt;;

ConnectionScope.Default = new Connection(connectionString, s);
```

### Moving tables to other databases

For big applications with lots of requests, the RDBMS tends to end up being the bottleneck. 

Microsoft SQL Server has no support for sharding (horizontal partitioning), but we can move some tables to other databases/database servers to improve performance. 

Another good strategy that we use often is moving the log tables (i.e.: `OperationLogDN`, `ExceptionDN`, `EmailMEssageDN`...) to a log database that we can backup less often, keeping the important data in principal database that we can move easily. 

In order to do this, `Table` and `MListTable` classes have a `Name` property of type `ObjectName`, that represents a [four-part name](http://msdn.microsoft.com/en-us/library/ms177563.aspx).  

```C#
public class ObjectName : IEquatable<ObjectName>
{
    public string Name { get;  }
    public SchemaName Schema { get; } //Mandatory, default dbo

    public ObjectName(SchemaName schema, string name)
}

public class SchemaName : IEquatable<SchemaName>
{
    public string Name { get; }
    public DatabaseName Database { get; } //Optional

    public SchemaName(DatabaseName database, string name)
}

public class DatabaseName : IEquatable<DatabaseName>
{
     public string Name { get; }
     public ServerName Server { get; } //Optional

     public DatabaseName(ServerName server, string name)
}

public class ServerName : IEquatable<ServerName> 
{
     public string Name { get; }
     public ServerName(string name) ;
}
```

And `Table` also has some helper methods `ToSchema`, to change the schema, and `ToDatabase` to change the database (that could also be in another `ServerName` using linked servers). 

```C#
/// <summary>
/// Use this method also to change the Server
/// </summary>
public void ToDatabase(DatabaseName databaseName)
{
    this.Name = this.Name.OnDatabase(databaseName);

    foreach (var item in TablesMList())
        item.ToDatabase(databaseName);
}

public void ToSchema(SchemaName schemaName)
{
    this.Name = this.Name.OnSchema(schemaName);

    foreach (var item in TablesMList())
        item.ToSchema(schemaName);
}
``` 


Example: 

```C#
//Called at the end of `Starter.Start` method: 
public static void SetLogDatabase(Schema schema, DatabaseName logDatabaseName)
{
    schema.Table<OperationLogDN>().ToDatabase(logDatabaseName);
    schema.Table<ExceptionDN>().ToDatabase(logDatabaseName);
}
```

Finally, is a common pattern to specify the name of both tables in the connection string, like this: 

```
Data Source=localhost\SQLEXPRESS2012;Initial Catalog=Southwind+_Log;User ID=sa;Password=sa
```

And use `Connector.TryExtractCatalogPostfix` to clean this connection string. 

```C#
public static void Start(string connectionString)
{
   //At the very beginning of Starter.Start
   string logDatabase = Connector.TryExtractDatabaseNameWithPostfix(ref connectionString, "_Log");

   //....

   //At the very end of Starter.Start
   if (logDatabase.HasText())
       SetLogDatabase(sb.Schema, new DatabaseName(null, logDatabase));
}
```

## Override SchemaBuilder behaviour

The last way of customizing the Schema is to inherit from `SchemaBuilder` and create your own `CustomSchemaBuilder` class and override any virtual method to customize types or names. 