# Legacy Databases

## History
> For year we have been designing the framework with a radical code-first approach. This has allow us to see business software development from a different perspective than any other framework, archive grate levels or re-utilization, and reducing redundancy by using succinct API and run-time intelligence. 

> On the other side, arbitrary conventions in our database schema (like column names and types) has traditionally make us incompatible with legacy databases, making the framework unsuitable in many scenarios. 

> Also, our reluctance to do code generation, specially after deprecating the Visual Studio Item Templates, has stopped us to remove the last bits of boilerplate that can not be removed otherwise. 

We're glad to announce that this two limitations have been solved, making it much easier to use Signum Framework with legacy applications.

* Many new [field attributes](.../Signum.Entities/FieldAttributes.md) have been added to customize the translation from entities to database (like `TableNameAttribute`, `ColumnNameAttribute`, `BackReferenceColumnNameAttribute`, `TicksColumnAttribute`... ).

* Using [PrimaryKey](.../Signum.Entities/PrimaryKey.md) and [PrimaryKeyAttribute](../Signum.Entities/FieldAttributes.md) we can change the type of the PrimaryKey to `int`, `long`, `Guid` or any other `IComparable`, even if the `Id` column is defined in the base `Entity` class.

* The new [EntityCodeGenerator](EntityCodeGenerator.md) is able to generate entities from almost any legacy database using the previous attributes. 

* Finally [LogicCodeGenerator](LogicCodeGenerator.md), [WebCodeGenerator](WebCodeGenerator.md) and [WindowsCodeGenerator](WindowsCodeGenerator.md) can be used to remove the redundant parts of creating Logic files and Web and Windows views and Client files. 

This new features does not sacrifice the experience for greenfield projects, instead they have a positive impact, allowing you to auto-generate Logic, Windows and Web from the hand-made entities. 

The end result is being able to create a full Signum Framework application in a mater of minutes (Ok, maybe some hours...) from the database. Of course you'll have to invest time writing the interesting business logic and polishing the user interface, but a lot of redundant code will be made for you automatically.  

## Connecting to AdventureWorks

The approach that we followed with **Northwind** example database was **revolutionary**: Create Southwind from scratch, maybe reconsidering the design, and load the data using LINQ to SQL to read the legacy database. 

Now with **AdventureWorks** we'll follow an **evolutionary** approach, creating an application with just the minimum database changes necessary, and maybe even allowing the old application(s) and the new one to work at the same time for a while, and when the old application(s) are deprecated, we can evolve the new one easily, alongside with the schema, using the schema synchronizer.

The strategy that you should follow depends on the quality of the database schema, and in the case of big databases, the downtime in production environment while loading the data. 

Take this detailed tutorial about `AdventureWorks` as a guide for converting your legacy database in a Signum Framework application following an **evolutionary** strategy: 

### Step 1: Generate empty application

1. Go to [Create Application](http://signumsoftware.com/en/DuplicateApplication), 
2. Choose "AdventureWorks" as the name.
3. Deselect "Example Entities and Logic", some other dependent modules should be deselected automatically. 
4. Select/Deselect the remaining modules as you need. 
5. Press "Create Application" and wait for the result
6. Download the application and follow the steps 1 to 4 in First steps. 

At this point you should have a version of Southwind that has been renamed to AdventureWorks and without a trace of any Southwind entity. The solution should compile correctly. 

### Step 2: Restore Adventure works database

1. Download "AdventureWorks2012-Full Database Backup.zip" from "http://msftdbprodsamples.codeplex.com/"
2. Restore the backup in "AdventureWorks" database using SQL Management Studio. 
3. If your database server is something different than `localhost`, you'll need to create a `UserConnections.txt` file in `C:\` with something like: 

```
AdventureWorks>Data Source=MyServer;Initial Catalog=AdventureWorks;User ID=sa;Password=sa
```

### Step 3: Disabling any other module

Before generating the entities, makes sense that we comment out any module that could be registering their own tables in the database, complicating the situation.

In `Start` method of `Starter` class, comment out every line between

```
OperationLogic.Start(sb, dqm);
```
to the line 

```
Schema.Current.OnSchemaCompleted();;
```

Both lines NOT included!

Also replace the lines 
```C#
sb.Schema.Settings.OverrideAttributes((ExceptionEntity ua) => ua.User, new ImplementedByAttribute(typeof(UserEntity)));
sb.Schema.Settings.OverrideAttributes((OperationLogEntity ua) => ua.User, new ImplementedByAttribute(typeof(UserEntity)));
````

By 

```C#
sb.Schema.Settings.OverrideAttributes((ExceptionEntity ua) => ua.User, new ImplementedByAttribute(/*typeof(UserEntity)*/));
sb.Schema.Settings.OverrideAttributes((OperationLogEntity ua) => ua.User, new ImplementedByAttribute(/*typeof(UserEntity)*/));
````

### Step 4: Adapting the legacy Schema

If you run the `AdventureWorks.Load` application and choose `[G]enerate` -> `[E]ntities` and exception will be thrown. We'll need to fix that.

Unfortunately is not that easy, we have to override `EntityCodeGenerator` to teach the code generator about the singularities of `AdventureWorks` and how to adapt them to the requirements of Signum Framework. This is the most complicated step. 

* First you need to have some knowledge of how Signum Framework represents [`Entity` and `EmbeddedEntity`](../../Signum.Entities/BaseEntities.md), [`MList<T>`](../../Signum.Entities/MList.md), [`Lite<T>`](../../Signum.Entities/Lite.md), etc... in the database and what are the benefits of using each one. It's recomended to have some previous experience with the framework or make some module manually to get used. 

* Then you need to take a look at the source code of [EntityCodeGenerator](EntityCodeGenerator.md) to see how it generates the entities code, is a self-contained piece of code relatively easy to understand. 

* Then you need to create your own class `AdventureWorksEntityCodeGenerator` that inherits from `EntityCodeGenerator`, and register at the very beginning of the `Main` method in `Program.cs`

```C#
static void Main(string[] args)
{
   CodeGenerator.Entities = new AdventureWorksEntityCodeGenerator();
   ...
}
```

In order to customize the generator, we'll need to start overriding methods in `AdventureWorksEntityCodeGenerator`. 

Signum Framework now is much more flexible with legacy database, supporting arbitrary names of tables and columns, letting you choose the type of the inherited primary key, change Ticks to be a `DateTime` column, supporting default constraints,  etc... But we still require that **every table has exactly one primary key column**, and that **the primary key is not a foreign key**. 

If you take a look at the [Adventure Works database diagram](http://merc.tv/img/fig/AdventureWorks2008.gif), these restrictions are almost not followed by any table. No panic!! We can classify the problems and solve them one by one. 

1. Some **main tables**, like `Person`, `Store` or `Vendor` have a primary key that is, at the same time, foreign key and primary key. This is a way to create 1-to-1 relationships that Signum Framework does not allow, so we'll need to create a new column in this tables for the foreign key.

2. Most **relational tables**, like `SalesTerritory`, `ProductDocument` or `BusinessEntityAddress` have multiple foreign keys as primary keys and do not have their own unique Id. But if we add a new `ID PRIMARY KEY IDENTITY`,  and we add one multiple-column unique index on the old primary keys, any code that access the table should be unaffected **as long as there are no FKs to this table**. Fortunately that's the case for most of the tables with a few exceptions.

3. The tables `SaleOrderDetail`, `PurchaseOrderDetail`, `EmailAddress` have a foreign key as part of their primary key columns, but they also contain a valid unique column that will serve us as `Id`, so we just have to remove the foreign key from the list of primary key columns. 

4. `SpecialOfferProduct` is the only that is referred that has multiple primary keys. In this case we'll need to remove the foreign key manually and replace them by a simple foreign key, or refer to `SpecialOffer` and `Product` directly. *(Well, actually is not necessary because the FKs are gone in AdventureWorks2012 for some reason!!)*.

After this analysis, we can override `GetTables` to make some modifications in the retrieved database schema before any code is generated. This one will work for example: 

```C#
public class AdventureWorksEntityCodeGenerator : EntityCodeGenerator
{
    protected override List<DiffTable> GetTables()
    {
        List<DiffTable> tables = base.GetTables();

        var dic = tables.ToDictionary(a => a.Name.Name);

        //Problem 3, remove redundant primary keys
        dic["PurchaseOrderDetail"].Columns["PurchaseOrderID"].PrimaryKey = false;
        dic["SalesOrderDetail"].Columns["SalesOrderID"].PrimaryKey = false;
        dic["EmailAddress"].Columns["BusinessEntityID"].PrimaryKey = false;

        foreach (var t in tables)
        {
            //Problem 2, replace multiple primary keys by unique index and an a new Id column  
            if (t.Columns.Values.Count(a => a.PrimaryKey) > 1)
            {
                var list = t.Columns.Values.Where(a => a.PrimaryKey).ToList();

                foreach (var item in list)
                    item.PrimaryKey = false;

                var index = new DiffIndex { Columns = list.Select(a => a.Name).ToList(), IsUnique = true, IndexName = "UIX_" + list.ToString(a => a.Name, "_"), Type = DiffIndexType.NonClustered };
                t.Indices.Add(index.IndexName, index);

                t.Columns.Add("Id", new DiffColumn
                {
                    Name = "Id",
                    Identity = true,
                    SqlDbType = SqlDbType.Int,
                    PrimaryKey = true,
                }); 
            }

            //Problem 1, primary keys that are also forein keys, split in two columns
            var primaryKey = t.Columns.Values.SingleOrDefault(a => a.PrimaryKey);
            if (primaryKey != null && primaryKey.ForeignKey != null)
            {
                var clone = primaryKey.Clone();
                clone.PrimaryKey = false;
                clone.Name = "id" + this.GetRelatedEntity(t, clone).RemoveSuffix("DN");
                var index = new DiffIndex { Columns = new List<string> { clone.Name }, IsUnique = true, IndexName = "UIX_" + clone.Name, Type = DiffIndexType.NonClustered };

                t.Indices.Add(index.IndexName, index);

                primaryKey.ForeignKey = null;

                t.Columns = t.Columns.Values.PreAnd(clone).ToDictionary(a => a.Name);
            }
        }

        return tables;
    }

    ...
}
```

### Step 5: Polishing the generated entities.

If we try to generate the entities now, using `[G]enerate` -> `[E]ntities` again, it will work, but the result could be improved a little bit. You could make the changes manually but we'll see how to teach the `EntityCodeGenerator` to follow your orders:

1.  Some entities contain `SqlHierarchyId` or `SqlGeometry`. We need to include a reference to `Microsoft.SqlServer.Types` assembly and add the necessary namespace overriding `GetUsingNamespaces`.  

    ```C#
    protected override List<string> GetUsingNamespaces(string fileName, IEnumerable<DiffTable> tables)
    {
        var result = base.GetUsingNamespaces(fileName, tables);
        if (tables.Any(t => t.Columns.Values.Any(c => c.UserTypeName != null)))
            result.Add("Microsoft.SqlServer.Types");
    
        return result;
    }
    ```
2. Some tables, like `PersonCreditCard` reference to `Person` but the column name is `BusinessEntityID`, let's override it so the field name makes more sense: 
   ```C#
   protected override string GetFieldName(DiffTable table, DiffColumn col)
   {
       if (col.Name == "BusinessEntityID" && col.ForeignKey != null)
           return GetEntityName(col.ForeignKey.TargetTable).RemoveSuffix("DN").FirstLower();

       return base.GetFieldName(table, col);
   }
   ```
3. Many tables have a `ModifiedDate` column of type `DateTime` for concurrency control. Signum Framework uses `Ticks` of type `long`, but we can override it using `TicksColumnAttribute`! 
   ```C#
   protected override string GetTicksColumnAttribute(DiffTable table)
   {
       if (table.Columns.ContainsKey("ModifiedDate"))
           return "TicksColumn(true, Name =\"ModifiedDate\", Type = typeof(DateTime), Default=\"getdate()\")";

       return "TicksColumn(false)";
   }
   ```
   Then we don't need to generate the `ModifiedDate` field anymore:
   ```C#
   protected override string WriteField(string fileName, DiffTable table, DiffColumn col)
   {
       if (col.Name == "ModifiedDate")
           return null;

       return base.WriteField(fileName, table, col);
   }
   ```
4. And finally, the most important step: We can tell the generator what tables should be considered `MList<T>` of a parent entity. This is a good idea if there are a few elements in the list that should be modified by the parent entity, like the lines in an order, not if there are plenty of independent entities, like the persons in a country. 
   
   For example, `SalesOrderDetail`, `PurchaseOrderDetail`, `ProductProductPhoto` and `PersonPhone` and `EmailAddress` could better be manipulated as an `MList` of **embedded entities** inside a parent entity, than as entities that will be manipulated independently.

   **Note:** The base implementation of `GetMListInfo` is now (2016) smart enought to detect MList-like tables using heuristics, still is interesting to see how you can override it in case this heuristics fail.

   ```C#
   protected override MListInfo GetMListInfo(DiffTable table)
   {
       switch (table.Name.Name)
       {
           case "SalesOrderDetail": return new MListInfo(table.Columns.GetOrThrow("SalesOrderID"));
           case "PurchaseOrderDetail": return new MListInfo(table.Columns.GetOrThrow("PurchaseOrderID"));
           case "ProductProductPhoto": return new MListInfo(table.Columns.GetOrThrow("ProductID"));
           case "PersonPhone": return new MListInfo(table.Columns.GetOrThrow("BusinessEntityID"));
           case "EmailAddress": return new MListInfo(table.Columns.GetOrThrow("BusinessEntityID"));
           default: return null;
       }
   }
   ```

   For other tables, like `SalesOrderHeaderSalesReason`, `ProductModelIllustration` and `ProductDocument`, an `MList` with an embedded entity will be an overkill, because the embedded entity will contains just a reference to another table. You can use `TrivialElementColumn` to directly refer the related entity in the `MList`. 

   ```C#
   protected override MListInfo GetMListInfo(DiffTable table)
   {
       switch (table.Name.Name)
       {
           case "SalesOrderDetail": ... 
           [...]
           case "SalesOrderHeaderSalesReason": return new MListInfo(table.Columns.GetOrThrow("SalesOrderID")) 
           { 
               TrivialElementColumn = table.Columns.GetOrThrow("SalesReasonID") 
           };
           case "ProductModelIllustration": return new MListInfo(table.Columns.GetOrThrow("ProductModelID")) 
           { 
               TrivialElementColumn = table.Columns.GetOrThrow("IllustrationID")
           };
           case "ProductDocument": return new MListInfo(table.Columns.GetOrThrow("ProductID")) 
           { 
               TrivialElementColumn = table.Columns.GetOrThrow("DocumentNode") 
           };
           default: return null;
       }
   }
   ```


Nice! now if run `AdventureWorks.Load` and choose  `[G]enerate` -> `[E]ntities`, a lot of classes should have been generated in `AdventureWorks.Entities`, you can see them with "Show all files" in Solution Explorer. Just include them in the project and they should compile cleanly after including the reference to `Microsoft.SqlServer.Types`.

**Note:** Don't essitate creating the entities too fast, check the generated result and keep iterating by overriding methods and re-generating again until the results are satisfactory. Designing good entities is the most important step building an application with Signum Framework. 


### Step 6: Generate Logic class. 

Generating the logic will be more straight forward. If we run `[G]enerate` -> `[L]ogic` it will start asking us how to group the entities into modules and what expressions to register. In our case this is a little bit annoying, since they are already grouped by namespace. Let's create a new `AdventureWorksLogicCodeGenerator` and override `GetModules` and `ShouldWriteExpression`: 

```C#
    public class AdventureWorksLogicCodeGenerator : LogicCodeGenerator
    {
        protected override IEnumerable<Module> GetModules()
        {
            return GroupByNamespace(CandidateTypes(), this.SolutionName + ".Entities");
        }

        public static IEnumerable<Module> GroupByNamespace(List<Type> candidates, string baseNamespace)
        {
            var result = candidates.Where(a => a != typeof(ApplicationConfigurationEntity)).GroupBy(a => a.Namespace).Select(gr => new Module
            {
                ModuleName = gr.Key == baseNamespace ? "Internals" :
                             gr.Key.RemoveStart(baseNamespace.Length + 1),
                Types = gr.ToList(),
            }).ToList();

            return result;
        }

        protected override bool ShouldWriteExpression(LogicCodeGenerator.ExpressionInfo ei)
        {
            return true;
        }
    }
```

Then we need to register our new class just as we did before, at the beginning of the `Main` method:

```C#
CodeGenerator.Logic = new AdventureWorksLogicCodeGenerator();
```

Let's run `[G]enerate` -> `[L]ogic` one more time and now it generates all the logic files, without asking questions.   

Just as we did before, include all the generated files in `AdventureWorks.Logic`. They should compile cleanly. 

Also, in the `Start` method, after the commented out region, call the newly generated logic classes so they became part of the in-memory representation of the database schema, like this: 

```C#
InternalsLogic.Start(sb, dqm);
ProductionLogic.Start(sb, dqm);
PersonLogic.Start(sb, dqm);
HumanResourcesLogic.Start(sb, dqm);
PurchasingLogic.Start(sb, dqm);
SalesLogic.Start(sb, dqm);
```

You'll need to include the namespaces to make it compile. 

### Step 7: Add Sql Migrations to the database

If everything has gone right, the application now should contain all the information to generate the database schema from scratch, and if we create new Sql Migrations by running `[SQL] Migrations` in `AdventureWorks.Load`, the changes should be relatively small: 

1. The synchronizer will ask for the renames of the some unnecessary columns in the tables that have been converted to MList, answer no ('n'). 
2. It will also ask for removing controlled indexes. This is because some AdventureWorks indexes have the same convention than the ones generated by the framework. Answer no. 
3. Ask for the default value for the new non-nullable columns, like the primary-foreign keys that have been splitted in primary key and foreign key. Just press [Enter].
4. Finally, it will ask you to create some recommended indexes in foreign keys. Also not necessary for now.

The generated script should contains the necessary modifications to adapt the database to the requirements of Signum Framework. While the script looks long, the modifications are pretty harmless: 

1. Set Snapshot isolation as the default.
2. Create some mandatory tables, like `TypeEntity`, `OperationSymbol`, `OperationLogEntity` and `ExceptionEntity`.
3. On the tables that have been converted to an `MList` drop `ModifiedDate`, alongside with his default constraint.
4. Drop the multi-column primary key constraints and add the new `Id INT IDENTITY NOT NULL PRIMARY KEY`, finally creating a multi-column unique index in the table. 
5. For the new non-nullable columns, an script to remove the unnecessary Default constaint will be created. Also we need to fill information in this new fields:

  ```SQL
  update Person.Person set idBusinessEntity = BusinessEntityID
  update Person.Password set idPerson = BusinessEntityID
  update Purchasing.Vendor set idBusinessEntity = BusinessEntityID
  update Sales.Store set idBusinessEntity = BusinessEntityID
  update Sales.SalesPerson set idEmployee = BusinessEntityID
  update HumanResources.Employee set idPerson = BusinessEntityID
  ```

6. Many foreign keys and indexes are renamed to follow the conventions of Signum Framework.
7. All the necessary rows in `TypeEntity` are created.
8. If cache module has been started, it will enable the notification broker. 
9. All the necessary rows in `OperationSymbol` are created.

Once you have reviewed and understood the script, fell free to execute it using Sql Migrations.
   
AdventureWorks has evolved to SignumAdventureWorks, now is free to grow wings and fangs :P 


### Step 8: Re-enable modules

1. Let's turn back to `Start` method in `Starter` class and bring back the commented-out code. 
2. Create another Sql Migration. Depending how many modules we selected in Step 1, more tables will be created. Fortunately, the synchronized does everything for us. It is possible that some of the stages of the synchronized have exception because previous ones have not been executed. Don't worry, the generated script will be just fine. Execute it all removing the exception messages and create another migration.
3. Load the application one more time, but this time run `[CS] C#-Migrations` to create some basic entities:
    1. Create Culture Info
    2. Import Export Chart Scripts

**Note:** Import Export AuthRules won't work untill you define your roles and export them.  

Create Sql Migrations one more time and now that we have the `CultureInfoEntity` registered, the  `EmailTemplateEntity` for the remember password will be created.

Your application is growing fast! 

### Step 9: Create example users

Also we will need to create some simple users and roles if we want to be able to log-in in the application (if Authorization module has been selected). 

Add this method in Program.cs:

```C#
public static void LoadUsers()
{
    using (Transaction tr = new Transaction())
    {
        RoleEntity role = new RoleEntity { MergeStrategy = MergeStrategy.Intersection, Name = "SuperUser" }.Save();

        new UserEntity
        {
            UserName = "su",
            PasswordHash = Security.EncodePassword("su"),
            Role = role,
            State = UserState.Saved,
        }.Save();

        new UserEntity
        {
            UserName = "System",
            PasswordHash = Security.EncodePassword("System"),
            Role = role,
            State = UserState.Saved,
        }.Save();
        
        tr.Commit();
    }

}
```

And register it C# Migrations. 

```C#
 {LoadUsers},
```

Then execute the new method using `[CS]-C# Migrations`. 

### Step 10: Generate Web Views

Just as we did with logic, we need to create a `AdventureWorksWebCodeGenerator` that groups the entities by name-space to avoid annoying questions: 

```C#
public class AdventureWorksWebCodeGenerator : WebCodeGenerator
{
    protected override IEnumerable<Module> GetModules()
    {
        return AdventureWorksLogicCodeGenerator.GroupByNamespace(CandiateTypes(), this.SolutionName + ".Entities");
    }
}
```

And we also need to register it in the `Main` method.  

```C#
CodeGenerator.React = new AdventureWorksWebCodeGenerator();
```

Then, if we just run `[G]enerate -> [React]` many new files should be created in `AdventureWorks.React/App`>

* For each module:
    * N Typescript React-Components (1 for each view).
    * 1 Typescript Client module to register the views and other client stuff.
    * 1 C# Web.API Controller Example. 
    * 1 C# Server file to register server stuff. 

Just register the new server classes in Global.asax `WebStart` method, just before the line `OmniboxServer.Start(config,`: 

```C#
InternalsServer.Start(config);
ProductionServer.Start(config);
PersonServer.Start(config);
HumanResourcesServer.Start(config);
PurchasingServer.Start(config);
SalesServer.Start(config);
```

Similarly, you will need to register the client modules in `Main.tsx` also before `OmniboxClient.start(`:

```C#
InternalsClient.start({ routes });;
ProductionClient.start({ routes });;
PersonClient.start({ routes });;
HumanResourcesClient.start({ routes });;
PurchasingClient.start({ routes });;
SalesClient.start({ routes });;
```

Note: Is recomended to register the modules in the order of dependencies, that's why we put them and the end.   

That's it. Now you can run the web application and log-in with `su` password `su`. A fully featured application with the SearchControl, Operations, Charting, Omnibox and the remaining modules that you could have selected are all available. 

### Step 11: Generate Windows Views

Exactly the same process should be followed for Windows. 

We need to create a `AdventureWorksWindowsCodeGenerator` that groups the entities by name-space to avoid annoying questions: 

```C#
public class AdventureWorksWindowsCodeGenerator : WindowsCodeGenerator
{
    protected override IEnumerable<Module> GetModules()
    {
        return AdventureWorksLogicCodeGenerator.GroupByNamespace(CandiateTypes(), this.SolutionName + ".Entities");
    }

    protected override string GetViewName(Type type)
    {
        var result = base.GetViewName(type);

        if (result == "Location" || result == "Person")
            return result + "View";

        return result;
    }
}
```

`GetViewName` has also been overridden to avoid some conflicts with the view names. 

And we also need to register it in the `Main` method.  

```C#
CodeGenerator.Windows = new AdventureWorksWindowsCodeGenerator();
```

Then, if we just run `[G]enerate -> [Windows]` many new files should be created in 

* `AdventureWorks.Windows/Controls`: Auto-generate controls for each entity, code behind included.
* `AdventureWorks.Windows/Code`: Auto-generated Client classes to register the views.

Just register the new modules in App.xaml.cs in the `Start` method, just before the line `Navigator.Initialize();`: 

```C#
InternalsClient.Start();
ProductionClient.Start();
PersonClient.Start();
HumanResourcesClient.Start();
PurchasingClient.Start();
SalesClient.Start();
```

Also ready!. Now you can run the windows application and log-in with `su` password `su`. A fully featured application with the SearchControl, Operations, Charting, Omnibox and the remaining modules that you could have selected are all available. 


### Step 12: Polish and Maintenance

This step will be the longest by far. The auto-generated stuff is ready, and there's not too much redundancy so everything should easy to change, but now is time that to own the generated code:

* Make changes in the entities.
* Implement business logic.
* Design windows and web user interfaces.
* Create a graph of roles, create users for each employee, and set permission for each role.
* ...
* Finishing is the hardest part! 

Enjoy!
