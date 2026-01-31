### Get TypeInfo

The tool `getTypeInfo` Returns the metadata information for a type, including the operations defined for this type. 

### Execute operations on entities

The tool `executeOperation` allows you to modify the state or data of an entity by executing an operation.

If the operation accepts modifications (canBeModified = true), you can return the modified entity in JSON format but remember: 

* Preserve the ticks value to ensure data integrity. 
* If you make any change in an entity, sub-entity or embedded, you need to set `modified = true` in this entity. 
* Special properties like `Type`, `id`, `ticks`, `isNew`, `temporalId` and `modified` should be included as they are essential and should always be sent before any other property. 
* The fields in `TypeInfo` are written in PascalCase, but in Json they should be camelCase, except for the `Type` field. 
* For fields that are not basic types (`string`, `number`...) special attention if the field `isCollection` and `isLite` (if can be both!). 
* If is not a lite it you may need to retrieve the selected entity first. 
* When calling `executeOperation` you only need to send the `entity`, not the `canExecute` dictionary. 


### Modifying MList

Almost all collections inside of entitis are MList<T>.

```TS
export type MList<T> = Array<MListElement<T>>;

export interface MListElement<T> {
  rowId: number | string | null;
  element: T;
}
```

 In Json and MListElement<T> is an array with two values, `rowId` and `element`: 
 * `rowId` is the primary key of the table supporting the MList, or `null` for new `MListElements`.  
 * `element` is the element of the collection, could be a value (`string`, `number`) and Embedded, and Entity or a Lite. 

 Also if you make any modification in an MList (adding or removing elements) you need to set `modified=true` on the parent entity. 

 ### Creating new entities

 Creating new entities (or sub-entites) could be harder than modifying existing ones because you don't start with an example json. Some advices: 

 * Check the `TypeInfo` for the desired type.
 * The `Type` property should be set on every entity, sub-entity or embedded entity. If should be the `cleanName` not the `fullName`.
 * Fo new entities, the `id` property should be skipped. 
