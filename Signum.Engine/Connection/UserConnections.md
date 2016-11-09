# UserConnections class

`UserConnections` is a simple class that let you replace global connection strings by local connection string defined in a global file. 

> The problem that `UserConnections` class tries to solve is letting each developer have control of his connection string for each application, without changing the `app.config`/`web.config` and potentially commiting the change and affecting other developers. 

>If you have experience in database-cenctric applications, probably the problem sounds familiar, but is more pressing using Signum Framework for two reasons: 

>* **Database changes are more common:** Signum Framework gives you the tools to make it easy to change the database schema, and encourages you to do so often if there have misspelled names, of you need to do refactorings that affect the database.
>* **Database has to mach the application:** Signum Framework tests, to an extent, that the database matches the application requirements when starting, so it's harder to share databases that 'almost' match. 
>* **Test generate databases:** Signum Framework promotes to create artificial test environments to test the application. Confusing it with production because someone changed `app.config` could be catastrophic. 

The most only important method of `UserConnections` class is `Replace`. Given the global `connectionString` (stored in `app.config`), returns the new machine-local `connectionString`. 

```C#
public static class UserConnections
{
    public static string Replace(string connectionString)
}
```

Internally, `UserConnections` is just a `Dictionary<string, string>`, from **database name** to **new connection string**. 

In order to find a replacement, the **database name** is obtained by parsing the original `connectionString` and retrieving the `Database=` or `Initial Catalog=` section from it. 

The dictionary is loaded from a know file, usually located in `C:\UserConnections.txt` (but can be changed with the property `UserConnections.FileName`), that contains lines with the format 

```
DatabaseName>newConectionstring
``` 

The lines can be commented-out using `--` or `//` at the beginning of the line. 

Example of `UserConnections.txt` used to replace target a named instance `localhost\SQLEXPRESS2012` with SQL Server credentials:  

```SQL
SignumTest>Data Source=localhost\SQLEXPRESS2012;Initial Catalog=SignumTest;Integrated Security=true
Southwind>Data Source=localhost\SQLEXPRESS2012;Initial Catalog=Southwind;User ID=sa;Password=sa
-- Southwind>Data Source=123.123.123.123;Initial Catalog=Southwind;User ID=sa;Password=sa.superSecret
```




