# Field Attributes

We should have it clear now that in Signum Framework, entities rule. The database just reflects the structure of your entities and there's not a lot of room for customization. 

There are, however, some situations where you have to enrich your entities with database-related information, like **Indexes**, specifying **Scale** and **Precision** for numbers, or use **different database types**. 

We use .Net Attributes over the entity **fields** to specify this information, but you can use [FieldAttributes](../Signum.Engine/Schema/Schema.md) to override this information for entities that are not in your control. 

Also note that if the attribute is set on a `MList<T>` field, it will be used by the MListTable or element column instead. 

```C#
[NonNullable]
MList<string> telephones = new MList<string>(); 
//The 'Value' column in PerdonDNTelephones table will receive the [NonNullable] attribute
```

Let's see the available attributes: 

## Common attributes

### IgnoreAttribute

Applied to field, prevent it from being a column and participating in any database related activity (Save, Retrieve, Queries...). Usually you need to calculate it after retrieving using `PostRetrieve` method. 

### NotNullableAttribute

Signum Framework keeps nullability of columns on the database trying to reduce the type mismatch between .Net and SQL. So by default it will make columns of reference types nullables *(i.e. string or any entity reference)* and value types not nullable *(i.e. int, DateTimes, GUIDs or enums)*. 

If you want to make a value type nullable just use `Nullable<T>` or the more convenient syntax `T?`. But since there's no way to express [non-nullability of reference types](https://roslyn.codeplex.com/discussions/541334) we need the `NotNullableAttribute` instead.

`NotNullableAttribute`, once applied over a field of a reference type, will make the related column not nullable. 

**Important Note:** Signum Framework allows you to save arbitrary objects graphs. When a cycle of new object is found, it saves inconsistent objects for a while, letting some FKs to be null until we know the actual `Id` of the related entity. If you use `NotNullableAttribute` over an entity reference and something like this happens you will get an exception when saving. Don't use `NotNullableAttribute` on entity fields that could participate in cycles, use `NotNullValidatorAttribute` on the property instead. See more about this in Database - Save.


### SqlDbTypeAttribute

Internally, Signum Engine follows the next tables to assign Sql Types to .Net Types:


| C# Type	| SqlDbType
| ----------|--------------- 
| bool	    | Bit
| byte	    | TinyInt
| short	    | SmallInt
| int	    | Int
| long	    | BigInt
| float	    | Real
| double	| Float
| decimal	| Decimal
| char	    | NChar
| string	| NVarChar
| DateTime	| DateTime
| byte[]	| VarBinary
| Guid	    | UniqueIdentifier

| SqlDbType	| Size/Precision
|-----------|-------:
| NVarChar	| 200
| Image	    | 8000
| Binary	| 8000
| Char	    | 1
| NChar	    | 1
| Decimal	| 18

|SqlDbType	|Scale
|-----------|-------:
|Decimal	|2

That means that: 

```C#
string name; // NVARCHAR(200) NULL
decimal price; // DECIMAL(18,2) NOT NULL
```

Sometimes you want to modify this behavior. You can do that using `[SqlDbTypeAttribute]` on any value field. It doesn't work on entity fields, embedded entities or lites, and has the following basic properties:

* **SqlDbType:** The `SqlDbType` enum that determines the underlying T-SQL system type. We trust that there's a conversion from your field type to your DbType, if you map a string to be a Bit column you will get a lot of exceptions. 
* **Size:** Used to set the `Length` (for string columns) or the Precision (for numeric columns).
* **Scale:** Used to determine the `Scale` of numeric columns. 
* **UserDefinedTypeName:** The name of the UDT that will be used, if any. 

Also, notice that if you change the `SqlType` then the default Size/Precision and Scale for the new type will be used, unless explicitly changed: 

```C#
[SqlDbType(SqlDbType=SqlDbType.NChar)]
string name; // NCHAR(1) NULL
      
[SqlDbType(SqlDbType=SqlDbType.NChar, Size = 100)]
string name; // NCHAR(100) NULL
```

Also, SqlServer 2005 deprecated `NTEXT` in favor of `NVARCHAR(MAX)`. To achieve the `MAX` behavior just use `int.MaxValue` as Size: 

```C#
[SqlDbType(Size = int.MaxValue)]
string name; // NVARCHAR(MAX) NULL
```

Additionally, `SqlDbType` has a `Default` property to create a default constraint in the database. This constraint will be ignored by the framework when saving, but will could be used by `UnsafeInsert` or any third-party legacy application accessing the database.    

### ColumnNameAttribute

`ColumnNameAttribute` let's you override the default name of any database column. Example:

```C#
[ColumnName("DepartmentID")]
Lite<DepartmentEntity> department;
```

Note that when applied to a field inside an embedded entity, the name will still be concatenated to the field name of the embedded entity itself. In order to override this you'll need to manipulate the generated `Schema` object directly. 

Also, when this attribute is applied to an `MList<T>` field, it will change the table name suffix (but ` TableNameAttribute` takes precedence), and the item column of the `MListTable`. 

### TableNameAttribute

Allows you to override the [four-part object name](http://technet.microsoft.com/en-us/library/ms187879(v=sql.105).aspx) (Name, Schema, Database and Server) of one table. Can be applied to any **type** that inherits from `Entity`, or any **field** of type `MList<T>`. 

### PrimaryKeyAttribute

This attribute can be applied to **types** inheriting from `Entity` or to **fields** of type `MList<T>` to customize the primary key of the table. 

Signum Framework does not support multi-column primary keys, but you can easily override the column name and, since `Entity` uses `PrimaryKey` structure, it has flexibility to support primary keys with any arbitrary type, like `int`, `long` or `Guid`. 

It also contains two properties to determine if the primary key is `Identity` (at the database level) or has `IdentityBehaviour` (should not be included when inserting). 

The difference is necessary because Sql Server implements `Identity` in `Guid` using `DEFAULT` constraints instead of normal `Identity`. Actually there are two options, `DEFAULT(NEWID())` (for random GUIDs) or `DEFAULT(NEWSEQUENTIALID())` (for faster sequential GUIDs).

Example: 

```C#
[Serializable, EntityKind(EntityKind.Shared, EntityData.Transactional)]
[PrimaryKey(typeof(Guid))]
public class NoteWithDateEntity : Entity
{
  ...
}
```

## BackReferenceColumnNameAttribute

Only applicable on `MList<T>` fields, let's you change the name of the column that refers back to the parent entity. By default is `idParent`. 

The type of this column can not be changed because it has to be the same type than the primary key of the parent entity.

## TicksColumnAttribute 

Allows you to remove the `Ticks` column in the database, together with the ability to detect concurrency problems when two users modify the same entity at the same time. 

Additionally, this attribute allows you to change the `Name`, `SqlDbType`, `Size`, `Scale` and `Default` constraint of the Ticks column, for example, to use a `DateTime` instead of a `long`.  

### UniqueIndexAttribute
Indexes are a very important point of database design in order to improve performance and define database constraints. Indexes can be:

* **Unique**: Allowing just one different value in the column. This constraint will affect your business logic. 
* **Multiple**: Allowing different values in the column but potentially improving performance. 

Signum Framework only takes care of **unique** indexes because they are an important part of your logical schema. **Multiple** indexes, on the other side, will be 'recommended' by the synchronizer for any foreign key but the last word depends on the performance characteristics of your database and the recommendations of your DBA / Microsoft SQL Profiler.

In order to add a Unique Index to some field just add `[UniqueIndexAttribute]` on top of it: 

```C#
[UniqueIndex]
string name;
```

It's common that null values are the only ones that can be repeated, so there's a special property for it: 

```C#
[UniqueIndex(AllowMultipleNulls=true)]
string name;
```

#### Multi-column indexes

`SchemaBuilder` class contains methods to create **unique** multi-column indexes with or without arbitrary `WHERE` statements: 

```C#
public UniqueIndex AddUniqueIndex<T>(
	Expression<Func<T, object>> fields) 
	where T : Entity

public UniqueIndex AddUniqueIndex<T>(
	Expression<Func<T, object>> fields, 
	Expression<Func<T, bool>> where) 
	where T : Entity
```

And similar variants for MList tables

```C#
public UniqueIndex AddUniqueIndexMList<T, V>(Expression<Func<T, MList<V>>> toMList,
	Expression<Func<MListElement<T, V>, object>> fields)
    where T : Entity
                        
public UniqueIndex AddUniqueIndexMList<T, V>(Expression<Func<T, MList<V>>> toMList,
	Expression<Func<MListElement<T, V>, object>> fields, 
	Expression<Func<MListElement<T, V>, bool>> where)
            where T : Entity
       
```

This indexes will be created when you generate/synchronize your application, and will be implemented using [Filtered Indexes](http://msdn.microsoft.com/en-us/library/cc280372.aspx) or [Indexed Views](http://technet.microsoft.com/en-us/library/ms187864.aspx) depending your SQL Server version and the complexity of the where statement. 


### ImplementedBy and ImplementedByAll

See more about this in [Inheritance](Inheritance.md). 

### NotifyOnCollectionChangeAttribute/NotifyPropertyChangedAttribute

See more about this in [Change Tracking](ChangeTracking.md).



## Advanced attributes

### AttachToAllUniqueIndexesAttribute

his allows a field to be included in the list of columns of any other unique index. Used by Isolation module. 

### FieldWithoutPropertyAttribute

Allows a field to have no property. Otherwise an exception is thrown when including the entity in the schema.  

### ForceForeignKeyAttribute 

Fields of enums with `[FlagsAttribute]` do not generate a foreign key. This attribute brings the foreign key back.  

### NullableAttribute

Allows a non-nullable value-type field to be nullable in the database. No use case known. 

### AvoidExpandQueryAttribute

The LINQ provider usually expands any query to retrieve related entities, or the `ToString` of  related lites. Using this attribute the expansions is avoided and the related entities are retrieved (if necessary) in an independent query. Useful for cycles.

### CombineStrategyAttribute

See more about this in [Inheritance](Inheritance.md). 
 

