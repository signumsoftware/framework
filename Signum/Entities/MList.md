﻿# MList\<T> class

`MList<T>` is a collection type that you can use to model One-To-Many and Many-To-Many relationships on Signum Framework. 

This class is a full featured collection with all the methods available in `List<T>`, but it supports data binding (like `ObservableCollection<T>`) and has change tracking embedded. 

### Database Mapping

We have already agreed that Signum Framework is all about writing the entities code and letting the engine have control over the database schema. `MList<T>` is not an exception, and in some senses it is the most radical decisions about database mapping.

Usually you make a One-To-Many relationship between Country and Continent by adding a foreign key in Country pointing to Continent. This is Databases 101, and is what will happen if you add a `ContinentEntity` field in `CountryEntity`.

However, if you add in `ContinentEntity` a field like this: 

```C#
public MList<CountryEntity> Countries { get; set; } = new MList<CountryEntity>();
```

Then the behavior is different, a relational table with name ContinentCountries is created that looks like this:

```SQL
CREATE TABLE ContinentCountries(
  Id INT IDENTITY NOT NULL PRIMARY KEY,
  idParent INT NOT NULL,
  idCountry INT NULL
)

CREATE INDEX IX_idParent ON ContinentCountries(idParent)
CREATE INDEX IX_idCountryEntity ON ContinentCountries(idCountry)

ALTER TABLE ContinentCountries ADD CONSTRAINT FK_ContinentCountries_idParent FOREIGN KEY (idParent) REFERENCES ComputerEntity(Id)
ALTER TABLE ContinentCountries ADD CONSTRAINT FK_ContinentCountries_idCountry FOREIGN KEY (idCountry) REFERENCES CountryEntity(Id)
```

This table has the following columns: 

* `Id`: This column is used internally by the Signum Framework to keep track of each particular element in the list. 
* `idParent`: Contains a reference to the entity that owns the collection, in this case the ContinentEntity. 
* `idCountry`: The actual translation of `T` in the database. In this case, since `CountryEntity` is an entity, a foreign key with name `idCountry` is enough.

This last point is very important. Tables generated by `MList<T>` will be relational tables just in the case that T is another entity, but it could be almost any other thing, so maybe **MList Table** is a better name. 

Other examples could be:

* **Value:** If storing Telephones just as `MList<strings>` in a `PersonEntity` entity, the string column will be included in he MList table. The result will looks like this:

```SQL
CREATE TABLE PersonDNTelepones(
  Id INT IDENTITY NOT NULL PRIMARY KEY,
  idParent INT NOT NULL,
  Value NVARCHAR(200) NULL
)

CREATE INDEX IX_idParent ON PersonDNTelepones(idParent)
ALTER TABLE PersonDNTelepones ADD CONSTRAINT FK_PersonDNTelepones_idParent FOREIGN KEY (idParent) REFERENCES PersonEntity(Id)
```

* **EmbeddedEntity**: The entity properties will just be embedded in the MList table. Very common pattern.
* **Lite**: Just like the reference example.
* **ImplementedBy and ImplementedByAll**: The collection table will be the owner of the polymorphic foreign key.
* **Enums**: ...you get the idea, don't you? 

Is also impotant to note that, in order to reduce type-mismatch, by default the nullability of elements will be exactly the same in SQL than in C#. That means that a MList with entities of embedded entities **could contain null values!**. 99.9% of the time you don't want that, so avoid it using `[NotNullableAttribute]` in your `MList<T>` field. 

Even better, always use `fieldMlist` snippet to create MList fields. 


### Change Tracking and RowId

In the previous version of Signum Framework, `MList<T>` only had two states: Clean and Modified. When an entity with a modified `MList<T>` was being saved, all their elements in the MList table where deleted and re-inserted.

Currently, `MList<T>` internally remembers the `RowId` of each element. This let's the engine be smarter, doing only the necessary INSERTS / DELETES / UPDATES, getting some performance improvements.

Even more important than the performance is that now the RowId are more stable, allowing scenarios like translate string fields in collections. 

If you manipulate the entity with the typical methods (`Add`, `AddRange`, `Remove`, `RemoveAt`, `RemoveAll`, `RemoveRange`) or using the indexer, the MList will remember the RowId of all the unaffected elements, inserting the new ones when saved. 

```C#
PersonEntity person = Database.Retrieve<PersonEntity>(1); 
person.Telephones.RemoveAt(0); //Ok
person.Telephones.Add("664 434 423"); //Ok
person.Telepgones.RemoveAll(t=>!t.StartsWith("6")); //Ok
```

On the other side, if you modify a retrieved entitiy by re-assign the MList field, typically using a LINQ query and `ToMList` extension method, all the previous elements will be removed and replaced by the new ones, **even if they have the same values!**.

```C#
PersonEntity person = Database.Retrieve<PersonEntity>(1); 
person.Telephones = person.Telephones.Where(t=>t.StartWith("6")).ToMList(); //All elements will be replaced!
```

In order to re-set all the elements of a retrieved entity, use `ResetRange` method in the MList instead. This way only the necessary changes will be made and the RowIds will be more stable. 

```C#
PersonEntity person = Database.Retrieve<PersonEntity>(1); 
person.Telephones.ResetRange(person.Telephones.Where(t=>t.StartWith("6"))); //Ok
```

### Remembering Order

Microsoft SQL Server (as many other RDBMS) does not guarantee any particular order if no `ORDER BY` is included in the query. That means that when an entity with an `MList<T>` field is retrieved the elements could be in a different order than they where when saved. 

In many situations the order doesn't matter and this is not an issue, but if order matters, then write a `[PreserveOrderAttribute]` in your `MList<T>` field. This will have the following effect: 

* An `Order` column will be included in the MList table. 
* When the entity is saved, the elements order will be remembered in the `Order` column. 
* When the entity is retrieved, the `MList<T>` order will be sorted according to the `Order` column. 
* Manually sorting the `MList<T>` will mark the `MList<T>` as modified (it doesn't normally). 

Additionally, when binding the `MList<T>` property to any user interface control, like `EntityList`, `EntityStrip`, etc... `Move` will be set to true, typically showing arrows to move elements up and down.  

### MList\<T> vs Back references

Let's consider this two design alternatives:

* **MList of embeddeds**: 
 * `OrderEntity` is an `Entity` with a `MList<OrderLineEmbedded>`.
 * `OrderLineEmbedded` is an `EmbeddedEntity` 
* **Back reference**:  
 * `OrderEntity` is an `Entity`. 
 * `OrderLineEntity` is also an `Entity` with a reference to `OrderEntity`. 

From a strictly relational point of view, this scenarios are very similar, in both cases there is a 1-to-N relationship from `Order` table to `OrderLine` (`OrderLines` in the first example).

But there are other considerations:

* The MList example requires to retrieve all the `OrderLineEntity` every time an `OrderEntity` is retrieved, so will usually be slower if there are many lines. 
* The MList example makes easier to validate the `OrderLineEntity` as part of the `OrderEntity`, for example to check that the `TotalPrice` of the `OrderEntity` is the sum of each `SubTotalPrice` of `OrderLineEntity`. 
* In the MList example you'll typically modify the `OrderLineEntity` by saving the `OrderEntity`, even if the user interface will promote this, while in the second example you'll typically navigate to the `OrderLineEntity` to make the changes, and will be logged independently.
* In one MList example the dependency goes from `OrderEntity` to `OrderLineEntity`, while in the back reference example is the other way around. This could be a problem if the two entities are in different modules. 

In this particular case, `MList` makes more sense, but for other cases, like Countries in a Continent, a back reference and [expressionMethods](../Signum.Utilities/ExpressionTrees/LinqExtensibility.md) are better options. 

#### VirtualMList

Imagine the case with `Order` and `OrderLine`, the desired UI and validation is controlled by the `Order`, so **Back reference** is out of qestion, 
but a **MList of embeddeds** also has his limitations. For example: 

* Imagine the `OrderLine` has his own `MList<DiscountEmbedded>`. It's not allowed to have nested MList so the only solution is to make OrderLine and Entity instead of an Embedded. 
* Imagine that other entities require to make a reference to one particula `OrderLineEmbedded` (like matching OrderLine with InvoiceLine). You can not have a reference to an embedded entity, so again, the OrderLine should be an Entity.

The problem is that if we make an `MList<OrderLineEntity>` then we have a N-to-M relationship between Orders and Lines, in other words, the database structure will allow an OrderLine to be shared between two invoices!

What we want it an `MList<T>` in the UI, but a back reference in the database. This is what a `VirtualMList` is. 

VirtualMList require a little bit more of work: 

* Create the main entity (`OrderLine`) with a MList of the child entity (`MList<OrderLineEntity>`). The MList should be `Ignore` (avoids DB table) but `QueryableProperty` (keeps querying functionality in search control). Optionally add `PreserveOrder` to keep order.

```C#

  [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
  public class OrderEntity : Entity
  {
      public DateTime OrderDate { get; set; }
      (...)

      [Ignore, QueryableProperty] //virtual Mlist
      [NotifyChildProperty, NotifyCollectionChanged, PreserveOrder]
      public MList<OrderLineEntity> Lines { get; set; } = new MList<OrderLineEntity>();

  }

  ```

  * Create a child entity (`OrderLineEntity`) as `Part` with a back reference to the main entity (`OrderEntity`). If you added `PreserveOrder` to keep order, you need to implement `ICanBeOrdered` with an `Order` property. 

```C#
   [Serializable, EntityKind(EntityKind.Part, EntityData.Transactional)]
   public class OrderLineEntity : Entity, ICanBeOrdered
   {
        [NotNullValidator(Disabled = true)]
        public Lite<OrderEntity> Order { get; set; }

        public int Order { get; set; }

        public Lite<ProductEntity> Product { get; set; }

   }
```

* In your Logic class (`OrderLogic`) when registering the main entity, you need add `WithMList` too. 

```C#
  sb.Include<OrderEntity>()
    .WithVirtualMList(p => p.Lines, mc => mc.Order) //<--- Register VirtualMList association
    .WithQuery(() => e => new
    {
        Entity = e,
        e.Id,
        e.OrderDate,
        (...)
    });

```

That's it!. In the UI you can use a normal `EntityTable` / `EntityRepeater` / etc.. but when you remove an element in the Lines MList and save, the `OrderLineEntity` will be deleted from the database. 

There are some overloads of `WithVirtualMList` that allow you to control how the sub-entities are saved / removed, or use your own operations to do it.  

VirtualMList is more complicated but has less limitations. We recomend to use them only when this limitations are making problems and default to the more normal **MList of Embedded** and `EntityTable`, or **Back reference** and `SearchControl` for the common cases. 
