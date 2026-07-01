### Retrieving Entities by Type and Id

The tool `retrieveEntity` allows you to obtain a full entity given its type and id. 

Together with the entity is returned a `canExecute` dictionary that contains the evaluation of all the preconditions to execute operations defined for that entity type.

This is useful to know the the operation will be rejected or not. 
