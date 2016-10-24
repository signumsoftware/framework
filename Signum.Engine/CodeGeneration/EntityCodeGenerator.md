# EntityCodeGenerator

Reads the database schema using `SchemaSynchronizer.DefaultGetDatabaseDescription` and generates all the entity classes, including operations. 

Typically, one entity per table is generated with [field attributes](../../Signum.Entities/FieldAttributes.md) (`SqlDbTypeAttribute`, `ColumnAttribute`, `TableAttribute`, `PrimaryKeyAttribute`...) so that once the entities are included in the Schema and Signum takes control of the database schema, executing the Synchronized should produce minimal changes.

More than one entity can be written in the same class if `GetFileName` returns the same value.

`EntityCodeGenerator` is also able to create `MList<T>` fields and `EmbeddedEntity` classes  for some tables, he uses some heuristics but you can override `GetMListInfo` to teach it which tables should be considered `MList<T>`. 

Finally, you can manipulate the source database information by overwriting `GetTables` to remove redundant columns, add foreign keys, etc.. to help `EntityCodeGenerator` create better classes.

### Call Tree

This tree shows the call hierarchy or the methods, all `protected` and `virtual` so you can override them.  

* `GenerateEntitiesFromDatabaseTables`
	1. `GetTables`
	    * `SchemaSynchronizer.DefaultGetDatabaseDescription`
	* `GetSolutionInfo`
	* **Foreach table, grouped by `GetFileName`**:
		* `WriteFile`
			* `GetUsingNamespaces`
			* **Foreach table in the file**
				* `WriteTableEntity`
					1. If `GetMListInfo` is not `null` 
					   * If is trivial return `null`
					   * Otherwise `WriteEmbeddedEntity`
					* If `IsEnum` call `WriteEnum`
	                * Otherwise `WriteEntity`
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
