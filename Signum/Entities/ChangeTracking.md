# Change Tracking

One reson we don't support POCO (saving and retrieving object not inheriting from `Entity`), is that the CLR has no infrastructure for knowing what properties have changed in an object from a given *start* moment. 

Not having embedded change tracking means that you have to save the whole object all the time. And since we work mainly with full graphs of objects, that would mean saving the whole graph just for a single update. (We could do change tracking in a separated session/DataContext object instead, but then you need to attach/detach entities and you loose the nice `static class Database`)

The basic responsibility for a `Modifiable` class, the root of any entity related object, is to have a unified model for the Engine to know about change tracking, there are three implementations: 

`MList<T>` implements Modifiable behaviour, but it's better explained in [it's own page](MList.md). 

`Lite<T>` has a degenerated Modifiable behaviour, since is semantically immutable (Fatness and Thinness doesn't affect to the meaning of the Lite). See more about [Lite](Lite.md).

Finally, `ModifiebleEntity` implements `Modifiable` behavior by keeping track of the modified fields , and it's the class we are going to focus on here. 

See more about the hierarchy in [Base Entities](BaseEntities.md). 

### Modifiable 

Basically `Modifiable` contains the property `Modified` of type `ModifiedState`. The possible values are: 

* `SelfModified`: The object itself has been modified.
* `Clean`: The object has not been modified. If checked during `Saving` event it also means no (recursively) child has changes either.
* `Modified`: The object itself has not changes, but some (recursively) child is `SerfModified`. 
* `Sealed`: The object has been retrieved in a `Sealed` context, so any attempt to modify it will throw an exception. Useful for shared caches. 


### ModifiableEntity 

`ModifiableEntity` has four main responsibilities about change tracking: 

* Implementing change tracking by overriding `Modifiable.Modified` property.
* Implementing `INotifyPropertyChanged` interface. Mainly for WPF's Binding infrastructure, so it raises an event when a property has changed.
* Facilitating child change notifications in a declarative fashion. Mainly for Validation.
* Allowing derived classes to do the last three things using a convenient `protected Set` method.

Also, `ModifiableEntity` implements `IDataErrorInfo` interface for validation (See more in [Validation](Validation.md)), and implements `IClonable` explicitly. 


## Implementation of INotifyPropertyChanged

[INotifyPropertyChanged](http://msdn.microsoft.com/en-us/library/system.componentmodel.inotifypropertychanged.aspx) interface is the standard interface since .Net Framework 2.0 for exposing an event when some properties have changed. This is useful for data-binding scenarios. 

### Notify methods

`Notify` method allows you to invalidate a property manually (usually in order to notify affected validations) 

```C#
class ModifiableEntity
{
   public void Notify<T>(Expression<Func<T>> property)
}
```

The method is strongly typed, taking an expression instead of a string

```C#
//So instead of writing
Notify("Name");
//We write
Notify(() => Name);
```

In order to notify changes in `ToString` property, `NotifyToString` can also be used. 

### Set method

The `Set` method is used in any property setter: 

```C#
protected virtual bool Set<T>(ref T field, T value, 
   [CallerMemberNameAttribute]string automaticPropertyName = null)
```

The simple `Set` method has a lot of competences: 

* Check if `field` is equal to `value`, returning false in this case.
* Otherwise:  
  * Detach any declarative event/validation for the current value of `field`. 
  * Set the entitiy as `SelfModified`
  * Set `field = value`.
  * Attach any declarative event/validation for the new value of `field`. 
  * Notify changes in `automaticPropertyName` property. 
  * Notify changes in `Error` property (from `IDataErrorInfo`). 

Notice that `automaticPropertyName` is meant to be used in the property setter and in this case the value is automatically filled by the C# compiler thanks to `CallerMemberNameAttribute`. If used elsewhere has to be set explicitly. 

If the property will affect the entity ToString, `SetToStr` can be used instead:

```C#
//instead of
set { if(Set(ref name, value, "Name")) NotifyToString(); }

//you can write
set { SetToStr(ref name, value, "Name")); }
``` 

## Receiving Child Changes

With `INotifyPropertyChanged` our entities are able to notify the world in the exact moment some property changes, but sometimes what you want is for your entities to get events from their sub-entities in order to make real-time validations on sub-entities or calculate redundant values. 

Attaching and detaching the events is cumbersome:
* Attach events every time the entity is retrieved.
* Attach events every time the sub entity changes, and detach the old ones.
* Since the event could be attached to WPF or any other object, the event delegate field has to be marked as `NonSerialized` and also as Ignore. So you have to wire it back after deserializing the full graph.

The above is also applicable to sub-collections using `INotifyCollectionChanged` instead. 

`NotifyPropertyChanged` and `NotifyCollectionChanged` facilitate your life using a declarative approach for attaching events to sub-entities in a similarly to [VisualBasic's WithEvents](http://msdn.microsoft.com/en-us/library/stf7ebaz.aspx). 

### For Sub-Entities

Place a `NotifyPropertyChangedAttribute` attribute over the field you are interested in, then override `ChildPropertyChanged` like this: 

```C#
public class SchoolEntity: Entity
{
    string name; 
    (...)

    [NotifyPropertyChanged]
    PersonEntity director; 
    (...)

    protected override void ChildPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
       if(sender == director && e.PropertyName == "Name")
       {
           name = director.Name + "'s School";
       }
    }
}
```

### For Sub-Collections

Place a `NotifyCollectionChangedAttribute` over the field you are interested in, then override `ChildCollectionChanged` like this: 

```C#
public class SchoolEntity: Entity
{
    decimal stateFunds; 
    (...)

    [NotifyCollectionChanged]
    MList<PersonEntity> students; 
    (...)

    protected override void ChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
    {
       if(sender == students)
       {
           stateFunds = students.Count * FundsPerStudent; 
       }

       base.ChildCollectionChanged(sender, args);
    }
}
```

### For Sub-Entities in Sub-Collections

Place a `NotifyCollectionChangedAttribute` and `NotifyPropertyChangedAttribute` attribute over the field of the collection you are interested in, then override `ChildPropertyChanged` like this: 

```C#
public class SchoolEntity: Entity
{
    decimal stateFunds; 
    (...)

    [NotifyCollectionChanged, NotifyCollectionChanged]
    MList<PersonEntity> students; 
    (...)

    protected override void ChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
    {
       if(sender == students)
       {
           stateFunds = students.Count * FundsPerStudent; 
       }

       base.ChildCollectionChanged(sender, args);
    }
}
```



