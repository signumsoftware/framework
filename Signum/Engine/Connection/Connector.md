# Connector

A **Connector** in Signum Framework is a connection factory that encapsulates the `Schema` and the `connectionString` required to access a specific database. The connector is typically configured globally for the application, but can be temporarily overridden for the current thread within a region of code. This enables flexible database access and transaction management, while keeping configuration centralized and consistent.

The connector also holds a `Version` property (such as `SqlServerVersion` or `PostgresVersion`) that determines which features the underlying RDBMS supports. This allows Signum to adapt its behavior and enable or disable features based on the database version.

Most operations in Signum use the globally configured connector, but you can override it for advanced scenarios such as multi-database loading, or testing. The override is thread-local and scoped, ensuring that changes do not affect other threads or requests.

For ecample [Database](../Database.md) and [Administrator](../Administrator.md) are static classes used to save, retrieve, and query objects, without needing to know the database structure ([Schema](../Schema.md)) or physical location (connection string).

If you need to change the connection for a block of code, use `Connector.Override`.

## Connector Class

The `Connector` base class defines common properties and methods for all database connectors:

- `Schema`: The database schema.
- `ConnectionString`: The connection string for the database.
- `Version`: The database version, used to determine feature support.
- `CommandTimeout`: Optional default timeout for commands.
- `ParameterBuilder`: Abstract property for creating database parameters.
- `IsolationLevel`: Transaction isolation level.
- `SqlBuilder`: SQL builder for the current connector.
- Methods for connection management, transaction save/rollback, bulk operations, and error handling (as abstract or virtual).

## Derived Connectors

### SqlServerConnector
Implements Microsoft SQL Server support:
- Detects and exposes `SqlServerVersion`.
- Enables/disables features based on SQL Server version (snapshot isolation, SQL dependency, partitioning, etc.).
- Handles SQL Server error codes and bulk copy logic.
- Overrides properties and methods for SQL Server specifics (e.g., max name length, schema cleaning).

### PostgreSqlConnector
Implements PostgreSQL support:
- Detects and exposes `PostgresVersion`.
- Enables/disables features based on PostgreSQL version (extensions, type reload, binary bulk import, etc.).
- Handles PostgreSQL error codes and script splitting.
- Overrides properties and methods for PostgreSQL specifics (e.g., max name length, extension management).

## Usage

Set the main connector:
```csharp
Connector.Default = new SqlServerConnector(connectionString, schema, version);
// or
Connector.Default = new PostgreSqlConnector(connectionString, schema, version);
```

Temporarily override the connector:
```csharp
using (Connector.Override(newConnector))
{
    // Code using newConnector
}
```

Override the command timeout:
```csharp
using (Connector.CommandTimeoutScope(3 * 60)) // 3 minutes
{
    // Slow query here
}
```

## Related Classes
- [Database](../Database.md): Main facade for entity operations
- [Administrator](../Administrator.md): Schema and administrative operations
- [Executor](Executor.md): Low-level ADO.NET access
- [Schema](../Schema.md): Database schema

## API Reference
- `public static Connector Default { get; set; }`
- `public static Connector Current { get; }`
- `public static IDisposable Override(Connector connector)`
- `public static IDisposable CommandTimeoutScope(int? timeoutSeconds)`
- `public Schema Schema { get; }`
- `public IsolationLevel IsolationLevel { get; set; }`
- `public string ConnectionString { get; protected set; }`
- `public int? CommandTimeout { get; set; }`
- `public abstract ParameterBuilder ParameterBuilder { get; protected set; }`
- `public Version Version { get; } // or SqlServerVersion/PostgresVersion in derived classes`

See [Connector.cs](Connector.cs), [SqlServerConnector.cs](SqlServerConnector.cs), and [PostgreSqlConnector.cs](PostgreSqlConnector.cs) for more details.





