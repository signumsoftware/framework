# WebCodeGenerator

Reads all the `System.Type` in the main entities assembly that inherit from `ModifiableEntity` and are non-abstract, generates default `cshtml` views for them and, after grouping by module name, generates the Client file and an empty TypeScript file to write the code-behind:

### Call Tree

This tree shows the call hierarchy or the methods, all `protected` and `virtual` so you can override them.

* `GenerateWebFromEntities`
	* `GetSolutionInfo`
	* **foreach module in** `GetModules`
		1. `WriteClientFile` and `GetClientFileName`
			* `GetClienUsingNamespaces`
			* `GetClientNamespace`
			* `WriteClientClass`
				* `GetViewPrefix`
				* `GetJsModule`
				* `WriteStartMethod`
					* `WritetEntitySettings`
						* `GetEntitySetting`
					* `WritetOperationSettings`
		* `WriteTypeScriptFile` and `GetTypeScriptFileName`
		* **foreach type in module**
			* `WriteViewFile` and `GetViewFileName`
				* `GetViewUsingNamespaces`
				* **foreach p in** `GetProperties`
					* `WriteProperty`
						* case `WriteEntityProperty`
						* case `WriteEmbeddedProperty`
						* case `WriteMListProperty`
						* case `WriteValueLine`
