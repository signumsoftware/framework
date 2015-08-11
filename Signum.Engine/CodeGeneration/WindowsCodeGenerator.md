# WindowsCodeGenerator

Reads all the `System.Type` in the main entities assembly that inherit from `ModifiableEntity` and are non-abstract, generates default `xaml` views for them, the `.xaml.cs` file with the code behind and, after grouping by module name, generates also the Client files that registers the views.

### Call Tree

This tree shows the call hierarchy or the methods, all `protected` and `virtual` so you can override them.

* `GenerateWindowsFromEntities`
	* `GetSolutionInfo`
	* **foreach module in** `GetModules`
		1. `WriteClientFile` and `GetClientFileName`
			* `GetClienUsingNamespaces`
			* `GetClientNamespace`
			* `WriteClientClass`
				* `WriteStartMethod`
					* `WritetEntitySettings`
						* `GetEntitySetting`
					* `WritetOperationSettings`
		* **foreach type in module**
			* `WriteViewFile` and `GetViewFileName`
				* **foreach p in** `GetProperties`
	 				* `GetViewNamespace`
					* `GetViewName`
					* `GetViewUsingNamespaces
					* `WriteProperty`
						* case `WriteEntityProperty`
						* case `WriteEmbeddedProperty`
						* case `WriteMListProperty`
						* case `WriteValueLine`
			* `WriteViewCodeBehindFile`
				* `GetViewCodeBehindUsingNamespaces`
				* `GetViewNamespace`
				* `WriteViewCodeBehindClass`
