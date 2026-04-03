# Executor class

The `Executor` static class is the main gateway for accessing the database in the Signum Framework. It is used internally by the engine and is the recommended way to execute raw SQL commands against the current connection.

Most methods accept a [SqlPreCommandSimple](SqlPreCommandSimple.md) or [SqlPreCommand](SqlPreCommand.md) as a parameter, and delegate execution to the current [Connector](Connector.md).

## Methods

### ExecuteScalar
Executes a SQL command and returns the first column of the first row in the result set.

```csharp
public static object? ExecuteScalar(string sql, List<DbParameter>? parameters = null, CommandType commandType = CommandType.Text)
public static object? ExecuteScalar(this SqlPreCommandSimple preCommand, CommandType commandType = CommandType.Text)
```

### ExecuteNonQuery
Executes a SQL command and returns the number of rows affected.

```csharp
public static int ExecuteNonQuery(string sql, List<DbParameter>? parameters = null, CommandType commandType = CommandType.Text)
public static int ExecuteNonQuery(this SqlPreCommandSimple preCommand, CommandType commandType = CommandType.Text)
```

### ExecuteDataTable
Executes a SQL command and returns the result as a `DataTable`.

```csharp
public static DataTable ExecuteDataTable(string sql, List<DbParameter>? parameters = null, CommandType commandType = CommandType.Text)
public static DataTable ExecuteDataTable(this SqlPreCommandSimple preCommand, CommandType commandType = CommandType.Text)
```

### UnsafeExecuteDataReader
Executes a SQL command and returns a `DbDataReaderWithCommand` for manual data reading and disposal.

```csharp
public static DbDataReaderWithCommand UnsafeExecuteDataReader(string sql, List<DbParameter>? parameters = null, CommandType commandType = CommandType.Text)
public static DbDataReaderWithCommand UnsafeExecuteDataReader(this SqlPreCommandSimple preCommand, CommandType commandType = CommandType.Text)
public static Task<DbDataReaderWithCommand> UnsafeExecuteDataReaderAsync(string sql, List<DbParameter>? parameters = null, CommandType commandType = CommandType.Text, CancellationToken token = default)
public static Task<DbDataReaderWithCommand> UnsafeExecuteDataReaderAsync(this SqlPreCommandSimple preCommand, CommandType commandType = CommandType.Text, CancellationToken token = default)
```

### ExecuteLeaves
Executes all the leaves of a [SqlPreCommand](SqlPreCommand.md) as non-query commands.

```csharp
public static void ExecuteLeaves(this SqlPreCommand preCommand, CommandType commandType = CommandType.Text)
```

### BulkCopy
Performs a bulk copy operation to the specified destination table.

```csharp
public static void BulkCopy(DataTable dt, List<IColumn> column, ObjectName destinationTable, SqlBulkCopyOptions options, int? timeout)
```

---

For more details, see the documentation for [Connector](Connector.md), [SqlPreCommandSimple](SqlPreCommandSimple.md), and [SqlPreCommand](SqlPreCommand.md).
