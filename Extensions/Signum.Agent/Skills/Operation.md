### Get TypeInfo

The tool `getTypeInfo` Returns the metadata information for a type, including the operations defined for this type.

IMPORTANT: Always call `getTypeInfo` of the type you want to operate on, to understand the operations available and the structure of the entity.

Every field in the explored type has, among other properties: 
* `name` field: Identifies the field, of the sub-field for embedded entities / collections / mixins. For example: 
  *	`"SimpleField"`: A simple field in the explored entity
  * `"SomeEmbedded.SimpleField"`: A field defined inside an Embedded Entity in a field. 
  * `"SomeCollection/SimpleField"`: A field inside an embedded inside a MList field. 
  * `"[SomeMixin].SimpleField"`: A Field inside an mixin entity. 
* `type` field with: 
  * `name`: Name of the type, could be a basic types (`string`, `number`...), or the name of a entity or embedded. Sometimes more than one type is allowed (ImplementedBy).
  * `isCollection`: An array is expected in an MList format (see below) 
  * `isLite`: a lightweight reference (Lite) is expected (using `EntityType`), like the one from `AutoCompleteLite`.
  * `isFullEntity`: A full entity (using `Type`) is expected. You may need to use `RetrieveEntity` tool!. 
   * `isEmbedded`: An embedded entity is expected (no id).

For `isFullEntity` consider calling `getTypeInfo` for the related type(s). 

### Execute operations on entities

The tool `executeOperation` allows you to modify the state or data of an entity by executing an operation.

If the operation accepts modifications (canBeModified = true), you can return the modified entity in JSON format but remember: 

* Preserve the ticks value to ensure data integrity. 
* If you make any change in an entity, sub-entity or embedded, you need to set `modified = true` in this entity. 
* IMPORTANT: Special properties like `Type`, `id`, `ticks`, `isNew`, `temporalId` and `modified` should be included as they are essential and should always be before any other property on each entity. 
* The fields in `getTypeInfo` are written in PascalCase, but in Json they should be camelCase, except for the `Type` field. 
* When calling `executeOperation` you only need to send the `entity`, not the `canExecute` dictionary. 


#### Modifying MList

Almost all collections inside of entitis are MList<T>.

```TS
export type MList<T> = Array<MListElement<T>>;

export interface MListElement<T> {
  rowId: number | string | null;
  element: T;
}
```

 In Json and MListElement<T> is an array of objects with two values, `rowId` and `element`: 
 * `rowId` is the primary key of the table supporting the MList, or `null` for new `MListElements`.  
 * `element` is the element of the collection, could be a value (`string`, `number`) and Embedded, and Entity or a Lite. 

 Also if you make any modification in an MList (adding or removing elements) you need to set `modified=true` on the parent entity. 

 ### Creating new entities

 Creating new entities (or sub-entites) could be harder than modifying existing ones because you don't start with an example json. Some advices:

 * Check the `TypeInfo` for the desired type.
 * The `Type` property should be set on every entity, sub-entity or embedded entity. If should be the `cleanName` not the `fullName`.
 * For new entities, the `id` property should be skipped.
	
For some types, custom constructor operations cound be defined:

#### Construct

The tool `operation_Construct` creates a new entity of a given type using a constructor operation (i.e. an operation of kind `ConstructOperation`).

Use it when you need to create a fresh entity and the type has a registered constructor operation (typically named `<TypeName>.Create`). The operation may apply default values or business logic during construction.

Returns an `EntityPackTS` with the newly created (unsaved) entity and its `canExecute` dictionary. To persist the entity, follow up with `operation_Execute` using a save operation.

#### ConstructFrom

The tool `operation_ConstructFrom` creates a new entity derived from an existing one using a `ConstructFromOperation`.

Use it when the new entity is logically created *from* another entity (e.g. creating an `Order` from a `Customer`, or cloning/converting an entity). The source entity is passed as JSON (same rules as `executeOperation` apply: include `Type`, `id`, `ticks`, `modified`).

Returns an `EntityPackTS` with the newly constructed entity and its `canExecute` dictionary. The source entity is not modified. To persist the result, follow up with `operation_Execute` using a save operation.

#### ConstructFromMany

The tool `operation_ConstructFromMany` creates a new entity derived from a collection of existing entities using a `ConstructFromManyOperation`.

Use it when an operation logically aggregates or merges several source entities into a new one (e.g. creating a combined shipment from multiple orders). The sources are passed as a JSON array of Lites:

```json
[
  { "EntityType": "Order", "id": 1, "model": "Order 1" },
  { "EntityType": "Order", "id": 2, "model": "Order 2" }
]
```

All lites must be of the same entity type. Returns an `EntityPackTS` with the newly constructed entity and its `canExecute` dictionary. The source entities are not modified. To persist the result, follow up with `operation_Execute` using a save operation.

### Delete

The tool `operation_Delete` permanently deletes an entity using a delete operation (i.e. an operation of kind `DeleteOperation`).

Pass the entity as JSON (same rules as `executeOperation` apply). Returns nothing on success.

**This action is irreversible** — always confirm with the user before calling this tool. Check the `canExecute` dictionary (from a prior retrieve or execute) to verify the delete operation is allowed before attempting it.
