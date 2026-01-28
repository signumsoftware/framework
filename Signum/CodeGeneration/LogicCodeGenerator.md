# LogicCodeGenerator

The `LogicCodeGenerator` scans all `System.Type` instances in the main entities assembly that inherit from `Entity` and are non-abstract. After grouping them by module name, it generates logic files that include:

* `sb.Include<T>()` instructions to include each entity type in the database schema.
* `.WithSave(...)`, and `.WithDelete(...)` to register operations.
* `.WithQuery(...)` for registering queries and `WithExpressionFrom(...)` for registering expression queries.

### Call Tree

The following tree shows the call hierarchy of the main methods. All methods are `protected` and `virtual`, so you can override them as needed.

* `GenerateLogicFromEntities`
    * **foreach module in** `GetModules` (using `CandidateTypes` and `CodeGenerator.GetModules`)
        * `WriteFile`
            1. **foreach type in module**
                * `GetExpressions(type)`
                    * `ShouldWriteExpression`
            * `GetUsingNamespaces`
            * `GetNamespace`
            * `WriteLogicClass`
                * **foreach expression**
                    * `WriteExpressionMethod`
                * `WriteStartMethod`
                    1. **foreach type in module**
                        * `WriteInclude`
                            * `ShouldWriteSimpleOperations` (for Save)
                            * `ShouldWriteSimpleOperations` (for Delete)
                            * `ShouldWriteSimpleQuery`
                        * `WriteQuery` (fallback for advanced scenarios)
                        * `WriteOperations` (fallback for advanced scenarios)
                            * **foreach operation in** `GetOperationsSymbols(type)`
                                * `WriteOperation`
                                    * case `WriteExecuteOperation`
                                    * case `WriteDeleteOperation`
                                    * case `WriteConstructFrom`
                                    * case `WriteConstructFromMany`
                                    * case `WriteConstructSimple`
