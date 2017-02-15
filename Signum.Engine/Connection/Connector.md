## Connector

[Database](../Database.md) and [Administrator](../Administrator.md) are static classes, and you usually save, retrieve and query objects with a certain level of ignorance of how the database looks ([Schema](../Schema.md)), or where it is physically located (connection string). 

Not having to pass the connectionString and the `Schema` as a parameter all the time doesn't mean that you don't have the flexibility to change it when you think it is necessary. In order to change the connection in a block of your code just use `Connector.Override`.


### Connector class
You can think in a Connector class like the container that has all the information to access a specific database in a DBMS:

* The `connectionString`.
* The `Schema`.

Internally, Connection class is also the only gateway to access the database, it's not intended to be used by client code. [Executor](Executor.md) static class is what should be used if you want low-level ADO.Net access through the current connection. 

You can also think of Connector class as a abstract factory of Connections: 

* **Connector:** abstract base class, 
	* **SqConnector:** The only supported implementation, works with Microsoft SQL Server. Contains:
		* `ConnectionString`: The connection string that will be used to create the connection. 
		* `CommandTimeout`: Default timeout, optional.
		* `IsolationLevel`: Default IsolationLevel, optional.
	* **SqlCeConnector:** incomplete prototype
	* ... maybe more in the future?

### Connector.Default and Connector.Override

Typically, you application has just one main `Connector` and is set using `Connector.Default`. 

`Connector.Default = new SqlConnector(connectionString, sb.Schema, dqm);`

`Connector` class has a static `Override` method that returns a `IDisposable` and let you switch `Connector.Current` to be something different to `Connector.Default` in a region of code. 

```C#
public static IDisposable Override(Connector connection)
```

**Note:** It's uncommon to use more than one `Connector` using Signum.Engine for something different that simple loading scenarios. Don't expect every module to work as expected using a non-default `Connector`. 

### Connector.CommandTimeoutScope

`Connector` class has a static `CommandTimeoutScope` method that returns a `IDisposable` and let you override the default `CommandTimeout` in a region of code.

```C#
public static IDisposable CommandTimeoutScope(int? timeoutMilliseconds)
```

Example: 

```C#
using(Connector.CommandTimeoutScope(3 * 60 * 1000)) // 3 mins
{
   // slow query here
}
```





