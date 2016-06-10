# Validation

Frequently, the type system is not rich enough to express all the restrictions you need on your data, then you have to add checking of your data at runtime. In a typical application these restrictions are written in three different places: 


* **User Interface:** Using custom validation controls, Javascript...
* **Business logic:** Sometimes you can suddenly find some sanity checking before doing some operation.
* **Database:** By using some triggers and stored procedures.
* **Your Entities:** Only in the fortunate ones that already have an entity-centric application.

That makes it way harder to know when some piece of data is valid and when it is not.  

Signum Framework validation strategy, based on the great [Kary Shifflett CodeProject tutorial](http://www.codeproject.com/Articles/24823/WPF-Business-Application-Series-Part-of-n-Busine), gives us some fresh air, moving almost all the validations to the entities. The reason we can do that is: 

* **We write entities first:** If you just drag and drop tables, then adding validation code is harder, even introducing [ad-hoc compilation tricks](http://community.bartdesmet.net/blogs/bart/archive/2007/07/28/c-3-0-partial-methods-what-why-and-how.aspx). It is not easy to add attributes to auto-generated fields and properties, for example.
* **Our entities are serializable:** This allows us to send entities to the client and have all the validation in there as well, so the user gets quick feedback if something is wrong while introducing data.
* **We use modern UI technology:** Signum Windows is only available for WPF, and we are planning Signum Web to be ASP.Net MVC only. 

Our goal is to make a validations system for our entities that, on one side, implements `IDataErrorInfo` interface so it works nicely with WPF bindings, and on the other side if provides the best possible experience to you when writing the entities:

* **Declarative:** Simple per-property validation with Attributes.
* **Imperative:** Powerful per-property validations (maybe taking other property values into account).
* **Overrideable:** Even if you don't have the control of the class (lives in another assembly) you can disable its validations and inject new ones. 

## Declarative (Validation Attributes)

Very often (about 70% in our applications) validations consist of just checking isolated values against a certain parameterized rule:

* This Entity can't be null
* This number has to be between certain values
* This string's length has to be smaller than some value
* This string has to match some regex.
* ...

In order to make these scenarios easy we created `ValidatorAttributes`.

Validators are just attributes with some parameters that you can place in your **properties** (not on fields) to create simple context-free constraints on your property values. Like this: 

```C#
[Serializable]
public class MyEntityEntity: Entity
{
  [StringLengthValidator(Min=3, Max=3), StringCaseValidator(Case.Uppercase)]
  public string Name
  {
     get{...}
     set{...}
  }
}
```

The code above constraints Name to be an upper-case string of exactly three characters long.

Let's see the default ValidatorAttributes that come with the framework:


### Not-Null Validator
By default all the validators let null be a valid value, so this validator should be placed in adition to remove null as a valid avalue. 

```C#
[NotNullValidator]
```

### String Validators

Validators for string properties

```C#
[StringLengthValidator(Min=4, Max=100, AllowNulls=true)] //AllowNulls is false by default, and makes no distiction between empty string and nulls, if the string is not-null should be greater than 4
[StringLengthValidator(Min=4, MultiLine = true)] //MultiLine is false by default, and when set to true allows '\r\n' characters, as well as leading and trailing spaces
[StringCaseValidator(Case.Uppercase)] // Everything in upercase

[EMailValidator] // should be a valid e-Mail address
[TelephoneValidator] // should be a valid telephone number (numbers, spaces and optional '+' prefix). Combine with StringLengthValidator. 
[MultipleTelephoneValidatorAttribute] //List of comma-separated valid telephone numbers
[URLValidator] // should be a valid http/s URL. 
[FileNameValidator] //should no contain symbols that are invalid for a Windows file name. New SF2.
```

### Numeric Validators

Validators for numeric properties (`int`, `long`, `decimal`, `float`, `double`, etc..) 

```C#
[NumberIsValidator(ComparisonType.GreaterThanOrEqualTo, 0)]
[NumberBetweenValidator(0,10)] //Not using C intervals to please user, so 10 is a valid number as well. 
[DecimalsValidator(3)] // Numbers should not have more than 3 decimal places, default is 2. Works only with decimal. 
```

### MList Validators

Validators for `MList<T>` properties

```C#
[CountIsValidator(ComparisonType.GreaterThanOrEqualTo, 3)]  //Limit the number of elements
[NoRepeatValidator] //Avoid repeated elements in a MList  
```

### DateTime Validators

Validators for `DateTime` properties

```C#
[DateTimePrecissionValidatorAttribute(DateTimePrecision.Minutes)] //The datetime shoudl not have seconds or miliseconds

[DaysPrecissionValidatorAttribute] //sortcut for DateTimePrecissionValidatorAttribute(Days)
[SecondsPrecissionValidatorAttribute] //sortcut for DateTimePrecissionValidatorAttribute(Days)
[MinutesPrecissionValidatorAttribute] //sortcut for DateTimePrecissionValidatorAttribute(Days)
```

### TimeSpan Validators

Validators for `TimeSpan` properties

```C#
[TimeSpanPrecissionValidatorAttribute(DateTimePrecision.Minutes)] //The timespan should not have seconds or miliseconds
```


### TypeEntity Validators

Validators for `TypeEntity` properties

```C#
[IsAssignableToValidatorAttribute(typeof(ProductEntity))] //To allow only TypeEntity assignable to one particular type
```

### ErrorMessage

All this validations already have localizeble error messages, but you can customize using `UnlocalizableErrorMessage` (if localization is not an issue) or `ErrorMassege` property, of type `Func<string>` using `Validator` overrides.

```C#
[StringLengthValidator(Min=3, Max=3, UnlocalizableErrorMessage="Please write a string of just 3 characters")]
[StringCaseValidator(Case.Uppercase, UnlocalizableErrorMessage="Please write the string in uppercase")]
public string Name
{
   get{...}
   set{...}
}
```

### Implementing custom Validator Attributes

ValidatorAttributes are just classes that inherit from `ValidatorAttribute` and implement `OverrideError` method and `HelpMessage` property. This is how `NotNullValidatorAttribute` is implemented. 

```C#
public class NotNullValidatorAttribute : ValidatorAttribute
{
    protected override string OverrideError(object obj)
    {
        if (obj == null)
            return ValidationMessage._0IsNotSet.NiceToString();

        return null;
    }

    public override string HelpMessage
    {
        get { return ValidationMessage.BeNotNull.NiceToString(); }
    }
}
```

## Imperative (virtual methods)

Sometimes (about 20%) declarative validations using attributes is not enough because the validation needs some context (other properties) or is just not worth creating a Validator because the rule is not going to be reused.

### PropertyValidation method

In order to provide an imperative validation that provides a nice user experience, the UI infrastructure needs to know what field should be decorated in the UI with a red box.

By overriding `PropertyValidation`, we can tell the validation system witch is the wrong property. Example:

```C#
protected override string PropertyValidation(PropertyInfo pi)
{
    if (pi.Name == nameof(Name) && name == "Neo" && dateOfBirth.Year < 1999)
        return "Nobody was named Neo before The Matrix";

    return null;
}
```

As you see, with this method you get the PropertyInfo as a parameter, and thanks to `Is` extension method you can test for your entity using strongly-typed reflection. This way you don't rely on error-prone strings. 

```C#
public static bool Is<T>(this PropertyInfo pi, Expression<Func<T>> property)
public static bool Is<S, T>(this PropertyInfo pi, Expression<Func<S, T>> property)
```


### StateValidator 

Very often your entities can get different states defined by an Enum and, depending on the value of the state, some properties should or shouldn't be null.

You could test this imperatively using `PropertyValidation` method. Let's see an example using a variation of Order class from Video 1.

```C#
[Serializable]
public class OrderEntity : Entity
{
    DateTime? paidOn;
    public DateTime? PaidOn
    {
        get { return paidOn; }
        set { Set(ref paidOn, value); }
    }

    DateTime? shipDate;
    public DateTime? ShipDate
    {
        get { return shipDate; }
        set { Set(ref shipDate, value); }
    }

    State state;
    public State State
    {
        get { return state; }
        set { Set(ref state, value); }
    }

    protected override string PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(ShipDate))
        {
            if (ShipDate.HasValue && State != State.Shipped)
                return "ShipDate has to be null if the state is {0}".FormatWith(State);
            else if (!ShipDate.HasValue && State == State.Shipped)
                return "ShipDate needs a value if the state is {0}".FormatWith(State); 
        }

        if (pi.Name == nameof(PaidOn))
        {
            if (PaidOn.HasValue && State != State.Ordered)
                return "PaidOn has to be null if the state is {0}".FormatWith(State);
            else if (!PaidOn.HasValue && (State == State.Shipped || State == State.Paid))
                return "PaidOn needs a value if the state is {0}".FormatWith(State);
        }

        return null;
    } 
}

public enum State
{
    Ordered,
    Paid,
    Shipped,
    Canceled
}

```

However, as the number of states and members increase, it gets easier to introduce bugs and harder to read the code. 

`StateValidator` allows you to define this rules using a collection initializer to emulate a table. So we could replace our implementation of `PropertyValidation` with this one: 


```C#
protected override string PropertyValidation(PropertyInfo pi)
{
    return stateValidator.Validate(this, pi);
}

static StateValidator<OrderEntity, State> stateValidator = new StateValidator<OrderEntity, State>(
   e => e.State,        e => e.PaidOn,    e => e.ShipDate){
   {State.Ordered,       false,            false          },
   {State.Paid,          true,             false          },
   {State.Shipped,       true,             true           },
   {State.Canceled,      null,             null           }};
```

As you see, in the constructor of our stateValidator we define first the State property, and then as many related properties as we need. All using Expressions.

Then, for each value in the state enum we add a bool? for each related property. Meaning:

* **True:** The property is mandatory in this state (not null).
* **False:** The property must be null in this state.
* **Null:** The property could or could not be set in this state (rule disabled). 

`StateValidator` is a strange animal between imperative and declarative validation. It's defined in a declarative table in a static field, and then is used in you `PropertyValidation` method, producing a user-friendly string error that uses the `NiceToString` version of your properties and states:
```C#
"Ship Date is necessary on state Shipped"
"Paid on is not allowed on state Ordered"
```
The class is defined like this: 

```C#
public class StateValidator<E, S> : IEnumerable //Just to allow collection initializer
     where E : Entity
     where S : struct
{
   public StateValidator(Func<E, S> getState, params Expression<Func<E, object>>[] properties);
   public void Add(S state, params bool?[] necessary);
   public string Validate(E entity, PropertyInfo pi);
}
```


## Override Validation

In order to make your business modules composable it's necessary some flexibility to change validation of entities defined in other assemblies (maybe not even defined by you). You don't want not being able to use one module because your customer needs that LastName is optional. 

Signum Framework currently provides different options to add or remove validations to your entities:

### ExternalPropertyValidation

An event that each ModifiableEntity **instance** has in order to delegate some imperative validations in another object (typically the parent entity).

```C#
[field: NonSerialized, Ignore]
public event Func<ModifiableEntity, PropertyInfo, string> ExternalPropertyValidation;
```

Subscribe and unsubscribe the event manually could be a little cumbersome. This event is usually used to add validations to child objects, one declarative way to do so is using `ValidateChildPropertyAttribute` over the field of the parent entity so the framework does the event wiring for you. 


```C#
public class ParentEntityEntity: Entity
{
    [ValidateChildPropertyAttribute]
    EntityEntity entity;
    public EntityEntity Entity
    {
       get{ return entity; }
       set{ Set(ref entity, value, ()=>Entity);}
    }
     
    protected virtual string ChildPropertyValidation(ModifiableEntity sender, PropertyInfo pi)
    {
       if(sender == entity && pi.Is(()=>entity.Name) && entity.Name == "AAA")
           return "AAA is an special name and can not be used"; 

       return base.ChildPropertyValidation(sender, pi);
    }
}
```

Using this technique, the events get rewired whenever you change the entity using Set method, after retrieving, and after deserializing the entity. Just like other [child notifications](ChangeTrackin.md).

### StaticValidation 

The previous method for injecting validations needs to track every instance to add the event handler. Sometimes this is not possible (the code is in other assemblies).

A much more convenient way of including imperative validations for entities that we don't have control is using `PropertyValidator<T>.StaticPropertyValidation`:


```C#
Validator.PropertyValidator((MyEntityEntity d) => d.Name).StaticPropertyValidation += (e, pi, value)=>
{
   if(e.Name == "AAA") //or (string)value == "AAA"
      return "AAA is an special name and can not be used"; 
   return null;
};
```

> **Note:*** *Notice how we din't have to code pi.Is(()=> entity.Name) this time, becuase PropertyPack allready works at the property level.


In order to make it work we need to register this change in the validation in every single start project that uses your entity (Client, Server, Loading Application...). Consider create a method or static constructor in a shared assembly.


### Validators Collection

Another way of extending validations for all the entities of a given class is modifying `PropertyValidator<T>.Validators` collection. This `List<ValidationAttributes>` contains the cache of all the `ValidationAttributes` found in every entity and property. You can just add or remove `ValidationAttributes` to this collection and it will affect the validation of all the entities of this type.

Let's see how to remove the `Uppercase` constraint and add a new `Lowercase` one. 

```C#
var list = Validator.PropertyValidator((MyEntityEntity d) => d.Name).Validators;
list.RemoveAll(va=>va is StringCaseValidatorAttribute);
list.Add(new StringCaseValidatorAttribute(Case.Lowercase))
```

Quite flexible, huh?

### Skip some validations

Finally, `PropertValidator<T>` contains events to disable all of this validations, or all together. 

```C#
public class PropertyValidator<T>
{
    //[...]
    public Func<T, bool> IsAplicable { get; set; }
    public Func<T, bool> IsAplicablePropertyValidation { get; set; }
    public Func<T, bool> IsAplicableExternalPropertyValidation { get; set; }
    public Func<T, bool> IsAplicableStaticPropertyValidation { get; set; }
    //[...]
}
```

You can even disable on particular `ValidatorAttrbute` using `IsApplicableValidator<V>` method: 

```C#
Validator.PropertyValidator((NoteWithDateEntity n) => n.Text)
    .IsApplicableValidator<StringLengthValidatorAttribute>(n => n.Text != null && !n.Text.StartWidth("A")); 
```


## Validation Strategy

ModifiableEntity is the base class with Validation support.

ModifiableEntity implements explicitly IDataErrorInfo interface:

```C#
// Provides the functionality to offer custom error information that a user interface can bind to.
public interface IDataErrorInfo
{
    // Gets an error message indicating what is wrong with this object.
    string Error { get; }

    // Gets the error message for the property with the given name.
    string this[string columnName] { get; }
}
```

So this is what we have to provide for a good WPF experience, but as we have seen in some previous examples, Signum Framework however we don't use this methods for validating the entity. Instead:

```C#
public class ModifiableEntity
{ 
   public string IntegrityCheck(); //Full entity integrity check by checking all the properties. 
 
   public string PropertyCheck(Expression<Func<object>> property)  //strongly-typed overload 
   public string PropertyCheck(string propertyName)  //weakly-typed overload 
}
```

* **IDataErrorInfo.Error**: Calls IntegrityCheck.
* **IntegrityCheck**: Call PropertyCheck for all the properties and concatenates the errors.
* **IDataErrorInfo.this[]**: Calls PropertyCheck.
* **PropertyCheck**: if `IsAplicable` doesn't return true, gets the first error for the property in this order:
 1. **ValidatorAttributes:** Get the first error of the Validator list if `IsAplicable` doesn't return true for this validator.
 2. **PropertyValidation:** Imperative internal method, if `IsAplicablePropertyValidation` doesn't return true.
 3. **ExternalPropertyValidation:** Imperative external event, if `IsAplicableExternalPropertyValidation` doesn't return true.
 4. **StaticPropertyValidation:** Imperative static event, if `IsAplicableStaticPropertyValidation` doesn't return true.


### FullIntegirtyCheck & IdentifiableIntegrityCheck 

`IntegrityCheck` method should only take care of the entity itself. Other child entities could be wrong as well but the parent entitiy shouldn't re-validate that for the sake of avoiding duplication.

Instead you can use `Modifiable.FullIntegrityCheck` method that walks along the full object graph and returns a big paragraph of all the entities wrong, with each entity and the actual errors they have.

Also, you can use `Entity.IdentifiableIntegrityCheck`, that does the same thing but for a smaller graph, the identifiable graph (including the entities MLists and Embeddes but stopping on other `IdentifiableEntities`). 

#### Notify Changes

As we have seen, using `PropertyValidation` you can make a property validation dependent of another property like this: 

```C#
public class PersonEntity : Entity
{
    string name;
    public string Name
    {
        get { return name; }
        set { Set(ref name, value); }
    }        

    DateTime dateOfBirth;
    public DateTime DateOfBirth
    {
        get { return dateOfBirth; }
        set { Set(ref dateOfBirth, value); }
    }        

    public override string PropertyValidation(PropertyInfo pi)
    {
        if(pi.Name == nameof(Name) && Name == "Neo" && DateOfBirth.Year < 1999)
            return "Nobody was named Neo before The Matrix"; 
    }
}
```

The previous code is nice, but there's a minor bug if you want a top quality user experience in WPF. If you change `dateOfBirth`, how does `Name` know that it needs to re-evaluate validation? It just doesn't if you don't tell it. Just call `Notify` after setting `dateOfBirth` field like this: 

```C#
public DateTime DateOfBirth
{
   get { return dateOfBirth; }
   set 
   { 
      if(Set(ref dateOfBirth, value, ()=>DateOfBirth))
         Notify(()=>Name); 
   }
}
```

Finally, you could use `ChildCollectionChanged` and `ChildItemPropertyChanged` methods explained in [Change Tracking](ChangeTracking.md) to force re-validate some properties when your children change. 

## Corrupt Entities

Finally, some entities could have optional Corruptness support by including CorruptMixin. Learn more about [Mixins](Mixins.md). 

```C#
[Serializable]
public class CorruptMixin : MixinEntity
{
    CorruptMixin(Entity mainEntity, MixinEntity next) : base(mainEntity, next) { }

    bool corrupt;
    public bool Corrupt
    {
        get { return corrupt; }
        set { Set(ref corrupt, value); }
    }

    protected internal override void PreSaving(ref bool graphModified)
    {
        base.PreSaving(ref graphModified);

        if (Corrupt)
        {
            string integrity = MainEntity.IdentifiableIntegrityCheckBase(); // So, no corruption allowed
            if (string.IsNullOrEmpty(integrity))
            {
                this.Corrupt = false;
                if (!MainEntity.IsNew)
                    Corruption.OnCorruptionRemoved(MainEntity);
            }
            else if (MainEntity.IsNew)
                Corruption.OnSaveCorrupted(MainEntity, integrity);
        }
    }
}
```

While `Entity` has hard-coded support for this mixin: 

```C#
public virtual string IdentifiableIntegrityCheck()
{
    using (Mixins.OfType<CorruptMixin>().Any(c => c.Corrupt) ? Corruption.AllowScope() : null)
    {
        return IdentifiableIntegrityCheckBase();
    }
}
```

Corruptness allows some entities to skip some validations (the ones you allow) while corrupt. This is useful since it gives a unified model for all the dirty tricks you will had to do in order to load your legacy data. 


So what you do is save an entities with Corrupt=true.

* If it has no errors at all it will be saved and corruptness disappears.
* If it has allowed errors only, it will be saved but remain corrupt
* If it has errors that aren't allowed it won't be saved, as usual. 

From the UI point of view, however, they all look like normal problems. You can even find entities by corruptness status. That means that you can let your customers fix their own data without removing your validations for the new 'pure an clean' entities. 

Here you have an example of a full corruptness-enabled entity


```C#
[Mixin(typeof(CorrupMixin))]
public class PersonEntity : Entity
{
    string name;
    [StringLengthValidator(AllowNulls = false)]
    public string Name
    {
        get { return name; }
        set { Set(ref name, value, ()=>Name); }
    }

    DateTime dateOfBirth;
    public DateTime DateOfBirth
    {
        get { return dateOfBirth; }
        set { Set(ref dateOfBirth, value, ()=>DateOfBirth); }
    }

    public override string PropertyValidation(PropertyInfo pi)
    {
        if(pi.Name == nameof(Name) && Corruption.Strict && Name == "Neo" && DateOfBirth.Year < 1999)
            return "Nobody was named Neo before The Matrix"; 
    }

    static PersonEntity()
    {
        Validator.PropertyValidator((PersonEntity n) => n.Name)
            .IsApplicableValidator<StringLengthValidator>(n => Corruption.Strict); 
    }
}



```