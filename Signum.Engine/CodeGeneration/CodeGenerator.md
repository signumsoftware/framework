# CodeGenerator

Whenever possible, we prefer hand-made code, succinct API and run-time intelligence instead of code generation, but once you get to the limits of what's possible with the language there's no reason not to save some time letting a machine write the remaining redundant code for you. 

This classes are designed to be run in the Load application, so the solution should be correctly compiling in order to take advantage of it (unlike the previous Visual Studio Item Templates). On the other side they don't require an installer and are more powerful and customizable. 

The aim stills the same though, once the code is generated you should own it, modifying it to your needs. 

`CodeGenerator` class is the facade for 4 classes that do the actual code generation: 

* **[EntityCodeGenerator](EntityCodeGenerator.md):** Useful if you have a legacy database. Reads the database System Views and generates compatible entities and operation declarations.
* **[LogicCodeGenerator](LogicCodeGenerator.md):**  Generates Logic classes from compiled Entity classes, including entity tables, registering queries and expression, and implementing operations.  
* **[WebCodeGenerator](WebCodeGenerator.md):**  Generates Web views from the compiled Entity classes, including the views itself, an empty TypeScript file and the Client class.
* **[WindowsCodeGenerator](WindowsCodeGenerator.md):**  Generates windows views from the compiled Entity classes, including the views itself with the empty code-behind file and the Client class. 

The classes are all self contained and easy to understand and override to adapt to custom needs.

Finally, [**Legacy Databases: Connecting to AdventureWorks**](LegacyDatabase.AdventureWorks.md) is a detailed tutorial about how to use an example legacy application and auto-generate entities, logic, windows and web. 



 