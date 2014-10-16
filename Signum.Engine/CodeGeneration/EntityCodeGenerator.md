# EntityCodeGenerator

Reads the database schema using `SchemaSynchronizer.DefaultGetDatabaseDescription` and generates all the entity classes, including operations. 

Typically, one entity per table is generated with [field attributes](../../Signum.Entities/FieldAttributes.md) (`SqlDbTypeAttribute`, `ColumnAttribute`, `TableAttribute`, `PrimaryKeyAttribute`...) so that once the entities are included in the Schema and Synchronized, the changes should be minimal.

More than one entity can be written in the same class if they return the same `GetFileName` returns the same value.

`EntityCodeGenerator` is also able to create `MList<T>` fields and `EmbeddedEntity` classes  for some tables if you override `GetMListInfo` to tell them which tables should be considered `MList<T>`. 

Finally, you can manipulate the source database information to remove redundant columns, add foreign keys, etc.. to help `EntityCodeGenerator` create better classes.

### Call Tree

This tree shows the call hierarchy or the methods, all `protected` and `virtual` so you can override them.  

* `GenerateEntitiesFromDatabaseTables`
	1. `SchemaSynchronizer.DefaultGetDatabaseDescription`
	* `CleanDiffTables`
	* `GetSolutionInfo`
	* **Foreach table, grouped by `GetFileName`**:
		* `WriteFile`
			* `GetUsingNamespaces`
			* **Foreach table in the file**
				* `WriteEntity`
					1. If `GetMListInfo` is not null and trivial, return null
					* `GetEntityName`
					* `GetEntityAttributes`
						* `GetEntityKind` and `GetEntityData`
  						* `GetTableNameAttribute`
						* `GetPrimaryKeyAttribute`
							* `GetPrimaryKeyColumn`
							* `GetValueType`
							* `GetSqlDbTypeParts`
						* `GetTicksColumnAttribute`
					* `GetBaseClass`
					* `WriteBeforeFields`
					* **Foreach column in table**
						* `WriteField`
							* `GetRelatedEntity`
							* `GetFieldType`
							* `GetFieldName`
							* `GetFieldAttributes`
								* `HasNotNullableAttribute`
								* `GetSqlTypeAttribute`
									* `GetSqlDbTypeParts`
								* `DefaultColumnName`
								* `HasUniqueIndex`
							* `GetPropertyAttributes`
								* `HasNotNullValidator`
							* `IsReadonly`
					* **Foreach relatedTable referencing to table**
						* If `GetMListInfo` is null, skip
						* `WriteFieldMList`
							* if MList of EmbeddedEntities
								* `GetEntityName`
							* else (MList of trivial Entity relationships or values)
								* `GetRelatedEntity`
								* `GetFieldType`
								* `GetFieldAttributes`
							* `GetPrimaryKeyAttribute`
							* `GetTableNameAttribute`
							* `GetBackColumnNameAttribute`
							* `GetFieldMListName`
					* `WriteAfterFields`
					* `WriteToString`
						* `GetToStringColumn`
					* `WriteOperations`
						* If `GetEntityKind` is not Main, Shared or String, skip
						* `GetOperationName`
