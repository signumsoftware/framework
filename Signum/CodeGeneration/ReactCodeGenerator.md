# ReactCodeGenerator

The `ReactCodeGenerator` scans all `System.Type` instances in the main assembly that inherit from `ModifiableEntity` and are non-abstract. After grouping them by module name, it generates:

- Default React component (`.tsx`) files for each entity.
- A client TypeScript file for each module to register entity settings and operation settings.
- An optional server file and controller file for each module.
- A typings file for TypeScript type definitions.

### Call Tree

This tree shows the call hierarchy of the main methods. All methods are `protected` and `virtual`, so you can override them.

* `GenerateReactFromEntities`
    * `GetSolutionInfo`
    * **foreach module in** `GetModules`
        * `WriteClientFile` and `GetClientFile`
            * `WriteClientStartMethod`
                * `WritetEntitySettings`
                    * `GetEntitySetting`
                * `WriteOperationSettings`
        * `WriteTypingsFile` and `GetTypingsFile`
        * `WriteServerFile` and `ServerFileName`
        * `WriteControllerFile` and `ControllerFileName`
        * **foreach type in module**
            * `WriteEntityComponentFile` and `GetViewFileName`
                * **foreach property in** `GetProperties`
                    * `WriteProperty`
                        * case `WriteEntityProperty`
                        * case `WriteEmbeddedProperty`
                        * case `WriteMListProperty`
                        * case `WriteValueLine`
