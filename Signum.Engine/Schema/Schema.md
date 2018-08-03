# Schema

`Schema` class is a data structure that stays between classes and tables, between fields and columns, between references and foreign keys. 

This structure is the only authority any time the engine needs mapping information, like... What is the column name of a field?


The main source of information for generating this mapping are your entities themselves. `SchemaBuilder` is the class that takes your entities as an input and generates a `Schema` as an output. 

That means that generating the schema is usually nothing more than two or three lines of code on your Global.asax (or just when loading your application) and it will do it just fine while you evolve your application. 

```C#
public static void Start(string connectionString)
{
    SchemaBuilder sb = new SchemaBuilder();
    sb.Include<CustomerEntity>();
    Connector.Default = new SqlConnector(connectionString, sb.Schema);
}
```

## Using SchemaBuilder

`SchemaBuilder` is just a utility class you just use to fill with some of your entities to be able to use the engine on them. The process can be summarized in three simple steps: 

* Create a `SchemaBuilder`.
* Fill the builder with some entities using some of the `Include` methods. A `Schema` embryo will be growing inside.
* Create a new `SqlConnector` with the newly born `Schema` created by the `SchemaBuilder` and set it as the default [Connector](Connector.md). 

Including entities is the process of inserting tables in the `Schema` by including their types. Any time a type is included, all the related entities that are reachable from it are included as well to avoid inconsistent states. 

There are however two overloads of `Include` method: 
```C#
public class SchemaBuilder
{
    public FluentInclude<T> Include<T>() where T : Entity
    {
        var table = Include(typeof(T), null);
        return new FluentInclude<T>(table, this);
    }

    public virtual Table Include(Type type)
    {
        return Include(type, null);
    }
}
```

Only non-abstract classes can be included. 

## Customizing the schema

The process above is OK when you want normal mapping of your entities, but if the default table or column names doesn't follow your company standards, or you want to override some entities from another project, we provide three ways to change the default Schema mapping. 

### 1. SchemaBuilderSettings: Override your entities Attributes

To remove duplication and centralize related information, Signum Framework uses attributes and the property/field declaration to provide database schema information. 

However, if the entity is re-used in many project and you can not modify it, this solution is not very flexible.

`SchemaBuilderSettings` allows you to add or remove [field attributes](../Signum.Entities/FieldAttributes.md) on fields at run-time to customize entities that you don't own. 

By overriding field attributes in an entity (`IgnoreAttribute`, `ImplementedByAttribute`, `SqlDbTypeAttribute`...) and including [Mixins](../Signum.Entities/FieldAttributes.md) we increase dramatically the scenarios where an entity can be reused in different projects, allowing vertical modules.

For example, `ExceptionEntity`, defined in Signum.Entities, saves detailed information of any exception that is recorded using `LogException` extension method. 

```C#
[Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
public class ExceptionEntity : Entity
{    
    //...
        [StringLengthValidator(AllowNulls = true, Max = 100)]
    public string ControllerName { get; set; }
    

    public Lite<IUserEntity> User { get; set; }
    //...
}
``` 

If in our application we have particularly long controller names, we could override the database size like this: 

```C#
sb.Schema.Settings.FieldAttributes((ExceptionEntity ua) => ua.User).Add(new SqlDbTypeAttribute{ Size = 200 });
```

And if we are using **Authorization module** we can implement `IUserEntity User` property with `UserEntity` type. 

```C#
sb.Schema.Settings.FieldAttributes((ExceptionEntity ua) => ua.User).Add(new ImplementedByAttribute(typeof(UserEntity)));
```

Some things to remember: 

* The attribute overrides have to be set **before** the table is included in the Schema.
* Once afield is overridden, it overrides all the attributes, so if there are 3 attributes and you want to override just one, remember to copy the other two.
* This technique also allows you to change attributes on an inherited field for a particular subclass, something that is not available in the CLR. 

### 2. Override SchemaBuilder behaviour

Another posibility is to implemente your own `CustomSchemaBuilder` and use it in your Starter class, overriding some virtual methods.

``C#
public class CustomSchemaBuilder : SchemaBuilder
{
    //Override methods to change conventions
}

SchemaBuilder sb = new CustomSchemaBuilder();
//include modules 
Connector.Current = sb.Schema; 
```


Two common usages os this technique are:
* Determine the SQL Database Schema of each table from the Entity namespace. 
* Move log tables to another database. 

For big applications with lots of requests, the RDBMS tends to end up being the bottleneck. 

Microsoft SQL Server has no support for sharding (horizontal partitioning), but we can move some tables to other databases/database servers to improve performance. 

One good strategy that we use often is moving the log tables (i.e.: `OperationLogEntity`, `ExceptionEntity`, `EmailMEssageEntity`...) to a log database that we can backup less often, keeping the important data in principal database that we can move easily. 

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

There are two methods in `SchemaBuilder`that are responsible of generating `ObjectNames`: `GenerateTableName`and `GenerateTableNameCollection` (for MLists). 

The following example overrides both methods to enable a multi-database and multi-schema configuration: 

```C#
public class CustomSchemaBuilder : SchemaBuilder
{
    public string LogDatabaseName;

    public override ObjectName GenerateTableName(Type type, TableNameAttribute tn)
    {
        return base.GenerateTableName(type, tn).OnSchema(GetSchemaName(type));
    }

    public override ObjectName GenerateTableNameCollection(Table table, NameSequence name, TableNameAttribute tn)
    {
        return base.GenerateTableNameCollection(table, name, tn).OnSchema(GetSchemaName(table.Type));
    }

    SchemaName GetSchemaName(Type type)
    {
        return new SchemaName(this.GetDatabaseName(type), GetSchemaNameName(type) ?? "dbo");
    }

    public Type[] InLogDatabase = new Type[]
    {
        typeof(OperationLogEntity),
        typeof(ExceptionEntity),
    };
    DatabaseName GetDatabaseName(Type type)
    {
        if (this.LogDatabaseName == null)
            return null;

        if (InLogDatabase.Contains(type))
            return new DatabaseName(null, this.LogDatabaseName);

        return null;
    }

    static string GetSchemaNameName(Type type)
    {
        type = EnumEntity.Extract(type) ?? type;

        if (type == typeof(ColumnOptionsMode) || type == typeof(FilterOperation) || type == typeof(PaginationMode) || type == typeof(OrderType))
            type = typeof(UserQueryEntity);

        if (type == typeof(SmtpDeliveryFormat) || type == typeof(SmtpDeliveryMethod))
            type = typeof(EmailMessageEntity);

        if (type == typeof(DayOfWeek))
            type = typeof(ScheduledTaskEntity);

        if (type.Assembly == typeof(ApplicationConfigurationEntity).Assembly)
            return null;

        if (type.Assembly == typeof(UserChartEntity).Assembly)
            return "extensions";

        if (type.Assembly == typeof(Entity).Assembly)
            return "framework";

        throw new InvalidOperationException("Impossible to determine SchemaName for {0}".FormatWith(type.FullName));
    }
}
```

And then in our Starter we use `CustomSchemaBuilder` instead of `SchemaBuilder`. 


Finally, is a common pattern to specify the name of both tables in the connection string, like this: 

```
Data Source=localhost\SQLEXPRESS2012;Initial Catalog=Southwind+_Log;User ID=sa;Password=sa
```

With this convention we're saying that our application will use `Southwind` and `Southwind_Log` databases. 

We can use `Connector.TryExtractDatabaseNameWithPostfix` to split clean the `_Log` prefix from the connection string and get `Southwind_Log` database name. 

```C#
public static void Start(string connectionString)
{
   //At the very beginning of Starter.Start
   string logDatabase = Connector.TryExtractDatabaseNameWithPostfix(ref connectionString, "_Log");
   SchemaBuilder sb = new CustomSchemaBuilder { LogDatabaseName = logDatabase };
}
```

Note > The example application Southwind already contains a similar code in its Starter class. Feel free to customize it.  


### 3. Change the generated Schema (Advanced)

The lass way (and least recommended) of customizing the Schema is manually modify it after is generated..

Once you have finished including entities in the SchemaBuilder, you have a new Schema in the Schema property. 

You can change whatever you want in this structure, and it will be reflected whenever you use the engine (thought Database or Administrator). 

Internally, a `Schema` is like a `Dictionary<Type, Table>`. 

`Table` is the class that maps an `Entity` to database table. Contains a `Type`, a `Name`, an `Identitiy` flag, and two dictionaries, one for Fields and one for Columns. 

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

((ValueField)s.Field((BillEntity b) => b.Lines.First().Quantity)).SqlDbType = SqlDbType.SmallInt;;

ConnectionScope.Default = new Connection(connectionString, s);
```

