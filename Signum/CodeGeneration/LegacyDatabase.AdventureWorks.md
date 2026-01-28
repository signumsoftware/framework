# Legacy Databases

## History
> For years, we have designed the framework with a radical code-first approach. This has allowed us to view business software development from a unique perspective, achieving high levels of reuse and reducing redundancy through a succinct API and runtime intelligence.

> However, arbitrary conventions in our database schema (such as column names and types) have traditionally made us incompatible with legacy databases, limiting the framework's applicability in many scenarios.

> Additionally, our reluctance to use code generation—especially after deprecating Visual Studio Item Templates—prevented us from removing the last bits of boilerplate that couldn't be eliminated otherwise.

We are pleased to announce that these two limitations have been addressed, making it much easier to use Signum Framework with legacy applications.

* Many new [field attributes](../../Signum.Entities/FieldAttributes.md) have been added to customize the translation from entities to the database (e.g., `TableNameAttribute`, `ColumnNameAttribute`, `BackReferenceColumnNameAttribute`, `TicksColumnAttribute`).
* Using [PrimaryKey](../../Signum.Entities/PrimaryKey.md) and [PrimaryKeyAttribute](../../Signum.Entities/FieldAttributes.md), you can change the type of the primary key to `int`, `long`, `Guid`, or any other `IComparable`, even if the `Id` column is defined in the base `Entity` class.
* The new [EntityCodeGenerator](EntityCodeGenerator.md) can generate entities from almost any legacy database using these attributes.
* [LogicCodeGenerator](LogicCodeGenerator.md) and [ReactCodeGenerator](ReactCodeGenerator.md) can be used to remove redundant parts of creating logic files, React views, and client files.

These new features do not sacrifice the experience for greenfield projects; instead, they have a positive impact, allowing you to auto-generate logic and React components from hand-crafted entities.

The end result is the ability to create a full Signum Framework application in a matter of minutes (well, maybe hours) from the database. Of course, you'll still need to invest time writing business logic and polishing the user interface, but much of the redundant code will be generated automatically.

## Connecting to AdventureWorks

With the **Northwind** example database, we took a revolutionary approach: creating Southwind from scratch, reconsidering the design, and loading data using LINQ to SQL to read the legacy database.

With **AdventureWorks**, we'll follow an evolutionary approach: creating an application with minimal database changes, possibly allowing the old and new applications to run simultaneously for a while. When the old applications are deprecated, you can evolve the new one easily, along with the schema, using the schema synchronizer.

The strategy you should follow depends on the quality of the database schema and, for large databases, the downtime required in production while loading data.

This tutorial guides you through converting your legacy AdventureWorks database into a Signum Framework application using an evolutionary strategy:

### Step 1: Generate an Empty Application

1. Go to [Create Application](http://signumsoftware.com/en/DuplicateApplication).
2. Choose "AdventureWorks" as the name.
3. Deselect "Example Entities and Logic"; some dependent modules will be deselected automatically.
4. Select/deselect the remaining modules as needed.
5. Press "Create Application" and wait for the result.
6. Download the application and follow steps 1 to 4 in the First Steps guide.

At this point, you should have a version of Southwind renamed to AdventureWorks, with no trace of any Southwind entity. The solution should compile correctly.

### Step 2: Restore AdventureWorks Database

1. Download "AdventureWorks2012-Full Database Backup.zip" from "https://learn.microsoft.com/en-us/sql/samples/adventureworks-install-configure?view=sql-server-ver17&tabs=ssms".
2. Restore the backup to the "AdventureWorks" database using SQL Management Studio.
3. If your database server is not `localhost`, fix `appsettings.json`.

### Step 3: Disable Other Modules

Before generating entities, comment out any module that could register its own tables in the database, complicating the situation.

In the `Start` method of the `Starter` class, comment out every line between:

```
OperationLogic.Start(sb);
```
and
```
Schema.Current.OnSchemaCompleted();
```
(Both lines NOT included!)

Also, replace:
```csharp
sb.Schema.Settings.OverrideAttributes((ExceptionEntity ua) => ua.User, new ImplementedByAttribute(typeof(UserEntity)));
sb.Schema.Settings.OverrideAttributes((OperationLogEntity ua) => ua.User, new ImplementedByAttribute(typeof(UserEntity)));
```
with:
```csharp
sb.Schema.Settings.OverrideAttributes((ExceptionEntity ua) => ua.User, new ImplementedByAttribute(/*typeof(UserEntity)*/));
sb.Schema.Settings.OverrideAttributes((OperationLogEntity ua) => ua.User, new ImplementedByAttribute(/*typeof(UserEntity)*/));
```

### Step 4: Adapt the Legacy Schema

If you run the `AdventureWorks.Terminal` application and choose `[G]enerate` -> `[E]ntities`, an exception will be thrown. We'll need to fix that.

Unfortunately, it's not that easy. You must override `EntityCodeGenerator` to teach the code generator about the singularities of AdventureWorks and how to adapt them to Signum Framework requirements. This is the most complex step.

* First, understand how Signum Framework represents [`Entity` and `EmbeddedEntity`](../../Signum.Entities/BaseEntities.md), [`MList<T>`](../../Signum.Entities/MList.md), [`Lite<T>`](../../Signum.Entities/Lite.md), etc., in the database and the benefits of each. It's recommended to have prior experience with the framework or manually create a module to get used to it.
* Review the source code of [EntityCodeGenerator](EntityCodeGenerator.md) to see how it generates entity code; it's a self-contained piece of code and relatively easy to understand.
* Create your own class `AdventureWorksEntityCodeGenerator` inheriting from `EntityCodeGenerator`, and register it at the very beginning of the `Main` method in `Program.cs`:

```csharp
static void Main(string[] args)
{
   CodeGenerator.Entities = new AdventureWorksEntityCodeGenerator();
   ...
}
```

To customize the generator, override methods in `AdventureWorksEntityCodeGenerator`.

Signum Framework is now much more flexible with legacy databases, supporting arbitrary table and column names, custom primary key types, changing Ticks to a `DateTime` column, supporting default constraints, etc. However, we still require that **every table has exactly one primary key column**, and that **the primary key is not a foreign key**.

If you look at the [AdventureWorks database diagram](http://merc.tv/img/fig/AdventureWorks2008.gif), these restrictions are rarely followed. No panic! We can classify the problems and solve them one by one:

1. Some **main tables** (e.g., `Person`, `Store`, `Vendor`) have a primary key that is also a foreign key. This creates 1-to-1 relationships that Signum Framework does not allow, so you'll need to create a new column in these tables for the foreign key.
2. Most **relational tables** (e.g., `SalesTerritory`, `ProductDocument`, `BusinessEntityAddress`) have multiple foreign keys as primary keys and no unique Id. Add a new `ID PRIMARY KEY IDENTITY` and a multi-column unique index on the old primary keys. Any code accessing the table should be unaffected **as long as there are no FKs to this table**. Fortunately, that's the case for most tables, with a few exceptions.
3. Tables like `SalesOrderDetail`, `PurchaseOrderDetail`, `EmailAddress` have a foreign key as part of their primary key columns, but also contain a valid unique column to serve as `Id`. Remove the foreign key from the list of primary key columns.
4. `SpecialOfferProduct` is the only table referred to with multiple primary keys. Remove the foreign key manually and replace it with a simple foreign key, or refer to `SpecialOffer` and `Product` directly. *(Actually, this is not necessary because the FKs are gone in AdventureWorks2012!)*

Override `GetTables` to modify the retrieved database schema before code generation. For example:

```csharp
public class AdventureWorksEntityCodeGenerator : EntityCodeGenerator
{
    protected override List<DiffTable> GetTables()
    {
        List<DiffTable> tables = base.GetTables();

        var dic = tables.ToDictionary(a => a.Name.Name);

        // Problem 3: remove redundant primary keys
        dic["PurchaseOrderDetail"].Columns["PurchaseOrderID"].PrimaryKey = false;
        dic["SalesOrderDetail"].Columns["SalesOrderID"].PrimaryKey = false;
        dic["EmailAddress"].Columns["BusinessEntityID"].PrimaryKey = false;

        foreach (var t in tables)
        {
            // Problem 2: replace multiple primary keys with a unique index and a new Id column
            if (t.Columns.Values.Count(a => a.PrimaryKey) > 1)
            {
                var list = t.Columns.Values.Where(a => a.PrimaryKey).ToList();

                foreach (var item in list)
                    item.PrimaryKey = false;

                var index = new DiffIndex { Columns = list.Select(a => a.Name).ToList(), IsUnique = true, IndexName = "UIX_" + string.Join("_", list.Select(a => a.Name)), Type = DiffIndexType.NonClustered };
                t.Indices.Add(index.IndexName, index);

                t.Columns.Add("Id", new DiffColumn
                {
                    Name = "Id",
                    Identity = true,
                    SqlDbType = SqlDbType.Int,
                    PrimaryKey = true,
                }); 
            }

            // Problem 1: primary keys that are also foreign keys, split into two columns
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

    // ...
}
```

### Step 5: Polish the Generated Entities

If you generate the entities now (`[G]enerate` -> `[E]ntities`), it will work, but the result could be improved. You could make changes manually, but it's better to teach `EntityCodeGenerator` to follow your rules:

1. Some entities contain `SqlHierarchyId` or `SqlGeometry`. Include a reference to the `dotMorten.Microsoft.SqlServer.Types` assembly and add the necessary namespace by overriding `GetUsingNamespaces`:

    ```csharp
    protected override List<string> GetUsingNamespaces(string fileName, IEnumerable<DiffTable> tables)
    {
        var result = base.GetUsingNamespaces(fileName, tables);
        if (tables.Any(t => t.Columns.Values.Any(c => c.UserTypeName != null)))
            result.Add("Microsoft.SqlServer.Types");
    
        return result;
    }
    ```
2. Some tables, like `PersonCreditCard`, reference `Person` but the column name is `BusinessEntityID`. Override it so the field name makes more sense:
   ```csharp
   protected override string GetFieldName(DiffTable table, DiffColumn col)
   {
       if (col.Name == "BusinessEntityID" && col.ForeignKey != null)
           return GetEntityName(col.ForeignKey.TargetTable).RemoveSuffix("DN").FirstLower();

       return base.GetFieldName(table, col);
   }
   ```
3. Many tables have a `ModifiedDate` column of type `DateTime` for concurrency control. Signum Framework uses `Ticks` of type `long`, but you can override it using `TicksColumnAttribute`:
   ```csharp
   protected override string GetTicksColumnAttribute(DiffTable table)
   {
       if (table.Columns.ContainsKey("ModifiedDate"))
           return "TicksColumn(true, Name =\"ModifiedDate\", Type = typeof(DateTime), Default=\"getdate()\")";

       return "TicksColumn(false)";
   }
   ```
   Then, don't generate the `ModifiedDate` field anymore:
   ```csharp
   protected override string WriteField(string fileName, DiffTable table, DiffColumn col)
   {
       if (col.Name == "ModifiedDate")
           return null;

       return base.WriteField(fileName, table, col);
   }
   ```
4. The most important step: tell the generator which tables should be considered `MList<T>` of a parent entity. This is useful for lists that should be modified by the parent entity (e.g., order lines), not for independent entities (e.g., persons in a country).
   For example, `SalesOrderDetail`, `PurchaseOrderDetail`, `ProductProductPhoto`, `PersonPhone`, and `EmailAddress` are better as `MList` of embedded entities inside a parent entity.

   **Note:** The base implementation of `GetMListInfo` is now smart enough to detect MList-like tables using heuristics, but you can override it if needed:

   ```csharp
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

   For other tables, like `SalesOrderHeaderSalesReason`, `ProductModelIllustration`, and `ProductDocument`, an `MList` with an embedded entity is overkill, since the embedded entity only contains a reference to another table. Use `TrivialElementColumn` to refer to the related entity directly:

   ```csharp
   protected override MListInfo GetMListInfo(DiffTable table)
   {
       switch (table.Name.Name)
       {
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

Now, if you run `AdventureWorks.Load` and choose `[G]enerate` -> `[E]ntities`, many classes should be generated in `AdventureWorks.Entities`. Include them in the project and they should compile cleanly after adding the reference to `Microsoft.SqlServer.Types`.

**Note:** Don't rush to create entities; check the generated result and keep iterating by overriding methods and regenerating until the results are satisfactory. Designing good entities is the most important step in building a Signum Framework application.

### Step 6: Generate Logic Classes

Generating logic is straightforward. If you run `[G]enerate` -> `[L]ogic`, it will ask how to group entities into modules and which expressions to register. This can be annoying since they're already grouped by namespace. Create a new `AdventureWorksLogicCodeGenerator` and override `GetModules` and `ShouldWriteExpression`:

```csharp
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

Register your new class at the beginning of the `Main` method:

```csharp
CodeGenerator.Logic = new AdventureWorksLogicCodeGenerator();
```

Run `[G]enerate` -> `[L]ogic` again; now it generates all logic files without asking questions.

Include all generated files in `AdventureWorks.Logic`. They should compile cleanly.

In the `Start` method, after the commented-out region, call the newly generated logic classes so they become part of the in-memory database schema:

```csharp
InternalsLogic.Start(sb);
ProductionLogic.Start(sb);
PersonLogic.Start(sb);
HumanResourcesLogic.Start(sb);
PurchasingLogic.Start(sb);
SalesLogic.Start(sb);
```

Include the necessary namespaces to compile.

### Step 7: Add SQL Migrations to the Database

If everything has gone well, the application should now contain all the information to generate the database schema from scratch. If you create new SQL migrations by running `[SQL] Migrations` in `AdventureWorks.Load`, the changes should be relatively small:

1. The synchronizer will ask to rename some unnecessary columns in tables converted to MList; answer no ('n').
2. It will ask to remove controlled indexes. Some AdventureWorks indexes have the same convention as those generated by the framework. Answer no.
3. It will ask for default values for new non-nullable columns, like primary-foreign keys split into primary key and foreign key. Just press [Enter].
4. Finally, it will ask to create recommended indexes on foreign keys. Not necessary for now.

The generated script contains the necessary modifications to adapt the database to Signum Framework requirements. While the script may look long, the modifications are harmless:

1. Set snapshot isolation as the default.
2. Create mandatory tables, like `TypeEntity`, `OperationSymbol`, `OperationLogEntity`, and `ExceptionEntity`.
3. In tables converted to `MList`, drop `ModifiedDate` and its default constraint.
4. Drop multi-column primary key constraints and add a new `Id INT IDENTITY NOT NULL PRIMARY KEY`, then create a multi-column unique index.
5. For new non-nullable columns, a script to remove unnecessary default constraints will be created. Also, fill information in these new fields:

  ```sql
  update Person.Person set idBusinessEntity = BusinessEntityID
  update Person.Password set idPerson = BusinessEntityID
  update Purchasing.Vendor set idBusinessEntity = BusinessEntityID
  update Sales.Store set idBusinessEntity = BusinessEntityID
  update Sales.SalesPerson set idEmployee = BusinessEntityID
  update HumanResources.Employee set idPerson = BusinessEntityID
  ```

6. Many foreign keys and indexes are renamed to follow Signum Framework conventions.
7. All necessary rows in `TypeEntity` are created.
8. If the cache module has started, it will enable the notification broker.
9. All necessary rows in `OperationSymbol` are created.

Once you have reviewed and understood the script, feel free to execute it using SQL migrations.

AdventureWorks has evolved to SignumAdventureWorks—now it's ready to grow wings and fangs :)

### Step 8: Re-enable Modules

1. Return to the `Start` method in the `Starter` class and restore the commented-out code.
2. Create another SQL migration. Depending on how many modules you selected in Step 1, more tables will be created. Fortunately, the synchronizer does everything for you. Some stages may throw exceptions because previous ones haven't been executed. Don't worry; the generated script will be fine. Execute it, remove exception messages, and create another migration.
3. Load the application again, but this time run `[CS] C#-Migrations` to create some basic entities:
    1. Create Culture Info
    2. Import/Export Chart Scripts

**Note:** Import/Export AuthRules won't work until you define your roles and export them.

Create SQL migrations again. Now that `CultureInfoEntity` is registered, the `EmailTemplateEntity` for the remember password will be created.

Your application is growing fast!

### Step 9: Create Example Users

You'll need to create some simple users and roles to log in to the application (if the Authorization module is selected).

Add this method in `Program.cs`:

```csharp
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

Register it in C# migrations:

```csharp
{LoadUsers},
```

Then execute the new method using `[CS]-C# Migrations`.

### Step 10: Generate Web Views

As with logic, create an `AdventureWorksWebCodeGenerator` that groups entities by namespace to avoid unnecessary questions:

```csharp
public class AdventureWorksWebCodeGenerator : WebCodeGenerator
{
    protected override IEnumerable<Module> GetModules()
    {
        return AdventureWorksLogicCodeGenerator.GroupByNamespace(CandidateTypes(), this.SolutionName + ".Entities");
    }
}
```

Register it in the `Main` method:

```csharp
CodeGenerator.React = new AdventureWorksWebCodeGenerator();
```

Run `[G]enerate -> [React]`; many new files should be created in `AdventureWorks.React/App`:

* For each module:
    * N TypeScript React components (one for each view)
    * 1 TypeScript client module to register views and other client code
    * 1 C# Web API controller example
    * 1 C# server file to register server code

Register the new server classes in `Global.asax` (`WebStart` method), just before the line `OmniboxServer.Start(config, ... )`:

```csharp
InternalsServer.Start(config);
ProductionServer.Start(config);
PersonServer.Start(config);
HumanResourcesServer.Start(config);
PurchasingServer.Start(config);
SalesServer.Start(config);
```

Similarly, register the client modules in `Main.tsx`, also before `OmniboxClient.start(`:

```typescript
InternalsClient.start({ routes });
ProductionClient.start({ routes });
PersonClient.start({ routes });
HumanResourcesClient.start({ routes });
PurchasingClient.start({ routes });
SalesClient.start({ routes });
```

Note: It's recommended to register modules in dependency order; that's why they're listed at the end.

That's it! Now you can run the web application and log in with `su`/`su`. A fully featured application with SearchControl, Operations, Charting, Omnibox, and any other selected modules is available.

### Step 11: Generate Windows Views

Follow the same process for Windows.

Create an `AdventureWorksWindowsCodeGenerator` that groups entities by namespace to avoid unnecessary questions:

```csharp
public class AdventureWorksWindowsCodeGenerator : WindowsCodeGenerator
{
    protected override IEnumerable<Module> GetModules()
    {
        return AdventureWorksLogicCodeGenerator.GroupByNamespace(CandidateTypes(), this.SolutionName + ".Entities");
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

Override `GetViewName` to avoid conflicts with view names.

Register it in the `Main` method:

```csharp
CodeGenerator.Windows = new AdventureWorksWindowsCodeGenerator();
```

Run `[G]enerate -> [Windows]`; many new files should be created in:

* `AdventureWorks.Windows/Controls`: Auto-generated controls for each entity, including code-behind
* `AdventureWorks.Windows/Code`: Auto-generated client classes to register views

Register the new modules in `App.xaml.cs` (`Start` method), just before the line `Navigator.Initialize();`:

```csharp
InternalsClient.Start();
ProductionClient.Start();
PersonClient.Start();
HumanResourcesClient.Start();
PurchasingClient.Start();
SalesClient.Start();
```

All set! Now you can run the Windows application and log in with `su`/`su`. A fully featured application with SearchControl, Operations, Charting, Omnibox, and any other selected modules is available.

### Step 12: Polish and Maintain

This step will take the longest. The auto-generated code is ready, and there's little redundancy, so everything should be easy to change. Now it's time to own the generated code:

* Make changes to entities
* Implement business logic
* Design Windows and web user interfaces
* Create a graph of roles, create users for each employee, and set permissions for each role
* ...
* Finishing is the hardest part!

Enjoy!
