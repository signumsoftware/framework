# Introduction to Signum.Entities

Signum Entities is the assembly that contains the base entities and attributes for modeling your entities. 

Designing your entities is the central task building an application in Signum Framework, the rest of the application will be affected by the shape of the entities, including:

* Database schema
* Validation
* Serialization
* Big influence in user interface.

Let's start by the most controversial design decisions:

### No POCO support

Every entity has to inherit from some of the base classes in Signum.Entities, so there's no POCO (Plain Old CLR Object) support. 

On the other side, Signum Entities base classes are just plain classes, with normal fields and properties (no `virtual` magic) and provide some useful features out-of-the-box: 

* Embedded change tracking using the `Set` method.
* Implement `INotifyPropertyChanged` so they can be used as WPF View-Models.
* Complete solution for validation implementing `IDataErrorInfo` and using `ValidationAttributes`, `PropertyCheck`, etc...
* Concurrency support. 
* Support for auto-wiring child entity events
* ...

### Automatic Database Schema 

Traditionally this has mean that you need to write the application from scratch 
but recently we allow some kind of flexibility in the Schema mapping using attributes and code generation (see [Legacy Databases tutorial](../Signum.Engine/CodeGeneration/LegacyDatabase.AdventureWorks.md)) 
to get started if you already have a legacy database. 

Still, after this initial step, Signum Framework promotes that the entities shape the schema and the database just follows. 

### Conclusion


* If you want to be able to access your data quickly by drag and dropping some tables to a designer, use **LINQ to SQL** or **Entity Framework**. 
* If you want to create a long-lasting application and save time writing the user interface, business logic, and be able to reuse already made vertical modules because chose **Signum Framework**: 
	* From a legacy database follow [Legacy Databases tutorial](../Signum.Engine/CodeGeneration/LegacyDatabase.AdventureWorks.md).
	* From scratch using (Create Application)[http://www.signumsoftware.com/en/DuplicateApplication] page .  

## Example

People prefer to see the utility of things from the very beginning and follow a top-down approach, so let's start at the end. The next example shows an entity defining a Computer using a lot of the Signum.Entities' available features.


```C#
[Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
public class ComputerEntity : Entity
{
    [UniqueIndex]
    string serialNumber;
    [StringLengthValidator(AllowNulls = false, Min = 10, Max = 12)]
    public string SerialNumber
    {
        get { return serialNumber; }
        set { Set(ref serialNumber, value); }
    }

    ProcessorEntity processor;
    public ProcessorEntity Processor
    {
        get { return processor; }
        set { Set(ref processor, value); }
    }

    Lite<ComputerBrandEntity> brand;
    public Lite<ComputerBrandEntity> Brand
    {
        get { return brand; }
        set { Set(ref brand, value); }
    }

    [ImplementedBy(typeof(HardDiskEntity), typeof(SolidStateDriveEntity))]
    IDrive drive;
    public IDrive Drive
    {
        get { return drive; }
        set { Set(ref drive, value); }
    }

    MList<MemoryModuleEntity> memoryModules;
    public MList<MemoryModuleEntity> MemoryModules
    {
        get { return memoryModules; }
        set { Set(ref memoryModules, value); }
    }

    ComputerState computerState;
    public ComputerState ComputerState
    {
        get { return computerState; }
        set { Set(ref computerState, value); }
    }

    static Expression<Func<ComputerEntity, string>> ToStringExpression = e => e.SerialNumber;
    [ExpressionField]
    public override string ToString()
    {
        return ToStringExpression.Evaluate(this);
    }
}

public enum ComputerState
{
    Demanded,
    OnStock,
    Shipped,
    Sold,
}
```

Some things to notice: 

* The entity is `Serializable`. This is mandatory in order to send the entities to a client application using WCF (Signum.Windows), or store new entities temporally in the view (Signum.Web).

* The `EntityKind` attribute classifies the entity as `EntityKind.Main` and `EntityData.Transactional`. This allows the framework to provide better default behaviour at many levels (cache, user interface buttons, ordering results...). See more in [Entity Kind](EntityKind.md). 

* The entity inherits from the `Entity` class. See more about [Base Entities](BaseEntities.md). 

* The first field, `serialNumber`, is just a plain old int field with a `UniqueIndexAttribute` over it to create an index on the database. See More about [Field Attributes](FieldAttributes.md)

* The property `SerialNumber`, as any other, uses `Set` method to make the assignment. This is useful for [Change Tracking](ChangeTracking.md). 

* The property `SerialNumber` has a `StringLengthValidatorAttribute` to enforce that the string is non null and between 10 and 12 characters. See more about [Validation](Valiation.md).

* The `Processor` field and property have the type `ProcessorEntity`. A foreign key to the `ProcessorEntity`'s table will be created in the database. 

* Since `ComputerBrandEntity` is a heavy entity, the developer uses a `Lite<T>` to make the association to it. Take a look at [Lite](Lite.md).

* There are two kinds of drives, `HardDisk` and `SolidStateDrive`, each one will have its' own table. By using `ImplementedByAttribute` you get a polymorphic foreign key. See more about this in [Inheritance](Inheritance.md). 

* You can have more than one memory module in a computer. Entities uses `MList<T>` to model One-to-Many and Many-to-Many relationships. Know more in [MList](MList.md).

* A computer can be in four states defined in a `enum`. Enums don't have the flexibility to change at run-time, but when logic depends on them they can be very convenient. Signum Entities have friction-free support for [Enums](Enums.md). 

* The framework needs to know the `ToString` representation of any entity and usually stores this value in a `ToStr` column when saving. In this case however `ToString` has been defined using a **Expression**, and is smart enough to save the redundant column in this case. 

### Generated Tables

Let's see now how this entity will be represented in the database. 

```SQL
CREATE TABLE ComputerEntity(
  Id INT IDENTITY NOT NULL PRIMARY KEY,
  Ticks BIGINT NOT NULL,
  SerialNumber NVARCHAR(12) NOT NULL,
  idProcessor INT NULL,
  idBrand INT NULL,
  idDrive_HardDisk INT NULL,
  idDrive_SolidStateDrive INT NULL,
  idComputerState INT NOT NULL
)

CREATE UNIQUE INDEX UIX_SerialNumber ON ComputerEntity(SerialNumber)
CREATE INDEX IX_idProcessor ON ComputerEntity(idProcessor)
CREATE INDEX IX_idBrand ON ComputerEntity(idBrand)
CREATE INDEX IX_idDrive_HardDisk ON ComputerEntity(idDrive_HardDisk)
CREATE INDEX IX_idDrive_SolidStateDrive ON ComputerEntity(idDrive_SolidStateDrive)
CREATE INDEX IX_idComputerState ON ComputerEntity(idComputerState)

ALTER TABLE ComputerEntity ADD CONSTRAINT FK_ComputerDN_idProcessor FOREIGN KEY (idProcessor) REFERENCES ProcessorEntity(Id)
ALTER TABLE ComputerEntity ADD CONSTRAINT FK_ComputerDN_idBrand FOREIGN KEY (idBrand) REFERENCES ComputerBrandEntity(Id)
ALTER TABLE ComputerEntity ADD CONSTRAINT FK_ComputerDN_idDrive_HardDisk FOREIGN KEY (idDrive_HardDisk) REFERENCES HardDiskEntity(Id)
ALTER TABLE ComputerEntity ADD CONSTRAINT FK_ComputerDN_idDrive_SolidStateDrive FOREIGN KEY (idDrive_SolidStateDrive) REFERENCES SolidStateDriveEntity(Id)
ALTER TABLE ComputerEntity ADD CONSTRAINT FK_ComputerDN_idComputerState FOREIGN KEY (idComputerState) REFERENCES ComputerState(Id)

```

As you can see, the table `ComputerEntity` is quite similar to the entity itself, but there are some interesting differences:
* As any `Entity`, it has a auto-numeric primary key with name `Id`. 
* In this case we don't have the `ToStr` column because we used `ToStringExpression`. 
* `Ticks` field is inherited from `Entity` to control concurrency.
* `SerialNumber` is `NOT NULL` and has length 12, determined by the field attributes. 
* `idProcessor` is a foreign keys to `ProcessorEntity`. Is nullable because is a reference type and the framework tries to reduce type mismatch. 
* Similarly, `idBrand` is a foreign key to `ComputerBrandEntity`. `Lite<T>` has no effect at the database level.
* The field `drive` is represented in to columns:  `idDrive_HardDisk` and `idDrive_SolidStateDrive`, following the directives of `ImplmentedByAttribute`. See more about Inheritance. 
* There's no column to represent the `memoryModules` because is a `MList<MemoryModuleEntity>`, instead a table is created: 

```SQL

CREATE TABLE ComputerDNMemoryModule(
  Id INT IDENTITY NOT NULL PRIMARY KEY,
  idParent INT NOT NULL,
  idMemoryModuleEntity INT NULL
)

CREATE INDEX IX_idParent ON ComputerDNMemoryModules(idParent)
CREATE INDEX IX_idMemoryModuleEntity ON ComputerDNMemoryModules(idMemoryModuleEntity)

ALTER TABLE ComputerDNMemoryModules ADD CONSTRAINT FK_ComputerDNMemoryModules_idParent FOREIGN KEY (idParent) REFERENCES ComputerEntity(Id)
ALTER TABLE ComputerDNMemoryModules ADD CONSTRAINT FK_ComputerDNMemoryModules_idMemoryModuleEntity FOREIGN KEY (idMemoryModuleEntity) REFERENCES MemoryModuleEntity(Id)

```
Notice how this table contains an auto-numeric primary key `Id`, a `idParent` referencing the computer, and a reference to `MemoryModuleEntity` table because `MemoryModuleEntity` inherits from `Entity`. 

* Finally, look how `computerState` is also a foreign key, but is `NOT NULL` because enums are value types. The framework even creates the table and inserts the enum values, keeping the Ids in sync with the enum numeric values. 

```SQL
CREATE TABLE ComputerState(
  Id INT NOT NULL PRIMARY KEY,
  ToStr NVARCHAR(200) NULL
)

INSERT ComputerState (Id, ToStr)
 VALUES (0, 'Demanded')

INSERT ComputerState (Id, ToStr)
 VALUES (1, 'OnStock')

INSERT ComputerState (Id, ToStr)
 VALUES (2, 'Shipped')

INSERT ComputerState (Id, ToStr)
 VALUES (3, 'Sold')
```
