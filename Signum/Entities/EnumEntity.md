#Enums

`enums`, following the [C# Reference](http://msdn.microsoft.com/en-us/library/sbbt4032.aspx), is a Type consisting of a set of name constants. The important thing about `enums` is that this set is known at compile time, so you have all the nice type checking and IntelliSense.

Each `enum` value has two important things. A **numeric value** of the type of the `enums` underlying type (usually int), and a **string identifier**. 

Using `enum` has always been very convenient while you stay in your programming language, but using it from the outside is a bit too complicated for such a simple thing. 

Typically, when you are storing `enums` on DB you have to choose one the following 3 alternatives: 

* **Store the numeric value:** Easy, but hides the meaning of the value if you don't have access to the source code.
* **Store the Identifier:** Easy, but lacks normalization.
* **Creating a table and foreign key to it:** The table could contain the numeric value as primary key and the string identifier as a dependent column. Then other entities could have a foreign key to this table. If done manually is too much work for just an `enum`, but Signum Framework does it for you!.


## Enum Support 

Signum Framework has complete support for `enum` from day 0 in all the areas: localization, queries, save, database generation, ... even synchronization. 

Just place an `enum` field in you entities and the `enum` table and foreign key will be created for you.

Enums have an excellent support for schema synchronization. Just as any other element (Tables, Columns, Types, etc..) the synchronize will ask for renames and then generate a script with the necessary INSERT / DELETE / UPDATE. 

Enums are special however, because when you move (or re-index) an `enum` value the primary key changes. Fortunately the synchronize is smart enough to update all the foreign keys, even when auxiliary primary keys are necessary. 

## EnumEntity\<T> (Advanced topic)

Internally, `EnumEntity<T>` is the table that acts as a bridge between Enums and Entity. This class does two things: 

* Stores the numeric value in the `Id` field.
* Stores the string identifier in the `ToStr` field.

Usually you don't need to know about this class, just if you want to manipulate the enum table using APIs that take a `Entity`
