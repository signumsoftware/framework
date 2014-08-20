# Field Attributes

We should have it clear now that in Signum Framework, entities rule. The database just reflects the structure of your entities and there's not a lot of room for customization. 

There are, however, some situations where you have to enrich your entities with database-related information, like **Indexes**, specifying **Scale** and **Precision** for numbers, or use **different database types**. 

We use .Net Attributes over the entity **fields** to specify this information, but there are ways to override this information for entities that are not in your control. 

Let's see the available attributes: 

### IgnoreAttribute

Applied to field, prevent it from being a column and participating in any database related activity (Save, Retrieve, Queries...). Usually you need to calculate it after retrieving using `PostRetrieve` method. 

### NotNullableAttribute

Signum Framework keeps nullability of columns on the database trying to reduce the type mismatch between .Net and SQL. So by default it will make columns of reference types nullables *(i.e. string or any entity reference)* and value types not nullable *(i.e. int, DateTimes, GUIDs or enums)*. 

If you want to make a value type nullable just use `Nullable<T>` or the more convenient syntax `T?`. But since there's no way to express [non-nullability of reference types](https://roslyn.codeplex.com/discussions/541334) we need the `NotNullableAttribute` instead.

`NotNullableAttribute`, once applied over a field of a reference type, will make the related column not nullable. 

**Important Note:** Signum Framework allows you to save arbitrary objects graphs. When a cycle of new object is found, it saves inconsistent objects for a while, letting some FKs to be null until we know the actual Id of the related entity. If you use `NotNullableAttribute` over an entity reference and something like this happens you will get an exception when saving. Don't use `NotNullableAttribute` on entity fields that could participate in cycles, use `NotNullValidatorAttribute` on the property instead. See more about this in Database - Save