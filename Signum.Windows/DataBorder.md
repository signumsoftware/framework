# DataBorder

`DataBorder` control is a simple control, inheriting from `Border`, that does three things:

### Hide Child

Hide the inherited `Child UIElement` when `DataContext == null`. This is convenient to avoid having zombie controls bounded to null, for example using `EntityList` with master-detail binding or `EntityDetail`. 

### Animate DataContextChange

Additionally, if `DataContextChange` event is raised in the control, a fast fade-out/fade-in animation is produced in the `Child UIElement`, useful using `EntityList` with master-detail binding.

### AutoChild

If the property `AutoChild` is set to `true`, `DataBorder` instantiates the appropriate control for the current `DataContext` object type using the controls registered in `Navigator`.   
