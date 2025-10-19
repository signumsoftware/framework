# EntityCodeGenerator

`EntityCodeGenerator` automates the creation of entity classes by reading the database schema using `SchemaSynchronizer.DefaultGetDatabaseDescription`. It generates one entity per table, including operations and field attributes such as [`SqlDbTypeAttribute`, `ColumnAttribute`, `TableAttribute`, `PrimaryKeyAttribute`](../../Signum/Entities/FieldAttributes.md). This ensures that, once entities are registered in the Schema, synchronizing the database will result in minimal changes.

## Features

- **Multiple Entities per File:** More than one entity can be written in the same class file if `GetFileName` returns the same value for different tables.
- **MList and Embedded Entities:** The generator can create `MList<T>` fields and `EmbeddedEntity` classes for certain tables. Heuristics are used, but you can override `GetMListInfo` to customize which tables are treated as `MList<T>`.
- **Schema Manipulation:** You can override `GetTables` to adjust the source database information, such as removing redundant columns or adding foreign keys, to improve the generated classes.

## Customization

All key methods are `protected` and `virtual`, allowing you to override them for custom behavior.

## Call Tree

The following call tree outlines the main workflow and extension points:

- `GenerateEntitiesFromDatabaseTables`
    1. `GetTables`
        - Uses `SchemaSynchronizer.DefaultGetDatabaseDescription`
    2. `GetSolutionInfo`
    3. For each table, grouped by `GetFileName`:
        - `WriteFile`
            - `GetUsingNamespaces`
            - For each table in the file:
                - `WriteTableEntity`
                    1. If `GetMListInfo` is not `null`:
                        - If trivial, returns `null`
                        - Otherwise, calls `WriteEmbeddedEntity`
                    2. If `IsEnum`, calls `WriteEnum`
                    3. Otherwise, calls `WriteEntity`
                        - `GetEntityName`
                        - `GetEntityAttributes`
                            - `GetEntityKind`, `GetEntityData`
                            - `GetTableNameAttribute`
                            - `GetPrimaryKeyAttribute`
                                - `GetPrimaryKeyColumn`
                                - `GetValueType`
                                - `GetSqlDbTypeParts`
                            - `GetTicksColumnAttribute`
                        - `GetBaseClass`
                        - `WriteBeforeFields`
                        - For each column in table:
                            - `WriteField`
                                - `GetRelatedEntity`
                                - `GetFieldType`
                                - `GetFieldName`
                                - `GetFieldAttributes`
                                    - `HasNotNullableAttribute`
                                    - `GetSqlTypeAttribute`
                                        - `GetSqlDbTypeParts`
                                    - `DefaultColumnName`
                                    - `HasUniqueIndex`
                                - `GetPropertyAttributes`
                                    - `HasNotNullValidator`
                                - `IsReadonly`
                        - For each related table referencing this table:
                            - If `GetMListInfo` is null, skip
                            - `WriteFieldMList`
                                - If MList of EmbeddedEntities:
                                    - `GetEntityName`
                                - Else (MList of trivial Entity relationships or values):
                                    - `GetRelatedEntity`
                                    - `GetFieldType`
                                    - `GetFieldAttributes`
                                - `GetPrimaryKeyAttribute`
                                - `GetTableNameAttribute`
                                - `GetBackColumnNameAttribute`
                                - `GetFieldMListName`
                        - `WriteAfterFields`
                        - `WriteToString`
                            - `GetToStringColumn`
                        - `WriteOperations`
                            - If `GetEntityKind` is not Main, Shared, or String, skip
                            - `GetOperationName`

## Usage

Override the relevant methods to customize entity generation for your database schema and project conventions.
