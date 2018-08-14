# LogicCodeGenerator

Reads all the `System.Type` in the main entities assembly that inherit from `Entity` and are non-abstract, and, after grouping by module name, generates logic files that include: 

* The `sb.Include<T>()` instructions to include each entity types in the database schema. 
* The `QueryLogic.Queries.Register(key, query)` instructions to register the default query for each type. 
* The `new Graph<T>.Execute(key) {}.Register()` instructions to register the operations declared for the type.

Additionally, the module is also able to declare expression queries to simplify navigating relationships backwards in the business logic, or also register them in `QueryLogic.Queries` to let end users do the same in the `SearchControl`.

### Call Tree

This tree shows the call hierarchy or the methods, all `protected` and `virtual` so you can override them.  

* `GenerateLogicFromEntities`
	* **foreach mod in** `GetModules` using (`CandiateTypes` and `CodeGenerator.GetModules`)
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
							* ShouldWriteSimpleOperations (for Save)
							* ShouldWriteSimpleOperations (for Delete)
							* ShouldWriteSimpleQuery
						* `WriteQuery``
						* `WriteOperations`
							* **foreach operation in** `GetOperationsSymbols(type)`
								* `WriteOperation`
									* case `WriteExecuteOperation`
									* case `WriteDeleteOperation`										
									* case `WriteConstructFrom`									
									* case `WriteConstructFromMany`						
									* case `WriteConstructSimple`