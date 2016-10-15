# Executor class

`Executor` static class is the main gate to access the database. It's the only one used by the engine and if you are planning to send raw SQL commands to the current connection, this is the one you have to use. 

Here are the available method overloads, many of them taking [SqlPreCommand](../Engine/SqlPreCommand.md) as a parameter:


### ExecuteScalar

Calls `ExecuteScalar` on a `DbConnection` of the current [Connector](Connector.md)

```C#
public static object ExecuteScalar(string sql)
public static object ExecuteScalar(string sql, List<DbParameter> parameters)
public static object ExecuteScalar(this SqlPreCommandSimple preCommand)
```


### ExecuteNonQuery

Calls `ExecuteNonQuery` on a `DbConnection` of the current [Connector](Connector.md)

```C#
public static object ExecuteNonQuery(string sql)
public static object ExecuteNonQuery(string sql, List<DbParameter> parameters)
public static object ExecuteNonQuery(this SqlPreCommandSimple preCommand)
```

### ExecuteDataTable

Calls `ExecuteDataTable` on a `DbConnection` of the current [Connector](Connector.md)

```C#
public static object ExecuteDataTable(string sql)
public static object ExecuteDataTable(string sql, List<DbParameter> parameters)
public static object ExecuteDataTable(this SqlPreCommandSimple preCommand)
```


### ExecuteDataSet

Calls `ExecuteDataSet` on a `DbConnection` of the current [Connector](Connector.md)

```C#
public static DataSet ExecuteDataSet(string sql)
public static DataSet ExecuteDataSet(string sql, List<DbParameter> parameters)
public static DataSet ExecuteDataSet(this SqlPreCommandSimple preCommand)
```

### ExecuteLeaves

Calls `ExecuteLeaves` on a `DbConnection` of the current [Connector](Connector.md)

```C#
public static void ExecuteLeaves(this SqlPreCommand preCommand)
```


### UnsafeExecuteDataReader

Calls `ExecuteReader` on a `DbConnection` of the current [Connector](Connector.md) and returns the `DbDataReader` for you to use and `Dispose`. 

```C#
public static DbDataReader UnsafeExecuteDataReader(string sql)
public static DbDataReader UnsafeExecuteDataReader(string sql, List<DbParameter> parameters)
public static DbDataReader UnsafeExecuteDataReader(this SqlPreCommandSimple preCommand)
```