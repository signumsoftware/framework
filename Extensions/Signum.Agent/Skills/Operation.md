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

### Quick checklist

1. Call `getTypeInfo` for the target type (and related full entities).
2. Build JSON using camelCase fields, keeping `Type` in PascalCase.
3. For MLists, use `{ rowId, element }` items.
4. Execute with `executeOperation` or construct first using `operation_Construct`.

### Key rules

* `Type`, `id`, `ticks`, `isNew`, `temporalId`, `modified` must appear before other fields.
* Use camelCase for member names from TypeInfo.
* Set `modified = true` on any entity/embedded you change.
* For Lites, use `{ EntityType, id, model }`.
* For full entities, use `{ Type, id, ticks, ... }` (include the embedded/mixin fields if present).

### Example: From TypeInfo to entity JSON

Suppose `getTypeInfo("Book")` returns a simplified shape like this:

```json
{
  "kind": "Entity",
  "fullName": "Library.BookEntity",
  "niceName": "Book",
  "members": {
    "Title": { "type": { "name": "string" }, "required": true },
    "Isbn": { "type": { "name": "string" }, "required": true },
    "PublishYear": { "type": { "name": "int" } },
    "IsPublished": { "type": { "isNotNullable": true, "name": "boolean" }, "required": true },
    "Publisher": { "type": { "isLite": true, "name": "Publisher" } },
    "Dimensions": { "type": { "isEmbedded": true, "name": "DimensionsEmbedded" }, "required": true },
    "Dimensions.Width": { "type": { "name": "decimal" }, "required": true },
    "Dimensions.Height": { "type": { "name": "decimal" }, "required": true },
    "Tags": { "type": { "isCollection": true, "name": "string" } },
    "Chapters": { "type": { "isCollection": true, "isEmbedded": true, "name": "ChapterEmbedded" } },
    "Chapters/Title": { "type": { "name": "string" }, "required": true },
    "Chapters/PageCount": { "type": { "name": "int" }, "required": true },
    "[AuditMixin].SourceSystem": { "type": { "name": "string" } }
  }
}
```

A compatible entity JSON (fields only, omitting `canExecute`, `propsMeta`, and operations) would look like:

```json
{
  "Type": "Book",
  "id": 101,
  "ticks": "0",
  "toStr": "Refactoring Recipes",
  "modified": false,
  "title": "Refactoring Recipes",
  "isbn": "978-1-23456-789-0",
  "publishYear": 2024,
  "isPublished": true,
  "publisher": {
    "EntityType": "Publisher",
    "id": 5,
    "model": "Acme Press"
  },
  "dimensions": {
    "Type": "DimensionsEmbedded",
    "toStr": "24 x 17",
    "modified": false,
    "width": 24.0,
    "height": 17.0
  },
  "tags": [
    { "rowId": 1, "element": "refactoring" },
    { "rowId": 2, "element": "patterns" }
  ],
  "chapters": [
    {
      "rowId": 10,
      "element": {
        "Type": "ChapterEmbedded",
        "toStr": "Intro",
        "modified": false,
        "title": "Introduction",
        "pageCount": 12
      }
    }
  ],
  "mixins": {
    "AuditMixin": {
      "Type": "AuditMixin",
      "modified": false,
      "sourceSystem": "Import"
    }
  }
}
```

* `toStr` field of an entity is server-generated and necessary.

### Operations

#### Execute operations on entities

The tool `executeOperation` allows you to modify the state or data of an entity by executing an operation.

If the operation accepts modifications (canBeModified = true), you can return the modified entity in JSON format. Follow the Key rules above, and preserve `ticks` when you do so. When calling `executeOperation` you only need to send the `entity`, not the `canExecute` dictionary. 


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
